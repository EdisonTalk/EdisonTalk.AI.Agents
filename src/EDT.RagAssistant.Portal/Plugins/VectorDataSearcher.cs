using EDT.RagAssistant.Portal.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using System.ComponentModel;

namespace EDT.RagAssistant.Portal.Plugins;

public class VectorDataSearcher<TKey> where TKey : notnull
{
    private readonly IVectorStoreRecordCollection<TKey, TextSnippet<TKey>> _vectorStoreRecordCollection;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public VectorDataSearcher(IVectorStoreRecordCollection<TKey, TextSnippet<TKey>> vectorStoreRecordCollection, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _vectorStoreRecordCollection = vectorStoreRecordCollection;
        _embeddingGenerator = embeddingGenerator;
    }

    [Description("Get top N text search results from vector store by user's query (N is 1 by default)")]
    [return: Description("Collection of text search result")]
    public async Task<IEnumerable<TextSearchResult>> GetTextSearchResults(string query, int topN = 1)
    {
        var queryEmbedding = await _embeddingGenerator.GenerateEmbeddingVectorAsync(query);
        // Query from vector data store
        var searchOptions = new VectorSearchOptions()
        {
            Top = topN,
            VectorPropertyName = nameof(TextSnippet<TKey>.TextEmbedding)
        };
        var searchResults = await _vectorStoreRecordCollection.VectorizedSearchAsync(queryEmbedding, searchOptions);
        var responseResults = new List<TextSearchResult>();
        await foreach (var result in searchResults.Results)
        {
            responseResults.Add(new TextSearchResult()
            {
                Value = result.Record.Text ?? string.Empty,
                Link = result.Record.ReferenceLink ?? string.Empty,
                Score = result.Score
            });
        }

        return responseResults;
    }
}