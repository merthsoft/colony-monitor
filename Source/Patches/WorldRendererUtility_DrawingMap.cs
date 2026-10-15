using HarmonyLib;
using RimWorld.Planet;

namespace Merthsoft.ColonyMonitor.Patches;

[HarmonyPatch(typeof(WorldRendererUtility), nameof(WorldRendererUtility.DrawingMap), MethodType.Getter)]
internal static class WorldRendererUtility_DrawingMap
{
    private static bool Prefix(ref bool __result)
    {
        if (!PreviewContext.Active) 
            return true;
        
        __result = true;
        return false;
    }
}
