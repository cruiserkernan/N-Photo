using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine.Abstractions;
using Editor.Engine.Abstractions.Rendering;

namespace Editor.Engine.Rendering;

public sealed class SkiaRenderBackend(INodeModuleRegistry moduleRegistry) : IRenderBackend
{
    public RgbaImage? Evaluate(
        NodeGraph graph,
        GraphExecutionPlan plan,
        NodeId targetNodeId,
        IReadOnlyDictionary<NodeId, RgbaImage> inputImages,
        ITileCache tileCache,
        CancellationToken cancellationToken)
    {
        var outputs = new Dictionary<NodeId, RgbaImage>();
        var context = new NodeEvaluationContext(graph, inputImages, outputs);

        foreach (var nodeId in plan.OrderedNodeIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var node = graph.GetNode(nodeId);
            if (!moduleRegistry.TryGet(node.Type, out var module))
            {
                throw new InvalidOperationException($"Unknown node type '{node.Type}'.");
            }

            var tileKey = new TileKey(
                nodeId,
                plan.Fingerprints[nodeId],
                Level: 0,
                TileX: 0,
                TileY: 0,
                Quality: "preview");

            if (tileCache.TryGet(tileKey, out var cached))
            {
                outputs[nodeId] = cached;
                continue;
            }

            var evaluated = module.Evaluate(node, context, cancellationToken);
            if (evaluated is null)
            {
                continue;
            }

            outputs[nodeId] = evaluated;
            tileCache.Set(tileKey, evaluated);
        }

        return outputs.TryGetValue(targetNodeId, out var output)
            ? output.Clone()
            : null;
    }
}
