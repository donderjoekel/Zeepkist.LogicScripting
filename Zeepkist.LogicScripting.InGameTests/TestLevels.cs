using System;
using System.IO;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// Saves and loads level files the way a player does, in the editor's save and load panels. The levels' names start with
/// <see cref="Prefix"/>, by which tools/ingame-test.sh finds them to remove them after the run.
/// </summary>
internal static class TestLevels
{
    public const string Prefix = "zls-ingame-test-";
    private const string LevelsFolder = "Levels";

    public static bool IsPanelOpen(LEV_LevelEditorCentral central)
    {
        return central.saveload.gameObject.activeInHierarchy;
    }

    /// <summary>
    /// Whether the panel is open and knows its folders, which it only does a frame after it first opens.
    /// </summary>
    public static bool IsPanelReady(LEV_LevelEditorCentral central)
    {
        return IsPanelOpen(central) && !string.IsNullOrEmpty(central.saveload.homeDir);
    }

    /// <summary>
    /// Saves from the open save panel, straight into the game's Levels folder, whichever folder the player used last:
    /// the run script clears only that one. Returns null when the game saved no file.
    /// </summary>
    public static FileInfo Save(LEV_LevelEditorCentral central, string name)
    {
        var saveload = central.saveload;
        saveload.GoHome();
        if (saveload.dir?.Name != LevelsFolder)
            throw new InvalidOperationException($"the save panel's home folder is \"{saveload.dir?.FullName}\", not {LevelsFolder}");

        saveload.levelWeJustSaved = null;
        saveload.fileName.text = Prefix + name;
        saveload.SaveFile();
        var saved = saveload.levelWeJustSaved;
        return saved != null && File.Exists(saved.FullName) ? new FileInfo(saved.FullName) : null;
    }

    /// <summary>
    /// Opens the load panel, without saving the level that's open.
    /// </summary>
    public static void OpenLoadPanel(LEV_LevelEditorCentral central)
    {
        central.tool.EnableLoadPanel();
        if (central.unsavedContentPopup.IsOpen)
            central.unsavedContentPopup.Yes();
    }

    public static void Load(LEV_LevelEditorCentral central, FileInfo file)
    {
        central.saveload.SelectThisFile(Path.GetFileNameWithoutExtension(file.Name), file);
        central.saveload.AreYouSure();
    }
}
