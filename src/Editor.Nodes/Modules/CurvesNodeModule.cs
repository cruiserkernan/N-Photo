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
        var input = ResolveInput(node, NodePortNames.Image, context, cancellationToken);
        if (input is null)
        {
            return null;
        }

        var processed = MvpNodeKernels.Curves(input, node.GetParameter("Gamma").AsFloat());
        return ApplyMaskIfPresent(node, input, processed, context, cancellationToken);
    }
}

