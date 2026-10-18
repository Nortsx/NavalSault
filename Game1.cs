using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NavalSault.GameLogic;
using NavalSault.GameLogic.Scripting;
using NavalSault.Input;
using NavalSault.Rendering;
using NavalSault.Utils;

namespace NavalSault;

public class Game1 : Game
{
    private static readonly Color SkyColor = new(140, 185, 225);
    private static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(0.4f, -1f, 0.3f));

    private GraphicsDeviceManager _graphics;
    private Camera _camera;
    private WaterRenderer _water;
    private TurnManager _turns;
    private ScriptRunner _script;
    private PlayerController _controller;
    private BattleRenderer _battleRenderer;
    private Hud _hud;
    private int _previousScrollWheel;

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
        _water = new WaterRenderer(GraphicsDevice, Content.Load<Effect>("Water"), SkyColor, LightDirection);
        var scenario = Scenario.Skirmish(GraphicsDevice);
        var grid = scenario.Grid;
        _camera.SetPanBounds(Vector3.Zero, grid.WorldRadius);

        _turns = new TurnManager(scenario.Ships, grid, new MovePathfinder(grid), scenario.VictoryConditions);
        _script = new ScriptRunner(_turns, scenario.Events);

        _controller = new PlayerController(GraphicsDevice, _camera, grid, _turns);
        _battleRenderer = new BattleRenderer(GraphicsDevice, LightDirection, grid, scenario.Island, _turns, _controller);
        _hud = new Hud(GraphicsDevice, Content.Load<SpriteFont>("DefaultFont"), _turns, _controller, _script.Context);
        _turns.Start();
        _previousScrollWheel = Mouse.GetState().ScrollWheelValue;
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
            Exit();

        var mouse = Mouse.GetState();
        _camera.Update(gameTime, keyboard, mouse.ScrollWheelValue - _previousScrollWheel);
        _previousScrollWheel = mouse.ScrollWheelValue;

        _controller.Update(keyboard, mouse);
        _turns.Update(gameTime);
        _script.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;

        float time = (float)gameTime.TotalGameTime.TotalSeconds;

        _water.PrepareTargets(_camera, view => _battleRenderer.DrawReflected(view, _camera.Projection));

        GraphicsDevice.Clear(SkyColor);
        _water.Draw(_camera, time);
        _battleRenderer.Draw(_camera.View, _camera.Projection);
        _hud.Draw(gameTime, _camera);

        base.Draw(gameTime);
    }
}
