using System;
using System.Collections.Generic;
using System.Linq;
using NavalSault.Utils;

namespace NavalSault.GameLogic;

// What each side can see: every tile within view radius of any tile of its ships' hulls.
// A side always knows where its own ships are, and remembers where it last saw each opposing ship.
public class FogOfWar
{
    private readonly Dictionary<Side, HashSet<(int Q, int R)>> _visible = new();
    private readonly Dictionary<Side, Dictionary<Ship, (int Q, int R)>> _lastSeen = new();

    public FogOfWar()
    {
        foreach (var side in Enum.GetValues<Side>())
        {
            _visible[side] = new HashSet<(int Q, int R)>();
            _lastSeen[side] = new Dictionary<Ship, (int Q, int R)>();
        }
    }

    // Recomputes sight from where the ships are now. Sunk ships see nothing.
    public void Update(IReadOnlyList<Ship> ships)
    {
        foreach (var (side, visible) in _visible)
        {
            visible.Clear();
            foreach (var ship in ships)
            {
                if (ship.Side != side || ship.IsSunk)
                    continue;

                foreach (var hullTile in ship.Tiles)
                    visible.UnionWith(HexGrid.TilesWithin(hullTile, ship.ViewRadius));
            }

            var lastSeen = _lastSeen[side];
            foreach (var ship in ships)
            {
                if (ship.Side == side || ship.IsSunk)
                    continue;

                if (CanSee(side, ship))
                    lastSeen[ship] = ship.Tile;
                // Looked where it was last seen and it's gone.
                else if (lastSeen.TryGetValue(ship, out var tile) && visible.Contains(tile))
                    lastSeen.Remove(ship);
            }

            foreach (var gone in lastSeen.Keys.Where(s => s.IsSunk || !ships.Contains(s)).ToList())
                lastSeen.Remove(gone);
        }
    }

    public bool IsVisible(Side side, (int Q, int R) tile)
    {
        return _visible[side].Contains(tile);
    }

    // True if the side sees any part of the ship's hull, or the ship is its own.
    public bool CanSee(Side side, Ship ship)
    {
        return ship.Side == side || ship.Tiles.Any(_visible[side].Contains);
    }

    // Anchor tile each opposing ship was last seen on, for ships the side has seen and not yet lost track of.
    public IReadOnlyDictionary<Ship, (int Q, int R)> LastSeen(Side side)
    {
        return _lastSeen[side];
    }
}
