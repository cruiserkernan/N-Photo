using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine.Abstractions;

namespace Editor.Nodes.Modules;

internal abstract class NodeModuleBase(string typeName) : INodeModule
{
    public NodeTypeId TypeId { get; } = new(typeName);

    public NodeTypeDefinition Definition => NodeTypeCatalog.GetByName(TypeId.Value);

    public abstract RgbaImage? Evaluate(Node node, INodeEvaluationContext context, CancellationToken cancellationToken);

    protected static RgbaImage? ResolveInput(
        Node node,
        string inputPort,
        INodeEvaluationContext context,
        CancellationToken cancellationToken)
    {
        return context.ResolveInput(node.Id, inputPort, cancellationToken);
    }
}
