using FrameData.Shared.Contracts;

namespace FrameData.Contracts.Tests;

public sealed class MoveQueryContractTests
{
    [Fact]
    public void MoveQueryRequest_GameField_IsOptional()
    {
        var request = new MoveQueryRequest
        {
            Character = "makoto",
            MoveInput = "2mk"
        };

        Assert.Null(request.Game);
        Assert.Equal("makoto", request.Character);
        Assert.Equal("2mk", request.MoveInput);
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
