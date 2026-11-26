using System.Collections;
using System.Globalization;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// Stores a Setting no Script can give, such as an input's override, on a block <c>place-block ... as &lt;name&gt;</c>
/// placed, by the key and the way the block stores it. <see cref="SetBlockSettingStep"/> sets the others.
/// </summary>
internal sealed class SetStoredSettingStep : ScenarioStep
{
    public SetStoredSettingStep(string name, StoredKind kind, string key, string value)
    {
        Name = name;
        Kind = kind;
        Key = key;
        Value = value;
    }

    public string Name { get; }
    public StoredKind Kind { get; }
    public string Key { get; }

    /// <summary>
    /// As the Scenario writes it, so a number in the invariant culture.
    /// </summary>
    public string Value { get; }

    public override IEnumerator Run(StepContext context)
    {
        var central = context.RequireEditor();
        if (central == null)
            yield break;

        var named = context.RequireNamedBlock(central, Name);
        if (named == null)
            yield break;

        var block = named.Block;
        var before = central.undoRedo.currentHistoryPosition;
        if (!context.EditSettings(named, Write))
            yield break;

        yield return null;

        var kept = Read(block.customBlockEditProperties);
        if (kept != Value)
        {
            context.Fail($"the block didn't keep {Kind} \"{Key}\"", Value, kept);
            yield break;
        }

        context.Detail = $"{named.Type.Function} \"{Name}\": {Kind} \"{Key}\" = {Value}, "
            + $"history position {before} → {central.undoRedo.currentHistoryPosition}";
    }

    private void Write(PropertyDictionariesJSON properties)
    {
        switch (Kind)
        {
            case StoredKind.Int:
                properties.SetIntDictionaryValue(Key, int.Parse(Value, CultureInfo.InvariantCulture));
                break;

            case StoredKind.Float:
                properties.SetFloatDictionaryValue(Key, float.Parse(Value, CultureInfo.InvariantCulture));
                break;

            default:
                properties.SetStringDictionaryValue(Key, Value);
                break;
        }
    }

    private string Read(PropertyDictionariesJSON properties)
    {
        return Kind switch
        {
            StoredKind.Int => properties.GetIntDictionaryValue(Key).ToString(CultureInfo.InvariantCulture),
            StoredKind.Float => properties.GetFloatDictionaryValue(Key).ToString(CultureInfo.InvariantCulture),
            _ => properties.GetStringDictionaryValue(Key)
        };
    }
}
