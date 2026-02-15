using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace App.Views;

public partial class TopToolbarView : UserControl
{
    public TopToolbarView()
    {
        InitializeComponent();
    }

    public Button ExportButtonControl => this.FindControl<Button>("ExportButton")
                                             ?? throw new InvalidOperationException("ExportButton not found.");

    public Button UndoButtonControl => this.FindControl<Button>("UndoButton")
                                           ?? throw new InvalidOperationException("UndoButton not found.");

    public Button RedoButtonControl => this.FindControl<Button>("RedoButton")
                                           ?? throw new InvalidOperationException("RedoButton not found.");

    public TextBlock StatusTextBlockControl => this.FindControl<TextBlock>("StatusTextBlock")
                                                   ?? throw new InvalidOperationException("StatusTextBlock not found.");

    public StackPanel NodeStripHostControl => this.FindControl<StackPanel>("NodeStripHost")
                                                ?? throw new InvalidOperationException("NodeStripHost not found.");

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
