using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Core;

namespace FrameData.Shared.Logging;

public static class FrameDataLogging
{
    public const string SeqServerUrlEnvironmentVariable = "SEQ_SERVER_URL";
    public const string SeqApiKeyEnvironmentVariable = "SEQ_API_KEY";
    public const string SeqMinimumLevelEnvironmentVariable = "SEQ_MINIMUM_LEVEL";

    public static Logger CreateLogger(IConfiguration configuration, string serviceName)
        => BuildLogger(configuration, serviceName);

    public static void Configure(ILoggingBuilder logging, Serilog.ILogger logger)
    {
        logging.ClearProviders();
        logging.AddSerilog(logger, dispose: false);
    }

    private static Logger BuildLogger(IConfiguration configuration, string serviceName)
    {
        var applicationLevel = ReadLevel(
            configuration,
            "Logging:LogLevel:FrameData",
            SeqMinimumLevelEnvironmentVariable,
            LogEventLevel.Debug);

        var defaultLevel = ReadLevel(
            configuration,
            "Logging:LogLevel:Default",
            null,
            LogEventLevel.Information);

        var microsoftLevel = ReadLevel(
            configuration,
            "Logging:LogLevel:Microsoft",
            null,
            LogEventLevel.Warning);

        var aspNetCoreLevel = ReadLevel(
            configuration,
            "Logging:LogLevel:Microsoft.AspNetCore",
            null,
            LogEventLevel.Information);

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(defaultLevel)
            .MinimumLevel.Override("FrameData", applicationLevel)
            .MinimumLevel.Override("Microsoft", microsoftLevel)
            .MinimumLevel.Override("Microsoft.AspNetCore", aspNetCoreLevel)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", serviceName)
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {ServiceName} {SourceContext} {Message:lj}{NewLine}{Exception}");

        var seqServerUrl = ReadFirstValue(configuration, "Seq:ServerUrl", SeqServerUrlEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(seqServerUrl))
        {
            var seqApiKey = ReadFirstValue(configuration, "Seq:ApiKey", SeqApiKeyEnvironmentVariable);
            loggerConfiguration.WriteTo.Seq(seqServerUrl, apiKey: NullIfWhiteSpace(seqApiKey));
        }

        return loggerConfiguration.CreateLogger();
    }

    private static LogEventLevel ReadLevel(
        IConfiguration configuration,
        string primaryKey,
        string? fallbackKey,
        LogEventLevel defaultLevel)
    {
        var rawValue = fallbackKey is null
            ? configuration[primaryKey]
            : ReadFirstValue(configuration, primaryKey, fallbackKey);

        return Enum.TryParse(rawValue, ignoreCase: true, out LogEventLevel parsed)
            ? parsed
            : defaultLevel;
    }

    private static string? ReadFirstValue(IConfiguration configuration, string primaryKey, string fallbackKey)
    {
        var primaryValue = configuration[primaryKey];
        if (!string.IsNullOrWhiteSpace(primaryValue))
        {
            return primaryValue;
        }

        return configuration[fallbackKey];
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
