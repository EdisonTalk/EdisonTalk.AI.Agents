using EDT.RagAssistant.Portal.Models;
using EDT.RagAssistant.Portal.Plugins;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using OpenAI;
using Qdrant.Client;
using System.ClientModel;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

Console.WriteLine("[LOG] Now loading the configuration...");
var config = new ConfigurationBuilder()
    .AddJsonFile($"appsettings.json")
#if DEBUG
    .AddJsonFile($"appsettings.Development.json")
#endif
    .Build();
Console.WriteLine("[LOG] Finished loading the configuration...");

Console.WriteLine("[LOG] Now loading the chat client...");
var apiKeyCredential = new ApiKeyCredential(config["LLM:ApiKey"]);
var aiClientOptions = new OpenAIClientOptions();
aiClientOptions.Endpoint = new Uri(config["LLM:EndPoint"]);
var aiClient = new OpenAIClient(apiKeyCredential, aiClientOptions)
    .AsChatClient(config["LLM:ModelId"]);
var chatClient = new ChatClientBuilder(aiClient)
    .UseFunctionInvocation()
    .Build();
Console.WriteLine("[LOG] Finished loading the chat client...");

Console.WriteLine("[LOG] Now initializing the embeding generator...");
var embedingGenerator =
    new OllamaEmbeddingGenerator(new Uri(config["Embeddings:Ollama:EndPoint"]), config["Embeddings:Ollama:ModelId"]);
Console.WriteLine("[LOG] Finished initializing the embeding generator...");

Console.WriteLine("[LOG] Now loading the vector store on qdrant...");
var vectorStore = 
    new QdrantVectorStore(new QdrantClient(host: config["VectorStores:Qdrant:Host"], port: int.Parse(config["VectorStores:Qdrant:Port"]), apiKey: config["VectorStores:Qdrant:ApiKey"]));
Console.WriteLine("[LOG] Finished loading the vector store on qdrant...");

//Console.WriteLine("[LOG] Now intializing the data loader for pdf...");
var ragConfig = config.GetSection("RAG");
// Get the unique key genrator
var uniqueKeyGenerator = new UniqueKeyGenerator<Guid>(() => Guid.NewGuid());
// Get the collection in qdrant
var ragVectorRecordCollection = vectorStore.GetCollection<Guid, TextSnippet<Guid>>(ragConfig["CollectionName"]);
// Get the PDF loader
var pdfLoader = new PdfDataLoader<Guid>(uniqueKeyGenerator, ragVectorRecordCollection, embedingGenerator);
Console.WriteLine("[LOG] Finished intializing the data loader for pdf...");

Console.WriteLine("[LOG] Now loading the PDF data into vector store...");
var pdfFilePath = ragConfig["PdfFileFolder"];
var pdfFiles = Directory.GetFiles(pdfFilePath);
try
{
    foreach (var pdfFile in pdfFiles)
    {
        Console.WriteLine($"[LOG] Start Loading PDF into vector store: {pdfFile}");
        await pdfLoader.LoadPdf(
            pdfFile,
            int.Parse(ragConfig["DataLoadingBatchSize"]),
            int.Parse(ragConfig["DataLoadingBetweenBatchDelayInMilliseconds"]));
        Console.WriteLine($"[LOG] Finished Loading PDF into vector store: {pdfFile}");
    }
    Console.WriteLine($"[LOG] All PDFs loaded into vector store succeed!");
}
catch (Exception ex)
{
    Console.WriteLine($"[ERROR] Failed to load PDFs: {ex.Message}");
    return;
}
finally
{
    Console.WriteLine("[LOG] Finished loading the PDF data into vector store...");
}

Console.WriteLine("[LOG] Now starting the chatting window for you...");
Console.ForegroundColor = ConsoleColor.Green;
var promptTemplate = """
                    请使用下面的提示使用工具从向量数据库中获取相关信息来回答用户提出的问题：
                    {{#with (SearchPlugin-GetTextSearchResults question)}}  
                      {{#each this}}  
                        Value: {{Value}}
                        Link: {{Link}}
                        Score: {{Score}}
                        -----------------
                      {{/each}}
                    {{/with}}

                    输出要求：请在回复中引用相关信息的地方包括对相关信息的引用。
                    
                    用户问题: {{question}}
                    """;
var chatHistory = new List<ChatMessage>
{
    new ChatMessage(ChatRole.System, "你是一个专业的AI聊天机器人，为易速鲜花网站的所有员工提供信息咨询服务。")
};
var vectorSearchTool = new VectorDataSearcher<Guid>(ragVectorRecordCollection, embedingGenerator);
var chatOptions = new ChatOptions()
{
    Tools =
    [
      AIFunctionFactory.Create(vectorSearchTool.GetTextSearchResults)
    ]
};
// Prompt the user for a question.
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"助手> 今天有什么可以帮到你的?");
while (true)
{
    // Read the user question.
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("用户> ");
    var question = Console.ReadLine();
    // Exit the application if the user didn't type anything.
    if (!string.IsNullOrWhiteSpace(question) && question.ToUpper() == "EXIT")
        break;

    var ragPrompt = promptTemplate.Replace("{question}", question);
    chatHistory.Add(new ChatMessage(ChatRole.User, ragPrompt));
    // Invoke the LLM with a template that uses the search plugin to
    // 1. get related information to the user query from the vector store
    // 2. add the information to the LLM prompt.
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("系统> 思考整理中...");
    var result = await chatClient.GetResponseAsync(chatHistory, chatOptions);
    var response = result.ToString();
    Console.WriteLine($"助手> {response}");
    chatHistory.Add(new ChatMessage(ChatRole.Assistant, response));

    Console.WriteLine();
}

Console.ReadKey();