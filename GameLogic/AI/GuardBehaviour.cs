using System.Linq;

namespace NavalSault.GameLogic.AI;

// Keeps watch over an area: engages opposing ships that come within Radius tiles of Post,
// otherwise sails back to it and fires at anything in range from there.
public class GuardBehaviour : ShipBehaviour
{
    public (int Q, int R) Post { get; }
    public int Radius { get; }

    public GuardBehaviour((int Q, int R) post, int radius)
    {
        Post = post;
        Radius = radius;
    }

    public override void PlanTurn(Ship ship, TurnManager turns)
    {
        var intruders = Opponents(ship, turns).Where(o => DistanceTo(Post, o) <= Radius).ToList();
        if (intruders.Count > 0)
        {
            Engage(ship, turns, intruders);
            return;
        }

        if (ship.PlannedTile != Post)
            MoveToward(ship, turns, Post, ship.TurnPoints);
        ShootWhilePossible(ship, turns, Opponents(ship, turns));
    }
}
