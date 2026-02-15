namespace App.Presentation.ViewModels;

public sealed class ShellViewModel : ObservableObject
{
    private string _status = "Ready";
    private string _nodeSearchText = string.Empty;
    private IReadOnlyList<string> _availableNodeTypes = Array.Empty<string>();

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string NodeSearchText
    {
        get => _nodeSearchText;
        set => SetProperty(ref _nodeSearchText, value);
    }

    public IReadOnlyList<string> AvailableNodeTypes
    {
        get => _availableNodeTypes;
        set => SetProperty(ref _availableNodeTypes, value);
    }
}
