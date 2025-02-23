namespace EDT.Agent.Shared.Constants;

public static class ConfigConstants
{
    public static class LLMApiProviders
    {
        public const string OpenAI = "OpenAI";
        public const string ZhiPuAI = "ZhiPuAI";
        public const string QwenAI = "QwenAI";
        public const string SiliconCloud = "SiliconCloud";
    }

    public static class LLMApiPaths
    {
        public const string OpenAIChatCompletions = "v1/chat/completions";
        public const string ZhiPuAIChatCompletions = "api/paas/v4/chat/completions";
        public const string OpenAIEmbedding = "/v1/embeddings";
    }

    public static class FunctionCallStatus
    {
        public const string Start = "Start";
        public const string Finished = "Finished";
    }
}
