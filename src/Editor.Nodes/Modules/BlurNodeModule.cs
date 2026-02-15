using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine.Abstractions;
using Editor.Imaging;

namespace Editor.Nodes.Modules;

internal sealed class BlurNodeModule : NodeModuleBase
{
    public BlurNodeModule()
        : base(NodeTypes.Blur)
    {
    }

    public override RgbaImage? Evaluate(Node node, INodeEvaluationContext context, CancellationToken cancellationToken)
    {
        var input = ResolveInput(node, NodePortNames.Image, context, cancellationToken);
        if (input is null)
        {
            return null;
        }

        var processed = MvpNodeKernels.GaussianBlur(
            input,
            node.GetParameter("Radius").AsInteger());
        return ApplyMaskIfPresent(node, input, processed, context, cancellationToken);
    }
}

