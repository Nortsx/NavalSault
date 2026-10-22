using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NavalSault.GameLogic;
using NavalSault.GameLogic.Scripting;
using NavalSault.Input;
using NavalSault.Utils;

namespace NavalSault.Rendering;

// Screen-space overlay: FPS, turn status, controls help, HP bars over ships, the initiative bar and the action buttons.
public class Hud : IDisposable
{
    private static readonly Color PlayerHpColor = new(90, 220, 110);
    private static readonly Color EnemyHpColor = new(230, 60, 50);
    private static readonly Color HpBackColor = Color.Black * 0.75f;
    // Health bars float this far above a ship's anchor tile.
    private static readonly Vector3 HpBarOffset = new(0f, 9f, 0f);

    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _font;
    private readonly Texture2D _pixel;
    private readonly TurnManager _turns;
    private readonly PlayerController _controller;
    private readonly ScriptContext _script;
    private readonly InitiativeBar _initiativeBar;

    private int _frameCount;
    private double _fpsTimer;
    private double _fps;

    public Hud(GraphicsDevice graphicsDevice, SpriteFont font, TurnManager turns, PlayerController controller, ScriptContext script)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _font = font;
        _turns = turns;
        _controller = controller;
        _script = script;
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _initiativeBar = new InitiativeBar(graphicsDevice, font, _pixel, turns);
    }

    public void Draw(GameTime gameTime, Camera camera)
    {
        _frameCount++;
        _fpsTimer += gameTime.ElapsedGameTime.TotalSeconds;
        if (_fpsTimer >= 1.0)
        {
            _fps = _frameCount / _fpsTimer;
            _frameCount = 0;
            _fpsTimer = 0;
        }

        string status = _turns.Phase switch
        {
            TurnPhase.Planning =>
                $"Turn {_turns.TurnNumber}  |  Planning  |  Active: {_turns.ActiveShip.Name}  |  TP {_turns.ActiveShip.TurnPoints}/{_turns.ActiveShip.MaxTurnPoints}",
            TurnPhase.Resolving => $"Turn {_turns.TurnNumber}  |  Resolving",
            _ => _turns.Winner switch
            {
                Side.Player => "Victory",
                Side.Enemy => "Defeat",
                _ => "Game over"
            } + $"  |  {_turns.GameOverReason}"
        };

        _spriteBatch.Begin();
        DrawHpBars(camera);
        _spriteBatch.DrawString(_font, $"FPS: {_fps:0}", new Vector2(10, 10), Color.White);
        _spriteBatch.DrawString(_font, status, new Vector2(10, 30), Color.White);
        _spriteBatch.DrawString(_font, "M: move   F: shoot   Q/E: end facing   Right click: back to move   Wheel: zoom   Backspace: undo   Enter: finish ship   Esc: menu",
            new Vector2(10, 50), Color.White);

        if (_script.Message is { } message)
        {
            var size = _font.MeasureString(message);
            var position = new Vector2((_graphicsDevice.Viewport.Width - size.X) / 2f, 30);
            _spriteBatch.Draw(_pixel, new Rectangle((int)position.X - 12, (int)position.Y - 6, (int)size.X + 24, (int)size.Y + 12), Color.Black * 0.6f);
            _spriteBatch.DrawString(_font, message, position, Color.White);
        }

        if (_turns.Phase != TurnPhase.GameOver)
            _initiativeBar.Draw(_spriteBatch);

        if (_controller.ButtonsVisible)
        {
            var mousePosition = Mouse.GetState().Position;
            foreach (var button in _controller.Buttons)
                button.Draw(_spriteBatch, _pixel, _font, mousePosition);
        }
        _spriteBatch.End();
    }

    // A health bar over every ship still afloat, green for the player's and red for the enemy's.
    private void DrawHpBars(Camera camera)
    {
        const int width = 60, height = 7, border = 1;

        foreach (var ship in _turns.Ships)
        {
            if (ship.IsSunk || !_turns.Vision.CanSee(Side.Player, ship))
                continue;

            var screen = _graphicsDevice.Viewport.Project(ship.Position + HpBarOffset, camera.Projection, camera.View, Matrix.Identity);
            if (screen.Z < 0f || screen.Z > 1f)
                continue;

            int left = (int)screen.X - width / 2;
            int top = (int)screen.Y - height / 2;
            int filled = (int)Math.Ceiling((width - 2 * border) * (float)ship.Hp / ship.MaxHp);
            _spriteBatch.Draw(_pixel, new Rectangle(left, top, width, height), HpBackColor);
            _spriteBatch.Draw(_pixel, new Rectangle(left + border, top + border, filled, height - 2 * border),
                ship.Side == Side.Player ? PlayerHpColor : EnemyHpColor);
        }
    }

    public void Dispose()
    {
        _initiativeBar.Dispose();
        _spriteBatch.Dispose();
        _pixel.Dispose();
    }
}
