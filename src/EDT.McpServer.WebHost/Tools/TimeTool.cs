using ModelContextProtocol.Server;
using System.ComponentModel;

namespace EDT.McpServer.WebHost.Tools;

[McpServerToolType]
public static class TimeTool
{
    [McpServerTool, Description("Get the current time for a city")]
    public static string GetCurrentTime(string city) =>
        $"It is {DateTime.Now.Hour}:{DateTime.Now.Minute} in {city}.";
}
