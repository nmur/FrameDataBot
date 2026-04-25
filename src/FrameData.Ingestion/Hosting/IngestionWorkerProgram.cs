using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FrameData.Ingestion.Hosting;

public static class IngestionWorkerProgram
{
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        var options = IngestionWorkerOptions.FromConfiguration(configuration);
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
