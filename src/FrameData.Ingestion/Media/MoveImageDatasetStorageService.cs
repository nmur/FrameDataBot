using FrameData.Domain.Media;
using FrameData.Domain.Moves;
using FrameData.Scraper.Parsing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FrameData.Ingestion.Media;

public sealed class MoveImageDatasetStorageService
{
    private readonly HitboxDisplayParser _parser;
    private readonly RepresentativeFrameSelector _selector;
    private readonly HitboxCanvasRenderer _renderer;
    private readonly ILogger<MoveImageDatasetStorageService> _logger;

    public MoveImageDatasetStorageService(
        HitboxDisplayParser? parser = null,
        RepresentativeFrameSelector? selector = null,
        HitboxCanvasRenderer? renderer = null,
        ILogger<MoveImageDatasetStorageService>? logger = null)
    {
        _parser = parser ?? new HitboxDisplayParser();
        _selector = selector ?? new RepresentativeFrameSelector();
        _renderer = renderer ?? new HitboxCanvasRenderer();
        _logger = logger ?? NullLogger<MoveImageDatasetStorageService>.Instance;
    }

    public MoveImageDatasetAsset? CaptureRepresentativeImage(
        Move move,
        string? sourceUrl,
        string? hitboxDisplayHtml,
        RepresentativeFrameSelectionPolicy policy)
    {
        if (!policy.IsMoveInScope(move.CharacterId, move.Id))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(sourceUrl) || string.IsNullOrWhiteSpace(hitboxDisplayHtml))
        {
            return CreateDummyFallback(move, sourceUrl, policy, "Hitbox display page was not available.");
        }

        var frames = _parser.Parse(hitboxDisplayHtml);
        var selection = _selector.Select(frames, policy, policy.FindOverride(move.CharacterId, move.Id));
        if (selection is null)
        {
            return CreateDummyFallback(move, sourceUrl, policy, "Representative active frame could not be derived.");
        }

        if (string.IsNullOrWhiteSpace(selection.Frame.SourceFrameImageUrl))
        {
            return CreateDummyFallback(
                move,
                sourceUrl,
                policy,
                "Selected frame image was not available.",
                selection.Frame.FrameId,
                selection.ActiveHitboxArea);
        }

        var image = CreateImage(
            move,
            sourceUrl,
            MoveImageCaptureStatus.Success,
            selectedFrame: selection.Frame.FrameId,
            sourceFrameImageUrl: selection.Frame.SourceFrameImageUrl,
            selectionStrategy: selection.SelectionStrategy,
            activeHitboxArea: selection.ActiveHitboxArea,
            fallbackReason: null);

        _logger.LogInformation(
            "Captured representative frame {SelectedFrame} for {CharacterId}/{MoveId} with active hitbox area {ActiveHitboxArea}.",
            image.SelectedFrame,
            move.CharacterId,
            move.Id,
            image.ActiveHitboxArea);

        return new MoveImageDatasetAsset
        {
            Image = image,
            Content = _renderer.RenderPng(selection.Frame, image.OverlayHitboxes)
        };
    }

    public async Task SaveAsync(
        string datasetDirectory,
        MoveImageDatasetAsset asset,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(datasetDirectory))
        {
            throw new ArgumentException("Dataset directory is required.", nameof(datasetDirectory));
        }

        var fullPath = ResolveDatasetPath(datasetDirectory, asset.Image.StoragePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, asset.Content, cancellationToken);
    }

    public static string BuildStoragePath(Move move)
        => Path.Combine(
                "media",
                SanitizePathPart(move.CharacterId),
                SanitizePathPart(move.Id),
                "representative-active-frame.png")
            .Replace('\\', '/');

    private MoveImageDatasetAsset CreateDummyFallback(
        Move move,
        string? sourceUrl,
        RepresentativeFrameSelectionPolicy policy,
        string fallbackReason,
        string? selectedFrame = null,
        int? activeHitboxArea = null)
    {
        var image = CreateImage(
            move,
            sourceUrl ?? string.Empty,
            MoveImageCaptureStatus.DummyFallback,
            selectedFrame,
            sourceFrameImageUrl: null,
            selectionStrategy: RepresentativeFrameSelectionPolicy.LargestActiveHitboxAreaStrategy,
            activeHitboxArea: activeHitboxArea,
            fallbackReason: fallbackReason);

        var content = !string.IsNullOrWhiteSpace(policy.DummyImagePath)
            ? File.ReadAllBytes(policy.DummyImagePath)
            : _renderer.RenderDummyPng();

        _logger.LogInformation(
            "Stored dummy representative image for {CharacterId}/{MoveId}: {FallbackReason}.",
            move.CharacterId,
            move.Id,
            image.FallbackReason);

        return new MoveImageDatasetAsset
        {
            Image = image,
            Content = content
        };
    }

    private static MoveImage CreateImage(
        Move move,
        string sourceUrl,
        MoveImageCaptureStatus status,
        string? selectedFrame,
        string? sourceFrameImageUrl,
        string selectionStrategy,
        int? activeHitboxArea,
        string? fallbackReason)
        => new()
        {
            Id = $"{move.Id}:representative-active-frame",
            MoveId = move.Id,
            ImageType = MoveImageType.RepresentativeActiveFrame,
            StoragePath = BuildStoragePath(move),
            SourceUrl = sourceUrl,
            SourceFrameImageUrl = sourceFrameImageUrl,
            SelectedFrame = selectedFrame,
            SelectionStrategy = selectionStrategy,
            ActiveHitboxArea = activeHitboxArea,
            OverlayHitboxes = HitboxOverlayTypes.DefaultP1Overlays,
            FallbackReason = SanitizeReason(fallbackReason),
            CapturedAt = DateTimeOffset.UtcNow,
            CaptureStatus = status
        };

    private static string ResolveDatasetPath(string datasetDirectory, string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidDataException($"Move image storage path must be relative: {relativePath}");
        }

        var root = Path.GetFullPath(datasetDirectory);
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Move image storage path escapes the dataset directory: {relativePath}");
        }

        return fullPath;
    }

    private static string SanitizePathPart(string value)
        => new(value
            .Trim()
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? char.ToLowerInvariant(ch) : '-')
            .ToArray());

    private static string? SanitizeReason(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.ReplaceLineEndings(" ").Trim();
    }
}

public sealed class MoveImageDatasetAsset
{
    public required MoveImage Image { get; init; }
    public required byte[] Content { get; init; }
}
