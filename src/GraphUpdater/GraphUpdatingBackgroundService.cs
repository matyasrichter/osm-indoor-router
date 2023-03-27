namespace GraphUpdater;

using GraphBuilding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public partial class GraphUpdatingBackgroundService : BackgroundService
{
    private readonly ILogger<GraphUpdatingBackgroundService> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly IHostApplicationLifetime applicationLifetime;

    public GraphUpdatingBackgroundService(
        ILogger<GraphUpdatingBackgroundService> logger,
        IServiceProvider serviceProvider,
        IHostApplicationLifetime applicationLifetime
    )
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
        this.applicationLifetime = applicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogGraphUpdaterStarting();
        using (var scope = serviceProvider.CreateScope())
        {
            var updater = scope.ServiceProvider.GetRequiredService<MapProcessor>();
            await updater.Process(stoppingToken);
        }

        LogGraphUpdaterFinished();

        applicationLifetime.StopApplication();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting GraphUpdater")]
    private partial void LogGraphUpdaterStarting();

    [LoggerMessage(Level = LogLevel.Information, Message = "Finished GraphUpdater cycle")]
    private partial void LogGraphUpdaterFinished();
}
