namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// A Wire as a step writes it: the two blocks by their names in the scenario, and the output and input as a Script
/// writes them.
/// </summary>
internal sealed class WireArgument
{
    public WireArgument(string from, string output, string to, string input)
    {
        From = from;
        Output = output;
        To = to;
        Input = input;
    }

    public string From { get; }
    public string Output { get; }
    public string To { get; }
    public string Input { get; }

    public override string ToString()
    {
        return $"{From} {Output} to {To} {Input}";
    }
}
