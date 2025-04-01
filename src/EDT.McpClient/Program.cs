using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using OpenAI;
using System.ClientModel;

// Connect to an MCP server
Console.WriteLine("Now connecting client to MCP server");
// Method1: TransportType = Standard Trasport IO
//await using var mcpClient = await McpClientFactory.CreateAsync(new()
//{
//    Id = "time",
//    Name = "Time MCP Server",
//    TransportType = TransportTypes.StdIo,
//    TransportOptions = new()
//    {
//        ["command"] = @"..\..\..\..\EDT.McpServer\bin\Debug\net8.0\EDT.McpServer.exe"
//    }
//});
// Method2:  TransportType = SSE
await using var mcpClient = await McpClientFactory.CreateAsync(new()
{
    Id = "time",
    Name = "Time MCP Server",
    TransportType = TransportTypes.Sse,
    Location = "https://localhost:8443/sse"
});
Console.WriteLine("Successfully connected!");

Console.WriteLine("---------------------------------");
// Get all available tools
Console.WriteLine("Tools available:");
var tools = await mcpClient.ListToolsAsync();
foreach (var tool in tools)
{
    Console.WriteLine($"{tool.Name} ({tool.Description})");
}
Console.WriteLine("---------------------------------");

// Execute a tool directly.
Console.WriteLine("Use tool directly in McpClient:");
var result = await mcpClient.CallToolAsync(
    "GetCurrentTime",
    new Dictionary<string, object?>() { ["city"] = "Chengdu" },
    CancellationToken.None);
Console.WriteLine(result.Content.First(c => c.Type == "text").Text);
Console.WriteLine("---------------------------------");

// Driven by LLM tool invocations.
Console.WriteLine("Use tool in ChatClient:");
var config = new ConfigurationBuilder()
    .AddJsonFile($"appsettings.json")
#if DEBUG
    .AddJsonFile($"appsettings.Development.json")
#endif
    .Build();
var apiKeyCredential = new ApiKeyCredential(config["LLM:ApiKey"]);
var aiClientOptions = new OpenAIClientOptions();
aiClientOptions.Endpoint = new Uri(config["LLM:EndPoint"]);
var aiClient = new OpenAIClient(apiKeyCredential, aiClientOptions)
    .AsChatClient(config["LLM:ModelId"]);
var chatClient = new ChatClientBuilder(aiClient)
    .UseFunctionInvocation()
    .Build();
IList<ChatMessage> chatHistory =
[
    new(ChatRole.System, """
                         You are a helpful assistant delivering time in one sentence
                         in a short format, like 'It is 10:08 in Paris, France.'
                         """),
];
// Core Part: Get AI Tools from MCP Server
var mcpTools = await mcpClient.ListToolsAsync();
var chatOptions = new ChatOptions()
{
    Tools = [..mcpTools]
};
// Prompt the user for a question.
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"Assistant> How can I assist you today?");
while (true)
{
    // Read the user question.
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("User> ");
    var question = Console.ReadLine();
    // Exit the application if the user didn't type anything.
    if (!string.IsNullOrWhiteSpace(question) && question.ToUpper() == "EXIT")
        break;

    chatHistory.Add(new ChatMessage(ChatRole.User, question));
    Console.ForegroundColor = ConsoleColor.Green;
    var response = await chatClient.GetResponseAsync(chatHistory, chatOptions);
    var content = response.ToString();
    Console.WriteLine($"Assistant> {content}");
    chatHistory.Add(new ChatMessage(ChatRole.Assistant, content));

    Console.WriteLine();
}

Console.ReadKey();