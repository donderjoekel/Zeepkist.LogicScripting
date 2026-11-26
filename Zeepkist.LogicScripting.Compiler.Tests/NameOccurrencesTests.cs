using System.Collections.Generic;
using System.Linq;
using System.Text;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Texts mark the caret with <c>|</c>, and each place expected to be underlined with <c>[</c> and <c>]</c> around it.
/// </summary>
public class NameOccurrencesTests
{
    [Fact]
    public void Definition_UnderlinesEachUse()
    {
        AssertUsages("|x = 1\ny = logic([x], [x])\npower_display([x])");
    }

    [Fact]
    public void Use_UnderlinesTheDefinitionAndTheOtherUses()
    {
        AssertUsages("[x] = 1\ny = logic(|x, [x])");
    }

    [Fact]
    public void CaretAtEitherEdgeOfTheName_Counts()
    {
        AssertUsages("[abc] = 1\npower_display(|abc)");
        AssertUsages("[abc] = 1\npower_display(abc|)");
        AssertUsages("[abc] = 1\npower_display(a|bc)");
    }

    [Fact]
    public void CaretOffAnyName_UnderlinesNothing()
    {
        AssertUsages("x = 1\npower_display(x)| ");
        AssertUsages("x = 1\npower_display(x)\n|");
        AssertUsages("x = |1\npower_display(x)");
    }

    [Fact]
    public void UndefinedName_UnderlinesNothing()
    {
        AssertUsages("y = logic(|nope, nope)");
    }

    [Fact]
    public void DefinedTwice_UnderlinesBothDefinitions()
    {
        AssertUsages("[x] = 1\n[x] = 2\npower_display(|x)");
    }

    [Fact]
    public void SameNameInAFunctionAndAtTheTopLevel_AreDifferentNames()
    {
        AssertUsages("|x = 1\nfunction f() -> y\n    x = 2\n    y = x\nend\npower_display([x])");
        AssertUsages("x = 1\nfunction f() -> y\n    [x] = 2\n    y = |x\nend\npower_display(x)");
    }

    [Fact]
    public void SameNameInTwoFunctions_AreDifferentNames()
    {
        AssertUsages("function f() -> y\n    y = 1\nend\nfunction g() -> [y]\n    |y = 2\nend");
    }

    [Fact]
    public void FunctionInput_UnderlinesItsUsesInTheBody()
    {
        AssertUsages("x = 1\nfunction f(|x) -> y\n    y = logic([x], [x])\nend\npower_display(x)");
        AssertUsages("function f([x]) -> y\n    y = |x\nend");
    }

    [Fact]
    public void FunctionOutput_IsTheSameNameAsItsDefinitionInTheBody()
    {
        AssertUsages("function f(x) -> |y\n    [y] = x\nend");
        AssertUsages("function f(x) -> [y]\n    |y = x\nend");
    }

    [Fact]
    public void FunctionOutputNotDefinedInItsBody_UnderlinesNothing()
    {
        AssertUsages("function f(x) -> |y\nend");
    }

    [Fact]
    public void FunctionNameAndItsCalls_AreNotNames()
    {
        AssertUsages("f = 1\nfunction |f(x) -> y\n    y = x\nend\nz = f(1)");
        AssertUsages("[f] = 1\nfunction f(x) -> y\n    y = x\nend\nz = f([f])\npower_display(|f)");
    }

    [Fact]
    public void PlacedBlockReferenceCall_Counts()
    {
        AssertUsages("|lamp_1 = placed(\"abc\")\n[lamp_1](a: 1)");
    }

    [Fact]
    public void Output_AfterADot_IsNotTheName()
    {
        AssertUsages("|p = player_input()\nhorn = 1\nd = logic([p].horn, horn)");
        AssertUsages("p = player_input()\n|horn = 1\nd = logic(p.horn, [horn])");
    }

    [Fact]
    public void LibraryQualifier_IsNotTheName()
    {
        AssertUsages("|timing = 1\nd = timing.edge([timing])");
    }

    [Fact]
    public void ArgumentName_IsNotTheName()
    {
        AssertUsages("|a = 1\nd = logic(a: [a], b: 1)");
    }

    [Fact]
    public void BlockFunctionInputValue_Counts()
    {
        AssertUsages("|x = 1\nd = logic(a: [x], b: 1)");
    }

    [Fact]
    public void ScriptFunctionArgumentValue_Counts()
    {
        AssertUsages("|x = 1\nfunction f(a) -> y\n    y = a\nend\nd = f(a: [x])");
    }

    [Fact]
    public void SettingOptionWord_IsNotTheName()
    {
        AssertUsages("|and = 1\nd = logic(a: [and], b: 1, op: and)");
        AssertUsages("|hide = 1\ndefault hide: hide\nd = logic([hide], 1)");
    }

    [Fact]
    public void ArgumentValueOnTheNextLine_IsToldLikeOnTheSameLine()
    {
        AssertUsages("|x = 1\nd = logic(a:\n    [x], b: 1)");
        AssertUsages("|and = 1\nd = logic([and], 1, op:\n    and)");
    }

    [Fact]
    public void SettingValueSpellingAnInputOfTheFunction_Counts()
    {
        AssertUsages("function f(x, |ticks) -> y\n    y = delay(x, ticks: [ticks])\nend");
        AssertUsages("|ticks = 1\ny = delay([ticks], ticks: ticks)");
    }

    [Fact]
    public void StringsAndComments_AreNotTheName()
    {
        AssertUsages("|x = 1\n# x is here\nd = placed(\"x\")\npower_display([x])");
    }

    [Fact]
    public void HalfTypedLine_StillUnderlines()
    {
        AssertUsages("[a] = generator(power: 1)\nx = logic(|a, ");
    }

    [Fact]
    public void ReservedWord_UnderlinesNothing()
    {
        AssertUsages("x = |true\ny = true");
    }

    [Fact]
    public void Library_InputsAndDefinitionsAreUnderlined()
    {
        AssertUsages("function edge(|x) -> rising\n    last = delay([x], ticks: 1)\n    rising = logic([x], last, op: and)\nend");
    }

    private static void AssertUsages(string marked)
    {
        var (text, caret, expected) = Parse(marked);
        var actual = NameOccurrences.At(text, caret).Select(s => (s.Start, s.Length)).ToList();

        Assert.Equal(expected, actual);
    }

    private static (string Text, int Caret, List<(int Start, int Length)> Expected) Parse(string marked)
    {
        StringBuilder text = new();
        var caret = -1;
        var open = -1;
        List<(int, int)> expected = [];
        foreach (var character in marked)
        {
            switch (character)
            {
                case '|':
                    caret = text.Length;
                    break;

                case '[' when open < 0:
                    open = text.Length;
                    break;

                case ']' when open >= 0:
                    expected.Add((open, text.Length - open));
                    open = -1;
                    break;

                default:
                    text.Append(character);
                    break;
            }
        }

        return (text.ToString(), caret, expected);
    }
}
