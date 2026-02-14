namespace Editor.Domain.Imaging;

public sealed class RgbaImage
{
    private readonly float[] _pixels;

    public RgbaImage(int width, int height)
        : this(width, height, null)
    {
    }

    public RgbaImage(int width, int height, float[]? pixels)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Width = width;
        Height = height;
        _pixels = pixels is null
            ? new float[width * height * 4]
            : ValidateAndClone(pixels, width, height);
    }

    public int Width { get; }

    public int Height { get; }

    public ReadOnlySpan<float> Pixels => _pixels;

    public RgbaImage Clone() => new(Width, Height, _pixels);

    public RgbaColor GetPixel(int x, int y)
    {
        var index = GetIndex(x, y);
        return new RgbaColor(
            _pixels[index],
            _pixels[index + 1],
            _pixels[index + 2],
            _pixels[index + 3]);
    }

    public void SetPixel(int x, int y, RgbaColor color)
    {
        var clamped = RgbaColor.Clamp(color);
        var index = GetIndex(x, y);
        _pixels[index] = clamped.R;
        _pixels[index + 1] = clamped.G;
        _pixels[index + 2] = clamped.B;
        _pixels[index + 3] = clamped.A;
    }

    public byte[] ToRgba8()
    {
        var bytes = new byte[_pixels.Length];
        for (var i = 0; i < _pixels.Length; i++)
        {
            bytes[i] = (byte)Math.Round(RgbaColor.Clamp01(_pixels[i]) * 255.0f);
        }

        return bytes;
    }

    public static RgbaImage FromRgba8(int width, int height, ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != width * height * 4)
        {
            throw new ArgumentException("Invalid byte length for RGBA8 buffer.", nameof(bytes));
        }

        var pixels = new float[bytes.Length];
        for (var i = 0; i < bytes.Length; i++)
        {
            pixels[i] = bytes[i] / 255.0f;
        }

        return new RgbaImage(width, height, pixels);
    }

    private int GetIndex(int x, int y)
    {
        if (x < 0 || x >= Width)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if (y < 0 || y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }

        return (y * Width + x) * 4;
    }

    private static float[] ValidateAndClone(float[] pixels, int width, int height)
    {
        if (pixels.Length != width * height * 4)
        {
            throw new ArgumentException("Invalid pixel count for RGBA image.", nameof(pixels));
        }

        var clone = new float[pixels.Length];
        Array.Copy(pixels, clone, pixels.Length);
        return clone;
    }
}
