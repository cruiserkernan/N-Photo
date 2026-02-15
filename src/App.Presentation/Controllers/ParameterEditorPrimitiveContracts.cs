using Editor.Domain.Graph;

namespace App.Presentation.Controllers;

public sealed class ParameterEditorPrimitiveContext
{
    public ParameterEditorPrimitiveContext(
        NodeId nodeId,
        string nodeType,
        NodeParameterDefinition definition,
        ParameterValue currentValue,
        Action<ParameterValue> applyValue,
        Action<string> setStatus)
    {
        NodeId = nodeId;
        NodeType = nodeType;
        Definition = definition;
        CurrentValue = currentValue;
        ApplyValue = applyValue;
        SetStatus = setStatus;
    }

    public NodeId NodeId { get; }

    public string NodeType { get; }

    public NodeParameterDefinition Definition { get; }

    public ParameterValue CurrentValue { get; }

    public Action<ParameterValue> ApplyValue { get; }

    public Action<string> SetStatus { get; }
}

public interface IParameterEditorPrimitive
{
    string Id { get; }

    bool CanCreate(NodeParameterDefinition definition, ParameterValue currentValue);

    Avalonia.Controls.Control Create(ParameterEditorPrimitiveContext context);
}
