using Discord;

namespace FrameData.Bot.Discord;

public static class FramedataSlashCommandDefinition
{
    public const string CommandName = "framedata";
    public const string CharacterOptionName = "character";
    public const string MoveOptionName = "move";

    public static SlashCommandProperties Build()
    {
        return new SlashCommandBuilder()
            .WithName(CommandName)
            .WithDescription("Look up Street Fighter III: 3rd Strike frame data.")
            .AddOption(
                CharacterOptionName,
                ApplicationCommandOptionType.String,
                "Character name.",
                isRequired: true)
            .AddOption(
                MoveOptionName,
                ApplicationCommandOptionType.String,
                "Move name.",
                isRequired: true)
            .Build();
    }

    public static bool IsFramedataCommand(string commandName)
    {
        return string.Equals(commandName, CommandName, StringComparison.Ordinal);
    }
}
