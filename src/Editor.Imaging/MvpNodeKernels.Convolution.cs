using Editor.Domain.Imaging;

namespace Editor.Imaging;

public static partial class MvpNodeKernels
{
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
}
