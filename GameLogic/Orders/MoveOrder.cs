using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NavalSault.Utils;

namespace NavalSault.GameLogic.Orders;

// Sail along a path of tiles. Costs 1 turn point per tile entered.
public class MoveOrder : Order
{
    private readonly HexGrid _grid;

    // Tiles from the start tile to the destination, both included.
    public IReadOnlyList<(int Q, int R)> Path { get; }

    public MoveOrder(Ship ship, IReadOnlyList<(int Q, int R)> path, HexGrid grid) : base(ship)
    {
        Path = path;
        _grid = grid;
    }

    public override int TurnPointCost => Path.Count - 1;
    public override (int Q, int R) EndTile => Path[^1];
    public override HexDirection EndFacing => HexDirections.Between(Path[^2], Path[^1]);

    public override void Begin()
    {
        // Each waypoint carries the direction of the step into it.
        var waypoints = new List<((int Q, int R), Vector3, HexDirection)>(Path.Count - 1);
        for (int i = 1; i < Path.Count; i++)
        {
            _grid.TryGetTilePosition(Path[i].Q, Path[i].R, out var position);
            waypoints.Add((Path[i], position, HexDirections.Between(Path[i - 1], Path[i])));
        }

        Ship.MoveAlong(waypoints);
    }

    public override bool Update(GameTime gameTime)
    {
        return !Ship.IsMoving;
    }
}
