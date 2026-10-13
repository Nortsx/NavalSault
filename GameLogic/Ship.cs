using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NavalSault.Utils;

namespace NavalSault.GameLogic;

public class Ship
{
    private const float MoveSpeed = 18f;

    private readonly VertexBuffer _vertexBuffer;
    private readonly IndexBuffer _indexBuffer;
    private readonly int _primitiveCount;

    private readonly Color _baseColor;
    private readonly Color _selectedColor;

    private Vector3 _targetPosition;
    private readonly Queue<(Vector3 Position, HexDirection Facing)> _waypoints = new();

    public Vector3 Position { get; private set; }
    public bool IsSelected { get; set; }
    public bool IsMoving { get; private set; }
    public float FootprintRadius { get; }
    // Bow direction; turns to each leg's direction as the ship moves along a path.
    public HexDirection Facing { get; private set; }

    public int Initiative { get; }
    // Movement radius in hex tiles.
    public int MoveRadius { get; }
    public Weapon Weapon { get; set; }

    public Ship(GraphicsDevice graphicsDevice, Vector3 startPosition, float size, Color baseColor, Color selectedColor,
        HexDirection facing, int initiative, int moveRadius, Weapon weapon)
    {
        Facing = facing;
        Initiative = initiative;
        MoveRadius = moveRadius;
        Weapon = weapon;
        Position = startPosition;
        _targetPosition = startPosition;
        _baseColor = baseColor;
        _selectedColor = selectedColor;
        FootprintRadius = size * 0.75f;

        // Hull is longer along -Z (the bow) than it is wide, so the facing is visible.
        float hw = size * 0.3f;
        float hl = size * 0.7f;
        float height = size * 0.6f;

        Vector3 p000 = new Vector3(-hw, 0f, -hl);
        Vector3 p100 = new Vector3(hw, 0f, -hl);
        Vector3 p110 = new Vector3(hw, 0f, hl);
        Vector3 p010 = new Vector3(-hw, 0f, hl);
        Vector3 p001 = new Vector3(-hw, height, -hl);
        Vector3 p101 = new Vector3(hw, height, -hl);
        Vector3 p111 = new Vector3(hw, height, hl);
        Vector3 p011 = new Vector3(-hw, height, hl);

        var vertices = new List<VertexPositionNormalTexture>();
        var indices = new List<ushort>();

        AddFace(vertices, indices, p001, p101, p111, p011, Vector3.Up);
        AddFace(vertices, indices, p010, p110, p100, p000, Vector3.Down);
        AddFace(vertices, indices, p000, p100, p101, p001, Vector3.Backward);
        AddFace(vertices, indices, p110, p010, p011, p111, Vector3.Forward);
        AddFace(vertices, indices, p010, p000, p001, p011, Vector3.Left);
        AddFace(vertices, indices, p100, p110, p111, p101, Vector3.Right);

        _primitiveCount = indices.Count / 3;

        _vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture), vertices.Count, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices.ToArray());

        _indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, indices.Count, BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices.ToArray());
    }

    private static void AddFace(List<VertexPositionNormalTexture> vertices, List<ushort> indices, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
    {
        ushort baseIndex = (ushort)vertices.Count;

        vertices.Add(new VertexPositionNormalTexture(a, normal, new Vector2(0f, 0f)));
        vertices.Add(new VertexPositionNormalTexture(b, normal, new Vector2(1f, 0f)));
        vertices.Add(new VertexPositionNormalTexture(c, normal, new Vector2(1f, 1f)));
        vertices.Add(new VertexPositionNormalTexture(d, normal, new Vector2(0f, 1f)));

        indices.Add(baseIndex);
        indices.Add((ushort)(baseIndex + 1));
        indices.Add((ushort)(baseIndex + 2));

        indices.Add(baseIndex);
        indices.Add((ushort)(baseIndex + 2));
        indices.Add((ushort)(baseIndex + 3));
    }

    // Each waypoint carries the direction of the leg leading into it.
    public void MoveAlong(IEnumerable<(Vector3 Position, HexDirection Facing)> waypoints)
    {
        _waypoints.Clear();
        foreach (var (position, facing) in waypoints)
            _waypoints.Enqueue((new Vector3(position.X, Position.Y, position.Z), facing));

        StartNextLeg();
    }

    private void StartNextLeg()
    {
        IsMoving = _waypoints.TryDequeue(out var waypoint);
        if (!IsMoving)
            return;

        _targetPosition = waypoint.Position;
        Facing = waypoint.Facing;
    }

    public void Update(GameTime gameTime)
    {
        if (!IsMoving)
            return;

        Vector3 toTarget = _targetPosition - Position;
        float distance = toTarget.Length();
        float step = MoveSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (step >= distance || distance < 0.001f)
        {
            Position = _targetPosition;
            StartNextLeg();
        }
        else
        {
            Position += toTarget / distance * step;
        }
    }

    public void Draw(GraphicsDevice graphicsDevice, BasicEffect effect)
    {
        effect.World = Matrix.CreateRotationY(HexDirections.ToYaw(Facing)) * Matrix.CreateTranslation(Position);
        effect.LightingEnabled = true;
        effect.VertexColorEnabled = false;
        effect.TextureEnabled = false;
        effect.DiffuseColor = (IsSelected ? _selectedColor : _baseColor).ToVector3();

        graphicsDevice.SetVertexBuffer(_vertexBuffer);
        graphicsDevice.Indices = _indexBuffer;

        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _primitiveCount);
        }
    }
}
