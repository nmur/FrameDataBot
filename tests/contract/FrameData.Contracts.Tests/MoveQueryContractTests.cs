using FrameData.Shared.Contracts;

namespace FrameData.Contracts.Tests;

public sealed class MoveQueryContractTests
{
    [Fact]
    public void ErrorResponse_ContainsExpectedFields()
    {
        var error = new ErrorResponse
        {
            Code = "move_not_found",
            Message = "Move not found"
        };

        Assert.Equal("move_not_found", error.Code);
        Assert.Equal("Move not found", error.Message);
    }

    [Fact]
    public void ErrorResponse_UsesSupportedErrorCodes()
    {
        var error = new ErrorResponse
        {
            Code = "unsupported_character",
            Message = "Unsupported character: remy"
        };

        Assert.Equal("unsupported_character", error.Code);
    }

    [Fact]
    public void MoveQueryResponse_ContainsExpectedFields()
    {
        var response = new MoveQueryResponse
        {
            Character = "makoto",
            MatchedMove = "2mk",
            Section = "Normals",
            MatchedBy = "Exact",
            Motion = "236P",
            Damage = "60",
            Stun = "7",
            CharacterFrameDataUrl = "http://example.test/source.php?id=1",
            MoveHitboxDisplayUrl = "http://example.test/hitboxesDisplay.php?iMove=2",
            GameRestaurantMoveUrl = "http://gere.stars.ne.jp/01_3rd/kouryaku/makoto/makoto_h1.html",
            FrameData = new FrameDataContract
            {
                Startup = "6",
                Active = "3",
                Recovery = "17",
                OnHit = "+1",
                OnBlock = "-2",
                OnCrouchingHit = "+3"
            }
        };

        Assert.Equal("makoto", response.Character);
        Assert.Equal("236P", response.Motion);
        Assert.Equal("60", response.Damage);
        Assert.Equal("7", response.Stun);
        Assert.Equal("http://example.test/source.php?id=1", response.CharacterFrameDataUrl);
        Assert.Equal("http://example.test/hitboxesDisplay.php?iMove=2", response.MoveHitboxDisplayUrl);
        Assert.Equal("http://gere.stars.ne.jp/01_3rd/kouryaku/makoto/makoto_h1.html", response.GameRestaurantMoveUrl);
        Assert.Equal("6", response.FrameData.Startup);
        Assert.Equal("+3", response.FrameData.OnCrouchingHit);
    }
}
