using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using NavalSault.GameLogic.AI;
using NavalSault.GameLogic.Rules;
using NavalSault.GameLogic.Scripting;
using NavalSault.Rendering;
using NavalSault.Utils;

namespace NavalSault.GameLogic;

// A scenario as the main menu lists it. Create builds a fresh copy of the battle every time it's picked; anything it
// loads through the content manager belongs to that battle and is unloaded when the battle ends.
public record ScenarioEntry(string Name, string Description, Func<GraphicsDevice, ContentManager, Scenario> Create);

// How a battle starts and plays out: the island on the map, both fleets with their positions and behaviours,
// how the battle is won, and the scripted events along the way.
public class Scenario : IDisposable
{
    private static readonly Color ActiveShipColor = new(255, 225, 70);
    private static readonly Color EnemyShipColor = new(70, 70, 78);
    private static readonly Color IslandColor = new(194, 170, 110);
    private static readonly Color GridColor = new(0, 255, 128);
    private static readonly Color GridHighlightColor = new(150, 255, 190);
    private const float HexRadius = 8f;
    private const float GridTileY = 1.3f;
    // The hull takes the ship's colour; the superstructure and guns keep their steel.
    private static readonly ShipModel GunboatModel = new("Models/Gunboat", TintedMeshes: new HashSet<string> { "Hull" });

    public HexGrid Grid { get; }
    public Island Island { get; }
    public IReadOnlyList<Ship> Ships { get; }
    // Checked in order at the start of every turn; the first one met ends the battle.
    public IReadOnlyList<VictoryCondition> VictoryConditions { get; }
    public IReadOnlyList<ScriptedEvent> Events { get; }

    // Every scenario the main menu offers, in the order it lists them.
    public static IReadOnlyList<ScenarioEntry> Catalog { get; } =
    [
        new("Skirmish", "Two ships against an enemy guarding the water behind an island. Reinforcements are on their way.", Skirmish),
        new("Last Stand", "One heavy ship caught between two hunters. Sink them both or stay afloat for 8 turns.", LastStand)
    ];

    private Scenario(HexGrid grid, Island island, IReadOnlyList<Ship> ships, IReadOnlyList<VictoryCondition> victoryConditions,
        IReadOnlyList<ScriptedEvent> events)
    {
        Grid = grid;
        Island = island;
        Ships = ships;
        VictoryConditions = victoryConditions;
        Events = events;
    }

    // The map is the scenario's; the ships are handed over to the turn manager, which disposes them.
    public void Dispose()
    {
        Grid.Dispose();
        Island.Dispose();
    }

    // Two player ships south of a small island, one enemy guarding the water behind it.
    // The enemy loses patience on turn 4, or once it sinks a player ship, and reinforcements arrive on turn 5.
    public static Scenario Skirmish(GraphicsDevice graphicsDevice, ContentManager content)
    {
        // Fourteen tiles out from the center, room to manoeuvre with long-range guns.
        var grid = CreateGrid(graphicsDevice, gridRadius: 20);

        // Two columns of three tiles, one tile of open water ahead of Ship A.
        var island = new Island(graphicsDevice, grid.HexRadius,
            [(0, -2), (0, -3), (0, -4), (1, -3), (1, -4), (1, -5)],
            IslandColor);
        foreach (var tile in island.Tiles)
            grid.SetBlocked(tile, true);

        // The enemy waits behind the island, bow towards it, just out of Ship A's sight but well within its guns' range.
        var enemyPost = (0, -7);
        var enemy = CreateShip(graphicsDevice, content, grid, "Enemy", Side.Enemy, HexFootprint.Line1x3, enemyPost, HexDirection.South, EnemyShipColor, initiative: 4);
        enemy.Behaviour = new GuardBehaviour(enemyPost, radius: 5);

        var ships = new[]
        {
            CreateShip(graphicsDevice, content, grid, "Ship A", Side.Player, HexFootprint.Line1x3, (0, 0), HexDirection.North, new Color(200, 60, 40), initiative: 5),
            CreateShip(graphicsDevice, content, grid, "Ship B", Side.Player, HexFootprint.Wide2x3, (3, -1), HexDirection.North, new Color(60, 90, 200), initiative: 3),
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
                var reinforcement = CreateShip(graphicsDevice, content, grid, "Enemy 2", Side.Enemy, HexFootprint.Line1x3, (-6, -1), HexDirection.South,
                    EnemyShipColor, initiative: 2);
                reinforcement.Behaviour = new AggressiveBehaviour();
                script.Spawn(reinforcement);
                script.ShowMessage("Enemy reinforcements sighted to the west!");
            })
        };

        return new Scenario(grid, island, ships, [new DestroyAllCondition()], events);
    }

    // A lone 2x3 ship in the middle of the map, two enemies closing in from north and south.
    // The player wins by sinking both, or by staying afloat through turn 8.
    public static Scenario LastStand(GraphicsDevice graphicsDevice, ContentManager content)
    {
        var grid = CreateGrid(graphicsDevice, gridRadius: 12);

        // A small rock to the east to hide behind.
        var island = new Island(graphicsDevice, grid.HexRadius, [(4, -1), (4, 0), (5, -1)], IslandColor);
        foreach (var tile in island.Tiles)
            grid.SetBlocked(tile, true);

        var north = CreateShip(graphicsDevice, content, grid, "Hunter N", Side.Enemy, HexFootprint.Line1x3, (2, -10), HexDirection.South, EnemyShipColor, initiative: 4);
        north.Behaviour = new AggressiveBehaviour();
        var south = CreateShip(graphicsDevice, content, grid, "Hunter S", Side.Enemy, HexFootprint.Line1x3, (-2, 10), HexDirection.North, EnemyShipColor, initiative: 2);
        south.Behaviour = new AggressiveBehaviour();

        var ships = new[]
        {
            CreateShip(graphicsDevice, content, grid, "Bastion", Side.Player, HexFootprint.Wide2x3, (0, 0), HexDirection.North, new Color(60, 90, 200), initiative: 3),
            north,
            south
        };

        var events = new[]
        {
            ScriptedEvent.OnTurn(1, script => script.ShowMessage("Two hunters close in from north and south. Hold out for 8 turns!")),
            ScriptedEvent.OnTurn(8, script => script.ShowMessage("Last turn: help is almost here!"))
        };

        return new Scenario(grid, island, ships, [new DestroyAllCondition(), new SurviveTurnsCondition(Side.Player, 8)], events);
    }

    private static HexGrid CreateGrid(GraphicsDevice graphicsDevice, int gridRadius)
    {
        return new HexGrid(graphicsDevice, HexRadius, gridRadius, GridTileY, GridColor, GridHighlightColor);
    }

    // 1x3 ships are gunboats; 2x3 ships have no model yet and are drawn as boxes.
    private static ShipModel ModelFor(HexFootprint footprint)
    {
        return footprint == HexFootprint.Line1x3 ? GunboatModel : null;
    }

    private static Ship CreateShip(GraphicsDevice graphicsDevice, ContentManager content, HexGrid grid, string name, Side side,
        HexFootprint footprint, (int Q, int R) tile, HexDirection facing, Color color, int initiative)
    {
        var hull = ShipHull.Create(graphicsDevice, content, grid, footprint, ModelFor(footprint));
        return new Ship(hull, grid, name, side, footprint, tile, baseColor: color, activeColor: ActiveShipColor,
            facing: facing, initiative: initiative, maxTurnPoints: 5, maxHp: 30, viewRadius: 4,
            weapon: new Weapon("Cannon", damage: 10, range: 10, shotCost: 3, spreadPerTile: 0.2f));
    }
}
