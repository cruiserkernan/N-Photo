using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine.Abstractions;

namespace Editor.Nodes.Modules;

internal sealed class OutputNodeModule : NodeModuleBase
{
    public OutputNodeModule()
        : base(NodeTypes.Output)
    {
    }

    public override RgbaImage? Evaluate(Node node, INodeEvaluationContext context, CancellationToken cancellationToken)
    {
        return ResolveInput(node, "Image", context, cancellationToken);
    }
}

