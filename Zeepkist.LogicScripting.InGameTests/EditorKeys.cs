namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// Presses one of the level editor's keys for one frame, through the input action the editor reads it from, and lets
/// the press go again once the part of the editor that reads it has.
/// </summary>
internal static class EditorKeys
{
    private static EditorKey _pressed;

    /// <summary>
    /// Whether the editor still has to read the key pressed last.
    /// </summary>
    public static bool IsPressed => _pressed != EditorKey.None;

    /// <summary>
    /// Whether the key is one <see cref="LEV_GizmoHandler"/> reads, instead of <see cref="LEV_UndoRedo"/>.
    /// </summary>
    public static bool IsGizmoKeyPressed => _pressed is EditorKey.Grab or EditorKey.Duplicate;

    public static void Press(EditorKey key)
    {
        _pressed = key;
    }

    public static void Release()
    {
        _pressed = EditorKey.None;
    }

    public static void BeforeUndoRedoUpdate(LEV_UndoRedo undoRedo)
    {
        if (!IsGizmoKeyPressed)
            SetDown(undoRedo.central);
    }

    public static void AfterUndoRedoUpdate(LEV_UndoRedo undoRedo)
    {
        if (!IsGizmoKeyPressed)
            SetUp(undoRedo.central);
    }

    public static void BeforeGizmoUpdate(LEV_GizmoHandler gizmos)
    {
        if (IsGizmoKeyPressed)
            SetDown(gizmos.central);
    }

    public static void AfterGizmoUpdate(LEV_GizmoHandler gizmos)
    {
        if (IsGizmoKeyPressed)
            SetUp(gizmos.central);
    }

    private static void SetDown(LEV_LevelEditorCentral central)
    {
        var action = GetAction(central);
        if (action != null)
            action.buttonDown = true;
    }

    private static void SetUp(LEV_LevelEditorCentral central)
    {
        var action = GetAction(central);
        if (action == null)
            return;

        action.buttonDown = false;
        Release();
    }

    private static InputActionScriptableObject GetAction(LEV_LevelEditorCentral central)
    {
        return _pressed switch
        {
            EditorKey.Undo => central.input.Undo,
            EditorKey.Redo => central.input.Redo,
            EditorKey.Grab => central.input.GizmoGridToggle,
            EditorKey.Duplicate => central.input.Dublicate,
            _ => null,
        };
    }
}
