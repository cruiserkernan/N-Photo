using Editor.Domain.Graph;

namespace Editor.Engine.Commands;

internal sealed class SetNodeParameterCommand(
    NodeId nodeId,
    string parameterName,
    ParameterValue newValue) : IEditorCommand
{
    private ParameterValue _previousValue;
    private bool _captured;

    public void Execute(NodeGraph graph, IDagValidator validator)
    {
        var node = graph.GetNode(nodeId);
        if (!_captured)
        {
            _previousValue = node.GetParameter(parameterName);
            _captured = true;
        }

        node.SetParameter(parameterName, newValue);
    }

    public void Undo(NodeGraph graph, IDagValidator validator)
    {
        var node = graph.GetNode(nodeId);
        node.SetParameter(parameterName, _previousValue);
    }
}
