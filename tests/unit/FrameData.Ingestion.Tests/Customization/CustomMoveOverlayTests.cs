using FrameData.Domain.Media;
using FrameData.Domain.Moves;
using FrameData.Ingestion.Customization;
using Shouldly;

namespace FrameData.Ingestion.Tests.Customization;

public sealed class CustomMoveOverlayTests
{
    private readonly CustomMoveOverlay _overlay = new();

    [Fact]
    public void Apply_WhenOroCrouchingRoundhouseExists_AddsPeanutMoveClone()
    {
        var sourceMove = CreateMove(
            "oro-normals-crouching-roundhouse",
            "oro",
            "Oro",
            "Crouching Roundhouse",
            damage: "80");

        var moves = _overlay.Apply("oro", [sourceMove]);

        var customMove = moves.Single(move => move.Id == "oro-custom-peanut");
        customMove.CanonicalName.ShouldBe("Indecent Exposure");
        customMove.CharacterId.ShouldBe("oro");
        customMove.CharacterName.ShouldBe("Oro");
        customMove.Section.ShouldBe("Specials");
        customMove.SourceMoveId.ShouldBe(sourceMove.SourceMoveId);
        customMove.SourceHitboxPath.ShouldBe(sourceMove.SourceHitboxPath);
        customMove.Damage.ShouldBe("69");
        customMove.Stun.ShouldBe(sourceMove.Stun);
        customMove.FrameData.Startup.ShouldBe(sourceMove.FrameData.Startup);
        customMove.FrameData.Active.ShouldBe(sourceMove.FrameData.Active);
        customMove.FrameData.Recovery.ShouldBe(sourceMove.FrameData.Recovery);
        customMove.Media.ShouldBeEmpty();
    }

    [Fact]
    public void Apply_WhenSourceMoveUsesNumpadName_AddsPeanutMoveClone()
    {
        var sourceMove = CreateMove("oro-normals-2hk", "oro", "Oro", "2hk");

        var moves = _overlay.Apply("oro", [sourceMove]);

        moves.ShouldContain(move => move.Id == "oro-custom-peanut");
    }

    [Fact]
    public void Apply_WhenCharacterIsNotOro_DoesNotAddCustomMove()
    {
        var sourceMove = CreateMove(
            "ken-normals-crouching-roundhouse",
            "ken",
            "Ken",
            "Crouching Roundhouse");

        var moves = _overlay.Apply("ken", [sourceMove]);

        moves.ShouldBe([sourceMove]);
    }

    [Fact]
    public void ApplyRepresentativeFrameOverrides_AddsFrameTwentyTwoOverrideForPeanutMove()
    {
        var sourceMove = CreateMove(
            "oro-normals-crouching-roundhouse",
            "oro",
            "Oro",
            "Crouching Roundhouse");
        var moves = _overlay.Apply("oro", [sourceMove]);

        var policy = _overlay.ApplyRepresentativeFrameOverrides(new RepresentativeFrameSelectionPolicy(), moves);

        var moveOverride = policy.FindOverride("oro", "oro-custom-peanut");
        moveOverride.ShouldNotBeNull();
        moveOverride.SelectedFrame.ShouldBe("22");
        moveOverride.SelectionStrategy.ShouldBeNull();
        moveOverride.OverlayHitboxes.ShouldNotBeNull();
        moveOverride.OverlayHitboxes.ShouldBeEmpty();
    }

    [Fact]
    public void ApplyRepresentativeFrameOverrides_WhenSourceMoveIsInPilotScope_IncludesPeanutMove()
    {
        var sourceMove = CreateMove(
            "oro-normals-crouching-roundhouse",
            "oro",
            "Oro",
            "Crouching Roundhouse");
        var moves = _overlay.Apply("oro", [sourceMove]);

        var policy = _overlay.ApplyRepresentativeFrameOverrides(
            new RepresentativeFrameSelectionPolicy
            {
                PilotMoveScope = ["oro/oro-normals-crouching-roundhouse"]
            },
            moves);

        policy.IsMoveInScope("oro", "oro-custom-peanut").ShouldBeTrue();
        policy.PilotMoveScope.ShouldContain("oro/oro-custom-peanut");
    }

    private static Move CreateMove(
        string id,
        string characterId,
        string characterName,
        string canonicalName,
        string? damage = null)
        => new()
        {
            Id = id,
            CharacterId = characterId,
            Game = "sf3_3s",
            CharacterName = characterName,
            Section = "Normals",
            CanonicalName = canonicalName,
            DisplayOrder = 4,
            SourceMoveId = "22",
            SourceHitboxPath = "hitboxesDisplay.php?iMove=22",
            Damage = damage,
            Stun = "13",
            FrameData = new MoveFrameData
            {
                Startup = "8",
                Active = "4",
                Recovery = "20",
                OnHit = "-2",
                OnBlock = "-8",
                OnCrouchingHit = "D"
            },
            Media =
            [
                new MoveImage
                {
                    Id = $"{id}:representative-active-frame",
                    MoveId = id,
                    StoragePath = $"media/{characterId}/{id}/representative-active-frame.png",
                    SourceUrl = "http://example.test/hitboxesDisplay.php?iMove=22"
                }
            ]
        };
}
