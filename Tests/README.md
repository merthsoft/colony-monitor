# Regression checks

Run `powershell -File Tests/Run.ps1`. For another installation, pass `-RimWorldDir` and `-HarmonyPath`.

The suite loads the built mod DLL and uses RimWorld's bundled runtime. Thirteen independent scenarios cover navigation, viewport projection, layout, native window ordering, native lightning overrides, water-layer separation, draw ordering and properties, weather blending, pointer ownership, camera interaction, save/load, and capture cleanup.

Assertions target regressions: wrong coordinate spaces, lost drag events, shader overrides losing precedence, modified main draw commands, and state leaking between maps or captures. Failures are reported by scenario; the process exits unsuccessfully if any fail.

These checks do not run the GPU or simulate the full UI event loop. Visual output, real mouse gestures, and mod compatibility still need in-game testing.
