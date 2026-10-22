using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NavalSault.GameLogic;
using NavalSault.Utils;

namespace NavalSault.Screens;

// The main menu over open water: a button per scenario in the catalog, with the hovered one's description below, and Quit.
public class MenuScreen : IDisposable
{
    private const string Title = "Naval Sault";
    private const float TitleScale = 3f;
    private const int ButtonWidth = 320, ButtonHeight = 48, ButtonGap = 12;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _font;
    private readonly Texture2D _pixel;
    // Looks down at empty sea behind the menu.
    private readonly Camera _camera;
    private readonly List<(Button Button, ScenarioEntry Entry)> _scenarioButtons = new();
    private readonly Button _quitButton = new("Quit");
    private readonly int _panelWidth;

    private MouseState _previousMouse;

    // The scenario the player picked, or null while they're still choosing.
    public ScenarioEntry ChosenScenario { get; private set; }
    public bool QuitRequested { get; private set; }

    public MenuScreen(GraphicsDevice graphicsDevice, SpriteFont font)
    {
        _graphicsDevice = graphicsDevice;
        _font = font;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _camera = new Camera(graphicsDevice, Vector3.Zero);

        // Wide enough for the title and the longest description.
        _panelWidth = 600;
        foreach (var entry in Scenario.Catalog)
        {
            _scenarioButtons.Add((new Button(entry.Name), entry));
            _panelWidth = Math.Max(_panelWidth, (int)font.MeasureString(entry.Description).X + 60);
        }
        LayOutButtons();

        // A click still held from the previous screen doesn't count as a fresh one here.
        _previousMouse = Mouse.GetState();
    }

    public void Update(MouseState mouse, bool escapePressed)
    {
        if (escapePressed)
            QuitRequested = true;

        LayOutButtons();

        bool leftClicked = mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
        if (leftClicked)
        {
            foreach (var (button, entry) in _scenarioButtons)
            {
                if (button.Contains(mouse.Position))
                    ChosenScenario = entry;
            }
            if (_quitButton.Contains(mouse.Position))
                QuitRequested = true;
        }
        _previousMouse = mouse;
    }

    public void Draw(GameTime gameTime, WaterRenderer water)
    {
        // Nothing stands on the water here, so there's nothing to reflect.
        water.PrepareTargets(_camera, _ => { });
        _graphicsDevice.Clear(Game1.SkyColor);
        water.Draw(_camera, (float)gameTime.TotalGameTime.TotalSeconds);

        var viewport = _graphicsDevice.Viewport;
        var mousePosition = Mouse.GetState().Position;
        float centerX = viewport.Width / 2f;

        _spriteBatch.Begin();

        var top = _scenarioButtons.Count > 0 ? _scenarioButtons[0].Button.Bounds.Top : _quitButton.Bounds.Top;
        var panel = new Rectangle((int)centerX - _panelWidth / 2, top - 160, _panelWidth, _quitButton.Bounds.Bottom - top + 240);
        _spriteBatch.Draw(_pixel, panel, Color.Black * 0.55f);

        var titleSize = _font.MeasureString(Title) * TitleScale;
        _spriteBatch.DrawString(_font, Title, new Vector2((int)(centerX - titleSize.X / 2f), top - 130), Color.White,
            0f, Vector2.Zero, TitleScale, SpriteEffects.None, 0f);
        DrawCentered("Choose a scenario", centerX, top - 40, Color.LightGray);

        string description = null;
        foreach (var (button, entry) in _scenarioButtons)
        {
            button.Draw(_spriteBatch, _pixel, _font, mousePosition);
            if (button.Contains(mousePosition))
                description = entry.Description;
        }
        _quitButton.Draw(_spriteBatch, _pixel, _font, mousePosition);

        if (description != null)
            DrawCentered(description, centerX, _quitButton.Bounds.Bottom + 30, Color.White);

        _spriteBatch.End();
    }

    // Scenario buttons stacked in a column around the middle of the screen, Quit a little apart below them.
    private void LayOutButtons()
    {
        var viewport = _graphicsDevice.Viewport;
        int left = (viewport.Width - ButtonWidth) / 2;
        int columnHeight = (_scenarioButtons.Count + 1) * (ButtonHeight + ButtonGap) + ButtonGap;
        int y = (viewport.Height - columnHeight) / 2;

        foreach (var (button, _) in _scenarioButtons)
        {
            button.Bounds = new Rectangle(left, y, ButtonWidth, ButtonHeight);
            y += ButtonHeight + ButtonGap;
        }
        _quitButton.Bounds = new Rectangle(left, y + ButtonGap, ButtonWidth, ButtonHeight);
    }

    private void DrawCentered(string text, float centerX, float y, Color color)
    {
        var size = _font.MeasureString(text);
        _spriteBatch.DrawString(_font, text, new Vector2((int)(centerX - size.X / 2f), (int)y), color);
    }

    public void Dispose()
    {
        _spriteBatch.Dispose();
        _pixel.Dispose();
    }
}
