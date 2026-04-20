//*** Guy Ronen � 2008-2011 ***//
using System;
using Infrastructure.Managers;
using Infrastructure.ServiceInterfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Infrastructure.ObjectModel.Screens
{
    public abstract class GameScreen : CompositeDrawableComponent<IGameComponent>
    {
        //CTOR:
        public GameScreen(Game i_Game)
            : base (i_Game)
        {
            this.Enabled = false;
            this.Visible = false;
        }

        protected bool m_IsModal = true;
        public bool IsModal // background screen should not be updated
        {
            get { return m_IsModal; }
            set { m_IsModal = value; }
        }

        protected bool m_IsOverlayed;
        public bool IsOverlayed // background screen should be drawn
        {
            get { return m_IsOverlayed; }
            set { m_IsOverlayed = value; }
        }

        protected GameScreen m_PreviousScreen;
        public GameScreen PreviousScreen // the screen behind me
        {
            get { return m_PreviousScreen; }
            set { m_PreviousScreen = value; }
        }

        protected bool m_HasFocus;
        public bool HasFocus // i should handle the input
        {
            get { return m_HasFocus; }
            set { m_HasFocus = value; }
        }

        private IInputManager m_InputManager;
        private IInputManager m_DummyInputManager = new DummyInputManager();
        public IInputManager InputManager
        {
            get { return this.HasFocus ? m_InputManager : m_DummyInputManager; }
        }

        private SoundsManager m_SoundsManager;
        public SoundsManager SoundsManager
        {
            get { return m_SoundsManager; }
        }

        public override void Initialize()
        {
            m_InputManager = Game.Services.GetService(typeof(IInputManager)) as IInputManager;
            m_SoundsManager = Game.Services.GetService(typeof(SoundsManager)) as SoundsManager;

            if (m_InputManager == null)
            {
                m_InputManager = m_DummyInputManager;
            }

            base.Initialize();
        }

        public void Activate()
        {
            this.Enabled = this.Visible = this.HasFocus = true;

            OnActivated();
        }

        protected virtual void OnActivated()
        {
            if (PreviousScreen != null && this.HasFocus)
            {
                PreviousScreen.HasFocus = false;
            }
        }

        public void Deactivate()
        {
            this.Enabled = this.Visible = this.HasFocus = false;
        }

        public void Close() => ExitScreen();

        protected void ExitScreen()
        {
            Deactivate();
            Dispose();
            OnClosed();
        }

        public event EventHandler Closed;
        protected virtual void OnClosed()
        {
            if (Closed != null)
            {
                Closed.Invoke(this, EventArgs.Empty);
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (PreviousScreen != null && !this.IsModal)
            {
                PreviousScreen.Update(gameTime);
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            if (PreviousScreen != null && IsOverlayed )
            {
                PreviousScreen.Draw(gameTime);

                drawFadedDarkCoverIfNeeded();
            }

            base.Draw(gameTime);
        }

        protected IScreensMananger m_ScreensManager;
        public IScreensMananger ScreensManager
        {
            get { return m_ScreensManager; }
            set { m_ScreensManager = value; }
        }

        #region Faded Background Support

        Texture2D m_GradientTexture;
        Texture2D m_BlankTexture;

        protected float m_BlackTintAlpha = 0;
        public float BlackTintAlpha
        {
            get { return m_BlackTintAlpha; }
            set { m_BlackTintAlpha = value; }
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            m_GradientTexture = this.ContentManager.Load<Texture2D>(@"Screens\gradient");
            m_BlankTexture = this.ContentManager.Load<Texture2D>(@"Screens\blank");
        }

        protected bool m_UseGradientBackground = false;
        public bool UseGradientBackground
        {
            get { return m_UseGradientBackground; }
            set { m_UseGradientBackground = value; }
        }

        public void drawFadedDarkCover(byte i_Alpha)
        {
            Viewport viewport = this.GraphicsDevice.Viewport;
            Texture2D background = UseGradientBackground ? m_GradientTexture : m_BlankTexture;

            SpriteBatch.Begin();
            SpriteBatch.Draw(background, new Rectangle(0, 0, viewport.Width, viewport.Height),
							 new Color(byte.MinValue, byte.MinValue, byte.MinValue, i_Alpha));
            SpriteBatch.End();
        }

        private void drawFadedDarkCoverIfNeeded()
        {
            if (BlackTintAlpha > 0 || UseGradientBackground)
            {
                drawFadedDarkCover((byte)(m_BlackTintAlpha * byte.MaxValue));
            }
        }
        #endregion Faded Background Support
    }
}
