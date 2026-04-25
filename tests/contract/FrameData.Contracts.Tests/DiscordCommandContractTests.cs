using Discord;
using FrameData.Bot.Discord;

namespace FrameData.Contracts.Tests;

public sealed class DiscordCommandContractTests
{
    [Fact]
    public void FramedataSlashCommand_UsesExpectedCommandNameAndOptions()
    {
        var command = FramedataSlashCommandDefinition.Build();
        var options = command.Options.Value.ToDictionary(option => option.Name);

        Assert.Equal("framedata", command.Name.Value);
        Assert.Equal(2, options.Count);
        Assert.True(options["character"].IsRequired);
        Assert.Equal(ApplicationCommandOptionType.String, options["character"].Type);
        Assert.True(options["move"].IsRequired);
        Assert.Equal(ApplicationCommandOptionType.String, options["move"].Type);
    }
}
