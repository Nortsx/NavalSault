using System.Collections.Generic;

namespace NavalSault.Utils;

// Works out which tiles a ship passes through on its way to a target.
// Currently a shortest route over open water; this is the place to route around islands and wrecks later.
public class MovePathfinder
{
    private readonly HexGrid _grid;

    public MovePathfinder(HexGrid grid)
    {
        _grid = grid;
    }

    // Returns tiles from start to goal (both included), or null if the goal can't be reached.
    // Walks one tile at a time: keeps the current direction while it still gets closer to the goal,
    // otherwise takes the smallest turn that does. So the ship goes straight first and turns afterwards.
    public List<(int Q, int R)> FindPath((int Q, int R) start, (int Q, int R) goal, HexDirection facing)
    {
        var path = new List<(int Q, int R)> { start };
        var current = start;
        var direction = facing;

        while (current != goal)
        {
            int remaining = HexGrid.Distance(current, goal);
            HexDirection? best = null;
            int bestTurn = int.MaxValue;

            for (int i = 0; i < HexDirections.Count; i++)
            {
                var candidate = (HexDirection)i;
                var next = HexDirections.Neighbor(current, candidate);
                if (HexGrid.Distance(next, goal) != remaining - 1 || !_grid.TryGetTilePosition(next.Q, next.R, out _))
                    continue;

                int turn = HexDirections.TurnSize(direction, candidate);
                if (turn < bestTurn)
                {
                    best = candidate;
                    bestTurn = turn;
                }
            }

            if (best == null)
                return null;

            direction = best.Value;
            current = HexDirections.Neighbor(current, direction);
            path.Add(current);
        }

        return path;
    }
}
