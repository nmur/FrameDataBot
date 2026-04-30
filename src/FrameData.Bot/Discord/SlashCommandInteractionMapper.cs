namespace FrameData.Bot.Discord;

public sealed class SlashCommandInteractionMapper
{
    public SlashCommandMapResult Map(string commandName, IEnumerable<SlashCommandOptionValue> options)
    {
        if (!FramedataSlashCommandDefinition.IsFramedataCommand(commandName))
        {
            return SlashCommandMapResult.Invalid("Unsupported command.");
        }

        var optionValues = options.ToDictionary(
            option => option.Name,
            option => option.Value?.ToString()?.Trim(),
            StringComparer.Ordinal);
        optionValues.TryGetValue(FramedataSlashCommandDefinition.CharacterOptionName, out var character);
        optionValues.TryGetValue(FramedataSlashCommandDefinition.MoveOptionName, out var move);

        if (string.IsNullOrWhiteSpace(character) || string.IsNullOrWhiteSpace(move))
        {
            return SlashCommandMapResult.Invalid("Character and move are required.");
        }

        return SlashCommandMapResult.Valid(new FramedataCommandInvocation(character, move));
    }
}

public sealed record SlashCommandOptionValue(string Name, object? Value);

public sealed record FramedataCommandInvocation(string Character, string Move);

public sealed class SlashCommandMapResult
{
    private SlashCommandMapResult(bool isValid, FramedataCommandInvocation? invocation, string? error)
    {
        IsValid = isValid;
        Invocation = invocation;
        Error = error;
    }

    public bool IsValid { get; }
    public FramedataCommandInvocation? Invocation { get; }
    public string? Error { get; }

    public static SlashCommandMapResult Valid(FramedataCommandInvocation invocation) => new(true, invocation, null);
    public static SlashCommandMapResult Invalid(string error) => new(false, null, error);
}
