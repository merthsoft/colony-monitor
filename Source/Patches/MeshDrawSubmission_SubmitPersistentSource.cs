using System;
using HarmonyLib;
using MeshDrawSubmission = Waylith.Rendering.MeshDrawSubmission;

namespace Merthsoft.ColonyMonitor.Patches;

// A mod may submit a persistent source outside the normal mesh queue. The
// engine's BeginCapture does not capture those sources. Fail closed rather
// than allowing the other map's geometry into the screen renderer.
[HarmonyPatch(typeof(MeshDrawSubmission), nameof(MeshDrawSubmission.SubmitPersistentSource))]
internal static class MeshDrawSubmission_SubmitPersistentSource
{
    private static void Prefix()
    {
        if (PreviewContext.Active)
            throw new NotSupportedException("A persistent draw source cannot be isolated for this preview.");
    }
}
