using Editor.Domain.Imaging;

namespace Editor.Imaging;

public static partial class MvpNodeKernels
{
    public static RgbaImage ExposureContrast(RgbaImage input, float exposure, float contrast)
    {
        var output = new RgbaImage(input.Width, input.Height);
        var exposureScale = MathF.Pow(2.0f, exposure);

        for (var y = 0; y < input.Height; y++)
        {
            for (var x = 0; x < input.Width; x++)
            {
                var pixel = input.GetPixel(x, y);
                output.SetPixel(
                    x,
                    y,
                    new RgbaColor(
                        ApplyExposureContrast(pixel.R, exposureScale, contrast),
                        ApplyExposureContrast(pixel.G, exposureScale, contrast),
                        ApplyExposureContrast(pixel.B, exposureScale, contrast),
                        pixel.A));
            }
        }

        return output;
    }

    public static RgbaImage Curves(RgbaImage input, float gamma)
    {
        var output = new RgbaImage(input.Width, input.Height);
        var safeGamma = MathF.Max(gamma, 0.001f);
        var inverse = 1.0f / safeGamma;

        for (var y = 0; y < input.Height; y++)
        {
            for (var x = 0; x < input.Width; x++)
            {
                var pixel = input.GetPixel(x, y);
                output.SetPixel(
                    x,
                    y,
                    new RgbaColor(
                        MathF.Pow(Clamp01(pixel.R), inverse),
                        MathF.Pow(Clamp01(pixel.G), inverse),
                        MathF.Pow(Clamp01(pixel.B), inverse),
                        pixel.A));
            }
        }

        return output;
    }

    public static RgbaImage Hsl(RgbaImage input, float hueShift, float saturationScale, float lightnessScale)
    {
        var output = new RgbaImage(input.Width, input.Height);

        for (var y = 0; y < input.Height; y++)
        {
            for (var x = 0; x < input.Width; x++)
            {
                var pixel = input.GetPixel(x, y);
                var (h, s, l) = RgbToHsl(pixel.R, pixel.G, pixel.B);
                var adjustedH = NormalizeDegrees(h + hueShift);
                var adjustedS = Clamp01(s * saturationScale);
                var adjustedL = Clamp01(l * lightnessScale);
                var (r, g, b) = HslToRgb(adjustedH, adjustedS, adjustedL);
                output.SetPixel(x, y, new RgbaColor(r, g, b, pixel.A));
            }
        }

        return output;
    }

    public static RgbaImage ApplyMask(RgbaImage original, RgbaImage processed, RgbaImage mask)
    {
        var output = new RgbaImage(processed.Width, processed.Height);

        for (var y = 0; y < processed.Height; y++)
        {
            for (var x = 0; x < processed.Width; x++)
            {
                var sourcePixel = processed.GetPixel(x, y);
                var originalPixel = x < original.Width && y < original.Height
                    ? original.GetPixel(x, y)
                    : sourcePixel;
                var maskPixel = x < mask.Width && y < mask.Height
                    ? mask.GetPixel(x, y)
                    : new RgbaColor(0, 0, 0, 0);
                var maskWeight = ResolveMaskWeight(maskPixel);

                output.SetPixel(x, y, Lerp(originalPixel, sourcePixel, maskWeight));
            }
        }

        return output;
    }

    private static float ApplyExposureContrast(float value, float exposureScale, float contrast)
    {
        var contrasted = ((value - 0.5f) * contrast) + 0.5f;
        return Clamp01(contrasted * exposureScale);
    }

    private static float ResolveMaskWeight(RgbaColor maskPixel)
    {
        var luma = (maskPixel.R + maskPixel.G + maskPixel.B) / 3.0f;
        return Clamp01(MathF.Max(maskPixel.A, luma));
    }

    private static (float H, float S, float L) RgbToHsl(float r, float g, float b)
    {
        var max = MathF.Max(r, MathF.Max(g, b));
        var min = MathF.Min(r, MathF.Min(g, b));
        var lightness = (max + min) * 0.5f;

        if (MathF.Abs(max - min) < 0.0001f)
        {
            return (0.0f, 0.0f, lightness);
        }

        var delta = max - min;
        var saturation = lightness > 0.5f
            ? delta / (2.0f - max - min)
            : delta / (max + min);

        float hue;
        if (MathF.Abs(max - r) < 0.0001f)
        {
            hue = ((g - b) / delta) + (g < b ? 6.0f : 0.0f);
        }
        else if (MathF.Abs(max - g) < 0.0001f)
        {
            hue = ((b - r) / delta) + 2.0f;
        }
        else
        {
            hue = ((r - g) / delta) + 4.0f;
        }

        return (hue * 60.0f, saturation, lightness);
    }

    private static (float R, float G, float B) HslToRgb(float hue, float saturation, float lightness)
    {
        if (saturation <= 0.0001f)
        {
            return (lightness, lightness, lightness);
        }

        var q = lightness < 0.5f
            ? lightness * (1.0f + saturation)
            : lightness + saturation - (lightness * saturation);
        var p = (2.0f * lightness) - q;
        var hk = hue / 360.0f;

        var r = HueToChannel(p, q, hk + (1.0f / 3.0f));
        var g = HueToChannel(p, q, hk);
        var b = HueToChannel(p, q, hk - (1.0f / 3.0f));

        return (Clamp01(r), Clamp01(g), Clamp01(b));
    }

    private static float HueToChannel(float p, float q, float t)
    {
        if (t < 0.0f)
        {
            t += 1.0f;
        }

        if (t > 1.0f)
        {
            t -= 1.0f;
        }

        if (t < 1.0f / 6.0f)
        {
            return p + ((q - p) * 6.0f * t);
        }

        if (t < 1.0f / 2.0f)
        {
            return q;
        }

        if (t < 2.0f / 3.0f)
        {
            return p + ((q - p) * ((2.0f / 3.0f) - t) * 6.0f);
        }

        return p;
    }

    private static float NormalizeDegrees(float value)
    {
        var normalized = value % 360.0f;
        if (normalized < 0.0f)
        {
            normalized += 360.0f;
        }

        return normalized;
    }
}
