using FrameData.Domain.Characters;
using FrameData.Domain.Ingestion;
using FrameData.Domain.Moves;
using FrameData.Infrastructure.Persistence.Repositories;
using FrameData.Scraper.Parsing;
using FrameData.Scraper.Source;

namespace FrameData.Ingestion.Services;

public sealed class IngestionOrchestrator
{
    private const string CurrentGame = "sf3_3s";

    private readonly ISourceHttpClient _sourceClient;
    private readonly CharacterSectionParser _sectionParser;
    private readonly CharacterRepository _characterRepository;
    private readonly MoveRepository _moveRepository;
    private readonly IngestionRunRepository _runRepository;
    private readonly CharacterExportWorkflow _exportWorkflow;

    public IngestionOrchestrator(
        ISourceHttpClient sourceClient,
        CharacterSectionParser sectionParser,
        CharacterRepository characterRepository,
        MoveRepository moveRepository,
        IngestionRunRepository runRepository,
        CharacterExportWorkflow exportWorkflow)
    {
        _sourceClient = sourceClient;
        _sectionParser = sectionParser;
        _characterRepository = characterRepository;
        _moveRepository = moveRepository;
        _runRepository = runRepository;
        _exportWorkflow = exportWorkflow;
    }

    public async Task<IngestionRun> RunAsync(IReadOnlyCollection<IngestionCharacterScope> scope, CancellationToken cancellationToken = default)
    {
        var run = new IngestionRun
        {
            Id = Guid.NewGuid().ToString("N"),
            StartedAt = DateTimeOffset.UtcNow,
            Status = "Running"
        };

        await _runRepository.SaveAsync(run, cancellationToken);
        return await ExecuteRunAsync(run, scope, cancellationToken);
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

        foreach (var characterScope in scope)
        {
            try
            {
                var html = await _sourceClient.GetCharacterPageAsync(characterScope.SourceCharacterId, cancellationToken);
                var parsedMoves = _sectionParser.Parse(html);
                var domainMoves = parsedMoves
                    .Select(parsed => MapMove(characterScope, parsed))
                    .ToList();

                var character = new Character
                {
                    Id = characterScope.CharacterId,
                    Game = CurrentGame,
                    Name = characterScope.CharacterName,
                    Aliases = characterScope.Aliases
                };

                await _characterRepository.UpsertAsync(character, cancellationToken);
                await _moveRepository.UpsertMovesAsync(character.Id, domainMoves, cancellationToken);
                await _exportWorkflow.ExportCharacterAsync(character, domainMoves, cancellationToken);

                run.CharactersProcessed += 1;
                run.MovesProcessed += domainMoves.Count;
            }
            catch (Exception ex)
            {
                run.Errors.Add($"{characterScope.CharacterId}: {ex.Message}");
            }
        }

        run.CompletedAt = DateTimeOffset.UtcNow;
        run.Status = GetFinalStatus(run);
        await _runRepository.SaveAsync(run, cancellationToken);
        return run;
    }

    private static Move MapMove(IngestionCharacterScope scope, ParsedMoveEntry parsed)
    {
        var normalizedSection = parsed.Section.Replace(" ", "-", StringComparison.Ordinal);
        var normalizedName = parsed.CanonicalName.Replace(" ", "-", StringComparison.Ordinal);

        return new Move
        {
            Id = $"{scope.CharacterId}-{normalizedSection}-{normalizedName}".ToLowerInvariant(),
            CharacterId = scope.CharacterId,
            Game = CurrentGame,
            CharacterName = scope.CharacterName,
            Section = parsed.Section,
            CanonicalName = parsed.CanonicalName,
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

    private static string GetFinalStatus(IngestionRun run)
    {
        if (run.Errors.Count == 0)
        {
            return "Succeeded";
        }

        return run.CharactersProcessed > 0 ? "PartiallySucceeded" : "Failed";
    }
}
