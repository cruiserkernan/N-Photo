using Editor.Domain.Graph;

namespace Editor.Engine.Commands;

internal sealed class DisconnectPortsCommand(Edge edge) : IEditorCommand
{
    public void Execute(NodeGraph graph, IDagValidator validator)
    {
        graph.RemoveEdge(edge);
    }

    public void Undo(NodeGraph graph, IDagValidator validator)
    {
        graph.AddEdge(edge, validator);
    }
}
