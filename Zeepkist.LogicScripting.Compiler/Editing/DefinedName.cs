using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

public sealed class DefinedName
{
    public DefinedName(
        string name,
        TextSpan span,
        DefinedValue value,
        DefinedFunction function = null,
        bool isInput = false)
    {
        Name = name;
        Span = span;
        Value = value;
        Function = function;
        IsInput = isInput;
    }

    public string Name { get; }

    /// <summary>
    /// Where the name is written in its definition.
    /// </summary>
    public TextSpan Span { get; }

    /// <summary>
    /// Null when the value is no call the editor knows, and for an input.
    /// </summary>
    public DefinedValue Value { get; }

    /// <summary>
    /// The Function whose body defines the name, or whose input it is; null at the top level. Only that body sees it.
    /// </summary>
    public DefinedFunction Function { get; }

    public bool IsInput { get; }

    internal bool IsInScopeAt(int index, IEnumerable<DefinedFunction> functions)
    {
        return Function == null ? !functions.Any(f => f.Contains(index)) : Function.Contains(index);
    }
}
