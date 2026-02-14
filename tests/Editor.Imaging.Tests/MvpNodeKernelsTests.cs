using Editor.Imaging;
using Editor.Tests.Common;

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
}
