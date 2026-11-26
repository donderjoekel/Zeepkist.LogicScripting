using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Graph;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// What is being typed at the caret. Read from the tokens around it rather than the syntax tree, which drops a
/// half-typed line.
/// </summary>
public sealed partial class CaretContext
{
    private CaretContext(
        string text,
        int caret,
        CaretContextKind kind,
        TextSpan word,
        int statementStart,
        bool canReferAhead,
        string callee,
        string calleeLibrary,
        string dottedName,
        string dottedCallee,
        string dottedLibrary,
        string dottedMode,
        string dottedUid,
        bool isCallNext,
        string settingName,
        IReadOnlyList<string> settingValues,
        IReadOnlyCollection<string> givenArguments,
        int positionalArguments,
        bool isStatementStart,
        IReadOnlyList<DefinedFunction> functions)
    {
        Text = text;
        Caret = caret;
        Kind = kind;
        Word = word;
        StatementStart = statementStart;
        CanReferAhead = canReferAhead;
        Callee = callee;
        CalleeLibrary = calleeLibrary;
        DottedName = dottedName;
        DottedCallee = dottedCallee;
        DottedLibrary = dottedLibrary;
        DottedMode = dottedMode;
        DottedUid = dottedUid;
        IsCallNext = isCallNext;
        SettingName = settingName;
        SettingValues = settingValues;
        GivenArguments = givenArguments;
        PositionalArguments = positionalArguments;
        IsStatementStart = isStatementStart;
        Functions = functions;
        EnclosingFunction = functions.FirstOrDefault(f => f.Contains(caret));
    }

    public string Text { get; }
    public int Caret { get; }
    public CaretContextKind Kind { get; }

    /// <summary>
    /// The name the caret is in or right after, which accepting a completion replaces; empty at the caret when none is
    /// typed yet.
    /// </summary>
    public TextSpan Word { get; }

    /// <summary>
    /// The part of <see cref="Word"/> before the caret, which completions are matched against.
    /// </summary>
    public string Prefix => Text.Substring(Word.Start, Caret - Word.Start);

    /// <summary>
    /// Where the statement the word is in starts; the names defined before it can be used there.
    /// </summary>
    public int StatementStart { get; }

    /// <summary>
    /// Whether the word is inside <c>delay(...)</c>, where a name may refer to a later definition or to the one being
    /// defined.
    /// </summary>
    public bool CanReferAhead { get; }

    /// <summary>
    /// The function or name called by the call whose argument the word is, for <see cref="CaretContextKind.Argument"/>
    /// and <see cref="CaretContextKind.ArgumentName"/>, or whose Setting the word is the value of, for
    /// <see cref="CaretContextKind.SettingValue"/>; null otherwise and after <c>default</c>. For a qualified call,
    /// <c>timing.edge(x)</c>, the Function's name without its Library's, and only with its <see cref="CalleeLibrary"/>.
    /// </summary>
    public string Callee { get; }

    /// <summary>
    /// The Library named in the qualified call whose argument the word is, <c>timing</c> in <c>timing.edge(x)</c>; null
    /// when the <see cref="Callee"/> isn't a Library's Function, and for a call the parser rejects.
    /// </summary>
    public string CalleeLibrary { get; }

    /// <summary>
    /// The name before the dot whose output the word is, for <see cref="CaretContextKind.Output"/>; null otherwise, and
    /// after a call.
    /// </summary>
    public string DottedName { get; }

    /// <summary>
    /// The function called by the call before the dot whose output the word is, for <see cref="CaretContextKind.Output"/>;
    /// null otherwise, and after a name. For a qualified call, <c>timing.edge(x)</c>, the Function's name without its
    /// Library's.
    /// </summary>
    public string DottedCallee { get; }

    /// <summary>
    /// The Library named in the qualified call before the dot, <c>timing</c> in <c>timing.edge(x)</c>; null when the
    /// <see cref="DottedCallee"/> isn't a Library's Function.
    /// </summary>
    public string DottedLibrary { get; }

    /// <summary>
    /// The option written for the mode Setting of the <see cref="DottedCallee"/>'s call (<c>mode: local_velocity</c>);
    /// null when none is written, or the call isn't to a Block Function.
    /// </summary>
    public string DottedMode { get; }

    /// <summary>
    /// The UID in quotes the <c>placed(...)</c> call before the dot takes; null when it takes anything else, or the
    /// <see cref="DottedCallee"/> isn't <c>placed</c>.
    /// </summary>
    public string DottedUid { get; }

