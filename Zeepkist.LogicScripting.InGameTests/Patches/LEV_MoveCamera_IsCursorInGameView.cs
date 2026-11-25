using HarmonyLib;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Patches;

/// <summary>
/// The editor reads its grab and duplicate keys only with the mouse over the level. A run can't count on where the
/// player's mouse is, so a key a step presses counts as pressed with it there.
/// </summary>
[HarmonyPatch(typeof(LEV_MoveCamera), nameof(LEV_MoveCamera.IsCursorInGameView))]
internal static class LEV_MoveCamera_IsCursorInGameView
{
    [HarmonyPostfix]
    private static void Postfix(ref bool __result)
    {
        __result |= EditorKeys.IsGizmoKeyPressed;
    }
}
