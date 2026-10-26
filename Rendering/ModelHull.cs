using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NavalSault.Rendering;

// A ship drawn with a model from the content pipeline, fitted onto its footprint and lit like the rest of the scene.
// The model belongs to the content manager that loaded it, so the hull has nothing of its own to dispose.
public class ModelHull : ShipHull
{
    // What every hull drawn with the same model shares, worked out once when it's first used. The content manager hands
    // every ship the same model, and its effects with it, so materials are kept here before any ship tints them.
    private class ModelInfo
    {
        public Matrix[] BoneTransforms;
        public BoundingBox Bounds;
        public Dictionary<BasicEffect, Vector3> MaterialColors;
    }

    private static readonly ConditionalWeakTable<Model, ModelInfo> Infos = new();

    private readonly Model _model;
    private readonly ModelInfo _info;
    private readonly ShipModel _definition;
    // Takes the model from the space it was made in onto the footprint.
    private readonly Matrix _fit;

    public ModelHull(Model model, ShipModel definition, BoundingBox footprintBounds)
    {
        _model = model;
        _definition = definition;
        _info = Infos.GetValue(model, Measure);

        // Turn the bow to -Z, then scale evenly until the model fills the footprint's length or width, whichever is
        // reached first, and slide it over the footprint's centre. The waterline stays at Y = 0.
        var yaw = Matrix.CreateRotationY(MathHelper.ToRadians(definition.YawDegrees));
        var turned = BoundingBox.CreateFromPoints(_info.Bounds.GetCorners().Select(c => Vector3.Transform(c, yaw)));
        Vector3 size = turned.Max - turned.Min, room = footprintBounds.Max - footprintBounds.Min;
        float scale = Math.Min(room.X / size.X, room.Z / size.Z);

        Vector3 modelCenter = (turned.Min + turned.Max) / 2f * scale;
        Vector3 footprintCenter = (footprintBounds.Min + footprintBounds.Max) / 2f;
        _fit = yaw * Matrix.CreateScale(scale)
               * Matrix.CreateTranslation(footprintCenter.X - modelCenter.X, 0f, footprintCenter.Z - modelCenter.Z);
    }

    private static ModelInfo Measure(Model model)
    {
        var bones = new Matrix[model.Bones.Count];
        model.CopyAbsoluteBoneTransformsTo(bones);

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        var materials = new Dictionary<BasicEffect, Vector3>();
        foreach (var mesh in model.Meshes)
        {
            foreach (var part in mesh.MeshParts)
            {
                if (part.Effect is BasicEffect effect)
                    materials.TryAdd(effect, effect.DiffuseColor);

                var declaration = part.VertexBuffer.VertexDeclaration;
                var position = declaration.GetVertexElements().First(e => e.VertexElementUsage == VertexElementUsage.Position);
                var positions = new Vector3[part.NumVertices];
                part.VertexBuffer.GetData(part.VertexOffset * declaration.VertexStride + position.Offset, positions, 0,
                    part.NumVertices, declaration.VertexStride);

                foreach (var p in positions)
                {
                    var transformed = Vector3.Transform(p, bones[mesh.ParentBone.Index]);
                    min = Vector3.Min(min, transformed);
                    max = Vector3.Max(max, transformed);
                }
            }
        }

        return new ModelInfo { BoneTransforms = bones, Bounds = new BoundingBox(min, max), MaterialColors = materials };
    }

    // Takes the camera and the lights from the scene's effect, so the model sits in the same light as everything else.
    public override void Draw(GraphicsDevice graphicsDevice, BasicEffect effect, Matrix world, Color color)
    {
        var tint = color.ToVector3();
        foreach (var mesh in _model.Meshes)
        {
            bool tinted = _definition.TintedMeshes == null || _definition.TintedMeshes.Contains(mesh.Name);
            foreach (var part in mesh.MeshParts)
            {
                if (part.Effect is not BasicEffect partEffect)
                    continue;

                partEffect.World = _info.BoneTransforms[mesh.ParentBone.Index] * _fit * world;
                partEffect.View = effect.View;
                partEffect.Projection = effect.Projection;
                partEffect.LightingEnabled = true;
                partEffect.AmbientLightColor = effect.AmbientLightColor;
                partEffect.SpecularColor = effect.SpecularColor;
                CopyLight(effect.DirectionalLight0, partEffect.DirectionalLight0);
                CopyLight(effect.DirectionalLight1, partEffect.DirectionalLight1);
                CopyLight(effect.DirectionalLight2, partEffect.DirectionalLight2);

                var material = _info.MaterialColors[partEffect];
                partEffect.DiffuseColor = tinted ? material * tint : material;
            }
            mesh.Draw();
        }
    }

    private static void CopyLight(DirectionalLight from, DirectionalLight to)
    {
        to.Enabled = from.Enabled;
        to.Direction = from.Direction;
        to.DiffuseColor = from.DiffuseColor;
        to.SpecularColor = from.SpecularColor;
    }
}
