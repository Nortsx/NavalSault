using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NavalSault.GameLogic;
using NavalSault.Utils;

namespace NavalSault;

public class Game1 : Game
{
    private const float WaterHeight = 0f;
    private const float WaveSpeed = 0.03f;
    private static readonly Color SkyColor = new(140, 185, 225);
    private static readonly Color DeepWaterColor = new(8, 42, 66);
    private static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(0.4f, -1f, 0.3f));

    private GraphicsDeviceManager _graphics;
    private Effect _waterEffect;
    private RenderTarget2D _reflectionTarget;
    private RenderTarget2D _refractionTarget;
    private Texture2D _distortionMap;
    private Texture2D _normalMap;
    private BasicEffect _effect;
    private Camera _camera;
    private WaterPlane _waterPlane;
    private HexGrid _hexGrid;
    private Ship _ship;
    private MouseState _previousMouseState;
    private (int Q, int R)? _hoveredTile;
    private MovePathfinder _pathfinder;
    private PathLine _pathLine;

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

        _waterPlane = new WaterPlane(GraphicsDevice, width: 360f, depth: 360f, segmentsX: 90, segmentsZ: 90);
        _hexGrid = new HexGrid(GraphicsDevice, hexRadius: 8f, gridRadius: 8, tileY: 1.3f, color: new Color(0, 255, 128), highlightColor: new Color(150, 255, 190));

        _hexGrid.TryGetTilePosition(0, 0, out var shipStart);
        _ship = new Ship(GraphicsDevice, shipStart, size: 6f, baseColor: new Color(200, 60, 40), selectedColor: new Color(255, 225, 70),
            facing: HexDirection.North, initiative: 5, moveRadius: 2, weapon: new Weapon("Cannon", damage: 10, range: 3));

        _pathfinder = new MovePathfinder(_hexGrid);
        _pathLine = new PathLine(new Color(255, 225, 70));

        _previousMouseState = Mouse.GetState();

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("DefaultFont");

        _waterEffect = Content.Load<Effect>("Water");
        var pp = GraphicsDevice.PresentationParameters;
        _reflectionTarget = new RenderTarget2D(GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
        _refractionTarget = new RenderTarget2D(GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
        _distortionMap = WaterTextures.CreateDistortionMap(GraphicsDevice, seed: 1);
        _normalMap = WaterTextures.CreateNormalMap(GraphicsDevice, seed: 2);
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

        var target = _hexGrid.WorldToAxial(picked.Value);
        if (!_hexGrid.TryGetTilePosition(target.Q, target.R, out _))
        {
            _ship.IsSelected = false;
            return;
        }

        var path = GetMovePath(target);
        if (path != null)
            _ship.MoveAlong(ToWaypoints(path));
    }

    // Pairs each tile after the start with the direction of the step into it.
    private List<(Vector3 Position, HexDirection Facing)> ToWaypoints(List<(int Q, int R)> path)
    {
        var positions = ToWorldPositions(path);
        var waypoints = new List<(Vector3, HexDirection)>(path.Count - 1);
        for (int i = 1; i < path.Count; i++)
            waypoints.Add((positions[i], HexDirections.Between(path[i - 1], path[i])));
        return waypoints;
    }

    // Returns the path the ship would take to the tile, or null if it can't move there right now.
    private List<(int Q, int R)> GetMovePath((int Q, int R) tile)
    {
        if (_ship.IsMoving)
            return null;

        var shipTile = _hexGrid.WorldToAxial(_ship.Position);
        var path = _pathfinder.FindPath(shipTile, tile, _ship.Facing);
        if (path == null || path.Count - 1 > _ship.MoveRadius)
            return null;

        return path;
    }

    private List<Vector3> ToWorldPositions(List<(int Q, int R)> path)
    {
        var positions = new List<Vector3>(path.Count);
        foreach (var (q, r) in path)
        {
            _hexGrid.TryGetTilePosition(q, r, out var position);
            positions.Add(position);
        }
        return positions;
    }

    private void UpdateHoveredTile(Vector2 mousePosition)
    {
        _hoveredTile = null;
        _pathLine.SetPath(null);

        if (!_ship.IsSelected)
            return;

        var picked = PickPlane(mousePosition, _hexGrid.TileY);
        if (picked == null)
            return;

        var axial = _hexGrid.WorldToAxial(picked.Value);
        if (!_hexGrid.TryGetTilePosition(axial.Q, axial.R, out _))
            return;

        var path = GetMovePath(axial);
        if (path == null)
            return;

        _hoveredTile = axial;
        _pathLine.SetPath(ToWorldPositions(path));
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

        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;

        float time = (float)gameTime.TotalGameTime.TotalSeconds;
        _waterPlane.UpdateWaves(time);

        DrawRefraction();
        Matrix reflectionView = DrawReflection();

        GraphicsDevice.Clear(SkyColor);
        DrawWater(reflectionView, time);
        DrawScene(_camera.View);

        string hoverText = _hoveredTile.HasValue ? $"({_hoveredTile.Value.Q}, {_hoveredTile.Value.R})" : "none";

        _spriteBatch.Begin();
        _spriteBatch.DrawString(_font, $"FPS: {_fps:0}", new Vector2(10, 10), Color.White);
        _spriteBatch.DrawString(_font, $"Selected: {_ship.IsSelected}  Hover: {hoverText}", new Vector2(10, 30), Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    // Nothing in the scene sits below the waterline yet, so the refraction target is just the deep-water color.
    private void DrawRefraction()
    {
        GraphicsDevice.SetRenderTarget(_refractionTarget);
        GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, DeepWaterColor, 1f, 0);
        GraphicsDevice.SetRenderTarget(null);
    }

    private Matrix DrawReflection()
    {
        var planeNormal = Vector3.Up;
        var planePosition = new Vector3(0f, WaterHeight, 0f);
        float projLength = Vector3.Dot(planeNormal, _camera.Position - planePosition);
        var reflectionCamPos = _camera.Position - 2f * planeNormal * projLength;
        var reflectionCamForward = Vector3.Reflect(_camera.Forward, planeNormal);
        var reflectionCamUp = Vector3.Reflect(_camera.Up, planeNormal);
        var reflectionView = Matrix.CreateLookAt(reflectionCamPos, reflectionCamPos + reflectionCamForward, reflectionCamUp);

        GraphicsDevice.SetRenderTarget(_reflectionTarget);
        GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, SkyColor, 1f, 0);
        // Hex tiles are a gameplay overlay, not world geometry, so only the ship gets reflected.
        _effect.View = reflectionView;
        _effect.Projection = _camera.Projection;
        _ship.Draw(GraphicsDevice, _effect);
        GraphicsDevice.SetRenderTarget(null);

        return reflectionView;
    }

    private void DrawScene(Matrix view)
    {
        _effect.View = view;
        _effect.Projection = _camera.Projection;
        _hexGrid.Draw(GraphicsDevice, _effect, _hoveredTile);
        _pathLine.Draw(GraphicsDevice, _effect);
        _ship.Draw(GraphicsDevice, _effect);
    }

    private void DrawWater(Matrix reflectionView, float time)
    {
        var world = Matrix.Identity;
        var p = _waterEffect.Parameters;

        p["World"].SetValue(world);
        p["WorldViewProjection"].SetValue(world * _camera.View * _camera.Projection);
        p["ReflectionView"].SetValue(reflectionView);
        p["Projection"].SetValue(_camera.Projection);

        p["ReflectionTexture"].SetValue(_reflectionTarget);
        p["RefractionTexture"].SetValue(_refractionTarget);
        p["DistortionMap"].SetValue(_distortionMap);
        p["NormalMap"].SetValue(_normalMap);
        p["Tiling"].SetValue(Vector2.One * 12f);

        p["MoveFactor"].SetValue(WaveSpeed * time % 1f);
        p["WaveStrength"].SetValue(0.01f);
        p["FresnelPower"].SetValue(2f);

        p["CameraPosition"].SetValue(_camera.Position);
        p["LightPosition"].SetValue(-LightDirection * 10000f);
        p["LightColor"].SetValue(Color.White.ToVector3());
        p["Shininess"].SetValue(25f);
        p["KSpecular"].SetValue(0.3f);

        _waterEffect.CurrentTechnique = _waterEffect.Techniques["Water"];
        _waterPlane.Draw(GraphicsDevice, _waterEffect);
    }
}
