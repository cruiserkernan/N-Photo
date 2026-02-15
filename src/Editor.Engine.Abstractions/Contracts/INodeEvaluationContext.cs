using Editor.Domain.Graph;
using Editor.Domain.Imaging;

namespace Editor.Engine.Abstractions;

public interface INodeEvaluationContext
{
    RgbaImage? ResolveInput(NodeId nodeId, string inputPort, CancellationToken cancellationToken);

    bool TryGetInputImage(NodeId nodeId, out RgbaImage image);
}
