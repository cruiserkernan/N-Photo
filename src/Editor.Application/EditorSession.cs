using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine;
using Editor.Engine.Abstractions;

namespace Editor.Application;

public sealed class EditorSession : IEditorSession, IDisposable
{
    private readonly IEditorEngine _engine;
    private readonly INodeModuleRegistry _nodeModuleRegistry;

    public EditorSession(IEditorEngine engine, INodeModuleRegistry nodeModuleRegistry)
    {
        _engine = engine;
        _nodeModuleRegistry = nodeModuleRegistry;
        _engine.PreviewUpdated += OnPreviewUpdated;
    }

    public event EventHandler<PreviewFrame>? PreviewUpdated;

    public EditorSnapshot GetSnapshot()
    {
        return new EditorSnapshot(
            _engine.Nodes,
            _engine.Edges,
            _nodeModuleRegistry.NodeTypes
                .Where(nodeType => !string.Equals(nodeType.TypeId.Value, NodeTypes.Output, StringComparison.Ordinal))
                .Select(nodeType => nodeType.TypeId.Value)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray(),
            _engine.Status,
            _engine.CanUndo,
            _engine.CanRedo,
            _engine.InputNodeId,
            _engine.OutputNodeId);
    }

    public NodeTypeDefinition GetNodeTypeDefinition(string nodeType)
    {
        return _nodeModuleRegistry.TryGet(nodeType, out var module)
            ? module.Definition
            : throw new InvalidOperationException($"Node type '{nodeType}' is not registered.");
    }

    public NodeId AddNode(NodeTypeId nodeTypeId)
    {
        return _engine.AddNode(nodeTypeId.Value);
    }

    public void Connect(NodeId fromNodeId, string fromPort, NodeId toNodeId, string toPort)
    {
        _engine.Connect(fromNodeId, fromPort, toNodeId, toPort);
    }

    public void Disconnect(NodeId fromNodeId, string fromPort, NodeId toNodeId, string toPort)
    {
        _engine.Disconnect(fromNodeId, fromPort, toNodeId, toPort);
    }

    public void SetParameter(NodeId nodeId, string parameterName, ParameterValue value)
    {
        _engine.SetParameter(nodeId, parameterName, value);
    }

    public void Undo()
    {
        _engine.Undo();
    }

    public void Redo()
    {
        _engine.Redo();
    }

    public void SetInputImage(NodeId nodeId, RgbaImage image)
    {
        _engine.SetInputImage(nodeId, image);
    }

    public GraphDocumentState CaptureGraphDocument()
    {
        return _engine.CaptureGraphDocument();
    }

    public void LoadGraphDocument(GraphDocumentState document)
    {
        _engine.LoadGraphDocument(document);
    }

    public bool TryRenderOutput(out RgbaImage? image, out string errorMessage, NodeId? targetNodeId = null)
    {
        return _engine.TryRenderOutput(out image, out errorMessage, targetNodeId);
    }

    public void RequestPreviewRender(NodeId? targetNodeId = null)
    {
        _engine.RequestPreviewRender(targetNodeId);
    }

    public void Dispose()
    {
        _engine.PreviewUpdated -= OnPreviewUpdated;
        if (_engine is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void OnPreviewUpdated(object? sender, PreviewFrame frame)
    {
        PreviewUpdated?.Invoke(this, frame);
    }
}
