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
            _logger.LogDebug(
                "Representative image capture skipped for {CharacterId}/{MoveId} because the move is outside the configured pilot scope.",
                move.CharacterId,
                move.Id);
            return null;
        }

        _logger.LogDebug(
            "Starting representative image capture for {CharacterId}/{MoveId}. SourceUrl={SourceUrl}; HasHitboxHtml={HasHitboxHtml}; HitboxHtmlBytes={HitboxHtmlBytes}; DefaultStrategy={DefaultStrategy}.",
            move.CharacterId,
            move.Id,
            sourceUrl,
            !string.IsNullOrWhiteSpace(hitboxDisplayHtml),
            hitboxDisplayHtml?.Length ?? 0,
            policy.DefaultStrategy);

        if (string.IsNullOrWhiteSpace(sourceUrl) || string.IsNullOrWhiteSpace(hitboxDisplayHtml))
        {
            _logger.LogWarning(
                "Representative image capture for {CharacterId}/{MoveId} cannot parse source data. SourceUrlMissing={SourceUrlMissing}; HitboxHtmlMissing={HitboxHtmlMissing}; creating dummy fallback.",
                move.CharacterId,
                move.Id,
                string.IsNullOrWhiteSpace(sourceUrl),
                string.IsNullOrWhiteSpace(hitboxDisplayHtml));
            return CreateDummyFallback(move, sourceUrl, policy, "Hitbox display page was not available.");
        }

        var frames = _parser.Parse(hitboxDisplayHtml);
        var totalHitboxes = frames.Sum(frame => frame.Hitboxes.Count);
        var activeFrameCount = frames.Count(frame => frame.Hitboxes.Any(hitbox => HitboxOverlayTypes.IsActiveAreaHitbox(hitbox.Type)));
        _logger.LogInformation(
            "Parsed hitbox display page for {CharacterId}/{MoveId}. Frames={FrameCount}; Hitboxes={HitboxCount}; FramesWithActiveHitboxes={ActiveFrameCount}.",
            move.CharacterId,
            move.Id,
            frames.Count,
            totalHitboxes,
            activeFrameCount);

        var moveOverride = policy.FindOverride(move.CharacterId, move.Id);
        if (moveOverride is not null)
        {
            _logger.LogInformation(
                "Applying representative frame override for {CharacterId}/{MoveId}. SelectedFrame={OverrideSelectedFrame}; SelectionStrategy={OverrideSelectionStrategy}.",
                move.CharacterId,
                move.Id,
                moveOverride.SelectedFrame,
                moveOverride.SelectionStrategy);
        }

        var selection = _selector.Select(frames, policy, moveOverride);
        if (selection is null)
        {
            _logger.LogWarning(
                "No representative active frame could be selected for {CharacterId}/{MoveId}. Frames={FrameCount}; FramesWithActiveHitboxes={ActiveFrameCount}; creating dummy fallback.",
                move.CharacterId,
                move.Id,
                frames.Count,
                activeFrameCount);
            return CreateDummyFallback(move, sourceUrl, policy, "Representative active frame could not be derived.");
        }

        _logger.LogInformation(
            "Selected representative frame {SelectedFrame} for {CharacterId}/{MoveId}. Strategy={SelectionStrategy}; ActiveHitboxArea={ActiveHitboxArea}; SourceFrameImageUrl={SourceFrameImageUrl}.",
            selection.Frame.FrameId,
            move.CharacterId,
            move.Id,
            selection.SelectionStrategy,
            selection.ActiveHitboxArea,
            selection.Frame.SourceFrameImageUrl);

        if (string.IsNullOrWhiteSpace(selection.Frame.SourceFrameImageUrl))
        {
            _logger.LogWarning(
                "Selected frame {SelectedFrame} for {CharacterId}/{MoveId} has no source frame image URL; creating dummy fallback.",
                selection.Frame.FrameId,
                move.CharacterId,
                move.Id);
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
            "Rendering representative image for {CharacterId}/{MoveId}. SelectedFrame={SelectedFrame}; ActiveHitboxArea={ActiveHitboxArea}; OverlayHitboxes={OverlayHitboxes}; RenderableHitboxes={RenderableHitboxCount}; StoragePath={StoragePath}.",
            move.CharacterId,
            move.Id,
            image.SelectedFrame,
            image.ActiveHitboxArea,
            image.OverlayHitboxes,
            _renderer.GetRenderableHitboxes(selection.Frame, image.OverlayHitboxes).Count,
            image.StoragePath);

        var content = _renderer.RenderPng(selection.Frame, image.OverlayHitboxes);
        _logger.LogInformation(
            "Captured representative image for {CharacterId}/{MoveId}. SelectedFrame={SelectedFrame}; StoragePath={StoragePath}; Bytes={ByteCount}.",
            move.CharacterId,
            move.Id,
            image.SelectedFrame,
            image.StoragePath,
            content.Length);

        return new MoveImageDatasetAsset
        {
            Image = image,
            Content = content
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

        _logger.LogInformation(
            "Wrote representative media asset for {MoveId} to {StoragePath} ({FullPath}); Status={CaptureStatus}; Bytes={ByteCount}.",
            asset.Image.MoveId,
            asset.Image.StoragePath,
            fullPath,
            asset.Image.CaptureStatus,
            asset.Content.Length);
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

        byte[] content;
        if (!string.IsNullOrWhiteSpace(policy.DummyImagePath))
        {
            _logger.LogInformation(
                "Using configured dummy representative image for {CharacterId}/{MoveId} from {DummyImagePath}.",
                move.CharacterId,
                move.Id,
                policy.DummyImagePath);
            content = File.ReadAllBytes(policy.DummyImagePath);
        }
        else
        {
            _logger.LogInformation(
                "Generating dummy representative image for {CharacterId}/{MoveId}.",
                move.CharacterId,
                move.Id);
            content = _renderer.RenderDummyPng();
        }

        _logger.LogInformation(
            "Stored dummy representative image for {CharacterId}/{MoveId}. FallbackReason={FallbackReason}; SelectedFrame={SelectedFrame}; ActiveHitboxArea={ActiveHitboxArea}; StoragePath={StoragePath}; Bytes={ByteCount}.",
            move.CharacterId,
            move.Id,
            image.FallbackReason,
            image.SelectedFrame,
            image.ActiveHitboxArea,
            image.StoragePath,
            content.Length);

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
