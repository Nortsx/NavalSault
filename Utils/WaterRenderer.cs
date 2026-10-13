using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NavalSault.Utils;

// Owns everything the water shader needs: the wave mesh, reflection/refraction targets and noise textures.
// Each frame: PrepareTargets (off-screen passes) first, then Draw once the back buffer is set.
public class WaterRenderer
{
    private const float WaterHeight = 0f;
    private const float WaveSpeed = 0.03f;
    private static readonly Color DeepWaterColor = new(8, 42, 66);

    private readonly GraphicsDevice _graphicsDevice;
    private readonly Effect _effect;
    private readonly WaterPlane _plane;
    private readonly RenderTarget2D _reflectionTarget;
    private readonly RenderTarget2D _refractionTarget;
    private readonly Texture2D _distortionMap;
    private readonly Texture2D _normalMap;
    private readonly Color _skyColor;
    private readonly Vector3 _lightDirection;

    private Matrix _reflectionView;

    public WaterRenderer(GraphicsDevice graphicsDevice, Effect waterEffect, Color skyColor, Vector3 lightDirection)
    {
        _graphicsDevice = graphicsDevice;
        _effect = waterEffect;
        _skyColor = skyColor;
        _lightDirection = lightDirection;

        _plane = new WaterPlane(graphicsDevice, width: 360f, depth: 360f, segmentsX: 90, segmentsZ: 90);

        var pp = graphicsDevice.PresentationParameters;
        _reflectionTarget = new RenderTarget2D(graphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
        _refractionTarget = new RenderTarget2D(graphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
        _distortionMap = WaterTextures.CreateDistortionMap(graphicsDevice, seed: 1);
        _normalMap = WaterTextures.CreateNormalMap(graphicsDevice, seed: 2);
    }

    // Renders the reflection and refraction targets. drawReflected is called with the mirrored
    // view matrix and should draw whatever the water reflects.
    public void PrepareTargets(Camera camera, Action<Matrix> drawReflected)
    {
        DrawRefraction();
        DrawReflection(camera, drawReflected);
    }

    // Nothing in the scene sits below the waterline yet, so the refraction target is just the deep-water color.
    private void DrawRefraction()
    {
        _graphicsDevice.SetRenderTarget(_refractionTarget);
        _graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, DeepWaterColor, 1f, 0);
        _graphicsDevice.SetRenderTarget(null);
    }

    private void DrawReflection(Camera camera, Action<Matrix> drawReflected)
    {
        var planeNormal = Vector3.Up;
        var planePosition = new Vector3(0f, WaterHeight, 0f);
        float projLength = Vector3.Dot(planeNormal, camera.Position - planePosition);
        var reflectionCamPos = camera.Position - 2f * planeNormal * projLength;
        var reflectionCamForward = Vector3.Reflect(camera.Forward, planeNormal);
        var reflectionCamUp = Vector3.Reflect(camera.Up, planeNormal);
        _reflectionView = Matrix.CreateLookAt(reflectionCamPos, reflectionCamPos + reflectionCamForward, reflectionCamUp);

        _graphicsDevice.SetRenderTarget(_reflectionTarget);
        _graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, _skyColor, 1f, 0);
        drawReflected(_reflectionView);
        _graphicsDevice.SetRenderTarget(null);
    }

    public void Draw(Camera camera, float time)
    {
        _plane.UpdateWaves(time);

        var world = Matrix.Identity;
        var p = _effect.Parameters;

        p["World"].SetValue(world);
        p["WorldViewProjection"].SetValue(world * camera.View * camera.Projection);
        p["ReflectionView"].SetValue(_reflectionView);
        p["Projection"].SetValue(camera.Projection);

        p["ReflectionTexture"].SetValue(_reflectionTarget);
        p["RefractionTexture"].SetValue(_refractionTarget);
        p["DistortionMap"].SetValue(_distortionMap);
        p["NormalMap"].SetValue(_normalMap);
        p["Tiling"].SetValue(Vector2.One * 12f);

        p["MoveFactor"].SetValue(WaveSpeed * time % 1f);
        p["WaveStrength"].SetValue(0.01f);
        p["FresnelPower"].SetValue(2f);

        p["CameraPosition"].SetValue(camera.Position);
        p["LightPosition"].SetValue(-_lightDirection * 10000f);
        p["LightColor"].SetValue(Color.White.ToVector3());
        p["Shininess"].SetValue(25f);
        p["KSpecular"].SetValue(0.3f);

        _effect.CurrentTechnique = _effect.Techniques["Water"];
        _plane.Draw(_graphicsDevice, _effect);
    }
}
