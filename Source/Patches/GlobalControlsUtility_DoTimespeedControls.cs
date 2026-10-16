using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace Merthsoft.ColonyMonitor.Patches;

[HarmonyPatch(typeof(GlobalControlsUtility), nameof(GlobalControlsUtility.DoTimespeedControls))]
internal static class GlobalControlsUtility_DoTimespeedControls
{
    private static void Postfix(float leftX, float width, ref float curBaseY)
    {
        var settings = ColonyMonitorMod.Settings;
        if (!MonitorToggle.Available || settings.ButtonLocation != 1) return;
        float size = Mathf.Min(width, 24f * settings.ButtonScale);
        curBaseY -= size + 4;
        MonitorToggle.Draw(new Rect(leftX + width - size, curBaseY, size, size));
    }
}
