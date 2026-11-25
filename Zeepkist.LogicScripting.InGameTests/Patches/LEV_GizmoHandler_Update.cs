using HarmonyLib;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Patches;

[HarmonyPatch(typeof(LEV_GizmoHandler), nameof(LEV_GizmoHandler.Update))]
internal static class LEV_GizmoHandler_Update
{
    [HarmonyPrefix]
    private static void Prefix(LEV_GizmoHandler __instance)
    {
        EditorKeys.BeforeGizmoUpdate(__instance);
    }

    [HarmonyPostfix]
    private static void Postfix(LEV_GizmoHandler __instance)
    {
        EditorKeys.AfterGizmoUpdate(__instance);
    }
}
