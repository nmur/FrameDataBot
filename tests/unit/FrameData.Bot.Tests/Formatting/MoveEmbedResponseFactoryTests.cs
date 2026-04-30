using FrameData.Bot.Formatting;
using FrameData.Bot.Hosting;
using FrameData.Shared.Contracts;
using Shouldly;

namespace FrameData.Bot.Tests.Formatting;

public sealed class MoveEmbedResponseFactoryTests
{
    private readonly MoveEmbedResponseFactory _factory = new();

    [Fact]
    public void Create_WhenMoveFound_BuildsFrameDataEmbedWithoutMessageContent()
    {
        var response = _factory.Create(new MoveQueryResponse
        {
            Character = "Makoto",
            MatchedMove = "Hayate",
            Section = "Specials",
            MatchedBy = "Exact",
            FrameData = new FrameDataContract
            {
                Startup = "12",
                Active = "3",
                Recovery = "21",
                OnHit = "+2",
                OnBlock = "-6"
            }
        });

        response.Content.ShouldBeNull();
        response.Embed.ShouldNotBeNull();
        response.Embed.Title.ShouldBe("Makoto - Hayate");
        FieldValue(response, "Section").ShouldBe("Specials");
        FieldValue(response, "Damage").ShouldBe("?");
        FieldValue(response, "Stun").ShouldBe("?");
        FieldValue(response, "Startup").ShouldBe("12");
        FieldValue(response, "Active").ShouldBe("3");
        FieldValue(response, "Recovery").ShouldBe("21");
        FieldValue(response, "On-Hit").ShouldBe("+2");
        FieldValue(response, "On-Block").ShouldBe("-6");
        response.IsEphemeral.ShouldBeFalse();
    }

    [Fact]
    public void Create_WhenMoveHasSourceAttributes_AddsOptionalFields()
    {
        var response = _factory.Create(new MoveQueryResponse
        {
            Character = "Makoto",
            MatchedMove = "Hayate",
            Section = "Specials",
            MatchedBy = "Exact",
            Motion = "236P",
            Damage = "120",
            Stun = "17",
            FrameData = new FrameDataContract
            {
                Startup = "12",
                Active = "3",
                Recovery = "21",
                OnHit = "+2",
                OnBlock = "-6"
            }
        });

        response.Content.ShouldBeNull();
        FieldValue(response, "Motion").ShouldBe("236P");
        FieldValue(response, "Damage").ShouldBe("120");
        FieldValue(response, "Stun").ShouldBe("17");
    }

