using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Infrastructure.ObjectModel;
using Infrastructure.ObjectModel.Screens;

namespace Invaders.Screens
{
    public class WelcomeScreen : GameScreen
    {
        public event Action<eNumberOfPlayers> DefineSettings;

        private const string k_FontType = "Consolas";
        private readonly Keys r_PlayTrigger = Keys.Enter;
        private readonly Keys r_MenuTrigger = Keys.M;
        private readonly Keys r_ExitTrigger = Keys.Escape;
        private readonly MainMenuScreen r_MainMenu;
        private readonly Text r_WelcomeMessage;
        private readonly Text r_InfoMessage;

        public WelcomeScreen(Game i_Game)
            : base (i_Game)
        {
            r_WelcomeMessage = new Text(this, k_FontType, @"Welcome
To Space Invaders");
            r_InfoMessage = new Text(this, k_FontType, @"Press 'Enter' to Start
Press 'M' for Main Menu
Press 'Esc' for Exit");
            r_MainMenu = new MainMenuScreen(i_Game);
        }

        public override void Initialize()
        {
            base.Initialize();
            
            this.BlendState = BlendState.NonPremultiplied;
            r_MainMenu.DefineSettings += onDefineSettings;
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

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (InputManager.KeyPressed(r_PlayTrigger))
            {
                onDefineSettings();
            }

            if (InputManager.KeyPressed(r_MenuTrigger))
            {
                ScreensManager.SetCurrentScreen(r_MainMenu);
            }

            if (InputManager.KeyPressed(r_ExitTrigger))
            {
                Game.Exit();
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
    }
}
