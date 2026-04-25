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
            Status = "Running",
            Scope = "Scoped",
            CharactersQueued = 1
        };

        Assert.Equal("run-1", accepted.RunId);
        Assert.Equal("Running", accepted.Status);
        Assert.Equal("Scoped", accepted.Scope);
        Assert.Equal(1, accepted.CharactersQueued);
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

    [Fact]
    public void IngestionRunRequest_AllowsOptionalScopedCharacterIds()
    {
        var request = new IngestionRunRequest
        {
            CharacterIds = ["makoto", "chun-li"]
        };

        Assert.Equal(["makoto", "chun-li"], request.CharacterIds);
    }

    [Fact]
    public void IngestionRunResponse_ContainsPerCharacterStatusPayloads()
    {
        var response = new IngestionRunResponse
        {
            RunId = "run-1",
            Status = "PartiallySucceeded",
            StartedAt = DateTimeOffset.UtcNow,
            CharacterStatuses =
            [
                new IngestionRunCharacterStatusContract
                {
                    CharacterId = "makoto",
                    SourceCharacterId = 17,
                    Status = "Succeeded",
                    MovesProcessed = 1
                },
                new IngestionRunCharacterStatusContract
                {
                    CharacterId = "chun-li",
                    SourceCharacterId = 16,
                    Status = "Failed",
                    MovesProcessed = 0,
                    Error = "source unavailable"
                }
            ]
        };

        Assert.Equal(2, response.CharacterStatuses.Count);
        Assert.Equal("chun-li", response.CharacterStatuses[1].CharacterId);
        Assert.Equal("Failed", response.CharacterStatuses[1].Status);
        Assert.Equal("source unavailable", response.CharacterStatuses[1].Error);
    }
}
