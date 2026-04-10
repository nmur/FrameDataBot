using FrameData.Shared.Contracts;

namespace FrameData.Contracts.Tests;

public sealed class IngestionRunContractTests
{
    [Fact]
    public void IngestionAcceptedResponse_UsesRunningStatus()
    {
        var accepted = new IngestionAcceptedResponse
        {
            RunId = "run-1",
            Status = "Running"
        };

        Assert.Equal("run-1", accepted.RunId);
        Assert.Equal("Running", accepted.Status);
    }

    [Fact]
    public void IngestionRunResponse_AllowsTerminalStatuses()
    {
        var statuses = new[] { "Succeeded", "PartiallySucceeded", "Failed" };

        foreach (var status in statuses)
        {
            var response = new IngestionRunResponse
            {
                RunId = "run-1",
                Status = status,
                StartedAt = DateTimeOffset.UtcNow
            };

            Assert.Equal(status, response.Status);
        }
    }
}
