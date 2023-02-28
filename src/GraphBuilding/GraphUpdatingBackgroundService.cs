namespace GraphBuilding;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public partial class GraphUpdatingBackgroundService : BackgroundService
{
    private readonly ILogger<GraphUpdatingBackgroundService> logger;
    private readonly IServiceProvider serviceProvider;

    public GraphUpdatingBackgroundService(
        ILogger<GraphUpdatingBackgroundService> logger,
        IServiceProvider serviceProvider
    )
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            LogGraphUpdaterStarting();
            using (var scope = serviceProvider.CreateScope())
            {
                var updater = scope.ServiceProvider.GetRequiredService<GraphUpdater>();
                await updater.UpdateGraph(stoppingToken);
            }

            LogGraphUpdaterFinished();

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }


    [LoggerMessage(Level = LogLevel.Information, Message = "Starting GraphUpdater")]
    private partial void LogGraphUpdaterStarting();

    [LoggerMessage(Level = LogLevel.Information, Message = "Finished GraphUpdater cycle")]
    private partial void LogGraphUpdaterFinished();
}
