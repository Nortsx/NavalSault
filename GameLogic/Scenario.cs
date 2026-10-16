using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NavalSault.Utils;

namespace NavalSault.GameLogic;

// How a battle starts: the island on the map and both fleets with their positions.
public class Scenario
{
    private static readonly Color ActiveShipColor = new(255, 225, 70);
    private static readonly Color EnemyShipColor = new(70, 70, 78);
    private static readonly Color IslandColor = new(194, 170, 110);

    public Island Island { get; }
    public IReadOnlyList<Ship> Ships { get; }

    private Scenario(Island island, IReadOnlyList<Ship> ships)
    {
        Island = island;
        Ships = ships;
    }

    // Two player ships south of a small island, one enemy waiting behind it. Marks the island blocked on the grid.
    public static Scenario Skirmish(GraphicsDevice graphicsDevice, HexGrid grid)
    {
        // Two columns of three tiles, one tile of open water ahead of Ship A.
        var island = new Island(graphicsDevice, grid.HexRadius,
            [(0, -2), (0, -3), (0, -4), (1, -3), (1, -4), (1, -5)],
            IslandColor);
        foreach (var tile in island.Tiles)
            grid.SetBlocked(tile, true);

        // The enemy waits behind the island, bow towards it, just out of reach of Ship A's guns.
        var ships = new[]
        {
            CreateShip(graphicsDevice, grid, "Ship A", Side.Player, HexFootprint.Line1x3, (0, 0), HexDirection.North, new Color(200, 60, 40), initiative: 5),
            CreateShip(graphicsDevice, grid, "Ship B", Side.Player, HexFootprint.Wide2x3, (3, -1), HexDirection.North, new Color(60, 90, 200), initiative: 3),
            CreateShip(graphicsDevice, grid, "Enemy", Side.Enemy, HexFootprint.Line1x3, (0, -7), HexDirection.South, EnemyShipColor, initiative: 4)
        };

        return new Scenario(island, ships);
    }

    private static Ship CreateShip(GraphicsDevice graphicsDevice, HexGrid grid, string name, Side side, HexFootprint footprint,
        (int Q, int R) tile, HexDirection facing, Color color, int initiative)
    {
        return new Ship(graphicsDevice, grid, name, side, footprint, tile, baseColor: color, activeColor: ActiveShipColor,
            facing: facing, initiative: initiative, maxTurnPoints: 5, maxHp: 30,
            weapon: new Weapon("Cannon", damage: 10, range: 6, shotCost: 3, spreadStep: 3));
    }
}
