using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using UnityStubs.Rendering;
using Verse;
using Waylith.Rendering;
using RenderTexture = UnityEngine.RenderTexture;
using Texture2D = UnityEngine.Texture2D;

namespace Merthsoft.ColonyMonitor;

internal sealed class PreviewWater : IDisposable
{
    private readonly List<MeshDrawCommand> depthCommands = new();
    private readonly PreviewDrawProperties properties = new();
    private RenderTexture depth;
    private Texture2D flow;
    private Map flowMap;

    internal static void Split(List<MeshDrawCommand> commands, List<MeshDrawCommand> water, int waterLayer, int visibleMask)
    {
        water.Clear();
        int kept = 0;
        for (int i = 0; i < commands.Count; i++)
        {
            var command = commands[i];
            if (command.Layer == waterLayer) water.Add(command);
            else if ((visibleMask & (1 << command.Layer)) != 0) commands[kept++] = command;
        }
        commands.RemoveRange(kept, commands.Count - kept);
    }

    internal void Render(Map map, Camera camera, List<MeshDrawCommand> commands, int visibleMask)
    {
        Split(commands, depthCommands, SubcameraDefOf.WaterDepth.LayerId, visibleMask);
        if (depthCommands.Count == 0) return;
        if (depth == null || depth.width != camera.targetTexture.width)
        {
            ReleaseDepth();
            depth = new RenderTexture(camera.targetTexture.width, camera.targetTexture.height, 0, RenderTextureFormat.RFloat);
            if (!depth.Create()) throw new InvalidOperationException("Could not allocate the water texture.");
        }
        var arena = MeshDrawSubmission.ActiveArena;
        var flowTexture = GetFlow(map);
        var flowProperty = new[] { DrawProperty.Texture(ShaderPropertyIDs.WaterOffsetTex, flowTexture.CoreTexture) };
        for (int i = 0; i < depthCommands.Count; i++)
            depthCommands[i] = properties.Apply(depthCommands[i], arena, flowProperty, null);
        try
        {
            CameraTextureRenderer.Current.Render(depth, depthCommands, CameraViewFactory.GetView(camera),
                new Rect(0, 0, 1, 1), new Color(0.5f, 0.5f, 0.5f, 1));
        }
        finally { depthCommands.Clear(); }
        var waterProperties = new[] {
            DrawProperty.Texture(ShaderPropertyIDs.WaterOutputTex, depth.CoreTexture),
            DrawProperty.Texture(ShaderPropertyIDs.WaterReflectionTex, ContentFinder<Texture2D>.Get("Other/WaterReflection").CoreTexture)
        };
        for (int i = 0; i < commands.Count; i++)
            commands[i] = properties.Apply(commands[i], arena, waterProperties, null);
    }

    private Texture2D GetFlow(Map map)
    {
        var existing = AccessTools.Field(typeof(WaterInfo), "riverFlowTexture").GetValue(map.waterInfo) as Texture2D;
        if (existing != null) return existing;
        if (flowMap == map && flow != null) return flow;
        if (flow != null) UnityEngine.Object.Destroy(flow);
        flow = null;
        flowMap = map;
        var values = AccessTools.Field(typeof(WaterInfo), "riverFlowMap").GetValue(map.waterInfo) as List<float>;
        if (values == null || values.Count < map.Size.x * map.Size.z * 2) return Texture2D.blackTexture;
        var pixels = new Color[map.Size.x * map.Size.z];
        for (int z = 0; z < map.Size.z; z++)
        for (int x = 0; x < map.Size.x; x++)
        {
            var cell = new IntVec3(x, 0, z);
            if (!map.terrainGrid.BaseTerrainAt(cell).IsRiver) continue;
            int index = (x * map.Size.z + z) * 2;
            var velocity = new Vector2(values[index], values[index + 1]);
            int count = 1;
            foreach (var offset in GenAdj.AdjacentCells)
            {
                var adjacent = cell + offset;
                if (!adjacent.InBounds(map) || !map.terrainGrid.BaseTerrainAt(adjacent).IsRiver) continue;
                int other = (adjacent.x * map.Size.z + adjacent.z) * 2;
                velocity += new Vector2(values[other], values[other + 1]);
                count++;
            }
            pixels[z * map.Size.x + x] = new Color(velocity.x / count, velocity.y / count, 0);
        }
        flow = new Texture2D(map.Size.x, map.Size.z, TextureFormat.RGFloat, false) { wrapMode = UnityEngine.TextureWrapMode.Clamp };
        flow.SetPixels(pixels);
        flow.Apply(false, false);
        return flow;
    }

    private void ReleaseDepth()
    {
        if (depth == null) return;
        CameraTextureRenderer.Current?.ReleaseTarget(depth);
        depth.Release();
        UnityEngine.Object.Destroy(depth);
        depth = null;
    }

    public void Dispose()
    {
        ReleaseDepth();
        if (flow != null) UnityEngine.Object.Destroy(flow);
        flow = null;
        flowMap = null;
        depthCommands.Clear();
    }
}
