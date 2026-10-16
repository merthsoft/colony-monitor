using HarmonyLib;
using RimWorld.Planet;

namespace Merthsoft.ColonyMonitor.Patches;

[HarmonyPatch(typeof(WorldCameraDriver), "AnythingPreventsCameraMotion", MethodType.Getter)]
internal static class WorldCameraDriver_AnythingPreventsCameraMotion
{
    private static bool Prefix(ref bool __result)
    {
        if (!MonitorWindow.BlocksMainCameraMotion())
            return true;

        __result = true;
        return false;
    }
}
