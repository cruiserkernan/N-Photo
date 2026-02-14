using Editor.Domain.Graph;

namespace Editor.Domain.Tests;

public class DagValidatorTests
{
    [Fact]
    public void AddEdge_WithValidator_RejectsCycle()
    {
        var validator = new DagValidator();
        var graph = new NodeGraph();
        var nodeA = new Node(NodeId.New(), NodeTypes.Transform);
        var nodeB = new Node(NodeId.New(), NodeTypes.Transform);

        graph.AddNode(nodeA);
        graph.AddNode(nodeB);
        graph.AddEdge(new Edge(nodeA.Id, "Image", nodeB.Id, "Image"), validator);

        var cycleEdge = new Edge(nodeB.Id, "Image", nodeA.Id, "Image");

        var exception = Assert.Throws<InvalidOperationException>(() => graph.AddEdge(cycleEdge, validator));
        Assert.Contains("cycle", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