    [Fact]
    public void Create_WhenMoveHasRepresentativeMedia_AttachesLocalFileReference()
    {
        var factory = new MoveEmbedResponseFactory(new()
        {
            DiscordBotToken = "token",
            BotGuildId = "123",
            DiscordGuildId = 123,
            BotApiBaseUrl = new Uri("http://api:8080"),
            ActiveDatasetPath = "/data/framedata/active"
        });

        var response = factory.Create(new MoveQueryResponse
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
                CaptureStatus = "Success"
            }
        });

        response.Attachment.ShouldNotBeNull();
        response.Attachment.FilePath.ShouldBe("/data/framedata/active/media/ken/ken-normals-jab/representative-active-frame.png");
        response.Attachment.FileName.ShouldBe("representative-active-frame.png");
        response.Embed!.Image!.Value.Url.ShouldBe("attachment://representative-active-frame.png");
    }

    [Fact]
    public void Create_WhenMoveHasRepresentativeMediaWithoutExplicitOptions_UsesDefaultContainerDatasetPath()
    {
        var response = _factory.Create(new MoveQueryResponse
        {
            Character = "Ken",
            MatchedMove = "Jab",
            Section = "Normals",
            MatchedBy = "Exact",
            FrameData = new FrameDataContract(),
            Media = new MoveMediaContract
            {
                RepresentativeFrameImageUrl = "media/ken/ken-normals-jab/representative-active-frame.png",
                CaptureStatus = "Success"
            }
        });

        response.Attachment.ShouldNotBeNull();
        response.Attachment.FilePath.ShouldBe("/data/framedata/active/media/ken/ken-normals-jab/representative-active-frame.png");
    }

    [Fact]
    public void Create_WhenMoveHasNoMedia_DoesNotAttachFile()
    {
        var response = _factory.Create(new MoveQueryResponse
        {
            Character = "Makoto",
            MatchedMove = "Hayate",
            Section = "Specials",
            MatchedBy = "Exact",
            FrameData = new FrameDataContract()
        });

        response.Attachment.ShouldBeNull();
        response.Embed!.Image.ShouldBeNull();
    }

    [Fact]
    public void Create_WhenMoveHasLegacyButtonNames_DisplaysLiteralButtonNamesInTitleAndMotion()
    {
        var response = _factory.Create(new MoveQueryResponse
        {
            Character = "Alex",
            MatchedMove = "Air Knee Smash (RH)",
            Section = "Specials",
            MatchedBy = "Exact",
            Motion = "DP + jab / Strong / Fierce / short / Forward / Roundhouse",
            FrameData = new FrameDataContract
            {
                Startup = "4",
                Active = "2",
                Recovery = "19",
                OnHit = "KD",
                OnBlock = "-10"
            }
        });

        response.Embed!.Title.ShouldBe("Alex - Air Knee Smash (HK)");
        FieldValue(response, "Motion").ShouldBe("DP + LP / MP / HP / LK / MK / HK");
    }

    [Fact]
    public void Create_WhenMoveIsAmbiguous_BuildsCandidateEmbedWithoutMessageContent()
    {
        var response = _factory.Create(new MoveAmbiguousResponse
        {
            Message = "Multiple moves matched. Try a more specific move name.",
            Candidates =
            [
                new MoveCandidate { MoveName = "2hk", Section = "Normals", Score = 100 },
                new MoveCandidate { MoveName = "5hk", Section = "Normals", Score = 94 }
            ]
        });

        response.Content.ShouldBeNull();
        response.Embed.ShouldNotBeNull();
        response.Embed.Title.ShouldBe("Multiple moves matched");
        response.Embed.Description.ShouldBe("Multiple moves matched. Try a more specific move name.");
        FieldValue(response, "Candidates").ShouldContain("1. 2hk (Normals, 100)");
        FieldValue(response, "Candidates").ShouldContain("2. 5hk (Normals, 94)");
    }

    [Fact]
    public void Create_WhenMoveIsAmbiguous_FormatsCandidateMoveNames()
    {
        var response = _factory.Create(new MoveAmbiguousResponse
        {
            Message = "Multiple moves matched. Try a more specific move name.",
            Candidates =
            [
                new MoveCandidate { MoveName = "Crouching Roundhouse", Section = "Normals", Score = 100 },
                new MoveCandidate { MoveName = "Megaton Press (Fierce)", Section = "Super Arts", Score = 94 }
            ]
        });

        FieldValue(response, "Candidates").ShouldContain("1. Crouching HK (Normals, 100)");
        FieldValue(response, "Candidates").ShouldContain("2. Megaton Press (HP) (Super Arts, 94)");
    }

    [Fact]
    public void Create_WhenErrorReturned_BuildsErrorEmbedWithoutMessageContent()
    {
        var response = _factory.Create(new ErrorResponse
        {
            Code = "move_not_found",
            Message = "Move not found"
        });

        response.Content.ShouldBeNull();
        response.Embed.ShouldNotBeNull();
        response.Embed.Title.ShouldBe("Move not found");
        response.Embed.Description.ShouldBe("Move not found. Try an exact move name or clearer notation.");
    }

    private static string FieldValue(DiscordMoveResponse response, string name)
    {
        return response.Embed!.Fields.Single(field => field.Name == name).Value;
    }
}
