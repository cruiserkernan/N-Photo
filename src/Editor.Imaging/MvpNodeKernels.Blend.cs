using Editor.Domain.Imaging;

namespace Editor.Imaging;

public static partial class MvpNodeKernels
{
    public static RgbaImage Blend(RgbaImage baseImage, RgbaImage topImage, string mode, float opacity)
    {
        return Blend(baseImage, topImage, BlendModeParser.Parse(mode), opacity);
    }

    public static RgbaImage Blend(RgbaImage baseImage, RgbaImage topImage, BlendMode mode, float opacity)
    {
        var width = Math.Min(baseImage.Width, topImage.Width);
        var height = Math.Min(baseImage.Height, topImage.Height);
        var safeOpacity = Clamp01(opacity);

        var output = new RgbaImage(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var backdrop = baseImage.GetPixel(x, y);
                var source = topImage.GetPixel(x, y);

                var sourceAlpha = Clamp01(source.A * safeOpacity);
                var backdropAlpha = Clamp01(backdrop.A);
                var outputAlpha = sourceAlpha + (backdropAlpha * (1 - sourceAlpha));

                if (outputAlpha <= 0.0001f)
                {
                    output.SetPixel(x, y, new RgbaColor(0, 0, 0, 0));
                    continue;
                }

                var blendedSource = BlendColor(backdrop, source, mode);

                var outputR = ((blendedSource.R * sourceAlpha) + (backdrop.R * backdropAlpha * (1 - sourceAlpha))) / outputAlpha;
                var outputG = ((blendedSource.G * sourceAlpha) + (backdrop.G * backdropAlpha * (1 - sourceAlpha))) / outputAlpha;
                var outputB = ((blendedSource.B * sourceAlpha) + (backdrop.B * backdropAlpha * (1 - sourceAlpha))) / outputAlpha;

                output.SetPixel(
                    x,
                    y,
                    new RgbaColor(
                        Clamp01(outputR),
                        Clamp01(outputG),
                        Clamp01(outputB),
                        Clamp01(outputAlpha)));
            }
        }

        return output;
    }

    private static RgbaColor BlendColor(RgbaColor backdrop, RgbaColor source, BlendMode mode)
    {
        return mode switch
        {
            BlendMode.Multiply => new RgbaColor(
                backdrop.R * source.R,
                backdrop.G * source.G,
                backdrop.B * source.B,
                source.A),
            BlendMode.Screen => new RgbaColor(
                1 - ((1 - backdrop.R) * (1 - source.R)),
                1 - ((1 - backdrop.G) * (1 - source.G)),
                1 - ((1 - backdrop.B) * (1 - source.B)),
                source.A),
            _ => source
        };
    }
}
