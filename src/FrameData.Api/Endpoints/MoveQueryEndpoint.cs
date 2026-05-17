using FrameData.Domain.MoveLookup;
using FrameData.Domain.Media;
using FrameData.Api.Responses;
using FrameData.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FrameData.Api.Endpoints;

public static class MoveQueryEndpoint
{
    private const string PublicSourceBaseUrl = "http://baston.esn3s.com";

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
            return Results.Ok(new MoveQueryResponse
            {
                Character = result.Move.CharacterName,
                MatchedMove = result.Move.CanonicalName,
                Section = result.Move.Section,
                MatchedBy = result.MatchedBy ?? "Exact",
                Motion = result.Move.Motion,
                Damage = result.Move.Damage,
                Stun = result.Move.Stun,
                CharacterFrameDataUrl = BuildCharacterFrameDataUrl(result.Move.SourceCharacterId),
                MoveHitboxDisplayUrl = BuildHitboxViewerUrl(result.Move.SourceHitboxPath),
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

    private static string? BuildCharacterFrameDataUrl(int? sourceCharacterId)
        => sourceCharacterId is null
            ? null
            : $"{PublicSourceBaseUrl}/index.php?id={sourceCharacterId.Value}";

    private static string? BuildHitboxViewerUrl(string? sourcePathOrUrl)
    {
        if (string.IsNullOrWhiteSpace(sourcePathOrUrl))
        {
            return null;
        }

        var query = ExtractQuery(sourcePathOrUrl);
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var sourceCharacterId = ReadQueryParameter(query, "iChar");
        var sourceMoveType = ReadQueryParameter(query, "sMoveType");
        var sourceMoveId = ReadQueryParameter(query, "iMove");
        if (string.IsNullOrWhiteSpace(sourceCharacterId)
            || string.IsNullOrWhiteSpace(sourceMoveType)
            || string.IsNullOrWhiteSpace(sourceMoveId))
        {
            return null;
        }

        return $"{PublicSourceBaseUrl}/hitboxesDisplay_spritesheet.php"
            + $"?iChar={Uri.EscapeDataString(sourceCharacterId)}"
            + $"&sMoveType={Uri.EscapeDataString(sourceMoveType)}"
            + $"&iMove={Uri.EscapeDataString(sourceMoveId)}";
    }

    private static string? ExtractQuery(string sourcePathOrUrl)
    {
        if (Uri.TryCreate(sourcePathOrUrl, UriKind.Absolute, out var uri))
        {
            return uri.Query.TrimStart('?');
        }

        var questionMarkIndex = sourcePathOrUrl.IndexOf('?', StringComparison.Ordinal);
        return questionMarkIndex < 0 || questionMarkIndex == sourcePathOrUrl.Length - 1
            ? null
            : sourcePathOrUrl[(questionMarkIndex + 1)..];
    }

    private static string? ReadQueryParameter(string query, string name)
    {
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var equalsIndex = pair.IndexOf('=', StringComparison.Ordinal);
            var key = equalsIndex < 0 ? pair : pair[..equalsIndex];
            if (!string.Equals(Uri.UnescapeDataString(key), name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = equalsIndex < 0 ? string.Empty : pair[(equalsIndex + 1)..];
            return Uri.UnescapeDataString(value.Replace('+', ' '));
        }

        return null;
    }
}
