using EDT.Agent.Shared.Constants;

namespace EDT.Agent.Shared.Configurations;

public class OpenAiConfiguration
{
    public string Provider { get; set; }
    public string ModelId { get; set; }
    public string EndPoint { get; set; }
    public string ApiKey { get; set; }

    public OpenAiConfiguration(string modelId, string endPoint, string apiKey)
    {
        Provider = ConfigConstants.LLMApiProviders.OpenAI; // Default OpenAI-Compatible LLM API Provider
        ModelId = modelId;
        EndPoint = endPoint;
        ApiKey = apiKey;
    }

    public OpenAiConfiguration(string provider, string modelId, string endPoint, string apiKey)
    {
        Provider = provider;
        ModelId = modelId;
        EndPoint = endPoint;
        ApiKey = apiKey;
    }
}