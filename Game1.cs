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
    private enum ActionMode
    {
        Move,
        Shoot
    }

    private static readonly Color SkyColor = new(140, 185, 225);
    private static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(0.4f, -1f, 0.3f));
    private static readonly Color ActiveShipColor = new(255, 225, 70);
    private static readonly Color PreviewPathColor = new(255, 225, 70);
    private static readonly Color PlannedPathColor = new(255, 255, 255);
    private static readonly Color PreviewShotColor = new(255, 90, 60);
    private static readonly Color PlannedShotColor = new(255, 255, 255);
    private static readonly Color ShellColor = new(255, 240, 180);
    private static readonly Color SplashColor = new(200, 230, 255);
    private static readonly Color HitColor = new(255, 50, 30);
    // Shells leave from about deck height above the tile.
    private static readonly Vector3 MuzzleOffset = new(0f, 3f, 0f);
    // Share of the arc drawn behind the shell as a tracer.
    private const float TracerLength = 0.2f;
    private const float SplashHeight = 8f;

    private GraphicsDeviceManager _graphics;
    private BasicEffect _effect;
    private Camera _camera;
    private WaterRenderer _water;
    private HexGrid _hexGrid;
    private HexOverlay _overlay;
    private Island _island;
    private TurnManager _turns;
    private MouseState _previousMouseState;
    private KeyboardState _previousKeyboardState;
    private (int Q, int R)? _hoveredTile;
    private List<(int Q, int R)> _previewPath;
    // Direction the active ship would face at the end of the previewed path.
    private HexDirection _previewFacing;
    // End facing picked with Q/E for the next move; null lets the pathfinder choose.
    private HexDirection? _desiredFacing;
    // What a click on a tile does for the active ship.
    private ActionMode _mode;
    private ShootOrder _previewShot;

    private SpriteBatch _spriteBatch;
    private SpriteFont _font;
    private Texture2D _pixel;
    private Button _moveButton;
    private Button _shootButton;
    private Button _endTurnButton;
    private Button[] _buttons;
    private int _frameCount;
    private double _fpsTimer;
    private double _fps;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1920;
        _graphics.PreferredBackBufferHeight = 1080;
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
        _overlay = new HexOverlay(GraphicsDevice, _hexGrid.HexRadius);
        _camera.SetPanBounds(Vector3.Zero, _hexGrid.WorldRadius);

        // Test island: two columns of three tiles, one tile of open water ahead of ship A.
        _island = new Island(GraphicsDevice, _hexGrid.HexRadius,
            [(0, -2), (0, -3), (0, -4), (1, -3), (1, -4), (1, -5)],
            new Color(194, 170, 110));
        foreach (var tile in _island.Tiles)
            _hexGrid.SetBlocked(tile, true);

        var ships = new[]
        {
            CreateShip("Ship A", HexFootprint.Line1x3, (0, 0), new Color(200, 60, 40), initiative: 5),
            CreateShip("Ship B", HexFootprint.Wide2x3, (3, -1), new Color(60, 90, 200), initiative: 3)
        };
        _turns = new TurnManager(ships, _hexGrid, new MovePathfinder(_hexGrid));

        _previousMouseState = Mouse.GetState();
        _previousKeyboardState = Keyboard.GetState();

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("DefaultFont");
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _moveButton = new Button("Move");
        _shootButton = new Button("Shoot");
        _endTurnButton = new Button("End Turn");
        _buttons = [_moveButton, _shootButton, _endTurnButton];
    }

    private Ship CreateShip(string name, HexFootprint footprint, (int Q, int R) tile, Color color, int initiative)
    {
        return new Ship(GraphicsDevice, _hexGrid, name, footprint, tile, baseColor: color, activeColor: ActiveShipColor,
            facing: HexDirection.North, initiative: initiative, maxTurnPoints: 5, maxHp: 30,
            weapon: new Weapon("Cannon", damage: 10, range: 6, shotCost: 3, spreadStep: 3));
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
            Exit();

        var mouse = Mouse.GetState();
        _camera.Update(gameTime, keyboard, mouse.ScrollWheelValue - _previousMouseState.ScrollWheelValue);

        if (WasPressed(keyboard, Keys.M))
            _mode = ActionMode.Move;
        if (WasPressed(keyboard, Keys.F))
            _mode = ActionMode.Shoot;
        if (WasPressed(keyboard, Keys.Enter))
            EndActiveShipTurn();
        if (WasPressed(keyboard, Keys.Back))
        {
            _turns.UndoLastOrder();
            _desiredFacing = null;
        }
        if (WasPressed(keyboard, Keys.Q))
            RotateDesiredFacing(-1);
        if (WasPressed(keyboard, Keys.E))
            RotateDesiredFacing(1);
        _previousKeyboardState = keyboard;

        var mousePosition = new Vector2(mouse.X, mouse.Y);
        bool leftClicked = mouse.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;

        if (mouse.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released)
        {
            _mode = ActionMode.Move;
            _desiredFacing = null;
        }

        UpdateButtons();
        bool overButton = ButtonsVisible && Array.Exists(_buttons, b => b.Contains(mouse.Position));

        if (leftClicked && _moveButton.IsEnabled && _moveButton.Contains(mouse.Position))
            _mode = ActionMode.Move;
        if (leftClicked && _shootButton.IsEnabled && _shootButton.Contains(mouse.Position))
            _mode = ActionMode.Shoot;
        if (leftClicked && _endTurnButton.IsEnabled && _endTurnButton.Contains(mouse.Position))
            EndActiveShipTurn();
        // Switching mode by key or click may have picked one the ship can't use.
        UpdateButtons();
        if (_mode != ActionMode.Move)
            _desiredFacing = null;

        // The buttons sit on top of the map, so the map ignores the mouse while it's over them.
        if (overButton)
        {
            _hoveredTile = null;
            _previewPath = null;
            _previewShot = null;
        }
        else
        {
            UpdateHover(mousePosition);
        }

        if (leftClicked && !overButton && _hoveredTile.HasValue)
        {
            if (_mode == ActionMode.Move)
            {
                if (_turns.TryAddMove(_hoveredTile.Value, _desiredFacing))
                    _desiredFacing = null;
            }
            else if (_turns.TryAddShot(_hoveredTile.Value))
                _mode = ActionMode.Move;
        }
        _previousMouseState = mouse;

        _turns.Update(gameTime);

        base.Update(gameTime);
    }

    private bool WasPressed(KeyboardState keyboard, Keys key)
    {
        return keyboard.IsKeyDown(key) && _previousKeyboardState.IsKeyUp(key);
    }

    // The active ship is done taking orders; the next one takes over, or the turn resolves after the last.
    private void EndActiveShipTurn()
    {
        _turns.FinishActiveShip();
        _mode = ActionMode.Move;
        _desiredFacing = null;
    }

    // Turns the end facing for the next move by 60° steps, starting from the facing the
    // previewed path would end with, or from where the ship faces if nothing is previewed.
    private void RotateDesiredFacing(int steps)
    {
        var ship = _turns.ActiveShip;
        if (ship == null || _mode != ActionMode.Move)
            return;

        var current = _desiredFacing ?? (_previewPath != null ? _previewFacing : ship.PlannedFacing);
        _desiredFacing = HexDirections.Turn(current, steps);
    }

    // Buttons only matter while a ship is taking orders.
    private bool ButtonsVisible => _turns.Phase == TurnPhase.Planning;

    // Lays the mode buttons out along the bottom centre and End Turn at the bottom right,
    // and enables only what the active ship can do. Falls back to Move if the ship can't afford a shot.
    private void UpdateButtons()
    {
        const int width = 160, height = 44, gap = 12, margin = 20;

        var viewport = GraphicsDevice.Viewport;
        int left = viewport.Width / 2 - width - gap / 2;
        int top = viewport.Height - height - margin;
        _moveButton.Bounds = new Rectangle(left, top, width, height);
        _shootButton.Bounds = new Rectangle(left + width + gap, top, width, height);
        _endTurnButton.Bounds = new Rectangle(viewport.Width - width - margin, top, width, height);

        var ship = _turns.ActiveShip;
        _moveButton.IsEnabled = ship != null;
        _endTurnButton.IsEnabled = ship != null;
        _shootButton.IsEnabled = ship?.Weapon != null && ship.TurnPoints >= ship.Weapon.ShotCost;
        _shootButton.Label = ship?.Weapon != null ? $"Shoot ({ship.Weapon.ShotCost} TP)" : "Shoot";

        if (!_shootButton.IsEnabled)
            _mode = ActionMode.Move;

        _moveButton.IsSelected = _moveButton.IsEnabled && _mode == ActionMode.Move;
        _shootButton.IsSelected = _shootButton.IsEnabled && _mode == ActionMode.Shoot;
    }

    // Highlights the hovered tile and previews the active ship's path to it, if it can get there,
    // or, while aiming, the shot at it and where the shell may land.
    private void UpdateHover(Vector2 mousePosition)
    {
        _hoveredTile = null;
        _previewPath = null;
        _previewShot = null;

        var picked = PickPlane(mousePosition, _hexGrid.TileY);
        if (picked == null)
            return;

        var tile = _hexGrid.WorldToAxial(picked.Value);
        if (_mode == ActionMode.Shoot)
        {
            _previewShot = _turns.PlanShot(tile);
            if (_previewShot != null)
                _hoveredTile = tile;
            return;
        }

        _previewPath = _turns.PlanMove(tile, _desiredFacing);
        if (_previewPath != null)
        {
            _hoveredTile = tile;
            _previewFacing = HexDirections.Between(_previewPath[^2], _previewPath[^1]);
        }
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
        string status = _turns.Phase switch
        {
            TurnPhase.Planning =>
                $"Turn {_turns.TurnNumber}  |  Planning  |  Active: {_turns.ActiveShip.Name}  |  TP {_turns.ActiveShip.TurnPoints}/{_turns.ActiveShip.MaxTurnPoints}",
            TurnPhase.Resolving => $"Turn {_turns.TurnNumber}  |  Resolving",
            _ => _turns.Ships.Count == 1 ? $"Game over  |  {_turns.Ships[0].Name} wins" : "Game over  |  Every ship sank"
        };

        _spriteBatch.Begin();
        _spriteBatch.DrawString(_font, $"FPS: {_fps:0}", new Vector2(10, 10), Color.White);
        _spriteBatch.DrawString(_font, status, new Vector2(10, 30), Color.White);
        _spriteBatch.DrawString(_font, "M: move   F: shoot   Q/E: end facing   Right click: back to move   Wheel: zoom   Backspace: undo   Enter: finish ship",
            new Vector2(10, 50), Color.White);

        if (ButtonsVisible)
        {
            var mousePosition = Mouse.GetState().Position;
            foreach (var button in _buttons)
                button.Draw(_spriteBatch, _pixel, _font, mousePosition);
        }

        float y = 80;
        foreach (var ship in _turns.Ships)
        {
            string weapon = ship.Weapon == null ? "" : $"  |  {ship.Weapon.Name}: range {ship.Weapon.Range}, {ship.Weapon.ShotCost} TP";
            _spriteBatch.DrawString(_font, $"{ship.Name} ({ship.Footprint.Name})  HP {ship.Hp}/{ship.MaxHp}{weapon}", new Vector2(10, y), Color.White);
            y += 20;
        }
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
                else if (order is ShootOrder shot && shot != _turns.ResolvingOrder)
                    DrawShotPlan(shot, PlannedShotColor, fillAlpha: 0.12f);
            }
        }

        // The tiles the active ship will cover once its planned orders are done.
        if (_turns.ActiveShip is { } active)
            _overlay.Draw(GraphicsDevice, _effect, _hexGrid, active.PlannedTiles, PlannedPathColor, fillAlpha: 0.08f);

        if (_previewPath != null)
        {
            PathLine.Draw(GraphicsDevice, _effect, ToWorldPositions(_previewPath), PreviewPathColor);
            _overlay.Draw(GraphicsDevice, _effect, _hexGrid,
                _turns.ActiveShip.Footprint.Tiles(_previewPath[^1], _previewFacing), PreviewPathColor, fillAlpha: 0.25f);
        }
        if (_previewShot != null)
            DrawShotPlan(_previewShot, PreviewShotColor, fillAlpha: 0.3f);
        if (_turns.ResolvingOrder is ShootOrder firing)
            DrawShotInFlight(firing);

        foreach (var ship in _turns.Ships)
            ship.Draw(GraphicsDevice, _effect);
    }

    // The arc from the firing tile to the aimed-at tile, and every tile the shell may land on.
    private void DrawShotPlan(ShootOrder shot, Color color, float fillAlpha)
    {
        var from = _hexGrid.AxialToWorld(shot.FiringTile) + MuzzleOffset;
        var to = _hexGrid.AxialToWorld(shot.TargetTile);
        PathLine.Draw(GraphicsDevice, _effect, ShotArc.Points(from, to, 0f, 1f, 24), color);

        _overlay.Draw(GraphicsDevice, _effect, _hexGrid, shot.SpreadTiles, color, fillAlpha);
        _overlay.Draw(GraphicsDevice, _effect, _hexGrid, [shot.TargetTile], color, fillAlpha);
    }

    // A tracer following the shell, then a splash column on the tile it landed on.
    private void DrawShotInFlight(ShootOrder shot)
    {
        var from = _hexGrid.AxialToWorld(shot.FiringTile) + MuzzleOffset;
        var to = _hexGrid.AxialToWorld(shot.LandingTile);

        if (!shot.HasLanded)
        {
            float t = shot.FlightProgress;
            PathLine.Draw(GraphicsDevice, _effect, ShotArc.Points(from, to, Math.Max(0f, t - TracerLength), t, 8), ShellColor);
            return;
        }

        var color = shot.HitShip != null ? HitColor : SplashColor;
        _overlay.Draw(GraphicsDevice, _effect, _hexGrid, [shot.LandingTile], color, fillAlpha: 0.6f);
        PathLine.Draw(GraphicsDevice, _effect, [to, to + new Vector3(0f, SplashHeight, 0f)], color);
    }
}
