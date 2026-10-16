using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace Merthsoft.ColonyMonitor;

internal static class MonitorWindowOrder
{
    private static readonly AccessTools.FieldRef<WindowStack, List<Window>> windows =
        AccessTools.FieldRefAccess<WindowStack, List<Window>>("windows");
    private static readonly Action<WindowStack, Window> insert = AccessTools.MethodDelegate<Action<WindowStack, Window>>(
        AccessTools.Method(typeof(WindowStack), "InsertAtCorrectPositionInList"));

    internal static void SetLayer(Window window, WindowLayer layer, WindowStack stack)
    {
        if (window.layer == layer) return;
        window.layer = layer;
        // Close/Add would dispose the preview and change its visibility setting.
        if (windows(stack).Remove(window)) insert(stack, window);
    }
}
