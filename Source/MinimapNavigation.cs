using UnityEngine;
using Verse;

namespace Merthsoft.ColonyMonitor;

internal static class MinimapNavigation
{
    internal static IntVec3 Target(Vector2 pointer, Rect screenImage, Rect world, IntVec3 mapSize)
    {
        float x = Mathf.Clamp01((pointer.x - screenImage.x) / screenImage.width);
        float z = Mathf.Clamp01((screenImage.yMax - pointer.y) / screenImage.height);
        return new IntVec3(Mathf.Clamp((int)(world.x + x * world.width), 0, mapSize.x - 1), 0,
            Mathf.Clamp((int)(world.y + z * world.height), 0, mapSize.z - 1));
    }
}
