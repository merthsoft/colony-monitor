using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Merthsoft.ColonyMonitor;

internal static class PreviewShadows
{
    private static readonly Func<SkyManager, Vector3?> getOverride = AccessTools.MethodDelegate<Func<SkyManager, Vector3?>>(
        AccessTools.Method(typeof(SkyManager), "GetOverridenShadowVector"));

    internal static Vector3? Override(SkyManager sky) => getOverride(sky);

    internal static void Resolve(Vector2 celestial, Vector3? overridden, Color color, float strength,
        out System.Numerics.Vector4 direction, out Color tint)
    {
        var vector = overridden.HasValue ? new Vector2(overridden.Value.x, overridden.Value.y) : celestial;
        direction = new System.Numerics.Vector4(vector.x, 0, vector.y, strength);
        tint = overridden.HasValue ? color : Color.Lerp(Color.white, color, strength);
    }
}
