using Editor.Domain.Graph;

namespace Editor.Engine.Abstractions.Rendering;

public sealed class GraphExecutionPlan
{
    public GraphExecutionPlan(
        NodeId targetNodeId,
        IReadOnlyList<NodeId> orderedNodeIds,
        IReadOnlyDictionary<NodeId, NodeFingerprint> fingerprints)
    {
        TargetNodeId = targetNodeId;
        OrderedNodeIds = orderedNodeIds;
        Fingerprints = fingerprints;
    }

    public NodeId TargetNodeId { get; }

    public IReadOnlyList<NodeId> OrderedNodeIds { get; }

    public IReadOnlyDictionary<NodeId, NodeFingerprint> Fingerprints { get; }
}
