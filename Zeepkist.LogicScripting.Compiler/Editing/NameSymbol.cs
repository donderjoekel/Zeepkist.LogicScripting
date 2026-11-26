using System;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// What a name in a Script stands for, so two uses of the same text in different scopes tell apart.
/// </summary>
internal sealed class NameSymbol : IEquatable<NameSymbol>
{
    public NameSymbol(NameSymbolKind kind, FunctionDefinition function, string name)
    {
        Kind = kind;
        Function = function;
        Name = name;
    }

    public NameSymbolKind Kind { get; }

    /// <summary>
    /// The Function it belongs to, or is; null for a name defined at the top level.
    /// </summary>
    public FunctionDefinition Function { get; }

    public string Name { get; }

    public bool Equals(NameSymbol other)
    {
        return other != null && Kind == other.Kind && Function == other.Function && Name == other.Name;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as NameSymbol);
    }

    public override int GetHashCode()
    {
        return ((int)Kind * 397) ^ (Name?.GetHashCode() ?? 0);
    }
}
