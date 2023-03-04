namespace Routing;

using Graph;
using Microsoft.Extensions.Logging;
using Ports;

public partial class GraphHolder
{
    private readonly IGraphLoadingPort graphLoadingPort;
    private readonly ILogger<GraphHolder> logger;

    private Guid? currentVersion;
    public IGraph? Graph { get; private set; }

    public GraphHolder(IGraphLoadingPort graphLoadingPort, ILogger<GraphHolder> logger)
    {
        this.graphLoadingPort = graphLoadingPort;
        this.logger = logger;
    }

    public async Task LoadGraph()
    {
        var newVersion = await graphLoadingPort.GetCurrentGraphVersion();
        if (newVersion is null)
        {
            LogNoCurrentGraphVersionFound();
            return;
        }

        if (newVersion == currentVersion)
        {
            return;
        }

        Graph = await graphLoadingPort.GetGraph(newVersion.Value);
        if (Graph == null)
        {
            LogNoGraphFoundForVersion(newVersion.Value);
            return;
        }

        currentVersion = newVersion;

        LogLoadedGraphVersion(currentVersion.Value);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "No current graph version found")]
    private partial void LogNoCurrentGraphVersionFound();

    [LoggerMessage(Level = LogLevel.Warning, Message = "No graph found for version {Version}")]
    private partial void LogNoGraphFoundForVersion(Guid version);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded graph version {Version}")]
    private partial void LogLoadedGraphVersion(Guid version);
}
