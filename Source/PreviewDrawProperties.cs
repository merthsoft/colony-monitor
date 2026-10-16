using System;
using Waylith.Rendering;

namespace Merthsoft.ColonyMonitor;

internal sealed class PreviewDrawProperties
{
    private DrawProperty[] scratch = Array.Empty<DrawProperty>();

    internal MeshDrawCommand Apply(MeshDrawCommand command, DrawPropertyArena arena,
        ReadOnlySpan<DrawProperty> environment, DrawProperty? color)
    {
        int count = command.PropertyRange.Count + environment.Length + (color.HasValue ? 1 : 0);
        if (scratch.Length < count)
            Array.Resize(ref scratch, Math.Max(count, scratch.Length * 2));

        // Read first: allocating a range can resize the arena's backing array.
        arena.GetProperties(command.PropertyRange).CopyTo(scratch);
        environment.CopyTo(scratch.AsSpan(command.PropertyRange.Count));
        if (color.HasValue)
            scratch[count - 1] = color.Value;

        var properties = arena.Copy(scratch.AsSpan(0, count));
        return new MeshDrawCommand(command.Mesh, command.Material, properties, command.Matrix,
            command.SubmeshIndex, command.PassId, command.Instancing, command.RenderQueueOverride,
            command.Layer, command.Storage);
    }
}
