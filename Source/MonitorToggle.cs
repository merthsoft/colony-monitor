using System.Linq;
using UnityEngine;
using Verse;

namespace Merthsoft.ColonyMonitor;

[StaticConstructorOnStartup]
internal static class MonitorToggle
{
    internal static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("UI/ColonyMonitor/Monitor");

    internal static bool Available => Current.Game != null && Find.Maps.Count > 0;

    internal static void SetVisible(bool visible)
    {
        var window = Find.WindowStack.Windows.OfType<MonitorWindow>().FirstOrDefault();
        ColonyMonitorMod.Settings.Enabled = visible;
        if (visible && window == null) Find.WindowStack.Add(new MonitorWindow());
        if (!visible && window != null) window.Close();
        ColonyMonitorMod.Instance.WriteSettings();
    }

    internal static void Draw(Rect rect)
    {
        MonitorMenu.HandleInput(rect);
        bool enabled = ColonyMonitorMod.Settings.Enabled;
        if (enabled) Widgets.DrawBoxSolid(rect, new Color(0.15f, 0.4f, 0.25f, 0.85f));
        if (Widgets.ButtonImage(rect.ContractedBy(3), Icon)) SetVisible(!enabled);
        TooltipHandler.TipRegion(rect, "Colony Monitor: click to show / hide; right-click for controls.");
    }

}
