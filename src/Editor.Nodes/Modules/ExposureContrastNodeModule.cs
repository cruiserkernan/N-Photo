using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine.Abstractions;
using Editor.Imaging;

namespace Editor.Nodes.Modules;

internal sealed class ExposureContrastNodeModule : NodeModuleBase
{
    public ExposureContrastNodeModule()
        : base(NodeTypes.ExposureContrast)
    {
    }

    public override RgbaImage? Evaluate(Node node, INodeEvaluationContext context, CancellationToken cancellationToken)
    {
        var input = ResolveInput(node, NodePortNames.Image, context, cancellationToken);
        if (input is null)
        {
            return null;
        }

        var processed = MvpNodeKernels.ExposureContrast(
            input,
            node.GetParameter("Exposure").AsFloat(),
            node.GetParameter("Contrast").AsFloat());
        return ApplyMaskIfPresent(node, input, processed, context, cancellationToken);
    }
}

