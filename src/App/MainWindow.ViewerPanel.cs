using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using App.Presentation.Controllers;
using Editor.Engine;

namespace App;

public partial class MainWindow
{
    private const double ViewerMinZoomScale = 0.1;
    private const double ViewerMaxZoomScale = 12.0;
    private const double ViewerZoomStepFactor = 1.1;

    private bool _isPanningViewer;
    private bool _hasAutoFittedViewer;
    private Point _lastViewerPanPointerScreen;
    private Vector _viewerPanOffset;
    private double _viewerZoomScale = 1.0;
    private PixelSize? _viewerImagePixelSize;
    private WriteableBitmap? _previewBitmap;

    private Canvas ViewerCanvas => ViewerPanelView.ViewerCanvasControl;

    private Border ViewerLayerClipHost => ViewerPanelView.ViewerLayerClipHostControl;

    private Canvas ViewerLayer => ViewerPanelView.ViewerLayerControl;

    private Image PreviewImage => ViewerPanelView.PreviewImageControl;

    private void OnPreviewUpdated(object? sender, PreviewFrame frame)
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                var previousViewerImageSize = _viewerImagePixelSize;
                _previewBitmap?.Dispose();
                var bitmap = new WriteableBitmap(
                    new PixelSize(frame.Width, frame.Height),
                    new Avalonia.Vector(96, 96),
                    PixelFormat.Rgba8888,
                    AlphaFormat.Unpremul);

                using (var locked = bitmap.Lock())
                {
                    Marshal.Copy(frame.RgbaBytes, 0, locked.Address, frame.RgbaBytes.Length);
                }

                _previewBitmap = bitmap;
                PreviewImage.Source = _previewBitmap;
                _viewerImagePixelSize = bitmap.PixelSize;

                if (!_hasAutoFittedViewer || previousViewerImageSize != _viewerImagePixelSize)
                {
                    AutoFitViewerToImage();
                    _hasAutoFittedViewer = true;
                }

                ApplyViewerTransform();
                SetStatus(_editorSession.GetSnapshot().Status);
            }
            catch (Exception exception)
            {
                SetStatus($"Preview update failed: {exception.Message}");
            }
        });
    }

    private void InitializeViewerViewport()
    {
        Canvas.SetLeft(ViewerLayerClipHost, 0);
        Canvas.SetTop(ViewerLayerClipHost, 0);
        ViewerLayerClipHost.Child = ViewerLayer;
        UpdateViewerViewportClip();
        ApplyViewerTransform();
    }

    private void OnViewerCanvasSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateViewerViewportClip();

        if (_viewerImagePixelSize.HasValue && !_hasAutoFittedViewer && e.NewSize.Width > 0 && e.NewSize.Height > 0)
        {
            AutoFitViewerToImage();
            _hasAutoFittedViewer = true;
        }

        ApplyViewerTransform();
    }

    private void OnViewerCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(ViewerCanvas);
        if (!pointerPoint.Properties.IsMiddleButtonPressed || !IsViewerSurfaceSource(e.Source))
        {
            return;
        }

        _isPanningViewer = true;
        _lastViewerPanPointerScreen = e.GetPosition(ViewerCanvas);
        ViewerCanvas.Focus();
        e.Pointer.Capture(ViewerCanvas);
        e.Handled = true;
    }

    private void OnViewerCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanningViewer || e.Pointer.Captured != ViewerCanvas)
        {
            return;
        }

        var position = e.GetPosition(ViewerCanvas);
        var delta = new Vector(
            position.X - _lastViewerPanPointerScreen.X,
            position.Y - _lastViewerPanPointerScreen.Y);
        _lastViewerPanPointerScreen = position;
        _viewerPanOffset += delta;
        ApplyViewerTransform();
        e.Handled = true;
    }

    private void OnViewerCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isPanningViewer)
        {
            return;
        }

        e.Pointer.Capture(null);
        StopViewerPan();
        e.Handled = true;
    }

    private void OnViewerCanvasPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (!_isPanningViewer)
        {
            return;
        }

        StopViewerPan();
    }

    private void OnViewerCanvasPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!PanZoomController.TryZoomAtPointer(
                pointerScreen: e.GetPosition(ViewerCanvas),
                wheelDelta: e.Delta.Y,
                currentZoom: _viewerZoomScale,
                currentPan: _viewerPanOffset,
                minZoom: ViewerMinZoomScale,
                maxZoom: ViewerMaxZoomScale,
                zoomStepFactor: ViewerZoomStepFactor,
                out var nextZoom,
                out var nextPan))
        {
            return;
        }

        _viewerZoomScale = nextZoom;
        _viewerPanOffset = nextPan;
        ApplyViewerTransform();
        e.Handled = true;
    }

    private bool IsViewerSurfaceSource(object? source)
    {
        return ReferenceEquals(source, ViewerCanvas) ||
               ReferenceEquals(source, ViewerLayer) ||
               ReferenceEquals(source, ViewerLayerClipHost) ||
               ReferenceEquals(source, PreviewImage);
    }

    private void StopViewerPan()
    {
        _isPanningViewer = false;
    }

    private void ApplyViewerTransform()
    {
        UpdateViewerViewportClip();
        GraphViewportController.ApplyGraphTransform(ViewerLayer, _viewerZoomScale, _viewerPanOffset);
    }

    private void UpdateViewerViewportClip()
    {
        GraphViewportController.UpdateGraphViewportClip(ViewerCanvas, ViewerLayerClipHost);
    }

    private void AutoFitViewerToImage()
    {
        if (!_viewerImagePixelSize.HasValue)
        {
            return;
        }

        var imageSize = _viewerImagePixelSize.Value;
        var viewportWidth = ViewerCanvas.Bounds.Width;
        var viewportHeight = ViewerCanvas.Bounds.Height;
        if (viewportWidth <= 0 || viewportHeight <= 0 || imageSize.Width <= 0 || imageSize.Height <= 0)
        {
            return;
        }

        var fitZoom = Math.Min(
            viewportWidth / imageSize.Width,
            viewportHeight / imageSize.Height);
        _viewerZoomScale = Math.Clamp(fitZoom, ViewerMinZoomScale, ViewerMaxZoomScale);
        _viewerPanOffset = new Vector(
            (viewportWidth - (imageSize.Width * _viewerZoomScale)) / 2.0,
            (viewportHeight - (imageSize.Height * _viewerZoomScale)) / 2.0);
    }
}
