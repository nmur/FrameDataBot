using FrameData.Domain.Characters;
using FrameData.Domain.Ingestion;
using FrameData.Domain.Moves;
using FrameData.Infrastructure.Persistence.Repositories;
using FrameData.Ingestion.Catalog;
using FrameData.Scraper.Parsing;
using FrameData.Scraper.Source;

namespace FrameData.Ingestion.Services;

public sealed class IngestionOrchestrator
{
    private const string CurrentGame = "sf3_3s";

    private readonly ISourceHttpClient _sourceClient;
    private readonly CharacterSectionParser _sectionParser;
    private readonly FrameDataDatasetRepository _datasetRepository;
    private readonly IngestionRunRepository _runRepository;
    private readonly CharacterExportWorkflow _exportWorkflow;
    private readonly ISupportedCharacterCatalog? _catalog;

    public IngestionOrchestrator(
        ISourceHttpClient sourceClient,
        CharacterSectionParser sectionParser,
        FrameDataDatasetRepository datasetRepository,
        IngestionRunRepository runRepository,
        CharacterExportWorkflow exportWorkflow,
        ISupportedCharacterCatalog? catalog = null)
    {
        _sourceClient = sourceClient;
        _sectionParser = sectionParser;
        _datasetRepository = datasetRepository;
        _runRepository = runRepository;
        _exportWorkflow = exportWorkflow;
        _catalog = catalog;
    }

    public Task<IngestionRun> RunAsync(CancellationToken cancellationToken = default)
    {
        if (_catalog is null)
        {
            throw new InvalidOperationException("Default ingestion requires a supported character catalog.");
        }

        return RunAsync(_catalog.ResolveScope([]), cancellationToken);
    }

    public async Task<IngestionRun> RunAsync(IReadOnlyCollection<IngestionCharacterScope> scope, CancellationToken cancellationToken = default)
    {
        var run = await CreateRunAsync(cancellationToken);
        return await ExecuteRunAsync(run, scope, cancellationToken);
    }

    public async Task<IngestionRun> CreateRunAsync(CancellationToken cancellationToken = default)
    {
        var run = new IngestionRun
        {
            Id = Guid.NewGuid().ToString("N"),
            StartedAt = DateTimeOffset.UtcNow,
            Status = "Running"
        };

        await _runRepository.SaveAsync(run, cancellationToken);
        return run;
    }

    public async Task<IngestionRun> ExecuteRunAsync(
        IngestionRun run,
        IReadOnlyCollection<IngestionCharacterScope> scope,
        CancellationToken cancellationToken = default)
    {
        if (scope.Count == 0)
        {
            throw new ArgumentException("Ingestion scope must include at least one character.", nameof(scope));
        }

        var replacementCharacters = new List<Character>();
        var replacementMoves = new List<Move>();

        foreach (var characterScope in scope)
        {
            try
            {
                var html = await _sourceClient.GetCharacterPageAsync(characterScope.SourceCharacterId, cancellationToken);
                var parsedMoves = _sectionParser.Parse(html);
                var domainMoves = parsedMoves
                    .Select((parsed, index) => MapMove(characterScope, parsed, index + 1))
                    .ToList();

                var character = new Character
                {
                    Id = characterScope.CharacterId,
                    Game = CurrentGame,
                    Name = characterScope.CharacterName,
                    SourceCharacterId = characterScope.SourceCharacterId,
                    DisplayOrder = characterScope.DisplayOrder,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    Aliases = characterScope.Aliases
                };

                await _exportWorkflow.ExportCharacterAsync(character, domainMoves, cancellationToken);
                replacementCharacters.Add(character);
                replacementMoves.AddRange(domainMoves);

                run.CharactersProcessed += 1;
                run.MovesProcessed += domainMoves.Count;
                run.CharacterStatuses.Add(new IngestionRunCharacterStatus
                {
                    CharacterId = characterScope.CharacterId,
                    SourceCharacterId = characterScope.SourceCharacterId,
                    Status = "Succeeded",
                    MovesProcessed = domainMoves.Count
                });
            }
            catch (Exception ex)
            {
                var error = ex.Message;
                run.Errors.Add($"{characterScope.CharacterId}: {error}");
                run.CharacterStatuses.Add(new IngestionRunCharacterStatus
                {
                    CharacterId = characterScope.CharacterId,
                    SourceCharacterId = characterScope.SourceCharacterId,
                    Status = "Failed",
                    MovesProcessed = 0,
                    Error = error
                });
            }
        }

        if (run.CharactersProcessed > 0)
        {
            await _datasetRepository.ReplaceAsync(replacementCharacters, replacementMoves, cancellationToken);
        }

        run.CompletedAt = DateTimeOffset.UtcNow;
        run.Status = GetFinalStatus(run);
        await _runRepository.SaveAsync(run, cancellationToken);
        return run;
    }

    private static Move MapMove(IngestionCharacterScope scope, ParsedMoveEntry parsed, int displayOrder)
    {
        var section = NormalizeSection(parsed.Section);
        var normalizedSection = NormalizeIdPart(section);
        var normalizedName = NormalizeIdPart(parsed.CanonicalName);

        return new Move
        {
            Id = $"{scope.CharacterId}-{normalizedSection}-{normalizedName}".ToLowerInvariant(),
            CharacterId = scope.CharacterId,
            Game = CurrentGame,
            CharacterName = scope.CharacterName,
            Section = section,
            CanonicalName = parsed.CanonicalName,
            DisplayOrder = displayOrder,
            FrameData = new MoveFrameData
            {
                Startup = parsed.Startup,
                Active = parsed.Active,
                Recovery = parsed.Recovery,
                OnHit = parsed.OnHit,
                OnBlock = parsed.OnBlock,
                FrameAdvantage = parsed.FrameAdvantage
            }
        };
    }

    private static string NormalizeSection(string section)
        => string.Equals(section, "Super Arts", StringComparison.OrdinalIgnoreCase) ? "SuperArts" : section;

    private static string NormalizeIdPart(string value)
        => new(value
            .Trim()
            .Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-')
            .ToArray());

    private static string GetFinalStatus(IngestionRun run)
    {
        if (run.Errors.Count == 0)
        {
            return "Succeeded";
        }

        return run.CharactersProcessed > 0 ? "PartiallySucceeded" : "Failed";
    }
}
