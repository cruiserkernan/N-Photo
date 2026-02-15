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

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
