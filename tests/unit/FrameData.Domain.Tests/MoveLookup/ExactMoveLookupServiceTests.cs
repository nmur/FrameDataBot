using FrameData.Domain.MoveLookup;
using FrameData.Domain.Moves;
using NSubstitute;
using Shouldly;

namespace FrameData.Domain.Tests.MoveLookup;

public sealed class ExactMoveLookupServiceTests
{
    private readonly IMoveQueryRepository _repository = Substitute.For<IMoveQueryRepository>();
    private readonly ExactMoveLookupService _service;

    public ExactMoveLookupServiceTests()
    {
        _service = new ExactMoveLookupService(_repository);
    }

    [Fact]
    public async Task LookupAsync_WhenGameNotSupported_ReturnsUnsupportedGame()
    {
        _repository.SupportsGameAsync("sf2", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.LookupAsync("sf2", "makoto", "2mk");

        result.IsFound.ShouldBeFalse();
        result.ErrorCode.ShouldBe("unsupported_game");
    }

    [Fact]
    public async Task LookupAsync_WhenCharacterNotSupported_ReturnsUnsupportedCharacter()
    {
        _repository.SupportsGameAsync("sf3_3s", Arg.Any<CancellationToken>()).Returns(true);
        _repository.SupportsCharacterAsync("sf3_3s", "q", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.LookupAsync(null, "q", "2mk");

        result.IsFound.ShouldBeFalse();
        result.ErrorCode.ShouldBe("unsupported_character");
    }

    [Fact]
    public async Task LookupAsync_WhenMoveNotFound_ReturnsMoveNotFound()
    {
        _repository.SupportsGameAsync("sf3_3s", Arg.Any<CancellationToken>()).Returns(true);
        _repository.SupportsCharacterAsync("sf3_3s", "makoto", Arg.Any<CancellationToken>()).Returns(true);
        _repository.FindExactMoveAsync("sf3_3s", "makoto", "5lk", Arg.Any<CancellationToken>()).Returns((Move?)null);

        var result = await _service.LookupAsync(null, "makoto", "5lk");

        result.IsFound.ShouldBeFalse();
        result.ErrorCode.ShouldBe("move_not_found");
    }

    [Fact]
    public async Task LookupAsync_WhenMoveFound_ReturnsFoundMove()
    {
        var move = new Move
        {
            Id = "id",
            CharacterId = "makoto",
            Game = "sf3_3s",
            CharacterName = "makoto",
            Section = "Normals",
            CanonicalName = "2mk",
            FrameData = new MoveFrameData { Startup = "6", Active = "3", Recovery = "17" }
        };

        _repository.SupportsGameAsync("sf3_3s", Arg.Any<CancellationToken>()).Returns(true);
        _repository.SupportsCharacterAsync("sf3_3s", "makoto", Arg.Any<CancellationToken>()).Returns(true);
        _repository.FindExactMoveAsync("sf3_3s", "makoto", "2mk", Arg.Any<CancellationToken>()).Returns(move);

        var result = await _service.LookupAsync(null, "makoto", "2mk");

        result.IsFound.ShouldBeTrue();
        result.Move.ShouldNotBeNull();
        result.Move.CanonicalName.ShouldBe("2mk");
    }
}
