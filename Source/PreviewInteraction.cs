using UnityEngine;
using Verse;

namespace Merthsoft.ColonyMonitor;

public sealed class PreviewInteraction
{
    private Vector2 lastPointer;

    public void Begin(Vector2 pointer) => lastPointer = pointer;

    public void Move(Vector2 pointer, CameraViewState view, float imageSize, IntVec3 mapSize)
    {
        // GUI delta can be missing or repeated in 1.7.
        view.Pan(pointer - lastPointer, imageSize, mapSize);
        lastPointer = pointer;
    }

    public static bool ProjectCachedImage(Rect captured, Rect requested, Rect image, out Rect destination, out Rect uv)
    {
        float left = Mathf.Max(captured.xMin, requested.xMin);
        float right = Mathf.Min(captured.xMax, requested.xMax);
        float bottom = Mathf.Max(captured.yMin, requested.yMin);
        float top = Mathf.Min(captured.yMax, requested.yMax);
        destination = uv = default;
        if (right <= left || top <= bottom) return false;
        destination = new Rect(image.x + (left - requested.x) / requested.width * image.width,
            image.y + (requested.yMax - top) / requested.height * image.height,
            (right - left) / requested.width * image.width, (top - bottom) / requested.height * image.height);
        uv = new Rect((left - captured.x) / captured.width, (bottom - captured.y) / captured.height,
            (right - left) / captured.width, (top - bottom) / captured.height);
        return true;
    }
}
