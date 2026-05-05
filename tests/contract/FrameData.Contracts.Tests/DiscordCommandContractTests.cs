using Discord;
using FrameData.Bot.Discord;
using FrameData.Bot.Formatting;
using FrameData.Shared.Contracts;

namespace FrameData.Contracts.Tests;

public sealed class DiscordCommandContractTests
{
    [Fact]
    public void FramedataSlashCommand_UsesExpectedCommandNameAndOptions()
    {
        var command = FramedataSlashCommandDefinition.Build();
        var options = command.Options.Value.ToDictionary(option => option.Name);

        Assert.Equal("framedata", command.Name.Value);
        Assert.Equal(2, options.Count);
        Assert.True(options["character"].IsRequired);
        Assert.Equal(ApplicationCommandOptionType.String, options["character"].Type);
        Assert.True(options["move"].IsRequired);
        Assert.Equal(ApplicationCommandOptionType.String, options["move"].Type);
    }

    [Fact]
    public void FramedataMoveResponse_UsesEmbedWithoutMessageContent()
    {
        var factory = new MoveEmbedResponseFactory();

        var response = factory.Create(new MoveQueryResponse
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
                OnBlock = "-6",
                OnCrouchingHit = "+4"
            }
        });

        Assert.Null(response.Content);
        Assert.NotNull(response.Embed);
        Assert.Equal("Makoto - Hayate (Specials)", response.Embed.Title);
        Assert.DoesNotContain(response.Embed.Fields, field => field.Name == "Section");
        Assert.Contains(response.Embed.Fields, field => field.Name == "Damage" && field.Value == "?");
        Assert.Contains(response.Embed.Fields, field => field.Name == "Stun" && field.Value == "?");
        Assert.Contains(response.Embed.Fields, field => field.Name == "Startup" && field.Value == "12");
        Assert.Contains(response.Embed.Fields, field => field.Name == "Active" && field.Value == "3");
        Assert.Contains(response.Embed.Fields, field => field.Name == "Recovery" && field.Value == "21");
        Assert.Contains(response.Embed.Fields, field => field.Name == "On-Hit" && field.Value == "+2");
        Assert.Contains(response.Embed.Fields, field => field.Name == "On-Block" && field.Value == "-6");
        Assert.Contains(response.Embed.Fields, field => field.Name == "Cr. On-Hit" && field.Value == "+4");
    }

    [Fact]
    public void FramedataErrorResponse_UsesErrorEmbedWithoutMessageContent()
    {
        var factory = new MoveEmbedResponseFactory();

        var response = factory.Create(new ErrorResponse
        {
            Code = "unsupported_character",
            Message = "Unsupported character"
        });

        Assert.Null(response.Content);
        Assert.NotNull(response.Embed);
        Assert.Equal("Unsupported character", response.Embed.Title);
        Assert.Equal("Unsupported character. Try a supported character name.", response.Embed.Description);
    }
}
