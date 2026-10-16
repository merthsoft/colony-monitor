using System;
using HarmonyLib;
using MeshDrawSubmission = Waylith.Rendering.MeshDrawSubmission;

namespace Merthsoft.ColonyMonitor.Patches;

// BeginCapture does not isolate persistent sources from the main render queue.
[HarmonyPatch(typeof(MeshDrawSubmission), nameof(MeshDrawSubmission.SubmitPersistentSource))]
internal static class MeshDrawSubmission_SubmitPersistentSource
{
    private static void Prefix()
    {
        if (PreviewContext.Active)
            throw new NotSupportedException("A persistent draw source cannot be isolated for this preview.");
    }
}
