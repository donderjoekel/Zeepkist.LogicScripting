namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

public readonly struct GameConnector
{
    public GameConnector(int index, ConnectorDirection direction)
    {
        Index = index;
        Direction = direction;
    }

    public int Index { get; }
    public ConnectorDirection Direction { get; }
}
