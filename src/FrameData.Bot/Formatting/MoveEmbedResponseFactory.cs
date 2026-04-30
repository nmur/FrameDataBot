using System.Text.RegularExpressions;
using Discord;
using FrameData.Bot.Hosting;
using FrameData.Shared.Contracts;

namespace FrameData.Bot.Formatting;

public sealed class MoveEmbedResponseFactory
{
    public const string RepositoryUrl = "https://github.com/nmur/FrameDataBot";

    private const string CorrectionIssueTemplate = "frame-data-correction.yml";
    private const int MaxIssueFieldLength = 40;
    private const string SpacerField = "\u200B";
    private static readonly Color SuccessColor = new(52, 152, 219);
    private static readonly Color AmbiguousColor = new(241, 196, 15);
    private static readonly Color ErrorColor = new(231, 76, 60);
    private static readonly Regex ButtonNomenclatureRegex = new(
        @"\b(?:jab|strong|fierce|short|forward|roundhouse|rh)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly IReadOnlyDictionary<string, string> ButtonDisplayNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["jab"] = "LP",
            ["strong"] = "MP",
            ["fierce"] = "HP",
            ["short"] = "LK",
            ["forward"] = "MK",
            ["roundhouse"] = "HK",
            ["rh"] = "HK"
        };
    private readonly string _activeDatasetPath;

    public MoveEmbedResponseFactory()
        : this("/data/framedata/active")
    {
    }

    public MoveEmbedResponseFactory(BotRuntimeOptions options)
        : this(options.ActiveDatasetPath)
    {
    }

    private MoveEmbedResponseFactory(string activeDatasetPath)
    {
        _activeDatasetPath = activeDatasetPath;
    }

    public DiscordMoveResponse Create(MoveQueryResponse response, MoveCorrectionIssueContext? issueContext = null)
    {
        var builder = new EmbedBuilder()
            .WithTitle($"{response.Character} - {DisplayButtonNomenclature(response.MatchedMove)} ({response.Section})")
            .WithColor(SuccessColor);

        AddOptionalField(builder, "Motion", DisplayOptionalButtonNomenclature(response.Motion));

        builder
            .AddField("Damage", DisplayValue(response.Damage), inline: true)
            .AddField("Stun", DisplayValue(response.Stun), inline: true)
            .AddField(SpacerField, SpacerField, inline: true)
            .AddField("Startup", DisplayValue(response.FrameData.Startup), inline: true)
            .AddField("Active", DisplayValue(response.FrameData.Active), inline: true)
            .AddField("Recovery", DisplayValue(response.FrameData.Recovery), inline: true)
            .AddField("On-Hit", DisplayValue(response.FrameData.OnHit), inline: true)
            .AddField("On-Block", DisplayValue(response.FrameData.OnBlock), inline: true)
            .AddField("Frame Advantage", DisplayValue(response.FrameData.FrameAdvantage), inline: true);

        var attachment = CreateAttachment(response.Media);
        if (attachment is not null)
        {
            builder.WithImageUrl($"attachment://{attachment.FileName}");
        }

        return new DiscordMoveResponse
        {
            Embed = builder.Build(),
            Components = CreateComponents(issueContext),
            Attachment = attachment
        };
    }

    public DiscordMoveResponse Create(MoveAmbiguousResponse response, MoveCorrectionIssueContext? issueContext = null)
    {
        var candidates = response.Candidates
            .Select((candidate, index) =>
                $"{index + 1}. {DisplayButtonNomenclature(candidate.MoveName)} ({candidate.Section}, {candidate.Score:0})");

        var builder = new EmbedBuilder()
            .WithTitle("Multiple moves matched")
            .WithDescription(response.Message)
            .WithColor(AmbiguousColor)
            .AddField("Candidates", LimitFieldValue(string.Join("\n", candidates)));

        return new DiscordMoveResponse
        {
            Embed = builder.Build(),
            Components = CreateComponents(issueContext)
        };
    }

    public DiscordMoveResponse Create(ErrorResponse error, MoveCorrectionIssueContext? issueContext = null)
    {
        var builder = new EmbedBuilder()
            .WithTitle(ErrorTitle(error.Code))
            .WithDescription(ErrorDescription(error))
            .WithColor(ErrorColor);

        return new DiscordMoveResponse
        {
            Embed = builder.Build(),
            Components = CreateComponents(issueContext),
            IsEphemeral = false
        };
    }

    public DiscordMoveResponse CreateFallbackError(MoveCorrectionIssueContext? issueContext = null)
    {
        return Create(new ErrorResponse
        {
            Code = "error",
            Message = "Unknown error"
        }, issueContext);
    }

    private static string DisplayValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "?" : value;
    }

    private static string DisplayButtonNomenclature(string value)
    {
        return ButtonNomenclatureRegex.Replace(value, match => ButtonDisplayNames[match.Value]);
    }

    private static string? DisplayOptionalButtonNomenclature(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? value : DisplayButtonNomenclature(value);
    }

    private static void AddOptionalField(EmbedBuilder builder, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.AddField(name, value);
        }
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

    private static string ErrorDescription(ErrorResponse error)
    {
        return error.Code switch
        {
            "unsupported_character" => "Unsupported character. Try a supported character name.",
            "move_not_found" => "Move not found. Try an exact move name or clearer notation.",
            _ => error.Message
        };
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

    private DiscordMoveAttachment? CreateAttachment(MoveMediaContract? media)
    {
        if (string.IsNullOrWhiteSpace(media?.RepresentativeFrameImageUrl))
        {
            return null;
        }

        var relativePath = media.RepresentativeFrameImageUrl.Replace('/', Path.DirectorySeparatorChar);
        var filePath = Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(_activeDatasetPath, relativePath);
        var fileName = Path.GetFileName(relativePath);

        return string.IsNullOrWhiteSpace(fileName)
            ? null
            : new DiscordMoveAttachment(filePath, fileName);
    }

    private static MessageComponent CreateComponents(MoveCorrectionIssueContext? issueContext)
    {
        var builder = new ComponentBuilder()
            .WithButton("GitHub", style: ButtonStyle.Link, url: RepositoryUrl);

        if (issueContext is not null)
        {
            builder.WithButton("Suggest Correction", style: ButtonStyle.Link, url: BuildCorrectionIssueUrl(issueContext));
        }

        return builder.Build();
    }

    public static string BuildCorrectionIssueUrl(MoveCorrectionIssueContext issueContext)
    {
        var character = LimitIssueField(issueContext.Character);
        var moveInput = LimitIssueField(issueContext.MoveInput);
        var command = $"/framedata character:{character} move:{moveInput}";
        var title = $"Frame data correction: Character: `{character}`, Move: `{moveInput}`".Trim();

        var parameters = new Dictionary<string, string>
        {
            ["template"] = CorrectionIssueTemplate,
            ["title"] = title,
            ["command"] = command,
            ["requested-character"] = character,
            ["requested-move"] = moveInput
        };

        return $"{RepositoryUrl}/issues/new?{string.Join("&", parameters.Select(pair => $"{Encode(pair.Key)}={Encode(pair.Value)}"))}";
    }

    private static string LimitIssueField(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
        return normalized.Length <= MaxIssueFieldLength
            ? normalized
            : normalized[..MaxIssueFieldLength];
    }

    private static string Encode(string value)
        => Uri.EscapeDataString(value);
}
