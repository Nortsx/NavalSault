using System.Linq;

namespace NavalSault.GameLogic.AI;

// Hunts the nearest opposing ship anywhere on the map and fires as soon as it's in range.
public class AggressiveBehaviour : ShipBehaviour
{
    public override void PlanTurn(Ship ship, TurnManager turns)
    {
        Engage(ship, turns, Opponents(ship, turns).ToList());
    }
}
