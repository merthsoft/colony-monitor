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
    private bool failed;
    private int width, height;
    private readonly CameraPreview cameraPreview = new();
    private PreviewMode appliedMode;
    private int appliedResolution;
    private bool cameraFailed;
    private CameraViewState view;
    private MonitorGameComponent gameState;
    private bool panning;
    private int panControl;
    private int panButton = 1;
    private bool previewDragged;
    private readonly PreviewInteraction pan = new();
    private bool previewPress;
    private Vector2 previewPressPosition;
    private int previewControl;
    private bool wasDocked;
    private Rect floatingRect;
    private bool followedCurrentMap;
    private bool Docked => ColonyMonitorMod.Settings.Docked;
    private bool FollowsCurrentMap => Docked ? ColonyMonitorMod.Settings.DockCurrentMapOnly : ColonyMonitorMod.Settings.CurrentMapOnly;
    protected override float Margin => Docked ? 0f : 18f;
    public override Vector2 InitialSize => new(390f, 440f);

    internal static bool BlocksMainCameraMotion()
    {
        if (Current.Game == null)
            return false;

        var pointer = UI.MousePositionOnUIInverted;
        return Find.WindowStack.Windows.OfType<MonitorWindow>()
            .Any(window => window.OwnsCameraInput(pointer));
    }

    internal bool OwnsCameraInput(Vector2 pointer)
        => panning || previewPress || windowRect.Contains(pointer);

    public MonitorWindow()
    {
        doCloseX = true;
        draggable = true;
        forcePause = false;
        absorbInputAroundWindow = false;
        preventCameraMotion = false;
        closeOnAccept = false;
        ColonyMonitorMod.Settings.Enabled = true;
        gameState = Current.Game.GetComponent<MonitorGameComponent>();
        var initial = Find.Maps.FirstOrDefault(m => m.uniqueID == gameState.SelectedMapId)
            ?? Find.Maps.FirstOrDefault(m => m.IsPlayerHome && m != Find.CurrentMap)
            ?? Find.Maps.FirstOrDefault(m => m != Find.CurrentMap) ?? Find.CurrentMap;
        if (initial != null) SelectMap(initial);
    }

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        if (Current.Game == null || Find.Maps.Count == 0) { Close(); return; }
        var settings = ColonyMonitorMod.Settings;
        if (FollowsCurrentMap && Find.CurrentMap != null && selected != Find.CurrentMap)
            SelectMap(Find.CurrentMap);
        if (followedCurrentMap && !FollowsCurrentMap)
        {
            var remembered = Find.Maps.FirstOrDefault(m => m.uniqueID == gameState.SelectedMapId);
            if (remembered != null && remembered != selected) SelectMap(remembered);
        }
        followedCurrentMap = FollowsCurrentMap;
        ApplyLayout(settings);
        if (!Find.Maps.Contains(selected)) SelectMap(Find.Maps[0]);
        // Recover mouse-up lost outside the game.
        if (panning && !Input.GetMouseButton(panButton)) EndPan();
        if (view.Follow != null)
        {
            if (view.Follow.Spawned && view.Follow.Map == selected && !view.Follow.Dead)
                view.Center = new Vector2(view.Follow.DrawPos.x, view.Follow.DrawPos.z);
            else view.Follow = null;
        }
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
                    cameraPreview.Refresh(selected, view.Center, view.Size, settings.CameraResolution);
                }
                else Refresh();
            }
            catch (Exception ex)
            {
                if (UseCamera)
                {
                    cameraFailed = true;
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

    public override void PostOpen()
    {
        base.PostOpen();
        var position = ColonyMonitorMod.Settings.FloatingPosition;
        if (position.x >= 0 && position.y >= 0)
            windowRect = MonitorLayout.FloatingRect(windowRect, position, UI.screenWidth, UI.screenHeight);
    }

    private void SelectMap(Map map)
    {
        EndPan();
        previewPress = false;
        selected = map;
        if (!FollowsCurrentMap) gameState.SelectedMapId = map.uniqueID;
        view = gameState.GetView(map, out _);
        ReleaseTexture();
        failed = false;
        cameraFailed = false;
        refreshedAt = -100f;
    }

    private bool UseCamera => ColonyMonitorMod.Settings.Mode == PreviewMode.Camera && !cameraFailed;

    private void ApplyLayout(MonitorSettings settings)
    {
        if (settings.Docked && !wasDocked)
        {
            floatingRect = windowRect;
            settings.FloatingPosition = windowRect.position;
        }
        if (!settings.Docked && wasDocked)
            windowRect = MonitorLayout.FloatingRect(floatingRect, floatingRect.position, UI.screenWidth, UI.screenHeight);
        if (settings.Docked)
            windowRect = MonitorLayout.DockRect(UI.screenWidth, UI.screenHeight, settings.DockSize,
                settings.DockCorner, settings.DockInsetX, settings.DockInsetY);
        doCloseX = draggable = doWindowBackground = drawShadow = !settings.Docked;
        layer = settings.Docked ? WindowLayer.GameUI : WindowLayer.Dialog;
        closeOnCancel = !settings.Docked;
        wasDocked = settings.Docked;
    }

    internal void ShowMapMenu()
    {
        var settings = ColonyMonitorMod.Settings;
        var choices = Find.Maps.Select(m =>
            new FloatMenuOption($"{m.Parent.LabelCap} - {m.mapPawns.FreeColonistsSpawnedCount} colonists"
                + (m == Find.CurrentMap ? " (current)" : ""), () =>
                {
                    if (Docked) settings.DockCurrentMapOnly = false;
                    else settings.CurrentMapOnly = false;
                    SelectMap(m);
                    ColonyMonitorMod.Instance.WriteSettings();
                })).ToList();
        choices.Insert(0, new FloatMenuOption("Follow current map", () =>
        {
            if (Docked) settings.DockCurrentMapOnly = true;
            else settings.CurrentMapOnly = true;
            ColonyMonitorMod.Instance.WriteSettings();
        }));
        Find.WindowStack.Add(new FloatMenu(choices));
    }

    internal void Zoom(float factor)
    {
        view.Zoom(factor, new Vector2(0.5f, 0.5f), selected.Size);
        refreshedAt = -100f;
    }

    internal void RetryPreview()
    {
        cameraFailed = failed = false;
        refreshedAt = -100f;
    }

    private void CenterCamera()
    {
        var colonists = selected.mapPawns.FreeColonistsSpawned;
        view.Follow = null;
        view.Center = colonists.Count == 0 ? new Vector2(selected.Size.x / 2f, selected.Size.z / 2f)
            : new Vector2((float)colonists.Average(p => p.Position.x), (float)colonists.Average(p => p.Position.z));
        refreshedAt = -100f;
    }

    private void Refresh()
    {

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
                // Terrain tint is usually white; it does not represent texture color.
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
        if (Docked)
        {
            DrawPreview(rect);
            return;
        }
        if (selected == null || !Find.Maps.Contains(selected)) return;
        if (Widgets.ButtonText(new Rect(0, 0, rect.width - 26, 28), selected.Parent.LabelCap))
            ShowMapMenu();

        if (Widgets.ButtonText(new Rect(0, 34, rect.width - 94, 28),
            UseCamera ? "Camera" : "Minimap" + (cameraFailed ? " (camera unavailable)" : "")))
        {
            ColonyMonitorMod.Settings.Mode = UseCamera ? PreviewMode.Minimap : PreviewMode.Camera;
            cameraFailed = false;
            EndPan();
            refreshedAt = -100f;
            ColonyMonitorMod.Instance.WriteSettings();
        }
        if (Widgets.ButtonText(new Rect(rect.width - 88, 34, 88, 28), "Settings"))
            Find.WindowStack.Add(new MonitorSettingsWindow());
        var area = new Rect(0, 68, rect.width, 280);
        DrawPreview(area);
    }

    private void DrawPreview(Rect area)
    {
        if (selected == null || !Find.Maps.Contains(selected)) return;
        Widgets.DrawBoxSolid(area, Color.black);
        if (failed)
        {
            Widgets.Label(area.ContractedBy(10), "Preview unavailable. Use Refresh preview in the monitor menu.");
            return;
        }
        Texture texture = UseCamera ? cameraPreview.Texture : preview;
        if (texture == null) { Widgets.Label(area.ContractedBy(10), "Preparing preview..."); return; }
        var world = UseCamera ? new Rect(view.Center.x - view.Size, view.Center.y - view.Size, view.Size * 2, view.Size * 2)
            : new Rect(0, 0, width, height);
        float scale = Math.Min(area.width / world.width, area.height / world.height);
        var image = new Rect(area.x + (area.width - world.width * scale) / 2,
            area.y + (area.height - world.height * scale) / 2, world.width * scale, world.height * scale);
        HandlePreviewInput(image, world, scale);
        if (UseCamera)
        {
            world = new Rect(view.Center.x - view.Size, view.Center.y - view.Size, view.Size * 2, view.Size * 2);
            scale = image.width / world.width;
            if (PreviewInteraction.ProjectCachedImage(cameraPreview.WorldRect, world, image, out var destination, out var uv))
                GUI.DrawTextureWithTexCoords(destination, texture, uv);
        }
        else GUI.DrawTexture(image, texture);
        if (ColonyMonitorMod.Settings.ShowViewport && selected == Find.CurrentMap
            && RimWorld.Planet.WorldRendererUtility.DrawingMap)
            ViewportOverlay.Draw(Find.Camera, world, image);
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
        TooltipHandler.TipRegion(image, UseCamera
            ? "Scroll to zoom. Drag to pan. Click to jump. Green: colonists; yellow: downed; red: hostiles."
            : "Click or drag to move the main camera. Green: colonists; yellow: downed; red: hostiles.");
        if (Docked)
        {
            TooltipHandler.TipRegion(area, $"{selected.Parent.LabelCap}. Right-click the monitor icon for controls.");
            return;
        }
        Widgets.Label(new Rect(0, 354, area.width, 24),
            $"Colonists: {selected.mapPawns.FreeColonistsSpawnedCount}   Downed: {downed}   Hostiles: {hostiles}");
        if (UseCamera)
        {
            if (Widgets.ButtonText(new Rect(0, 380, 32, 24), "-")) view.Zoom(1.3f, new Vector2(0.5f, 0.5f), selected.Size);
            if (Widgets.ButtonText(new Rect(36, 380, 32, 24), "+")) view.Zoom(1 / 1.3f, new Vector2(0.5f, 0.5f), selected.Size);
            if (Widgets.ButtonText(new Rect(74, 380, 90, 24), "Center")) ShowCenterMenu();
            if (Widgets.ButtonText(new Rect(170, 380, area.width - 170, 24), view.Follow == null ? "Follow colonist" : "Following: " + view.Follow.LabelShortCap)) ShowFollowMenu();
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
        EndPan();
        ReleaseTexture();
        if (Current.Game != null)
        {
            if (!wasDocked) ColonyMonitorMod.Settings.FloatingPosition = windowRect.position;
            ColonyMonitorMod.Settings.Enabled = false;
            ColonyMonitorMod.Instance.WriteSettings();
        }
        base.PostClose();
    }

    internal void ShowCenterMenu()
    {
        var choices = new System.Collections.Generic.List<FloatMenuOption>
        {
            new("Whole map", () => { view.FitMap(selected.Size); refreshedAt = -100f; }),
            new("Colonists", CenterCamera),
            new("Map center", () => { view.Follow = null; view.Center = new Vector2(selected.Size.x / 2f, selected.Size.z / 2f); })
        };
        choices.Add(selected == Find.CurrentMap ? new FloatMenuOption("Use main view", () =>
        {
            view.Follow = null;
            var position = Find.CameraDriver.MapPosition;
            view.Center = new Vector2(position.x, position.z);
            view.Size = Find.CameraDriver.RootSize;
            view.Clamp(selected.Size);
        }) : new FloatMenuOption("Use main view (switch to this map first)", null));
        Find.WindowStack.Add(new FloatMenu(choices));
    }

    internal void ShowFollowMenu()
    {
        var choices = new System.Collections.Generic.List<FloatMenuOption> { new("Stop following", () => view.Follow = null) };
        choices.AddRange(selected.mapPawns.FreeColonistsSpawned.OrderBy(p => p.LabelShortCap.ToString())
            .Select(p => new FloatMenuOption(p.LabelShortCap, () => view.Follow = p)));
        Find.WindowStack.Add(new FloatMenu(choices));
    }

    private void EndPan()
    {
        bool wasPanning = panning;
        if (panning && GUIUtility.hotControl == panControl) GUIUtility.hotControl = 0;
        panning = false;
        if (previewPress && GUIUtility.hotControl == previewControl) GUIUtility.hotControl = 0;
        previewPress = false;
        if (wasPanning) refreshedAt = -100f;
    }

    private void HandlePreviewInput(Rect image, Rect world, float scale)
    {
        var ev = Event.current;
        int control = GUIUtility.GetControlID(FocusType.Passive);
        bool inside = image.Contains(ev.mousePosition);
        if (UseCamera && ev.type == EventType.ScrollWheel && inside)
        {
            view.Zoom(Mathf.Pow(1.15f, Mathf.Clamp(ev.delta.y, -5, 5)),
                new Vector2((ev.mousePosition.x - image.x) / image.width, (ev.mousePosition.y - image.y) / image.height), selected.Size);
            ev.Use();
        }
        if (UseCamera && ev.type == EventType.MouseDown && ev.button == 1 && inside)
        {
            panning = true;
            panButton = 1;
            panControl = control;
            GUIUtility.hotControl = control;
            pan.Begin(ev.mousePosition);
            view.Follow = null;
            previewPress = false;
            ev.Use();
        }
        if (panning && (ev.rawType == EventType.MouseDrag || ev.rawType == EventType.MouseMove))
        {
            pan.Move(ev.mousePosition, view, image.width, selected.Size);
            ev.Use();
        }
        if (panning && ev.rawType == EventType.MouseUp && ev.button == panButton)
        {
            pan.Move(ev.mousePosition, view, image.width, selected.Size);
            EndPan();
            ev.Use();
        }
        // Retain the gesture through mouse-up to block main-map orders.
        if (ev.type == EventType.MouseDown && ev.button == 0 && inside)
        {
            previewPress = true;
            previewDragged = false;
            previewControl = control;
            GUIUtility.hotControl = control;
            previewPressPosition = ev.mousePosition;
            ev.Use();
        }
        if (previewPress && (ev.rawType == EventType.MouseDrag || ev.rawType == EventType.MouseMove))
        {
            previewDragged |= (ev.mousePosition - previewPressPosition).sqrMagnitude >= 25;
            if (previewDragged)
            {
                if (UseCamera)
                {
                    panning = true;
                    panButton = 0;
                    panControl = previewControl;
                    pan.Begin(previewPressPosition);
                    pan.Move(ev.mousePosition, view, image.width, selected.Size);
                    previewPress = false;
                }
                else JumpFromPreview(ev.mousePosition, image, world, scale);
            }
            ev.Use();
        }
        if (previewPress && ev.rawType == EventType.MouseUp && ev.button == 0)
        {
            previewPress = false;
            if (GUIUtility.hotControl == previewControl) GUIUtility.hotControl = 0;
            if (inside && !previewDragged && (ev.mousePosition - previewPressPosition).sqrMagnitude < 25)
            {
                JumpFromPreview(ev.mousePosition, image, world, scale);
            }
            ev.Use();
        }
    }

    private void JumpFromPreview(Vector2 pointer, Rect image, Rect world, float scale)
    {
        var cell = new IntVec3(Mathf.Clamp((int)(world.x + (pointer.x - image.x) / scale), 0, selected.Size.x - 1), 0,
            Mathf.Clamp((int)(world.y + (image.yMax - pointer.y) / scale), 0, selected.Size.z - 1));
        CameraJumper.TryJump(cell, selected);
    }
}
