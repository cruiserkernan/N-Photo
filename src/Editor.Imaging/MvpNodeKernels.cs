using Editor.Domain.Imaging;
using SkiaSharp;

namespace Editor.Imaging;

public static class MvpNodeKernels
{
    public static RgbaImage Transform(RgbaImage input, float scale, float rotateDegrees)
    {
        using var source = ToSkBitmap(input);
        using var output = CreateEmptyBitmap(input.Width, input.Height);
        using var canvas = new SKCanvas(output);
        using var paint = new SKPaint
        {
            FilterQuality = SKFilterQuality.Medium,
            IsAntialias = true
        };

        var centerX = input.Width * 0.5f;
        var centerY = input.Height * 0.5f;

        canvas.Clear(SKColors.Transparent);
        canvas.Translate(centerX, centerY);
        canvas.Scale(MathF.Max(scale, 0.001f));
        canvas.RotateDegrees(rotateDegrees);
        canvas.Translate(-centerX, -centerY);
        canvas.DrawBitmap(source, 0, 0, paint);
        canvas.Flush();

        return ToRgbaImage(output);
    }

    public static RgbaImage Crop(RgbaImage input, int x, int y, int width, int height)
    {
        using var source = ToSkBitmap(input);
        using var output = CreateEmptyBitmap(width, height);
        using var canvas = new SKCanvas(output);
        using var paint = new SKPaint { FilterQuality = SKFilterQuality.High };

        var srcRect = new SKRectI(x, y, x + width, y + height);
        var dstRect = new SKRect(0, 0, width, height);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(source, srcRect, dstRect, paint);
        canvas.Flush();

        return ToRgbaImage(output);
    }

