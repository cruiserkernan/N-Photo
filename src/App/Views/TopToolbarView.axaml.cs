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

    public Button NewProjectButtonControl => this.FindControl<Button>("NewProjectButton")
                                                 ?? throw new InvalidOperationException("NewProjectButton not found.");

    public Button OpenProjectButtonControl => this.FindControl<Button>("OpenProjectButton")
                                                  ?? throw new InvalidOperationException("OpenProjectButton not found.");

    public Button SaveProjectButtonControl => this.FindControl<Button>("SaveProjectButton")
                                                  ?? throw new InvalidOperationException("SaveProjectButton not found.");

    public Button SaveProjectAsButtonControl => this.FindControl<Button>("SaveProjectAsButton")
                                                    ?? throw new InvalidOperationException("SaveProjectAsButton not found.");

    public Button UndoButtonControl => this.FindControl<Button>("UndoButton")
                                           ?? throw new InvalidOperationException("UndoButton not found.");

    public Button RedoButtonControl => this.FindControl<Button>("RedoButton")
                                           ?? throw new InvalidOperationException("RedoButton not found.");

    public TextBlock StatusTextBlockControl => this.FindControl<TextBlock>("StatusTextBlock")
                                                   ?? throw new InvalidOperationException("StatusTextBlock not found.");

    public StackPanel NodeStripHostControl => this.FindControl<StackPanel>("NodeStripHost")
                                                ?? throw new InvalidOperationException("NodeStripHost not found.");

    public TextBox NodeSearchBoxControl => this.FindControl<TextBox>("NodeSearchBox")
                                           ?? throw new InvalidOperationException("NodeSearchBox not found.");

    public Button NodeSearchAddButtonControl => this.FindControl<Button>("NodeSearchAddButton")
                                                ?? throw new InvalidOperationException("NodeSearchAddButton not found.");

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
