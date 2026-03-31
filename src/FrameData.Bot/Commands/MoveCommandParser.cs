namespace FrameData.Bot.Commands;

public sealed class MoveCommandParser
{
    public MoveCommandParseResult Parse(string[] args)
    {
        if (args.Length < 2)
        {
            return MoveCommandParseResult.Invalid("Usage: /framedata <character> <move> [game]");
        }

        var character = args[0].Trim();
        var move = args[1].Trim();
        var game = args.Length > 2 ? args[2].Trim() : null;

        if (string.IsNullOrWhiteSpace(character) || string.IsNullOrWhiteSpace(move))
        {
            return MoveCommandParseResult.Invalid("Character and move are required.");
        }

        return MoveCommandParseResult.Valid(character, move, game);
    }
}

public sealed class MoveCommandParseResult
{
    private MoveCommandParseResult(bool isValid, string? error, string? character, string? move, string? game)
    {
        IsValid = isValid;
        Error = error;
        Character = character;
        Move = move;
        Game = game;
    }

    public bool IsValid { get; }
    public string? Error { get; }
    public string? Character { get; }
    public string? Move { get; }
    public string? Game { get; }

    public static MoveCommandParseResult Valid(string character, string move, string? game) => new(true, null, character, move, game);
    public static MoveCommandParseResult Invalid(string error) => new(false, error, null, null, null);
}
