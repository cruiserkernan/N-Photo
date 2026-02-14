namespace Editor.Domain.Graph;

public sealed class DagValidator : IDagValidator
{
    public GraphValidationResult Validate(NodeGraph graph)
    {
        return TryFindCycle(graph, null, out var cyclePath)
            ? GraphValidationResult.Cyclic(cyclePath)
            : GraphValidationResult.Success;
    }

    public bool CanConnect(NodeGraph graph, Edge edge)
    {
        return !TryFindCycle(graph, edge, out _);
    }

    private static bool TryFindCycle(
        NodeGraph graph,
        Edge? candidateEdge,
        out IReadOnlyList<NodeId> cyclePath)
    {
        var adjacency = BuildAdjacency(graph, candidateEdge);
        var states = adjacency.Keys.ToDictionary(nodeId => nodeId, _ => VisitState.Unvisited);
        var traversalPath = new List<NodeId>();
        var pathIndex = new Dictionary<NodeId, int>();

        foreach (var nodeId in adjacency.Keys)
        {
            if (states[nodeId] != VisitState.Unvisited)
            {
                continue;
            }

            if (DepthFirst(nodeId, adjacency, states, traversalPath, pathIndex, out cyclePath))
            {
                return true;
            }
        }

        cyclePath = Array.Empty<NodeId>();
        return false;
    }

    private static Dictionary<NodeId, List<NodeId>> BuildAdjacency(NodeGraph graph, Edge? candidateEdge)
    {
        var adjacency = graph.Nodes.ToDictionary(node => node.Id, _ => new List<NodeId>());

        foreach (var edge in graph.Edges)
        {
            adjacency[edge.FromNodeId].Add(edge.ToNodeId);
        }

        if (candidateEdge is not null)
        {
            if (!adjacency.ContainsKey(candidateEdge.FromNodeId) ||
                !adjacency.ContainsKey(candidateEdge.ToNodeId))
            {
                return adjacency;
            }

            adjacency[candidateEdge.FromNodeId].Add(candidateEdge.ToNodeId);
        }

        return adjacency;
    }

    private static bool DepthFirst(
        NodeId nodeId,
        IReadOnlyDictionary<NodeId, List<NodeId>> adjacency,
        IDictionary<NodeId, VisitState> states,
        IList<NodeId> traversalPath,
        IDictionary<NodeId, int> pathIndex,
        out IReadOnlyList<NodeId> cyclePath)
    {
        states[nodeId] = VisitState.Visiting;
        pathIndex[nodeId] = traversalPath.Count;
        traversalPath.Add(nodeId);

        foreach (var neighbor in adjacency[nodeId])
        {
            if (states[neighbor] == VisitState.Visiting)
            {
                var startIndex = pathIndex[neighbor];
                cyclePath = traversalPath
                    .Skip(startIndex)
                    .Append(neighbor)
                    .ToArray();
                return true;
            }

            if (states[neighbor] == VisitState.Unvisited &&
                DepthFirst(neighbor, adjacency, states, traversalPath, pathIndex, out cyclePath))
            {
                return true;
            }
        }

        traversalPath.RemoveAt(traversalPath.Count - 1);
        pathIndex.Remove(nodeId);
        states[nodeId] = VisitState.Visited;
        cyclePath = Array.Empty<NodeId>();
        return false;
    }

    private enum VisitState
    {
        Unvisited,
        Visiting,
        Visited
    }
}
