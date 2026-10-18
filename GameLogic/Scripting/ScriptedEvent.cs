using System;
using System.Collections.Generic;
using System.Linq;
using NavalSault.GameLogic.Orders;

namespace NavalSault.GameLogic.Scripting;

// Something a scenario does when a moment in the battle comes up: a trigger and the action it sets off.
// Built with the static helpers, which listen for one kind of moment each.
public class ScriptedEvent
{
    private Func<int, bool> _onTurnStarted;
    private Func<Ship, bool> _onShipSunk;
    private Func<Order, bool> _onOrderResolved;

    public Action<ScriptContext> Action { get; }
    // A one-off event is dropped after it fires; otherwise it fires every time its trigger matches.
    public bool Once { get; }

    private ScriptedEvent(Action<ScriptContext> action, bool once)
    {
        Action = action;
        Once = once;
    }

    // At the start of the given turn, before any ship plans.
    public static ScriptedEvent OnTurn(int turn, Action<ScriptContext> action)
    {
        return new ScriptedEvent(action, once: true) { _onTurnStarted = t => t == turn };
    }

    // When the named ship is sunk.
    public static ScriptedEvent OnShipSunk(string shipName, Action<ScriptContext> action)
    {
        return new ScriptedEvent(action, once: true) { _onShipSunk = s => s.Name == shipName };
    }

    // When any ship of the side is sunk.
    public static ScriptedEvent OnShipSunk(Side side, Action<ScriptContext> action, bool once = true)
    {
        return new ScriptedEvent(action, once) { _onShipSunk = s => s.Side == side };
    }

    // When the named ship finishes a move with any part of its hull on the area.
    public static ScriptedEvent OnShipEnters(string shipName, IEnumerable<(int Q, int R)> area, Action<ScriptContext> action)
    {
        var tiles = area.ToHashSet();
        return new ScriptedEvent(action, once: true)
        {
            _onOrderResolved = o => o is MoveOrder && o.Ship.Name == shipName && o.Ship.Tiles.Any(tiles.Contains)
        };
    }

    public bool MatchesTurnStarted(int turn) => _onTurnStarted?.Invoke(turn) ?? false;
    public bool MatchesShipSunk(Ship ship) => _onShipSunk?.Invoke(ship) ?? false;
    public bool MatchesOrderResolved(Order order) => _onOrderResolved?.Invoke(order) ?? false;
}
