using FrameData.Domain.Characters;
using FrameData.Domain.Moves;
using FrameData.Infrastructure.Storage;

namespace FrameData.Ingestion.Services;

public sealed class CharacterExportWorkflow
{
    private readonly CharacterJsonExportService _exportService;
    private readonly string _exportDirectory;

    public CharacterExportWorkflow(CharacterJsonExportService exportService, string exportDirectory)
    {
        _exportService = exportService;
        _exportDirectory = exportDirectory;
    }

    public Task ExportCharacterAsync(Character character, IReadOnlyCollection<Move> moves, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            character.Id,
            character.Name,
            character.Aliases,
            Moves = moves.Select(m => new
            {
                m.Section,
                m.CanonicalName,
                FrameData = new
                {
                    m.FrameData.Startup,
                    m.FrameData.Active,
                    m.FrameData.Recovery,
                    m.FrameData.OnHit,
                    m.FrameData.OnBlock,
                    m.FrameData.FrameAdvantage
                }
            })
        };

        var exportPath = Path.Combine(_exportDirectory, $"{character.Id}.json");
        return _exportService.ExportAsync(exportPath, payload, cancellationToken);
    }
}
