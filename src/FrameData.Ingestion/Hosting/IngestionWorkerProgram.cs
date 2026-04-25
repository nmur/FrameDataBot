using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        services.AddLogging(builder => builder.AddSimpleConsole());
        services.AddFrameDataIngestionWorker(options);

        await using var provider = services.BuildServiceProvider();
        try
        {
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
    }
}
