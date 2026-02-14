using Editor.Domain.Graph;
using Editor.Domain.Imaging;

namespace Editor.Engine;

public interface IEditorEngine
{
    string Status { get; }

    event EventHandler<PreviewFrame>? PreviewUpdated;

    IReadOnlyList<Node> Nodes { get; }

    IReadOnlyList<Edge> Edges { get; }

    IReadOnlyList<string> AvailableNodeTypes { get; }

    bool CanUndo { get; }

    bool CanRedo { get; }

    NodeId InputNodeId { get; }

    NodeId OutputNodeId { get; }

    NodeId AddNode(string nodeType);

    void Connect(NodeId fromNodeId, string fromPort, NodeId toNodeId, string toPort);

    void SetParameter(NodeId nodeId, string parameterName, ParameterValue value);

    void Undo();

    void Redo();

    void SetInputImage(RgbaImage image);

    bool TryRenderOutput(out RgbaImage? image, out string errorMessage);

    void RequestPreviewRender();
}
