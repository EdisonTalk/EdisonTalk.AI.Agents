using EDT.Agent.Shared.Constants;

namespace EDT.Agent.Shared.Handlers;

public class OpenAiHttpHandler : HttpClientHandler
{
    private readonly string _openAiProvider;
    private readonly string _openAiBaseAddress;

    public OpenAiHttpHandler(string openAiProvider, string openAiBaseAddress)
    {
        _openAiProvider = openAiProvider;
        _openAiBaseAddress = openAiBaseAddress;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        UriBuilder uriBuilder;
        var uri = new Uri(_openAiBaseAddress);
        if (request.RequestUri?.LocalPath == "v1/chat/completions"
            || request.RequestUri?.LocalPath == "/v1/chat/completions") // Chatting
        {
            switch (_openAiProvider)
            {
                case ConfigConstants.LLMApiProviders.ZhiPuAI: // ZhiPu is not OpenAI-Compatible
                    uriBuilder = new UriBuilder(request.RequestUri)
                    {
                        Scheme = "https",
                        Host = uri.Host,
                        Path = ConfigConstants.LLMApiPaths.ZhiPuAIChatCompletions,
                    };
                    request.RequestUri = uriBuilder.Uri;
                    break;
                default: // Default: OpenAI-Compatible API Providers
                    uriBuilder = new UriBuilder(request.RequestUri)
                    {
                        Scheme = "https",
                        Host = uri.Host,
                        Path = ConfigConstants.LLMApiPaths.OpenAIChatCompletions,
                    };
                    request.RequestUri = uriBuilder.Uri;
                    break;
            }
        }
        else if(request.RequestUri?.LocalPath == "/v1/embeddings"
            || request.RequestUri?.LocalPath == "/v1/embeddings") // Embedding
        {
            uriBuilder = new UriBuilder(request.RequestUri)
            {
                Scheme = "https",
                Host = uri.Host,
                Path = ConfigConstants.LLMApiPaths.OpenAIEmbedding,
            };
            request.RequestUri = uriBuilder.Uri;
        }

        var response = await base.SendAsync(request, cancellationToken);
        return response;
    }
}
