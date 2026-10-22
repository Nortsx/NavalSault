using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NavalSault.GameLogic;
using NavalSault.Utils;

namespace NavalSault.Rendering;

// A row of ship icons along the bottom of the screen in turn order, highest Initiative on the left.
// The hull's shape tells the ship class apart, its fill is the ship's colour and its outline the side it fights for.
// The ship taking orders or carrying them out is raised and outlined in gold; ships done planning are dimmed.
public class InitiativeBar : IDisposable
{
    private enum IconShape
    {
        Triangle,
        Square,
        Circle
    }

    private const int TextureSize = 64;
    private const int IconSize = 48;
    private const int OutlineThickness = 3;
    private const int SlotWidth = 92;
    private const int Padding = 8;
    // Lift of the current ship's icon above the others.
    private const int ActiveLift = 6;
    // The bar sits this far above the bottom of the screen, clear of the action buttons.
    private const int BottomMargin = 76;

    private static readonly Color PlayerOutline = new(90, 220, 110);
    private static readonly Color EnemyOutline = new(230, 60, 50);
    private static readonly Color ActiveOutline = new(240, 200, 60);
    private static readonly Color PanelColor = new Color(10, 15, 25) * 0.65f;
    private static readonly Color HpBackColor = Color.Black * 0.75f;
    private const float DoneAlpha = 0.45f;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteFont _font;
    private readonly Texture2D _pixel;
    private readonly TurnManager _turns;
    private readonly Texture2D[] _shapes;

    public InitiativeBar(GraphicsDevice graphicsDevice, SpriteFont font, Texture2D pixel, TurnManager turns)
    {
        _graphicsDevice = graphicsDevice;
        _font = font;
        _pixel = pixel;
        _turns = turns;
        _shapes = new Texture2D[Enum.GetValues<IconShape>().Length];
        foreach (var shape in Enum.GetValues<IconShape>())
            _shapes[(int)shape] = CreateShapeTexture(graphicsDevice, shape);
    }

