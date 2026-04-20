using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Infrastructure.ObjectModel;
using Infrastructure.ObjectModel.Screens;
using Infrastructure.Managers;

namespace Invaders.Screens
{
    public class WelcomeScreen : GameScreen
    {
        public event Action<eNumberOfPlayers> DefineSettings;
        public event Action BackToDashboard;

        private const string k_FontType = "Consolas";
        private readonly Keys r_PlayTrigger = Keys.Enter;
        private readonly Keys r_MenuTrigger = Keys.M;
        private readonly Keys r_BackTrigger = Keys.Escape;
        private readonly MainMenuScreen r_MainMenu;
        private readonly Text r_WelcomeMessage;
        private readonly Text r_InfoMessage;
        private bool m_JustActivated;

        public WelcomeScreen(Game i_Game)
            : base (i_Game)
        {
            r_WelcomeMessage = new Text(this, k_FontType, @"Welcome
To Space Invaders");
            r_InfoMessage = new Text(this, k_FontType, @"Press 'Enter' to Start
Press 'M' for Main Menu
Press 'Esc' to return to Dashboard");
            r_MainMenu = new MainMenuScreen(i_Game);
        }

        public override void Initialize()
        {
            base.Initialize();

            this.BlendState = BlendState.NonPremultiplied;
            r_MainMenu.DefineSettings  += onDefineSettings;
            r_MainMenu.BackToDashboard += onMainMenuBackToDashboard;
            setWelcomeMsg();
            setInfoMsg();
        }

        private void setWelcomeMsg()
        {
            r_WelcomeMessage.TintColor = Color.CadetBlue;
            r_WelcomeMessage.Scales = new Vector2(2, 3);
            r_WelcomeMessage.Position = CenterOfViewPort - new Vector2(r_WelcomeMessage.Width, r_WelcomeMessage.Height);
        }

        private void setInfoMsg()
        {
            r_InfoMessage.TintColor = Color.Black;
            r_InfoMessage.Position = r_WelcomeMessage.Position + new Vector2(0, (float)(r_WelcomeMessage.Height * 1.5));
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            m_JustActivated = true;
        }

        public override void Update(GameTime gameTime)
        {
            if (m_JustActivated)
            {
                m_JustActivated = false;
                return;
            }

            base.Update(gameTime);

            if (InputManager.KeyPressed(r_PlayTrigger))
            {
                onDefineSettings();
            }

            if (InputManager.KeyPressed(r_MenuTrigger))
            {
                ScreensManager.SetCurrentScreen(r_MainMenu);
            }

            if (InputManager.KeyPressed(r_BackTrigger))
            {
                ExitScreen();
                BackToDashboard?.Invoke();
            }
        }

        private void onDefineSettings()
        {
            ExitScreen();

            if (DefineSettings != null)
            {
                DefineSettings.Invoke(r_MainMenu.NumberOfPlayers);
            }
        }

        private void onMainMenuBackToDashboard()
        {
            ExitScreen();
            BackToDashboard?.Invoke();
        }
    }
}
