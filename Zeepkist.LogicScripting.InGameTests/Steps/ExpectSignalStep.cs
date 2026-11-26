using System.Collections;
using System.Globalization;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Checks the Signal the input got in the last Tick.
/// </summary>
internal sealed class ExpectSignalStep : ScenarioStep
{
    public ExpectSignalStep(string name, string input, long signal)
    {
        Name = name;
        Input = input;
        Signal = signal;
    }

    public string Name { get; }
    public string Input { get; }
    public long Signal { get; }

    public override IEnumerator Run(StepContext context)
    {
        var running = context.RequireRunningBlock(Name);
        if (running == null)
            yield break;

        var input = NamedBlock.FindConnector(Name, "input", running.Type.Inputs, Input, out var error);
        if (input == null)
        {
            context.Fail(error);
            yield break;
        }

        var signals = running.Script.newInputState;
        if (input.Index >= signals.Count)
        {
            context.Fail($"\"{Name}\" keeps no Signal for its input {Input}");
            yield break;
        }

        var expected = Signal.ToString(CultureInfo.InvariantCulture);
        var actual = signals[input.Index].ToString(CultureInfo.InvariantCulture);
        if (signals[input.Index] == Signal)
            context.Pass($"\"{Name}\"'s input {Input} is {actual}", expected, actual);
        else
            context.Fail($"\"{Name}\"'s input {Input} is {actual}", expected, actual);
    }
}
