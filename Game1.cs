using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NavalSault.Screens;
using NavalSault.Utils;

namespace NavalSault;

// Owns the window and what every screen shares, and switches between the main menu and a battle.
public class Game1 : Game
{
    public static readonly Color SkyColor = new(140, 185, 225);
    private static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(0.4f, -1f, 0.3f));

    private GraphicsDeviceManager _graphics;
    private WaterRenderer _water;
    private SpriteFont _font;
    private KeyboardState _previousKeyboard;
    private GamePadState _previousGamePad;

    // Exactly one of these is set: the menu while choosing, the battle while playing.
    private MenuScreen _menu;
    private BattleScreen _battle;

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

    protected override void LoadContent()
    {
        _water = new WaterRenderer(GraphicsDevice, Content.Load<Effect>("Water"), SkyColor, LightDirection);
        _font = Content.Load<SpriteFont>("DefaultFont");
        _menu = new MenuScreen(GraphicsDevice, _font);
        _previousKeyboard = Keyboard.GetState();
        _previousGamePad = GamePad.GetState(PlayerIndex.One);
    }

    // The shared assets loaded into Content are released with it when the game shuts down.
    protected override void UnloadContent()
    {
        _battle?.Dispose();
        _menu?.Dispose();
        _water.Dispose();
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var gamePad = GamePad.GetState(PlayerIndex.One);
        var mouse = Mouse.GetState();
        // Escape backs out one screen: from a battle to the menu, from the menu out of the game.
        bool escapePressed = (keyboard.IsKeyDown(Keys.Escape) && _previousKeyboard.IsKeyUp(Keys.Escape))
                             || (gamePad.Buttons.Back == ButtonState.Pressed && _previousGamePad.Buttons.Back == ButtonState.Released);
        _previousKeyboard = keyboard;
        _previousGamePad = gamePad;

        if (_battle != null)
        {
            _battle.Update(gameTime, keyboard, mouse, escapePressed);
            if (_battle.ExitRequested)
            {
                _battle.Dispose();
                _battle = null;
                _menu = new MenuScreen(GraphicsDevice, _font);
            }
        }
        else
        {
            _menu.Update(mouse, escapePressed);
            if (_menu.QuitRequested)
                Exit();
            else if (_menu.ChosenScenario is { } scenario)
            {
                _menu.Dispose();
                _menu = null;
                _battle = new BattleScreen(GraphicsDevice, Services, _font, LightDirection, scenario);
            }
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;

        if (_battle != null)
            _battle.Draw(gameTime, _water);
        else
            _menu.Draw(gameTime, _water);

        base.Draw(gameTime);
    }
}
