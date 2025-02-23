using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;
using System.ComponentModel;
using System.Text;
using UglyToad.PdfPig;

namespace EDT.PaperAssistant.Portal.Plugins;

public sealed class PaperSummaryPlugin
{
    public IChatClient ChatClient { get; }

    public PaperSummaryPlugin(IConfiguration config)
    {
        var apiKeyCredential = new ApiKeyCredential(config["PaperSummaryModel:ApiKey"] ?? config["OneAPI:ApiKey"]);
        var aiClientOptions = new OpenAIClientOptions();
        aiClientOptions.Endpoint = new Uri(config["PaperSummaryModel:EndPoint"] ?? config["OneAPI:EndPoint"]);
        var aiClient = new OpenAIClient(apiKeyCredential, aiClientOptions)
            .AsChatClient(config["PaperSummaryModel:ModelId"]);
        ChatClient = new ChatClientBuilder(aiClient)
            .UseFunctionInvocation()
            .Build();
    }

    [Description("Read the PDF content from the file path")]
    [return: Description("PDF content")]
    public string ExtractPdfContent(string filePath)
    {
        Console.WriteLine($"[Tool] Now executing {nameof(ExtractPdfContent)}, params: {filePath}");
        var pdfContentBuilder = new StringBuilder();
        using (var document = PdfDocument.Open(filePath))
        {
            foreach (var page in document.GetPages())
                pdfContentBuilder.Append(page.Text);
        }
        return pdfContentBuilder.ToString();
    }

    [Description("Create a markdown note file by file path")]
    public void SaveMarkDownFile([Description("The file path to save")] string filePath, [Description("The content of markdown note")] string content)
    {
        Console.WriteLine($"[Tool] Now executing {nameof(SaveMarkDownFile)}, params: {filePath}, {content}");
        try
        {
            if (!File.Exists(filePath))
                File.WriteAllText(filePath, content);
            else
                File.WriteAllText(filePath, content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] An error occurred: {ex.Message}");
        }
    }

    [Description("Generate one summary of one paper and save the summary to a local file by file path")]
    public async Task GeneratePaperSummary(string sourceFilePath, string destFilePath)
    {
        var pdfContent = this.ExtractPdfContent(sourceFilePath);
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
            new ChatMessage(ChatRole.System, prompt),
            new ChatMessage(ChatRole.User, pdfContent)
        };
        var result = await ChatClient.CompleteAsync(history);
        this.SaveMarkDownFile(destFilePath, result.ToString());
    }
}