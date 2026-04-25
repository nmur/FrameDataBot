namespace FrameData.Ingestion.Hosting;

public enum IngestionWorkerMode
{
    Ingest,
    Backup,
    Restore
}

public sealed class IngestionWorkerCommand
{
    public IngestionWorkerMode Mode { get; init; } = IngestionWorkerMode.Ingest;
    public string? BackupPath { get; init; }
    public string? RestorePath { get; init; }
    public IReadOnlyList<string> ConfigurationArgs { get; init; } = [];

    public static IngestionWorkerCommand Parse(string[] args)
    {
        var mode = IngestionWorkerMode.Ingest;
        var configurationArgs = new List<string>();
        string? backupPath = null;
        string? restorePath = null;

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (index == 0 && !arg.StartsWith("--", StringComparison.Ordinal))
            {
                mode = arg.ToLowerInvariant() switch
                {
                    "ingest" => IngestionWorkerMode.Ingest,
                    "backup" => IngestionWorkerMode.Backup,
                    "restore" => IngestionWorkerMode.Restore,
                    _ => throw new ArgumentException($"Unknown ingestion worker command: {arg}")
                };
                continue;
            }

            if (TryReadPathOption(args, ref index, "--out", out var outPath))
            {
                backupPath = outPath;
                continue;
            }

            if (TryReadPathOption(args, ref index, "--backup-path", out var backupPathValue))
            {
                backupPath = backupPathValue;
                continue;
            }

            if (TryReadPathOption(args, ref index, "--in", out var inPath))
            {
                restorePath = inPath;
                continue;
            }

            if (TryReadPathOption(args, ref index, "--restore-path", out var restorePathValue))
            {
                restorePath = restorePathValue;
                continue;
            }

            configurationArgs.Add(arg);
        }

        return new IngestionWorkerCommand
        {
            Mode = mode,
            BackupPath = backupPath,
            RestorePath = restorePath,
            ConfigurationArgs = configurationArgs
        };
    }

    private static bool TryReadPathOption(string[] args, ref int index, string optionName, out string? value)
    {
        var arg = args[index];
        value = null;

        if (arg.StartsWith($"{optionName}=", StringComparison.Ordinal))
        {
            value = arg[(optionName.Length + 1)..];
            return true;
        }

        if (!string.Equals(arg, optionName, StringComparison.Ordinal))
        {
            return false;
        }

        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"{optionName} requires a path value.");
        }

        value = args[index + 1];
        index++;
        return true;
    }
}
