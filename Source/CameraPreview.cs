using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityStubs.Rendering;
using Verse;
using RimWorld.Planet;
using MeshDrawCommand = Waylith.Rendering.MeshDrawCommand;
using MeshDrawSubmission = Waylith.Rendering.MeshDrawSubmission;
using RenderTexture = UnityEngine.RenderTexture;

namespace Merthsoft.ColonyMonitor;

// 1.7's capture scope routes submissions into a private command list instead of
// the screen queue. Render that list directly: Camera.Render() would also copy
// pending commands belonging to the main map.
public sealed class CameraPreview : IDisposable
{
    private readonly List<MeshDrawCommand> commands = new();
    private GameObject cameraObject;
    private Camera camera;
    private RenderTexture target;
    public RenderTexture Texture => target;
    public int DrawCount { get; private set; }
    public Rect WorldRect { get; private set; }

    public void Refresh(Map map, Vector2 center, float size, int resolution)
    {
        if (CameraTextureRenderer.Current == null)
            throw new InvalidOperationException("RimWorld's offscreen renderer is unavailable.");
        if (MeshDrawSubmission.IsCapturing || PreviewContext.Active)
            throw new InvalidOperationException("Another render capture is already active.");
        EnsureTarget(resolution);
        WorldRect = new Rect(center.x - size, center.y - size, size * 2, size * 2);
        camera.orthographicSize = size;
        camera.transform.position = new Vector3(center.x, 100f, center.y);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        commands.Clear();
        bool singleThreadedDrawing = DebugViewSettings.singleThreadedDrawing;
        try
        {
            // Pawn pre-draw jobs can consult map/camera getters. Keep this small
            // preview on the scoped thread, rather than exposing context to workers.
            DebugViewSettings.singleThreadedDrawing = true;
            using (new PreviewContext(map, camera, WorldRect, size))
            using (MeshDrawSubmission.BeginCapture(commands))
            {
                map.mapDrawer.MapMeshDrawerUpdate_First();
                map.mapDrawer.DrawMapMesh();
                map.dynamicDrawManager.DrawDynamicThings();
            }
            DrawCount = commands.Count;
            // Submit immediately, while per-draw property ranges are still valid.
            CameraTextureRenderer.Current.Render(target, commands, CameraViewFactory.GetView(camera),
                new Rect(0, 0, 1, 1), Color.black);
        }
        finally
        {
            DebugViewSettings.singleThreadedDrawing = singleThreadedDrawing;
            // Commands reference frame-owned material properties; never retain them.
            commands.Clear();
        }
    }

    private void EnsureTarget(int resolution)
    {
        if (cameraObject == null)
        {
            cameraObject = new GameObject("Colony Monitor offscreen camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.aspect = 1f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
        }
        if (target != null && target.width == resolution) return;
        ReleaseTarget();
        target = new RenderTexture(resolution, resolution, 24);
        target.filterMode = FilterMode.Bilinear;
        if (!target.Create()) throw new InvalidOperationException("Could not allocate the camera render texture.");
        camera.targetTexture = target;
    }

    private void ReleaseTarget()
    {
        if (target == null) return;
        if (camera != null) camera.targetTexture = null;
        CameraTextureRenderer.Current?.ReleaseTarget(target);
        target.Release();
        UnityEngine.Object.Destroy(target);
        target = null;
    }

    public void Dispose()
    {
        ReleaseTarget();
        if (cameraObject != null) UnityEngine.Object.Destroy(cameraObject);
        cameraObject = null;
        camera = null;
        commands.Clear();
    }
}

// A mod may submit a persistent source outside the normal mesh queue. The
// engine's BeginCapture does not capture those sources. Fail closed rather
// than allowing the other map's geometry into the screen renderer.
[HarmonyPatch(typeof(MeshDrawSubmission), nameof(MeshDrawSubmission.SubmitPersistentSource))]
internal static class PreviewPersistentSourcePatch
{
    private static void Prefix()
    {
        if (PreviewContext.Active)
            throw new NotSupportedException("A persistent draw source cannot be isolated for this preview.");
    }
}

[HarmonyPatch(typeof(WorldRendererUtility), nameof(WorldRendererUtility.DrawingMap), MethodType.Getter)]
internal static class PreviewDrawingMapPatch
{
    private static bool Prefix(ref bool __result)
    {
        if (!PreviewContext.Active) return true;
        __result = true;
        return false;
    }
}

// Scope getter overrides to this thread and this synchronous draw only. Do not
// assign CurrentMap: its setter changes selection, camera state and map events.
internal sealed class PreviewContext : IDisposable
{
    [ThreadStatic] internal static PreviewContext Current;
    internal static bool Active => Current != null;
    internal readonly Map Map;
    internal readonly Camera Camera;
    internal readonly CellRect ViewRect;
    internal readonly float Size;

    internal PreviewContext(Map map, Camera camera, Rect rect, float size)
    {
        if (Current != null) throw new InvalidOperationException("Nested colony preview.");
        Map = map;
        Camera = camera;
        Size = size;
        ViewRect = CellRect.FromLimits((int)Math.Floor(rect.xMin), (int)Math.Floor(rect.yMin),
            (int)Math.Ceiling(rect.xMax), (int)Math.Ceiling(rect.yMax));
        Current = this;
    }

    public void Dispose() => Current = null;
}

[HarmonyPatch(typeof(Find), nameof(Find.CurrentMap), MethodType.Getter)]
internal static class PreviewMapPatch
{
    private static bool Prefix(ref Map __result)
    {
        if (!PreviewContext.Active) return true;
        __result = PreviewContext.Current.Map;
        return false;
    }
}

[HarmonyPatch(typeof(Find), nameof(Find.Camera), MethodType.Getter)]
internal static class PreviewCameraPatch
{
    private static bool Prefix(ref Camera __result)
    {
        if (!PreviewContext.Active) return true;
        __result = PreviewContext.Current.Camera;
        return false;
    }
}

[HarmonyPatch(typeof(CameraDriver), nameof(CameraDriver.CurrentViewRect), MethodType.Getter)]
internal static class PreviewViewPatch
{
    private static bool Prefix(ref CellRect __result)
    {
        if (!PreviewContext.Active) return true;
        __result = PreviewContext.Current.ViewRect;
        return false;
    }
}

[HarmonyPatch(typeof(CameraDriver), nameof(CameraDriver.RootSize), MethodType.Getter)]
internal static class PreviewSizePatch
{
    private static bool Prefix(ref float __result)
    {
        if (!PreviewContext.Active) return true;
        __result = PreviewContext.Current.Size;
        return false;
    }
}
