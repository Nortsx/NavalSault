using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NavalSault;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private BasicEffect _effect;
    private Camera _camera;
    private WaterPlane _waterPlane;
    private HexGrid _hexGrid;
    private Ship _ship;
    private MouseState _previousMouseState;
    private (int Q, int R)? _hoveredTile;

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
        _effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(0.4f, -1f, 0.3f));
        _effect.DirectionalLight0.DiffuseColor = new Vector3(1f, 1f, 0.95f);
        _effect.DirectionalLight0.SpecularColor = Vector3.Zero;
        _effect.DirectionalLight1.Enabled = false;
        _effect.DirectionalLight2.Enabled = false;

        _waterPlane = new WaterPlane(GraphicsDevice, width: 360f, depth: 360f, segmentsX: 90, segmentsZ: 90);
        _hexGrid = new HexGrid(GraphicsDevice, hexRadius: 8f, gridRadius: 4, tileY: 1.3f, color: new Color(0, 255, 128), highlightColor: new Color(150, 255, 190));

        _hexGrid.TryGetTilePosition(0, 0, out var shipStart);
        _ship = new Ship(GraphicsDevice, shipStart, size: 6f, baseColor: new Color(200, 60, 40), selectedColor: new Color(255, 225, 70));

        _previousMouseState = Mouse.GetState();

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("DefaultFont");
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
            Exit();

        _camera.Update(gameTime, keyboard);

        var mouse = Mouse.GetState();
        var mousePosition = new Vector2(mouse.X, mouse.Y);

        if (mouse.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            HandleClick(mousePosition);
        _previousMouseState = mouse;

        UpdateHoveredTile(mousePosition);

        _ship.Update(gameTime);

        base.Update(gameTime);
    }

    private void HandleClick(Vector2 mousePosition)
    {
        var picked = PickPlane(mousePosition, _hexGrid.TileY);
        if (picked == null)
            return;

        Vector2 pickedXZ = new Vector2(picked.Value.X, picked.Value.Z);
        Vector2 shipXZ = new Vector2(_ship.Position.X, _ship.Position.Z);

        if (Vector2.Distance(pickedXZ, shipXZ) <= _ship.FootprintRadius)
        {
            _ship.IsSelected = true;
            return;
        }

        if (!_ship.IsSelected)
            return;

        if (_hexGrid.TryPickTile(picked.Value, out var tilePosition))
            _ship.MoveTo(tilePosition);
        else
            _ship.IsSelected = false;
    }

    private void UpdateHoveredTile(Vector2 mousePosition)
    {
        if (!_ship.IsSelected)
        {
            _hoveredTile = null;
            return;
        }

        var picked = PickPlane(mousePosition, _hexGrid.TileY);
        if (picked != null)
        {
            var axial = _hexGrid.WorldToAxial(picked.Value);
            _hoveredTile = _hexGrid.TryGetTilePosition(axial.Q, axial.R, out _) ? axial : null;
        }
        else
        {
            _hoveredTile = null;
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

        GraphicsDevice.Clear(Color.CornflowerBlue);

        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;

        _waterPlane.UpdateWaves((float)gameTime.TotalGameTime.TotalSeconds);

        _effect.World = Matrix.Identity;
        _effect.View = _camera.View;
        _effect.Projection = _camera.Projection;

        _effect.DiffuseColor = new Color(25, 70, 110).ToVector3();
        _waterPlane.Draw(GraphicsDevice, _effect);

        _hexGrid.Draw(GraphicsDevice, _effect, _hoveredTile);

        _ship.Draw(GraphicsDevice, _effect);

        string hoverText = _hoveredTile.HasValue ? $"({_hoveredTile.Value.Q}, {_hoveredTile.Value.R})" : "none";

        _spriteBatch.Begin();
        _spriteBatch.DrawString(_font, $"FPS: {_fps:0}", new Vector2(10, 10), Color.White);
        _spriteBatch.DrawString(_font, $"Selected: {_ship.IsSelected}  Hover: {hoverText}", new Vector2(10, 30), Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
