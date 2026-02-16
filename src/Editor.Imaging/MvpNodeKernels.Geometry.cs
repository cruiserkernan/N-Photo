using Editor.Domain.Imaging;

namespace Editor.Imaging;

public static partial class MvpNodeKernels
{
    public static RgbaImage Transform(RgbaImage input, float scale, float rotateDegrees)
    {
        var safeScale = MathF.Max(scale, 0.001f);
        var radians = -rotateDegrees * (MathF.PI / 180.0f);
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);
        var centerX = (input.Width - 1) * 0.5f;
        var centerY = (input.Height - 1) * 0.5f;

        var output = new RgbaImage(input.Width, input.Height);
        for (var y = 0; y < output.Height; y++)
        {
            for (var x = 0; x < output.Width; x++)
            {
                var translatedX = x - centerX;
                var translatedY = y - centerY;

                var sourceX = ((translatedX * cos) - (translatedY * sin)) / safeScale + centerX;
                var sourceY = ((translatedX * sin) + (translatedY * cos)) / safeScale + centerY;

                output.SetPixel(x, y, SampleBilinear(input, sourceX, sourceY));
            }
        }

        return output;
    }

    public static RgbaImage Crop(RgbaImage input, int x, int y, int width, int height)
    {
        var output = new RgbaImage(width, height);
        for (var row = 0; row < height; row++)
        {
            for (var col = 0; col < width; col++)
            {
                var sourceX = x + col;
                var sourceY = y + row;
                output.SetPixel(col, row, GetPixelSafe(input, sourceX, sourceY));
            }
        }

        return output;
    }
}
