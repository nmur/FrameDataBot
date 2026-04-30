namespace FrameData.Ingestion.Hosting;

public enum IngestionWorkerMode
{
    Ingest
}

public sealed class IngestionWorkerCommand
{
    public IngestionWorkerMode Mode { get; init; } = IngestionWorkerMode.Ingest;
    public IReadOnlyList<string> ConfigurationArgs { get; init; } = [];

    public static IngestionWorkerCommand Parse(string[] args)
    {
        var mode = IngestionWorkerMode.Ingest;
        var configurationArgs = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (index == 0 && !arg.StartsWith("--", StringComparison.Ordinal))
            {
                mode = arg.ToLowerInvariant() switch
                {
                    "ingest" => IngestionWorkerMode.Ingest,
                    _ => throw new ArgumentException($"Unknown ingestion worker command: {arg}")
                };
                continue;
            }

            configurationArgs.Add(arg);
        }

        return new IngestionWorkerCommand
        {
            Mode = mode,
            ConfigurationArgs = configurationArgs
        };
    }
}
