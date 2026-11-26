namespace TNRD.Zeepkist.LogicScripting.Compiler.Import;

internal sealed class ImportLine
{
    public ImportLine(string uid, string text)
    {
        Uid = uid;
        Text = text;
    }

    /// <summary>
    /// The block the line places, refers to or wires into, or null when it's about all of them.
    /// </summary>
    public string Uid { get; }

    public string Text { get; }
}
