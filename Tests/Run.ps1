param(
    [string]$RimWorldDir = "${env:ProgramFiles(x86)}\Steam\steamapps\common\RimWorld"
)

$ErrorActionPreference = 'Stop'
dotnet build "$PSScriptRoot\CameraChecks.csproj" -c Release --nologo "-p:RimWorldDir=$RimWorldDir"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Harmony must run on the same JIT as the game, including when the system's
# dotnet muxer selects an older preview runtime.
dotnet exec --runtimeconfig "$RimWorldDir\RimWorld.runtimeconfig.json" --depsfile "$RimWorldDir\RimWorld.deps.json" "$PSScriptRoot\bin\ColonyMonitor.CameraChecks.dll"
exit $LASTEXITCODE
