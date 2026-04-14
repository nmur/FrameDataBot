using System.Net;
using System.Net.Http.Json;
using FrameData.Shared.Contracts;

namespace FrameData.Bot.Api;

public sealed class MoveQueryApiClient : IMoveQueryApiClient
{
    private readonly HttpClient _httpClient;

    public MoveQueryApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(MoveQueryResponse? Response, ErrorResponse? Error)> QueryMoveAsync(
        string character,
        string moveInput,
        CancellationToken cancellationToken = default)
    {
        var url = $"/v1/moves/query?character={Uri.EscapeDataString(character)}&moveInput={Uri.EscapeDataString(moveInput)}";
        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            return (await response.Content.ReadFromJsonAsync<MoveQueryResponse>(cancellationToken), null);
        }

        return (null, await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken));
    }
}
