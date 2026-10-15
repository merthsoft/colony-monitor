using HarmonyLib;
using Verse;

namespace Merthsoft.ColonyMonitor.Patches;

[HarmonyPatch(typeof(CameraDriver), nameof(CameraDriver.RootSize), MethodType.Getter)]
internal static class CameraDriver_RootSize
{
    private static bool Prefix(ref float __result)
    {
        if (!PreviewContext.Active) 
            return true;
        
        __result = PreviewContext.Current.Size;
        return false;
    }
}
