using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace NavalSault.GameLogic.Scripting;

// Listens to the turn manager and fires a scenario's scripted events when their moments come up.
public class ScriptRunner
{
    private readonly List<ScriptedEvent> _events;

    public ScriptContext Context { get; }

    public ScriptRunner(TurnManager turns, IEnumerable<ScriptedEvent> events)
    {
        _events = new List<ScriptedEvent>(events);
        Context = new ScriptContext(turns);

        turns.TurnStarted += turn => Fire(e => e.MatchesTurnStarted(turn));
        turns.ShipSunk += ship => Fire(e => e.MatchesShipSunk(ship));
        turns.OrderResolved += order => Fire(e => e.MatchesOrderResolved(order));
    }

    public void Update(GameTime gameTime)
    {
        Context.Update(gameTime);
    }

    private void Fire(Func<ScriptedEvent, bool> matches)
    {
        // Collect first: an action may end the battle or otherwise run while the list is being walked.
        var fired = _events.FindAll(e => matches(e));
        foreach (var scriptedEvent in fired)
        {
            if (scriptedEvent.Once)
                _events.Remove(scriptedEvent);
            scriptedEvent.Action(Context);
        }
    }
}
