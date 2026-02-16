using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine.Abstractions;
using Editor.Engine.Abstractions.Rendering;
using Editor.Engine.Commands;
using Editor.Engine.Rendering;
using Editor.Nodes;

namespace Editor.Engine;

public sealed class BootstrapEditorEngine : IEditorEngine, IDisposable
{
    private static readonly PreviewFrame EmptyPreviewFrame = new(1, 1, new byte[] { 0, 0, 0, 255 });
    private readonly object _sync = new();
    private readonly NodeGraph _graph = new();
    private readonly IDagValidator _dagValidator = new DagValidator();
    private readonly InputImageStore _inputImageStore = new();
    private readonly EditorCommandProcessor _commandProcessor;
    private readonly INodeModuleRegistry _nodeModuleRegistry;
    private readonly IGraphCompiler _graphCompiler;
    private readonly ITileCache _tileCache;
    private readonly IRenderBackend _renderBackend;
    private readonly IRenderScheduler _renderScheduler;
    private RgbaImage? _lastOutput;

    public BootstrapEditorEngine()
        : this(new BuiltInNodeModuleRegistry())
    {
    }

    public BootstrapEditorEngine(INodeModuleRegistry nodeModuleRegistry)
        : this(
            nodeModuleRegistry,
            new GraphCompiler(),
            new InMemoryTileCache(),
            new SkiaRenderBackend(nodeModuleRegistry),
            new LatestRenderScheduler())
    {
    }

