namespace Editor.Domain.Graph;

public sealed class NodeGraph
{
    private readonly Dictionary<NodeId, Node> _nodes = new();
    private readonly List<Edge> _edges = new();

    public IReadOnlyCollection<Node> Nodes => _nodes.Values;

    public IReadOnlyList<Edge> Edges => _edges;

    public bool ContainsNode(NodeId nodeId) => _nodes.ContainsKey(nodeId);

    public void AddNode(Node node)
    {
        if (!_nodes.TryAdd(node.Id, node))
        {
            throw new InvalidOperationException($"Node '{node.Id}' already exists.");
        }
    }

    public void AddEdge(Edge edge)
    {
        EnsureNodesExist(edge);
        _edges.Add(edge);
    }

    public void AddEdge(Edge edge, IDagValidator validator)
    {
        EnsureNodesExist(edge);

        if (!validator.CanConnect(this, edge))
        {
            throw new InvalidOperationException("Edge would introduce a cycle.");
        }

        _edges.Add(edge);
    }

    private void EnsureNodesExist(Edge edge)
    {
        if (!ContainsNode(edge.FromNodeId))
        {
            throw new InvalidOperationException($"Source node '{edge.FromNodeId}' does not exist.");
        }

        if (!ContainsNode(edge.ToNodeId))
        {
            throw new InvalidOperationException($"Target node '{edge.ToNodeId}' does not exist.");
        }
    }
}
