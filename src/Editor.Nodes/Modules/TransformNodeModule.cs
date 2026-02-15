using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine.Abstractions;
using Editor.Imaging;

namespace Editor.Nodes.Modules;

internal sealed class TransformNodeModule : NodeModuleBase
{
    public TransformNodeModule()
        : base(NodeTypes.Transform)
    {
    }

    public override RgbaImage? Evaluate(Node node, INodeEvaluationContext context, CancellationToken cancellationToken)
    {
        var input = ResolveInput(node, NodePortNames.Image, context, cancellationToken);
        if (input is null)
        {
            return null;
        }

        var transformed = MvpNodeKernels.Transform(
            input,
            node.GetParameter("Scale").AsFloat(),
            node.GetParameter("RotateDegrees").AsFloat());
        return ApplyMaskIfPresent(node, input, transformed, context, cancellationToken);
    }
}

