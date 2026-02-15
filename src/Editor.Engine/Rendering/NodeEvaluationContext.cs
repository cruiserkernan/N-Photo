using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine.Abstractions;

namespace Editor.Engine.Rendering;

internal sealed class NodeEvaluationContext(
    NodeGraph graph,
    IReadOnlyDictionary<NodeId, RgbaImage> inputImages,
    IReadOnlyDictionary<NodeId, RgbaImage> outputs) : INodeEvaluationContext
{
    public RgbaImage? ResolveInput(NodeId nodeId, string inputPort, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var edge = graph.FindIncomingEdge(nodeId, inputPort);
        if (edge is null)
        {
            return null;
        }

        return outputs.TryGetValue(edge.FromNodeId, out var output)
            ? output.Clone()
            : null;
    }

    public bool TryGetInputImage(NodeId nodeId, out RgbaImage image)
    {
        if (inputImages.TryGetValue(nodeId, out var stored))
        {
            image = stored.Clone();
            return true;
        }

        image = null!;
        return false;
    }
}