    public static RgbaImage ExposureContrast(RgbaImage input, float exposure, float contrast)
    {
        var exposureScale = MathF.Pow(2.0f, exposure);
        using var bitmap = ToSkBitmap(input);

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                bitmap.SetPixel(
                    x,
                    y,
                    new SKColor(
                        ToByte(ApplyExposureContrast(color.Red / 255.0f, exposureScale, contrast)),
                        ToByte(ApplyExposureContrast(color.Green / 255.0f, exposureScale, contrast)),
                        ToByte(ApplyExposureContrast(color.Blue / 255.0f, exposureScale, contrast)),
                        color.Alpha));
            }
        }

        return ToRgbaImage(bitmap);
    }

    public static RgbaImage Curves(RgbaImage input, float gamma)
    {
        using var bitmap = ToSkBitmap(input);
        var safeGamma = MathF.Max(gamma, 0.001f);
        var inverse = 1.0f / safeGamma;

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                bitmap.SetPixel(
                    x,
                    y,
                    new SKColor(
                        ToByte(MathF.Pow(color.Red / 255.0f, inverse)),
                        ToByte(MathF.Pow(color.Green / 255.0f, inverse)),
                        ToByte(MathF.Pow(color.Blue / 255.0f, inverse)),
                        color.Alpha));
            }
        }

        return ToRgbaImage(bitmap);
    }

    public static RgbaImage Hsl(RgbaImage input, float hueShift, float saturationScale, float lightnessScale)
    {
        using var bitmap = ToSkBitmap(input);

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                var (h, s, l) = RgbToHsl(color);
                var adjustedH = NormalizeDegrees(h + hueShift);
                var adjustedS = Clamp01(s * saturationScale);
                var adjustedL = Clamp01(l * lightnessScale);
                bitmap.SetPixel(x, y, HslToColor(adjustedH, adjustedS, adjustedL, color.Alpha));
            }
        }

        return ToRgbaImage(bitmap);
    }

    public static RgbaImage GaussianBlur(RgbaImage input, int radius)
    {
        if (radius <= 0)
        {
            return input.Clone();
        }

        var sigma = Math.Max(0.1f, radius * 0.5f);
        using var source = ToSkBitmap(input);
        using var output = CreateEmptyBitmap(input.Width, input.Height);
        using var canvas = new SKCanvas(output);
        using var filter = SKImageFilter.CreateBlur(sigma, sigma);
        using var paint = new SKPaint { ImageFilter = filter };

        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(source, 0, 0, paint);
        canvas.Flush();

        return ToRgbaImage(output);
    }

    public static RgbaImage Sharpen(RgbaImage input, float amount, int radius)
    {
        if (amount <= 0.0f)
        {
            return input.Clone();
        }

        var blurred = GaussianBlur(input, Math.Max(radius, 1));
        using var originalBitmap = ToSkBitmap(input);
        using var blurredBitmap = ToSkBitmap(blurred);
        using var output = CreateEmptyBitmap(input.Width, input.Height);

        for (var y = 0; y < output.Height; y++)
        {
            for (var x = 0; x < output.Width; x++)
            {
                var original = originalBitmap.GetPixel(x, y);
                var blur = blurredBitmap.GetPixel(x, y);

                var r = (original.Red / 255.0f) + (((original.Red - blur.Red) / 255.0f) * amount);
                var g = (original.Green / 255.0f) + (((original.Green - blur.Green) / 255.0f) * amount);
                var b = (original.Blue / 255.0f) + (((original.Blue - blur.Blue) / 255.0f) * amount);

                output.SetPixel(x, y, new SKColor(ToByte(r), ToByte(g), ToByte(b), original.Alpha));
            }
        }

        return ToRgbaImage(output);
    }

    public static RgbaImage Blend(RgbaImage baseImage, RgbaImage topImage, string mode, float opacity)
    {
        var width = Math.Min(baseImage.Width, topImage.Width);
        var height = Math.Min(baseImage.Height, topImage.Height);
        using var baseBitmap = ToSkBitmap(Crop(baseImage, 0, 0, width, height));
        using var topBitmap = ToSkBitmap(Crop(topImage, 0, 0, width, height));
        using var output = CreateEmptyBitmap(width, height);
        using var canvas = new SKCanvas(output);
        using var topPaint = new SKPaint
        {
            BlendMode = ResolveBlendMode(mode),
            Color = new SKColor(255, 255, 255, ToByte(Clamp01(opacity)))
        };

        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(baseBitmap, 0, 0);
        canvas.DrawBitmap(topBitmap, 0, 0, topPaint);
        canvas.Flush();

        return ToRgbaImage(output);
    }

    private static SKBlendMode ResolveBlendMode(string mode)
    {
        return mode switch
        {
            "multiply" => SKBlendMode.Multiply,
            "screen" => SKBlendMode.Screen,
            _ => SKBlendMode.SrcOver
        };
    }

    private static float ApplyExposureContrast(float value, float exposureScale, float contrast)
    {
        var contrasted = ((value - 0.5f) * contrast) + 0.5f;
        return Clamp01(contrasted * exposureScale);
    }

    private static SKBitmap ToSkBitmap(RgbaImage image)
    {
        var bitmap = CreateEmptyBitmap(image.Width, image.Height);
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

    private static SKBitmap CreateEmptyBitmap(int width, int height)
    {
        return new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
    }

    private static (float H, float S, float L) RgbToHsl(SKColor color)
    {
        var r = color.Red / 255.0f;
        var g = color.Green / 255.0f;
        var b = color.Blue / 255.0f;
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

    private static SKColor HslToColor(float hue, float saturation, float lightness, byte alpha)
    {
        if (saturation <= 0.0001f)
        {
            var channel = ToByte(lightness);
            return new SKColor(channel, channel, channel, alpha);
        }

        var q = lightness < 0.5f
            ? lightness * (1.0f + saturation)
            : lightness + saturation - (lightness * saturation);
        var p = (2.0f * lightness) - q;
        var hk = hue / 360.0f;

        var r = HueToChannel(p, q, hk + (1.0f / 3.0f));
        var g = HueToChannel(p, q, hk);
        var b = HueToChannel(p, q, hk - (1.0f / 3.0f));
        return new SKColor(ToByte(r), ToByte(g), ToByte(b), alpha);
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

    private static byte ToByte(float value)
    {
        return (byte)Math.Clamp((int)Math.Round(Clamp01(value) * 255.0f), 0, 255);
    }
}
