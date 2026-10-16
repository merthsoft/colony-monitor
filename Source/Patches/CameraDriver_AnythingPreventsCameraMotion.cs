using HarmonyLib;
using Verse;

namespace Merthsoft.ColonyMonitor.Patches;

[HarmonyPatch(typeof(CameraDriver), "AnythingPreventsCameraMotion", MethodType.Getter)]
internal static class CameraDriver_AnythingPreventsCameraMotion
{
    private static bool Prefix(ref bool __result)
    {
        if (!MonitorWindow.BlocksMainCameraMotion())
            return true;

        __result = true;
        return false;
    }
}
