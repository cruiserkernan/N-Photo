using Avalonia.Controls;
using Editor.Domain.Graph;

namespace App.Presentation.Controllers;

internal sealed class EnumParameterEditorPrimitive : IParameterEditorPrimitive
{
    public string Id => ParameterEditorPrimitiveIds.EnumSelect;

    public bool CanCreate(NodeParameterDefinition definition, ParameterValue currentValue)
    {
        return definition.Kind == ParameterValueKind.Enum;
    }

    public Control Create(ParameterEditorPrimitiveContext context)
    {
        var comboBox = new ComboBox
        {
            ItemsSource = context.Definition.EnumValues ?? Array.Empty<string>(),
            SelectedItem = context.CurrentValue.AsEnum()
        };

        comboBox.SelectionChanged += (_, _) =>
        {
            if (comboBox.SelectedItem is not string selected)
            {
                return;
            }

            context.ApplyValue(ParameterValue.Enum(selected));
        };

        return new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock
                {
                    Classes = { "section-label" },
                    Text = NodeParameterEditorController.GetParameterLabel(context.Definition)
                },
                comboBox
            }
        };
    }
}
