using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Editor.Domain.Graph;

namespace App.Presentation.Controllers;

internal sealed class NumericSliderParameterEditorPrimitive : IParameterEditorPrimitive
{
    public string Id => ParameterEditorPrimitiveIds.Slider;

    public bool CanCreate(NodeParameterDefinition definition, ParameterValue currentValue)
    {
        return definition.Kind switch
        {
            ParameterValueKind.Float => definition.MinFloat.HasValue && definition.MaxFloat.HasValue,
            ParameterValueKind.Integer => definition.MinInt.HasValue && definition.MaxInt.HasValue,
            _ => false
        };
    }

    public Control Create(ParameterEditorPrimitiveContext context)
    {
        var minimum = context.Definition.Kind == ParameterValueKind.Integer
            ? context.Definition.MinInt!.Value
            : context.Definition.MinFloat!.Value;
        var maximum = context.Definition.Kind == ParameterValueKind.Integer
            ? context.Definition.MaxInt!.Value
            : context.Definition.MaxFloat!.Value;

        var slider = new Slider
        {
            Minimum = minimum,
            Maximum = maximum,
            Value = context.Definition.Kind == ParameterValueKind.Integer
                ? context.CurrentValue.AsInteger()
                : context.CurrentValue.AsFloat()
        };

        if (context.Definition.Kind == ParameterValueKind.Integer)
        {
            slider.TickFrequency = 1;
            slider.IsSnapToTickEnabled = true;
        }

        var valueText = new TextBlock
        {
            Classes = { "hint-text" }
        };

        void UpdateValueText()
        {
            var value = context.Definition.Kind == ParameterValueKind.Integer
                ? ParameterValue.Integer((int)Math.Round(slider.Value))
                : ParameterValue.Float((float)slider.Value);

            valueText.Text = NodeParameterEditorController.FormatParameterValue(value);
        }

        slider.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.Property != RangeBase.ValueProperty)
            {
                return;
            }

            var next = context.Definition.Kind == ParameterValueKind.Integer
                ? ParameterValue.Integer((int)Math.Round(slider.Value))
                : ParameterValue.Float((float)slider.Value);
            context.ApplyValue(next);
            UpdateValueText();
        };

        UpdateValueText();

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
                slider,
                valueText
            }
        };
    }
}
