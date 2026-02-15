using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine.Abstractions;
using Editor.Imaging;

namespace Editor.Nodes.Modules;

internal sealed class CropNodeModule : NodeModuleBase
{
    public CropNodeModule()
        : base(NodeTypes.Crop)
    {
    }

    public override RgbaImage? Evaluate(Node node, INodeEvaluationContext context, CancellationToken cancellationToken)
    {
        var input = ResolveInput(node, NodePortNames.Image, context, cancellationToken);
        return input is null
            ? null
            : MvpNodeKernels.Crop(
                input,
                node.GetParameter("X").AsInteger(),
                node.GetParameter("Y").AsInteger(),
                node.GetParameter("Width").AsInteger(),
                node.GetParameter("Height").AsInteger());
    }
}

