namespace Editor.Domain.Imaging;

public readonly record struct RgbaColor(float R, float G, float B, float A)
{
    public static RgbaColor Clamp(RgbaColor color)
    {
        return new(
            Clamp01(color.R),
            Clamp01(color.G),
            Clamp01(color.B),
            Clamp01(color.A));
    }

    public static float Clamp01(float value)
    {
        return value switch
        {
            < 0.0f => 0.0f,
            > 1.0f => 1.0f,
            _ => value
        };
    }
}
