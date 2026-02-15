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
        var input = ResolveInput(node, NodePortNames.Image, context, cancellationToken);
        if (input is null)
        {
            return null;
        }

        var processed = MvpNodeKernels.Hsl(
            input,
            node.GetParameter("HueShift").AsFloat(),
            node.GetParameter("Saturation").AsFloat(),
            node.GetParameter("Lightness").AsFloat());
        return ApplyMaskIfPresent(node, input, processed, context, cancellationToken);
    }
}

