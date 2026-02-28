using Microsoft.Xna.Framework;
using Infrastructure.ObjectModel;
using Infrastructure.ObjectModel.Screens;

namespace Invaders.Screens
{
    public class LevelTransitionScreen : GameScreen
    {
        private const string k_FontType = "Consolas";
        private const float k_OneSecond = 1f;
        private const float k_HalfSecond = 0.5f;
        private readonly Text r_CounterText;
        private readonly Headline r_LevelText;
        private float m_TimeCounter;
        private int m_Counter;

        public LevelTransitionScreen(Game i_Game, int i_Level) : base (i_Game)
        {
            r_LevelText = new Headline(this, k_FontType, "Level " + i_Level.ToString());
            r_CounterText = new Text(this, k_FontType);
        }

        public override void Initialize()
        {
            base.Initialize();

            r_CounterText.Scales = new Vector2(2, 2);
            r_CounterText.Centralize();
            Game.Window.ClientSizeChanged += window_ClientSizeChanged;
        }

        private void window_ClientSizeChanged(object sender, System.EventArgs e)
        {
            r_CounterText.Centralize();
        }

        public override void Update(GameTime i_GameTime)
        {
            base.Update(i_GameTime);

            m_TimeCounter += (float)i_GameTime.ElapsedGameTime.TotalSeconds;

            if (m_TimeCounter >= k_OneSecond)
            {
                m_TimeCounter -= k_OneSecond;
                m_Counter--;
                r_CounterText.Content = m_Counter.ToString();
            }

            if (m_Counter == 1 && m_TimeCounter >= k_HalfSecond)
            {
                ExitScreen();
            }
        }

        public void RestartLevel(int i_Level)
        {
            r_LevelText.Content = "Level " + i_Level.ToString();
            m_Counter = 3;
            r_CounterText.Content = m_Counter.ToString();
            m_TimeCounter = 0f;
        }
    }
}
