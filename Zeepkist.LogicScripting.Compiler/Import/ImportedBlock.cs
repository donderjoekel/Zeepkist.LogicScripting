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
    private const char PaintKeyPrefix = 'p';

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
            .. WithoutReserved(ints).Select(i => Setting.Int(i.Key, i.Value)),
            .. WithoutReserved(texts).Select(t => Setting.Text(t.Key, t.Value))
        ];
        Floats = WithoutReserved(floats).ToDictionary(f => f.Key, f => f.Value);
    }

    public string Uid { get; }
    public int BlockId { get; }
    public bool IsInBlockGroup { get; }

    /// <summary>
    /// What the block stores, whether or not the Block Manifest knows the key, without its Wires, its paint and the
    /// mod's own keys.
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
        return GetTargetKeys(setting).Where(t => !string.IsNullOrEmpty(t)).ToList();
    }

    /// <summary>
    /// A Script stores its targets under the first keys, without a gap, and each once.
    /// </summary>
    internal bool HasWritableTargets(BlockSetting setting)
    {
        var targets = FindTargets(setting);
        return targets.Count > 0
            && targets.Distinct().Count() == targets.Count
            && targets.SequenceEqual(GetTargetKeys(setting).Take(targets.Count));
    }

    private IEnumerable<string> GetTargetKeys(BlockSetting setting)
    {
        for (var i = 1; i <= BlockSetting.MaxTargets; i++)
        {
            var key = setting.Key + i.ToString(CultureInfo.InvariantCulture);
            yield return Settings.FirstOrDefault(s => s.Kind == SettingKind.Text && s.Key == key)?.TextValue;
        }
    }

    private static IEnumerable<KeyValuePair<string, TValue>> WithoutReserved<TValue>(IReadOnlyDictionary<string, TValue> stored)
    {
        return (stored ?? new Dictionary<string, TValue>()).Where(s => !IsReserved(s.Key));
    }

    private static bool IsReserved(string key)
    {
        return key.StartsWith(WireKeyPrefix, StringComparison.Ordinal)
            || key is BlockGroupTag.Key or ScriptCarrier.TextKey or ScriptCarrier.NameKey or ScriptCarrier.LanguageVersionKey
            || (key.Length > 1 && key[0] == PaintKeyPrefix && int.TryParse(key.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out _));
    }
}
