namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// One place in a Script's text that defines or uses a <see cref="NameSymbol"/>.
/// </summary>
internal sealed class NameUsage
{
    public NameUsage(NameSymbol symbol, TextSpan span)
    {
        Symbol = symbol;
        Span = span;
    }

    public NameSymbol Symbol { get; }

    public TextSpan Span { get; }

    public string Name => Symbol.Name;
}
