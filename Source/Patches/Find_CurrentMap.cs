using HarmonyLib;
using Verse;

namespace Merthsoft.ColonyMonitor.Patches;

[HarmonyPatch(typeof(Find), nameof(Find.CurrentMap), MethodType.Getter)]
internal static class Find_CurrentMap
{
    private static bool Prefix(ref Map __result)
    {
        if (!PreviewContext.Active) 
            return true;
        
        __result = PreviewContext.Current.Map;
        return false;
    }
}
