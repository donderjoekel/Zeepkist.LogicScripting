using System.Collections;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

internal sealed class ExpectDisplayStep : ScenarioStep
{
    public ExpectDisplayStep(string name, string text)
    {
        Name = name;
        Text = text;
    }

    public string Name { get; }
    public string Text { get; }

    public override IEnumerator Run(StepContext context)
    {
        var running = context.RequireRunningBlock(Name);
        if (running == null)
            yield break;

        if (running.Script is not LogicScript_SignalPowerDisplay display)
        {
            context.Fail($"\"{Name}\" is a {running.Type.Function}, not a power_display");
            yield break;
        }

        if (display.actuallyDontDrawText || display.powerLabel == null)
        {
            context.Fail($"\"{Name}\" draws no number");
            yield break;
        }

        var shown = display.powerLabel.text.Trim();
        if (shown == Text)
            context.Pass($"\"{Name}\" shows {shown}", Text, shown);
        else
            context.Fail($"\"{Name}\" shows {shown}", Text, shown);
    }
}
