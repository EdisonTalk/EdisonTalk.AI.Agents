namespace EDT.Agent.Shared.Models;

public class QueryModel
{
    public string Index { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;

    public QueryModel(string index, string text)
    {
        Index = index;
        Text = text;
    }

    public QueryModel(string text)
    {
        Text = text;
    }
}