using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine.Abstractions;
using Editor.Imaging;

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

    protected static RgbaImage ApplyMaskIfPresent(
        Node node,
        RgbaImage unprocessedInput,
        RgbaImage processedOutput,
        INodeEvaluationContext context,
        CancellationToken cancellationToken)
    {
        var maskInput = ResolveInput(node, NodePortNames.Mask, context, cancellationToken);
        return maskInput is null
            ? processedOutput
            : MvpNodeKernels.ApplyMask(unprocessedInput, processedOutput, maskInput);
    }
}
