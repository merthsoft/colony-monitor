using System;
using System.Collections.Generic;
using UnityEngine;
using UnityStubs.Rendering;
using Verse;
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
        
        if (target != null && target.width == resolution) 
            return;
        
        ReleaseTarget();
        target = new RenderTexture(resolution, resolution, 24)
        {
            filterMode = FilterMode.Bilinear
        };
        
        if (!target.Create()) 
            throw new InvalidOperationException("Could not allocate the camera render texture.");
        
        camera.targetTexture = target;
    }

    private void ReleaseTarget()
    {
        if (target == null) return;
        camera?.targetTexture = null;
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
