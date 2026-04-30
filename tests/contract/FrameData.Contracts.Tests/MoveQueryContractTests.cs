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
            FrameData = new FrameDataContract
            {
                Startup = "6",
                Active = "3",
                Recovery = "17",
                OnHit = "+1",
                OnBlock = "-2",
                FrameAdvantage = "-2"
            }
        };

        Assert.Equal("makoto", response.Character);
        Assert.Equal("236P", response.Motion);
        Assert.Equal("60", response.Damage);
        Assert.Equal("7", response.Stun);
        Assert.Equal("6", response.FrameData.Startup);
    }
}
