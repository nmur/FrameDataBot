using FrameData.Domain.MoveLookup;
using FrameData.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FrameData.Api.Endpoints;

public static class MoveQueryEndpoint
{
    public static void MapMoveQueryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/moves/query", async (
            [FromQuery] string character,
            [FromQuery] string moveInput,
            [FromQuery] string? game,
            ExactMoveLookupService lookupService,
            CancellationToken cancellationToken) =>
        {
            var result = await lookupService.LookupAsync(game, character, moveInput, cancellationToken);
            if (!result.IsFound || result.Move is null)
            {
                return Results.NotFound(new ErrorResponse
                {
                    Code = result.ErrorCode ?? "not_found",
                    Message = result.ErrorMessage ?? "Move not found"
                });
            }

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
