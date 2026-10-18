using Microsoft.Xna.Framework;
using NavalSault.GameLogic.AI;

namespace NavalSault.GameLogic.Scripting;

// What a scripted event's action can do to the battle.
public class ScriptContext
{
    private string _messageText;
    private float _messageTimeLeft;

    public TurnManager Turns { get; }
    // Text a script wants on screen, or null once its time is up.
    public string Message => _messageTimeLeft > 0f ? _messageText : null;

    public ScriptContext(TurnManager turns)
    {
        Turns = turns;
    }

    // The ship with that name still afloat, or null.
    public Ship FindShip(string name)
    {
        return Turns.FindShip(name);
    }

    // Brings a new ship into the battle at the start of the next turn (this turn, if called when a turn starts).
    public void Spawn(Ship ship)
    {
        Turns.AddShip(ship);
    }

    // Changes how an AI ship plans from its next planning on. Null hands it to the player.
    public void SetBehaviour(string shipName, ShipBehaviour behaviour)
    {
        if (FindShip(shipName) is { } ship)
            ship.Behaviour = behaviour;
    }

    public void ShowMessage(string text, float seconds = 4f)
    {
        _messageText = text;
        _messageTimeLeft = seconds;
    }

    public void EndBattle(Side? winner, string reason)
    {
        Turns.EndBattle(winner, reason);
    }

    public void Update(GameTime gameTime)
    {
        _messageTimeLeft -= (float)gameTime.ElapsedGameTime.TotalSeconds;
    }
}
