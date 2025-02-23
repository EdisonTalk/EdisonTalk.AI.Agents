using EDT.PaperAssistant.Portal.Plugins;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;
using System.Text;

Console.WriteLine("Now loading the configuration...");
var config = new ConfigurationBuilder()
    .AddJsonFile($"appsettings.json")
    .Build();
Console.WriteLine("Now loading the chat client...");
var apiKeyCredential = new ApiKeyCredential(config["MainChatModel:ApiKey"] ?? config["OneAPI:ApiKey"]);
var aiClientOptions = new OpenAIClientOptions();
aiClientOptions.Endpoint = new Uri(config["MainChatModel:EndPoint"] ?? config["OneAPI:EndPoint"]);
var aiClient = new OpenAIClient(apiKeyCredential, aiClientOptions)
    .AsChatClient(config["MainChatModel:ModelId"]);
var chatClient = new ChatClientBuilder(aiClient)
    .UseFunctionInvocation()
    .Build();
Console.WriteLine("Now loading the plugins...");
var plugins = new PaperSummaryPlugin(config);
var chatOptions = new ChatOptions()
{
    Tools =
    [
      AIFunctionFactory.Create(plugins.ExtractPdfContent),
      AIFunctionFactory.Create(plugins.SaveMarkDownFile),
      AIFunctionFactory.Create(plugins.GeneratePaperSummary)
    ]
};
Console.WriteLine("Now starting chatting...");
var prompt = """
             You're one smart agent for reading the content of a PDF paper and summarizing it into a markdown note.
             User will provide the path of the paper and the path to create the note.
             Please make sure the file path is in the following format:
             "D:\Documents\xxx.pdf"
             "D:\Documents\xxx.md"
             Please summarize the abstract, introduction, literature review, main points, research methods, results, and conclusion of the paper.
             The tile should be 《[Title]》, Authour should be [Author] and published in [Year].
             Please make sure the summary should include the following:
             (1) Abstrat
             (2) Introduction
             (3) Literature Review
             (4) Main Research Questions and Background
             (5) Research Methods and Techniques Used
             (6) Main Results and Findings
             (7) Conclusion and Future Research Directions
             """;
var history = new List<ChatMessage>
{
    new ChatMessage(ChatRole.System, prompt)
};
bool isComplete = false;
Console.WriteLine("AI> I'm Ready! What can I do for you?");
var result = new StringBuilder();
do
{
    Console.Write("You> ");
    string? input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input.Trim().Equals("EXIT", StringComparison.OrdinalIgnoreCase))
    {
        isComplete = true;
        break;
    }

    if (input.Trim().Equals("Clear", StringComparison.OrdinalIgnoreCase))
    {
        history.Clear();
        Console.WriteLine("Cleared our chatting history successfully!");
        continue;
    }

    history.Add(new ChatMessage(ChatRole.User, input));
    Console.Write("AI> ");
    result.Clear();
    await foreach (var message in chatClient.CompleteStreamingAsync(input, chatOptions))
    {
        result.Append(message);
        Console.Write(message);
    }
    Console.WriteLine();
    history.Add(new ChatMessage(ChatRole.Assistant, result.ToString() ?? string.Empty));
} while (!isComplete);