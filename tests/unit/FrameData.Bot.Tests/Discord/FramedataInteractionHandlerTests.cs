using FrameData.Bot.Api;
using FrameData.Bot.Discord;
using FrameData.Bot.Formatting;
using FrameData.Shared.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace FrameData.Bot.Tests.Discord;

public sealed class FramedataInteractionHandlerTests
{
    private readonly IMoveQueryApiClient _apiClient = Substitute.For<IMoveQueryApiClient>();
    private readonly TestDiscordInteractionResponder _responder = new();

    [Fact]
    public async Task HandleAsync_WhenMoveFound_DefersAndSendsFormattedFollowup()
    {
        _apiClient
            .QueryMoveAsync("makoto", "2mk", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(MoveQueryResponse?, ErrorResponse?)>((new MoveQueryResponse
            {
                Character = "Makoto",
                MatchedMove = "2mk",
                Section = "Normals",
                MatchedBy = "Exact",
                FrameData = new FrameDataContract
                {
                    Startup = "6",
                    Active = "3",
                    Recovery = "17",
                    OnHit = "+1",
                    OnBlock = "-2"
                }
            }, null)));
        var handler = CreateHandler();

        await handler.HandleAsync(
            "framedata",
            [
                new SlashCommandOptionValue("character", "makoto"),
                new SlashCommandOptionValue("move", "2mk")
            ],
            _responder);

        _responder.DeferCount.ShouldBe(1);
        _responder.InitialResponses.ShouldBeEmpty();
        _responder.Followups.Single().ShouldBe("Makoto 2mk (Normals) | Startup 6 Active 3 Recovery 17 OnHit +1 OnBlock -2");
    }

    [Fact]
    public async Task HandleAsync_WhenMoveNotFound_DefersAndSendsErrorFollowup()
    {
        _apiClient
            .QueryMoveAsync("makoto", "unknown", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(MoveQueryResponse?, ErrorResponse?)>((null, new ErrorResponse
            {
                Code = "move_not_found",
                Message = "Move not found"
            })));
        var handler = CreateHandler();

        await handler.HandleAsync(
            "framedata",
            [
                new SlashCommandOptionValue("character", "makoto"),
                new SlashCommandOptionValue("move", "unknown")
            ],
            _responder);

        _responder.DeferCount.ShouldBe(1);
        _responder.Followups.Single().ShouldBe("Move not found. Check spelling or use canonical notation.");
    }

    [Fact]
    public async Task HandleAsync_WhenOptionsInvalid_SendsValidationResponseWithoutCallingApi()
    {
        var handler = CreateHandler();

        await handler.HandleAsync(
            "framedata",
            [new SlashCommandOptionValue("character", "makoto")],
            _responder);

        _responder.DeferCount.ShouldBe(0);
        _responder.InitialResponses.Single().ShouldBe("Character and move are required.");
        await _apiClient.DidNotReceiveWithAnyArgs().QueryMoveAsync(default!, default!, default);
    }

    private FramedataInteractionHandler CreateHandler()
    {
        return new FramedataInteractionHandler(
            new SlashCommandInteractionMapper(),
            _apiClient,
            new MoveResponseFormatter(),
            NullLogger<FramedataInteractionHandler>.Instance);
    }

    private sealed class TestDiscordInteractionResponder : IDiscordInteractionResponder
    {
        public int DeferCount { get; private set; }
        public List<string> InitialResponses { get; } = [];
        public List<string> Followups { get; } = [];

        public Task DeferAsync(bool ephemeral = false)
        {
            DeferCount++;
            return Task.CompletedTask;
        }

        public Task RespondAsync(string content, bool ephemeral = false)
        {
            InitialResponses.Add(content);
            return Task.CompletedTask;
        }

        public Task FollowupAsync(string content, bool ephemeral = false)
        {
            Followups.Add(content);
            return Task.CompletedTask;
        }
    }
}
