using FrameData.Domain.Moves;

namespace FrameData.Domain.MoveLookup;

public sealed class MoveLookupResult
{
    private MoveLookupResult(
        bool isFound,
        bool isAmbiguous,
        string? errorCode,
        string? errorMessage,
        Move? move,
        string? matchedBy,
        IReadOnlyList<MatchCandidate> candidates)
    {
        IsFound = isFound;
        IsAmbiguous = isAmbiguous;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Move = move;
        MatchedBy = matchedBy;
        Candidates = candidates;
    }

    public bool IsFound { get; }
    public bool IsAmbiguous { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public Move? Move { get; }
    public string? MatchedBy { get; }
    public IReadOnlyList<MatchCandidate> Candidates { get; }

    public static MoveLookupResult Found(Move move, string matchedBy = "Exact") => new(true, false, null, null, move, matchedBy, []);
    public static MoveLookupResult Ambiguous(string move, IReadOnlyList<MatchCandidate> candidates) => new(
        false,
        true,
        "ambiguous_move",
        $"Multiple moves matched: {move}",
        null,
        null,
        candidates);
    public static MoveLookupResult UnsupportedCharacter(string character) => new(false, false, "unsupported_character", $"Unsupported character: {character}", null, null, []);
    public static MoveLookupResult NotFound(string move) => new(false, false, "move_not_found", $"Move not found: {move}", null, null, []);
}
