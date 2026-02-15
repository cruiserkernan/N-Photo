using Editor.Imaging;
using Editor.Tests.Common;
using Editor.Domain.Imaging;

namespace Editor.Imaging.Tests;

public class MvpNodeKernelsTests
{
    [Fact]
    public void GaussianBlur_IsDeterministic()
    {
        var input = TestImageFactory.CreateGradient(20, 20);

        var first = MvpNodeKernels.GaussianBlur(input, radius: 3);
        var second = MvpNodeKernels.GaussianBlur(input, radius: 3);

        Assert.Equal(first.ToRgba8(), second.ToRgba8());
    }

    [Fact]
    public void Transform_PreservesOutputDimensions()
    {
        var input = TestImageFactory.CreateGradient(21, 13);
        var output = MvpNodeKernels.Transform(input, scale: 1.1f, rotateDegrees: 15f);

        Assert.Equal(21, output.Width);
        Assert.Equal(13, output.Height);
    }

    [Fact]
    public void ExposureContrast_PreservesSub8BitPrecisionInPipeline()
    {
        var input = new RgbaImage(1, 1);
        input.SetPixel(0, 0, new RgbaColor(0.5f, 0.5f, 0.5f, 1.0f));

        var output = MvpNodeKernels.ExposureContrast(input, exposure: 0.001f, contrast: 1.0f);
        var delta = output.GetPixel(0, 0).R - 0.5f;

        Assert.InRange(delta, 0.0001f, 0.001f);
    }

    [Fact]
    public void ApplyMask_BlendsBetweenOriginalAndProcessedByMaskAlpha()
    {
        var original = new RgbaImage(1, 1);
        original.SetPixel(0, 0, new RgbaColor(0.2f, 0.2f, 0.2f, 1.0f));

        var processed = new RgbaImage(1, 1);
        processed.SetPixel(0, 0, new RgbaColor(0.8f, 0.8f, 0.8f, 1.0f));

        var mask = new RgbaImage(1, 1);
        mask.SetPixel(0, 0, new RgbaColor(0.0f, 0.0f, 0.0f, 0.5f));

        var output = MvpNodeKernels.ApplyMask(original, processed, mask);
        var pixel = output.GetPixel(0, 0);

        Assert.Equal(0.5f, pixel.R, 3);
        Assert.Equal(0.5f, pixel.G, 3);
        Assert.Equal(0.5f, pixel.B, 3);
        Assert.Equal(1.0f, pixel.A, 3);
    }

    [Fact]
    public void ApplyMask_UsesLuminanceWhenAlphaIsZero()
    {
        var original = new RgbaImage(1, 1);
        original.SetPixel(0, 0, new RgbaColor(0.1f, 0.1f, 0.1f, 1.0f));

        var processed = new RgbaImage(1, 1);
        processed.SetPixel(0, 0, new RgbaColor(0.9f, 0.9f, 0.9f, 1.0f));

        var mask = new RgbaImage(1, 1);
        mask.SetPixel(0, 0, new RgbaColor(1.0f, 1.0f, 1.0f, 0.0f));

        var output = MvpNodeKernels.ApplyMask(original, processed, mask);
        var pixel = output.GetPixel(0, 0);

        Assert.Equal(0.9f, pixel.R, 3);
        Assert.Equal(0.9f, pixel.G, 3);
        Assert.Equal(0.9f, pixel.B, 3);
    }
}
