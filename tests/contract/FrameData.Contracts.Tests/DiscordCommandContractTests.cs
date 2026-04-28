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
    public void FramedataMoveResponse_UsesEmbedWithTextFallback()
    {
        var factory = new MoveEmbedResponseFactory(new MoveResponseFormatter());

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
                OnBlock = "-6"
            }
        });

        Assert.False(string.IsNullOrWhiteSpace(response.Content));
        Assert.NotNull(response.Embed);
        Assert.Equal("Makoto - Hayate", response.Embed.Title);
        Assert.Contains(response.Embed.Fields, field => field.Name == "Section" && field.Value == "Specials");
        Assert.Contains(response.Embed.Fields, field => field.Name == "Startup" && field.Value == "12");
        Assert.Contains(response.Embed.Fields, field => field.Name == "Active" && field.Value == "3");
        Assert.Contains(response.Embed.Fields, field => field.Name == "Recovery" && field.Value == "21");
        Assert.Contains(response.Embed.Fields, field => field.Name == "On-Hit" && field.Value == "+2");
        Assert.Contains(response.Embed.Fields, field => field.Name == "On-Block" && field.Value == "-6");
    }

    [Fact]
    public void FramedataErrorResponse_KeepsTextFallbackWithErrorEmbed()
    {
        var factory = new MoveEmbedResponseFactory(new MoveResponseFormatter());

        var response = factory.Create(new ErrorResponse
        {
            Code = "unsupported_character",
            Message = "Unsupported character"
        });

        Assert.Equal("Unsupported character. Try a supported character name.", response.Content);
        Assert.NotNull(response.Embed);
        Assert.Equal("Unsupported character", response.Embed.Title);
        Assert.Equal(response.Content, response.Embed.Description);
    }
}
