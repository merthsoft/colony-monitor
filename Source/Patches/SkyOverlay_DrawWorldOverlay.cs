using HarmonyLib;
using UnityEngine;
using Verse;

namespace Merthsoft.ColonyMonitor.Patches;

[HarmonyPatch(typeof(SkyOverlay), nameof(SkyOverlay.DrawWorldOverlay), new[] { typeof(Map), typeof(Material), typeof(float), typeof(int) })]
internal static class SkyOverlay_DrawWorldOverlay
{
    private static bool Prefix(Map __0, Material __1, float __2, int __3)
    {
        if (!PreviewContext.Active)
            return true;

        // wholeMapPlane belongs to the active map and may have a different size.
        var size = __0.Size;
        var matrix = Matrix4x4.TRS(new Vector3(size.x / 2f, __2, size.z / 2f),
            Quaternion.identity, new Vector3(size.x, 1, size.z));
        Graphics.DrawMesh(MeshPool.plane10, matrix, __1, __3);
        return false;
    }
}
