using EDT.RagAssistant.Portal.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;

namespace EDT.RagAssistant.Portal.Plugins;

public sealed class PdfDataLoader<TKey> where TKey : notnull
{
    private readonly IVectorStoreRecordCollection<TKey, TextSnippet<TKey>> _vectorStoreRecordCollection;
    private readonly UniqueKeyGenerator<TKey> _uniqueKeyGenerator;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public PdfDataLoader(
        UniqueKeyGenerator<TKey> uniqueKeyGenerator,
        IVectorStoreRecordCollection<TKey, TextSnippet<TKey>> vectorStoreRecordCollection,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _vectorStoreRecordCollection = vectorStoreRecordCollection;
        _uniqueKeyGenerator = uniqueKeyGenerator;
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task LoadPdf(string pdfPath, int batchSize, int betweenBatchDelayInMs)
    {
        // Create the collection if it doesn't exist.
        await _vectorStoreRecordCollection.CreateCollectionIfNotExistsAsync();

        // Load the text and images from the PDF file and split them into batches.
        var sections = LoadAllTexts(pdfPath);
        var batches = sections.Chunk(batchSize);

        // Process each batch of content items.
        foreach (var batch in batches)
        {
            // Get text contents
            var textContentTasks = batch.Select(async content =>
            {
                if (content.Text != null)
                    return content;

                return new RawContent { Text = string.Empty, PageNumber = content.PageNumber };
            });
            var textContent = (await Task.WhenAll(textContentTasks))
                .Where(c => !string.IsNullOrEmpty(c.Text))
                .ToList();

            // Map each paragraph to a TextSnippet and generate an embedding for it.
            var recordTasks = textContent.Select(async content => new TextSnippet<TKey>
            {
                Key = _uniqueKeyGenerator.GenerateKey(),
                Text = content.Text,
                ReferenceDescription = $"{new FileInfo(pdfPath).Name}#page={content.PageNumber}",
                ReferenceLink = $"{new Uri(new FileInfo(pdfPath).FullName).AbsoluteUri}#page={content.PageNumber}",
                TextEmbedding = await _embeddingGenerator.GenerateEmbeddingVectorAsync(content.Text!)
            });

            // Upsert the records into the vector store.
            var records = await Task.WhenAll(recordTasks);
            var upsertedKeys = _vectorStoreRecordCollection.UpsertBatchAsync(records);
            await foreach (var key in upsertedKeys)
            {
                Console.WriteLine($"Upserted record '{key}' into VectorDB");
            }

            await Task.Delay(betweenBatchDelayInMs);
        }
    }

    private static IEnumerable<RawContent> LoadAllTexts(string pdfPath)
    {
        using (PdfDocument document = PdfDocument.Open(pdfPath))
        {
            foreach (Page page in document.GetPages())
            {
                var blocks = DefaultPageSegmenter.Instance.GetBlocks(page.GetWords());
                foreach (var block in blocks)
                    yield return new RawContent { Text = block.Text, PageNumber = page.Number };
            }
        }
    }
}