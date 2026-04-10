using System.Text.Json;
using FrameData.Domain.Characters;
using FrameData.Domain.Moves;
using FrameData.Infrastructure.Storage;
using FrameData.Ingestion.Services;
using Shouldly;

namespace FrameData.Ingestion.Tests.Export;

public sealed class CharacterJsonExportServiceTests
{
    [Fact]
    public async Task ExportCharacterAsync_WritesOneJsonFilePerCharacter()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"framedata-export-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var exportService = new CharacterJsonExportService();
            var workflow = new CharacterExportWorkflow(exportService, tempDirectory);
            var character = new Character
            {
                Id = "makoto",
                Game = "sf3_3s",
                Name = "makoto",
                Aliases = ["mako"]
            };
            var moves = new[]
            {
                new Move
                {
                    Id = "makoto-normals-2mk",
                    CharacterId = "makoto",
                    Game = "sf3_3s",
                    CharacterName = "makoto",
                    Section = "Normals",
                    CanonicalName = "2mk",
                    FrameData = new MoveFrameData
                    {
                        Startup = "6",
                        Active = "3",
                        Recovery = "17",
                        OnHit = "+1",
                        OnBlock = "-2",
                        FrameAdvantage = "-2"
                    }
                }
            };

            await workflow.ExportCharacterAsync(character, moves);

            var exportPath = Path.Combine(tempDirectory, "makoto.json");
            File.Exists(exportPath).ShouldBeTrue();
            var json = await File.ReadAllTextAsync(exportPath);
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("Id").GetString().ShouldBe("makoto");
            doc.RootElement.GetProperty("Moves")[0].GetProperty("CanonicalName").GetString().ShouldBe("2mk");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }
}
