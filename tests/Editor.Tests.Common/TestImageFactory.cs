using Editor.Domain.Imaging;

namespace Editor.Tests.Common;

public static class TestImageFactory
{
    public static RgbaImage CreateGradient(int width = 16, int height = 16)
    {
        var image = new RgbaImage(width, height);
        var maxX = Math.Max(1, width - 1);
        var maxY = Math.Max(1, height - 1);

        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var r = x / (float)maxX;
                var g = y / (float)maxY;
                var b = (x + y) / (float)(maxX + maxY);
                image.SetPixel(x, y, new RgbaColor(r, g, b, 1.0f));
            }
        }

        return image;
    }
}
