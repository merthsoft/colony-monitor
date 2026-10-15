using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Merthsoft.ColonyMonitor;

public sealed class MonitorWindow : Window
{
    private Map selected;
    private Texture2D preview;
    private Color[] pixels;
    private float refreshedAt = -100f;
    private double refreshMilliseconds;
    private bool failed;
    private int width, height;
    private readonly CameraPreview cameraPreview = new();
    private PreviewMode appliedMode;
    private int appliedResolution;
    private bool cameraFailed;
    private Vector2 cameraCenter;
    private float cameraSize = 40f;
    private string cameraError;
    public override Vector2 InitialSize => new(390f, 500f);

    public MonitorWindow()
    {
        doCloseX = true;
        draggable = true;
        forcePause = false;
        absorbInputAroundWindow = false;
        preventCameraMotion = false;
        closeOnAccept = false;
        selected = Find.Maps.FirstOrDefault(m => m.IsPlayerHome && m != Find.CurrentMap)
            ?? Find.Maps.FirstOrDefault(m => m != Find.CurrentMap) ?? Find.CurrentMap;
        if (selected != null) CenterCamera();
    }

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        if (Current.Game == null || Find.Maps.Count == 0) { Close(); return; }
        if (!Find.Maps.Contains(selected)) SelectMap(Find.Maps[0]);
        var settings = ColonyMonitorMod.Settings;
        if (appliedMode != settings.Mode || appliedResolution != settings.CameraResolution)
        {
            ReleaseTexture();
            appliedMode = settings.Mode;
            appliedResolution = settings.CameraResolution;
            cameraFailed = failed = false;
            refreshedAt = -100f;
        }
        float interval = UseCamera ? 1f / settings.CameraFps : settings.MinimapInterval;
        if (!failed && Time.realtimeSinceStartup - refreshedAt >= interval)
        {
            refreshedAt = Time.realtimeSinceStartup;
            try
            {
                if (UseCamera)
                {
                    var timer = System.Diagnostics.Stopwatch.StartNew();
                    cameraPreview.Refresh(selected, cameraCenter, cameraSize, settings.CameraResolution);
                    refreshMilliseconds = timer.Elapsed.TotalMilliseconds;
                }
                else Refresh();
            }
            catch (Exception ex)
            {
                if (UseCamera)
                {
                    cameraFailed = true;
                    cameraError = ex.Message;
                    cameraPreview.Dispose();
                    refreshedAt = -100f;
                    Log.Warning("[Colony Monitor] Camera failed; falling back to minimap: " + ex);
                }
                else
                {
                    failed = true;
                    Log.Error("[Colony Monitor] Preview refresh failed: " + ex);
                }
            }
        }
    }

    private void SelectMap(Map map)
    {
        selected = map;
        ReleaseTexture();
        failed = false;
        cameraFailed = false;
        CenterCamera();
        refreshedAt = -100f;
    }

    private bool UseCamera => ColonyMonitorMod.Settings.Mode == PreviewMode.Camera && !cameraFailed;

    private void CenterCamera()
    {
        var colonists = selected.mapPawns.FreeColonistsSpawned;
        cameraCenter = colonists.Count == 0 ? new Vector2(selected.Size.x / 2f, selected.Size.z / 2f)
            : new Vector2((float)colonists.Average(p => p.Position.x), (float)colonists.Average(p => p.Position.z));
        refreshedAt = -100f;
    }

    private void Refresh()
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        // One pixel per cell preserves walls even on large maps. Only this map is cached.
        if (preview == null)
        {
            width = selected.Size.x;
            height = selected.Size.z;
            preview = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            pixels = new Color[width * height];
        }
        for (int z = 0; z < height; z++)
        for (int x = 0; x < width; x++)
        {
            var cell = new IntVec3(x, 0, z);
            Color color;
            if (selected.fogGrid.IsFogged(cell)) color = new Color(0.04f, 0.04f, 0.05f);
            else
            {
                var terrain = selected.terrainGrid.TerrainAt(cell);
                // Most terrain graphics use white tint over a texture. Use a readable
                // schematic palette rather than treating that tint as the terrain image.
                color = TerrainColor(terrain);
                var building = cell.GetEdifice(selected);
                if (building != null)
                    color = building.def.building.isNaturalRock
                        ? new Color(0.28f, 0.27f, 0.25f) : new Color(0.73f, 0.73f, 0.76f);
            }
            color.a = 1f;
            pixels[z * width + x] = color;
        }
        preview.SetPixels(pixels);
        preview.Apply(false, false);
        refreshMilliseconds = timer.Elapsed.TotalMilliseconds;
    }

    private static Color TerrainColor(TerrainDef terrain)
    {
        if (terrain == null) return Color.gray;
        if (terrain.IsWater) return new Color(0.16f, 0.34f, 0.48f);
        if (terrain.IsIce) return new Color(0.68f, 0.80f, 0.84f);
        if (terrain.IsSoil) return new Color(0.34f, 0.38f, 0.23f);
        if (terrain.IsFloor) return new Color(0.52f, 0.46f, 0.37f);
        if (terrain.IsRock) return new Color(0.39f, 0.38f, 0.36f);
        return new Color(0.57f, 0.51f, 0.36f);
    }

    public override void DoWindowContents(Rect rect)
    {
        Widgets.Label(new Rect(0, 0, rect.width - 25, 28), "Colony Monitor - prototype");
        if (selected == null || !Find.Maps.Contains(selected)) return;
        if (Widgets.ButtonText(new Rect(0, 34, rect.width, 28), selected.Parent.LabelCap))
            Find.WindowStack.Add(new FloatMenu(Find.Maps.Select(m =>
                new FloatMenuOption(m.Parent.LabelCap + (m == Find.CurrentMap ? " (viewing)" : ""),
                    () => SelectMap(m))).ToList()));

        if (Widgets.ButtonText(new Rect(0, 68, rect.width - 94, 28),
            UseCamera ? "Mode: Camera" : "Mode: Minimap" + (cameraFailed ? " (fallback)" : "")))
        {
            ColonyMonitorMod.Settings.Mode = UseCamera ? PreviewMode.Minimap : PreviewMode.Camera;
            cameraFailed = false;
            refreshedAt = -100f;
            ColonyMonitorMod.Instance.WriteSettings();
        }
        if (Widgets.ButtonText(new Rect(rect.width - 88, 68, 88, 28), "Settings"))
            Find.WindowStack.Add(new MonitorSettingsWindow());
        var area = new Rect(0, 102, rect.width, 280);
        Widgets.DrawBoxSolid(area, Color.black);
        if (failed)
        {
            Widgets.Label(area.ContractedBy(10), "Preview failed. See the log; select a map to retry.");
            return;
        }
        Texture texture = UseCamera ? cameraPreview.Texture : preview;
        if (texture == null) { Widgets.Label(area.ContractedBy(10), "Preparing preview..."); return; }
        var world = UseCamera ? cameraPreview.WorldRect : new Rect(0, 0, width, height);
        float scale = Math.Min(area.width / world.width, area.height / world.height);
        var image = new Rect(area.x + (area.width - world.width * scale) / 2,
            area.y + (area.height - world.height * scale) / 2, world.width * scale, world.height * scale);
        GUI.DrawTexture(image, texture);
        int hostiles = 0, downed = 0;
        foreach (var pawn in selected.mapPawns.AllPawnsSpawned)
        {
            if (selected.fogGrid.IsFogged(pawn.Position)) continue;
            bool hostile = pawn.HostileTo(Faction.OfPlayer);
            if (hostile) hostiles++;
            if (pawn.IsColonist && pawn.Downed) downed++;
            if (!ColonyMonitorMod.Settings.ShowMarkers) continue;
            if (!hostile && !pawn.IsColonistPlayerControlled) continue;
            Color color = hostile ? Color.red : pawn.Downed ? Color.yellow : Color.green;
            var point = new Rect(image.x + (pawn.Position.x + 0.5f - world.x) * scale - 2,
                image.yMax - (pawn.Position.z + 0.5f - world.y) * scale - 2, 4, 4);
            if (!image.Contains(point.min) || !image.Contains(point.max)) continue;
            Widgets.DrawBoxSolid(point, color);
            TooltipHandler.TipRegion(point.ExpandedBy(2), pawn.LabelShortCap);
        }
        TooltipHandler.TipRegion(image, "Click to jump here. Green: colonists; yellow: downed; red: hostiles.");
        if (Widgets.ButtonInvisible(image))
        {
            var mouse = Event.current.mousePosition;
            var cell = new IntVec3(Mathf.Clamp((int)(world.x + (mouse.x - image.x) / scale), 0, selected.Size.x - 1), 0,
                Mathf.Clamp((int)(world.y + (image.yMax - mouse.y) / scale), 0, selected.Size.z - 1));
            CameraJumper.TryJump(cell, selected);
        }
        Widgets.Label(new Rect(0, 388, rect.width, 24),
            $"Colonists: {selected.mapPawns.FreeColonistsSpawnedCount}   Downed: {downed}   Hostiles: {hostiles}");
        Widgets.Label(new Rect(0, 412, rect.width, 26), UseCamera
            ? $"{ColonyMonitorMod.Settings.CameraResolution}px / {ColonyMonitorMod.Settings.CameraFps} FPS | {refreshMilliseconds:0.0} ms CPU"
            : $"{ColonyMonitorMod.Settings.MinimapInterval}s background | {refreshMilliseconds:0.0} ms refresh");
        if (cameraFailed)
            TooltipHandler.TipRegion(area, "Camera unavailable: " + cameraError + ". Click Mode to retry.");
        if (UseCamera)
        {
            if (Widgets.ButtonText(new Rect(0, 440, 32, 24), "-")) { cameraSize = Math.Min(150, cameraSize * 1.3f); refreshedAt = -100f; }
            if (Widgets.ButtonText(new Rect(36, 440, 32, 24), "+")) { cameraSize = Math.Max(8, cameraSize / 1.3f); refreshedAt = -100f; }
            if (Widgets.ButtonText(new Rect(74, 440, 90, 24), "Colonists")) CenterCamera();
            if (Widgets.ButtonText(new Rect(170, 440, rect.width - 170, 24), "Use main view"))
            {
                if (selected == Find.CurrentMap)
                {
                    var position = Find.CameraDriver.MapPosition;
                    cameraCenter = new Vector2(position.x, position.z);
                    cameraSize = Find.CameraDriver.RootSize;
                    refreshedAt = -100f;
                }
                else Messages.Message("Switch to the monitored map first to copy its camera position.", MessageTypeDefOf.RejectInput, false);
            }
        }
    }

    private void ReleaseTexture()
    {
        if (preview != null) UnityEngine.Object.Destroy(preview);
        preview = null;
        pixels = null;
        cameraPreview.Dispose();
    }

    public override void PostClose()
    {
        ReleaseTexture();
        base.PostClose();
    }
}
