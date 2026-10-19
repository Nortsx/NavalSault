using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NavalSault.GameLogic.Orders;
using NavalSault.GameLogic.Rules;
using NavalSault.Utils;

namespace NavalSault.GameLogic;

public enum TurnPhase
{
    // Ships take orders one at a time, highest Initiative first, whichever side they're on.
    // AI ships plan on the spot when their slot comes up; the turn waits on player ships.
    Planning,
    // Orders are carried out ship by ship in the same order.
    Resolving,
    // A victory condition was met, or a script ended the battle.
    GameOver
}

public class TurnManager
{
    // Sorted by Initiative, highest first. Ships plan and resolve in this order.
    private readonly List<Ship> _ships;
    private readonly HexGrid _grid;
    private readonly MovePathfinder _pathfinder;
    private readonly IReadOnlyList<VictoryCondition> _victoryConditions;
    private readonly Random _random = new();
    // Ships waiting to join the battle at the start of a turn.
    private readonly List<Ship> _pendingShips = new();

    private int _activeIndex;
    private int _resolvingShipIndex;
    private int _resolvingOrderIndex;

    public IReadOnlyList<Ship> Ships => _ships;
    // What each side sees; kept up to date as ships move.
    public FogOfWar Vision { get; } = new();
    public int TurnNumber { get; private set; }
    public TurnPhase Phase { get; private set; }
    public Ship ActiveShip => Phase == TurnPhase.Planning ? _ships[_activeIndex] : null;
    // The order being carried out right now while resolving, or null.
    public Order ResolvingOrder { get; private set; }
    // The side that won once the game is over; null while playing, or if nobody won.
    public Side? Winner { get; private set; }
    // Why the battle ended, for the HUD; null while playing.
    public string GameOverReason { get; private set; }

    // Raised once a new turn has begun, before any ship plans, so changes made by handlers apply to this turn.
    public event Action<int> TurnStarted;
    // Raised after each order has been carried out.
    public event Action<Order> OrderResolved;
    // Raised when a resolved shot sinks a ship.
    public event Action<Ship> ShipSunk;

    public TurnManager(IEnumerable<Ship> ships, HexGrid grid, MovePathfinder pathfinder, IReadOnlyList<VictoryCondition> victoryConditions)
    {
        _ships = ships.OrderByDescending(s => s.Initiative).ToList();
        _grid = grid;
        _pathfinder = pathfinder;
        _victoryConditions = victoryConditions;
    }

    // Starts the first turn. Separate from the constructor so listeners can subscribe to TurnStarted first.
    public void Start()
    {
        StartTurn();
    }

    // The ship with that name still afloat, or null.
    public Ship FindShip(string name)
    {
        return _ships.Find(s => s.Name == name && !s.IsSunk);
    }

    // The ship joins the battle at the start of the next turn, or of the first turn its tiles are free.
    public void AddShip(Ship ship)
    {
        _pendingShips.Add(ship);
    }

    // Ends the battle at once, even in the middle of a turn.
    public void EndBattle(Side? winner, string reason)
    {
        if (Phase == TurnPhase.GameOver)
            return;

        if (Phase == TurnPhase.Planning)
            _ships[_activeIndex].IsActive = false;

        Phase = TurnPhase.GameOver;
        Winner = winner;
        GameOverReason = reason;
        ResolvingOrder = null;
    }

    // Path of anchor tiles the active ship would sail to bring its anchor onto the tile, starting from where
    // its planned orders leave it, optionally arriving with the given facing. Null if it's not planning time,
    // the tile can't be reached, or the ship lacks the turn points.
    public List<(int Q, int R)> PlanMove((int Q, int R) target, HexDirection? endFacing = null)
    {
        var ship = ActiveShip;
        if (ship == null)
            return null;

        var occupied = OccupiedTiles(ship);
        var path = _pathfinder.FindPath(ship.PlannedTile, target, ship.PlannedFacing, ship.Footprint, occupied.Contains, endFacing);
        if (path == null || path.Count < 2 || path.Count - 1 > ship.TurnPoints)
            return null;

        return path;
    }

    public bool TryAddMove((int Q, int R) target, HexDirection? endFacing = null)
    {
        var path = PlanMove(target, endFacing);
        return path != null && TryAddOrder(new MoveOrder(ActiveShip, path, _grid));
    }

    // A shot the active ship would fire at the tile from where its planned orders leave it.
    // Null if it's not planning time, the ship lacks a weapon or the turn points, or the tile is off the map or out of range.
    public ShootOrder PlanShot((int Q, int R) target)
    {
        var ship = ActiveShip;
        if (ship?.Weapon == null || ship.Weapon.ShotCost > ship.TurnPoints || !_grid.IsOnMap(target))
            return null;

        int distance = HexGrid.Distance(ship.PlannedTile, target);
        if (distance < 1 || distance > ship.Weapon.Range || ship.PlannedTiles.Contains(target))
            return null;

        return new ShootOrder(ship, ship.Weapon, ship.PlannedTile, ship.PlannedFacing, target, _ships, _random);
    }

    public bool TryAddShot((int Q, int R) target)
    {
        var shot = PlanShot(target);
        return shot != null && TryAddOrder(shot);
    }

    public bool TryAddOrder(Order order)
    {
        var ship = ActiveShip;
        if (ship == null || order.Ship != ship || order.TurnPointCost > ship.TurnPoints)
            return false;

        ship.PlannedOrders.Add(order);
        ship.TurnPoints -= order.TurnPointCost;
        return true;
    }

