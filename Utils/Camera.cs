using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NavalSault.Utils;

public class Camera
{
    private const float PitchDegrees = 55f;
    private const float Distance = 100f;
    private const float PanSpeed = 30f;
    private const float FieldOfViewDegrees = 45f;
    private const float NearPlane = 0.1f;
    private const float FarPlane = 1000f;

    private Vector3 _focusPoint;

    public Matrix View { get; private set; }
    public Matrix Projection { get; private set; }
    public Vector3 Position { get; private set; }
    public Vector3 Forward { get; private set; }
    public Vector3 Up => Vector3.Up;

    public Camera(GraphicsDevice graphicsDevice, Vector3 initialFocus)
    {
        Projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(FieldOfViewDegrees),
            graphicsDevice.Viewport.AspectRatio,
            NearPlane,
            FarPlane);

        _focusPoint = initialFocus;
        RecomputeView();
    }

    public void Update(GameTime gameTime, KeyboardState keyboard)
    {
        Vector2 move = Vector2.Zero;

        if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up))
            move.Y -= 1f;
        if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down))
            move.Y += 1f;
        if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
            move.X -= 1f;
        if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
            move.X += 1f;

        if (move != Vector2.Zero)
        {
            move.Normalize();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _focusPoint.X += move.X * PanSpeed * dt;
            _focusPoint.Z += move.Y * PanSpeed * dt;
            RecomputeView();
        }
    }

    private void RecomputeView()
    {
        float pitchRad = MathHelper.ToRadians(PitchDegrees);
        Vector3 offset = new Vector3(0f, (float)Math.Sin(pitchRad), (float)Math.Cos(pitchRad)) * Distance;
        Position = _focusPoint + offset;
        Forward = Vector3.Normalize(_focusPoint - Position);
        View = Matrix.CreateLookAt(Position, _focusPoint, Up);
    }
}
