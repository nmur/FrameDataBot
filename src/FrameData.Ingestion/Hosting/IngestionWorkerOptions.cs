using Microsoft.Extensions.Configuration;
using FrameData.Domain.Media;
using FrameData.Ingestion.Publishing;

namespace FrameData.Ingestion.Hosting;

public sealed class IngestionWorkerOptions
{
    public IngestionWorkerMode Mode { get; init; } = IngestionWorkerMode.Ingest;
    public string SourceBaseUrl { get; init; } = "http://ensabahnur.free.fr/BastonNew/index.php";
    public string DatasetRoot { get; init; } = Path.Combine("data", "framedata");
    public string ActiveDatasetPath { get; init; } = Path.Combine("data", "framedata", "active");
    public IReadOnlyList<string> CharacterIds { get; init; } = [];
    public RepresentativeFrameSelectionPolicy RepresentativeFramePolicy { get; init; } = new();

    public static IngestionWorkerOptions FromConfiguration(IConfiguration configuration)
        => FromConfiguration(configuration, new IngestionWorkerCommand());

    public static IngestionWorkerOptions FromConfiguration(IConfiguration configuration, IngestionWorkerCommand command)
    {
        var characterScope = configuration["characters"] ?? configuration["Ingestion:Characters"];
        var mediaPilotScope = configuration["Ingestion:Media:PilotScope"]
            ?? configuration["Ingestion:Media:PilotMoveScope"]
            ?? configuration["INGESTION_MEDIA_PILOT_SCOPE"];

        return new IngestionWorkerOptions
        {
            Mode = command.Mode,
            SourceBaseUrl = configuration["Ingestion:SourceBaseUrl"]
                ?? configuration["INGESTION_SOURCE_BASE_URL"]
                ?? "http://ensabahnur.free.fr/BastonNew/index.php",
            DatasetRoot = configuration["FrameData:DatasetRoot"]
                ?? configuration["FRAMEDATA_DATASET_ROOT"]
                ?? Path.Combine("data", "framedata"),
            ActiveDatasetPath = configuration["FrameData:ActiveDatasetPath"]
                ?? configuration["FRAMEDATA_ACTIVE_DATASET_PATH"]
                ?? Path.Combine("data", "framedata", "active"),
            CharacterIds = ParseCharacterIds(characterScope),
            RepresentativeFramePolicy = new RepresentativeFrameSelectionPolicy
            {
                PilotMoveScope = ParseCharacterIds(mediaPilotScope),
                DummyImagePath = configuration["Ingestion:Media:DummyImagePath"]
                    ?? configuration["INGESTION_MEDIA_DUMMY_IMAGE_PATH"]
            }
        };
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(SourceBaseUrl))
        {
            errors.Add("Ingestion source base URL is required.");
        }

        errors.AddRange(new StaticDatasetPublisherOptions
        {
            DatasetRoot = DatasetRoot,
            ActiveDatasetPath = ActiveDatasetPath
        }.Validate());

        if (!string.IsNullOrWhiteSpace(RepresentativeFramePolicy.DummyImagePath)
            && !File.Exists(RepresentativeFramePolicy.DummyImagePath))
        {
            errors.Add($"Representative frame dummy image was not found: {RepresentativeFramePolicy.DummyImagePath}.");
        }

        return errors;
    }

    private static IReadOnlyList<string> ParseCharacterIds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(characterId => !string.IsNullOrWhiteSpace(characterId))
            .ToArray();
    }
}
