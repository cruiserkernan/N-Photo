using Editor.Domain.Imaging;

namespace Editor.Imaging;

public static partial class MvpNodeKernels
{
    private const int ChannelCount = 4;

    private static float[] ToFloatBuffer(RgbaImage image)
    {
        var buffer = new float[image.Width * image.Height * ChannelCount];
        var index = 0;
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var pixel = image.GetPixel(x, y);
                buffer[index++] = pixel.R;
                buffer[index++] = pixel.G;
                buffer[index++] = pixel.B;
                buffer[index++] = pixel.A;
            }
        }

        return buffer;
    }

    private static RgbaImage FromFloatBuffer(int width, int height, float[] buffer)
    {
        var image = new RgbaImage(width, height);
        var index = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image.SetPixel(
                    x,
                    y,
                    new RgbaColor(
                        buffer[index++],
                        buffer[index++],
                        buffer[index++],
                        buffer[index++]));
            }
        }

        return image;
    }

    private static RgbaColor SampleBilinear(RgbaImage image, float x, float y)
    {
        var x0 = (int)MathF.Floor(x);
        var y0 = (int)MathF.Floor(y);
        var x1 = x0 + 1;
        var y1 = y0 + 1;

        var tx = x - x0;
        var ty = y - y0;

        var c00 = GetPixelSafe(image, x0, y0);
        var c10 = GetPixelSafe(image, x1, y0);
        var c01 = GetPixelSafe(image, x0, y1);
        var c11 = GetPixelSafe(image, x1, y1);

        var top = Lerp(c00, c10, tx);
        var bottom = Lerp(c01, c11, tx);
        return Lerp(top, bottom, ty);
    }

    private static RgbaColor GetPixelSafe(RgbaImage image, int x, int y)
    {
        if (x < 0 || x >= image.Width || y < 0 || y >= image.Height)
        {
            return new RgbaColor(0, 0, 0, 0);
        }

        return image.GetPixel(x, y);
    }

    private static RgbaColor Lerp(RgbaColor a, RgbaColor b, float t)
    {
        return new RgbaColor(
            a.R + ((b.R - a.R) * t),
            a.G + ((b.G - a.G) * t),
            a.B + ((b.B - a.B) * t),
            a.A + ((b.A - a.A) * t));
    }

    private static float Clamp01(float value)
    {
        return value switch
        {
            < 0.0f => 0.0f,
            > 1.0f => 1.0f,
            _ => value
        };
    }
}
