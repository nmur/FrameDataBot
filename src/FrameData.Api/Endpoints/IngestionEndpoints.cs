using FrameData.Domain.Ingestion;
using FrameData.Infrastructure.Persistence.Repositories;
using FrameData.Ingestion.Catalog;
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
            ISupportedCharacterCatalog catalog,
            HttpRequest httpRequest,
            CancellationToken cancellationToken) =>
        {
            var request = await ReadRequestAsync(httpRequest, cancellationToken);
            var requestedCharacters = request?.CharacterIds ?? [];
            IReadOnlyList<IngestionCharacterScope> scope;
            try
            {
                scope = catalog.ResolveScope(requestedCharacters);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Code = "unsupported_ingestion_scope",
                    Message = ex.Message
                });
            }

            var run = await orchestrator.CreateRunAsync(cancellationToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    await orchestrator.ExecuteRunAsync(run, scope, CancellationToken.None);
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
                Status = "Running",
                Scope = requestedCharacters.Count == 0 ? "FullCatalog" : "Scoped",
                CharactersQueued = scope.Count
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
                Errors = run.Errors,
                CharacterStatuses = run.CharacterStatuses.Select(status => new IngestionRunCharacterStatusContract
                {
                    CharacterId = status.CharacterId,
                    SourceCharacterId = status.SourceCharacterId,
                    Status = status.Status,
                    MovesProcessed = status.MovesProcessed,
                    Error = status.Error
                }).ToArray()
            });
        });
    }

    private static async Task<IngestionRunRequest?> ReadRequestAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength is null or 0)
        {
            return null;
        }

        return await request.ReadFromJsonAsync<IngestionRunRequest>(cancellationToken: cancellationToken);
    }
}
