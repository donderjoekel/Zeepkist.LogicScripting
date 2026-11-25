namespace TNRD.Zeepkist.LogicScripting.Compiler.Import;

/// <summary>
/// A Wire in the level with an end on a block to import, from an output Connector to an input Connector.
/// </summary>
public sealed class ImportedWire
{
    public ImportedWire(string fromUid, int fromConnector, string toUid, int toConnector)
    {
        FromUid = fromUid;
        FromConnector = fromConnector;
        ToUid = toUid;
        ToConnector = toConnector;
    }

    public string FromUid { get; }
    public int FromConnector { get; }
    public string ToUid { get; }
    public int ToConnector { get; }
}