    /// <summary>
    /// Whether an opening parenthesis follows the word, so the parser reads it as the function a call calls: after a
    /// name and a dot, a Library's Function rather than an output.
    /// </summary>
    public bool IsCallNext { get; }

    /// <summary>
    /// The Setting whose value the word is, for <see cref="CaretContextKind.SettingValue"/>; null otherwise.
    /// </summary>
    public string SettingName { get; }

    /// <summary>
    /// The words the <see cref="SettingName"/> Setting takes, in the order it stores them; empty for a number.
    /// </summary>
    public IReadOnlyList<string> SettingValues { get; }

    /// <summary>
    /// The named arguments the <see cref="Callee"/>'s call already has, before or after the word, but not the word
    /// itself.
    /// </summary>
    public IReadOnlyCollection<string> GivenArguments { get; }

    /// <summary>
    /// How many positional arguments the <see cref="Callee"/>'s call has, apart from the one the word starts; they
    /// give its first inputs.
    /// </summary>
    public int PositionalArguments { get; }

    /// <summary>
    /// Whether the word is the first on its line, where <c>function</c> and <c>end</c> go.
    /// </summary>
    public bool IsStatementStart { get; }

    /// <summary>
    /// The Functions the text defines, which can be called from anywhere in it; one without a name only gives its body
    /// a scope.
    /// </summary>
    public IReadOnlyList<DefinedFunction> Functions { get; }

    /// <summary>
    /// The Function whose body the caret is in; null at the top level.
    /// </summary>
    public DefinedFunction EnclosingFunction { get; }

    public static CaretContext At(string text, int caret)
    {
        text ??= string.Empty;
        caret = Math.Max(0, Math.Min(caret, text.Length));
        var word = FindWord(text, caret);
        List<TextSpan> comments = [];
        var tokens = Lexer.Tokenize(text, [], comments);
        var call = OpenCall.FindStartedAt(tokens, word.Start);
        var settingValues = FindSettingValues(tokens, word, out var settingCallee, out var settingName);
        var dotted = FindDotted(tokens, word, out var isDottedCall, out var library);
        CaretContextKind kind;
        if (IsInCommentOrString(tokens, comments, caret))
            kind = CaretContextKind.None;
        else if (settingValues != null)
            kind = CaretContextKind.SettingValue;
        else if (word.Length > 0 && Lexer.IsDigit(text[word.Start]))
            kind = CaretContextKind.None;
        else if (IsOnFunctionsFirstLine(tokens, word))
            kind = CaretContextKind.None;
        else if (dotted >= 0)
            kind = CaretContextKind.Output;
        else
            kind = GetKind(tokens, word, call != null);

        var statementStart = FindStatementStart(tokens, word, out var canReferAhead);
        var inCall = kind is CaretContextKind.Argument or CaretContextKind.ArgumentName;
        var calleeLibrary = inCall ? call.Library : null;
        var callee = inCall ? call.Function : null;
        var positionalArguments = 0;
        var isOutput = kind == CaretContextKind.Output;
        var dottedName = isOutput && !isDottedCall ? tokens[dotted].Text : null;
        var dottedCallee = isOutput && isDottedCall ? tokens[dotted].Text : null;
        var dottedLibrary = dottedCallee != null && library >= 0 ? tokens[library].Text : null;
        var dottedMode = dottedCallee != null && dottedLibrary == null && BlockManifest.TryGetFunction(dottedCallee, out var type)
            ? DefinedNames.FindWrittenMode(tokens, dotted + 1, type)
            : null;
        var dottedUid = dottedCallee == GraphBuilder.PlacedFunction && dottedLibrary == null
            ? DefinedNames.FindPlacedUid(tokens, dotted)
            : null;

        IReadOnlyCollection<string> givenArguments = inCall ? FindGivenArguments(call, word, out positionalArguments) : [];
        if (kind == CaretContextKind.SettingValue)
        {
            callee = settingCallee;
        }
        else
        {
            settingName = null;
            settingValues = null;
        }

        return new CaretContext(
            text,
            caret,
            kind,
            word,
            statementStart,
            canReferAhead,
            callee,
            calleeLibrary,
            dottedName,
            dottedCallee,
            dottedLibrary,
            dottedMode,
            dottedUid,
            IsOpenParenNext(tokens, word),
            settingName,
            settingValues,
            givenArguments,
            positionalArguments,
            IsFirstOnLine(tokens, word),
            DefinedNames.ReadFunctions(tokens).Select(f => f.Function).ToList());
    }
}
