using FrameData.Bot.Discord;
using Shouldly;

namespace FrameData.Bot.Tests.Discord;

public sealed class SlashCommandInteractionMapperTests
{
    private readonly SlashCommandInteractionMapper _mapper = new();

    [Fact]
    public void Map_WhenRequiredOptionsExist_ReturnsInvocation()
    {
        var result = _mapper.Map(
            "framedata",
            [
                new SlashCommandOptionValue("character", "makoto"),
                new SlashCommandOptionValue("move", "2mk")
            ]);

        result.IsValid.ShouldBeTrue();
        result.Invocation.ShouldNotBeNull();
        result.Invocation.Character.ShouldBe("makoto");
        result.Invocation.Move.ShouldBe("2mk");
    }

    [Fact]
    public void Map_WhenOptionValuesHaveWhitespace_TrimsValues()
    {
        var result = _mapper.Map(
            "framedata",
            [
                new SlashCommandOptionValue("character", " makoto "),
                new SlashCommandOptionValue("move", " 2mk ")
            ]);

        result.Invocation.ShouldNotBeNull();
        result.Invocation.Character.ShouldBe("makoto");
        result.Invocation.Move.ShouldBe("2mk");
    }

    [Fact]
    public void Map_WhenMoveMissing_ReturnsValidationError()
    {
        var result = _mapper.Map(
            "framedata",
            [new SlashCommandOptionValue("character", "makoto")]);

        result.IsValid.ShouldBeFalse();
        result.Error.ShouldBe("Character and move are required.");
    }

    [Fact]
    public void Map_WhenCommandNameDoesNotMatch_ReturnsUnsupportedCommand()
    {
        var result = _mapper.Map(
            "help",
            [
                new SlashCommandOptionValue("character", "makoto"),
                new SlashCommandOptionValue("move", "2mk")
            ]);

        result.IsValid.ShouldBeFalse();
        result.Error.ShouldBe("Unsupported command.");
    }
}
