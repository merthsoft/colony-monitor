using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading.Tasks;
using HarmonyLib;
using Merthsoft.ColonyMonitor;
using UnityEngine;
using Verse;
using Waylith.Rendering;

internal static class Program
{
    public static void Main()
    {
        var game = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam/steamapps/common/RimWorld");
        var harmony = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam/steamapps/workshop/content/294100/2009463077/1.7/Assemblies");
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            foreach (var directory in new[] { game, harmony })
            {
                var file = Path.Combine(directory, name.Name + ".dll");
                if (File.Exists(file)) return context.LoadFromAssemblyPath(file);
            }
            return null;
        };
        Run();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        new Harmony("Merthsoft.ColonyMonitor.CameraChecks").PatchAll();
        Check(!PreviewContext.Active, "context initially inactive");
        var bounds = new Rect(10.5f, 20.5f, 80, 80);
        try
        {
            using (new PreviewContext(map, null, bounds, 40))
            {
                Check(Find.CurrentMap == map, "draw uses monitored map");
                Check(PreviewContext.Current.ViewRect.minX == 10 && PreviewContext.Current.ViewRect.maxX == 91, "culling includes fractional edges");
                Check(Task.Run(() => !PreviewContext.Active).Result, "context does not leak to other threads");
                try { using var nested = new PreviewContext(map, null, bounds, 40); throw new Exception("Nested context was accepted"); }
                catch (InvalidOperationException) { Check(PreviewContext.Current.Map == map, "nested rejection preserves context"); }
                throw new ApplicationException("simulated draw failure");
            }
        }
        catch (ApplicationException) { }
        Check(!PreviewContext.Active, "context restored after draw exception");

        var before = new List<MeshDrawCommand>();
        MeshDrawSubmission.CopyPendingCommands(before, -1);
        var captured = new List<MeshDrawCommand>();
        try
        {
            using (MeshDrawSubmission.BeginCapture(captured))
            {
                var command = default(MeshDrawCommand);
                MeshDrawSubmission.Enqueue(in command);
                Check(captured.Count == 1, "offscreen scope captures draw command");
                throw new ApplicationException("simulated capture failure");
            }
        }
        catch (ApplicationException) { }
        Check(!MeshDrawSubmission.IsCapturing, "capture restored after failure");
        var after = new List<MeshDrawCommand>();
        MeshDrawSubmission.CopyPendingCommands(after, -1);
        Check(before.Count == after.Count, "preview commands do not enter main queue");
        using (new PreviewContext(map, null, bounds, 40))
        {
            try { MeshDrawSubmission.SubmitPersistentSource(null); throw new Exception("Persistent source escaped capture"); }
            catch (NotSupportedException) { Check(true, "unsupported persistent source blocked before screen submission"); }
        }
        Console.WriteLine("Camera isolation checks passed.");
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        Console.WriteLine("PASS: " + name);
    }
}
