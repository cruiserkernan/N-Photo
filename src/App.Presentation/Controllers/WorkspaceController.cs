using Editor.Application;
using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine.Abstractions;

namespace App.Presentation.Controllers;

public sealed class WorkspaceController
{
    private readonly IEditorSession _session;

    public WorkspaceController(IEditorSession session)
    {
        _session = session;
    }

    public EditorSnapshot Snapshot => _session.GetSnapshot();

    public NodeId AddNode(NodeTypeId nodeTypeId)
    {
        return _session.AddNode(nodeTypeId);
    }

    public void Connect(NodeId fromNodeId, string fromPort, NodeId toNodeId, string toPort)
    {
        _session.Connect(fromNodeId, fromPort, toNodeId, toPort);
    }

    public void Disconnect(NodeId fromNodeId, string fromPort, NodeId toNodeId, string toPort)
    {
        _session.Disconnect(fromNodeId, fromPort, toNodeId, toPort);
    }

    public void SetParameter(NodeId nodeId, string parameterName, ParameterValue value)
    {
        _session.SetParameter(nodeId, parameterName, value);
    }

    public void SetInputImage(NodeId nodeId, RgbaImage image)
    {
        _session.SetInputImage(nodeId, image);
    }

    public bool TryRenderOutput(out RgbaImage? image, out string errorMessage, NodeId? targetNodeId = null)
    {
        return _session.TryRenderOutput(out image, out errorMessage, targetNodeId);
    }

    public void Undo()
    {
        _session.Undo();
    }

    public void Redo()
    {
        _session.Redo();
    }

    public void RequestPreviewRender(NodeId? targetNodeId = null)
    {
        _session.RequestPreviewRender(targetNodeId);
    }
}
