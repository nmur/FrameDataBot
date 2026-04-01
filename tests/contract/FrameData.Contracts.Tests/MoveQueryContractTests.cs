using FrameData.Shared.Contracts;

namespace FrameData.Contracts.Tests;

public sealed class MoveQueryContractTests
{
    [Fact]
    public void ErrorResponse_ContainsExpectedFields()
    {
        var error = new ErrorResponse
        {
            Code = "not_found",
            Message = "Move not found"
        };

        Assert.Equal("not_found", error.Code);
        Assert.Equal("Move not found", error.Message);
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
        Assert.Equal("6", response.FrameData.Startup);
    }
}
