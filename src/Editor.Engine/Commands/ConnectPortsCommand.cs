using Editor.Domain.Graph;

namespace Editor.Engine.Commands;

internal sealed class ConnectPortsCommand(Edge edge) : IEditorCommand
{
    private Edge? _replaced;
    private bool _initialized;

    public void Execute(NodeGraph graph, IDagValidator validator)
    {
        var existing = graph.FindIncomingEdge(edge.ToNodeId, edge.ToPort);
        if (!_initialized)
        {
            _replaced = existing;
            _initialized = true;
        }

        if (existing is not null)
        {
            graph.RemoveEdge(existing);
        }

        graph.AddEdge(edge, validator);
    }

    public void Undo(NodeGraph graph, IDagValidator validator)
    {
        graph.RemoveEdge(edge);
        if (_replaced is not null)
        {
            graph.AddEdge(_replaced, validator);
        }
    }
}
