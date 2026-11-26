namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

internal enum NameSymbolKind
{
    /// <summary>
    /// A name defined at the top level, a Placed Block Reference included.
    /// </summary>
    TopLevel,

    /// <summary>
    /// A name a Function's body defines that isn't one of its outputs.
    /// </summary>
    Local,

    Function,
    Input,
    Output
}
