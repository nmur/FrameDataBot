namespace FrameData.Bot.Commands;

public sealed class MoveCommandParser
{
    public MoveCommandParseResult Parse(string[] args)
    {
        if (args.Length != 2)
        {
            return MoveCommandParseResult.Invalid("Usage: /framedata <character> <move>");
        }

        var character = args[0].Trim();
        var move = args[1].Trim();

        if (string.IsNullOrWhiteSpace(character) || string.IsNullOrWhiteSpace(move))
        {
            return MoveCommandParseResult.Invalid("Character and move are required.");
        }

        return MoveCommandParseResult.Valid(character, move);
    }
}

public sealed class MoveCommandParseResult
{
    private MoveCommandParseResult(bool isValid, string? error, string? character, string? move)
    {
        IsValid = isValid;
        Error = error;
        Character = character;
        Move = move;
    }

    public bool IsValid { get; }
    public string? Error { get; }
    public string? Character { get; }
    public string? Move { get; }

    public static MoveCommandParseResult Valid(string character, string move) => new(true, null, character, move);
    public static MoveCommandParseResult Invalid(string error) => new(false, error, null, null);
}
