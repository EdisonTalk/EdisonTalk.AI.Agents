using EDT.McpServer.Tools.ConsoleHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

try
{
    Console.WriteLine("Starting MCP Server...");

    var builder = Host.CreateEmptyApplicationBuilder(settings: null);
    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly();

    builder.Services.AddWeatherToolService();

    await builder.Build().RunAsync();
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Host terminated unexpectedly : {ex.Message}");
    return 1;
}