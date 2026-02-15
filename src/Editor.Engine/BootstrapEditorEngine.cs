using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Imaging;

namespace Editor.Engine;

public sealed class BootstrapEditorEngine : IEditorEngine
{
    private static readonly PreviewFrame EmptyPreviewFrame = new(1, 1, new byte[] { 0, 0, 0, 255 });
    private readonly object _sync = new();
    private readonly NodeGraph _graph = new();
    private readonly IDagValidator _dagValidator = new DagValidator();
    private readonly Stack<IEditorCommand> _undoStack = new();
    private readonly Stack<IEditorCommand> _redoStack = new();
    private CancellationTokenSource? _renderCts;
    private RgbaImage? _inputImage;
    private RgbaImage? _lastOutput;

    public BootstrapEditorEngine()
    {
        Status = "Idle";

        var inputNode = new Node(NodeId.New(), NodeTypes.ImageInput);
        var outputNode = new Node(NodeId.New(), NodeTypes.Output);
        _graph.AddNode(inputNode);
        _graph.AddNode(outputNode);
        _graph.AddEdge(new Edge(inputNode.Id, "Image", outputNode.Id, "Image"), _dagValidator);

        InputNodeId = inputNode.Id;
        OutputNodeId = outputNode.Id;
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

    public IReadOnlyList<string> AvailableNodeTypes { get; } = NodeTypeCatalog.All
        .Where(nodeType => !string.Equals(nodeType.Name, NodeTypes.ImageInput, StringComparison.Ordinal) &&
                           !string.Equals(nodeType.Name, NodeTypes.Output, StringComparison.Ordinal))
        .Select(nodeType => nodeType.Name)
        .OrderBy(name => name, StringComparer.Ordinal)
        .ToArray();

    public bool CanUndo
    {
        get
        {
            lock (_sync)
            {
                return _undoStack.Count > 0;
            }
        }
    }

    public bool CanRedo
    {
        get
        {
            lock (_sync)
            {
                return _redoStack.Count > 0;
            }
        }
    }

    public NodeId InputNodeId { get; }

    public NodeId OutputNodeId { get; }

    public NodeId AddNode(string nodeType)
    {
        var node = new Node(NodeId.New(), nodeType);
        ExecuteCommand(new AddNodeCommand(node));
        Status = $"Node '{nodeType}' added";
        RequestPreviewRender();
        return node.Id;
    }

    public void Connect(NodeId fromNodeId, string fromPort, NodeId toNodeId, string toPort)
    {
        ExecuteCommand(new ConnectPortsCommand(new Edge(fromNodeId, fromPort, toNodeId, toPort)));
        Status = $"Connected '{fromPort}' -> '{toPort}'";
        RequestPreviewRender();
    }

    public void SetParameter(NodeId nodeId, string parameterName, ParameterValue value)
    {
        ExecuteCommand(new SetNodeParameterCommand(nodeId, parameterName, value));
        Status = $"Parameter '{parameterName}' updated";
        RequestPreviewRender();
    }

    public void Undo()
    {
        lock (_sync)
        {
            if (_undoStack.Count == 0)
            {
                return;
            }

            var command = _undoStack.Pop();
            command.Undo(_graph, _dagValidator);
            _redoStack.Push(command);
            Status = "Undo";
        }

        RequestPreviewRender();
    }

    public void Redo()
    {
        lock (_sync)
        {
            if (_redoStack.Count == 0)
            {
                return;
            }

            var command = _redoStack.Pop();
            command.Execute(_graph, _dagValidator);
            _undoStack.Push(command);
            Status = "Redo";
        }

        RequestPreviewRender();
    }

    public void SetInputImage(RgbaImage image)
    {
        lock (_sync)
        {
            _inputImage = image.Clone();
        }

        Status = "Image loaded";
        RequestPreviewRender();
    }

    public bool TryRenderOutput(out RgbaImage? image, out string errorMessage, NodeId? targetNodeId = null)
    {
        lock (_sync)
        {
            try
            {
                image = EvaluateGraph(CancellationToken.None, targetNodeId);
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
        Status = "Preview requested";

        var nextCts = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _renderCts, nextCts);
        previous?.Cancel();
        previous?.Dispose();

        _ = RenderPreviewAsync(nextCts.Token, targetNodeId);
    }

    private void ExecuteCommand(IEditorCommand command)
    {
        lock (_sync)
        {
            command.Execute(_graph, _dagValidator);
            _undoStack.Push(command);
            _redoStack.Clear();
        }
    }

    private async Task RenderPreviewAsync(CancellationToken cancellationToken, NodeId? targetNodeId)
    {
        try
        {
            var output = await Task.Run(() =>
            {
                lock (_sync)
                {
                    return EvaluateGraph(cancellationToken, targetNodeId);
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
                }

                Status = "Preview ready";
                PreviewUpdated?.Invoke(this, new PreviewFrame(output.Width, output.Height, output.ToRgba8()));
                return;
            }

            Status = targetNodeId is null
                ? "Preview empty"
                : $"Preview empty at node {targetNodeId}";
            PreviewUpdated?.Invoke(this, EmptyPreviewFrame);
        }
        catch (OperationCanceledException)
        {
            // Intentionally ignored. A newer render request replaced this one.
        }
        catch (Exception exception)
        {
            Status = $"Render failed: {exception.Message}";
        }
    }

    private RgbaImage? EvaluateGraph(CancellationToken cancellationToken, NodeId? targetNodeId = null)
    {
        var cache = new Dictionary<NodeId, RgbaImage>();
        var evaluationNodeId = targetNodeId ?? OutputNodeId;
        return EvaluateNode(evaluationNodeId, cache, cancellationToken);
    }

    private RgbaImage? EvaluateNode(
        NodeId nodeId,
        IDictionary<NodeId, RgbaImage> cache,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (cache.TryGetValue(nodeId, out var cached))
        {
            return cached;
        }

        var node = _graph.GetNode(nodeId);
        RgbaImage? output = node.Type switch
        {
            NodeTypes.ImageInput => _inputImage?.Clone(),
            NodeTypes.Transform => EvaluateTransform(node, cache, cancellationToken),
            NodeTypes.Crop => EvaluateCrop(node, cache, cancellationToken),
            NodeTypes.ExposureContrast => EvaluateExposureContrast(node, cache, cancellationToken),
            NodeTypes.Curves => EvaluateCurves(node, cache, cancellationToken),
            NodeTypes.Hsl => EvaluateHsl(node, cache, cancellationToken),
            NodeTypes.Blur => EvaluateBlur(node, cache, cancellationToken),
            NodeTypes.Sharpen => EvaluateSharpen(node, cache, cancellationToken),
            NodeTypes.Blend => EvaluateBlend(node, cache, cancellationToken),
            NodeTypes.Output => ResolveInput(node.Id, "Image", cache, cancellationToken),
            _ => throw new InvalidOperationException($"Unknown node type '{node.Type}'.")
        };

        if (output is not null)
        {
            cache[nodeId] = output;
        }

        return output;
    }

    private RgbaImage? EvaluateTransform(Node node, IDictionary<NodeId, RgbaImage> cache, CancellationToken token)
    {
        var input = ResolveInput(node.Id, "Image", cache, token);
        return input is null
            ? null
            : MvpNodeKernels.Transform(
                input,
                node.GetParameter("Scale").AsFloat(),
                node.GetParameter("RotateDegrees").AsFloat());
    }

    private RgbaImage? EvaluateCrop(Node node, IDictionary<NodeId, RgbaImage> cache, CancellationToken token)
    {
        var input = ResolveInput(node.Id, "Image", cache, token);
        return input is null
            ? null
            : MvpNodeKernels.Crop(
                input,
                node.GetParameter("X").AsInteger(),
                node.GetParameter("Y").AsInteger(),
                node.GetParameter("Width").AsInteger(),
                node.GetParameter("Height").AsInteger());
    }

    private RgbaImage? EvaluateExposureContrast(Node node, IDictionary<NodeId, RgbaImage> cache, CancellationToken token)
    {
        var input = ResolveInput(node.Id, "Image", cache, token);
        return input is null
            ? null
            : MvpNodeKernels.ExposureContrast(
                input,
                node.GetParameter("Exposure").AsFloat(),
                node.GetParameter("Contrast").AsFloat());
    }

    private RgbaImage? EvaluateCurves(Node node, IDictionary<NodeId, RgbaImage> cache, CancellationToken token)
    {
        var input = ResolveInput(node.Id, "Image", cache, token);
        return input is null
            ? null
            : MvpNodeKernels.Curves(input, node.GetParameter("Gamma").AsFloat());
    }

    private RgbaImage? EvaluateHsl(Node node, IDictionary<NodeId, RgbaImage> cache, CancellationToken token)
    {
        var input = ResolveInput(node.Id, "Image", cache, token);
        return input is null
            ? null
            : MvpNodeKernels.Hsl(
                input,
                node.GetParameter("HueShift").AsFloat(),
                node.GetParameter("Saturation").AsFloat(),
                node.GetParameter("Lightness").AsFloat());
    }

    private RgbaImage? EvaluateBlur(Node node, IDictionary<NodeId, RgbaImage> cache, CancellationToken token)
    {
        var input = ResolveInput(node.Id, "Image", cache, token);
        return input is null
            ? null
            : MvpNodeKernels.GaussianBlur(
                input,
                node.GetParameter("Radius").AsInteger());
    }

    private RgbaImage? EvaluateSharpen(Node node, IDictionary<NodeId, RgbaImage> cache, CancellationToken token)
    {
        var input = ResolveInput(node.Id, "Image", cache, token);
        return input is null
            ? null
            : MvpNodeKernels.Sharpen(
                input,
                node.GetParameter("Amount").AsFloat(),
                node.GetParameter("Radius").AsInteger());
    }

    private RgbaImage? EvaluateBlend(Node node, IDictionary<NodeId, RgbaImage> cache, CancellationToken token)
    {
        var baseImage = ResolveInput(node.Id, "Base", cache, token);
        var topImage = ResolveInput(node.Id, "Top", cache, token);
        if (baseImage is null || topImage is null)
        {
            return baseImage ?? topImage;
        }

        return MvpNodeKernels.Blend(
            baseImage,
            topImage,
            node.GetParameter("Mode").AsEnum(),
            node.GetParameter("Opacity").AsFloat());
    }

    private RgbaImage? ResolveInput(
        NodeId nodeId,
        string inputPort,
        IDictionary<NodeId, RgbaImage> cache,
        CancellationToken token)
    {
        var edge = _graph.FindIncomingEdge(nodeId, inputPort);
        return edge is null
            ? null
            : EvaluateNode(edge.FromNodeId, cache, token);
    }
}

internal interface IEditorCommand
{
    void Execute(NodeGraph graph, IDagValidator validator);

    void Undo(NodeGraph graph, IDagValidator validator);
}

internal sealed class AddNodeCommand(Node node) : IEditorCommand
{
    public void Execute(NodeGraph graph, IDagValidator validator)
    {
        graph.AddNode(node);
    }

    public void Undo(NodeGraph graph, IDagValidator validator)
    {
        graph.RemoveNode(node.Id);
    }
}

internal sealed class ConnectPortsCommand(Edge edge) : IEditorCommand
{
    private Edge? _replaced;
    private bool _initialized;

    public void Execute(NodeGraph graph, IDagValidator validator)
    {
        var existing = graph.FindIncomingEdge(edge.ToNodeId, edge.ToPort);
        if (!_initialized)
        {
            _replaced = existing;
            _initialized = true;
        }

        if (existing is not null)
        {
            graph.RemoveEdge(existing);
        }

        graph.AddEdge(edge, validator);
    }

    public void Undo(NodeGraph graph, IDagValidator validator)
    {
        graph.RemoveEdge(edge);
        if (_replaced is not null)
        {
            graph.AddEdge(_replaced, validator);
        }
    }
}

internal sealed class SetNodeParameterCommand(
    NodeId nodeId,
    string parameterName,
    ParameterValue newValue) : IEditorCommand
{
    private ParameterValue _previousValue;
    private bool _captured;

    public void Execute(NodeGraph graph, IDagValidator validator)
    {
        var node = graph.GetNode(nodeId);
        if (!_captured)
        {
            _previousValue = node.GetParameter(parameterName);
            _captured = true;
        }

        node.SetParameter(parameterName, newValue);
    }

    public void Undo(NodeGraph graph, IDagValidator validator)
    {
        var node = graph.GetNode(nodeId);
        node.SetParameter(parameterName, _previousValue);
    }
}
