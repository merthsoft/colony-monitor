using System;
using UnityEngine;
using Verse;

namespace Merthsoft.ColonyMonitor;

public enum PreviewMode { Minimap, Camera }

public sealed class MonitorSettings : ModSettings
{
    public PreviewMode Mode = PreviewMode.Minimap;
    public int CameraResolution = 512;
    public int CameraFps = 2;
    public int MinimapInterval = 2;
    public bool ShowMarkers = true;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref Mode, "previewMode", PreviewMode.Minimap);
        Scribe_Values.Look(ref CameraResolution, "cameraResolution", 512);
        Scribe_Values.Look(ref CameraFps, "cameraFps", 2);
        Scribe_Values.Look(ref MinimapInterval, "minimapInterval", 2);
        Scribe_Values.Look(ref ShowMarkers, "showMarkers", true);
        if (Mode != PreviewMode.Minimap && Mode != PreviewMode.Camera) Mode = PreviewMode.Minimap;
        CameraResolution = CameraResolution <= 256 ? 256 : CameraResolution <= 512 ? 512 : 1024;
        CameraFps = Math.Clamp(CameraFps, 1, 15);
        MinimapInterval = Math.Clamp(MinimapInterval, 1, 10);
    }

    public void Draw(Rect rect)
    {
        var list = new Listing_Standard();
        list.Begin(rect);
        list.Label("Preview mode");
        if (list.RadioButton("Minimap - inexpensive terrain and building overview", Mode == PreviewMode.Minimap)) Mode = PreviewMode.Minimap;
        if (list.RadioButton("Camera - experimental full graphics", Mode == PreviewMode.Camera)) Mode = PreviewMode.Camera;
        list.Gap();
        list.Label("Camera renders another view of a loaded map. Higher resolution and refresh rates use more CPU and GPU time. Start at 256 pixels / 1 FPS on slower machines.");
        if (list.ButtonText($"Camera resolution: {CameraResolution} x {CameraResolution}"))
            Find.WindowStack.Add(new FloatMenu(new System.Collections.Generic.List<FloatMenuOption> {
                new("256 - low", () => CameraResolution = 256),
                new("512 - medium", () => CameraResolution = 512),
                new("1024 - high", () => CameraResolution = 1024)
            }));
        if (list.ButtonText($"Camera refresh: {CameraFps} FPS"))
            Find.WindowStack.Add(new FloatMenu(new System.Collections.Generic.List<FloatMenuOption> {
                new("1 FPS", () => CameraFps = 1), new("2 FPS", () => CameraFps = 2),
                new("5 FPS", () => CameraFps = 5), new("10 FPS", () => CameraFps = 10), new("15 FPS", () => CameraFps = 15)
            }));
        if (list.ButtonText($"Minimap background refresh: every {MinimapInterval}s"))
            Find.WindowStack.Add(new FloatMenu(new System.Collections.Generic.List<FloatMenuOption> {
                new("Every 1s", () => MinimapInterval = 1), new("Every 2s", () => MinimapInterval = 2),
                new("Every 5s", () => MinimapInterval = 5), new("Every 10s", () => MinimapInterval = 10)
            }));
        list.CheckboxLabeled("Show pawn markers", ref ShowMarkers);
        list.Gap();
        list.Label("Camera mode includes map geometry and dynamic things. Weather effects and UI overlays are omitted. It never advances the simulation. If rendering fails, the monitor falls back to the minimap until you retry camera mode.");
        list.End();
    }
}
