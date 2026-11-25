using HarmonyLib;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Patches;

/// <summary>
/// Blocks a step drops mustn't count in the player's Steam stats, which a run can't put back.
/// </summary>
[HarmonyPatch(typeof(Steam_TheAchiever), nameof(Steam_TheAchiever.AddIntToStat))]
internal static class Steam_TheAchiever_AddIntToStat
{
    [HarmonyPrefix]
    private static bool Prefix()
    {
        return !EditorKeys.IsGizmoKeyPressed;
    }
}
