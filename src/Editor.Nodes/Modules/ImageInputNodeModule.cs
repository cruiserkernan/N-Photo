using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine.Abstractions;

namespace Editor.Nodes.Modules;

internal sealed class ImageInputNodeModule : NodeModuleBase
{
    public ImageInputNodeModule()
        : base(NodeTypes.ImageInput)
    {
    }

    public override RgbaImage? Evaluate(Node node, INodeEvaluationContext context, CancellationToken cancellationToken)
    {
        return context.TryGetInputImage(node.Id, out var image)
            ? image.Clone()
            : null;
    }
}

