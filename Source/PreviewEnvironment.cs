using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using UnityStubs.Rendering;
using Verse;
using Waylith.Rendering;
using NativeMaterial = Waylith.Rendering.Material;
using Vector4 = System.Numerics.Vector4;

namespace Merthsoft.ColonyMonitor;

internal sealed class PreviewEnvironment
{
    private static readonly Dictionary<System.Type, FieldInfo[]> materialFields = new();
    private static readonly int shineSize = (int)AccessTools.Field(typeof(SkyManager), "LightsourceShineSizeReduction").GetValue(null);
    private static readonly int shineIntensity = (int)AccessTools.Field(typeof(SkyManager), "LightsourceShineIntensity").GetValue(null);
    private static readonly int dayPercent = (int)AccessTools.Field(typeof(SkyManager), "DayPercent").GetValue(null);
    private static readonly Color fogBase = (Color)AccessTools.Field(typeof(SkyManager), "FogOfWarBaseColor").GetValue(null);
    private readonly Dictionary<NativeMaterial, Color> colors = new();
    private readonly PreviewWeatherLayers weather = new();
    private readonly PreviewDrawProperties properties = new();
    private readonly List<DrawProperty> common = new();

    internal void Prepare(Map map, Camera camera, bool showWeather)
    {
        colors.Clear();
        common.Clear();
        var sky = map.skyManager.CurSky;
        bool unlit = map.Biome.disableSkyLighting;
        AddColor(MatBases.LightOverlay, unlit ? new Color(1, 1, 1, 0) : sky.colors.sky);
        var fogColor = map.FogOfWarColor ?? fogBase;
        var skyColor = sky.colors.sky;
        skyColor.a = 1;
        AddColor(MatBases.FogOfWar, unlit ? fogColor : skyColor * fogColor);
        float strength = GenCelestial.CurShadowStrength(map);
        var shadowSource = GenCelestial.GetLightSourceInfo(map, GenCelestial.LightType.Shadow);
        PreviewShadows.Resolve(shadowSource.vector, PreviewShadows.Override(map.skyManager), sky.colors.shadow,
            strength, out var shadowDirection, out var shadow);
        AddColor(MatBases.SunShadow, shadow);
        AddColor(MatBases.SunShadowFade, shadow);

        var sun = GenCelestial.GetLightSourceInfo(map, GenCelestial.LightType.LightingSun);
        var moon = GenCelestial.GetLightSourceInfo(map, GenCelestial.LightType.LightingMoon);
        common.Add(DrawProperty.Vector(ShaderPropertyIDs.MapSunLightDirection, shadowDirection));
        common.Add(DrawProperty.Vector(ShaderPropertyIDs.WaterCastVectSun, LightVector(sun, sun.intensity)));
        common.Add(DrawProperty.Vector(ShaderPropertyIDs.WaterCastVectMoon, LightVector(moon, moon.intensity)));
        common.Add(DrawProperty.Float(shineSize, 20f / Mathf.Max(0.001f, sky.lightsourceShineSize)));
        common.Add(DrawProperty.Float(shineIntensity, sky.lightsourceShineIntensity));
        common.Add(DrawProperty.Float(dayPercent, GenLocalDate.DayPercent(map)));
        common.Add(DrawProperty.Vector(ShaderPropertyIDs.MapSize, new Vector4(map.Size.x, map.Size.z, 0, 0)));
        common.Add(DrawProperty.Vector(ShaderPropertyIDs.MainCameraScreenParams,
            new Vector4(camera.targetTexture.width, camera.targetTexture.height,
                1f / camera.targetTexture.width, 1f / camera.targetTexture.height)));
        common.Add(DrawProperty.Matrix(ShaderPropertyIDs.MainCameraVP, CameraViewFactory.GetView(camera).ViewProjection));

        weather.Layers.Clear();
        if (!showWeather)
            return;

        weather.Collect(map);
        foreach (var entry in weather.Layers)
        {
            var color = PreviewWeatherLayers.ColorFor(entry.Key, sky.colors.overlay, entry.Value);
            foreach (var field in GetMaterialFields(entry.Key.GetType()))
                AddColor(field.GetValue(field.IsStatic ? null : entry.Key) as UnityEngine.Material, color);
        }
    }

    internal void DrawWeather(Map map)
    {
        foreach (var overlay in weather.Layers.Keys)
            overlay.DrawOverlay(map);
        map.weatherManager.eventHandler.WeatherEventsDraw();
    }

    internal void Apply(List<MeshDrawCommand> commands)
    {
        var environment = common.ToArray();
        var arena = MeshDrawSubmission.ActiveArena;
        for (int i = 0; i < commands.Count; i++)
        {
            var command = commands[i];
            DrawProperty? color = null;
            if (command.Material != null && colors.TryGetValue(command.Material, out var tint))
                color = DrawProperty.Color(ShaderPropertyIDs.Color, new Vector4(tint.r, tint.g, tint.b, tint.a));
            commands[i] = properties.Apply(command, arena, environment, color);
        }
    }

    private void AddColor(UnityEngine.Material material, Color color)
    {
        if (material != null)
            colors[material.engineMaterial] = color;
    }

    private static Vector4 LightVector(GenCelestial.LightInfo source, float intensity)
        => new(source.vector.x, 0, source.vector.y, intensity);

    private static FieldInfo[] GetMaterialFields(System.Type type)
    {
        if (materialFields.TryGetValue(type, out var cached))
            return cached;

        var result = new List<FieldInfo>();
        for (var current = type; current != null && typeof(SkyOverlay).IsAssignableFrom(current); current = current.BaseType)
            foreach (var field in current.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                if (typeof(UnityEngine.Material).IsAssignableFrom(field.FieldType))
                    result.Add(field);

        return materialFields[type] = result.ToArray();
    }
}
