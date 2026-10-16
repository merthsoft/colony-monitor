using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Merthsoft.ColonyMonitor;

internal sealed class PreviewWeatherLayers
{
    internal readonly Dictionary<SkyOverlay, float> Layers = new();

    internal void Add(SkyOverlay overlay, float opacity)
    {
        if (overlay == null || opacity <= 0)
            return;

        Layers.TryGetValue(overlay, out float previous);
        Layers[overlay] = Mathf.Clamp01(previous + opacity);
    }

    internal void Collect(Map map)
    {
        Layers.Clear();
        var weather = map.weatherManager;
        float transition = weather.TransitionLerpFactor;
        foreach (var overlay in weather.curWeather.Worker.overlays)
            Add(overlay, transition);
        foreach (var overlay in weather.lastWeather.Worker.overlays)
            Add(overlay, 1 - transition);
        foreach (var condition in map.gameConditionManager.ActiveConditions)
        {
            var overlays = condition.SkyOverlays(map);
            if (overlays == null)
                continue;

            foreach (var overlay in overlays)
                Add(overlay, condition.SkyTargetLerpFactor(map));
        }
    }

    internal static Color ColorFor(SkyOverlay overlay, Color skyColor, float opacity)
    {
        var color = overlay.ForcedOverlayColor ?? skyColor;
        color.a = Mathf.Clamp01(opacity);
        return color;
    }
}
