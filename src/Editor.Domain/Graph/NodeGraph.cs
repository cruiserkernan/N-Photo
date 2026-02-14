namespace Editor.Domain.Graph;

public sealed class NodeGraph
{
    private readonly Dictionary<NodeId, Node> _nodes = new();
    private readonly List<Edge> _edges = new();

    public IReadOnlyCollection<Node> Nodes => _nodes.Values;

    public IReadOnlyList<Edge> Edges => _edges;

    public bool ContainsNode(NodeId nodeId) => _nodes.ContainsKey(nodeId);

    public bool TryGetNode(NodeId nodeId, out Node node)
    {
        if (_nodes.TryGetValue(nodeId, out var existing))
        {
            node = existing;
            return true;
        }

        node = null!;
        return false;
    }

    public Node GetNode(NodeId nodeId)
    {
        return TryGetNode(nodeId, out var node)
            ? node
            : throw new InvalidOperationException($"Node '{nodeId}' does not exist.");
    }

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
        EnsurePortsExist(edge);
        EnsureEdgeUnique(edge);
        _edges.Add(edge);
    }

    public void AddEdge(Edge edge, IDagValidator validator)
    {
        EnsureNodesExist(edge);
        EnsurePortsExist(edge);
        EnsureEdgeUnique(edge);

        if (!validator.CanConnect(this, edge))
        {
            throw new InvalidOperationException("Edge would introduce a cycle.");
        }

        _edges.Add(edge);
    }

    public void RemoveNode(NodeId nodeId)
    {
        if (!_nodes.Remove(nodeId))
        {
            throw new InvalidOperationException($"Node '{nodeId}' does not exist.");
        }

        _edges.RemoveAll(edge => edge.FromNodeId == nodeId || edge.ToNodeId == nodeId);
    }

    public void RemoveEdge(Edge edge)
    {
        if (!_edges.Remove(edge))
        {
            throw new InvalidOperationException("Edge does not exist.");
        }
    }

    public IReadOnlyList<Edge> GetIncomingEdges(NodeId nodeId)
    {
        return _edges.Where(edge => edge.ToNodeId == nodeId).ToArray();
    }

    public IReadOnlyList<Edge> GetOutgoingEdges(NodeId nodeId)
    {
        return _edges.Where(edge => edge.FromNodeId == nodeId).ToArray();
    }

    public Edge? FindIncomingEdge(NodeId nodeId, string toPort)
    {
        return _edges.FirstOrDefault(edge =>
            edge.ToNodeId == nodeId &&
            string.Equals(edge.ToPort, toPort, StringComparison.Ordinal));
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

    private void EnsurePortsExist(Edge edge)
    {
        var fromNode = GetNode(edge.FromNodeId);
        var toNode = GetNode(edge.ToNodeId);
        var fromType = NodeTypeCatalog.GetByName(fromNode.Type);
        var toType = NodeTypeCatalog.GetByName(toNode.Type);

        if (!fromType.HasOutputPort(edge.FromPort))
        {
            throw new InvalidOperationException(
                $"Port '{edge.FromPort}' does not exist as output on node type '{fromNode.Type}'.");
        }

        if (!toType.HasInputPort(edge.ToPort))
        {
            throw new InvalidOperationException(
                $"Port '{edge.ToPort}' does not exist as input on node type '{toNode.Type}'.");
        }
    }

    private void EnsureEdgeUnique(Edge edge)
    {
        if (_edges.Contains(edge))
        {
            throw new InvalidOperationException("Edge already exists.");
        }
    }
}
