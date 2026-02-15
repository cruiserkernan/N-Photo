using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace App.Views;

public partial class PropertiesPanelView : UserControl
{
    public PropertiesPanelView()
    {
        InitializeComponent();
    }

    public TextBlock SelectedNodeTextControl => this.FindControl<TextBlock>("SelectedNodeText")
                                                ?? throw new InvalidOperationException("SelectedNodeText not found.");

    public StackPanel PropertyEditorHostControl => this.FindControl<StackPanel>("PropertyEditorHost")
                                                   ?? throw new InvalidOperationException("PropertyEditorHost not found.");

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
