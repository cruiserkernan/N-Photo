using Editor.Domain.Imaging;

namespace Editor.Imaging;

public static class MvpNodeKernels
{
    private const int ChannelCount = 4;

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

    public static RgbaImage GaussianBlur(RgbaImage input, int radius)
    {
        if (radius <= 0)
        {
            return input.Clone();
        }

        var width = input.Width;
        var height = input.Height;
        var source = ToFloatBuffer(input);
        var horizontal = new float[source.Length];
        var vertical = new float[source.Length];
        var kernel = BuildGaussianKernel(radius);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                for (var channel = 0; channel < ChannelCount; channel++)
                {
                    var sum = 0.0f;
                    for (var kernelIndex = -radius; kernelIndex <= radius; kernelIndex++)
                    {
                        var sampleX = Math.Clamp(x + kernelIndex, 0, width - 1);
                        var sampleOffset = ((y * width) + sampleX) * ChannelCount + channel;
                        sum += source[sampleOffset] * kernel[kernelIndex + radius];
                    }

                    horizontal[((y * width) + x) * ChannelCount + channel] = sum;
                }
            }
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                for (var channel = 0; channel < ChannelCount; channel++)
                {
                    var sum = 0.0f;
                    for (var kernelIndex = -radius; kernelIndex <= radius; kernelIndex++)
                    {
                        var sampleY = Math.Clamp(y + kernelIndex, 0, height - 1);
                        var sampleOffset = ((sampleY * width) + x) * ChannelCount + channel;
                        sum += horizontal[sampleOffset] * kernel[kernelIndex + radius];
                    }

                    vertical[((y * width) + x) * ChannelCount + channel] = Clamp01(sum);
                }
            }
        }

        return FromFloatBuffer(width, height, vertical);
    }

    public static RgbaImage Sharpen(RgbaImage input, float amount, int radius)
    {
        if (amount <= 0.0f)
        {
            return input.Clone();
        }

        var blurred = GaussianBlur(input, Math.Max(radius, 1));
        var output = new RgbaImage(input.Width, input.Height);

        for (var y = 0; y < input.Height; y++)
        {
            for (var x = 0; x < input.Width; x++)
            {
                var original = input.GetPixel(x, y);
                var blur = blurred.GetPixel(x, y);

                output.SetPixel(
                    x,
                    y,
                    new RgbaColor(
                        Clamp01(original.R + ((original.R - blur.R) * amount)),
                        Clamp01(original.G + ((original.G - blur.G) * amount)),
                        Clamp01(original.B + ((original.B - blur.B) * amount)),
                        original.A));
            }
        }

        return output;
    }

    public static RgbaImage Blend(RgbaImage baseImage, RgbaImage topImage, string mode, float opacity)
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

    private static float[] BuildGaussianKernel(int radius)
    {
        var sigma = Math.Max(0.1f, radius * 0.5f);
        var denominator = 2.0f * sigma * sigma;
        var kernel = new float[(radius * 2) + 1];
        var sum = 0.0f;

        for (var i = -radius; i <= radius; i++)
        {
            var value = MathF.Exp(-(i * i) / denominator);
            kernel[i + radius] = value;
            sum += value;
        }

        if (sum <= 0.0001f)
        {
            return kernel;
        }

        for (var i = 0; i < kernel.Length; i++)
        {
            kernel[i] /= sum;
        }

        return kernel;
    }

    private static RgbaColor BlendColor(RgbaColor backdrop, RgbaColor source, string mode)
    {
        return mode switch
        {
            "multiply" => new RgbaColor(
                backdrop.R * source.R,
                backdrop.G * source.G,
                backdrop.B * source.B,
                source.A),
            "screen" => new RgbaColor(
                1 - ((1 - backdrop.R) * (1 - source.R)),
                1 - ((1 - backdrop.G) * (1 - source.G)),
                1 - ((1 - backdrop.B) * (1 - source.B)),
                source.A),
            _ => source
        };
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
