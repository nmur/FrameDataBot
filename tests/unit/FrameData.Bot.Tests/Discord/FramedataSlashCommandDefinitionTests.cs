using Discord;
using FrameData.Bot.Discord;
using Shouldly;

namespace FrameData.Bot.Tests.Discord;

public sealed class FramedataSlashCommandDefinitionTests
{
    [Fact]
    public void Build_CreatesFramedataSlashCommand()
    {
        var command = FramedataSlashCommandDefinition.Build();

        command.Name.Value.ShouldBe("framedata");
        command.Description.Value.ShouldNotBeNullOrWhiteSpace();
        command.Options.Value.Count.ShouldBe(2);
    }

    [Fact]
    public void Build_CreatesRequiredCharacterAndMoveOptions()
    {
        var command = FramedataSlashCommandDefinition.Build();
        var options = command.Options.Value.ToDictionary(option => option.Name);

        options.Keys.ShouldBe(["character", "move"], ignoreOrder: true);
        options["character"].Type.ShouldBe(ApplicationCommandOptionType.String);
        options["character"].IsRequired.ShouldBe(true);
        options["move"].Type.ShouldBe(ApplicationCommandOptionType.String);
        options["move"].IsRequired.ShouldBe(true);
    }
}
