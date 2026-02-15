using Editor.Domain.Graph;

namespace App.Presentation.ViewModels;

public sealed class GraphViewModel : ObservableObject
{
    private NodeId? _selectedNodeId;

    public NodeId? SelectedNodeId
    {
        get => _selectedNodeId;
        set => SetProperty(ref _selectedNodeId, value);
    }
}
