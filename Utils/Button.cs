using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NavalSault.Utils;

// A clickable rectangle with a label, drawn in screen space with a SpriteBatch.
public class Button
{
    private static readonly Color FillColor = new(20, 30, 45, 200);
    private static readonly Color HoverFillColor = new(45, 65, 90, 220);
    private static readonly Color SelectedFillColor = new(190, 150, 30, 230);
    private static readonly Color BorderColor = new(230, 230, 230);
    private static readonly Color TextColor = Color.White;
    private static readonly Color DisabledColor = new(120, 120, 120);
    private const int BorderThickness = 2;

    public string Label { get; set; }
    public Rectangle Bounds { get; set; }
    // A disabled button is drawn greyed out and ignores clicks.
    public bool IsEnabled { get; set; } = true;
    // Drawn highlighted, e.g. the currently chosen mode.
    public bool IsSelected { get; set; }

    public Button(string label)
    {
        Label = label;
    }

    public bool Contains(Point point)
    {
        return Bounds.Contains(point);
    }

    // Pixel is a 1x1 white texture used to draw the rectangles.
    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Point mousePosition)
    {
        var fill = !IsEnabled ? FillColor
            : IsSelected ? SelectedFillColor
            : Contains(mousePosition) ? HoverFillColor
            : FillColor;
        var foreground = IsEnabled ? TextColor : DisabledColor;
        var border = IsEnabled ? BorderColor : DisabledColor;

        spriteBatch.Draw(pixel, Bounds, fill);

        var b = Bounds;
        spriteBatch.Draw(pixel, new Rectangle(b.Left, b.Top, b.Width, BorderThickness), border);
        spriteBatch.Draw(pixel, new Rectangle(b.Left, b.Bottom - BorderThickness, b.Width, BorderThickness), border);
        spriteBatch.Draw(pixel, new Rectangle(b.Left, b.Top, BorderThickness, b.Height), border);
        spriteBatch.Draw(pixel, new Rectangle(b.Right - BorderThickness, b.Top, BorderThickness, b.Height), border);

        var textSize = font.MeasureString(Label);
        var textPosition = new Vector2(b.Center.X - textSize.X / 2f, b.Center.Y - textSize.Y / 2f);
        spriteBatch.DrawString(font, Label, new Vector2((int)textPosition.X, (int)textPosition.Y), foreground);
    }
}
