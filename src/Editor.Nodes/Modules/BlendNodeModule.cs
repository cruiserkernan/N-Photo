using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine.Abstractions;
using Editor.Imaging;

namespace Editor.Nodes.Modules;

internal sealed class BlendNodeModule : NodeModuleBase
{
    public BlendNodeModule()
        : base(NodeTypes.Blend)
    {
    }

    public override RgbaImage? Evaluate(Node node, INodeEvaluationContext context, CancellationToken cancellationToken)
    {
        var baseImage = ResolveInput(node, "Base", context, cancellationToken);
        var topImage = ResolveInput(node, "Top", context, cancellationToken);
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
}

