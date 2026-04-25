using FrameData.Domain.Ingestion;

namespace FrameData.Ingestion.Hosting;

public static class IngestionWorkerExitCodes
{
    public const int Success = 0;
    public const int Failure = 1;
    public const int PartialSuccess = 2;
    public const int ConfigurationError = 64;
}

public static class IngestionWorkerExitCodeMapper
{
    public static int MapRunStatus(IngestionRun run)
        => run.Status switch
        {
            "Succeeded" => IngestionWorkerExitCodes.Success,
            "PartiallySucceeded" => IngestionWorkerExitCodes.PartialSuccess,
            _ => IngestionWorkerExitCodes.Failure
        };
}
