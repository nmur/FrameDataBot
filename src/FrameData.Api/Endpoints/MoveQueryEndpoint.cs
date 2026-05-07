using System.Diagnostics;
using FrameData.Domain.MoveLookup;
using FrameData.Domain.Media;
using FrameData.Api.Responses;
using FrameData.Shared.Contracts;
using FrameData.Shared.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FrameData.Api.Endpoints;

public static class MoveQueryEndpoint
{
    public static void MapMoveQueryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/moves/query", async (
            [FromQuery] string character,
            [FromQuery] string moveInput,
            ExactMoveLookupService lookupService,
            MoveDisambiguationResponseFactory disambiguationResponseFactory,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("FrameData.Api.MoveQuery");
            var started = Stopwatch.GetTimestamp();
            logger.LogInformation(
                "Move query received for character {Character} and move input {MoveInput}.",
                character,
                moveInput);

            var result = await lookupService.LookupAsync(character, moveInput, cancellationToken);
            if (result.IsAmbiguous)
            {
                logger.LogInformation(
                    "Move query for character {Character} and move input {MoveInput} returned {CandidateCount} ambiguous candidate(s).",
                    character,
                    moveInput,
                    result.Candidates.Count);
                LogMoveQueryCompletedMetric(
                    logger,
                    character,
                    moveInput,
                    result.ErrorCode ?? "ambiguous_move",
                    null,
                    null,
                    null,
                    result.Candidates.Count,
                    result.ErrorCode,
                    false,
                    null,
                    Stopwatch.GetElapsedTime(started));

                return Results.Json(
                    disambiguationResponseFactory.Create(result.Candidates),
                    statusCode: StatusCodes.Status300MultipleChoices);
            }

            if (!result.IsFound || result.Move is null)
            {
                logger.LogInformation(
                    "Move query for character {Character} and move input {MoveInput} returned {ErrorCode}.",
                    character,
                    moveInput,
                    result.ErrorCode ?? "not_found");
                LogMoveQueryCompletedMetric(
                    logger,
                    character,
                    moveInput,
                    result.ErrorCode ?? "not_found",
                    null,
                    null,
                    null,
                    0,
                    result.ErrorCode ?? "not_found",
                    false,
                    null,
                    Stopwatch.GetElapsedTime(started));

                return Results.NotFound(new ErrorResponse
                {
                    Code = result.ErrorCode ?? "not_found",
                    Message = result.ErrorMessage ?? "Move not found"
                });
            }

            logger.LogDebug(
                "Move query matched {CharacterId}/{MoveId}: {CharacterName} {MoveName} in section {Section}. MediaCount={MediaCount}.",
                result.Move.CharacterId,
                result.Move.Id,
                result.Move.CharacterName,
                result.Move.CanonicalName,
                result.Move.Section,
                result.Move.Media.Count);

            var media = ToMediaContract(result.Move.Media);
            LogMoveQueryCompletedMetric(
                logger,
                character,
                moveInput,
                "ok",
                result.Move.CharacterName,
                result.Move.CanonicalName,
                result.MatchedBy ?? "Exact",
                1,
                null,
                media is not null,
                media?.CaptureStatus,
                Stopwatch.GetElapsedTime(started));

            return Results.Ok(new MoveQueryResponse
            {
                Character = result.Move.CharacterName,
                MatchedMove = result.Move.CanonicalName,
                Section = result.Move.Section,
                MatchedBy = result.MatchedBy ?? "Exact",
                Motion = result.Move.Motion,
                Damage = result.Move.Damage,
                Stun = result.Move.Stun,
                CharacterFrameDataUrl = BuildCharacterFrameDataUrl(
                    result.Move.SourceBaseUrl,
                    result.Move.SourceCharacterId),
                MoveHitboxDisplayUrl = ResolveSourceUrl(
                    result.Move.SourceBaseUrl,
                    result.Move.SourceHitboxPath),
                GameRestaurantMoveUrl = GameRestaurantUrlResolver.Resolve(result.Move),
                FrameData = new FrameDataContract
                {
                    Startup = result.Move.FrameData.Startup,
                    Active = result.Move.FrameData.Active,
                    Recovery = result.Move.FrameData.Recovery,
                    OnHit = result.Move.FrameData.OnHit,
                    OnBlock = result.Move.FrameData.OnBlock,
                    OnCrouchingHit = result.Move.FrameData.OnCrouchingHit
                },
                Media = media
            });
        });
    }

    private static void LogMoveQueryCompletedMetric(
        ILogger logger,
        string character,
        string moveInput,
        string resultCode,
        string? matchedCharacter,
        string? matchedMove,
        string? matchedBy,
        int candidateCount,
        string? errorCode,
        bool hasMedia,
        string? mediaCaptureStatus,
        TimeSpan elapsed)
    {
        logger.LogInformation(
            "Metric {MetricName}: move query completed. ResultCode={ResultCode}; Character={Character}; MoveInput={MoveInput}; MatchedCharacter={MatchedCharacter}; MatchedMove={MatchedMove}; MatchedBy={MatchedBy}; CandidateCount={CandidateCount}; ErrorCode={ErrorCode}; HasMedia={HasMedia}; MediaCaptureStatus={MediaCaptureStatus}; ElapsedMs={ElapsedMs:0.000}.",
            FrameDataMetricNames.MoveQueryCompleted,
            resultCode,
            character,
            moveInput,
            matchedCharacter,
            matchedMove,
            matchedBy,
            candidateCount,
            errorCode,
            hasMedia,
            mediaCaptureStatus,
            elapsed.TotalMilliseconds);
    }

    private static MoveMediaContract? ToMediaContract(IReadOnlyList<MoveImage> media)
    {
        var representative = media.FirstOrDefault(image => image.ImageType == MoveImageType.RepresentativeActiveFrame);
        if (representative is null)
        {
            return null;
        }

        return new MoveMediaContract
        {
            RepresentativeFrameImageUrl = representative.StoragePath,
            SelectedFrame = representative.SelectedFrame,
            SelectionStrategy = representative.SelectionStrategy,
            CaptureStatus = representative.CaptureStatus.ToString(),
            FallbackReason = representative.FallbackReason
        };
    }

    private static string? BuildCharacterFrameDataUrl(string? sourceBaseUrl, int? sourceCharacterId)
    {
        if (string.IsNullOrWhiteSpace(sourceBaseUrl) || sourceCharacterId is null)
        {
            return null;
        }

        if (!Uri.TryCreate(sourceBaseUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var separator = string.IsNullOrEmpty(uri.Query) ? "?" : "&";
        return $"{uri.GetLeftPart(UriPartial.Path)}{uri.Query}{separator}id={sourceCharacterId.Value}";
    }

    private static string? ResolveSourceUrl(string? sourceBaseUrl, string? sourcePathOrUrl)
    {
        if (string.IsNullOrWhiteSpace(sourcePathOrUrl))
        {
            return null;
        }

        if (Uri.TryCreate(sourcePathOrUrl, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        if (string.IsNullOrWhiteSpace(sourceBaseUrl)
            || !Uri.TryCreate(sourceBaseUrl, UriKind.Absolute, out var baseUri))
        {
            return null;
        }

        var resolved = sourcePathOrUrl.StartsWith("?", StringComparison.Ordinal)
            ? new Uri($"{baseUri.GetLeftPart(UriPartial.Path)}{sourcePathOrUrl}")
            : new Uri(baseUri, sourcePathOrUrl);

        return resolved.ToString();
    }
}
