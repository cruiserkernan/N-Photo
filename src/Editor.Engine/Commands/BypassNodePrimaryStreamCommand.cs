using Editor.Domain.Graph;

namespace Editor.Engine.Commands;

internal sealed class BypassNodePrimaryStreamCommand(NodeId nodeId) : IEditorCommand
{
    private bool _initialized;
    private IReadOnlyList<Edge> _removedEdges = Array.Empty<Edge>();
    private IReadOnlyList<Edge> _addedEdges = Array.Empty<Edge>();

    public void Execute(NodeGraph graph, IDagValidator validator)
    {
        if (!_initialized)
        {
            var plan = NodeBypassPlanner.Build(graph, nodeId, reconnectPrimaryStream: true);
            _removedEdges = plan.RemovedEdges;
            _addedEdges = plan.AddedEdges;
            _initialized = true;
        }

        foreach (var edge in _removedEdges)
        {
            if (graph.Edges.Contains(edge))
            {
                graph.RemoveEdge(edge);
            }
        }

        foreach (var edge in _addedEdges)
        {
            if (!graph.Edges.Contains(edge))
            {
                graph.AddEdge(edge, validator);
            }
        }
    }

    public void Undo(NodeGraph graph, IDagValidator validator)
    {
        foreach (var edge in _addedEdges)
        {
            if (graph.Edges.Contains(edge))
            {
                graph.RemoveEdge(edge);
            }
        }

        foreach (var edge in _removedEdges)
        {
            if (!graph.Edges.Contains(edge))
            {
                graph.AddEdge(edge, validator);
            }
        }
    }
}
