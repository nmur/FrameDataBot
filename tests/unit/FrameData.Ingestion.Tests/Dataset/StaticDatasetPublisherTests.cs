using System.Text.Json;
using FrameData.Domain.Characters;
using FrameData.Domain.Moves;
using FrameData.Infrastructure.Dataset;
using FrameData.Ingestion.Publishing;
using FrameData.Shared.Contracts;
using Shouldly;

namespace FrameData.Ingestion.Tests.Dataset;

public sealed class StaticDatasetPublisherTests
{
    [Fact]
    public async Task PublishAsync_WritesManifestCharacterFilesAndActiveDataset()
    {
        var root = CreateTempDirectory();
        try
        {
            var publisher = CreatePublisher(root);

            var manifest = await publisher.PublishAsync(
                [CreateCharacter("makoto", "Makoto")],
                [CreateMove("makoto", "2mk")],
                "http://example.test/source.php");

            var activePath = Path.Combine(root, "active");
            File.Exists(Path.Combine(activePath, "manifest.json")).ShouldBeTrue();
            File.Exists(Path.Combine(activePath, "characters", "makoto.json")).ShouldBeTrue();
            Directory.Exists(Path.Combine(activePath, "media")).ShouldBeTrue();

            await using var manifestStream = File.OpenRead(Path.Combine(activePath, "manifest.json"));
            var writtenManifest = await JsonSerializer.DeserializeAsync<StaticDatasetManifest>(
                manifestStream,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            writtenManifest.ShouldNotBeNull();
            writtenManifest.DatasetId.ShouldBe(manifest.DatasetId);
            writtenManifest.CharacterCount.ShouldBe(1);
            writtenManifest.MoveCount.ShouldBe(1);
            writtenManifest.Characters[0].File.ShouldBe("characters/makoto.json");
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task PublishAsync_LeavesPreviousActiveDatasetWhenValidationFails()
    {
        var root = CreateTempDirectory();
        try
        {
            var publisher = CreatePublisher(root);
            await publisher.PublishAsync(
                [CreateCharacter("makoto", "Makoto")],
                [CreateMove("makoto", "2mk", startup: "6")]);

            var activePath = Path.Combine(root, "active");
            var before = await File.ReadAllTextAsync(Path.Combine(activePath, "manifest.json"));

            await Should.ThrowAsync<InvalidDataException>(() => publisher.PublishAsync(
                [CreateCharacter("makoto", "Makoto")],
                [CreateMove("ken", "2mk", startup: "5")]));

            var after = await File.ReadAllTextAsync(Path.Combine(activePath, "manifest.json"));
            after.ShouldBe(before);

            var loaded = await new StaticFrameDataDatasetLoader().LoadAsync(activePath);
            loaded.Moves.Single().FrameData.Startup.ShouldBe("6");
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task PublishAsync_PreservesMotionDamageAndStunInCharacterFiles()
    {
        var root = CreateTempDirectory();
        try
        {
            var publisher = CreatePublisher(root);

            await publisher.PublishAsync(
                [CreateCharacter("makoto", "Makoto")],
                [CreateMove("makoto", "Hayate", motion: "236P", damage: "120", stun: "17")]);

            var loaded = await new StaticFrameDataDatasetLoader().LoadAsync(Path.Combine(root, "active"));

            var move = loaded.Moves.Single();
            move.Motion.ShouldBe("236P");
            move.Damage.ShouldBe("120");
            move.Stun.ShouldBe("17");
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    private static StaticDatasetPublisher CreatePublisher(string root)
        => new(new StaticDatasetPublisherOptions
        {
            DatasetRoot = root,
            ActiveDatasetPath = Path.Combine(root, "active")
        });

    private static Character CreateCharacter(string id, string name)
        => new()
        {
            Id = id,
            Game = "sf3_3s",
            Name = name,
            SourceCharacterId = 1,
            DisplayOrder = 1,
            UpdatedAt = DateTimeOffset.UtcNow,
            Aliases = [id[..3]]
        };

    private static Move CreateMove(
        string characterId,
        string canonicalName,
        string startup = "6",
        string? motion = null,
        string? damage = null,
        string? stun = null)
        => new()
        {
            Id = $"{characterId}-normals-{canonicalName}",
            CharacterId = characterId,
            Game = "sf3_3s",
            CharacterName = characterId,
            Section = "Normals",
            CanonicalName = canonicalName,
            DisplayOrder = 1,
            Motion = motion,
            Damage = damage,
            Stun = stun,
            FrameData = new MoveFrameData
            {
                Startup = startup,
                Active = "3",
                Recovery = "17",
                OnHit = "+1",
                OnBlock = "-2"
            }
        };

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"framedata-static-publish-{Guid.NewGuid():N}");
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
}
