using FrameData.Domain.Moves;

namespace FrameData.Domain.MoveLookup;

public sealed class MoveLookupResult
{
    private MoveLookupResult(bool isFound, string? errorCode, string? errorMessage, Move? move)
    {
        IsFound = isFound;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Move = move;
    }

    public bool IsFound { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public Move? Move { get; }

    public static MoveLookupResult Found(Move move) => new(true, null, null, move);
    public static MoveLookupResult UnsupportedGame(string game) => new(false, "unsupported_game", $"Unsupported game: {game}", null);
    public static MoveLookupResult UnsupportedCharacter(string character) => new(false, "unsupported_character", $"Unsupported character: {character}", null);
    public static MoveLookupResult NotFound(string move) => new(false, "move_not_found", $"Move not found: {move}", null);
}
