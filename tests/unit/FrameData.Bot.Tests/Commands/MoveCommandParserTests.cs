using FrameData.Bot.Commands;
using Shouldly;

namespace FrameData.Bot.Tests.Commands;

public sealed class MoveCommandParserTests
{
    private readonly MoveCommandParser _parser = new();

    [Fact]
    public void Parse_WhenCharacterAndMoveProvided_ParsesSuccessfullyWithDefaultGame()
    {
        var result = _parser.Parse(["makoto", "2mk"]);

        result.IsValid.ShouldBeTrue();
        result.Character.ShouldBe("makoto");
        result.Move.ShouldBe("2mk");
        result.Game.ShouldBeNull();
    }

    [Fact]
    public void Parse_WhenGameProvided_ParsesSuccessfully()
    {
        var result = _parser.Parse(["makoto", "2mk", "sf3_3s"]);

        result.IsValid.ShouldBeTrue();
        result.Game.ShouldBe("sf3_3s");
    }

    [Fact]
    public void Parse_WhenMissingArgs_ReturnsInvalid()
    {
        var result = _parser.Parse(["makoto"]);

        result.IsValid.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
    }
}
