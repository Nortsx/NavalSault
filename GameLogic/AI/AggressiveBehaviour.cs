using System.Linq;
using NavalSault.Utils;

namespace NavalSault.GameLogic.AI;

// Hunts the nearest opposing ship its side can see and fires as soon as it's in range. With nothing in sight
// it sails to where an opponent was last seen, or, with no leads at all, towards SearchTile.
public class AggressiveBehaviour : ShipBehaviour
{
    public (int Q, int R) SearchTile { get; }

    public AggressiveBehaviour((int Q, int R)? searchTile = null)
    {
        SearchTile = searchTile ?? (0, 0);
    }

    public override void PlanTurn(Ship ship, TurnManager turns)
    {
        var targets = Opponents(ship, turns).ToList();
        if (targets.Count > 0)
        {
            Engage(ship, turns, targets);
            return;
        }

        var leads = turns.Vision.LastSeen(ship.Side).Values.ToList();
        var goal = leads.Count > 0 ? leads.MinBy(t => HexGrid.Distance(ship.PlannedTile, t)) : SearchTile;
        MoveToward(ship, turns, goal, ship.TurnPoints);
    }
}
