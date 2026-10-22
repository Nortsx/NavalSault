using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NavalSault.GameLogic;
using NavalSault.GameLogic.Scripting;
using NavalSault.Input;
using NavalSault.Rendering;
using NavalSault.Utils;

namespace NavalSault.Screens;

// One battle from start to finish: builds everything a scenario needs, runs the turns and draws them.
// Escape, or the button shown once the battle is over, takes the player back to the menu.
// Everything the battle built or loaded is released when it's disposed.
public class BattleScreen : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _font;
    private readonly Texture2D _pixel;
    // Assets loaded for this battle only; unloading it disposes them all.
    private readonly ContentManager _content;
    private readonly Scenario _scenario;
    private readonly Camera _camera;
    private readonly TurnManager _turns;
    private readonly ScriptRunner _script;
    private readonly PlayerController _controller;
    private readonly BattleRenderer _battleRenderer;
    private readonly Hud _hud;
    private readonly Button _menuButton = new("Back to Menu");

    private int _previousScrollWheel;
    private MouseState _previousMouse;

    // Set once the player asks to leave the battle.
    public bool ExitRequested { get; private set; }

    public BattleScreen(GraphicsDevice graphicsDevice, IServiceProvider services, SpriteFont font, Vector3 lightDirection, ScenarioEntry entry)
    {
        _graphicsDevice = graphicsDevice;
        _font = font;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);

        _content = new ContentManager(services, "Content");
        _scenario = entry.Create(graphicsDevice, _content);
        var grid = _scenario.Grid;
        _camera = new Camera(graphicsDevice, Vector3.Zero);
        _camera.SetPanBounds(Vector3.Zero, grid.WorldRadius);

        _turns = new TurnManager(_scenario.Ships, grid, new MovePathfinder(grid), _scenario.VictoryConditions);
        _script = new ScriptRunner(_turns, _scenario.Events);

        _controller = new PlayerController(graphicsDevice, _camera, grid, _turns);
        _battleRenderer = new BattleRenderer(graphicsDevice, lightDirection, grid, _scenario.Island, _turns, _controller);
        _hud = new Hud(graphicsDevice, font, _turns, _controller, _script.Context);
        _turns.Start();

        var mouse = Mouse.GetState();
        _previousScrollWheel = mouse.ScrollWheelValue;
        _previousMouse = mouse;
    }

    public void Update(GameTime gameTime, KeyboardState keyboard, MouseState mouse, bool escapePressed)
    {
        if (escapePressed)
            ExitRequested = true;

        _camera.Update(gameTime, keyboard, mouse.ScrollWheelValue - _previousScrollWheel);
        _previousScrollWheel = mouse.ScrollWheelValue;

        _controller.Update(keyboard, mouse);
        _turns.Update(gameTime);
        _script.Update(gameTime);

        if (_turns.Phase == TurnPhase.GameOver)
        {
            const int width = 220, height = 48;
            var viewport = _graphicsDevice.Viewport;
            _menuButton.Bounds = new Rectangle((viewport.Width - width) / 2, viewport.Height / 2 + 20, width, height);

            bool leftClicked = mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
            if (leftClicked && _menuButton.Contains(mouse.Position))
                ExitRequested = true;
        }
        _previousMouse = mouse;
    }

    public void Draw(GameTime gameTime, WaterRenderer water)
    {
        float time = (float)gameTime.TotalGameTime.TotalSeconds;

        water.PrepareTargets(_camera, view => _battleRenderer.DrawReflected(view, _camera.Projection));

        _graphicsDevice.Clear(Game1.SkyColor);
        water.Draw(_camera, time);
        _battleRenderer.Draw(_camera.View, _camera.Projection);
        _hud.Draw(gameTime, _camera);

        if (_turns.Phase == TurnPhase.GameOver)
            DrawGameOver();
    }

    // A banner with the outcome in the middle of the screen, and the way back to the menu under it.
    private void DrawGameOver()
    {
        string title = _turns.Winner switch
        {
            Side.Player => "Victory",
            Side.Enemy => "Defeat",
            _ => "Game over"
        };
        var viewport = _graphicsDevice.Viewport;
        var titleSize = _font.MeasureString(title) * 2f;
        var reasonSize = _font.MeasureString(_turns.GameOverReason ?? "");
        var center = new Vector2(viewport.Width / 2f, viewport.Height / 2f);

        _spriteBatch.Begin();
        _spriteBatch.Draw(_pixel, new Rectangle((int)center.X - 220, (int)center.Y - 90, 440, 180), Color.Black * 0.6f);
        _spriteBatch.DrawString(_font, title, new Vector2((int)(center.X - titleSize.X / 2f), (int)(center.Y - 75)), Color.White,
            0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_font, _turns.GameOverReason ?? "", new Vector2((int)(center.X - reasonSize.X / 2f), (int)(center.Y - 15)), Color.White);
        _menuButton.Draw(_spriteBatch, _pixel, _font, Mouse.GetState().Position);
        _spriteBatch.End();
    }

    public void Dispose()
    {
        _hud.Dispose();
        _battleRenderer.Dispose();
        _turns.Dispose();
        _scenario.Dispose();
        _content.Unload();
        _content.Dispose();
        _spriteBatch.Dispose();
        _pixel.Dispose();
    }
}
