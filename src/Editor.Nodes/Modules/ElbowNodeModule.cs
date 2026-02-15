using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine.Abstractions;

namespace Editor.Nodes.Modules;

internal sealed class ElbowNodeModule : NodeModuleBase
{
    public ElbowNodeModule()
        : base(NodeTypes.Elbow)
    {
    }

    public override RgbaImage? Evaluate(Node node, INodeEvaluationContext context, CancellationToken cancellationToken)
    {
        return ResolveInput(node, NodePortNames.Image, context, cancellationToken);
    }
}
