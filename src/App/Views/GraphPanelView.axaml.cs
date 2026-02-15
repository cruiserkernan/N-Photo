using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace App.Views;

public partial class GraphPanelView : UserControl
{
    public GraphPanelView()
    {
        InitializeComponent();
    }

    public Canvas NodeCanvasControl => this.FindControl<Canvas>("NodeCanvas")
                                           ?? throw new InvalidOperationException("NodeCanvas not found.");

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
