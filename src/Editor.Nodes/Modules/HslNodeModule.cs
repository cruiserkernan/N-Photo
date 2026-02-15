using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine.Abstractions;
using Editor.Imaging;

namespace Editor.Nodes.Modules;

internal sealed class HslNodeModule : NodeModuleBase
{
    public HslNodeModule()
        : base(NodeTypes.Hsl)
    {
    }

    public override RgbaImage? Evaluate(Node node, INodeEvaluationContext context, CancellationToken cancellationToken)
    {
        var input = ResolveInput(node, "Image", context, cancellationToken);
        return input is null
            ? null
            : MvpNodeKernels.Hsl(
                input,
                node.GetParameter("HueShift").AsFloat(),
                node.GetParameter("Saturation").AsFloat(),
                node.GetParameter("Lightness").AsFloat());
    }
}

