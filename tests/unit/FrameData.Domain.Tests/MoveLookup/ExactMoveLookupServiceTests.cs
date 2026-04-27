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
    public async Task LookupAsync_WhenCharacterNotSupported_ReturnsUnsupportedCharacter()
    {
        _repository.SupportsCharacterAsync("q", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.LookupAsync("q", "2mk");

        result.IsFound.ShouldBeFalse();
        result.ErrorCode.ShouldBe("unsupported_character");
    }

    [Fact]
    public async Task LookupAsync_WhenMoveNotFound_ReturnsMoveNotFound()
    {
        _repository.SupportsCharacterAsync("makoto", Arg.Any<CancellationToken>()).Returns(true);
        _repository.FindExactMoveAsync("makoto", "5lk", Arg.Any<CancellationToken>()).Returns((Move?)null);
        _repository.GetMovesForCharacterAsync("makoto", Arg.Any<CancellationToken>()).Returns(Array.Empty<Move>());

        var result = await _service.LookupAsync("makoto", "5lk");

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

        _repository.SupportsCharacterAsync("makoto", Arg.Any<CancellationToken>()).Returns(true);
        _repository.FindExactMoveAsync("makoto", "2mk", Arg.Any<CancellationToken>()).Returns(move);

        var result = await _service.LookupAsync("makoto", "2mk");

        result.IsFound.ShouldBeTrue();
        result.Move.ShouldNotBeNull();
        result.Move.CanonicalName.ShouldBe("2mk");
    }

    [Fact]
    public async Task LookupAsync_WhenAliasMatchesSingleMove_ReturnsAliasMatch()
    {
        _repository.SupportsCharacterAsync("makoto", Arg.Any<CancellationToken>()).Returns(true);
        _repository.FindExactMoveAsync("makoto", "cr.HK", Arg.Any<CancellationToken>()).Returns((Move?)null);
        _repository.GetMovesForCharacterAsync("makoto", Arg.Any<CancellationToken>()).Returns(new[]
        {
            new Move
            {
                Id = "makoto-2hk",
                CharacterId = "makoto",
                Game = "sf3_3s",
                CharacterName = "Makoto",
                Section = "Normals",
                CanonicalName = "2hk",
                FrameData = new MoveFrameData { Startup = "8" }
            }
        });

        var result = await _service.LookupAsync("makoto", "cr.HK");

        result.IsFound.ShouldBeTrue();
        result.MatchedBy.ShouldBe("Alias");
        result.Move.ShouldNotBeNull();
        result.Move.CanonicalName.ShouldBe("2hk");
    }

    [Fact]
    public async Task LookupAsync_WhenFuzzyCandidatesAreAmbiguous_ReturnsCandidates()
    {
        _repository.SupportsCharacterAsync("makoto", Arg.Any<CancellationToken>()).Returns(true);
        _repository.FindExactMoveAsync("makoto", "hk", Arg.Any<CancellationToken>()).Returns((Move?)null);
        _repository.GetMovesForCharacterAsync("makoto", Arg.Any<CancellationToken>()).Returns(new[]
        {
            new Move
            {
                Id = "makoto-2hk",
                CharacterId = "makoto",
                Game = "sf3_3s",
                CharacterName = "Makoto",
                Section = "Normals",
                CanonicalName = "2hk",
                DisplayOrder = 1,
                FrameData = new MoveFrameData { Startup = "8" }
            },
            new Move
            {
                Id = "makoto-5hk",
                CharacterId = "makoto",
                Game = "sf3_3s",
                CharacterName = "Makoto",
                Section = "Normals",
                CanonicalName = "5hk",
                DisplayOrder = 2,
                FrameData = new MoveFrameData { Startup = "10" }
            }
        });

        var result = await _service.LookupAsync("makoto", "hk");

        result.IsAmbiguous.ShouldBeTrue();
        result.Candidates.Select(candidate => candidate.CanonicalName).ShouldBe(new[] { "2hk", "5hk" });
    }
}
