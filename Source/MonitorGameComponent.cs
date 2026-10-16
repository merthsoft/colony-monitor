using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Merthsoft.ColonyMonitor;

public sealed class MonitorGameComponent : GameComponent
{
    private List<CameraViewState> views = new();
    public int SelectedMapId = -1;

    public MonitorGameComponent(Game game) { }

    public override void GameComponentUpdate()
    {
        var settings = ColonyMonitorMod.Settings;
        if (settings == null || Find.CurrentMap == null) return;
        var window = Find.WindowStack.Windows.OfType<MonitorWindow>().FirstOrDefault();
        if (settings.Enabled && window == null) Find.WindowStack.Add(new MonitorWindow());
        if (!settings.Enabled && window != null) window.Close();
    }

    public CameraViewState GetView(Map map, out bool created)
    {
        var view = views.Find(v => v.MapId == map.uniqueID);
        created = view == null;
        if (created)
        {
            view = new CameraViewState { MapId = map.uniqueID };
            views.Add(view);
        }
        view.Clamp(map.Size);
        return view;
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref SelectedMapId, "colonyMonitorSelectedMap", -1);
        Scribe_Collections.Look(ref views, "colonyMonitorCameraViews", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            views ??= new List<CameraViewState>();
            views.RemoveAll(v => v == null);
        }
    }
}
