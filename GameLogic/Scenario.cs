using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NavalSault.GameLogic.AI;
using NavalSault.GameLogic.Rules;
using NavalSault.GameLogic.Scripting;
using NavalSault.Utils;

namespace NavalSault.GameLogic;

// How a battle starts and plays out: the island on the map, both fleets with their positions and behaviours,
// how the battle is won, and the scripted events along the way.
public class Scenario
{
    private static readonly Color ActiveShipColor = new(255, 225, 70);
    private static readonly Color EnemyShipColor = new(70, 70, 78);
    private static readonly Color IslandColor = new(194, 170, 110);
    private static readonly Color GridColor = new(0, 255, 128);
    private static readonly Color GridHighlightColor = new(150, 255, 190);
    private const float HexRadius = 8f;
    private const float GridTileY = 1.3f;

    public HexGrid Grid { get; }
    public Island Island { get; }
    public IReadOnlyList<Ship> Ships { get; }
    // Checked in order at the start of every turn; the first one met ends the battle.
    public IReadOnlyList<VictoryCondition> VictoryConditions { get; }
    public IReadOnlyList<ScriptedEvent> Events { get; }

    private Scenario(HexGrid grid, Island island, IReadOnlyList<Ship> ships, IReadOnlyList<VictoryCondition> victoryConditions,
        IReadOnlyList<ScriptedEvent> events)
    {
        Grid = grid;
        Island = island;
        Ships = ships;
        VictoryConditions = victoryConditions;
        Events = events;
    }

    // Two player ships south of a small island, one enemy guarding the water behind it.
    // The enemy loses patience on turn 4, or once it sinks a player ship, and reinforcements arrive on turn 5.
    public static Scenario Skirmish(GraphicsDevice graphicsDevice)
    {
        // Fourteen tiles out from the center, room to manoeuvre with long-range guns.
        var grid = CreateGrid(graphicsDevice, gridRadius: 14);

        // Two columns of three tiles, one tile of open water ahead of Ship A.
        var island = new Island(graphicsDevice, grid.HexRadius,
            [(0, -2), (0, -3), (0, -4), (1, -3), (1, -4), (1, -5)],
            IslandColor);
        foreach (var tile in island.Tiles)
            grid.SetBlocked(tile, true);

        // The enemy waits behind the island, bow towards it, just out of Ship A's sight but well within its guns' range.
        var enemyPost = (0, -7);
        var enemy = CreateShip(graphicsDevice, grid, "Enemy", Side.Enemy, HexFootprint.Line1x3, enemyPost, HexDirection.South, EnemyShipColor, initiative: 4);
        enemy.Behaviour = new GuardBehaviour(enemyPost, radius: 5);

        var ships = new[]
        {
            CreateShip(graphicsDevice, grid, "Ship A", Side.Player, HexFootprint.Line1x3, (0, 0), HexDirection.North, new Color(200, 60, 40), initiative: 5),
            CreateShip(graphicsDevice, grid, "Ship B", Side.Player, HexFootprint.Wide2x3, (3, -1), HexDirection.North, new Color(60, 90, 200), initiative: 3),
            enemy
        };

        bool enemyEnraged = false;
        void Enrage(ScriptContext script)
        {
            if (enemyEnraged)
                return;

            enemyEnraged = true;
            script.SetBehaviour("Enemy", new AggressiveBehaviour());
            script.ShowMessage("The enemy captain leaves the island and comes for you!");
        }

        var events = new[]
        {
            ScriptedEvent.OnTurn(1, script => script.ShowMessage("An enemy ship guards the water behind the island.")),
            ScriptedEvent.OnTurn(4, Enrage),
            ScriptedEvent.OnShipSunk(Side.Player, Enrage),
            ScriptedEvent.OnTurn(5, script =>
            {
                var reinforcement = CreateShip(graphicsDevice, grid, "Enemy 2", Side.Enemy, HexFootprint.Line1x3, (-6, -1), HexDirection.South,
                    EnemyShipColor, initiative: 2);
                reinforcement.Behaviour = new AggressiveBehaviour();
                script.Spawn(reinforcement);
                script.ShowMessage("Enemy reinforcements sighted to the west!");
            })
        };

        return new Scenario(grid, island, ships, [new DestroyAllCondition()], events);
    }

    private static HexGrid CreateGrid(GraphicsDevice graphicsDevice, int gridRadius)
    {
        return new HexGrid(graphicsDevice, HexRadius, gridRadius, GridTileY, GridColor, GridHighlightColor);
    }

    private static Ship CreateShip(GraphicsDevice graphicsDevice, HexGrid grid, string name, Side side, HexFootprint footprint,
        (int Q, int R) tile, HexDirection facing, Color color, int initiative)
    {
        return new Ship(graphicsDevice, grid, name, side, footprint, tile, baseColor: color, activeColor: ActiveShipColor,
            facing: facing, initiative: initiative, maxTurnPoints: 5, maxHp: 30, viewRadius: 4,
            weapon: new Weapon("Cannon", damage: 10, range: 10, shotCost: 3, spreadPerTile: 0.2f));
    }
}
