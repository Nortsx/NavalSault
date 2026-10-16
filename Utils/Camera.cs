using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NavalSault.Utils;

public class Camera
{
    private const float PitchDegrees = 55f;
    private const float DefaultDistance = 100f;
    private const float MinDistance = 35f;
    private const float MaxDistance = 170f;
    // Each wheel notch (120 units) moves the camera this share of its distance closer or further.
    private const float ZoomPerNotch = 0.12f;
    // How quickly the camera eases towards the zoom it was asked for; higher is snappier.
    private const float ZoomSmoothing = 12f;
    // Pan speed at the default distance; it scales with distance so panning feels the same at any zoom.
    private const float PanSpeed = 30f;
    private const float FieldOfViewDegrees = 45f;
    private const float NearPlane = 0.1f;
    private const float FarPlane = 1000f;

    private Vector3 _focusPoint;
    // The point the camera looks at stays within this circle on the XZ plane, if one is set.
    private Vector2 _boundsCenter;
    private float? _boundsRadius;
    private float _distance = DefaultDistance;
    private float _targetDistance = DefaultDistance;

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

    // Keeps panning within radius of center, e.g. the map, so the camera can't wander off into empty sea.
    public void SetPanBounds(Vector3 center, float radius)
    {
        _boundsCenter = new Vector2(center.X, center.Z);
        _boundsRadius = radius;
        ClampFocus();
        RecomputeView();
    }

    private void ClampFocus()
    {
        if (_boundsRadius == null)
            return;

        var offset = new Vector2(_focusPoint.X, _focusPoint.Z) - _boundsCenter;
        if (offset.Length() <= _boundsRadius.Value)
            return;

        offset = Vector2.Normalize(offset) * _boundsRadius.Value;
        _focusPoint.X = _boundsCenter.X + offset.X;
        _focusPoint.Z = _boundsCenter.Y + offset.Y;
    }

    // scrollDelta is the change in the mouse wheel value since last frame; positive zooms in.
    public void Update(GameTime gameTime, KeyboardState keyboard, int scrollDelta)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        bool changed = false;

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
            float speed = PanSpeed * _distance / DefaultDistance;
            _focusPoint.X += move.X * speed * dt;
            _focusPoint.Z += move.Y * speed * dt;
            ClampFocus();
            changed = true;
        }

        if (scrollDelta != 0)
        {
            float notches = scrollDelta / 120f;
            _targetDistance = MathHelper.Clamp(_targetDistance * MathF.Pow(1f - ZoomPerNotch, notches), MinDistance, MaxDistance);
        }

        if (Math.Abs(_targetDistance - _distance) > 0.01f)
        {
            _distance = MathHelper.Lerp(_distance, _targetDistance, 1f - MathF.Exp(-ZoomSmoothing * dt));
            changed = true;
        }

        if (changed)
            RecomputeView();
    }

    private void RecomputeView()
    {
        float pitchRad = MathHelper.ToRadians(PitchDegrees);
        Vector3 offset = new Vector3(0f, (float)Math.Sin(pitchRad), (float)Math.Cos(pitchRad)) * _distance;
        Position = _focusPoint + offset;
        Forward = Vector3.Normalize(_focusPoint - Position);
        View = Matrix.CreateLookAt(Position, _focusPoint, Up);
    }
}
