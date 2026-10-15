using HarmonyLib;
using UnityEngine;
using Verse;

namespace Merthsoft.ColonyMonitor.Patches;

[HarmonyPatch(typeof(Find), nameof(Find.Camera), MethodType.Getter)]
internal static class Find_Camera
{
    private static bool Prefix(ref Camera __result)
    {
        if (!PreviewContext.Active) 
            return true;
        
        __result = PreviewContext.Current.Camera;
        return false;
    }
}
