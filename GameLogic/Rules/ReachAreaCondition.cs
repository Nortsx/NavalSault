using System.Collections.Generic;
using System.Linq;

namespace NavalSault.GameLogic.Rules;

// The side wins once the named ship ends a turn with any part of its hull on the area.
public class ReachAreaCondition : VictoryCondition
{
    private readonly string _shipName;
    private readonly HashSet<(int Q, int R)> _area;
    private readonly string _areaName;
    private readonly Side _winner;

    public ReachAreaCondition(string shipName, IEnumerable<(int Q, int R)> area, string areaName, Side winner)
    {
        _shipName = shipName;
        _area = area.ToHashSet();
        _areaName = areaName;
        _winner = winner;
    }

    public override bool TryGetResult(TurnManager turns, out Side? winner, out string reason)
    {
        winner = _winner;
        reason = $"{_shipName} reached {_areaName}";
        var ship = turns.FindShip(_shipName);
        return ship != null && ship.Tiles.Any(_area.Contains);
    }
}
