using Discord;
using FrameData.Shared.Contracts;

namespace FrameData.Bot.Formatting;

public sealed class MoveEmbedResponseFactory
{
    private static readonly Color SuccessColor = new(52, 152, 219);
    private static readonly Color AmbiguousColor = new(241, 196, 15);
    private static readonly Color ErrorColor = new(231, 76, 60);
    private readonly MoveResponseFormatter _formatter;

    public MoveEmbedResponseFactory(MoveResponseFormatter formatter)
    {
        _formatter = formatter;
    }

    public DiscordMoveResponse Create(MoveQueryResponse response)
    {
        var builder = new EmbedBuilder()
            .WithTitle($"{response.Character} - {response.MatchedMove}")
            .WithColor(SuccessColor)
            .AddField("Section", response.Section, inline: true)
            .AddField("Startup", DisplayValue(response.FrameData.Startup), inline: true)
            .AddField("Active", DisplayValue(response.FrameData.Active), inline: true)
            .AddField("Recovery", DisplayValue(response.FrameData.Recovery), inline: true)
            .AddField("On-Hit", DisplayValue(response.FrameData.OnHit), inline: true)
            .AddField("On-Block", DisplayValue(response.FrameData.OnBlock), inline: true);

        if (!string.IsNullOrWhiteSpace(response.FrameData.FrameAdvantage))
        {
            builder.AddField("Frame Advantage", response.FrameData.FrameAdvantage, inline: true);
        }

        return new DiscordMoveResponse
        {
            Content = LimitMessageContent(_formatter.FormatSuccess(response)),
            Embed = builder.Build()
        };
    }

    public DiscordMoveResponse Create(MoveAmbiguousResponse response)
    {
        var candidates = response.Candidates
            .Select((candidate, index) => $"{index + 1}. {candidate.MoveName} ({candidate.Section}, {candidate.Score:0})");

        var builder = new EmbedBuilder()
            .WithTitle("Multiple moves matched")
            .WithDescription(response.Message)
            .WithColor(AmbiguousColor)
            .AddField("Candidates", LimitFieldValue(string.Join("\n", candidates)));

        return new DiscordMoveResponse
        {
            Content = LimitMessageContent(_formatter.FormatAmbiguous(response)),
            Embed = builder.Build()
        };
    }

    public DiscordMoveResponse Create(ErrorResponse error)
    {
        var content = _formatter.FormatError(error);
        var builder = new EmbedBuilder()
            .WithTitle(ErrorTitle(error.Code))
            .WithDescription(content)
            .WithColor(ErrorColor);

        return new DiscordMoveResponse
        {
            Content = LimitMessageContent(content),
            Embed = builder.Build(),
            IsEphemeral = false
        };
    }

    public DiscordMoveResponse CreateFallbackError()
    {
        return Create(new ErrorResponse
        {
            Code = "error",
            Message = "Unknown error"
        });
    }

    private static string DisplayValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "?" : value;
    }

    private static string ErrorTitle(string code)
    {
        return code switch
        {
            "unsupported_character" => "Unsupported character",
            "move_not_found" => "Move not found",
            _ => "Frame data unavailable"
        };
    }

    private static string LimitMessageContent(string value)
    {
        return Limit(value, 2000);
    }

    private static string LimitFieldValue(string value)
    {
        return Limit(string.IsNullOrWhiteSpace(value) ? "No candidates returned." : value, 1024);
    }

    private static string Limit(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..(maxLength - 3)] + "...";
    }
}
