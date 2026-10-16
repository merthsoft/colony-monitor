using System;
using UnityEngine;
using Verse;

namespace Merthsoft.ColonyMonitor;

public sealed class CameraViewState : IExposable
{
    public int MapId;
    public Vector2 Center;
    public float Size = 40f;
    public Pawn Follow;

    public void ExposeData()
    {
        Scribe_Values.Look(ref MapId, "mapId");
        Scribe_Values.Look(ref Center, "center");
        Scribe_Values.Look(ref Size, "size", 40f);
        Scribe_References.Look(ref Follow, "follow");
    }

    public void Clamp(IntVec3 mapSize)
    {
        Size = float.IsFinite(Size) ? Math.Clamp(Size, 8f, FullMapSize(mapSize)) : FullMapSize(mapSize);
        Center = new Vector2(float.IsFinite(Center.x) ? Math.Clamp(Center.x, 0, mapSize.x) : mapSize.x / 2f,
            float.IsFinite(Center.y) ? Math.Clamp(Center.y, 0, mapSize.z) : mapSize.z / 2f);
    }

    private static float FullMapSize(IntVec3 mapSize) => Math.Max(8f, Math.Max(mapSize.x, mapSize.z) / 2f);

    public void FitMap(IntVec3 mapSize)
    {
        Center = new Vector2(mapSize.x / 2f, mapSize.z / 2f);
        Size = FullMapSize(mapSize);
        Follow = null;
    }

    public void Pan(Vector2 deltaPixels, float imageSize, IntVec3 mapSize)
    {
        if (imageSize <= 0) return;
        Center += new Vector2(-deltaPixels.x, deltaPixels.y) * (Size * 2 / imageSize);
        Follow = null;
        Clamp(mapSize);
    }

    public void Zoom(float factor, Vector2 anchor, IntVec3 mapSize)
    {
        if (!float.IsFinite(factor) || factor <= 0) return;
        float previous = Size;
        Size = Math.Clamp(Size * factor, 8f, FullMapSize(mapSize));

        Center += new Vector2(anchor.x - 0.5f, 0.5f - anchor.y) * (2 * (previous - Size));
        Clamp(mapSize);
    }
}
