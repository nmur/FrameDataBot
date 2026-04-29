using System.Globalization;
using System.Net.Http;
using System.Text;

namespace FrameData.Scraper.Source;

public sealed class SourceHttpClient : ISourceHttpClient, IHitboxSourceClient
{
    private const string ContentContainerId = "content_char";
    private const int MaxSectionLoadAttempts = 5;
    private static readonly TimeSpan SectionLoadRetryDelay = TimeSpan.FromMilliseconds(250);

    private static readonly IReadOnlyList<FrameDataSectionRequest> FrameDataSections =
    [
        new("Normals", "normals", RequiresFrameTable: true),
        new("Specials", "specials", RequiresFrameTable: true),
        new("Super Arts", "supers", RequiresFrameTable: true),
        new("Misc", "misc", RequiresFrameTable: false)
    ];

    private readonly HttpClient _httpClient;

    public SourceHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetCharacterPageAsync(int sourceCharacterId, CancellationToken cancellationToken = default)
    {
        var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;
        var url = string.IsNullOrWhiteSpace(baseUrl)
            ? $"?id={sourceCharacterId}"
            : $"{baseUrl}?id={sourceCharacterId}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!ContainsContentCharacterContainer(html))
        {
            return html;
        }

        return await GetLoadedCharacterContentAsync(sourceCharacterId, cancellationToken);
    }

    public async Task<string> GetHitboxDisplayPageAsync(
        string sourcePathOrUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePathOrUrl))
        {
            throw new ArgumentException("Hitbox display source path is required.", nameof(sourcePathOrUrl));
        }

        using var response = await _httpClient.GetAsync(ResolveSourceUri(sourcePathOrUrl), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private async Task<string> GetLoadedCharacterContentAsync(int sourceCharacterId, CancellationToken cancellationToken)
    {
        var html = new StringBuilder("<html><body><div id=\"");
        html.Append(ContentContainerId);
        html.Append("\">");
        foreach (var section in FrameDataSections)
        {
            var sectionHtml = await GetLoadedSectionAsync(sourceCharacterId, section, cancellationToken);
            html.Append("<h2>");
            html.Append(section.Heading);
            html.Append("</h2>");
            html.Append(sectionHtml);
        }

        html.Append("</div></body></html>");
        return html.ToString();
    }

    private async Task<string> GetLoadedSectionAsync(
        int sourceCharacterId,
        FrameDataSectionRequest section,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxSectionLoadAttempts; attempt++)
        {
            var sectionHtml = await FetchSectionAsync(sourceCharacterId, section.SourceId, cancellationToken);
            if (ContainsLoadedSectionContent(sectionHtml, section.RequiresFrameTable))
            {
                return sectionHtml;
            }

            if (attempt < MaxSectionLoadAttempts)
            {
                await Task.Delay(SectionLoadRetryDelay, cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Timed out waiting for #{ContentContainerId} to load {section.Heading} frame data.");
    }

    private async Task<string> FetchSectionAsync(
        int sourceCharacterId,
        string sectionSourceId,
        CancellationToken cancellationToken)
    {
        var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["page"] = "ajax_loadData.php",
            ["iChar"] = sourceCharacterId.ToString(CultureInfo.InvariantCulture),
            ["type"] = "fd",
            ["id"] = sectionSourceId,
            ["div"] = ContentContainerId,
            ["version"] = "revised"
        });

        using var response = await _httpClient.PostAsync(baseUrl, form, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static bool ContainsContentCharacterContainer(string html)
        => html.Contains($"id=\"{ContentContainerId}\"", StringComparison.OrdinalIgnoreCase)
            || html.Contains($"id='{ContentContainerId}'", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsLoadedSectionContent(string html, bool requiresFrameTable)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return false;
        }

        return !requiresFrameTable
            || html.Contains("<table", StringComparison.OrdinalIgnoreCase)
            || html.Contains("id=\"fd_table\"", StringComparison.OrdinalIgnoreCase)
            || html.Contains("id='fd_table'", StringComparison.OrdinalIgnoreCase);
    }

    private Uri ResolveSourceUri(string sourcePathOrUrl)
    {
        if (Uri.TryCreate(sourcePathOrUrl, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri;
        }

        if (_httpClient.BaseAddress is null)
        {
            return new Uri(sourcePathOrUrl, UriKind.Relative);
        }

        var baseUri = _httpClient.BaseAddress;
        if (sourcePathOrUrl.StartsWith("?", StringComparison.Ordinal))
        {
            return new Uri($"{baseUri.GetLeftPart(UriPartial.Path)}{sourcePathOrUrl}");
        }

        return new Uri(baseUri, sourcePathOrUrl);
    }

    private sealed record FrameDataSectionRequest(string Heading, string SourceId, bool RequiresFrameTable);
}
