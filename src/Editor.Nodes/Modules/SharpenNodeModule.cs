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
        var input = ResolveInput(node, "Image", context, cancellationToken);
        return input is null
            ? null
            : MvpNodeKernels.Sharpen(
                input,
                node.GetParameter("Amount").AsFloat(),
                node.GetParameter("Radius").AsInteger());
    }
}

