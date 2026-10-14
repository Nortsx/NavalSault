using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NavalSault.Utils;

namespace NavalSault.GameLogic.Orders;

// Fire one shell at a tile. The shell lands on a random tile of the spread area around the target,
// which grows the further away the target is; a ship on that tile takes the weapon's damage.
public class ShootOrder : Order
{
    private const float FlightTime = 0.9f;
    // How long the impact stays on screen before the next order starts.
    private const float ImpactTime = 0.6f;

    private readonly IReadOnlyList<Ship> _ships;
    private readonly Random _random;
    private readonly HexDirection _facing;
    private float _elapsed;

    public Weapon Weapon { get; }
    // Tile the ship fires from: wherever its earlier orders leave it.
    public (int Q, int R) FiringTile { get; }
    public (int Q, int R) TargetTile { get; }
    // Every tile the shell can land on, target included. May reach past the map edge.
    public IReadOnlyList<(int Q, int R)> SpreadTiles { get; }

    // Rolled when the order starts resolving.
    public (int Q, int R) LandingTile { get; private set; }
    // Set on impact if the shell landed on a ship.
    public Ship HitShip { get; private set; }
    public bool HasLanded => _elapsed >= FlightTime;
    // 0 at firing, 1 at impact.
    public float FlightProgress => Math.Min(_elapsed / FlightTime, 1f);

    public ShootOrder(Ship ship, Weapon weapon, (int Q, int R) firingTile, HexDirection facing, (int Q, int R) targetTile,
        IReadOnlyList<Ship> ships, Random random) : base(ship)
    {
        Weapon = weapon;
        FiringTile = firingTile;
        TargetTile = targetTile;
        _facing = facing;
        _ships = ships;
        _random = random;

        int spread = weapon.SpreadRadius(HexGrid.Distance(firingTile, targetTile));
        SpreadTiles = HexGrid.TilesWithin(targetTile, spread).ToList();
    }

    public override int TurnPointCost => Weapon.ShotCost;
    public override (int Q, int R) EndTile => FiringTile;
    public override HexDirection EndFacing => _facing;

    public override void Begin()
    {
        _elapsed = 0f;
        HitShip = null;
        LandingTile = SpreadTiles[_random.Next(SpreadTiles.Count)];
    }

    public override bool Update(GameTime gameTime)
    {
        bool wasInFlight = !HasLanded;
        _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (wasInFlight && HasLanded)
            Impact();

        return _elapsed >= FlightTime + ImpactTime;
    }

    private void Impact()
    {
        foreach (var ship in _ships)
        {
            if (!ship.Occupies(LandingTile))
                continue;

            HitShip = ship;
            ship.TakeDamage(Weapon.Damage);
            return;
        }
    }
}
