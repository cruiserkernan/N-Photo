using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Editor.Domain.Graph;
using Editor.Domain.Imaging;

namespace App.Presentation.Controllers;

public sealed class ParameterEditorPrimitiveRegistry
{
    private readonly IReadOnlyList<IParameterEditorPrimitive> _primitives;
    private readonly Dictionary<string, IParameterEditorPrimitive> _byId;

    public ParameterEditorPrimitiveRegistry(IEnumerable<IParameterEditorPrimitive> primitives)
    {
        _primitives = primitives.ToArray();
        _byId = _primitives.ToDictionary(primitive => primitive.Id, StringComparer.OrdinalIgnoreCase);
    }

    public static ParameterEditorPrimitiveRegistry CreateDefault()
    {
        return new ParameterEditorPrimitiveRegistry(
            new IParameterEditorPrimitive[]
            {
                new ColorParameterEditorPrimitive(),
                new BooleanParameterEditorPrimitive(),
                new EnumParameterEditorPrimitive(),
                new NumericSliderParameterEditorPrimitive(),
                new NumericInputParameterEditorPrimitive(),
                new UnsupportedParameterEditorPrimitive()
            });
    }

    public IParameterEditorPrimitive Resolve(NodeParameterDefinition definition, ParameterValue currentValue)
    {
        if (!string.IsNullOrWhiteSpace(definition.EditorPrimitive) &&
            _byId.TryGetValue(definition.EditorPrimitive, out var explicitPrimitive) &&
            explicitPrimitive.CanCreate(definition, currentValue))
        {
            return explicitPrimitive;
        }

        foreach (var primitive in _primitives)
        {
            if (primitive.CanCreate(definition, currentValue))
            {
                return primitive;
            }
        }

        return new UnsupportedParameterEditorPrimitive();
    }
}

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

internal sealed class ColorParameterEditorPrimitive : IParameterEditorPrimitive
{
    public string Id => ParameterEditorPrimitiveIds.Color;

    public bool CanCreate(NodeParameterDefinition definition, ParameterValue currentValue)
    {
        return definition.Kind == ParameterValueKind.Color;
    }

    public Control Create(ParameterEditorPrimitiveContext context)
    {
        var current = context.CurrentValue.AsColor();

        var swatch = new Border
        {
            Height = 18,
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Black,
            Margin = new Thickness(0, 0, 0, 4)
        };

        var hexBox = new TextBox
        {
            Text = NodeParameterEditorController.FormatColor(current),
            Watermark = "#RRGGBBAA"
        };

        var applyHexButton = new Button
        {
            Content = "Apply"
        };

        var redSlider = CreateColorSlider(current.R);
        var greenSlider = CreateColorSlider(current.G);
        var blueSlider = CreateColorSlider(current.B);
        var alphaSlider = CreateColorSlider(current.A);

        var isSyncing = false;

        void SetUiColor(RgbaColor color)
        {
            var clamped = RgbaColor.Clamp(color);

            isSyncing = true;
            redSlider.Value = clamped.R;
            greenSlider.Value = clamped.G;
            blueSlider.Value = clamped.B;
            alphaSlider.Value = clamped.A;
            hexBox.Text = NodeParameterEditorController.FormatColor(clamped);
            swatch.Background = new SolidColorBrush(
                Color.FromArgb(
                    ToByte(clamped.A),
                    ToByte(clamped.R),
                    ToByte(clamped.G),
                    ToByte(clamped.B)));
            isSyncing = false;
        }

        void ApplyColor(RgbaColor color)
        {
            var clamped = RgbaColor.Clamp(color);
            context.ApplyValue(ParameterValue.Color(clamped));
            SetUiColor(clamped);
        }

        void ApplyFromSliders()
        {
            if (isSyncing)
            {
                return;
            }

            ApplyColor(new RgbaColor(
                (float)redSlider.Value,
                (float)greenSlider.Value,
                (float)blueSlider.Value,
                (float)alphaSlider.Value));
        }

        applyHexButton.Click += (_, _) =>
        {
            try
            {
                ApplyColor(NodeParameterEditorController.ParseColor(hexBox.Text ?? string.Empty));
            }
            catch (Exception exception)
            {
                context.SetStatus($"Set parameter failed: {exception.Message}");
            }
        };

        hexBox.KeyDown += (_, keyEventArgs) =>
        {
            if (keyEventArgs.Key != Key.Enter)
            {
                return;
            }

            applyHexButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            keyEventArgs.Handled = true;
        };

        redSlider.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.Property == RangeBase.ValueProperty)
            {
                ApplyFromSliders();
            }
        };

        greenSlider.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.Property == RangeBase.ValueProperty)
            {
                ApplyFromSliders();
            }
        };

        blueSlider.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.Property == RangeBase.ValueProperty)
            {
                ApplyFromSliders();
            }
        };

        alphaSlider.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.Property == RangeBase.ValueProperty)
            {
                ApplyFromSliders();
            }
        };

        var hexRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 8
        };
        hexRow.Children.Add(hexBox);
        hexRow.Children.Add(applyHexButton);
        Grid.SetColumn(applyHexButton, 1);

        var sliderGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"),
            ColumnSpacing = 8,
            RowSpacing = 6
        };

        AddColorSliderRow(sliderGrid, 0, "R", redSlider);
        AddColorSliderRow(sliderGrid, 1, "G", greenSlider);
        AddColorSliderRow(sliderGrid, 2, "B", blueSlider);
        AddColorSliderRow(sliderGrid, 3, "A", alphaSlider);

        SetUiColor(current);

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
                swatch,
                hexRow,
                sliderGrid
            }
        };
    }

    private static Slider CreateColorSlider(float initial)
    {
        return new Slider
        {
            Minimum = 0,
            Maximum = 1,
            Value = initial,
            TickFrequency = 0.01,
            IsSnapToTickEnabled = false
        };
    }

    private static void AddColorSliderRow(Grid grid, int rowIndex, string channelLabel, Slider slider)
    {
        var label = new TextBlock
        {
            Classes = { "hint-text" },
            Text = channelLabel,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        grid.Children.Add(label);
        Grid.SetRow(label, rowIndex);

        grid.Children.Add(slider);
        Grid.SetColumn(slider, 1);
        Grid.SetRow(slider, rowIndex);
    }

    private static byte ToByte(float value)
    {
        return (byte)Math.Clamp((int)Math.Round(RgbaColor.Clamp01(value) * 255.0f), 0, 255);
    }
}

internal sealed class UnsupportedParameterEditorPrimitive : IParameterEditorPrimitive
{
    public string Id => "unsupported";

    public bool CanCreate(NodeParameterDefinition definition, ParameterValue currentValue)
    {
        return true;
    }

    public Control Create(ParameterEditorPrimitiveContext context)
    {
        return new TextBlock
        {
            Classes = { "hint-text" },
            Text = $"Unsupported parameter kind '{context.Definition.Kind}'."
        };
    }
}
