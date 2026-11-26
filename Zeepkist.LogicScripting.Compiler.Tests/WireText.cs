namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// A compiled Wire as text, such as <c>0.0 -> 2.1</c>.
/// </summary>
internal static class WireText
{
    public static string Describe(CompiledWire wire)
    {
        return $"{wire.From}.{wire.FromConnector} -> {wire.To}.{wire.ToConnector}";
    }
}
