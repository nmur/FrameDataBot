using FrameData.Shared.Contracts;

namespace FrameData.Bot.Formatting;

public sealed class MoveResponseFormatter
{
    public string FormatSuccess(MoveQueryResponse response)
    {
        return $"{response.Character} {response.MatchedMove} ({response.Section}) | Startup {response.FrameData.Startup} Active {response.FrameData.Active} Recovery {response.FrameData.Recovery} OnHit {response.FrameData.OnHit} OnBlock {response.FrameData.OnBlock}";
    }

    public string FormatError(ErrorResponse error)
    {
        return error.Code switch
        {
            "unsupported_character" => "Unsupported character. Try a supported character name.",
            "unsupported_game" => "Unsupported game. Omit game or use sf3_3s.",
            "move_not_found" => "Move not found. Check spelling or use canonical notation.",
            _ => error.Message
        };
    }
}
