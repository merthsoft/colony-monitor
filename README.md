# Colony Monitor prototype

RimWorld **1.7 only**. Requires Harmony.

## Use

Enable Colony Monitor (Prototype) alongside Harmony and restart. Click **Monitor** in the bottom-right play-settings row. Drag the window to move it. The map-name button selects any loaded colony, camp, or other map; clicking the image jumps to that map and location.

Use the **Mode** button to switch between:

- **Minimap** (default): schematic terrain/buildings with live pawn markers. Background refreshes every two seconds by default.
- **Camera** (experimental): real map geometry and dynamic things rendered to a separate texture. Starts at 512 x 512 and 2 FPS.

**Settings** opens the same options as Options > Mod options > Colony Monitor. Choose 256/512/1024 camera resolution, 1/2/5/10/15 FPS, 1/2/5/10-second minimap background refresh, and pawn-marker visibility. Settings persist between sessions.

For slower computers, start with minimap or camera at 256 pixels / 1 FPS. Increase resolution and refresh separately, watching game FPS/tick rate. There is no hardware auto-detection. The footer reports CPU time spent refreshing; it is not total GPU cost or a game performance benchmark.

Camera controls:

- **- / +** zoom out/in.
- **Colonists** centers on the monitored map's colonists (or map center if empty).
- **Use main view** copies camera location/zoom when the monitored map is also the main map. Set the framing, then switch the main view to another colony to watch remotely.
- Clicking the image still jumps the main camera; it does not pan the preview.

Green markers are player-controlled colonists, yellow are downed colonists, red are hostiles. Hover a marker for its name. Counts cover the entire map, including pawns outside the camera frame, while respecting fog for hostile/downed counts.

## Scope and limitations

Only the selected map is refreshed. Closing the window or changing mode/map releases its textures and camera. The mod does not generate maps or alter simulation ticking. Paused maps have stationary pawns even if preview refresh continues.

The experimental camera draws the game's map layers and dynamic things, including terrain, buildings, vegetation, items, and pawns, with fog respected by the game's draw path. It omits weather effects, flecks, designations, and UI overlays. Global lighting/shader effects may reflect the main map; exact lighting parity is not yet guaranteed. Compatibility with mods that maintain their own draw caches needs in-game testing. Unsupported persistent draw sources trigger a fallback rather than entering the main render queue.

If camera rendering throws, the window falls back to minimap for the session and logs the exception. Click Mode to retry. Minimap remains available independently.

## Build and checks

`dotnet build Source/Merthsoft.ColonyMonitor.csproj -c Release`

Uses .NET 10 and installed 1.7 assemblies. Override `RimWorldDir` and `HarmonyPath` through MSBuild properties for another installation. Output: `1.7/Assemblies/Merthsoft.ColonyMonitor.dll`.

`dotnet run --project Tests/CameraChecks.csproj -c Release`

Checks against the real 1.7/Harmony assemblies cover scoped map overrides, nested-capture rejection, thread isolation, cleanup after exceptions, private draw capture, unchanged main draw queue, and fail-closed handling of persistent draw sources. These checks do not initialize a GPU or replace visual in-game validation.

In-game checks: switch between a colony and a camp, compare preview orientation/click locations, test both modes and quality settings, close/reopen repeatedly, change the monitored map, verify save/load, and compare FPS/tick rate with the monitor open/closed.
