namespace TNRD.Zeepkist.LogicScripting.Storage;

/// <summary>
/// <see cref="Size"/> is along the axis the panel is resized on; zero means it was never resized.
/// </summary>
internal sealed class PanelState
{
    public bool Collapsed { get; set; }
    public float Size { get; set; }

    public void ToggleCollapsed()
    {
        Collapsed = !Collapsed;
    }

    public PanelState Copy()
    {
        return (PanelState)MemberwiseClone();
    }

    public bool IsSameAs(PanelState other)
    {
        return Collapsed == other.Collapsed && Size == other.Size;
    }
}
