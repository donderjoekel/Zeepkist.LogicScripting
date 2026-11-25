using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Import;

/// <summary>
/// A block already in the level, to write into a Script, with everything it stores that could be a Setting.
/// </summary>
public sealed class ImportedBlock
{
    private const string WireKeyPrefix = "id";
    private const string PaintKeyPrefix = "p";

    /// <param name="uid">The block's UID.</param>
    /// <param name="blockId">The block's ID, which picks its type and style.</param>
    /// <param name="isInBlockGroup">Whether the block is in any Script's Block Group.</param>
    /// <param name="ints">The whole numbers the block stores by key, or null for none.</param>
    /// <param name="texts">The texts the block stores by key, or null for none.</param>
    /// <param name="floats">The other numbers the block stores by key, or null for none.</param>
    public ImportedBlock(
        string uid,
        int blockId,
        bool isInBlockGroup = false,
        IReadOnlyDictionary<string, int> ints = null,
        IReadOnlyDictionary<string, string> texts = null,
        IReadOnlyDictionary<string, float> floats = null)
    {
        Uid = uid;
        BlockId = blockId;
        IsInBlockGroup = isInBlockGroup;
        Settings =
        [
            .. Without(ints, IsReservedInt).Select(i => Setting.Int(i.Key, i.Value)),
            .. Without(texts, IsReservedText).Select(t => Setting.Text(t.Key, t.Value))
        ];
        Floats = floats ?? new Dictionary<string, float>();
    }

    public string Uid { get; }
    public int BlockId { get; }
    public bool IsInBlockGroup { get; }

    /// <summary>
    /// What the block stores, whether or not the Block Manifest knows the key, without its Wires, its paint and the
    /// mod's own keys. A key a fresh Compile wouldn't write is checked, so any new one the game adds counts.
    /// </summary>
    public IReadOnlyList<Setting> Settings { get; }

    /// <summary>
    /// Like <see cref="Settings"/>, for the numbers stored in a way no Setting of the Block Manifest is.
    /// </summary>
    public IReadOnlyDictionary<string, float> Floats { get; }

    /// <summary>
    /// What the block stores for a Setting, none for the value a block placed by hand starts with.
    /// </summary>
    internal IReadOnlyList<Setting> FindValues(BlockSetting setting)
    {
        return Settings.Where(s => s.Key == setting.Key && ImportCompare.GetValue(s) != null).ToList();
    }

    /// <summary>
    /// The UIDs stored for a <see cref="SettingType.Targets"/> Setting.
    /// </summary>
    internal IReadOnlyList<string> FindTargets(BlockSetting setting)
    {
        return GetStoredTargets(setting).Where(t => !string.IsNullOrEmpty(t)).ToList();
    }

    /// <summary>
    /// A Script stores its targets under the first keys, without a gap, and each once.
    /// </summary>
    internal bool HasWritableTargets(BlockSetting setting)
    {
        var targets = FindTargets(setting);
        return targets.Count > 0
            && targets.Distinct().Count() == targets.Count
            && targets.SequenceEqual(GetStoredTargets(setting).Take(targets.Count));
    }

    internal bool HasValue(BlockSetting setting)
    {
        return setting.Type == SettingType.Targets ? FindTargets(setting).Count > 0 : FindValues(setting).Count > 0;
    }

    /// <summary>
    /// The first key with a value a fresh block doesn't store that none of the type's Settings is stored under, or null.
    /// </summary>
    internal string FindUnknownKey(BlockType type)
    {
        return Settings.Where(s => !IsKnown(s, type) && !ImportCompare.IsFresh(s, type))
            .Select(s => s.Key)
            .Concat(Floats.Where(f => f.Value != 0).Select(f => f.Key))
            .FirstOrDefault();
    }

    private static bool IsKnown(Setting stored, BlockType type)
    {
        return type.Settings.Append(BlockSetting.Hide).Any(s => s.Type == SettingType.Targets
            ? stored.Kind == SettingKind.Text && GetTargetKeys(s).Contains(stored.Key)
            : s.Key == stored.Key);
    }

    private IEnumerable<string> GetStoredTargets(BlockSetting setting)
    {
        return GetTargetKeys(setting).Select(k => Settings.FirstOrDefault(s => s.Kind == SettingKind.Text && s.Key == k)?.TextValue);
    }

    private static IEnumerable<string> GetTargetKeys(BlockSetting setting)
    {
        return Enumerable.Range(1, BlockSetting.MaxTargets).Select(i => setting.Key + i.ToString(CultureInfo.InvariantCulture));
    }

    private static IEnumerable<KeyValuePair<string, TValue>> Without<TValue>(
        IReadOnlyDictionary<string, TValue> stored,
        Func<string, bool> isReserved)
    {
        return (stored ?? new Dictionary<string, TValue>()).Where(s => !isReserved(s.Key));
    }

    /// <summary>
    /// Only the keys known to be something else, as any other could be a Setting: how many Wires a Connector has,
    /// <c>id0</c>, and paint, <c>p0</c>.
    /// </summary>
    private static bool IsReservedInt(string key)
    {
        return key == ScriptCarrier.LanguageVersionKey
            || IsNumbered(key, WireKeyPrefix)
            || IsNumbered(key, PaintKeyPrefix);
    }

    /// <summary>
    /// Like <see cref="IsReservedInt"/>: one of a Connector's Wires, <c>id0-0</c>.
    /// </summary>
    private static bool IsReservedText(string key)
    {
        var dash = key.IndexOf('-');
        return key is BlockGroupTag.Key or ScriptCarrier.TextKey or ScriptCarrier.NameKey
            || (dash >= 0 && IsNumbered(key.Substring(0, dash), WireKeyPrefix) && IsNumber(key.Substring(dash + 1)));
    }

    private static bool IsNumbered(string key, string prefix)
    {
        return key.StartsWith(prefix, StringComparison.Ordinal) && IsNumber(key.Substring(prefix.Length));
    }

    private static bool IsNumber(string text)
    {
        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out _);
    }
}
