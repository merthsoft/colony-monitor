using System;
using UnityEngine;
using Verse;

namespace Merthsoft.ColonyMonitor;

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
