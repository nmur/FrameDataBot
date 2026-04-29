using FrameData.Shared.Contracts;

namespace FrameData.Contracts.Tests;

public sealed class MoveMediaContractTests
{
    [Fact]
    public void MoveQueryResponse_CanCarryRepresentativeMediaFields()
    {
        var response = new MoveQueryResponse
        {
            Character = "Ken",
            MatchedMove = "Jab",
            Section = "Normals",
            MatchedBy = "Exact",
            FrameData = new FrameDataContract(),
            Media = new MoveMediaContract
            {
                RepresentativeFrameImageUrl = "media/ken/ken-normals-jab/representative-active-frame.png",
                SelectedFrame = "006",
                SelectionStrategy = "largest-active-hitbox-area",
                CaptureStatus = "Success",
                FallbackReason = null
            }
        };

        Assert.NotNull(response.Media);
        Assert.Equal("media/ken/ken-normals-jab/representative-active-frame.png", response.Media.RepresentativeFrameImageUrl);
        Assert.Equal("006", response.Media.SelectedFrame);
        Assert.Equal("largest-active-hitbox-area", response.Media.SelectionStrategy);
        Assert.Equal("Success", response.Media.CaptureStatus);
        Assert.Null(response.Media.FallbackReason);
    }

    [Fact]
    public void MoveQueryResponse_CanCarryDummyFallbackMediaFields()
    {
        var response = new MoveQueryResponse
        {
            Character = "Ken",
            MatchedMove = "Strong",
            Section = "Normals",
            MatchedBy = "Exact",
            FrameData = new FrameDataContract(),
            Media = new MoveMediaContract
            {
                RepresentativeFrameImageUrl = "media/ken/ken-normals-strong/representative-active-frame.png",
                SelectionStrategy = "largest-active-hitbox-area",
                CaptureStatus = "DummyFallback",
                FallbackReason = "Selected frame image was not available."
            }
        };

        Assert.NotNull(response.Media);
        Assert.Equal("DummyFallback", response.Media.CaptureStatus);
        Assert.Equal("Selected frame image was not available.", response.Media.FallbackReason);
    }
}
