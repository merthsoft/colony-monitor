using System.Collections.Generic;
using Waylith.Rendering;

namespace Merthsoft.ColonyMonitor;

internal sealed class PreviewDrawOrder
{
    private readonly List<(MeshDrawCommand Command, int Queue, int Index)> ordered = new();

    internal void Apply(List<MeshDrawCommand> commands)
    {
        ordered.Clear();
        for (int i = 0; i < commands.Count; i++)
        {
            var command = commands[i];
            int queue = command.RenderQueueOverride > 0
                ? command.RenderQueueOverride : command.Material?.RenderQueue ?? 2000;
            ordered.Add((command, queue, i));
        }

        // Unlike the screen renderer, the texture baker does not sort queues.
        ordered.Sort((a, b) => a.Queue != b.Queue
            ? a.Queue.CompareTo(b.Queue) : a.Index.CompareTo(b.Index));
        for (int i = 0; i < ordered.Count; i++)
            commands[i] = ordered[i].Command;
        ordered.Clear();
    }
}
