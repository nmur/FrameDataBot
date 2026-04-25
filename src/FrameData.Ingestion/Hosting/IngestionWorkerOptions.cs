using Microsoft.Extensions.Configuration;

namespace FrameData.Ingestion.Hosting;

public sealed class IngestionWorkerOptions
{
    public string PostgresConnectionString { get; init; } = "";
    public string SourceBaseUrl { get; init; } = "http://ensabahnur.free.fr/BastonNew/index.php";
    public string ExportPath { get; init; } = Path.Combine("exports", "characters");
    public IReadOnlyList<string> CharacterIds { get; init; } = [];

    public static IngestionWorkerOptions FromConfiguration(IConfiguration configuration)
    {
        var characterScope = configuration["characters"] ?? configuration["Ingestion:Characters"];

        return new IngestionWorkerOptions
        {
            PostgresConnectionString = configuration["POSTGRES_CONNECTION_STRING"]
                ?? configuration.GetConnectionString("Postgres")
                ?? configuration["Postgres:ConnectionString"]
                ?? "",
            SourceBaseUrl = configuration["Ingestion:SourceBaseUrl"]
                ?? configuration["INGESTION_SOURCE_BASE_URL"]
                ?? "http://ensabahnur.free.fr/BastonNew/index.php",
            ExportPath = configuration["Ingestion:ExportPath"]
                ?? configuration["FRAMEDATA_EXPORTS_PATH"]
                ?? Path.Combine("exports", "characters"),
            CharacterIds = ParseCharacterIds(characterScope)
        };
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(PostgresConnectionString))
        {
            errors.Add("POSTGRES_CONNECTION_STRING is required.");
        }

        if (string.IsNullOrWhiteSpace(SourceBaseUrl))
        {
            errors.Add("Ingestion source base URL is required.");
        }

        if (string.IsNullOrWhiteSpace(ExportPath))
        {
            errors.Add("Frame data export path is required.");
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
