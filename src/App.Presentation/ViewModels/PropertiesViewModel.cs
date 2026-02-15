namespace App.Presentation.ViewModels;

public sealed class PropertiesViewModel : ObservableObject
{
    private string _selectedNodeDisplay = "None";

    public string SelectedNodeDisplay
    {
        get => _selectedNodeDisplay;
        set => SetProperty(ref _selectedNodeDisplay, value);
    }
}
