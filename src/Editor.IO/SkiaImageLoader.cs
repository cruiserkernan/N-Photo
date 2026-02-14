using Editor.Domain.Imaging;
using SkiaSharp;

namespace Editor.IO;

public sealed class SkiaImageLoader : IImageLoader
{
    public bool TryLoad(string path, out RgbaImage? image, out string errorMessage)
    {
        image = null;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            errorMessage = "Path is required.";
            return false;
        }

        if (!File.Exists(path))
        {
            errorMessage = $"File not found: '{path}'.";
            return false;
        }

        try
        {
            using var bitmap = SKBitmap.Decode(path);
            if (bitmap is null)
            {
                errorMessage = "Skia failed to decode image.";
                return false;
            }

            image = ToRgbaImage(bitmap);
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    private static RgbaImage ToRgbaImage(SKBitmap bitmap)
    {
        var image = new RgbaImage(bitmap.Width, bitmap.Height);
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                image.SetPixel(
                    x,
                    y,
                    new RgbaColor(
                        color.Red / 255.0f,
                        color.Green / 255.0f,
                        color.Blue / 255.0f,
                        color.Alpha / 255.0f));
            }
        }

        return image;
    }
}
