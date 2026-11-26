using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Block IDs and blocks as a level stores them, for the import tests. The IDs are written out rather than read from the
/// Block Manifest, so a wrong ID there fails the tests.
/// </summary>
internal static class ImportedBlocks
{
    public const int DoorId = 1;
    public const int TriggerId = 2413;
    public const int GeneratorId = 2414;
    public const int DisplayId = 2415;
    public const int MathId = 2416;
    public const int TogglerId = 2417;
    public const int DigitizerId = 2418;
    public const int LogicId = 2420;
    public const int RandomId = 2425;
    public const int PassthroughId = 2426;
    public const int DeathId = 2427;
    public const int DelayId = 2428;
    public const int CounterId = 2434;
    public const int TelemetryId = 2440;
    public const int SensorId = 2442;
    public const int TimerId = 2444;
    public const int ForceGiverId = 2446;
    public const int LampId = 2448;
    public const int MultiplierId = 2449;
    public const int Math3PinsId = 2453;
    public const int CompactDisplayId = 2513;

    public static ImportedBlock Trigger(string uid = "t") => new(uid, TriggerId);
    public static ImportedBlock Display(string uid = "p") => new(uid, DisplayId);
    public static ImportedBlock Logic(string uid = "l") => new(uid, LogicId);
    public static ImportedBlock Lamp(string uid = "lamp") => new(uid, LampId);

    /// <summary>
    /// A Generator, with no power stored when <paramref name="power"/> is null.
    /// </summary>
    public static ImportedBlock Generator(string uid = "g", string power = null)
    {
        return new ImportedBlock(uid, GeneratorId, texts: power == null ? null : new Dictionary<string, string> { ["lbp"] = power });
    }

    public static ImportedBlock Delay(string uid, int ticks)
    {
        return new ImportedBlock(uid, DelayId, texts: new Dictionary<string, string> { ["ldl"] = $"{ticks}" });
    }

    public static ImportedBlock Toggler(string uid, params string[] targets)
    {
        return new ImportedBlock(
            uid,
            TogglerId,
            texts: Enumerable.Range(0, targets.Length).ToDictionary(i => $"xuid{i + 1}", i => targets[i]));
    }
}
