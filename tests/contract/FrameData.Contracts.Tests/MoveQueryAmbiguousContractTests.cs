using FrameData.Shared.Contracts;

namespace FrameData.Contracts.Tests;

public sealed class MoveQueryAmbiguousContractTests
{
    [Fact]
    public void MoveAmbiguousResponse_ContainsMessageAndOrderedCandidates()
    {
        var response = new MoveAmbiguousResponse
        {
            Message = "Multiple moves matched. Try a more specific move name.",
            Candidates =
            [
                new MoveCandidate
                {
                    MoveName = "2hk",
                    Section = "Normals",
                    Score = 92
                },
                new MoveCandidate
                {
                    MoveName = "5hk",
                    Section = "Normals",
                    Score = 90
                }
            ]
        };

        Assert.Equal("Multiple moves matched. Try a more specific move name.", response.Message);
        Assert.Equal("2hk", response.Candidates[0].MoveName);
        Assert.Equal("Normals", response.Candidates[0].Section);
        Assert.Equal(92, response.Candidates[0].Score);
    }
}
