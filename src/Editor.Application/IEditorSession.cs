using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine;
using Editor.Engine.Abstractions;

namespace Editor.Application;

public interface IEditorSession
{
    event EventHandler<PreviewFrame>? PreviewUpdated;

    EditorSnapshot GetSnapshot();

    NodeTypeDefinition GetNodeTypeDefinition(string nodeType);

    NodeId AddNode(NodeTypeId nodeTypeId);

    void Connect(NodeId fromNodeId, string fromPort, NodeId toNodeId, string toPort);

    void SetParameter(NodeId nodeId, string parameterName, ParameterValue value);

    void Undo();

    void Redo();

    void SetInputImage(NodeId nodeId, RgbaImage image);

    bool TryRenderOutput(out RgbaImage? image, out string errorMessage, NodeId? targetNodeId = null);

    void RequestPreviewRender(NodeId? targetNodeId = null);
}
