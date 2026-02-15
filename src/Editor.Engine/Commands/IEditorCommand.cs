using Editor.Domain.Graph;

namespace Editor.Engine.Commands;

internal interface IEditorCommand
{
    void Execute(NodeGraph graph, IDagValidator validator);

    void Undo(NodeGraph graph, IDagValidator validator);
}
