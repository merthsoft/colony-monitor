using System;
using System.Collections.Generic;
using UnityEngine;
using UnityStubs.Rendering;
using Verse;
using MeshDrawCommand = Waylith.Rendering.MeshDrawCommand;
using MeshDrawSubmission = Waylith.Rendering.MeshDrawSubmission;
using RenderTexture = UnityEngine.RenderTexture;

namespace Merthsoft.ColonyMonitor;

// Camera.Render() also copies pending main-map commands.
public sealed class CameraPreview : IDisposable
{
    private readonly List<MeshDrawCommand> commands = new();
    private GameObject cameraObject;
    private Camera camera;
    private RenderTexture target;
    private readonly PreviewEnvironment environment = new();
    private readonly PreviewDrawOrder drawOrder = new();
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
            // Worker threads cannot see PreviewContext.
            DebugViewSettings.singleThreadedDrawing = true;
            bool showWeather = ColonyMonitorMod.Settings.ShowWeather;
            environment.Prepare(map, camera, showWeather);
            using (new PreviewContext(map, camera, WorldRect, size))
            using (MeshDrawSubmission.BeginCapture(commands))
            {
                map.mapDrawer.MapMeshDrawerUpdate_First();
                map.mapDrawer.DrawMapMesh();
                map.dynamicDrawManager.DrawDynamicThings();
                if (showWeather)
                    environment.DrawWeather(map);
            }
            environment.Apply(commands);
            drawOrder.Apply(commands);
            DrawCount = commands.Count;

            CameraTextureRenderer.Current.Render(target, commands, CameraViewFactory.GetView(camera),
                new Rect(0, 0, 1, 1), Color.black);
        }
        finally
        {
            DebugViewSettings.singleThreadedDrawing = singleThreadedDrawing;
            // Property ranges expire with the frame.
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
