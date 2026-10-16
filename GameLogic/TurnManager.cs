using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NavalSault.GameLogic.Orders;
using NavalSault.Utils;

namespace NavalSault.GameLogic;

public enum TurnPhase
{
    // Player ships take orders one at a time, highest Initiative first.
    Planning,
    // Orders are carried out ship by ship in the same order.
    Resolving,
    // One side, or both, has no ships left afloat.
    GameOver
}

public class TurnManager
{
    // Sorted by Initiative, highest first. Ships plan and resolve in this order.
    private readonly List<Ship> _ships;
    private readonly HexGrid _grid;
    private readonly MovePathfinder _pathfinder;
    private readonly Random _random = new();

    private int _activeIndex;
    private int _resolvingShipIndex;
    private int _resolvingOrderIndex;

    public IReadOnlyList<Ship> Ships => _ships;
    public int TurnNumber { get; private set; }
    public TurnPhase Phase { get; private set; }
    public Ship ActiveShip => Phase == TurnPhase.Planning ? _ships[_activeIndex] : null;
    // The order being carried out right now while resolving, or null.
    public Order ResolvingOrder { get; private set; }
    // The side still afloat once the game is over; null while playing, or if both sides sank.
    public Side? Winner { get; private set; }

    public TurnManager(IEnumerable<Ship> ships, HexGrid grid, MovePathfinder pathfinder)
    {
        _ships = ships.OrderByDescending(s => s.Initiative).ToList();
        _grid = grid;
        _pathfinder = pathfinder;
        StartTurn();
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

    // The active ship is done taking orders; hand over to the next player ship, or resolve after the last.
    public void FinishActiveShip()
    {
        if (Phase != TurnPhase.Planning)
            return;

        _ships[_activeIndex].IsActive = false;
        _activeIndex = NextPlayerShipIndex(_activeIndex + 1);

        if (_activeIndex < _ships.Count)
        {
            _ships[_activeIndex].IsActive = true;
            return;
        }

        Phase = TurnPhase.Resolving;
        _resolvingShipIndex = 0;
        _resolvingOrderIndex = 0;
        ResolvingOrder = null;
    }

    public void Update(GameTime gameTime)
    {
        foreach (var ship in _ships)
            ship.Update(gameTime);

        if (Phase == TurnPhase.Resolving)
            Resolve(gameTime);
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

            _resolvingOrderIndex++;
            ResolvingOrder = null;
        }

        // Let ships sunk this turn finish going under before they're cleared away.
        if (_ships.Any(s => s.IsSinking))
            return;

        StartTurn();
    }

    private void StartTurn()
    {
        _ships.RemoveAll(s => s.IsSunk);
        foreach (var ship in _ships)
        {
            ship.PlannedOrders.Clear();
            ship.TurnPoints = ship.MaxTurnPoints;
            ship.IsActive = false;
        }

        bool playerAfloat = _ships.Any(s => s.Side == Side.Player);
        bool enemyAfloat = _ships.Any(s => s.Side == Side.Enemy);
        if (!playerAfloat || !enemyAfloat)
        {
            Phase = TurnPhase.GameOver;
            Winner = playerAfloat ? Side.Player : enemyAfloat ? Side.Enemy : null;
            return;
        }

        TurnNumber++;
        Phase = TurnPhase.Planning;
        _activeIndex = NextPlayerShipIndex(0);
        _ships[_activeIndex].IsActive = true;
    }

    // Index of the first ship from the given one on that takes orders from the player; Count if there are none.
    // Enemy ships have no orders yet, so they just sit where they are.
    private int NextPlayerShipIndex(int from)
    {
        int index = from;
        while (index < _ships.Count && !_ships[index].IsPlayerControlled)
            index++;
        return index;
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
