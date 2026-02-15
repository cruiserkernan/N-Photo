using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine.Abstractions;
using Editor.Imaging;

namespace Editor.Nodes.Modules;

internal sealed class SharpenNodeModule : NodeModuleBase
{
    public SharpenNodeModule()
        : base(NodeTypes.Sharpen)
    {
    }

    public override RgbaImage? Evaluate(Node node, INodeEvaluationContext context, CancellationToken cancellationToken)
    {
        var input = ResolveInput(node, NodePortNames.Image, context, cancellationToken);
        if (input is null)
        {
            return null;
        }

        var processed = MvpNodeKernels.Sharpen(
            input,
            node.GetParameter("Amount").AsFloat(),
            node.GetParameter("Radius").AsInteger());
        return ApplyMaskIfPresent(node, input, processed, context, cancellationToken);
    }
}

