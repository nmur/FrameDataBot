using FrameData.Shared.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FrameData.Ingestion.Hosting;

public static class IngestionWorkerProgram
{
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        IngestionWorkerCommand command;
        try
        {
            command = IngestionWorkerCommand.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return IngestionWorkerExitCodes.ConfigurationError;
        }

        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddCommandLine(command.ConfigurationArgs.ToArray())
            .Build();

        var options = IngestionWorkerOptions.FromConfiguration(configuration, command);
        var validationErrors = options.Validate();
        if (validationErrors.Count > 0)
        {
            foreach (var validationError in validationErrors)
            {
                Console.Error.WriteLine(validationError);
            }

            return IngestionWorkerExitCodes.ConfigurationError;
        }

        var services = new ServiceCollection();
        services.AddLogging(builder => FrameDataLogging.Configure(builder, configuration, "FrameData.Ingestion"));
        services.AddFrameDataIngestionWorker(options);

        try
        {
            await using var provider = services.BuildServiceProvider();
            return await provider.GetRequiredService<IngestionWorker>().ExecuteAsync(cancellationToken);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return IngestionWorkerExitCodes.ConfigurationError;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return IngestionWorkerExitCodes.Failure;
        }
        finally
        {
            FrameDataLogging.CloseAndFlush();
        }
    }
}
