using UnityEngine;

namespace Merthsoft.ColonyMonitor;

internal static class MonitorLayout
{
    internal static Rect FloatingRect(Rect rect, Vector2 position, float screenWidth, float screenHeight)
        => new(Mathf.Clamp(position.x, 0, Mathf.Max(0, screenWidth - rect.width)),
            Mathf.Clamp(position.y, 0, Mathf.Max(0, screenHeight - rect.height)), rect.width, rect.height);

    internal static Rect DockRect(float screenWidth, float screenHeight, int size, int corner, int insetX, int insetY)
    {
        float side = Mathf.Min(size, Mathf.Min(screenWidth, screenHeight));
        float x = corner % 2 == 0 ? insetX : screenWidth - side - insetX;
        float y = corner < 2 ? insetY : screenHeight - side - insetY;
        return new Rect(Mathf.Clamp(x, 0, screenWidth - side), Mathf.Clamp(y, 0, screenHeight - side), side, side);
    }
}
