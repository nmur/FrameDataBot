using System.Net;
using System.Net.Http.Json;
using FrameData.Shared.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FrameData.Bot.Api;

public sealed class MoveQueryApiClient : IMoveQueryApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MoveQueryApiClient> _logger;

    public MoveQueryApiClient(HttpClient httpClient, ILogger<MoveQueryApiClient>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger ?? NullLogger<MoveQueryApiClient>.Instance;
    }

    public async Task<(MoveQueryResponse? Response, MoveAmbiguousResponse? Ambiguous, ErrorResponse? Error)> QueryMoveAsync(
        string character,
        string moveInput,
        CancellationToken cancellationToken = default)
    {
        var url = $"/v1/moves/query?character={Uri.EscapeDataString(character)}&moveInput={Uri.EscapeDataString(moveInput)}";
        _logger.LogInformation(
            "Calling frame data API for character {Character} and move input {MoveInput}.",
            character,
            moveInput);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        _logger.LogInformation(
            "Frame data API returned status {StatusCode} for character {Character} and move input {MoveInput}.",
            (int)response.StatusCode,
            character,
            moveInput);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var payload = await response.Content.ReadFromJsonAsync<MoveQueryResponse>(cancellationToken);
            _logger.LogDebug(
                "Frame data API matched {Character} {MoveName} by {MatchedBy}.",
                payload?.Character,
                payload?.MatchedMove,
                payload?.MatchedBy);

            return (payload, null, null);
        }

        if (response.StatusCode == HttpStatusCode.MultipleChoices)
        {
            var payload = await response.Content.ReadFromJsonAsync<MoveAmbiguousResponse>(cancellationToken);
            _logger.LogDebug(
                "Frame data API returned {CandidateCount} ambiguous candidate(s).",
                payload?.Candidates.Count ?? 0);

            return (null, payload, null);
        }

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
        _logger.LogDebug(
            "Frame data API returned error {ErrorCode}: {ErrorMessage}.",
            error?.Code,
            error?.Message);

        return (null, null, error);
    }
}
