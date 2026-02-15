using Editor.Domain.Graph;
using Editor.Domain.Imaging;

namespace Editor.Engine.Abstractions;

public interface INodeModule
{
    NodeTypeId TypeId { get; }

    NodeTypeDefinition Definition { get; }

    RgbaImage? Evaluate(Node node, INodeEvaluationContext context, CancellationToken cancellationToken);
}
