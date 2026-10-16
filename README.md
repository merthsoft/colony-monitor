# Colony Monitor

RimWorld 1.7. Requires Harmony. Enable both mods and restart the game.

## Use

Click the monitor icon in the lower-right corner to show or hide it. Right-click the icon for controls and settings.

- **Floating window:** choose a colony or camp to watch. Drag the window to move it; its position is remembered.
- **Docked:** a compact view without buttons. Follow the current map or pin another colony from the icon menu.
- **Minimap:** terrain, buildings, and pawn markers.
- **Camera:** the full game view, with lighting and optional weather.

Click the picture to jump to that location. In camera mode, scroll to zoom and right-drag to pan. Use **Center** or **Follow colonist** from the window or icon menu. Each map remembers its camera view.

Markers: green for colonists, yellow for downed colonists, red for hostiles. A gray rectangle shows your viewport when watching the current map; turn it off with Current viewport in settings.

## Settings

Open **Options > Mod settings > Colony Monitor**, or choose **Settings** from the icon menu. All settings and camera controls are available there, including when docked.

Choose the display style, dock corner and size, picture quality, markers, weather, and icon placement. Choosing a corner resets the margins. For slower machines, use Minimap or Camera at 256 pixels and 1 FPS.

## Limits

Only loaded maps can be watched. The monitor does not change simulation speed. Camera mode may differ from the main view in saturation and some effects. If it fails, it switches to Minimap; use **Refresh preview** to retry.

## Build

Requires .NET 10 and installed RimWorld 1.7/Harmony assemblies.

```powershell
dotnet build Source/Merthsoft.ColonyMonitor.csproj -c Release
powershell -File Tests/Run.ps1
```

Set `RimWorldDir` and `HarmonyPath` MSBuild properties for a different installation.
