using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading.Tasks;
using HarmonyLib;
using Merthsoft.ColonyMonitor;
using UnityEngine;
using Verse;
using Waylith.Rendering;
using Shader = UnityEngine.Shader;

internal static class Program
{
    public static void Main(string[] args)
    {
        var game = args.Length > 0 ? args[0] : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam/steamapps/common/RimWorld");
        var harmony = args.Length > 1 ? args[1] : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam/steamapps/workshop/content/294100/2009463077/1.7/Assemblies");
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            foreach (var directory in new[] { Path.GetDirectoryName(typeof(Program).Assembly.Location)!, game, harmony })
            {
                var file = Path.Combine(directory, name.Name + ".dll");
                if (File.Exists(file)) return context.LoadFromAssemblyPath(file);
            }
            return null;
        };
        Run();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        new Harmony("Merthsoft.ColonyMonitor.CameraChecks").PatchAll(typeof(ColonyMonitorMod).Assembly);
        RunCase("WindowOrdering", WindowOrdering);
        RunCase("Shadows", Shadows);
        RunCase("Navigation", Navigation);
        RunCase("Viewport", Viewport);
        RunCase("WaterLayers", WaterLayers);
        RunCase("WindowLayout", WindowLayout);
        RunCase("DrawOrdering", DrawOrdering);
        RunCase("WeatherLayers", WeatherLayers);
        RunCase("DrawProperties", DrawProperties);
        RunCase("InputOwnership", InputOwnership);
        RunCase("CameraInteraction", CameraInteraction);
        RunCase("SavedViews", SavedViews);
        RunCase("RenderIsolation", RenderIsolation);
        if (failures != 0) throw new Exception($"{failures} scenarios failed.");
        Console.WriteLine("All 13 scenarios passed.");
    }

    private static int failures;

    private static void RunCase(string name, Action test)
    {
        try { test(); Console.WriteLine("PASS: " + name); }
        catch (Exception error) { failures++; Console.WriteLine($"FAIL: {name}: {error}"); }
    }

    private static Map Map(int width, int height)
    {
        var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        map.info = new MapInfo { Size = new IntVec3(width, 1, height) };
        return map;
    }

    private static void WindowOrdering()
    {
        var stack = (WindowStack)RuntimeHelpers.GetUninitializedObject(typeof(WindowStack));
        var lower = (MonitorSettingsWindow)RuntimeHelpers.GetUninitializedObject(typeof(MonitorSettingsWindow));
        var dialog = (MonitorSettingsWindow)RuntimeHelpers.GetUninitializedObject(typeof(MonitorSettingsWindow));
        var reorderMonitor = (MonitorWindow)RuntimeHelpers.GetUninitializedObject(typeof(MonitorWindow));
        lower.layer = WindowLayer.GameUI;
        dialog.layer = reorderMonitor.layer = WindowLayer.Dialog;
        var windows = new List<Window> { lower, dialog, reorderMonitor };
        AccessTools.Field(typeof(WindowStack), "windows").SetValue(stack, windows);
        AccessTools.Field(typeof(WindowStack), "focusedWindow").SetValue(stack, dialog);
        MonitorWindowOrder.SetLayer(reorderMonitor, WindowLayer.GameUI, stack);
        Check(windows.Count == 3 && windows[1] == reorderMonitor && windows[2] == dialog,
            "docking reinserts the existing monitor below dialogs without closing it");
        Check(ReferenceEquals(AccessTools.Field(typeof(WindowStack), "focusedWindow").GetValue(stack), dialog),
            "reordering the monitor preserves the focused dialog");
        MonitorWindowOrder.SetLayer(reorderMonitor, WindowLayer.Dialog, stack);
        Check(windows.Count == 3 && windows[2] == reorderMonitor,
            "undocking restores the floating monitor to the dialog layer");
    }

    private static void Shadows()
    {
        var shadowColor = new Color(0.2f, 0.3f, 0.4f, 0.8f);
        PreviewShadows.Resolve(new Vector2(3, 4), null, shadowColor, 0.25f, out var shadowDirection, out var shadowTint);
        Check(shadowDirection == new System.Numerics.Vector4(3, 0, 4, 0.25f)
            && Math.Abs(shadowTint.r - 0.8f) < 0.00001f && Math.Abs(shadowTint.g - 0.825f) < 0.00001f
            && Math.Abs(shadowTint.b - 0.85f) < 0.00001f && Math.Abs(shadowTint.a - 0.95f) < 0.00001f,
            "normal shadows retain celestial direction and strength attenuation");
        var map = Map(250, 250);
        map.skyManager = (SkyManager)RuntimeHelpers.GetUninitializedObject(typeof(SkyManager));
        AccessTools.Field(typeof(SkyManager), "map").SetValue(map.skyManager, map);
        map.weatherManager = (RimWorld.WeatherManager)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.WeatherManager));
        AccessTools.Field(typeof(RimWorld.WeatherManager), "eventHandler").SetValue(map.weatherManager,
            RuntimeHelpers.GetUninitializedObject(typeof(WeatherEventHandler)));
        var flash = (WeatherEvent)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.WeatherEvent_LightningFlash));
        AccessTools.Field(flash.GetType(), "shadowVector").SetValue(flash, new Vector2(7, 9));
        AccessTools.Field(typeof(WeatherEventHandler), "liveEvents").SetValue(map.weatherManager.eventHandler, new List<WeatherEvent> { flash });
        var overridden = PreviewShadows.Override(map.skyManager);
        Check(overridden == new Vector3(7, 9, 0), "preview retrieves the real lightning event override through SkyManager");
        PreviewShadows.Resolve(new Vector2(3, 4), overridden, shadowColor, 0.25f, out shadowDirection, out shadowTint);
        Check(shadowDirection == new System.Numerics.Vector4(7, 0, 9, 0.25f) && shadowTint == shadowColor,
            "lightning shadows use the override vector and unattenuated sky shadow color");
    }

    private static void Navigation()
    {
        var navigationImage = new Rect(800, 100, 200, 200);
        var navigationWorld = new Rect(0, 0, 325, 325);
        var navigationSize = new IntVec3(325, 1, 325);
        Check(MinimapNavigation.Target(new Vector2(850, 150), navigationImage, navigationWorld, navigationSize)
            == new IntVec3(81, 0, 243), "RTS navigation maps the pointer inside an offset minimap directly to world coordinates");
        Check(MinimapNavigation.Target(new Vector2(850, 150), navigationImage, navigationWorld, navigationSize)
            == MinimapNavigation.Target(new Vector2(250, 450), new Rect(200, 400, 200, 200), navigationWorld, navigationSize),
            "moving the minimap window does not change the world location under the same relative pointer");
        Check(MinimapNavigation.Target(new Vector2(1200, 0), navigationImage, navigationWorld, navigationSize)
            == new IntVec3(324, 0, 324), "captured minimap drags stop at map edges");
    }

    private static void Viewport()
    {
        var viewport = ViewportOverlay.Project(new Rect(20, 30, 40, 20), new Rect(0, 0, 100, 100), new Rect(10, 20, 200, 200));
        Check(viewport == new Rect(50, 120, 80, 40), "viewport overlay maps world coordinates with north at the top");
        viewport = ViewportOverlay.Project(new Rect(10, 30, 40, 20), new Rect(20, 20, 40, 40), new Rect(0, 0, 200, 200));
        Check(viewport == new Rect(-50, 50, 200, 100), "viewport overlay preserves offscreen edges for clipping in camera view");
    }

    private static void WaterLayers()
    {
        var waterDraws = new List<MeshDrawCommand>();
        var visibleDraws = new List<MeshDrawCommand>();
        foreach (int layer in new[] { 0, 4, 0, 7, 4 })
            visibleDraws.Add(new MeshDrawCommand(null, null, default, System.Numerics.Matrix4x4.Identity,
                visibleDraws.Count, 0, null, 2000, layer, null));
        PreviewWater.Split(visibleDraws, waterDraws, 4, 1);
        Check(visibleDraws.Count == 2 && visibleDraws[0].SubmeshIndex == 0 && visibleDraws[1].SubmeshIndex == 2,
            "water depth and excluded camera layers never enter the visible preview");
        Check(waterDraws.Count == 2 && waterDraws[0].SubmeshIndex == 1 && waterDraws[1].SubmeshIndex == 4,
            "water depth commands retain order in their separate pass");
    }

    private static void WindowLayout()
    {
        var floating = MonitorLayout.FloatingRect(new Rect(0, 0, 390, 440), new Vector2(120, 80), 1280, 720);
        floating = MonitorLayout.FloatingRect(floating, new Vector2(1200, 700), 1024, 600);
        Check(floating.position == new Vector2(634, 160), "saved floating position stays on screen after a resolution change");
        var dock = MonitorLayout.DockRect(1280, 720, 220, 2, 12, 190);
        Check(dock == new Rect(12, 310, 220, 220), "bottom-left dock respects size and toolbar clearance");
        dock = MonitorLayout.DockRect(1280, 720, 220, 1, 12, 24);
        Check(dock == new Rect(1048, 24, 220, 220), "top-right dock respects selected anchor");
        dock = MonitorLayout.DockRect(180, 140, 420, 3, 1000, 1000);
        Check(dock == new Rect(0, 0, 140, 140), "dock remains reachable on small screens with oversized offsets");
    }

    private static void DrawOrdering()
    {
        var drawCommands = new List<MeshDrawCommand>();
        foreach (int queue in new[] { 3100, 2000, 3000, 2000, 3200 })
            drawCommands.Add(new MeshDrawCommand(null, null, default, System.Numerics.Matrix4x4.Identity,
                drawCommands.Count, 0, null, queue, 0, null));
        new PreviewDrawOrder().Apply(drawCommands);
        Check(drawCommands[0].SubmeshIndex == 1 && drawCommands[1].SubmeshIndex == 3
            && drawCommands[2].SubmeshIndex == 2 && drawCommands[3].SubmeshIndex == 0
            && drawCommands[4].SubmeshIndex == 4,
            "lighting draws after terrain and pawns, before fog, preserving equal-queue order");
    }

    private static void WeatherLayers()
    {
        var rain = (SkyOverlay)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.WeatherOverlay_Rain));
        var weatherLayers = new PreviewWeatherLayers();
        weatherLayers.Add(rain, 0.3f);
        weatherLayers.Add(rain, 0.7f);
        Check(weatherLayers.Layers.Count == 1 && weatherLayers.Layers[rain] == 1,
            "shared weather overlay weights combine without duplicate entries");
        weatherLayers.Add(rain, 0.5f);
        weatherLayers.Add(null, 1);
        Check(weatherLayers.Layers.Count == 1 && weatherLayers.Layers[rain] == 1,
            "weather opacity clamps and null overlays are ignored");
        var weatherColor = new Color(0.2f, 0.4f, 0.7f, 0.9f);
        AccessTools.PropertySetter(typeof(SkyOverlay), nameof(SkyOverlay.ForcedOverlayColor)).Invoke(rain, new object[] { (Color?)weatherColor });
        var previewColor = PreviewWeatherLayers.ColorFor(rain, Color.red, 0.25f);
        Check(previewColor.r == weatherColor.r && previewColor.g == weatherColor.g && previewColor.b == weatherColor.b
            && previewColor.a == 0.25f && rain.ForcedOverlayColor.Value == weatherColor,
            "preview weather tint respects forced color without changing shared overlay state");
    }

    private static void DrawProperties()
    {
        var arena = new DrawPropertyArena();
        int environmentId = Shader.PropertyToID("ColonyMonitor_TestEnvironment");
        int colorId = Shader.PropertyToID("_Color");
        var originalRange = arena.Copy(new[] { DrawProperty.Float(environmentId, 3) });
        var originalCommand = new MeshDrawCommand(null, null, originalRange, System.Numerics.Matrix4x4.CreateTranslation(4, 5, 6), 2, 7, null, 4001, 3, null);
        var drawProperties = new PreviewDrawProperties();
        var adjusted = drawProperties.Apply(originalCommand, arena,
            new[] { DrawProperty.Float(environmentId, 9) },
            DrawProperty.Color(colorId, new System.Numerics.Vector4(0.2f, 0.3f, 0.4f, 1)));
        Check(arena.GetProperties(originalRange)[0].FloatValue == 3 && originalCommand.PropertyRange == originalRange,
            "property overrides leave the original command unchanged");
        var adjustedProperties = arena.GetProperties(adjusted.PropertyRange);
        Check(DrawPropertySpanLookup.TryGetFloat(adjustedProperties, environmentId, out var resolvedEnvironment) && resolvedEnvironment == 9
            && DrawPropertySpanLookup.TryGetColor(adjustedProperties, colorId, out var resolvedColor)
            && resolvedColor == new System.Numerics.Vector4(0.2f, 0.3f, 0.4f, 1),
            "renderer resolves preview properties ahead of original values");
        Check(adjusted.Layer == 3 && adjusted.RenderQueueOverride == 4001
            && adjusted.SubmeshIndex == originalCommand.SubmeshIndex && adjusted.PassId == originalCommand.PassId
            && adjusted.Matrix == originalCommand.Matrix, "local property overrides preserve draw routing and transform");
    }

    private static void InputOwnership()
    {
        var monitor = (MonitorWindow)RuntimeHelpers.GetUninitializedObject(typeof(MonitorWindow));
        monitor.windowRect = new Rect(100, 100, 390, 500);
        Check(monitor.OwnsCameraInput(new Vector2(200, 200)), "monitor blocks main-camera input before GUI handles pointer");
        Check(!monitor.OwnsCameraInput(new Vector2(20, 20)), "main-camera input remains available outside monitor");
        AccessTools.Field(typeof(MonitorWindow), "panning").SetValue(monitor, true);
        Check(monitor.OwnsCameraInput(new Vector2(20, 20)), "captured monitor drag blocks main camera outside window");
        AccessTools.Field(typeof(MonitorWindow), "panning").SetValue(monitor, false);
        AccessTools.Field(typeof(MonitorWindow), "previewPress").SetValue(monitor, true);
        Check(monitor.OwnsCameraInput(new Vector2(20, 20)), "captured click also retains ownership outside monitor");
    }

    private static void CameraInteraction()
    {
        var mapSize = new IntVec3(250, 1, 250);
        var state = new CameraViewState { Center = new Vector2(100, 100), Size = 40 };
        var pan = new PreviewInteraction();
        state.Center = new Vector2(100, 100);
        pan.Begin(new Vector2(30, 40));
        pan.Move(new Vector2(58, 26), state, 280, mapSize);
        pan.Move(new Vector2(58, 26), state, 280, mapSize);
        Check(state.Center == new Vector2(92, 96), "repeated pointer events do not double-count drag");
        pan.Move(new Vector2(86, 12), state, 280, mapSize);
        Check(state.Center == new Vector2(84, 92), "missing intermediate events preserve full pointer displacement");
        Check(PreviewInteraction.ProjectCachedImage(new Rect(0, 0, 80, 80), new Rect(-8, 0, 80, 80),
            new Rect(0, 0, 280, 280), out var projected, out var uv) && projected.x == 28 && projected.width == 252
            && uv.x == 0 && Mathf.Abs(uv.width - 0.9f) < 0.001f, "cached image immediately follows drag and clips newly exposed edge");
        Check(PreviewInteraction.ProjectCachedImage(new Rect(0, 0, 80, 80), new Rect(20, 20, 40, 40),
            new Rect(0, 0, 280, 280), out projected, out uv) && projected.width == 280 && uv == new Rect(0.25f, 0.25f, 0.5f, 0.5f),
            "cached zoom crops instantly without another render");
        state.Center = new Vector2(100, 100);
        state.Size = 40;
        var anchor = new Vector2(0.75f, 0.25f);
        var anchorBefore = state.Center + new Vector2(0.25f, 0.25f) * state.Size * 2;
        state.Zoom(0.5f, anchor, mapSize);
        var anchorAfter = state.Center + new Vector2(0.25f, 0.25f) * state.Size * 2;
        Check((anchorBefore - anchorAfter).sqrMagnitude < 0.001f, "zoom preserves world position under pointer");
        state.Size = float.NaN;
        state.Center = new Vector2(float.PositiveInfinity, -100);
        state.Clamp(mapSize);
        Check(state.Size == 125 && state.Center.x == 125 && state.Center.y == 0, "invalid saved view sanitized");
        var largeMap = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        largeMap.info = new MapInfo { Size = new IntVec3(325, 1, 325) };
        var fullView = new MonitorGameComponent(null).GetView(largeMap, out _);
        Check(fullView.Center == new Vector2(162.5f, 162.5f) && fullView.Size == 162.5f,
            "325-cell map starts centered and fully visible");
        fullView.Zoom(2, new Vector2(0.5f, 0.5f), largeMap.Size);
        Check(fullView.Size == 162.5f, "zoom-out limit fits the full map instead of stopping at 150 cells");
    }

    private static void SavedViews()
    {
        var mapSize = new IntVec3(250, 1, 250);
        var map = Map(250, 250);
        map.uniqueID = 101;
        var anotherMap = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        anotherMap.info = new MapInfo { Size = mapSize };
        anotherMap.uniqueID = 102;
        var store = new MonitorGameComponent(null);
        var first = store.GetView(map, out var firstCreated);
        first.Size = 28;
        var second = store.GetView(anotherMap, out var secondCreated);
        second.Size = 70;
        Check(firstCreated && secondCreated && store.GetView(map, out _) == first && first.Size == 28 && second.Size == 70,
            "switching maps retains independent view objects");
        Check(new MonitorGameComponent(null).GetView(map, out _) != first, "views are not shared across game instances");
        store.SelectedMapId = 102;
        first.Center = new Vector2(91, 83);
        var stateFile = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location)!, "camera-check-state.xml");
        // Scribe's profiler reads preferences; supply an in-memory default only.
        AccessTools.Field(typeof(Prefs), "data").SetValue(null, RuntimeHelpers.GetUninitializedObject(typeof(PrefsData)));
        Scribe.saver.InitSaving(stateFile, "CameraCheck");
        Scribe_Deep.Look(ref store, "monitor", new object[] { null });
        Scribe.saver.FinalizeSaving();
        MonitorGameComponent restored = null;
        Scribe.loader.InitLoading(stateFile);
        Scribe_Deep.Look(ref restored, "monitor", new object[] { null });
        Scribe.loader.FinalizeLoading();
        var restoredFirst = restored.GetView(map, out var recreated);
        Check(!recreated && restored.SelectedMapId == 102 && restoredFirst.Size == 28 && restoredFirst.Center == new Vector2(91, 83)
            && restored.GetView(anotherMap, out _).Size == 70, "Scribe round-trip preserves selected map and independent camera framing");
    }

    private static void RenderIsolation()
    {
        var map = Map(250, 250);
        var bounds = new Rect(10.5f, 20.5f, 80, 80);
        try
        {
            using (new PreviewContext(map, null, bounds, 40))
            {
                Check(Find.CurrentMap == map, "draw uses monitored map");
                Check(PreviewContext.Current.ViewRect.minX == 10 && PreviewContext.Current.ViewRect.maxX == 91, "culling includes fractional edges");
                Check(Task.Run(() => !PreviewContext.Active).Result, "context does not leak to other threads");
                try { using var nested = new PreviewContext(map, null, bounds, 40); throw new Exception("Nested context was accepted"); }
                catch (InvalidOperationException) { Check(PreviewContext.Current.Map == map, "nested rejection preserves context"); }
                throw new ApplicationException("simulated draw failure");
            }
        }
        catch (ApplicationException) { }
        Check(!PreviewContext.Active, "context restored after draw exception");

        var sentinel = new MeshDrawCommand(null, null, default, System.Numerics.Matrix4x4.Identity, 9, 0, null, 2000, 0, null);
        MeshDrawSubmission.Enqueue(in sentinel);
        var before = new List<MeshDrawCommand>();
        MeshDrawSubmission.CopyPendingCommands(before, -1);
        var captured = new List<MeshDrawCommand>();
        try
        {
            using (new PreviewContext(map, null, bounds, 40))
            using (MeshDrawSubmission.BeginCapture(captured))
            {
                var command = new MeshDrawCommand(null, null, default, System.Numerics.Matrix4x4.Identity, 4, 0, null, 3100, 0, null);
                MeshDrawSubmission.Enqueue(in command);
                throw new ApplicationException("simulated capture failure");
            }
        }
        catch (ApplicationException) { }
        Check(!MeshDrawSubmission.IsCapturing && !PreviewContext.Active,
            "both capture and map context unwind after a rendering exception");
        var after = new List<MeshDrawCommand>();
        MeshDrawSubmission.CopyPendingCommands(after, -1);
        Check(before.SequenceEqual(after) && after.Count == 1 && after[0].SubmeshIndex == 9
            && captured.Count == 1 && captured[0].SubmeshIndex == 4,
            "failed preview capture preserves the actual main draw commands, not just their count");
        using (new PreviewContext(map, null, bounds, 40))
        using (MeshDrawSubmission.BeginCapture(captured))
        {
            bool blocked = false;
            try { MeshDrawSubmission.SubmitPersistentSource(null); }
            catch (NotSupportedException) { blocked = true; }
            Check(blocked, "persistent-source Harmony guard rejects submissions during a preview");
        }
        MeshDrawSubmission.ClearFrameSubmissions();
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
    }
}
