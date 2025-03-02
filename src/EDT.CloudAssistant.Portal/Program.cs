using EDT.CloudAssistant.Portal.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;

Console.WriteLine("[LOG] Now loading the configuration...");
var config = new ConfigurationBuilder()
    .AddJsonFile($"appsettings.json")
    .Build();
Console.WriteLine("[LOG] Finished loading the configuration...");

Console.WriteLine("[LOG] Now initializing the embeding generator...");
//var generator = new OpenAIClient(new ApiKeyCredential(config["OneAPI:ApiKey"]), new OpenAIClientOptions() { Endpoint = new Uri(config["OneAPI:EndPoint"]) })
//    .AsEmbeddingGenerator(modelId: config["Embedding:ModelId"]);
var generator =
    new OllamaEmbeddingGenerator(new Uri(config["Embedding:EndPoint"]), config["Embedding:Model"]);
Console.WriteLine("[LOG] Finished initializing the embeding generator...");

Console.WriteLine("[LOG] Now loading the vector store - qdrant...");
var vectorStore = new QdrantVectorStore(new QdrantClient(host: config["Qdrant:Host"], port: int.Parse(config["Qdrant:Port"]));
Console.WriteLine("[LOG] Finished loading the vector store - qdrant...");

Console.WriteLine("[LOG] Now creating test data...");
// Get the collection if it exist in qdrant
var cloudServicesStore = vectorStore.GetCollection<ulong, CloudService>("cloudServices");
// Create the collection if it doesn't exist yet.
await cloudServicesStore.CreateCollectionIfNotExistsAsync();
// Define the test data
var cloudServices = new List<CloudService>()
{
    new CloudService
        {
            Key=1,
            Name="Azure App Service",
            Description="Host .NET, Java, Node.js, and Python web applications and APIs in a fully managed Azure service. You only need to deploy your code to Azure. Azure takes care of all the infrastructure management like high availability, load balancing, and autoscaling."
        },
    new CloudService
        {
            Key=2,
            Name="Azure Service Bus",
            Description="A fully managed enterprise message broker supporting both point to point and publish-subscribe integrations. It's ideal for building decoupled applications, queue-based load leveling, or facilitating communication between microservices."
        },
    new CloudService
        {
            Key=3,
            Name="Azure Blob Storage",
            Description="Azure Blob Storage allows your applications to store and retrieve files in the cloud. Azure Storage is highly scalable to store massive amounts of data and data is stored redundantly to ensure high availability."
        },
    new CloudService
        {
            Key=4,
            Name="Microsoft Entra ID",
            Description="Manage user identities and control access to your apps, data, and resources.."
        },
    new CloudService
        {
            Key=5,
            Name="Azure Key Vault",
            Description="Store and access application secrets like connection strings and API keys in an encrypted vault with restricted access to make sure your secrets and your application aren't compromised."
        },
    new CloudService
        {
            Key=6,
            Name="Azure AI Search",
            Description="Information retrieval at scale for traditional and conversational search applications, with security and options for AI enrichment and vectorization."
        }
};
// Insert test data into the collection in qdrant
foreach (var service in cloudServices)
{
    service.Vector = await generator.GenerateEmbeddingVectorAsync(service.Description);
    await cloudServicesStore.UpsertAsync(service);
}
Console.WriteLine("[LOG] Finished creating test data...");

Console.WriteLine("[LOG] Now creating a search from the vector store - qdrant...");
// Generate query embedding
var query = "Which Azure service should I use to store my Word documents?";
var queryEmbedding = await generator.GenerateEmbeddingVectorAsync(query);
// Query from vector data store
var searchOptions = new VectorSearchOptions()
{
    Top = 1,
    VectorPropertyName = "Vector"
};
var results = await cloudServicesStore.VectorizedSearchAsync(queryEmbedding, searchOptions);
await foreach (var result in results.Results)
{
    Console.WriteLine($"Name: {result.Record.Name}");
    Console.WriteLine($"Description: {result.Record.Description}");
    Console.WriteLine($"Vector match score: {result.Score}");
    Console.WriteLine();
}
Console.WriteLine("[LOG] Finished searching from the vector store - qdrant...");
Console.ReadKey();