using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NavalSault;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private BasicEffect _effect;
    private Camera _camera;
    private WaterPlane _waterPlane;
    private HexGrid _hexGrid;

    private SpriteBatch _spriteBatch;
    private SpriteFont _font;
    private int _frameCount;
    private double _fpsTimer;
    private double _fps;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
        _graphics.GraphicsProfile = GraphicsProfile.Reach;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _camera = new Camera(GraphicsDevice, Vector3.Zero);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _effect = new BasicEffect(GraphicsDevice)
        {
            LightingEnabled = true,
            AmbientLightColor = new Vector3(0.35f, 0.35f, 0.4f),
            SpecularColor = Vector3.Zero,
            TextureEnabled = false,
            VertexColorEnabled = false
        };
        _effect.DirectionalLight0.Enabled = true;
        _effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(0.4f, -1f, 0.3f));
        _effect.DirectionalLight0.DiffuseColor = new Vector3(1f, 1f, 0.95f);
        _effect.DirectionalLight0.SpecularColor = Vector3.Zero;
        _effect.DirectionalLight1.Enabled = false;
        _effect.DirectionalLight2.Enabled = false;

        _waterPlane = new WaterPlane(GraphicsDevice, width: 360f, depth: 360f, segmentsX: 90, segmentsZ: 90);
        _hexGrid = new HexGrid(GraphicsDevice, hexRadius: 8f, gridRadius: 4, tileY: 1.3f, color: new Color(0, 255, 128));

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("DefaultFont");
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
            Exit();

        _camera.Update(gameTime, keyboard);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _frameCount++;
        _fpsTimer += gameTime.ElapsedGameTime.TotalSeconds;
        if (_fpsTimer >= 1.0)
        {
            _fps = _frameCount / _fpsTimer;
            _frameCount = 0;
            _fpsTimer = 0;
        }

        GraphicsDevice.Clear(Color.CornflowerBlue);

        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;

        _waterPlane.UpdateWaves((float)gameTime.TotalGameTime.TotalSeconds);

        _effect.World = Matrix.Identity;
        _effect.View = _camera.View;
        _effect.Projection = _camera.Projection;

        _effect.DiffuseColor = new Color(25, 70, 110).ToVector3();
        _waterPlane.Draw(GraphicsDevice, _effect);

        _hexGrid.Draw(GraphicsDevice, _effect);

        _spriteBatch.Begin();
        _spriteBatch.DrawString(_font, $"FPS: {_fps:0}", new Vector2(10, 10), Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
