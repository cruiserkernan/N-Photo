using Avalonia.Media.Imaging;

namespace App.Presentation.ViewModels;

public sealed class ViewerViewModel : ObservableObject
{
    private WriteableBitmap? _previewBitmap;

    public WriteableBitmap? PreviewBitmap
    {
        get => _previewBitmap;
        set => SetProperty(ref _previewBitmap, value);
    }
}
