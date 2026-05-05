using FrameData.Domain.Characters;
using FrameData.Domain.Moves;
using FrameData.Infrastructure.Dataset;
using FrameData.Ingestion.Hosting;
using FrameData.Ingestion.Publishing;
using FrameData.Ingestion.Services;
using FrameData.Scraper.Parsing;
using FrameData.Scraper.Source;

namespace FrameData.Ingestion.IntegrationTests;

public sealed class StaticDatasetPublishingTests
{
    private const string SampleHtml = """
        <html><body>
          <h2>Normals</h2>
          <table>
            <tr><th>Move</th><th>Startup</th><th>Active</th><th>Recovery</th><th>On Hit</th><th>On Block</th><th>Cr. Hit Adv.</th></tr>
            <tr><td>2mk</td><td>6</td><td>3</td><td>17</td><td>+1</td><td>-2</td><td>+3</td></tr>
          </table>
        </body></html>
        """;

    [Fact]
    public async Task RunAsync_WhenCharactersSucceed_PublishesManifestAndCharacterFiles()
    {
        var root = CreateTempDirectory();
        try
        {
            var orchestrator = CreateOrchestrator(root, new FakeSourceHttpClient(_ => SampleHtml));

            var run = await orchestrator.RunAsync(
            [
                new IngestionCharacterScope { CharacterId = "makoto", CharacterName = "Makoto", SourceCharacterId = 1 },
                new IngestionCharacterScope { CharacterId = "ken", CharacterName = "Ken", SourceCharacterId = 2 }
            ]);

            Assert.Equal("Succeeded", run.Status);
            Assert.Equal(2, run.CharactersProcessed);
            Assert.True(File.Exists(Path.Combine(root, "active", "manifest.json")));
            Assert.True(File.Exists(Path.Combine(root, "active", "characters", "makoto.json")));
            Assert.True(File.Exists(Path.Combine(root, "active", "characters", "ken.json")));

            var dataset = await new StaticFrameDataDatasetLoader().LoadAsync(Path.Combine(root, "active"));
            Assert.Equal(2, dataset.Characters.Count);
            Assert.Equal(2, dataset.Moves.Count);
            Assert.All(dataset.Moves, move => Assert.Equal("+3", move.FrameData.OnCrouchingHit));
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task RunAsync_WhenSomeCharactersFail_PublishesSuccessfulCharacterScopes()
    {
        var root = CreateTempDirectory();
        try
        {
            var orchestrator = CreateOrchestrator(root, new FakeSourceHttpClient(id =>
            {
                if (id == 2)
                {
                    throw new InvalidOperationException("Source unavailable");
                }

                return SampleHtml;
            }));

            var run = await orchestrator.RunAsync(
            [
                new IngestionCharacterScope { CharacterId = "makoto", CharacterName = "Makoto", SourceCharacterId = 1 },
                new IngestionCharacterScope { CharacterId = "ken", CharacterName = "Ken", SourceCharacterId = 2 }
            ]);

            Assert.Equal("PartiallySucceeded", run.Status);
            Assert.Equal(1, run.CharactersProcessed);
            Assert.True(File.Exists(Path.Combine(root, "active", "characters", "makoto.json")));
            Assert.False(File.Exists(Path.Combine(root, "active", "characters", "ken.json")));
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task PublishAsync_WhenNewDatasetIsInvalid_PreservesPreviousActiveDataset()
    {
        var root = CreateTempDirectory();
        try
        {
            var publisher = CreatePublisher(root);
            await publisher.PublishAsync(
                [CreateCharacter("makoto")],
                [CreateMove("makoto", "2mk", startup: "6")]);

            await Assert.ThrowsAsync<InvalidDataException>(() => publisher.PublishAsync(
                [CreateCharacter("makoto")],
                [CreateMove("ken", "2mk", startup: "5")]));

            var dataset = await new StaticFrameDataDatasetLoader().LoadAsync(Path.Combine(root, "active"));
            Assert.Single(dataset.Moves);
            Assert.Equal("6", dataset.Moves[0].FrameData.Startup);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    private static IngestionOrchestrator CreateOrchestrator(string root, ISourceHttpClient source)
    {
        var options = new IngestionWorkerOptions
        {
            SourceBaseUrl = "http://example.test/source.php",
            DatasetRoot = root,
            ActiveDatasetPath = Path.Combine(root, "active")
        };

        return new IngestionOrchestrator(
            source,
            new CharacterSectionParser(),
            CreatePublisher(root),
            options);
    }

    private static StaticDatasetPublisher CreatePublisher(string root)
        => new(new StaticDatasetPublisherOptions
        {
            DatasetRoot = root,
            ActiveDatasetPath = Path.Combine(root, "active")
        });

    private static Character CreateCharacter(string id)
        => new()
        {
            Id = id,
            Game = "sf3_3s",
            Name = id,
            SourceCharacterId = 1,
            DisplayOrder = 1,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    private static Move CreateMove(string characterId, string canonicalName, string startup)
        => new()
        {
            Id = $"{characterId}-normals-{canonicalName}",
            CharacterId = characterId,
            Game = "sf3_3s",
            CharacterName = characterId,
            Section = "Normals",
            CanonicalName = canonicalName,
            FrameData = new MoveFrameData
            {
                Startup = startup,
                Active = "3",
                Recovery = "17"
            }
        };

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"framedata-static-ingestion-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteTempDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class FakeSourceHttpClient : ISourceHttpClient
    {
        private readonly Func<int, string> _resolver;

        public FakeSourceHttpClient(Func<int, string> resolver)
        {
            _resolver = resolver;
        }

        public Task<string> GetCharacterPageAsync(int sourceCharacterId, CancellationToken cancellationToken = default)
            => Task.FromResult(_resolver(sourceCharacterId));
    }
}
