using FrameData.Shared.Contracts;

namespace FrameData.Bot.Formatting;

public sealed class MoveResponseFormatter
{
    public string FormatSuccess(MoveQueryResponse response)
    {
        var values = new List<string>();
        AddOptionalValue(values, "Motion", response.Motion);
        AddOptionalValue(values, "Damage", response.Damage);
        AddOptionalValue(values, "Stun", response.Stun);
        values.Add($"Startup {response.FrameData.Startup}");
        values.Add($"Active {response.FrameData.Active}");
        values.Add($"Recovery {response.FrameData.Recovery}");
        values.Add($"OnHit {response.FrameData.OnHit}");
        values.Add($"OnBlock {response.FrameData.OnBlock}");

        return $"{response.Character} {response.MatchedMove} ({response.Section}) | {string.Join(' ', values)}";
    }

    public string FormatAmbiguous(MoveAmbiguousResponse response)
    {
        var candidates = string.Join(
            "; ",
            response.Candidates.Select(candidate => $"{candidate.MoveName} ({candidate.Section}, {candidate.Score:0})"));

        return $"{response.Message} Candidates: {candidates}";
    }

    public string FormatError(ErrorResponse error)
    {
        return error.Code switch
        {
            "unsupported_character" => "Unsupported character. Try a supported character name.",
            "move_not_found" => "Move not found. Try an exact move name or clearer notation.",
            _ => error.Message
        };
    }

    private static void AddOptionalValue(ICollection<string> values, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values.Add($"{label} {value}");
        }
    }
}
