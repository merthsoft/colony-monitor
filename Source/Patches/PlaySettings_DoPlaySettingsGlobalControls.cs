using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Merthsoft.ColonyMonitor.Patches;

[HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
public static class PlaySettings_DoPlaySettingsGlobalControls
{
    public static void Postfix(WidgetRow row)
    {
        if (Current.Game == null || Find.Maps.Count == 0)
            return;

        if (!row.ButtonText("Monitor"))
            return;

        var window = Find.WindowStack.Windows.OfType<MonitorWindow>().FirstOrDefault();
        
        if (window != null) 
            window.Close();
        else 
            Find.WindowStack.Add(new MonitorWindow());
    }
}
