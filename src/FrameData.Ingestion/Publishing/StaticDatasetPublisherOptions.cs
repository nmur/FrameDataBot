namespace FrameData.Ingestion.Publishing;

public sealed class StaticDatasetPublisherOptions
{
    public string DatasetRoot { get; init; } = Path.Combine("data", "framedata");
    public string ActiveDatasetPath { get; init; } = Path.Combine("data", "framedata", "active");

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(DatasetRoot))
        {
            errors.Add("FRAMEDATA_DATASET_ROOT is required.");
        }

        if (string.IsNullOrWhiteSpace(ActiveDatasetPath))
        {
            errors.Add("FRAMEDATA_ACTIVE_DATASET_PATH is required.");
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var root = Path.GetFullPath(DatasetRoot);
        var active = Path.GetFullPath(ActiveDatasetPath);
        if (string.Equals(root, active, StringComparison.Ordinal))
        {
            errors.Add("FRAMEDATA_ACTIVE_DATASET_PATH must point to a child of FRAMEDATA_DATASET_ROOT.");
        }
        else if (!active.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            errors.Add("FRAMEDATA_ACTIVE_DATASET_PATH must be inside FRAMEDATA_DATASET_ROOT.");
        }

        return errors;
    }
}
