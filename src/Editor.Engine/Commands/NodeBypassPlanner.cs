using Editor.Domain.Graph;

namespace Editor.Engine.Commands;

internal sealed class NodeBypassPlan
{
    public NodeBypassPlan(Node node, IReadOnlyList<Edge> removedEdges, IReadOnlyList<Edge> addedEdges)
    {
        Node = node;
        RemovedEdges = removedEdges;
        AddedEdges = addedEdges;
    }

    public Node Node { get; }

    public IReadOnlyList<Edge> RemovedEdges { get; }

    public IReadOnlyList<Edge> AddedEdges { get; }
}

internal static class NodeBypassPlanner
{
    public static NodeBypassPlan Build(NodeGraph graph, NodeId nodeId, bool reconnectPrimaryStream)
    {
        var node = graph.GetNode(nodeId);
        var incoming = graph.GetIncomingEdges(nodeId);
        var outgoing = graph.GetOutgoingEdges(nodeId);
        var removedEdges = incoming
            .Concat(outgoing)
            .Distinct()
            .ToArray();
        if (!reconnectPrimaryStream)
        {
            return new NodeBypassPlan(node, removedEdges, Array.Empty<Edge>());
        }

        var nodeType = NodeTypeCatalog.GetByName(node.Type);
        var primaryInputPort = nodeType.Inputs.FirstOrDefault()?.Name;
        var primaryOutputPort = nodeType.Outputs.FirstOrDefault()?.Name;
        if (string.IsNullOrWhiteSpace(primaryInputPort) || string.IsNullOrWhiteSpace(primaryOutputPort))
        {
            return new NodeBypassPlan(node, removedEdges, Array.Empty<Edge>());
        }

        var primaryIncoming = incoming.FirstOrDefault(edge =>
            string.Equals(edge.ToPort, primaryInputPort, StringComparison.Ordinal));
        if (primaryIncoming is null)
        {
            return new NodeBypassPlan(node, removedEdges, Array.Empty<Edge>());
        }

        var primaryOutgoing = outgoing
            .Where(edge => string.Equals(edge.FromPort, primaryOutputPort, StringComparison.Ordinal))
            .ToArray();
        if (primaryOutgoing.Length == 0)
        {
            return new NodeBypassPlan(node, removedEdges, Array.Empty<Edge>());
        }

        var planned = new HashSet<Edge>();
        foreach (var outgoingEdge in primaryOutgoing)
        {
            var candidate = new Edge(
                primaryIncoming.FromNodeId,
                primaryIncoming.FromPort,
                outgoingEdge.ToNodeId,
                outgoingEdge.ToPort);

            if (planned.Contains(candidate) || graph.Edges.Contains(candidate))
            {
                continue;
            }

            planned.Add(candidate);
        }

        return new NodeBypassPlan(node, removedEdges, planned.ToArray());
    }
}
