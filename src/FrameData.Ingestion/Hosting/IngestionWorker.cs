using FrameData.Ingestion.Catalog;
using FrameData.Ingestion.Services;
using Microsoft.Extensions.Logging;

namespace FrameData.Ingestion.Hosting;

public sealed class IngestionWorker
{
    private readonly IngestionWorkerOptions _options;
    private readonly ISupportedCharacterCatalog _catalog;
    private readonly IngestionOrchestrator _orchestrator;
    private readonly ILogger<IngestionWorker> _logger;

    public IngestionWorker(
        IngestionWorkerOptions options,
        ISupportedCharacterCatalog catalog,
        IngestionOrchestrator orchestrator,
        ILogger<IngestionWorker> logger)
    {
        _options = options;
        _catalog = catalog;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var scope = _catalog.ResolveScope(_options.CharacterIds);
        _logger.LogInformation("Starting ingestion for {CharacterCount} character(s).", scope.Count);

        var run = await _orchestrator.RunAsync(scope, cancellationToken);
        _logger.LogInformation(
            "Ingestion run {RunId} finished with status {Status}. Characters: {CharactersProcessed}; moves: {MovesProcessed}.",
            run.Id,
            run.Status,
            run.CharactersProcessed,
            run.MovesProcessed);

        return IngestionWorkerExitCodeMapper.MapRunStatus(run);
    }
}
