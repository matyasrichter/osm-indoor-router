namespace Routing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ports;

public class GraphHolderReloadBackgroundService : BackgroundService
{
    private readonly GraphHolder graphHolder;
    private readonly IServiceProvider serviceProvider;

    public GraphHolderReloadBackgroundService(
        GraphHolder graphHolder,
        IServiceProvider serviceProvider
    )
    {
        this.graphHolder = graphHolder;
        this.serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var loader = scope.ServiceProvider.GetRequiredService<IGraphLoadingPort>();
                await graphHolder.LoadGraph(loader);
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
