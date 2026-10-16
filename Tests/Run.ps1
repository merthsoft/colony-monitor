param(
    [string]$RimWorldDir = "${env:ProgramFiles(x86)}\Steam\steamapps\common\RimWorld",
    [string]$HarmonyPath = "${env:ProgramFiles(x86)}\Steam\steamapps\workshop\content\294100\2009463077\1.7\Assemblies\0Harmony.dll"
)

$ErrorActionPreference = 'Stop'
dotnet build "$PSScriptRoot\CameraChecks.csproj" -c Release --nologo "-p:RimWorldDir=$RimWorldDir" "-p:HarmonyPath=$HarmonyPath"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Harmony requires the game's JIT.
dotnet exec --runtimeconfig "$RimWorldDir\RimWorld.runtimeconfig.json" --depsfile "$RimWorldDir\RimWorld.deps.json" "$PSScriptRoot\bin\ColonyMonitor.CameraChecks.dll" "$RimWorldDir" (Split-Path -Parent $HarmonyPath)
exit $LASTEXITCODE
