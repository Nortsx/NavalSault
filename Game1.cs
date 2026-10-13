using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NavalSault.GameLogic;
using NavalSault.GameLogic.Orders;
using NavalSault.Utils;

namespace NavalSault;

public class Game1 : Game
{
    private static readonly Color SkyColor = new(140, 185, 225);
    private static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(0.4f, -1f, 0.3f));
    private static readonly Color ActiveShipColor = new(255, 225, 70);
    private static readonly Color PreviewPathColor = new(255, 225, 70);
    private static readonly Color PlannedPathColor = new(255, 255, 255);

    private GraphicsDeviceManager _graphics;
    private BasicEffect _effect;
    private Camera _camera;
    private WaterRenderer _water;
    private HexGrid _hexGrid;
    private Island _island;
    private TurnManager _turns;
    private MouseState _previousMouseState;
    private KeyboardState _previousKeyboardState;
    private (int Q, int R)? _hoveredTile;
    private List<(int Q, int R)> _previewPath;

    private SpriteBatch _spriteBatch;
    private SpriteFont _font;
    private int _frameCount;
    private double _fpsTimer;
    private double _fps;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
        _graphics.GraphicsProfile = GraphicsProfile.Reach;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _camera = new Camera(GraphicsDevice, Vector3.Zero);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _effect = new BasicEffect(GraphicsDevice)
        {
            LightingEnabled = true,
            AmbientLightColor = new Vector3(0.35f, 0.35f, 0.4f),
            SpecularColor = Vector3.Zero,
            TextureEnabled = false,
            VertexColorEnabled = false
        };
        _effect.DirectionalLight0.Enabled = true;
        _effect.DirectionalLight0.Direction = LightDirection;
        _effect.DirectionalLight0.DiffuseColor = new Vector3(1f, 1f, 0.95f);
        _effect.DirectionalLight0.SpecularColor = Vector3.Zero;
        _effect.DirectionalLight1.Enabled = false;
        _effect.DirectionalLight2.Enabled = false;

        _water = new WaterRenderer(GraphicsDevice, Content.Load<Effect>("Water"), SkyColor, LightDirection);
        _hexGrid = new HexGrid(GraphicsDevice, hexRadius: 8f, gridRadius: 8, tileY: 1.3f, color: new Color(0, 255, 128), highlightColor: new Color(150, 255, 190));

        // Test island: two columns of three tiles, one tile of open water ahead of ship A.
        _island = new Island(GraphicsDevice, _hexGrid.HexRadius,
            [(0, -2), (0, -3), (0, -4), (1, -3), (1, -4), (1, -5)],
            new Color(194, 170, 110));
        foreach (var tile in _island.Tiles)
            _hexGrid.SetBlocked(tile, true);

        var ships = new[]
        {
            CreateShip("Ship A", (0, 0), new Color(200, 60, 40), initiative: 5),
            CreateShip("Ship B", (3, -1), new Color(60, 90, 200), initiative: 3)
        };
        _turns = new TurnManager(ships, _hexGrid, new MovePathfinder(_hexGrid));

        _previousMouseState = Mouse.GetState();
        _previousKeyboardState = Keyboard.GetState();

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("DefaultFont");
    }

    private Ship CreateShip(string name, (int Q, int R) tile, Color color, int initiative)
    {
        _hexGrid.TryGetTilePosition(tile.Q, tile.R, out var position);
        return new Ship(GraphicsDevice, name, tile, position, size: 6f, baseColor: color, activeColor: ActiveShipColor,
            facing: HexDirection.North, initiative: initiative, maxTurnPoints: 5, weapon: new Weapon("Cannon", damage: 10, range: 3));
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
            Exit();

        _camera.Update(gameTime, keyboard);

        if (WasPressed(keyboard, Keys.Enter))
            _turns.FinishActiveShip();
        if (WasPressed(keyboard, Keys.Back))
            _turns.UndoLastOrder();
        _previousKeyboardState = keyboard;

        var mouse = Mouse.GetState();
        var mousePosition = new Vector2(mouse.X, mouse.Y);

        UpdateHover(mousePosition);

        if (mouse.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released && _hoveredTile.HasValue)
            _turns.TryAddMove(_hoveredTile.Value);
        _previousMouseState = mouse;

        _turns.Update(gameTime);

        base.Update(gameTime);
    }

    private bool WasPressed(KeyboardState keyboard, Keys key)
    {
        return keyboard.IsKeyDown(key) && _previousKeyboardState.IsKeyUp(key);
    }

    // Highlights the hovered tile and previews the active ship's path to it, if it can get there.
    private void UpdateHover(Vector2 mousePosition)
    {
        _hoveredTile = null;
        _previewPath = null;

        var picked = PickPlane(mousePosition, _hexGrid.TileY);
        if (picked == null)
            return;

        var tile = _hexGrid.WorldToAxial(picked.Value);
        _previewPath = _turns.PlanMove(tile);
        if (_previewPath != null)
            _hoveredTile = tile;
    }

    private Vector3? PickPlane(Vector2 mousePosition, float planeY)
    {
        var near = GraphicsDevice.Viewport.Unproject(new Vector3(mousePosition, 0f), _camera.Projection, _camera.View, Matrix.Identity);
        var far = GraphicsDevice.Viewport.Unproject(new Vector3(mousePosition, 1f), _camera.Projection, _camera.View, Matrix.Identity);

        Vector3 direction = far - near;
        if (Math.Abs(direction.Y) < 1e-6f)
            return null;

        float t = (planeY - near.Y) / direction.Y;
        if (t < 0f)
            return null;

        return near + direction * t;
    }

    private List<Vector3> ToWorldPositions(IReadOnlyList<(int Q, int R)> path)
    {
        var positions = new List<Vector3>(path.Count);
        foreach (var (q, r) in path)
        {
            _hexGrid.TryGetTilePosition(q, r, out var position);
            positions.Add(position);
        }
        return positions;
    }

    protected override void Draw(GameTime gameTime)
    {
        _frameCount++;
        _fpsTimer += gameTime.ElapsedGameTime.TotalSeconds;
        if (_fpsTimer >= 1.0)
        {
            _fps = _frameCount / _fpsTimer;
            _frameCount = 0;
            _fpsTimer = 0;
        }

        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;

        float time = (float)gameTime.TotalGameTime.TotalSeconds;

        _water.PrepareTargets(_camera, DrawReflected);

        GraphicsDevice.Clear(SkyColor);
        _water.Draw(_camera, time);
        DrawScene(_camera.View);
        DrawHud();

        base.Draw(gameTime);
    }

    private void DrawHud()
    {
        string status = _turns.Phase == TurnPhase.Planning
            ? $"Turn {_turns.TurnNumber}  |  Planning  |  Active: {_turns.ActiveShip.Name}  |  TP {_turns.ActiveShip.TurnPoints}/{_turns.ActiveShip.MaxTurnPoints}"
            : $"Turn {_turns.TurnNumber}  |  Resolving";

        _spriteBatch.Begin();
        _spriteBatch.DrawString(_font, $"FPS: {_fps:0}", new Vector2(10, 10), Color.White);
        _spriteBatch.DrawString(_font, status, new Vector2(10, 30), Color.White);
        _spriteBatch.DrawString(_font, "Click: move   Backspace: undo   Enter: finish ship", new Vector2(10, 50), Color.White);
        _spriteBatch.End();
    }

    // Hex tiles are a gameplay overlay, not world geometry, so only ships get reflected.
    private void DrawReflected(Matrix reflectionView)
    {
        _effect.View = reflectionView;
        _effect.Projection = _camera.Projection;
        foreach (var ship in _turns.Ships)
            ship.Draw(GraphicsDevice, _effect);
    }

    private void DrawScene(Matrix view)
    {
        _effect.View = view;
        _effect.Projection = _camera.Projection;
        _hexGrid.Draw(GraphicsDevice, _effect, _hoveredTile);
        _island.Draw(GraphicsDevice, _effect, _hexGrid);

        foreach (var ship in _turns.Ships)
        {
            foreach (var order in ship.PlannedOrders)
            {
                if (order is MoveOrder move)
                    PathLine.Draw(GraphicsDevice, _effect, ToWorldPositions(move.Path), PlannedPathColor);
            }
        }

        if (_previewPath != null)
            PathLine.Draw(GraphicsDevice, _effect, ToWorldPositions(_previewPath), PreviewPathColor);

        foreach (var ship in _turns.Ships)
            ship.Draw(GraphicsDevice, _effect);
    }
}
