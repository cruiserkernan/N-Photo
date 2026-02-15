using Editor.Domain.Graph;

namespace Editor.Engine.Commands;

internal sealed class AddNodeCommand(Node node) : IEditorCommand
{
    public void Execute(NodeGraph graph, IDagValidator validator)
    {
        graph.AddNode(node);
    }

    public void Undo(NodeGraph graph, IDagValidator validator)
    {
        graph.RemoveNode(node.Id);
    }
}
