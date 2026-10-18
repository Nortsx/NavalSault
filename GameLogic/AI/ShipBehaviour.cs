using System.Collections.Generic;
using System.Linq;
using NavalSault.Utils;

namespace NavalSault.GameLogic.AI;

// How a ship that isn't the player's plans its orders. A ship's behaviour can be swapped at any time,
// e.g. by a scripted event, and takes effect the next time the ship plans.
public abstract class ShipBehaviour
{
    // Called while the ship is the active one during planning. Plans orders through the same TurnManager calls
    // the player's clicks go through, so every rule applies; the turn manager moves on once this returns.
    public abstract void PlanTurn(Ship ship, TurnManager turns);

    protected static IEnumerable<Ship> Opponents(Ship ship, TurnManager turns)
    {
        return turns.Ships.Where(s => s.Side != ship.Side && !s.IsSunk);
    }

    // Tiles from the tile to the nearest part of the other ship's hull.
    protected static int DistanceTo((int Q, int R) from, Ship other)
    {
        return other.Tiles.Min(t => HexGrid.Distance(from, t));
    }

    protected static Ship Nearest(Ship ship, IEnumerable<Ship> candidates)
    {
        return candidates.MinBy(o => DistanceTo(ship.PlannedTile, o));
    }

    // Fires one shell at the nearest target in range, if the turn points allow. Aims at where targets are now,
    // not where their hidden orders will take them, and at the hull tile closest to the ship, where the spread is smallest.
    protected static bool TryShoot(Ship ship, TurnManager turns, IEnumerable<Ship> targets)
    {
        if (ship.Weapon == null)
            return false;

        foreach (var target in targets.OrderBy(t => DistanceTo(ship.PlannedTile, t)))
        {
            foreach (var tile in target.Tiles.OrderBy(t => HexGrid.Distance(ship.PlannedTile, t)))
            {
                if (turns.TryAddShot(tile))
                    return true;
            }
        }

        return false;
    }

    protected static void ShootWhilePossible(Ship ship, TurnManager turns, IEnumerable<Ship> targets)
    {
        var targetList = targets.ToList();
        while (TryShoot(ship, turns, targetList))
        {
        }
    }

    // Shoots at targets in range; failing that, closes in on the nearest one, keeping enough turn points
    // to fire this turn if some tile within reach brings it into range, or sailing as far as it can otherwise.
    protected static void Engage(Ship ship, TurnManager turns, IReadOnlyCollection<Ship> targets)
    {
        if (targets.Count == 0)
            return;

        if (TryShoot(ship, turns, targets))
        {
            ShootWhilePossible(ship, turns, targets);
            return;
        }

        var target = Nearest(ship, targets);
        if (ship.Weapon != null && ship.TurnPoints > ship.Weapon.ShotCost
            && TryMoveIntoRange(ship, turns, target, ship.TurnPoints - ship.Weapon.ShotCost))
        {
            ShootWhilePossible(ship, turns, targets);
            return;
        }

        var goal = target.Tiles.MinBy(t => HexGrid.Distance(ship.PlannedTile, t));
        MoveToward(ship, turns, goal, ship.TurnPoints);
    }

    // Sails, within maxSteps tiles, to the reachable tile that gets closest to the goal, by the shortest path
    // among equally close ones. Stays put if no reachable tile is closer than where the ship already is.
    protected static bool MoveToward(Ship ship, TurnManager turns, (int Q, int R) goal, int maxSteps)
    {
        List<(int Q, int R)> best = null;
        int bestDistance = HexGrid.Distance(ship.PlannedTile, goal);
        foreach (var path in ReachablePaths(ship, turns, maxSteps))
        {
            int distance = HexGrid.Distance(path[^1], goal);
            if (distance < bestDistance || (distance == bestDistance && best != null && path.Count < best.Count))
            {
                best = path;
                bestDistance = distance;
            }
        }

        return best != null && turns.TryAddMove(best[^1]);
    }

    // Sails, within maxSteps tiles, to the reachable tile nearest the ship from which the target is in range.
    protected static bool TryMoveIntoRange(Ship ship, TurnManager turns, Ship target, int maxSteps)
    {
        List<(int Q, int R)> best = null;
        foreach (var path in ReachablePaths(ship, turns, maxSteps))
        {
            int distance = DistanceTo(path[^1], target);
            if (distance < 1 || distance > ship.Weapon.Range)
                continue;

            if (best == null || path.Count < best.Count
                || (path.Count == best.Count && distance < DistanceTo(best[^1], target)))
                best = path;
        }

        return best != null && turns.TryAddMove(best[^1]);
    }

    // Every path the ship could plan right now that ends on another tile at most maxSteps tiles away.
    private static IEnumerable<List<(int Q, int R)>> ReachablePaths(Ship ship, TurnManager turns, int maxSteps)
    {
        foreach (var tile in HexGrid.TilesWithin(ship.PlannedTile, maxSteps))
        {
            if (tile == ship.PlannedTile)
                continue;

            var path = turns.PlanMove(tile);
            if (path != null && path.Count - 1 <= maxSteps)
                yield return path;
        }
    }
}
