using FrameData.Domain.Media;
using Shouldly;

namespace FrameData.Ingestion.Tests.Media;

public sealed class RepresentativeFrameSelectionPolicyTests
{
    [Fact]
    public void IsMoveInScope_AcceptsFullMoveKeyOrMoveId()
    {
        var policy = new RepresentativeFrameSelectionPolicy
        {
            PilotMoveScope = ["ken/ken-normals-jab", "ken-normals-strong", "sean", "yun/*"]
        };

        policy.IsMoveInScope("ken", "ken-normals-jab").ShouldBeTrue();
        policy.IsMoveInScope("ken", "ken-normals-strong").ShouldBeTrue();
        policy.IsMoveInScope("ken", "ken-normals-fierce").ShouldBeFalse();
        policy.IsMoveInScope("sean", "sean-normals-jab").ShouldBeTrue();
        policy.IsMoveInScope("yun", "yun-specials-zesshou-hohou").ShouldBeTrue();
    }

    [Fact]
    public void Validate_AcceptsCharacterScopeWhenCharacterHasKnownMoves()
    {
        var policy = new RepresentativeFrameSelectionPolicy
        {
            PilotMoveScope = ["ken", "yun/*"]
        };

        policy.Validate(["ken/ken-normals-jab", "yun/yun-normals-jab"]).ShouldBeEmpty();
    }

    [Fact]
    public void Validate_WhenOverrideHasSelectedFrameAndStrategy_ReturnsError()
    {
        var policy = new RepresentativeFrameSelectionPolicy
        {
            PilotMoveScope = ["ken/ken-normals-jab"],
            MoveOverrides = new Dictionary<string, RepresentativeFrameSelectionOverride>(StringComparer.OrdinalIgnoreCase)
            {
                ["ken/ken-normals-jab"] = new()
                {
                    SelectedFrame = "006",
                    SelectionStrategy = RepresentativeFrameSelectionPolicy.LargestActiveHitboxAreaStrategy
                }
            }
        };

        var errors = policy.Validate(["ken/ken-normals-jab"]);

        errors.Single().ShouldContain("cannot specify both selectedFrame and selectionStrategy");
    }
}
