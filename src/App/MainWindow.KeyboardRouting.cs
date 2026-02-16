using Avalonia.Controls;
using Avalonia.Input;
using App.Presentation.Controllers;
using Editor.Domain.Graph;

namespace App;

public partial class MainWindow
{
    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (TryHandleProjectShortcut(e))
        {
            return;
        }

        if (e.Key == Key.Escape && _isConnectionDragging)
        {
            ResetConnectionDragState();
            SetStatus("Connection canceled.");
            e.Handled = true;
            return;
        }

        if ((e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) != 0)
        {
            return;
        }

        if (GetFocusedElement() is TextBox)
        {
            return;
        }

        if (e.Key == Key.Delete)
        {
            DeleteSelectedNodeWithBypassReconnect();
            e.Handled = true;
            return;
        }

        if (PreviewRoutingController.IsPreviewResetKey(e.Key))
        {
            _previewRouting.Reset();
            _editorSession.RequestPreviewRender();
            SetStatus("Preview reset to output.");
            OnPersistentStateMutated();
            e.Handled = true;
            return;
        }

        if (!PreviewRoutingController.TryMapPreviewSlot(e.Key, out var slot))
        {
            return;
        }

        if (_selectedNodeId is NodeId selectedNodeId && _nodeLookup.ContainsKey(selectedNodeId))
        {
            _previewRouting.AssignSlot(slot, selectedNodeId);
            RequestPreviewForActiveSlot();
            SetStatus($"Assigned preview slot {slot} to {_nodeLookup[selectedNodeId].Type}.");
            OnPersistentStateMutated();
            e.Handled = true;
            return;
        }

        if (!_previewRouting.HasSlot(slot))
        {
            SetStatus($"Preview slot {slot} is empty.");
            e.Handled = true;
            return;
        }

        _previewRouting.Activate(slot);
        RequestPreviewForActiveSlot();
        SetStatus($"Preview slot {slot} activated.");
        OnPersistentStateMutated();
        e.Handled = true;
    }

    private void RequestPreviewForActiveSlot()
    {
        if (_previewRouting.TryGetActiveTarget(_nodeLookup.Keys.ToArray(), out var previewNodeId))
        {
            _editorSession.RequestPreviewRender(previewNodeId);
            return;
        }

            _editorSession.RequestPreviewRender();
    }

    private void DeleteSelectedNodeWithBypassReconnect()
    {
        if (_selectedNodeId is not NodeId selectedNodeId ||
            !_nodeLookup.TryGetValue(selectedNodeId, out var selectedNode))
        {
            return;
        }

        try
        {
            _editorSession.RemoveNode(selectedNodeId, reconnectPrimaryStream: true);
            RefreshGraphBindings();
            SetStatus($"Deleted node '{selectedNode.Type}' with reconnect.");
        }
        catch (Exception exception)
        {
            SetStatus($"Delete failed: {exception.Message}");
        }
    }
}
