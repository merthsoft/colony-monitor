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
    public bool ShowViewport = true;
    public bool ShowWeather = true;
    public bool Enabled;
    public bool Docked;
    public bool CurrentMapOnly;
    public bool DockCurrentMapOnly = true;
    public int DockSize = 220;
    public int DockCorner = 2;
    public int DockInsetX = 12;
    public int DockInsetY = 190;
    public int ButtonLocation;
    public float ButtonScale = 1.5f;
    public Vector2 FloatingPosition = new(-1, -1);
    private readonly MonitorSettingsPanel panel = new();

    public override void ExposeData()
    {
        Scribe_Values.Look(ref Mode, "previewMode", PreviewMode.Minimap);
        Scribe_Values.Look(ref CameraResolution, "cameraResolution", 512);
        Scribe_Values.Look(ref CameraFps, "cameraFps", 2);
        Scribe_Values.Look(ref MinimapInterval, "minimapInterval", 2);
        Scribe_Values.Look(ref ShowMarkers, "showMarkers", true);
        Scribe_Values.Look(ref ShowViewport, "showViewport", true);
        Scribe_Values.Look(ref ShowWeather, "showWeather", true);
        Scribe_Values.Look(ref Enabled, "enabled", false);
        Scribe_Values.Look(ref Docked, "docked", false);
        Scribe_Values.Look(ref CurrentMapOnly, "currentMapOnly", false);
        Scribe_Values.Look(ref DockCurrentMapOnly, "dockCurrentMapOnly", true);
        Scribe_Values.Look(ref DockSize, "dockSize", 220);
        Scribe_Values.Look(ref DockCorner, "dockCorner", 2);
        Scribe_Values.Look(ref DockInsetX, "dockInsetX", 12);
        Scribe_Values.Look(ref DockInsetY, "dockInsetY", 190);
        Scribe_Values.Look(ref ButtonLocation, "buttonLocation", 0);
        Scribe_Values.Look(ref ButtonScale, "buttonScale", 1.5f);
        Scribe_Values.Look(ref FloatingPosition, "floatingPosition", new Vector2(-1, -1));
        if (!float.IsFinite(FloatingPosition.x) || !float.IsFinite(FloatingPosition.y)) FloatingPosition = new Vector2(-1, -1);
        ButtonLocation = Math.Clamp(ButtonLocation, 0, 2);
        ButtonScale = float.IsFinite(ButtonScale) ? Math.Clamp(ButtonScale, 1f, 4f) : 1.5f;
        DockSize = Math.Clamp(DockSize, 140, 420);
        DockCorner = Math.Clamp(DockCorner, 0, 3);
        DockInsetX = Math.Clamp(DockInsetX, 0, 1000);
        DockInsetY = Math.Clamp(DockInsetY, 0, 1000);
        if (Mode != PreviewMode.Minimap && Mode != PreviewMode.Camera) Mode = PreviewMode.Minimap;
        CameraResolution = CameraResolution <= 256 ? 256 : CameraResolution <= 512 ? 512 : 1024;
        CameraFps = Math.Clamp(CameraFps, 1, 15);
        MinimapInterval = Math.Clamp(MinimapInterval, 1, 10);
    }

    public void Draw(Rect rect) => panel.Draw(this, rect);
}
