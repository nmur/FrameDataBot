namespace FrameData.Bot.Diagnostics;

public static class DatasetMountDiagnostics
{
    private const int MaxEntries = 20;

    public static DatasetMountDiagnosticSnapshot Capture(string? activeDatasetPath, string? attachmentFilePath = null)
    {
        var effectiveActiveDatasetPath = string.IsNullOrWhiteSpace(activeDatasetPath)
            ? TryInferActiveDatasetPath(attachmentFilePath)
            : activeDatasetPath;
        var activeFullPath = GetFullPathOrNull(effectiveActiveDatasetPath);
        var activeDirectory = activeFullPath is null ? null : new DirectoryInfo(activeFullPath);
        var activeParentPath = activeFullPath is null ? null : Directory.GetParent(activeFullPath)?.FullName;
        var manifestPath = activeFullPath is null ? null : Path.Combine(activeFullPath, "manifest.json");
        var mediaRootPath = activeFullPath is null ? null : Path.Combine(activeFullPath, "media");
        var attachmentFullPath = GetFullPathOrNull(attachmentFilePath);
        var attachmentDirectoryPath = attachmentFullPath is null ? null : Path.GetDirectoryName(attachmentFullPath);

        return new DatasetMountDiagnosticSnapshot
        {
            ActiveDatasetPath = effectiveActiveDatasetPath,
            ActiveDatasetFullPath = activeFullPath,
            ActiveDatasetExists = Directory.Exists(activeFullPath),
            ActiveDatasetLinkTarget = ReadLinkTarget(activeDirectory),
            ActiveDatasetResolvedLinkTarget = ResolveLinkTarget(activeDirectory),
            ActiveDatasetManifestPath = manifestPath,
            ActiveDatasetManifestExists = File.Exists(manifestPath),
            ActiveDatasetParentPath = activeParentPath,
            ActiveDatasetParentExists = Directory.Exists(activeParentPath),
            ActiveDatasetParentEntries = ListEntries(activeParentPath),
            MediaRootPath = mediaRootPath,
            MediaRootExists = Directory.Exists(mediaRootPath),
            MediaRootEntries = ListEntries(mediaRootPath),
            AttachmentFilePath = attachmentFilePath,
            AttachmentFullPath = attachmentFullPath,
            AttachmentExists = File.Exists(attachmentFullPath),
            AttachmentDirectoryPath = attachmentDirectoryPath,
            AttachmentDirectoryExists = Directory.Exists(attachmentDirectoryPath),
            AttachmentDirectoryEntries = ListEntries(attachmentDirectoryPath),
            CurrentDirectory = Directory.GetCurrentDirectory(),
            BaseDirectory = AppContext.BaseDirectory,
            EnvironmentActiveDatasetPath = Environment.GetEnvironmentVariable("FRAMEDATA_ACTIVE_DATASET_PATH"),
            EnvironmentDatasetRoot = Environment.GetEnvironmentVariable("FRAMEDATA_DATASET_ROOT")
        };
    }

    private static string? TryInferActiveDatasetPath(string? attachmentFilePath)
    {
        var fullPath = GetFullPathOrNull(attachmentFilePath);
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return null;
        }

        var normalized = fullPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var mediaSegment = $"{Path.DirectorySeparatorChar}media{Path.DirectorySeparatorChar}";
        var mediaIndex = normalized.IndexOf(mediaSegment, StringComparison.Ordinal);
        return mediaIndex <= 0 ? null : normalized[..mediaIndex];
    }

    private static string? GetFullPathOrNull(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return $"<invalid path: {ex.Message}>";
        }
    }

    private static string? ReadLinkTarget(DirectoryInfo? directory)
    {
        try
        {
            return directory?.LinkTarget;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return $"<unavailable: {ex.Message}>";
        }
    }

    private static string? ResolveLinkTarget(DirectoryInfo? directory)
    {
        try
        {
            return directory?.ResolveLinkTarget(returnFinalTarget: true)?.FullName;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return $"<unavailable: {ex.Message}>";
        }
    }

    private static string[] ListEntries(string? directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return [];
        }

        try
        {
            return Directory.EnumerateFileSystemEntries(directoryPath)
                .Order(StringComparer.Ordinal)
                .Take(MaxEntries)
                .Select(DescribeEntry)
                .ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [$"<unavailable: {ex.Message}>"];
        }
    }

    private static string DescribeEntry(string path)
    {
        var name = Path.GetFileName(path);
        var type = Directory.Exists(path) ? "dir" : File.Exists(path) ? "file" : "other";
        string? linkTarget = null;

        try
        {
            linkTarget = File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint)
                ? new FileInfo(path).LinkTarget ?? new DirectoryInfo(path).LinkTarget
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            linkTarget = $"<unavailable: {ex.Message}>";
        }

        return string.IsNullOrWhiteSpace(linkTarget)
            ? $"{name} [{type}]"
            : $"{name} [{type} -> {linkTarget}]";
    }
}

public sealed class DatasetMountDiagnosticSnapshot
{
    public string? ActiveDatasetPath { get; init; }
    public string? ActiveDatasetFullPath { get; init; }
    public bool ActiveDatasetExists { get; init; }
    public string? ActiveDatasetLinkTarget { get; init; }
    public string? ActiveDatasetResolvedLinkTarget { get; init; }
    public string? ActiveDatasetManifestPath { get; init; }
    public bool ActiveDatasetManifestExists { get; init; }
    public string? ActiveDatasetParentPath { get; init; }
    public bool ActiveDatasetParentExists { get; init; }
    public IReadOnlyList<string> ActiveDatasetParentEntries { get; init; } = [];
    public string? MediaRootPath { get; init; }
    public bool MediaRootExists { get; init; }
    public IReadOnlyList<string> MediaRootEntries { get; init; } = [];
    public string? AttachmentFilePath { get; init; }
    public string? AttachmentFullPath { get; init; }
    public bool AttachmentExists { get; init; }
    public string? AttachmentDirectoryPath { get; init; }
    public bool AttachmentDirectoryExists { get; init; }
    public IReadOnlyList<string> AttachmentDirectoryEntries { get; init; } = [];
    public string CurrentDirectory { get; init; } = string.Empty;
    public string BaseDirectory { get; init; } = string.Empty;
    public string? EnvironmentActiveDatasetPath { get; init; }
    public string? EnvironmentDatasetRoot { get; init; }
}
