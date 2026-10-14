using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NavalSault.GameLogic.Orders;
using NavalSault.Utils;

namespace NavalSault.GameLogic;

public enum TurnPhase
{
    // Ships take orders one at a time, highest Initiative first.
    Planning,
    // Orders are carried out ship by ship in the same order.
    Resolving,
    // At most one ship is left afloat.
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

    public TurnManager(IEnumerable<Ship> ships, HexGrid grid, MovePathfinder pathfinder)
    {
        _ships = ships.OrderByDescending(s => s.Initiative).ToList();
        _grid = grid;
        _pathfinder = pathfinder;
        StartTurn();
    }

    // Path the active ship would sail to reach the tile, starting from where its planned orders leave it.
    // Null if it's not planning time, the tile can't be reached, or the ship lacks the turn points.
    public List<(int Q, int R)> PlanMove((int Q, int R) target)
    {
        var ship = ActiveShip;
        if (ship == null)
            return null;

        var path = _pathfinder.FindPath(ship.PlannedTile, target, ship.PlannedFacing, tile => IsOccupied(tile, ship));
        if (path == null || path.Count < 2 || path.Count - 1 > ship.TurnPoints)
            return null;

        return path;
    }

    public bool TryAddMove((int Q, int R) target)
    {
        var path = PlanMove(target);
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
        if (distance < 1 || distance > ship.Weapon.Range)
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

    // The active ship is done taking orders; hand over to the next one, or resolve after the last.
    public void FinishActiveShip()
    {
        if (Phase != TurnPhase.Planning)
            return;

        _ships[_activeIndex].IsActive = false;
        _activeIndex++;

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

        StartTurn();
    }

    private void StartTurn()
    {
        _ships.RemoveAll(s => s.IsSunk);
        if (_ships.Count < 2)
        {
            Phase = TurnPhase.GameOver;
            foreach (var ship in _ships)
            {
                ship.PlannedOrders.Clear();
                ship.IsActive = false;
            }
            return;
        }

        TurnNumber++;
        Phase = TurnPhase.Planning;
        _activeIndex = 0;

        foreach (var ship in _ships)
        {
            ship.PlannedOrders.Clear();
            ship.TurnPoints = ship.MaxTurnPoints;
            ship.IsActive = false;
        }

        _ships[_activeIndex].IsActive = true;
    }

    // Ships resolve in planning order, so a ship that acts earlier will already sit on its planned
    // end tile, while one that acts later is still on its current tile.
    private bool IsOccupied((int Q, int R) tile, Ship planningShip)
    {
        int planningIndex = _ships.IndexOf(planningShip);
        for (int i = 0; i < _ships.Count; i++)
        {
            if (i == planningIndex)
                continue;

            var other = _ships[i];
            var occupiedTile = i < planningIndex ? other.PlannedTile : other.Tile;
            if (occupiedTile == tile)
                return true;
        }

        return false;
    }
}
