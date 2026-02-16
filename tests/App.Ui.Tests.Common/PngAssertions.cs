namespace App.Ui.Tests.Common;

public static class PngAssertions
{
    public static void AssertValid(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Expected screenshot file was not found: {path}");
        }

        var fileInfo = new FileInfo(path);
        if (fileInfo.Length <= 0)
        {
            throw new InvalidOperationException($"Screenshot file is empty: {path}");
        }

        using var stream = File.OpenRead(path);
        if (stream.Length < 24)
        {
            throw new InvalidOperationException($"Screenshot file is too small to be a valid PNG: {path}");
        }

        Span<byte> header = stackalloc byte[24];
        var bytesRead = stream.Read(header);
        if (bytesRead != header.Length)
        {
            throw new InvalidOperationException($"Failed to read PNG header from: {path}");
        }

        var pngSignature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (!header[..8].SequenceEqual(pngSignature))
        {
            throw new InvalidOperationException($"Screenshot file is not a PNG: {path}");
        }

        var width = ReadBigEndianInt32(header[16..20]);
        var height = ReadBigEndianInt32(header[20..24]);
        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException($"Screenshot dimensions are invalid: {width}x{height}");
        }
    }

    private static int ReadBigEndianInt32(ReadOnlySpan<byte> bytes)
    {
        return (bytes[0] << 24) |
               (bytes[1] << 16) |
               (bytes[2] << 8) |
               bytes[3];
    }
}
