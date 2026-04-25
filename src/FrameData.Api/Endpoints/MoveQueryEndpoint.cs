using FrameData.Domain.MoveLookup;
using FrameData.Shared.Contracts;
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
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("FrameData.Api.MoveQuery");
            logger.LogInformation(
                "Move query received for character {Character} and move input {MoveInput}.",
                character,
                moveInput);

            var result = await lookupService.LookupAsync(character, moveInput, cancellationToken);
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
                "Move query matched {CharacterId}/{MoveId}: {CharacterName} {MoveName} in section {Section}.",
                result.Move.CharacterId,
                result.Move.Id,
                result.Move.CharacterName,
                result.Move.CanonicalName,
                result.Move.Section);

            return Results.Ok(new MoveQueryResponse
            {
                Character = result.Move.CharacterName,
                MatchedMove = result.Move.CanonicalName,
                Section = result.Move.Section,
                MatchedBy = "Exact",
                FrameData = new FrameDataContract
                {
                    Startup = result.Move.FrameData.Startup,
                    Active = result.Move.FrameData.Active,
                    Recovery = result.Move.FrameData.Recovery,
                    OnHit = result.Move.FrameData.OnHit,
                    OnBlock = result.Move.FrameData.OnBlock,
                    FrameAdvantage = result.Move.FrameData.FrameAdvantage
                }
            });
        });
    }
}
