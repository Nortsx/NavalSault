using System.Collections.Generic;

namespace NavalSault.Utils;

// The tiles a ship covers, as offsets from its anchor tile for a ship facing North.
// The anchor is the tile the ship is moved by and turns around.
public class HexFootprint
{
    // Bow, anchor, stern in a single column.
    public static readonly HexFootprint Line1x3 = new("1x3", [(0, -1), (0, 0), (0, 1)]);

    // Two columns of three. Flat-topped hex columns are offset by half a tile, so the
    // starboard column sits half a tile further forward than the anchor column.
    public static readonly HexFootprint Wide2x3 = new("2x3", [(0, -1), (0, 0), (0, 1), (1, -2), (1, -1), (1, 0)]);

    public string Name { get; }
    // Offsets from the anchor while facing North; (0, 0) is the anchor itself.
    public IReadOnlyList<(int Q, int R)> Offsets { get; }

    public HexFootprint(string name, IReadOnlyList<(int Q, int R)> offsets)
    {
        Name = name;
        Offsets = offsets;
    }

    // Tiles covered with the anchor on the given tile and the bow pointing in the given direction.
    public IEnumerable<(int Q, int R)> Tiles((int Q, int R) anchor, HexDirection facing)
    {
        foreach (var offset in Offsets)
        {
            var rotated = HexDirections.Rotate(offset, facing);
            yield return (anchor.Q + rotated.Q, anchor.R + rotated.R);
        }
    }
}
