using HarmonyLib;
using Verse;

namespace Merthsoft.ColonyMonitor.Patches;

[HarmonyPatch(typeof(CameraDriver), nameof(CameraDriver.CurrentViewRect), MethodType.Getter)]
internal static class CameraDriver_CurrentViewRect
{
    private static bool Prefix(ref CellRect __result)
    {
        if (!PreviewContext.Active) 
            return true;
        
        __result = PreviewContext.Current.ViewRect;
        return false;
    }
}
