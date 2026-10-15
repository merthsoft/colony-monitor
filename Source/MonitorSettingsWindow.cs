using UnityEngine;
using Verse;

namespace Merthsoft.ColonyMonitor;

internal sealed class MonitorSettingsWindow : Window
{
    public override Vector2 InitialSize => new(610, 570);

    internal MonitorSettingsWindow()
    {
        doCloseX = true;
        doCloseButton = true;
    }

    public override void DoWindowContents(Rect rect) 
        => ColonyMonitorMod.Settings.Draw(new Rect(0, 0, rect.width, rect.height - 50));
    
    public override void PostClose() 
    { 
        ColonyMonitorMod.Instance.WriteSettings(); 
        base.PostClose(); 
    }
}
