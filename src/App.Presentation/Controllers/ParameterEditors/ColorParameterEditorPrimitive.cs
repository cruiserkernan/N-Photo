using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Editor.Domain.Graph;
using Editor.Domain.Imaging;

namespace App.Presentation.Controllers;

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
