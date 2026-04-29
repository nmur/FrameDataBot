using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using FrameData.Domain.Media;

namespace FrameData.Ingestion.Media;

public sealed class HitboxCanvasRenderer
{
    private const int CanvasWidth = 384;
    private const int CanvasHeight = 224;
    private const int BytesPerPixel = 4;

    public IReadOnlyList<HitboxRectangle> GetRenderableHitboxes(
        HitboxFrame frame,
        IReadOnlyCollection<string> overlayHitboxes)
        => frame.Hitboxes
            .Where(hitbox => HitboxOverlayTypes.ShouldRender(hitbox.Type, overlayHitboxes))
            .ToArray();

    public byte[] RenderPng(HitboxFrame frame, IReadOnlyCollection<string> overlayHitboxes)
    {
        var pixels = CreateCanvas(245, 245, 245, 255);
        foreach (var hitbox in GetRenderableHitboxes(frame, overlayHitboxes))
        {
            DrawRectangle(pixels, hitbox, ColorFor(hitbox.Type));
        }

        return EncodePng(pixels, CanvasWidth, CanvasHeight);
    }

    public byte[] RenderDummyPng()
    {
        var pixels = CreateCanvas(238, 238, 238, 255);
        DrawBorder(pixels, 0, 0, CanvasWidth, CanvasHeight, new Rgba(140, 140, 140, 255), thickness: 3);
        return EncodePng(pixels, CanvasWidth, CanvasHeight);
    }

    private static byte[] CreateCanvas(byte red, byte green, byte blue, byte alpha)
    {
        var pixels = new byte[CanvasWidth * CanvasHeight * BytesPerPixel];
        for (var i = 0; i < pixels.Length; i += BytesPerPixel)
        {
            pixels[i] = red;
            pixels[i + 1] = green;
            pixels[i + 2] = blue;
            pixels[i + 3] = alpha;
        }

        return pixels;
    }

    private static void DrawRectangle(byte[] pixels, HitboxRectangle hitbox, Rgba color)
    {
        if (hitbox.Width <= 0 || hitbox.Height <= 0)
        {
            return;
        }

        DrawBorder(pixels, hitbox.X, hitbox.Y, hitbox.Width, hitbox.Height, color, thickness: 2);
    }

    private static void DrawBorder(byte[] pixels, int x, int y, int width, int height, Rgba color, int thickness)
    {
        var left = Math.Clamp(x, 0, CanvasWidth - 1);
        var top = Math.Clamp(y, 0, CanvasHeight - 1);
        var right = Math.Clamp(x + width - 1, 0, CanvasWidth - 1);
        var bottom = Math.Clamp(y + height - 1, 0, CanvasHeight - 1);

        for (var offset = 0; offset < thickness; offset++)
        {
            DrawHorizontalLine(pixels, left, right, top + offset, color);
            DrawHorizontalLine(pixels, left, right, bottom - offset, color);
            DrawVerticalLine(pixels, top, bottom, left + offset, color);
            DrawVerticalLine(pixels, top, bottom, right - offset, color);
        }
    }

    private static void DrawHorizontalLine(byte[] pixels, int left, int right, int y, Rgba color)
    {
        if (y < 0 || y >= CanvasHeight)
        {
            return;
        }

        for (var x = left; x <= right; x++)
        {
            SetPixel(pixels, x, y, color);
        }
    }

    private static void DrawVerticalLine(byte[] pixels, int top, int bottom, int x, Rgba color)
    {
        if (x < 0 || x >= CanvasWidth)
        {
            return;
        }

        for (var y = top; y <= bottom; y++)
        {
            SetPixel(pixels, x, y, color);
        }
    }

    private static void SetPixel(byte[] pixels, int x, int y, Rgba color)
    {
        var offset = ((y * CanvasWidth) + x) * BytesPerPixel;
        pixels[offset] = color.Red;
        pixels[offset + 1] = color.Green;
        pixels[offset + 2] = color.Blue;
        pixels[offset + 3] = color.Alpha;
    }

    private static Rgba ColorFor(string type)
    {
        return HitboxOverlayTypes.Normalize(type) switch
        {
            "P1_P" => new Rgba(241, 196, 15, 255),
            "P1_V" => new Rgba(52, 152, 219, 255),
            "P1_A" => new Rgba(231, 76, 60, 255),
            "P1_T" => new Rgba(46, 204, 113, 255),
            "P1_TA" => new Rgba(155, 89, 182, 255),
            _ => new Rgba(231, 76, 60, 255)
        };
    }

    private static byte[] EncodePng(byte[] rgbaPixels, int width, int height)
    {
        using var output = new MemoryStream();
        output.Write([137, 80, 78, 71, 13, 10, 26, 10]);

        Span<byte> ihdr = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr[..4], width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr[4..8], height);
        ihdr[8] = 8;
        ihdr[9] = 6;
        WriteChunk(output, "IHDR", ihdr);

        using var raw = new MemoryStream();
        var stride = width * BytesPerPixel;
        for (var y = 0; y < height; y++)
        {
            raw.WriteByte(0);
            raw.Write(rgbaPixels.AsSpan(y * stride, stride));
        }

        raw.Position = 0;
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
        {
            raw.CopyTo(zlib);
        }

        WriteChunk(output, "IDAT", compressed.ToArray());
        WriteChunk(output, "IEND", []);
        return output.ToArray();
    }

    private static void WriteChunk(Stream output, string chunkType, ReadOnlySpan<byte> data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        output.Write(length);

        var typeBytes = Encoding.ASCII.GetBytes(chunkType);
        output.Write(typeBytes);
        output.Write(data);

        var crc = Crc32(typeBytes, data);
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        output.Write(crcBytes);
    }

    private static uint Crc32(ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        var crc = 0xffffffffu;
        crc = UpdateCrc(crc, type);
        crc = UpdateCrc(crc, data);
        return crc ^ 0xffffffffu;
    }

    private static uint UpdateCrc(uint crc, ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes)
        {
            crc ^= value;
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 1) == 1 ? (crc >> 1) ^ 0xedb88320u : crc >> 1;
            }
        }

        return crc;
    }

    private readonly record struct Rgba(byte Red, byte Green, byte Blue, byte Alpha);
}
