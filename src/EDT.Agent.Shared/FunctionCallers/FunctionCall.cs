namespace EDT.Agent.Shared.FunctionCallers;

internal class FunctionCall
{
    public string Name { get; set; }
    public List<FunctionCallParameter> Parameters { get; set; }
}

internal class FunctionCallParameter
{
    public string Name { get; set; }
    public object Value { get; set; }
}