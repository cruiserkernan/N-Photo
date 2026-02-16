using Avalonia.Controls;
using Editor.Domain.Graph;

namespace App.Presentation.Controllers;

internal sealed class BooleanParameterEditorPrimitive : IParameterEditorPrimitive
{
    public string Id => ParameterEditorPrimitiveIds.Toggle;

    public bool CanCreate(NodeParameterDefinition definition, ParameterValue currentValue)
    {
        return definition.Kind == ParameterValueKind.Boolean;
    }

    public Control Create(ParameterEditorPrimitiveContext context)
    {
        var checkBox = new CheckBox
        {
            Content = NodeParameterEditorController.GetParameterLabel(context.Definition),
            IsChecked = context.CurrentValue.AsBoolean()
        };

        checkBox.IsCheckedChanged += (_, _) =>
            context.ApplyValue(ParameterValue.Boolean(checkBox.IsChecked ?? false));

        return checkBox;
    }
}
