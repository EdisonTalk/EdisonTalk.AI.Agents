using EDT.McpServer.WebHost.Extensions;
using EDT.McpServer.WebHost.Tools;
using ModelContextProtocol;

try
{
    Console.WriteLine("Starting MCP Server...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddMcpServer().WithToolsFromAssembly();
    builder.Services.AddWeatherToolService();
    var app = builder.Build();

    app.UseHttpsRedirection();
    app.MapGet("/", () => "Hello MCP Server!");
    app.MapMcpSse();

    app.Run();
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Host terminated unexpectedly : {ex.Message}");
    return 1;
}