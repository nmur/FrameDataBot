using Discord;
using FrameData.Bot.Api;
using FrameData.Bot.Discord;
using FrameData.Bot.Formatting;
using FrameData.Shared.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace FrameData.Bot.Tests.Discord;

public sealed class FramedataInteractionHandlerEmbedResponseTests
{
    private readonly IMoveQueryApiClient _apiClient = Substitute.For<IMoveQueryApiClient>();

    [Fact]
    public async Task HandleAsync_WhenMoveFound_SendsEmbedAndTextFallback()
    {
        _apiClient
            .QueryMoveAsync("makoto", "hayate", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(MoveQueryResponse?, MoveAmbiguousResponse?, ErrorResponse?)>((new MoveQueryResponse
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
            }, null, null)));
        var responder = new TestDiscordInteractionResponder();
        var handler = CreateHandler();

        await handler.HandleAsync(
            "framedata",
            [
                new SlashCommandOptionValue("character", "makoto"),
                new SlashCommandOptionValue("move", "hayate")
            ],
            responder);

        responder.DeferCount.ShouldBe(1);
        var followup = responder.Followups.Single();
        followup.Content.ShouldBe("Makoto Hayate (Specials) | Startup 12 Active 3 Recovery 21 OnHit +2 OnBlock -6");
        followup.Embed.ShouldNotBeNull();
        followup.Embed.Title.ShouldBe("Makoto - Hayate");
        followup.Ephemeral.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenApiThrows_SendsContentOnlyFallback()
    {
        _apiClient
            .QueryMoveAsync("makoto", "hayate", Arg.Any<CancellationToken>())
            .Returns<Task<(MoveQueryResponse?, MoveAmbiguousResponse?, ErrorResponse?)>>(_ => throw new InvalidOperationException("API down"));
        var responder = new TestDiscordInteractionResponder();
        var handler = CreateHandler();

        await handler.HandleAsync(
            "framedata",
            [
                new SlashCommandOptionValue("character", "makoto"),
                new SlashCommandOptionValue("move", "hayate")
            ],
            responder);

        var followup = responder.Followups.Single();
        followup.Content.ShouldBe("Unable to query frame data right now. Try again shortly.");
        followup.Embed.ShouldBeNull();
    }

    private FramedataInteractionHandler CreateHandler()
    {
        return new FramedataInteractionHandler(
            new SlashCommandInteractionMapper(),
            _apiClient,
            new MoveEmbedResponseFactory(new MoveResponseFormatter()),
            NullLogger<FramedataInteractionHandler>.Instance);
    }

    private sealed class TestDiscordInteractionResponder : IDiscordInteractionResponder
    {
        public int DeferCount { get; private set; }
        public List<SentInteractionResponse> Followups { get; } = [];

        public Task DeferAsync(bool ephemeral = false)
        {
            DeferCount++;
            return Task.CompletedTask;
        }

        public Task RespondAsync(string content, Embed? embed = null, bool ephemeral = false)
        {
            return Task.CompletedTask;
        }

        public Task FollowupAsync(string content, Embed? embed = null, bool ephemeral = false)
        {
            Followups.Add(new SentInteractionResponse(content, embed, ephemeral));
            return Task.CompletedTask;
        }
    }

    private sealed record SentInteractionResponse(string Content, Embed? Embed, bool Ephemeral);
}
