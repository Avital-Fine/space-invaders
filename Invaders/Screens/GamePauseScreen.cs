using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Infrastructure.ObjectModel;
using Infrastructure.ObjectModel.Screens;

namespace Invaders.Screens
{
    public class GamePauseScreen : GameScreen
    {
        private const string k_FontType = "Consolas";
        private const float k_BlackTintAlpha = 0.4f;
        private readonly Keys r_ResumeTrigger = Keys.R;
        private readonly Headline r_PauseText;
        private readonly Text r_ExitText;

        public GamePauseScreen(Game i_Game) : base(i_Game)
        {
            r_PauseText = new Headline(this, k_FontType, @"Pause");
            r_ExitText = new Text(this, k_FontType, @"Press 'R' to Resume Game");
        }

        public override void Initialize()
        {
            base.Initialize();

            this.IsModal = true;
            this.IsOverlayed = true;
            this.UseGradientBackground = false;
            this.BlackTintAlpha = k_BlackTintAlpha;
            r_ExitText.Centralize();
            Game.Window.ClientSizeChanged += window_ClientSizeChanged;
        }

        private void window_ClientSizeChanged(object sender, System.EventArgs e)
        {
            r_ExitText.Centralize();
        }

        private bool m_EscIsReleased = false;

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (InputManager.KeyboardState.IsKeyUp(Keys.Escape))
            {
                m_EscIsReleased = true;
            }

            if (InputManager.KeyPressed(r_ResumeTrigger) || (m_EscIsReleased && InputManager.KeyPressed(Keys.Escape)))
            {
                m_EscIsReleased = false;
                this.ExitScreen();
            }
        }
    }
}

