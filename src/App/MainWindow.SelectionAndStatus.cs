using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Editor.Domain.Graph;

namespace App;

public partial class MainWindow
{
    private void PruneSelectionAndPreviewSlots(IReadOnlyList<Node> nodes)
    {
        var liveNodeIds = nodes.Select(node => node.Id).ToHashSet();
        if (_selectedNodeId is NodeId selectedNodeId && !liveNodeIds.Contains(selectedNodeId))
        {
            _selectedNodeId = null;
        }

        _nodeActionController.PruneUnavailableNodes(liveNodeIds);

        if (_previewRouting.Prune(liveNodeIds))
        {
            _editorSession.RequestPreviewRender();
            SetStatus("Active preview slot was removed. Showing output.");
        }

        ApplyNodeSelectionVisuals();
    }

    private void SetSelectedNode(NodeId nodeId)
    {
        if (_selectedNodeId == nodeId)
        {
            return;
        }

        _selectedNodeId = nodeId;
        ApplyNodeSelectionVisuals();
        RefreshPropertiesEditor();
    }

    private InputElement? GetFocusedElement()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        return topLevel?.FocusManager?.GetFocusedElement() as InputElement;
    }

    private void RefreshPropertiesEditor()
    {
        _propertiesPanelController.Refresh(
            PropertyEditorHost,
            SelectedNodeText,
            _selectedNodeId,
            _nodeLookup);
    }

    private IBrush ResolveBrush(string resourceKey, string fallbackHex)
    {
        if (TryGetResource(resourceKey, ActualThemeVariant, out var resource) &&
            resource is IBrush brush)
        {
            return brush;
        }

        return Brush.Parse(fallbackHex);
    }

    private void SetStatus(string message)
    {
        StatusTextBlock.Text = message;
    }
}
