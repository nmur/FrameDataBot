using FrameData.Domain.Ingestion;
using FrameData.Infrastructure.Persistence.Repositories;
using FrameData.Ingestion.Services;
using FrameData.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FrameData.Api.Endpoints;

public static class IngestionEndpoints
{
    public static void MapIngestionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/ingestion/runs", async (
            IngestionRunRepository runRepository,
            IngestionOrchestrator orchestrator,
            CancellationToken cancellationToken) =>
        {
            var run = new IngestionRun
            {
                Id = Guid.NewGuid().ToString("N"),
                StartedAt = DateTimeOffset.UtcNow,
                Status = "Running"
            };

            await runRepository.SaveAsync(run, cancellationToken);

            var defaultScope = new[]
            {
                new IngestionCharacterScope
                {
                    CharacterId = "makoto",
                    CharacterName = "makoto",
                    SourceCharacterId = 1
                }
            };

            _ = Task.Run(async () =>
            {
                try
                {
                    await orchestrator.ExecuteRunAsync(run, defaultScope, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    run.CompletedAt = DateTimeOffset.UtcNow;
                    run.Status = "Failed";
                    run.Errors.Add($"runtime: {ex.Message}");
                    await runRepository.SaveAsync(run, CancellationToken.None);
                }
            });

            return Results.Accepted($"/v1/ingestion/runs/{run.Id}", new IngestionAcceptedResponse
            {
                RunId = run.Id,
                Status = "Running"
            });
        });

        app.MapGet("/v1/ingestion/runs/{runId}", async (
            [FromRoute] string runId,
            IngestionRunRepository runRepository,
            CancellationToken cancellationToken) =>
        {
            var run = await runRepository.GetByIdAsync(runId, cancellationToken);
            if (run is null)
            {
                return Results.NotFound(new ErrorResponse
                {
                    Code = "run_not_found",
                    Message = $"Run not found: {runId}"
                });
            }

            return Results.Ok(new IngestionRunResponse
            {
                RunId = run.Id,
                Status = run.Status,
                StartedAt = run.StartedAt,
                CompletedAt = run.CompletedAt,
                CharactersProcessed = run.CharactersProcessed,
                MovesProcessed = run.MovesProcessed,
                Errors = run.Errors
            });
        });
    }
}
