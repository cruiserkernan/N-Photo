using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Editor.Domain.Graph;

namespace App.Presentation.Controllers;

internal sealed class NumericInputParameterEditorPrimitive : IParameterEditorPrimitive
{
    public string Id => ParameterEditorPrimitiveIds.NumberInput;

    public bool CanCreate(NodeParameterDefinition definition, ParameterValue currentValue)
    {
        return definition.Kind is ParameterValueKind.Float or ParameterValueKind.Integer;
    }

    public Control Create(ParameterEditorPrimitiveContext context)
    {
        var textBox = new TextBox
        {
            Text = NodeParameterEditorController.FormatParameterValue(context.CurrentValue),
            Watermark = NodeParameterEditorController.GetParameterHint(context.Definition)
        };

        var applyButton = new Button
        {
            Content = "Apply"
        };

        applyButton.Click += (_, _) =>
        {
            try
            {
                var parsedValue = NodeParameterEditorController.ParseTextParameterValue(context.Definition, textBox.Text ?? string.Empty);
                context.ApplyValue(parsedValue);
            }
            catch (Exception exception)
            {
                context.SetStatus($"Set parameter failed: {exception.Message}");
            }
        };

        textBox.KeyDown += (_, keyEventArgs) =>
        {
            if (keyEventArgs.Key != Key.Enter)
            {
                return;
            }

            applyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            keyEventArgs.Handled = true;
        };

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 8
        };
        row.Children.Add(textBox);
        row.Children.Add(applyButton);
        Grid.SetColumn(applyButton, 1);

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
                row
            }
        };
    }
}
