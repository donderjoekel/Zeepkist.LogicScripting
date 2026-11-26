namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal readonly struct Token
{
    public Token(TokenKind kind, string text, Position position, TextSpan span, bool isUnclosed = false)
    {
        Kind = kind;
        Text = text;
        Position = position;
        Span = span;
        IsUnclosed = isUnclosed;
    }

    public Token(BinaryOperator binaryOperator, UnaryOperator unaryOperator, Position position, TextSpan span)
        : this(TokenKind.Operator, binaryOperator?.Symbol ?? unaryOperator.Symbol, position, span)
    {
        BinaryOperator = binaryOperator;
        UnaryOperator = unaryOperator;
    }

    public TokenKind Kind { get; }
    public string Text { get; }
    public Position Position { get; }

    /// <summary>
    /// The characters the token covers in the text; a string's include its quotes, which <see cref="Text"/> leaves
    /// out.
    /// </summary>
    public TextSpan Span { get; }

    /// <summary>
    /// An opening parenthesis with no closing one to match it.
    /// </summary>
    public bool IsUnclosed { get; }

    public BinaryOperator BinaryOperator { get; }

    /// <summary>
    /// <c>-</c> sets both this and <see cref="BinaryOperator"/>.
    /// </summary>
    public UnaryOperator UnaryOperator { get; }

    public bool IsDirectlyAfter(Token other)
    {
        return Span.Start == other.Span.End;
    }
}