    public void UndoLastOrder()
    {
        var ship = ActiveShip;
        if (ship == null || ship.PlannedOrders.Count == 0)
            return;

        var order = ship.PlannedOrders[^1];
        ship.PlannedOrders.RemoveAt(ship.PlannedOrders.Count - 1);
        ship.TurnPoints += order.TurnPointCost;
    }

    // The active player ship is done taking orders; hand over to the next ship, or resolve after the last.
    public void FinishActiveShip()
    {
        if (Phase != TurnPhase.Planning || !ActiveShip.IsPlayerControlled)
            return;

        _ships[_activeIndex].IsActive = false;
        AdvancePlanning(_activeIndex + 1);
    }

    public void Update(GameTime gameTime)
    {
        foreach (var ship in _ships)
            ship.Update(gameTime);

        if (Phase == TurnPhase.Resolving)
            Resolve(gameTime);

        Vision.Update(_ships);
    }

    // Hands planning to the ships from the given index on, in turn order. AI ships plan their orders on the spot;
    // stops at the first player ship, or starts resolving once every ship has planned.
    private void AdvancePlanning(int from)
    {
        for (_activeIndex = from; _activeIndex < _ships.Count; _activeIndex++)
        {
            var ship = _ships[_activeIndex];
            if (ship.IsPlayerControlled)
            {
                ship.IsActive = true;
                return;
            }

            ship.Behaviour.PlanTurn(ship, this);
        }

        Phase = TurnPhase.Resolving;
        _resolvingShipIndex = 0;
        _resolvingOrderIndex = 0;
        ResolvingOrder = null;
    }

    // Steps through every ship's orders in turn order, one at a time; starts the next turn when all are done.
    // A ship sunk earlier in the turn drops the rest of its orders.
    private void Resolve(GameTime gameTime)
    {
        while (_resolvingShipIndex < _ships.Count)
        {
            var ship = _ships[_resolvingShipIndex];
            if (ship.IsSunk || _resolvingOrderIndex >= ship.PlannedOrders.Count)
            {
                _resolvingShipIndex++;
                _resolvingOrderIndex = 0;
                continue;
            }

            if (ResolvingOrder == null)
            {
                ResolvingOrder = ship.PlannedOrders[_resolvingOrderIndex];
                ResolvingOrder.Begin();
            }

            if (!ResolvingOrder.Update(gameTime))
                return;

            var order = ResolvingOrder;
            _resolvingOrderIndex++;
            ResolvingOrder = null;

            if (order is ShootOrder { HitShip.IsSunk: true } shot)
                ShipSunk?.Invoke(shot.HitShip);
            OrderResolved?.Invoke(order);

            // A handler may have ended the battle.
            if (Phase != TurnPhase.Resolving)
                return;
        }

        // Let ships sunk this turn finish going under before they're cleared away.
        if (_ships.Any(s => s.IsSinking))
            return;

        StartTurn();
    }

    private void StartTurn()
    {
        _ships.RemoveAll(s => s.IsSunk);
        AddPendingShips();
        foreach (var ship in _ships)
        {
            ship.PlannedOrders.Clear();
            ship.TurnPoints = ship.MaxTurnPoints;
            ship.IsActive = false;
        }

        foreach (var condition in _victoryConditions)
        {
            if (condition.TryGetResult(this, out var winner, out var reason))
            {
                EndBattle(winner, reason);
                return;
            }
        }

        // Without a condition that covers it, a battle with nobody left would otherwise go on forever.
        if (_ships.Count == 0)
        {
            EndBattle(null, "Every ship sank");
            return;
        }

        TurnNumber++;
        Phase = TurnPhase.Planning;
        TurnStarted?.Invoke(TurnNumber);
        if (Phase != TurnPhase.Planning)
            return;

        // Ships spawned by TurnStarted handlers act this turn already.
        AddPendingShips();
        // AI ships plan on what their side sees now, ships that just joined included.
        Vision.Update(_ships);
        AdvancePlanning(0);
    }

    // Puts waiting ships into turn order, after any ships with the same Initiative.
    // A ship whose tiles are blocked or taken keeps waiting.
    private void AddPendingShips()
    {
        for (int i = 0; i < _pendingShips.Count; i++)
        {
            var ship = _pendingShips[i];
            if (!ship.Tiles.All(t => _grid.IsPassable(t) && !_ships.Any(s => s.Occupies(t))))
                continue;

            int index = _ships.FindIndex(s => s.Initiative < ship.Initiative);
            _ships.Insert(index < 0 ? _ships.Count : index, ship);
            _pendingShips.RemoveAt(i--);
        }
    }

    // Ships resolve in planning order, so a ship that acts earlier will already sit on its planned
    // end tiles, while one that acts later is still on its current tiles.
    private HashSet<(int Q, int R)> OccupiedTiles(Ship planningShip)
    {
        var occupied = new HashSet<(int Q, int R)>();
        int planningIndex = _ships.IndexOf(planningShip);
        for (int i = 0; i < _ships.Count; i++)
        {
            if (i == planningIndex)
                continue;

            var other = _ships[i];
            occupied.UnionWith(i < planningIndex ? other.PlannedTiles : other.Tiles);
        }

        return occupied;
    }
}
