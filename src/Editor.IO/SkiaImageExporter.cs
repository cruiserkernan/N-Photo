using Editor.Domain.Imaging;
using SkiaSharp;

namespace Editor.IO;

public sealed class SkiaImageExporter : IImageExporter
{
    public bool TryExport(RgbaImage image, string path, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            errorMessage = "Path is required.";
            return false;
        }

        try
        {
            var extension = Path.GetExtension(path);
            if (!TryResolveFormat(extension, out var format))
            {
                errorMessage = $"Unsupported extension '{extension}'. Use .png, .jpg, or .jpeg.";
                return false;
            }

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var bitmap = ToSkBitmap(image);
            using var skImage = SKImage.FromBitmap(bitmap);
            using var data = skImage.Encode(format, quality: 92);
            if (data is null)
            {
                errorMessage = "Skia failed to encode image.";
                return false;
            }

            using var file = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
            data.SaveTo(file);
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    private static bool TryResolveFormat(string extension, out SKEncodedImageFormat format)
    {
        format = SKEncodedImageFormat.Png;
        if (string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase))
        {
            format = SKEncodedImageFormat.Png;
            return true;
        }

        if (string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            format = SKEncodedImageFormat.Jpeg;
            return true;
        }

        return false;
    }

    private static SKBitmap ToSkBitmap(RgbaImage image)
    {
        var bitmap = new SKBitmap(image.Width, image.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var pixel = image.GetPixel(x, y);
                bitmap.SetPixel(
                    x,
                    y,
                    new SKColor(
                        ToByte(pixel.R),
                        ToByte(pixel.G),
                        ToByte(pixel.B),
                        ToByte(pixel.A)));
            }
        }

        return bitmap;
    }

    private static byte ToByte(float value)
    {
        return (byte)Math.Clamp((int)Math.Round(value * 255.0f), 0, 255);
    }
}
