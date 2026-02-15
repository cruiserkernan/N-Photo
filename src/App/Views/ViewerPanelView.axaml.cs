using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace App.Views;

public partial class ViewerPanelView : UserControl
{
    public ViewerPanelView()
    {
        InitializeComponent();
    }

    public Image PreviewImageControl => this.FindControl<Image>("PreviewImage")
                                            ?? throw new InvalidOperationException("PreviewImage not found.");

    public Canvas ViewerCanvasControl => this.FindControl<Canvas>("ViewerCanvas")
                                             ?? throw new InvalidOperationException("ViewerCanvas not found.");

    public Border ViewerLayerClipHostControl => this.FindControl<Border>("ViewerLayerClipHost")
                                                    ?? throw new InvalidOperationException("ViewerLayerClipHost not found.");

    public Canvas ViewerLayerControl => this.FindControl<Canvas>("ViewerLayer")
                                            ?? throw new InvalidOperationException("ViewerLayer not found.");

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
