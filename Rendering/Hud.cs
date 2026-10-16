using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NavalSault.GameLogic;
using NavalSault.Input;
using NavalSault.Utils;

namespace NavalSault.Rendering;

// Screen-space overlay: FPS, turn status, controls help, the ship list, HP bars over ships and the action buttons.
public class Hud
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

    private int _frameCount;
    private double _fpsTimer;
    private double _fps;

    public Hud(GraphicsDevice graphicsDevice, SpriteFont font, TurnManager turns, PlayerController controller)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _font = font;
        _turns = turns;
        _controller = controller;
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
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
                Side.Player => "Victory  |  Every enemy ship sunk",
                Side.Enemy => "Defeat  |  Your fleet was sunk",
                _ => "Game over  |  Every ship sank"
            }
        };

        _spriteBatch.Begin();
        DrawHpBars(camera);
        _spriteBatch.DrawString(_font, $"FPS: {_fps:0}", new Vector2(10, 10), Color.White);
        _spriteBatch.DrawString(_font, status, new Vector2(10, 30), Color.White);
        _spriteBatch.DrawString(_font, "M: move   F: shoot   Q/E: end facing   Right click: back to move   Wheel: zoom   Backspace: undo   Enter: finish ship",
            new Vector2(10, 50), Color.White);

        if (_controller.ButtonsVisible)
        {
            var mousePosition = Mouse.GetState().Position;
            foreach (var button in _controller.Buttons)
                button.Draw(_spriteBatch, _pixel, _font, mousePosition);
        }

        float y = 80;
        foreach (var ship in _turns.Ships)
        {
            string weapon = ship.Weapon == null ? "" : $"  |  {ship.Weapon.Name}: range {ship.Weapon.Range}, {ship.Weapon.ShotCost} TP";
            string side = ship.IsPlayerControlled ? "" : " [enemy]";
            string hp = ship.IsSunk ? "SUNK" : $"HP {ship.Hp}/{ship.MaxHp}";
            _spriteBatch.DrawString(_font, $"{ship.Name}{side} ({ship.Footprint.Name})  {hp}{weapon}", new Vector2(10, y), Color.White);
            y += 20;
        }
        _spriteBatch.End();
    }

    // A health bar over every ship still afloat, green for the player's and red for the enemy's.
    private void DrawHpBars(Camera camera)
    {
        const int width = 60, height = 7, border = 1;

        foreach (var ship in _turns.Ships)
        {
            if (ship.IsSunk)
                continue;

            var screen = _graphicsDevice.Viewport.Project(ship.Position + HpBarOffset, camera.Projection, camera.View, Matrix.Identity);
            if (screen.Z < 0f || screen.Z > 1f)
                continue;

            int left = (int)screen.X - width / 2;
            int top = (int)screen.Y - height / 2;
            int filled = (int)Math.Ceiling((width - 2 * border) * (float)ship.Hp / ship.MaxHp);
            _spriteBatch.Draw(_pixel, new Rectangle(left, top, width, height), HpBackColor);
            _spriteBatch.Draw(_pixel, new Rectangle(left + border, top + border, filled, height - 2 * border),
                ship.IsPlayerControlled ? PlayerHpColor : EnemyHpColor);
        }
    }
}
