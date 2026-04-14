using FrameData.Shared.Contracts;

namespace FrameData.Bot.Api;

public interface IMoveQueryApiClient
{
    Task<(MoveQueryResponse? Response, ErrorResponse? Error)> QueryMoveAsync(
        string character,
        string moveInput,
        CancellationToken cancellationToken = default);
}
