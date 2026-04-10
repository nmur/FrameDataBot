using System.Net.Http;

namespace FrameData.Scraper.Source;

public sealed class SourceHttpClient : ISourceHttpClient
{
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
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
