using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Import;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;

namespace TNRD.Zeepkist.LogicScripting;

/// <summary>
/// Makes blocks in the level, the selected ones or an Orphaned Group's, the Block Group of a Script that describes them,
/// where they stand, as one undo step.
/// </summary>
internal static class ScriptImport
{
    /// <summary>
    /// What an imported Script is called when nothing else names it, before <see cref="ScriptList.GetFreeName"/>.
    /// </summary>
    public const string DefaultName = "Imported";

    private const string UndoLabel = "Logic Scripting Import";
    private const string NothingSelectedMessage = "Select blocks in the level first";

    /// <summary>
    /// The level editor, or null with the reason no selected blocks can be imported now.
    /// </summary>
    public static LEV_LevelEditorCentral GetCentral(out string blocker)
    {
        var central = LevelEdit.GetCentral(out blocker);
        if (central == null || central.selection.list.Count > 0)
            return central;

        blocker = NothingSelectedMessage;
        return null;
    }

    /// <summary>
    /// Makes the selected blocks a new Script. Returns the Script the level now holds, or null when the level is
    /// unchanged, with the result line either way.
    /// </summary>
    public static StoredScript RunSelected(string name, StartupCheck check, ManualLogSource logger, out string message)
    {
        var central = GetCentral(out message);
        return central == null ? null : Run(central, [.. central.selection.list], ScriptId.New(), name, check, logger, out message);
    }

    /// <summary>
    /// Like <see cref="RunSelected"/>, with the blocks tagged for an Orphaned Group, whatever is selected, and its Script
    /// id, so they keep their tags.
    /// </summary>
    public static StoredScript RunOrphan(ScriptId id, string name, StartupCheck check, ManualLogSource logger, out string message)
    {
        var central = LevelEdit.GetCentral(out message);
        if (central == null)
            return null;

        var group = BlockGroups.Find(central, id);
        if (group.Count > 0)
            return Run(central, group, id, name, check, logger, out message);

        message = "Nothing imported: the group has no blocks in the level";
        return null;
    }

    private static StoredScript Run(
        LEV_LevelEditorCentral central,
        IReadOnlyList<BlockProperties> blocks,
        ScriptId id,
        string name,
        StartupCheck check,
        ManualLogSource logger,
        out string message)
    {
        check.RunOnce();
        if (!check.CanPlace)
        {
            message = $"{StartupCheck.UpdateNeededMessage}, so nothing imported";
            return null;
        }

        var import = BlockImport.Import(
            [.. InTextOrder(blocks).Select(b => Read(b, id))],
            [.. blocks.SelectMany(ReadWires)],
            FindTogglerTargets(central),
            check.DisabledBlockIds,
            (uid, imported) => PlacedBlocks.Find(central, uid, imported.Contains, id));
        if (!import.Succeeded)
        {
            message = $"Nothing imported: {import.Refusal}";
            return null;
        }

        var taken = blocks.Where(b => import.ImportedUids.Contains(b.UID)).ToList();

        // An Orphaned Group's block that stays leaves the group, or the next Compile would replace it
        var left = blocks.Where(b => !import.ImportedUids.Contains(b.UID) && BlockGroups.GetScriptId(b) == id).ToList();
        var script = new StoredScript(id, name, import.Text, ScriptCarrier.LanguageVersion);
        try
        {
            // The carrier's yaw turns the next Compile's layout, so a tilted or scaled block only carries the text when
            // every block is
            var carrier = taken.FirstOrDefault(b => !RecompilePlacement.IsReshaped(b)) ?? taken[0];
            BlockBatch.Change(
                central,
                [.. taken, .. left],
                block =>
                {
                    if (left.Contains(block))
                        LevelScripts.Untag(block.customBlockEditProperties);
                    else
                        TakeIn(block, script, block == carrier);
                },
                UndoLabel);
        }
        catch (Exception e)
        {
            logger.LogError($"Import failed: {e}");
            message = "Import failed, level unchanged";
            return null;
        }

        message = taken.Count == 1
            ? $"Imported 1 block as \"{name}\". The next Compile replaces it with a new layout"
            : $"Imported {taken.Count} blocks as \"{name}\". The next Compile replaces them with a new layout";
        if (import.Kept.Count == 1)
            message += ". 1 stayed as a placed block: it uses settings a Script can't write";
        else if (import.Kept.Count > 1)
            message += $". {import.Kept.Count} stayed as placed blocks: they use settings a Script can't write";

        return script;
    }

    /// <summary>
    /// A pasted copy of another group's block holds that group's tag, and maybe its Script, which would give the new
    /// group a second carrier.
    /// </summary>
    private static void TakeIn(BlockProperties block, StoredScript script, bool isCarrier)
    {
        block.customBlockEditProperties ??= new PropertyDictionariesJSON();
        var properties = block.customBlockEditProperties;
        LevelScripts.Untag(properties);
        LevelScripts.Tag(properties, script.Id, block.UID);
        if (isCarrier)
            LevelScripts.Write(properties, script);
    }

    /// <summary>
    /// The order the lines take where the Wires leave it open, whatever order the blocks were selected in: by place in
    /// the level. A UID is random, so it only decides between blocks at the same place.
    /// </summary>
    private static IEnumerable<BlockProperties> InTextOrder(IEnumerable<BlockProperties> blocks)
    {
        return blocks
            .OrderBy(b => b.transform.position.x)
            .ThenBy(b => b.transform.position.z)
            .ThenBy(b => b.transform.position.y)
            .ThenBy(b => b.UID, StringComparer.Ordinal);
    }

    /// <summary>
    /// A block in the group of the Script with the id, which the import makes the text of, isn't in another's.
    /// </summary>
    private static ImportedBlock Read(BlockProperties block, ScriptId id)
    {
        var properties = block.customBlockEditProperties;
        return new ImportedBlock(
            block.UID,
            block.blockID,
            BlockGroups.IsInOtherGroup(block, id),
            properties?.n,
            properties?.t,
            properties?.f);
    }

    /// <summary>
    /// From both ends, so a Wire only one end stores is found too.
    /// </summary>
    private static IEnumerable<ImportedWire> ReadWires(BlockProperties block)
    {
        foreach (var connector in block.allConnectorHubsOnThisBlock)
        {
            var isInput = connector.Value != null && connector.Value.connectionType == ConnectionTypeEnum.Logic_In;
            foreach (var wire in PlacedBlocks.GetWires(block, connector.Key))
            {
                yield return isInput
                    ? new ImportedWire(wire.targetUID, wire.connectorID, block.UID, connector.Key)
                    : new ImportedWire(block.UID, connector.Key, wire.targetUID, wire.connectorID);
            }
        }
    }

    /// <summary>
    /// Goes through all the level's blocks, so call it on an Import, not per frame.
    /// </summary>
    private static HashSet<string> FindTogglerTargets(LEV_LevelEditorCentral central)
    {
        var targetsSetting = BlockManifest.Toggler.Settings.First(s => s.Type == SettingType.Targets);
        HashSet<string> targets = [];
        foreach (var block in central.undoRedo.allBlocksDictionary.Values)
        {
            if (block == null || BlockManifest.GetPlacedType(block.blockID) != BlockManifest.Toggler)
                continue;

            foreach (var key in targetsSetting.GetTargetKeys())
            {
                if (block.customBlockEditProperties?.t != null && block.customBlockEditProperties.t.TryGetValue(key, out var uid))
                    targets.Add(uid);
            }
        }

        return targets;
    }
}
