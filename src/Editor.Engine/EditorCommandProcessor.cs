using Editor.Domain.Graph;
using Editor.Engine.Commands;

namespace Editor.Engine;

internal sealed class EditorCommandProcessor
{
    private readonly NodeGraph _graph;
    private readonly IDagValidator _validator;
    private readonly Action _onGraphMutated;
    private readonly Stack<IEditorCommand> _undoStack = new();
    private readonly Stack<IEditorCommand> _redoStack = new();

    public EditorCommandProcessor(NodeGraph graph, IDagValidator validator, Action onGraphMutated)
    {
        _graph = graph;
        _validator = validator;
        _onGraphMutated = onGraphMutated;
    }

    public bool CanUndo => _undoStack.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    public void Execute(IEditorCommand command)
    {
        command.Execute(_graph, _validator);
        _undoStack.Push(command);
        _redoStack.Clear();
        _onGraphMutated();
    }

    public bool Undo()
    {
        if (_undoStack.Count == 0)
        {
            return false;
        }

        var command = _undoStack.Pop();
        command.Undo(_graph, _validator);
        _redoStack.Push(command);
        _onGraphMutated();
        return true;
    }

    public bool Redo()
    {
        if (_redoStack.Count == 0)
        {
            return false;
        }

        var command = _redoStack.Pop();
        command.Execute(_graph, _validator);
        _undoStack.Push(command);
        _onGraphMutated();
        return true;
    }

    public void Reset()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
