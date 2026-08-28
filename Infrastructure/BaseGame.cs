using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Infrastructure.Managers;

namespace Infrastructure
{
    public class BaseGame : Game
    {
        private GraphicsDeviceManager m_Graphics;
        private SpriteBatch m_SpriteBatch;
        private InputManager m_InputManager;
        private CollisionsManager m_CollisionsManager;
        private ScreensMananger m_ScreensMananger;
        private SoundsManager m_SoundManager;

        public InputManager InputManager { get{ return m_InputManager; } }
        public ScreensMananger ScreensMananger { get{ return m_ScreensMananger; } }
        public CollisionsManager CollisionsManager { get { return m_CollisionsManager; } }
        public SoundsManager SoundsManager { get { return m_SoundManager; } }
        public GraphicsDeviceManager GraphicsDeviceManager { get { return m_Graphics; } }

        public const int k_DefaultWindowWidth = 1024;
        public const int k_DefaultWindowHeight = 768;

        public BaseGame()
        {
            m_Graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = k_DefaultWindowWidth,
                PreferredBackBufferHeight = k_DefaultWindowHeight
            };
            m_Graphics.ApplyChanges();

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;

            m_ScreensMananger = new ScreensMananger(this);
            m_CollisionsManager = new CollisionsManager(this);
            m_InputManager = new InputManager(this);
            m_SoundManager = new SoundsManager(this);
        }

        protected override void Initialize()
        {
            if (m_Graphics.PreferredBackBufferWidth != k_DefaultWindowWidth || m_Graphics.PreferredBackBufferHeight != k_DefaultWindowHeight)
            {
                m_Graphics.PreferredBackBufferWidth = k_DefaultWindowWidth;
                m_Graphics.PreferredBackBufferHeight = k_DefaultWindowHeight;
                m_Graphics.ApplyChanges();
            }

            base.Initialize();
        }

        protected override void LoadContent()
        {
            m_SpriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                Exit();
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            
            base.Draw(gameTime);
        }
    }
}
