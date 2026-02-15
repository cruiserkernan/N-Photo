using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine.Abstractions;
using Editor.Imaging;

namespace Editor.Nodes.Modules;

internal sealed class CurvesNodeModule : NodeModuleBase
{
    public CurvesNodeModule()
        : base(NodeTypes.Curves)
    {
    }

    public override RgbaImage? Evaluate(Node node, INodeEvaluationContext context, CancellationToken cancellationToken)
    {
        var input = ResolveInput(node, "Image", context, cancellationToken);
        return input is null
            ? null
            : MvpNodeKernels.Curves(input, node.GetParameter("Gamma").AsFloat());
    }
}

