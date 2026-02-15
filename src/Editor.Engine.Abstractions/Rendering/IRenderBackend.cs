using Editor.Domain.Graph;
using Editor.Domain.Imaging;

namespace Editor.Engine.Abstractions.Rendering;

public interface IRenderBackend
{
    RgbaImage? Evaluate(
        NodeGraph graph,
        GraphExecutionPlan plan,
        NodeId targetNodeId,
        IReadOnlyDictionary<NodeId, RgbaImage> inputImages,
        ITileCache tileCache,
        CancellationToken cancellationToken);
}
