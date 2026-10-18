namespace NavalSault.GameLogic.Rules;

// The side wins once it has come through the given number of turns.
public class SurviveTurnsCondition : VictoryCondition
{
    private readonly Side _side;
    private readonly int _turns;

    public SurviveTurnsCondition(Side side, int turns)
    {
        _side = side;
        _turns = turns;
    }

    public override bool TryGetResult(TurnManager turns, out Side? winner, out string reason)
    {
        // Checked before the next turn starts, so TurnNumber is the number of turns played so far.
        winner = _side;
        reason = $"Held out for {_turns} turns";
        return turns.TurnNumber >= _turns;
    }
}
