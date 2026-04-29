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
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    public IReadOnlyList<HitboxRectangle> GetRenderableHitboxes(
        HitboxFrame frame,
        IReadOnlyCollection<string> overlayHitboxes)
        => frame.Hitboxes
            .Where(hitbox => HitboxOverlayTypes.ShouldRender(hitbox.Type, overlayHitboxes))
            .ToArray();

    public byte[] RenderPng(HitboxFrame frame, IReadOnlyCollection<string> overlayHitboxes)
        => RenderPng(frame, overlayHitboxes, sourceFrame: null);

    public byte[] RenderPng(
        HitboxFrame frame,
        IReadOnlyCollection<string> overlayHitboxes,
        DecodedPngImage? sourceFrame)
    {
        var pixels = sourceFrame is null
            ? CreateCanvas(245, 245, 245, 255)
            : CreateCanvas(0, 0, 0, 255);

        if (sourceFrame is not null)
        {
            CompositeSourceFrame(pixels, sourceFrame);
        }

        foreach (var hitbox in GetRenderableHitboxes(frame, overlayHitboxes))
        {
            DrawRectangle(pixels, hitbox, StyleFor(hitbox.Type));
        }

        return EncodePng(pixels, CanvasWidth, CanvasHeight);
    }

    public byte[] RenderDummyPng()
    {
        var pixels = CreateCanvas(238, 238, 238, 255);
        DrawBorder(pixels, 0, 0, CanvasWidth, CanvasHeight, new Rgba(140, 140, 140, 255), thickness: 3);
        return EncodePng(pixels, CanvasWidth, CanvasHeight);
    }

    public bool TryDecodePng(byte[] content, out DecodedPngImage? image, out string? error)
    {
        try
        {
            image = DecodePng(content);
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or NotSupportedException)
        {
            image = null;
            error = ex.Message;
            return false;
        }
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

    private static void CompositeSourceFrame(byte[] pixels, DecodedPngImage sourceFrame)
    {
        var width = Math.Min(CanvasWidth, sourceFrame.Width);
        var height = Math.Min(CanvasHeight, sourceFrame.Height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var sourceOffset = ((y * sourceFrame.Width) + x) * BytesPerPixel;
                var red = sourceFrame.Pixels[sourceOffset];
                var green = sourceFrame.Pixels[sourceOffset + 1];
                var blue = sourceFrame.Pixels[sourceOffset + 2];
                var alpha = sourceFrame.Pixels[sourceOffset + 3];
                if (alpha == 0 || IsSourceFrameTransparencyKey(red, green, blue))
                {
                    continue;
                }

                var destinationOffset = ((y * CanvasWidth) + x) * BytesPerPixel;
                if (alpha == 255)
                {
                    pixels[destinationOffset] = red;
                    pixels[destinationOffset + 1] = green;
                    pixels[destinationOffset + 2] = blue;
                    pixels[destinationOffset + 3] = 255;
                    continue;
                }

                pixels[destinationOffset] = Blend(red, alpha, pixels[destinationOffset]);
                pixels[destinationOffset + 1] = Blend(green, alpha, pixels[destinationOffset + 1]);
                pixels[destinationOffset + 2] = Blend(blue, alpha, pixels[destinationOffset + 2]);
                pixels[destinationOffset + 3] = 255;
            }
        }
    }

    private static bool IsSourceFrameTransparencyKey(byte red, byte green, byte blue)
        => red == 255 && green == 0 && blue == 255;

    private static byte Blend(byte source, byte alpha, byte destination)
        => (byte)(((source * alpha) + (destination * (255 - alpha))) / 255);

    private static void DrawRectangle(byte[] pixels, HitboxRectangle hitbox, HitboxStyle style)
    {
        if (hitbox.Width <= 0 || hitbox.Height <= 0)
        {
            return;
        }

        FillRectangle(pixels, hitbox.X, hitbox.Y, hitbox.Width, hitbox.Height, style.Fill);
        DrawBorder(pixels, hitbox.X, hitbox.Y, hitbox.Width, hitbox.Height, style.Border, thickness: 1);
    }

    private static void FillRectangle(byte[] pixels, int x, int y, int width, int height, Rgba color)
    {
        var left = Math.Clamp(x, 0, CanvasWidth - 1);
        var top = Math.Clamp(y, 0, CanvasHeight - 1);
        var right = Math.Clamp(x + width - 1, 0, CanvasWidth - 1);
        var bottom = Math.Clamp(y + height - 1, 0, CanvasHeight - 1);

        for (var row = top; row <= bottom; row++)
        {
            for (var column = left; column <= right; column++)
            {
                SetPixel(pixels, column, row, color);
            }
        }
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
        if (color.Alpha == 255)
        {
            pixels[offset] = color.Red;
            pixels[offset + 1] = color.Green;
            pixels[offset + 2] = color.Blue;
            pixels[offset + 3] = 255;
            return;
        }

        pixels[offset] = Blend(color.Red, color.Alpha, pixels[offset]);
        pixels[offset + 1] = Blend(color.Green, color.Alpha, pixels[offset + 1]);
        pixels[offset + 2] = Blend(color.Blue, color.Alpha, pixels[offset + 2]);
        pixels[offset + 3] = 255;
    }

    private static HitboxStyle StyleFor(string type)
    {
        var color = HitboxOverlayTypes.Normalize(type) switch
        {
            "P1_P" => new Rgba(0, 0, 160, 255),
            "P1_V" => new Rgba(0, 255, 255, 255),
            "P1_A" => new Rgba(255, 0, 0, 255),
            "P1_T" => new Rgba(255, 128, 0, 255),
            "P1_TA" => new Rgba(0, 192, 0, 255),
            _ => new Rgba(255, 0, 0, 255)
        };

        return new HitboxStyle(
            Border: color,
            Fill: color with { Alpha = 96 });
    }

    private static DecodedPngImage DecodePng(byte[] content)
    {
        if (content.Length < PngSignature.Length || !content.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature))
        {
            throw new InvalidDataException("Source frame image was not a PNG file.");
        }

        using var stream = new MemoryStream(content);
        stream.Position = 8;
        var idat = new MemoryStream();
        byte[]? palette = null;
        byte[]? transparency = null;
        var width = 0;
        var height = 0;
        byte bitDepth = 0;
        byte colorType = 0;

        while (stream.Position < stream.Length)
        {
            var length = ReadInt32BigEndian(stream);
            var chunkType = ReadAscii(stream, 4);
            var data = ReadBytes(stream, length);
            _ = ReadInt32BigEndian(stream);

            switch (chunkType)
            {
                case "IHDR":
                    if (data.Length != 13)
                    {
                        throw new InvalidDataException("PNG IHDR chunk had an invalid length.");
                    }

                    width = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0, 4));
                    height = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(4, 4));
                    bitDepth = data[8];
                    colorType = data[9];
                    if (data[10] != 0 || data[11] != 0 || data[12] != 0)
                    {
                        throw new NotSupportedException("Interlaced or non-deflate PNG source frame images are not supported.");
                    }

                    break;
                case "PLTE":
                    palette = data;
                    break;
                case "tRNS":
                    transparency = data;
                    break;
                case "IDAT":
                    idat.Write(data);
                    break;
                case "IEND":
                    stream.Position = stream.Length;
                    break;
            }
        }

        if (width <= 0 || height <= 0)
        {
            throw new InvalidDataException("PNG source frame image was missing a valid IHDR chunk.");
        }

        if (bitDepth != 8)
        {
            throw new NotSupportedException($"PNG source frame bit depth {bitDepth} is not supported.");
        }

        var channels = ChannelsForColorType(colorType);
        var scanlineLength = checked(width * channels);
        idat.Position = 0;
        using var zlib = new ZLibStream(idat, CompressionMode.Decompress);
        using var decompressed = new MemoryStream();
        zlib.CopyTo(decompressed);
        var unfiltered = Unfilter(decompressed.ToArray(), width, height, channels);
        var rgba = ToRgba(unfiltered, width, height, colorType, channels, palette, transparency);
        if (unfiltered.Length != checked(scanlineLength * height))
        {
            throw new InvalidDataException("PNG source frame image had an unexpected scanline length.");
        }

        return new DecodedPngImage(width, height, colorType, bitDepth, rgba);
    }

    private static int ChannelsForColorType(byte colorType)
        => colorType switch
        {
            0 => 1,
            2 => 3,
            3 => 1,
            4 => 2,
            6 => 4,
            _ => throw new NotSupportedException($"PNG source frame color type {colorType} is not supported.")
        };

    private static byte[] Unfilter(byte[] filtered, int width, int height, int channels)
    {
        var stride = checked(width * channels);
        var expectedLength = checked((stride + 1) * height);
        if (filtered.Length < expectedLength)
        {
            throw new InvalidDataException("PNG source frame image did not contain enough scanline data.");
        }

        var output = new byte[checked(stride * height)];
        var previous = new byte[stride];
        var current = new byte[stride];
        var sourceOffset = 0;
        var outputOffset = 0;

        for (var y = 0; y < height; y++)
        {
            var filter = filtered[sourceOffset++];
            Array.Copy(filtered, sourceOffset, current, 0, stride);
            sourceOffset += stride;

            for (var x = 0; x < stride; x++)
            {
                var left = x >= channels ? current[x - channels] : (byte)0;
                var up = previous[x];
                var upLeft = x >= channels ? previous[x - channels] : (byte)0;
                current[x] = filter switch
                {
                    0 => current[x],
                    1 => unchecked((byte)(current[x] + left)),
                    2 => unchecked((byte)(current[x] + up)),
                    3 => unchecked((byte)(current[x] + ((left + up) / 2))),
                    4 => unchecked((byte)(current[x] + Paeth(left, up, upLeft))),
                    _ => throw new InvalidDataException($"PNG source frame image used unsupported filter type {filter}.")
                };
            }

            Array.Copy(current, 0, output, outputOffset, stride);
            outputOffset += stride;
            (previous, current) = (current, previous);
        }

        return output;
    }

    private static byte Paeth(byte left, byte up, byte upLeft)
    {
        var prediction = left + up - upLeft;
        var distanceLeft = Math.Abs(prediction - left);
        var distanceUp = Math.Abs(prediction - up);
        var distanceUpLeft = Math.Abs(prediction - upLeft);

        if (distanceLeft <= distanceUp && distanceLeft <= distanceUpLeft)
        {
            return left;
        }

        return distanceUp <= distanceUpLeft ? up : upLeft;
    }

    private static byte[] ToRgba(
        byte[] source,
        int width,
        int height,
        byte colorType,
        int channels,
        byte[]? palette,
        byte[]? transparency)
    {
        if (colorType == 3 && palette is null)
        {
            throw new InvalidDataException("Palette PNG source frame image was missing a PLTE chunk.");
        }

        var rgba = new byte[checked(width * height * BytesPerPixel)];
        for (var pixel = 0; pixel < width * height; pixel++)
        {
            var sourceOffset = pixel * channels;
            var destinationOffset = pixel * BytesPerPixel;
            switch (colorType)
            {
                case 0:
                    rgba[destinationOffset] = source[sourceOffset];
                    rgba[destinationOffset + 1] = source[sourceOffset];
                    rgba[destinationOffset + 2] = source[sourceOffset];
                    rgba[destinationOffset + 3] = 255;
                    break;
                case 2:
                    rgba[destinationOffset] = source[sourceOffset];
                    rgba[destinationOffset + 1] = source[sourceOffset + 1];
                    rgba[destinationOffset + 2] = source[sourceOffset + 2];
                    rgba[destinationOffset + 3] = 255;
                    break;
                case 3:
                    var paletteIndex = source[sourceOffset];
                    var paletteOffset = paletteIndex * 3;
                    if (paletteOffset + 2 >= palette!.Length)
                    {
                        throw new InvalidDataException("Palette PNG source frame image referenced an invalid palette index.");
                    }

                    rgba[destinationOffset] = palette[paletteOffset];
                    rgba[destinationOffset + 1] = palette[paletteOffset + 1];
                    rgba[destinationOffset + 2] = palette[paletteOffset + 2];
                    rgba[destinationOffset + 3] = transparency is not null && paletteIndex < transparency.Length
                        ? transparency[paletteIndex]
                        : (byte)255;
                    break;
                case 4:
                    rgba[destinationOffset] = source[sourceOffset];
                    rgba[destinationOffset + 1] = source[sourceOffset];
                    rgba[destinationOffset + 2] = source[sourceOffset];
                    rgba[destinationOffset + 3] = source[sourceOffset + 1];
                    break;
                case 6:
                    rgba[destinationOffset] = source[sourceOffset];
                    rgba[destinationOffset + 1] = source[sourceOffset + 1];
                    rgba[destinationOffset + 2] = source[sourceOffset + 2];
                    rgba[destinationOffset + 3] = source[sourceOffset + 3];
                    break;
            }
        }

        return rgba;
    }

    private static int ReadInt32BigEndian(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[4];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadInt32BigEndian(buffer);
    }

    private static string ReadAscii(Stream stream, int length)
    {
        var buffer = ReadBytes(stream, length);
        return Encoding.ASCII.GetString(buffer);
    }

    private static byte[] ReadBytes(Stream stream, int length)
    {
        var buffer = new byte[length];
        stream.ReadExactly(buffer);
        return buffer;
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
    private readonly record struct HitboxStyle(Rgba Border, Rgba Fill);
}

public sealed record DecodedPngImage(
    int Width,
    int Height,
    byte ColorType,
    byte BitDepth,
    byte[] Pixels);
