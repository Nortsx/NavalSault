using System.Linq;

namespace NavalSault.GameLogic.Rules;

// The battle ends when one side, or both, has no ships left afloat.
public class DestroyAllCondition : VictoryCondition
{
    public override bool TryGetResult(TurnManager turns, out Side? winner, out string reason)
    {
        bool playerAfloat = turns.Ships.Any(s => s.Side == Side.Player && !s.IsSunk);
        bool enemyAfloat = turns.Ships.Any(s => s.Side == Side.Enemy && !s.IsSunk);

        winner = playerAfloat ? Side.Player : enemyAfloat ? Side.Enemy : null;
        reason = winner switch
        {
            Side.Player => "Every enemy ship sunk",
            Side.Enemy => "Your fleet was sunk",
            _ => "Every ship sank"
        };
        return !playerAfloat || !enemyAfloat;
    }
}
