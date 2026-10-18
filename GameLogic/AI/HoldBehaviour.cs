namespace NavalSault.GameLogic.AI;

// Never moves; fires at whatever comes into range.
public class HoldBehaviour : ShipBehaviour
{
    public override void PlanTurn(Ship ship, TurnManager turns)
    {
        ShootWhilePossible(ship, turns, Opponents(ship, turns));
    }
}
