using System;
using Microsoft.Xna.Framework;

namespace NavalSault.Utils;

// The six neighbours of a flat-topped hex, clockwise from screen-up (-Z).
public enum HexDirection
{
    North,
    NorthEast,
    SouthEast,
    South,
    SouthWest,
    NorthWest
}

public static class HexDirections
{
    public const int Count = 6;

    private static readonly (int Q, int R)[] Offsets =
    [
        (0, -1),
        (1, -1),
        (1, 0),
        (0, 1),
        (-1, 1),
        (-1, 0)
    ];

    public static (int Q, int R) Neighbor((int Q, int R) tile, HexDirection direction)
    {
        var offset = Offsets[(int)direction];
        return (tile.Q + offset.Q, tile.R + offset.R);
    }

    // Direction from a tile to one of its neighbours.
    public static HexDirection Between((int Q, int R) from, (int Q, int R) to)
    {
        var offset = (to.Q - from.Q, to.R - from.R);
        int index = Array.IndexOf(Offsets, offset);
        if (index < 0)
            throw new ArgumentException($"{to} is not a neighbour of {from}.");
        return (HexDirection)index;
    }

    // Number of 60° turns between two directions (0 = same, 3 = opposite).
    public static int TurnSize(HexDirection a, HexDirection b)
    {
        int diff = Math.Abs((int)a - (int)b);
        return Math.Min(diff, Count - diff);
    }

    // Direction after turning the given number of 60° steps clockwise (negative = anticlockwise).
    public static HexDirection Turn(HexDirection direction, int steps)
    {
        return (HexDirection)((((int)direction + steps) % Count + Count) % Count);
    }

    // Rotates an offset given for a North-facing shape so it matches the direction, clockwise around (0, 0).
    public static (int Q, int R) Rotate((int Q, int R) offset, HexDirection direction)
    {
        // Cube coordinates: x = Q, z = R, y = -x - z. One clockwise 60° step maps (x, y, z) to (-z, -x, -y).
        int x = offset.Q, z = offset.R, y = -x - z;
        for (int i = 0; i < (int)direction; i++)
            (x, y, z) = (-z, -x, -y);
        return (x, z);
    }

    // Rotation around Y that turns a model facing -Z towards the direction.
    public static float ToYaw(HexDirection direction)
    {
        return MathHelper.ToRadians(-60f * (int)direction);
    }
}
