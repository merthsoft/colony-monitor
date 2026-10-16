using HarmonyLib;
using RimWorld;
using Verse;

namespace Merthsoft.ColonyMonitor.Patches;

[HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
public static class PlaySettings_DoPlaySettingsGlobalControls
{
    public static void Postfix(WidgetRow row)
    {
        if (!MonitorToggle.Available || ColonyMonitorMod.Settings.ButtonLocation != 0)
            return;
        bool enabled = ColonyMonitorMod.Settings.Enabled;
        bool previous = enabled;
        MonitorMenu.HandleInput(row.ButtonIconRect(24f));
        row.ToggleableIcon(ref enabled, MonitorToggle.Icon, "Colony Monitor: click to show / hide; right-click for controls");
        if (enabled != previous) MonitorToggle.SetVisible(enabled);
    }
}
