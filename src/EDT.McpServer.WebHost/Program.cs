using EDT.McpServer.WebHost.Tools;
using ModelContextProtocol.AspNetCore;

try
{
    Console.WriteLine("Starting MCP Server...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddMcpServer().WithToolsFromAssembly();
    builder.Services.AddWeatherToolService();
    var app = builder.Build();

    app.UseHttpsRedirection();
    app.MapGet("/", () => "Hello MCP Server!");
    app.MapMcp();

    app.Run();
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Host terminated unexpectedly : {ex.Message}");
    return 1;
}