    internal BootstrapEditorEngine(
        INodeModuleRegistry nodeModuleRegistry,
        IGraphCompiler graphCompiler,
        ITileCache tileCache,
        IRenderBackend renderBackend,
        IRenderScheduler renderScheduler)
    {
        _nodeModuleRegistry = nodeModuleRegistry;
        _graphCompiler = graphCompiler;
        _tileCache = tileCache;
        _renderBackend = renderBackend;
        _renderScheduler = renderScheduler;
        _commandProcessor = new EditorCommandProcessor(_graph, _dagValidator, OnGraphMutated);

        Status = "Idle";

        var inputNode = new Node(NodeId.New(), NodeTypes.ImageInput);
        var outputNode = new Node(NodeId.New(), NodeTypes.Output);
        _graph.AddNode(inputNode);
        _graph.AddNode(outputNode);
        _graph.AddEdge(new Edge(inputNode.Id, "Image", outputNode.Id, "Image"), _dagValidator);

        InputNodeId = inputNode.Id;
        OutputNodeId = outputNode.Id;

        AvailableNodeTypes = _nodeModuleRegistry.NodeTypes
            .Where(nodeType => !string.Equals(nodeType.TypeId.Value, NodeTypes.Output, StringComparison.Ordinal))
            .Select(nodeType => nodeType.TypeId.Value)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    public string Status { get; private set; }

    public event EventHandler<PreviewFrame>? PreviewUpdated;

    public IReadOnlyList<Node> Nodes
    {
        get
        {
            lock (_sync)
            {
                return _graph.Nodes
                    .OrderBy(node => node.Id.Value)
                    .ToArray();
            }
        }
    }

    public IReadOnlyList<Edge> Edges
    {
        get
        {
            lock (_sync)
            {
                return _graph.Edges.ToArray();
            }
        }
    }

    public IReadOnlyList<string> AvailableNodeTypes { get; }

    public bool CanUndo
    {
        get
        {
            lock (_sync)
            {
                return _commandProcessor.CanUndo;
            }
        }
    }

    public bool CanRedo
    {
        get
        {
            lock (_sync)
            {
                return _commandProcessor.CanRedo;
            }
        }
    }

    public NodeId InputNodeId { get; private set; }

    public NodeId OutputNodeId { get; private set; }

    public NodeId AddNode(string nodeType)
    {
        lock (_sync)
        {
            if (!_nodeModuleRegistry.TryGet(nodeType, out _))
            {
                throw new InvalidOperationException($"Node type '{nodeType}' is not registered.");
            }

            var node = new Node(NodeId.New(), nodeType);
            _commandProcessor.Execute(new AddNodeCommand(node));
            Status = $"Node '{nodeType}' added";
            RequestPreviewRenderLocked(null);
            return node.Id;
        }
    }

    public void Connect(NodeId fromNodeId, string fromPort, NodeId toNodeId, string toPort)
    {
        lock (_sync)
        {
            _commandProcessor.Execute(new ConnectPortsCommand(new Edge(fromNodeId, fromPort, toNodeId, toPort)));
            Status = $"Connected '{fromPort}' -> '{toPort}'";
            RequestPreviewRenderLocked(null);
        }
    }

    public void Disconnect(NodeId fromNodeId, string fromPort, NodeId toNodeId, string toPort)
    {
        lock (_sync)
        {
            _commandProcessor.Execute(new DisconnectPortsCommand(new Edge(fromNodeId, fromPort, toNodeId, toPort)));
            Status = $"Disconnected '{fromPort}' -> '{toPort}'";
            RequestPreviewRenderLocked(null);
        }
    }

    public void SetParameter(NodeId nodeId, string parameterName, ParameterValue value)
    {
        lock (_sync)
        {
            _commandProcessor.Execute(new SetNodeParameterCommand(nodeId, parameterName, value));
            Status = $"Parameter '{parameterName}' updated";
            RequestPreviewRenderLocked(null);
        }
    }

    public void Undo()
    {
        lock (_sync)
        {
            if (!_commandProcessor.Undo())
            {
                return;
            }

            Status = "Undo";
            RequestPreviewRenderLocked(null);
        }
    }

    public void Redo()
    {
        lock (_sync)
        {
            if (!_commandProcessor.Redo())
            {
                return;
            }

            Status = "Redo";
            RequestPreviewRenderLocked(null);
        }
    }

    public void SetInputImage(RgbaImage image)
    {
        SetInputImage(InputNodeId, image);
    }

    public void SetInputImage(NodeId nodeId, RgbaImage image)
    {
        lock (_sync)
        {
            if (!_graph.TryGetNode(nodeId, out var node))
            {
                throw new InvalidOperationException($"Node '{nodeId}' was not found.");
            }

            if (!string.Equals(node.Type, NodeTypes.ImageInput, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Node '{nodeId}' is type '{node.Type}', expected '{NodeTypes.ImageInput}'.");
            }

            _inputImageStore.Set(nodeId, image);
            _tileCache.Clear();
            Status = $"Image loaded for {nodeId}";
            RequestPreviewRenderLocked(null);
        }
    }

    public GraphDocumentState CaptureGraphDocument()
    {
        lock (_sync)
        {
            var nodes = _graph.Nodes
                .OrderBy(node => node.Id.Value)
                .Select(node => new GraphNodeState(
                    node.Id,
                    node.Type,
                    node.Parameters
                        .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
                        .ToDictionary(parameter => parameter.Key, parameter => parameter.Value, StringComparer.Ordinal)))
                .ToArray();

            var edges = _graph.Edges
                .OrderBy(edge => edge.FromNodeId.Value)
                .ThenBy(edge => edge.FromPort, StringComparer.Ordinal)
                .ThenBy(edge => edge.ToNodeId.Value)
                .ThenBy(edge => edge.ToPort, StringComparer.Ordinal)
                .ToArray();

            return new GraphDocumentState(
                InputNodeId,
                OutputNodeId,
                nodes,
                edges);
        }
    }

    public void LoadGraphDocument(GraphDocumentState document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        lock (_sync)
        {
            var validated = ValidateDocumentState(document);

            _graph.Clear();
            foreach (var node in validated.Nodes)
            {
                _graph.AddNode(new Node(node.NodeId, node.NodeType, node.Parameters));
            }

            foreach (var edge in validated.Edges)
            {
                _graph.AddEdge(edge, _dagValidator);
            }

            InputNodeId = validated.InputNodeId;
            OutputNodeId = validated.OutputNodeId;
            _commandProcessor.Reset();
            _inputImageStore.Clear();
            _tileCache.Clear();
            _lastOutput = null;
            Status = "Graph document loaded";
            RequestPreviewRenderLocked(null);
        }
    }

    public bool TryRenderOutput(out RgbaImage? image, out string errorMessage, NodeId? targetNodeId = null)
    {
        lock (_sync)
        {
            try
            {
                image = EvaluateGraphLocked(CancellationToken.None, targetNodeId);
                _lastOutput = image?.Clone();
                errorMessage = string.Empty;
                return image is not null;
            }
            catch (Exception exception)
            {
                image = null;
                errorMessage = exception.Message;
                Status = $"Render failed: {exception.Message}";
                return false;
            }
        }
    }

    public void RequestPreviewRender(NodeId? targetNodeId = null)
    {
        lock (_sync)
        {
            RequestPreviewRenderLocked(targetNodeId);
        }
    }

    public void Dispose()
    {
        _renderScheduler.Dispose();
        _lastOutput = null;
    }

    private void RequestPreviewRenderLocked(NodeId? targetNodeId)
    {
        Status = "Preview requested";
        _renderScheduler.ScheduleLatest(token => RenderPreviewAsync(token, targetNodeId));
    }

    private void OnGraphMutated()
    {
        _inputImageStore.PruneTo(_graph);
    }

    private async Task RenderPreviewAsync(CancellationToken cancellationToken, NodeId? targetNodeId)
    {
        try
        {
            var output = await Task.Run(() =>
            {
                lock (_sync)
                {
                    return EvaluateGraphLocked(cancellationToken, targetNodeId);
                }
            }, cancellationToken).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (output is not null)
            {
                lock (_sync)
                {
                    _lastOutput = output.Clone();
                    Status = "Preview ready";
                }

                PreviewUpdated?.Invoke(this, new PreviewFrame(output.Width, output.Height, output.ToRgba8()));
                return;
            }

            lock (_sync)
            {
                Status = targetNodeId is null
                    ? "Preview empty"
                    : $"Preview empty at node {targetNodeId}";
            }

            PreviewUpdated?.Invoke(this, EmptyPreviewFrame);
        }
        catch (OperationCanceledException)
        {
            // Intentionally ignored. Newer render request replaced this one.
        }
        catch (Exception exception)
        {
            lock (_sync)
            {
                Status = $"Render failed: {exception.Message}";
            }
        }
    }

    private RgbaImage? EvaluateGraphLocked(CancellationToken cancellationToken, NodeId? targetNodeId = null)
    {
        var evaluationNodeId = targetNodeId ?? OutputNodeId;
        var plan = _graphCompiler.Compile(_graph, evaluationNodeId);
        return _renderBackend.Evaluate(
            _graph,
            plan,
            evaluationNodeId,
            _inputImageStore.Snapshot(),
            _tileCache,
            cancellationToken);
    }

    private GraphDocumentState ValidateDocumentState(GraphDocumentState document)
    {
        if (document.Nodes is null || document.Nodes.Count == 0)
        {
            throw new InvalidOperationException("Graph document must contain at least one node.");
        }

        if (document.Edges is null)
        {
            throw new InvalidOperationException("Graph document edges are required.");
        }

        var normalizedNodes = new List<GraphNodeState>(document.Nodes.Count);
        var seenNodeIds = new HashSet<NodeId>();
        foreach (var node in document.Nodes)
        {
            if (!seenNodeIds.Add(node.NodeId))
            {
                throw new InvalidOperationException($"Duplicate node id '{node.NodeId}'.");
            }

            if (string.IsNullOrWhiteSpace(node.NodeType))
            {
                throw new InvalidOperationException($"Node '{node.NodeId}' has an empty type.");
            }

            if (node.Parameters is null)
            {
                throw new InvalidOperationException($"Node '{node.NodeId}' parameters are required.");
            }

            var normalizedParameters = node.Parameters
                .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
                .ToDictionary(
                    parameter => parameter.Key,
                    parameter => parameter.Value,
                    StringComparer.Ordinal);

            normalizedNodes.Add(new GraphNodeState(node.NodeId, node.NodeType, normalizedParameters));
        }

        if (!seenNodeIds.Contains(document.InputNodeId))
        {
            throw new InvalidOperationException($"Input node '{document.InputNodeId}' does not exist.");
        }

        if (!seenNodeIds.Contains(document.OutputNodeId))
        {
            throw new InvalidOperationException($"Output node '{document.OutputNodeId}' does not exist.");
        }

        var nodeById = normalizedNodes.ToDictionary(node => node.NodeId);
        if (!string.Equals(nodeById[document.InputNodeId].NodeType, NodeTypes.ImageInput, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Input node '{document.InputNodeId}' must be type '{NodeTypes.ImageInput}'.");
        }

        if (!string.Equals(nodeById[document.OutputNodeId].NodeType, NodeTypes.Output, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Output node '{document.OutputNodeId}' must be type '{NodeTypes.Output}'.");
        }

        var normalizedEdges = document.Edges
            .OrderBy(edge => edge.FromNodeId.Value)
            .ThenBy(edge => edge.FromPort, StringComparer.Ordinal)
            .ThenBy(edge => edge.ToNodeId.Value)
            .ThenBy(edge => edge.ToPort, StringComparer.Ordinal)
            .ToArray();

        foreach (var edge in normalizedEdges)
        {
            if (string.IsNullOrWhiteSpace(edge.FromPort) || string.IsNullOrWhiteSpace(edge.ToPort))
            {
                throw new InvalidOperationException("Edges must provide both source and destination port names.");
            }
        }

        var validationGraph = new NodeGraph();
        foreach (var node in normalizedNodes.OrderBy(node => node.NodeId.Value))
        {
            validationGraph.AddNode(new Node(node.NodeId, node.NodeType, node.Parameters));
        }

        foreach (var edge in normalizedEdges)
        {
            validationGraph.AddEdge(edge, _dagValidator);
        }

        var validatedNodes = validationGraph.Nodes
            .OrderBy(node => node.Id.Value)
            .Select(node => new GraphNodeState(
                node.Id,
                node.Type,
                node.Parameters
                    .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
                    .ToDictionary(parameter => parameter.Key, parameter => parameter.Value, StringComparer.Ordinal)))
            .ToArray();

        var validatedEdges = validationGraph.Edges
            .OrderBy(edge => edge.FromNodeId.Value)
            .ThenBy(edge => edge.FromPort, StringComparer.Ordinal)
            .ThenBy(edge => edge.ToNodeId.Value)
            .ThenBy(edge => edge.ToPort, StringComparer.Ordinal)
            .ToArray();

        return new GraphDocumentState(
            document.InputNodeId,
            document.OutputNodeId,
            validatedNodes,
            validatedEdges);
    }
}
