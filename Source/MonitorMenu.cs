using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Merthsoft.ColonyMonitor;

internal static class MonitorMenu
{
    internal static void HandleInput(Rect rect)
    {
        var ev = Event.current;
        if (ev.type != EventType.MouseDown || ev.button != 1 || !rect.Contains(ev.mousePosition)) return;
        ev.Use();
        Show();
    }

    private static void Show()
    {
        var settings = ColonyMonitorMod.Settings;
        var monitor = Find.WindowStack.Windows.OfType<MonitorWindow>().FirstOrDefault();
        var options = new List<FloatMenuOption>();
        if (monitor != null && settings.Mode == PreviewMode.Camera)
        {
            options.Add(new("Zoom in", () => monitor.Zoom(1 / 1.3f)));
            options.Add(new("Zoom out", () => monitor.Zoom(1.3f)));
            options.Add(new("Center…", monitor.ShowCenterMenu));
            options.Add(new("Follow colonist…", monitor.ShowFollowMenu));
        }
        if (monitor != null)
        {
            options.Add(new("Choose map…", monitor.ShowMapMenu));
            options.Add(new("Refresh preview", monitor.RetryPreview));
        }
        options.Add(new(settings.Mode == PreviewMode.Minimap ? "Camera view" : "Minimap view", () =>
        {
            settings.Mode = settings.Mode == PreviewMode.Minimap ? PreviewMode.Camera : PreviewMode.Minimap;
            ColonyMonitorMod.Instance.WriteSettings();
        }));
        options.Add(new(settings.Enabled ? "Hide monitor" : "Show monitor", () => MonitorToggle.SetVisible(!settings.Enabled)));
        options.Add(new("Settings", () => Find.WindowStack.Add(new MonitorSettingsWindow())));
        Find.WindowStack.Add(new FloatMenu(options));
    }
}
