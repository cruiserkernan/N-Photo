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
        var input = ResolveInput(node, "Image", context, cancellationToken);
        return input is null
            ? null
            : MvpNodeKernels.GaussianBlur(
                input,
                node.GetParameter("Radius").AsInteger());
    }
}