    // Draws inside the caller's SpriteBatch.Begin/End.
    public void Draw(SpriteBatch spriteBatch)
    {
        var ships = _turns.Ships;
        var current = _turns.ActiveShip ?? _turns.ResolvingOrder?.Ship;
        int currentIndex = current == null ? -1 : IndexOf(ships, current);

        int count = 0;
        foreach (var ship in ships)
            if (IsShown(ship))
                count++;
        if (count == 0)
            return;

        float scale = 0.75f;
        int textHeight = (int)Math.Ceiling(_font.LineSpacing * scale);
        int slotHeight = ActiveLift + IconSize + 4 + 5 + 3 + textHeight;
        int width = count * SlotWidth + 2 * Padding;
        int height = slotHeight + 2 * Padding;
        var viewport = _graphicsDevice.Viewport;
        int left = (viewport.Width - width) / 2;
        int top = viewport.Height - BottomMargin - height;
        spriteBatch.Draw(_pixel, new Rectangle(left, top, width, height), PanelColor);

        int slot = 0;
        for (int i = 0; i < ships.Count; i++)
        {
            var ship = ships[i];
            if (!IsShown(ship))
                continue;

            bool isCurrent = i == currentIndex;
            // Ships before the current one have already planned (or resolved) this turn.
            bool isDone = ship.IsSunk || (currentIndex >= 0 && i < currentIndex);
            float alpha = isDone ? DoneAlpha : 1f;

            int slotLeft = left + Padding + slot * SlotWidth;
            int iconLeft = slotLeft + (SlotWidth - IconSize) / 2;
            int iconTop = top + Padding + (isCurrent ? 0 : ActiveLift);
            DrawIcon(spriteBatch, ship, new Rectangle(iconLeft, iconTop, IconSize, IconSize), isCurrent, alpha);

            int barTop = top + Padding + ActiveLift + IconSize + 4;
            DrawHpBar(spriteBatch, ship, new Rectangle(iconLeft, barTop, IconSize, 5), alpha);

            string name = Fit(ship.Name, SlotWidth - 6, scale);
            var nameSize = _font.MeasureString(name) * scale;
            var namePosition = new Vector2((int)(slotLeft + (SlotWidth - nameSize.X) / 2f), barTop + 5 + 3);
            spriteBatch.DrawString(_font, name, namePosition, (isCurrent ? ActiveOutline : Color.White) * alpha,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            slot++;
        }
    }

    // Enemies out of sight stay off the bar, like everywhere else in the HUD.
    private bool IsShown(Ship ship)
    {
        return _turns.Vision.CanSee(Side.Player, ship);
    }

    private void DrawIcon(SpriteBatch spriteBatch, Ship ship, Rectangle bounds, bool isCurrent, float alpha)
    {
        var texture = _shapes[(int)ShapeOf(ship.Footprint)];
        var outline = isCurrent ? ActiveOutline : ship.Side == Side.Player ? PlayerOutline : EnemyOutline;
        int thickness = isCurrent ? OutlineThickness + 1 : OutlineThickness;

        // The outline is the same shape drawn larger underneath; shapes are convex and centred, so it rings evenly.
        var inner = bounds;
        inner.Inflate(-thickness, -thickness);
        spriteBatch.Draw(texture, bounds, outline * alpha);
        spriteBatch.Draw(texture, inner, ship.BaseColor * alpha);

        string initiative = ship.Initiative.ToString();
        var size = _font.MeasureString(initiative);
        var center = new Vector2(inner.Center.X, inner.Center.Y + (ShapeOf(ship.Footprint) == IconShape.Triangle ? inner.Height * 0.15f : 0f));
        var position = new Vector2((int)(center.X - size.X / 2f), (int)(center.Y - size.Y / 2f));
        spriteBatch.DrawString(_font, initiative, position + Vector2.One, Color.Black * (0.8f * alpha));
        spriteBatch.DrawString(_font, initiative, position, Color.White * alpha);

        if (ship.IsSunk)
        {
            // A cross over the icon of a ship that went down this turn.
            var c = bounds.Center.ToVector2();
            float length = bounds.Width * 1.1f;
            foreach (float angle in new[] { MathHelper.PiOver4, -MathHelper.PiOver4 })
                spriteBatch.Draw(_pixel, c, null, EnemyOutline, angle, new Vector2(0.5f, 0.5f), new Vector2(length, 4f), SpriteEffects.None, 0f);
        }
    }

    private void DrawHpBar(SpriteBatch spriteBatch, Ship ship, Rectangle bounds, float alpha)
    {
        spriteBatch.Draw(_pixel, bounds, HpBackColor * alpha);
        if (ship.IsSunk)
            return;

        int filled = (int)Math.Ceiling((bounds.Width - 2) * (float)ship.Hp / ship.MaxHp);
        spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 1, bounds.Y + 1, filled, bounds.Height - 2),
            (ship.Side == Side.Player ? PlayerOutline : EnemyOutline) * alpha);
    }

    // One shape per hull class: a slim 1x3 hull is a triangle, a wide 2x3 hull a square, anything else a circle.
    private static IconShape ShapeOf(HexFootprint footprint)
    {
        if (footprint == HexFootprint.Line1x3)
            return IconShape.Triangle;
        if (footprint == HexFootprint.Wide2x3)
            return IconShape.Square;
        return IconShape.Circle;
    }

    // Trims the name with an ellipsis until it fits the slot.
    private string Fit(string text, float maxWidth, float scale)
    {
        if (_font.MeasureString(text).X * scale <= maxWidth)
            return text;

        while (text.Length > 1 && _font.MeasureString(text + "...").X * scale > maxWidth)
            text = text[..^1];
        return text + "...";
    }

    private static int IndexOf(IReadOnlyList<Ship> ships, Ship ship)
    {
        for (int i = 0; i < ships.Count; i++)
            if (ships[i] == ship)
                return i;
        return -1;
    }

    // A white shape on transparent, edges smoothed by sampling each pixel 4x4 times.
    private static Texture2D CreateShapeTexture(GraphicsDevice graphicsDevice, IconShape shape)
    {
        const int samples = 4;
        var data = new Color[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
        for (int x = 0; x < TextureSize; x++)
        {
            int inside = 0;
            for (int sy = 0; sy < samples; sy++)
            for (int sx = 0; sx < samples; sx++)
            {
                // Sample position in [-1, 1] across the texture.
                float u = (x + (sx + 0.5f) / samples) / TextureSize * 2f - 1f;
                float v = (y + (sy + 0.5f) / samples) / TextureSize * 2f - 1f;
                if (Contains(shape, u, v))
                    inside++;
            }

            // Premultiplied white, as SpriteBatch expects by default.
            data[y * TextureSize + x] = Color.White * ((float)inside / (samples * samples));
        }

        var texture = new Texture2D(graphicsDevice, TextureSize, TextureSize);
        texture.SetData(data);
        return texture;
    }

    private static bool Contains(IconShape shape, float u, float v)
    {
        switch (shape)
        {
            case IconShape.Triangle:
                // Point up, like a bow: apex at the top centre, base along the bottom.
                if (v > 1f)
                    return false;
                float halfWidth = (v + 1f) / 2f;
                return Math.Abs(u) <= halfWidth;
            case IconShape.Square:
                return Math.Abs(u) <= 0.85f && Math.Abs(v) <= 0.85f;
            default:
                return u * u + v * v <= 1f;
        }
    }

    public void Dispose()
    {
        foreach (var texture in _shapes)
            texture.Dispose();
    }
}
