using HarmonyLib;
using UnityEngine;
using Verse;

namespace Merthsoft.ColonyMonitor;

public sealed class ColonyMonitorMod : Mod
{
    public static ColonyMonitorMod Instance;
    public static MonitorSettings Settings;

    public ColonyMonitorMod(ModContentPack content) : base(content)
    {
        Instance = this;
        Settings = GetSettings<MonitorSettings>();
        new Harmony("Merthsoft.ColonyMonitor").PatchAll();
    }

    public override string SettingsCategory() 
        => "Colony Monitor";
    
    public override void DoSettingsWindowContents(Rect rect) 
        => Settings.Draw(rect);
}
