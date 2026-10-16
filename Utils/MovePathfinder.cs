using System;
using System.Collections.Generic;

namespace NavalSault.Utils;

// Works out which tiles a ship passes through on its way to a target, steering around blocked tiles.
public class MovePathfinder
{
    // Each step costs far more than any number of turns a route could add up, so the search always
    // finds a shortest route first and only uses turns to choose between equally short ones.
    private const int StepCost = 100;

    private readonly HexGrid _grid;

    public MovePathfinder(HexGrid grid)
    {
        _grid = grid;
    }

    // Returns the anchor tiles from start to goal (both included), or null if the goal can't be reached.
    // Among the shortest routes, picks the one with the least total turning (60° = 1), counting
    // the first step's turn away from facing. That makes the ship go straight first and turn afterwards.
    // A ship turns at most 60° per tile entered and never reverses. Before stepping in a new direction it
    // pivots on its anchor, so its whole footprint must fit both pivoted in place and on the next tile.
    // isOccupied marks extra tiles to avoid on top of the grid's own blocked tiles, e.g. other ships.
    // endFacing, if given, is the direction the ship must face on the goal.
    public List<(int Q, int R)> FindPath((int Q, int R) start, (int Q, int R) goal, HexDirection facing,
        HexFootprint footprint, Func<(int Q, int R), bool> isOccupied = null, HexDirection? endFacing = null)
    {
        bool Fits((int Q, int R) anchor, HexDirection direction)
        {
            foreach (var tile in footprint.Tiles(anchor, direction))
            {
                if (!_grid.IsPassable(tile) || (isOccupied != null && isOccupied(tile)))
                    return false;
            }
            return true;
        }

        if (endFacing.HasValue ? !Fits(goal, endFacing.Value) : !GoalFitsAnyFacing(goal, Fits))
            return null;

        // Search state is tile + direction the ship faces on it, since turning has a cost.
        var startState = (Tile: start, Facing: facing);
        var cost = new Dictionary<((int Q, int R) Tile, HexDirection Facing), int> { [startState] = 0 };
        var cameFrom = new Dictionary<((int Q, int R) Tile, HexDirection Facing), ((int Q, int R) Tile, HexDirection Facing)>();
        var queue = new PriorityQueue<((int Q, int R) Tile, HexDirection Facing), int>();
        queue.Enqueue(startState, 0);

        while (queue.TryDequeue(out var state, out int stateCost))
        {
            if (stateCost > cost[state])
                continue;

            if (state.Tile == goal && (!endFacing.HasValue || state.Facing == endFacing.Value))
                return BuildPath(cameFrom, state);

            for (int turn = -1; turn <= 1; turn++)
            {
                var direction = HexDirections.Turn(state.Facing, turn);
                var next = (Tile: HexDirections.Neighbor(state.Tile, direction), Facing: direction);
                if (!Fits(next.Tile, direction) || (turn != 0 && !Fits(state.Tile, direction)))
                    continue;

                int nextCost = stateCost + StepCost + HexDirections.TurnSize(state.Facing, direction);
                if (cost.TryGetValue(next, out int known) && known <= nextCost)
                    continue;

                cost[next] = nextCost;
                cameFrom[next] = state;
                queue.Enqueue(next, nextCost);
            }
        }

        return null;
    }

    private static bool GoalFitsAnyFacing((int Q, int R) goal, Func<(int Q, int R), HexDirection, bool> fits)
    {
        for (int i = 0; i < HexDirections.Count; i++)
        {
            if (fits(goal, (HexDirection)i))
                return true;
        }
        return false;
    }

    private static List<(int Q, int R)> BuildPath(
        Dictionary<((int Q, int R) Tile, HexDirection Facing), ((int Q, int R) Tile, HexDirection Facing)> cameFrom,
        ((int Q, int R) Tile, HexDirection Facing) end)
    {
        var path = new List<(int Q, int R)> { end.Tile };
        var state = end;
        while (cameFrom.TryGetValue(state, out state))
            path.Add(state.Tile);

        path.Reverse();
        return path;
    }
}
