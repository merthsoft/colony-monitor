using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Merthsoft.ColonyMonitor;

internal sealed class MonitorSettingsPanel
{
    private Vector2 scroll;
    private float height = 1000;

    internal void Draw(MonitorSettings settings, Rect rect)
    {
        Widgets.BeginScrollView(rect, ref scroll, new Rect(0, 0, rect.width - 20, height));
        var list = new Listing_Standard();
        list.Begin(new Rect(0, 0, rect.width - 20, 2000));
        list.CheckboxLabeled("Show monitor", ref settings.Enabled);

        Header(list, "Display");
        Choice(list, "Style", settings.Docked ? "Docked" : "Floating window", new[] {
            new FloatMenuOption("Floating window", () => settings.Docked = false),
            new FloatMenuOption("Docked", () => settings.Docked = true)
        }, "Docked hides the window controls. Right-click the monitor icon to choose a map or view.");
        if (!settings.Docked)
            list.CheckboxLabeled("Follow current map", ref settings.CurrentMapOnly);
        else
        {
            list.CheckboxLabeled("Follow current map", ref settings.DockCurrentMapOnly);
            string[] corners = { "Top left", "Top right", "Bottom left", "Bottom right" };
            list.Label("Corner");
            for (int y = 0; y < 2; y++)
            {
                var row = list.GetRect(32);
                float width = (row.width - 8) / 2;
                for (int x = 0; x < 2; x++)
                {
                    int corner = y * 2 + x;
                    var previous = GUI.color;
                    if (settings.DockCorner == corner) GUI.color = new Color(0.7f, 0.9f, 0.7f);
                    if (Widgets.ButtonText(new Rect(row.x + x * (width + 8), row.y, width, 28), corners[corner]))
                    {
                        settings.DockCorner = corner;
                        settings.DockInsetX = 12;
                        settings.DockInsetY = corner < 2 ? 12 : 190;
                    }
                    GUI.color = previous;
                }
            }
            settings.DockSize = Slider(list, "Size", settings.DockSize, 140, 420, "px");
            settings.DockInsetX = Slider(list, "Horizontal margin", settings.DockInsetX, 0, 1000, "px");
            settings.DockInsetY = Slider(list, "Vertical margin", settings.DockInsetY, 0, 1000, "px");
            if (list.ButtonText("Reset dock layout"))
            {
                settings.DockCorner = 2;
                settings.DockSize = 220;
                settings.DockInsetX = 12;
                settings.DockInsetY = 190;
            }
        }

        Header(list, "Picture");
        Choice(list, "View", settings.Mode.ToString(), new[] {
            new FloatMenuOption("Minimap", () => settings.Mode = PreviewMode.Minimap),
            new FloatMenuOption("Camera", () => settings.Mode = PreviewMode.Camera)
        }, "Minimap shows terrain and buildings. Camera shows the full game view and uses more performance.");
        if (settings.Mode == PreviewMode.Camera)
        {
            Choice(list, "Resolution", $"{settings.CameraResolution} × {settings.CameraResolution}", new[] { 256, 512, 1024 }
                .Select(value => new FloatMenuOption($"{value} × {value}", () => settings.CameraResolution = value)));
            Choice(list, "Refresh rate", $"{settings.CameraFps} FPS", new[] { 1, 2, 5, 10, 15 }
                .Select(value => new FloatMenuOption($"{value} FPS", () => settings.CameraFps = value)),
                "Lower the resolution or refresh rate if the game slows down.");
            list.CheckboxLabeled("Weather effects", ref settings.ShowWeather);
        }
        else
            Choice(list, "Refresh interval", $"{settings.MinimapInterval}s", new[] { 1, 2, 5, 10 }
                .Select(value => new FloatMenuOption($"{value}s", () => settings.MinimapInterval = value)));
        list.CheckboxLabeled("Pawn markers", ref settings.ShowMarkers);

        Header(list, "Monitor icon");
        string[] locations = { "Play-settings row", "Above time controls", "Hidden" };
        Choice(list, "Position", locations[settings.ButtonLocation], Enumerable.Range(0, 3)
            .Select(i => new FloatMenuOption(locations[i], () => settings.ButtonLocation = i)),
            "Left-click to show or hide the monitor. Right-click for controls.");
        if (settings.ButtonLocation == 1)
        {
            list.Label($"Icon size: {settings.ButtonScale:0.0}×");
            settings.ButtonScale = Mathf.Round(list.Slider(settings.ButtonScale, 1, 4) * 2) / 2;
        }

        var monitor = Current.Game == null ? null : Find.WindowStack.Windows.OfType<MonitorWindow>().FirstOrDefault();
        if (monitor != null)
        {
            Header(list, "Controls");
            if (list.ButtonText("Choose map…")) monitor.ShowMapMenu();
            if (settings.Mode == PreviewMode.Camera)
            {
                var row = list.GetRect(30);
                float half = (row.width - 8) / 2;
                if (Widgets.ButtonText(new Rect(row.x, row.y, half, 28), "Zoom out")) monitor.Zoom(1.3f);
                if (Widgets.ButtonText(new Rect(row.x + half + 8, row.y, half, 28), "Zoom in")) monitor.Zoom(1 / 1.3f);
                if (list.ButtonText("Center…")) monitor.ShowCenterMenu();
                if (list.ButtonText("Follow colonist…")) monitor.ShowFollowMenu();
            }
            if (list.ButtonText("Refresh preview")) monitor.RetryPreview();
        }
        height = list.CurHeight + 16;
        list.End();
        Widgets.EndScrollView();
    }

    private static void Header(Listing_Standard list, string label)
    {
        list.Gap(14);
        Text.Font = GameFont.Medium;
        list.Label(label);
        Text.Font = GameFont.Small;
        list.Gap(4);
    }

    private static void Choice(Listing_Standard list, string label, string value,
        IEnumerable<FloatMenuOption> options, string tooltip = null)
    {
        var row = list.GetRect(34);
        float split = row.width * 0.44f;
        Widgets.Label(new Rect(row.x, row.y + 3, split, 28), label);
        if (Widgets.ButtonText(new Rect(row.x + split, row.y, row.width - split, 28), value))
            Find.WindowStack.Add(new FloatMenu(options.ToList()));
        if (tooltip != null) TooltipHandler.TipRegion(row, tooltip);
    }

    private static int Slider(Listing_Standard list, string label, int value, int min, int max, string unit)
    {
        list.Label($"{label}: {value}{unit}");
        return Mathf.RoundToInt(list.Slider(value, min, max));
    }
}
