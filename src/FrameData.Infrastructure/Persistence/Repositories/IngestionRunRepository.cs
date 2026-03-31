using FrameData.Domain.Ingestion;

namespace FrameData.Infrastructure.Persistence.Repositories;

public sealed class IngestionRunRepository
{
    private readonly Dictionary<string, IngestionRun> _runs = new(StringComparer.OrdinalIgnoreCase);

    public Task SaveAsync(IngestionRun run, CancellationToken cancellationToken = default)
    {
        _runs[run.Id] = run;
        return Task.CompletedTask;
    }

    public Task<IngestionRun?> GetByIdAsync(string runId, CancellationToken cancellationToken = default)
    {
        _runs.TryGetValue(runId, out var run);
        return Task.FromResult(run);
    }
}
