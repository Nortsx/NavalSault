namespace NavalSault.GameLogic.Rules;

// One way a battle can end. The turn manager checks a scenario's conditions in order at the start of every turn;
// the first one that's met ends the battle.
public abstract class VictoryCondition
{
    // True if the battle is over; winner is null if nobody won.
    public abstract bool TryGetResult(TurnManager turns, out Side? winner, out string reason);
}
