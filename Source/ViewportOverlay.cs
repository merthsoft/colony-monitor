using UnityEngine;
using Verse;

namespace Merthsoft.ColonyMonitor;

internal static class ViewportOverlay
{
    internal static Rect Project(Rect viewport, Rect world, Rect image)
        => new(image.x + (viewport.x - world.x) * image.width / world.width,
            image.yMax - (viewport.yMax - world.y) * image.height / world.height,
            viewport.width * image.width / world.width, viewport.height * image.height / world.height);

    internal static void Draw(Camera camera, Rect world, Rect image)
    {
        float minX = float.PositiveInfinity, minZ = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxZ = float.NegativeInfinity;
        for (int i = 0; i < 4; i++)
        {
            var ray = camera.ViewportPointToRay(new Vector3(i % 2, i / 2, 0));
            if (Mathf.Abs(ray.direction.y) < 0.0001f) return;
            var point = ray.origin + ray.direction * (-ray.origin.y / ray.direction.y);
            if (!float.IsFinite(point.x) || !float.IsFinite(point.z)) return;
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minZ = Mathf.Min(minZ, point.z);
            maxZ = Mathf.Max(maxZ, point.z);
        }
        var projected = Project(Rect.MinMaxRect(minX, minZ, maxX, maxZ), world, image);
        if (!image.Overlaps(projected)) return;
        GUI.BeginGroup(image);
        projected.position -= image.position;
        Widgets.DrawBoxSolid(projected, new Color(0.65f, 0.65f, 0.65f, 0.12f));
        var color = GUI.color;
        GUI.color = new Color(0.65f, 0.65f, 0.65f, 0.9f);
        Widgets.DrawBox(projected, 2);
        GUI.color = color;
        GUI.EndGroup();
    }
}
