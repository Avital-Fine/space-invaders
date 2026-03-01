using System;
using System.IO;
using System.Text.Json;
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

        private const string k_FontType = "Consolas";
        private readonly Keys r_PlayTrigger = Keys.Enter;
        private readonly Keys r_MenuTrigger = Keys.M;
        private readonly Keys r_FacebookTrigger = Keys.F;
        private readonly Keys r_ExitTrigger = Keys.Escape;
        private readonly MainMenuScreen r_MainMenu;
        private readonly Text r_WelcomeMessage;
        private readonly Text r_InfoMessage;
        private readonly Text r_LoginStatus;
        private readonly FacebookManager r_FacebookManager;

        public WelcomeScreen(Game i_Game)
            : base (i_Game)
        {
            r_WelcomeMessage = new Text(this, k_FontType, @"Welcome
To Space Invaders");
            r_InfoMessage = new Text(this, k_FontType, @"Press 'Enter' to Start
Press 'M' for Main Menu
Press 'F' for Facebook Login
Press 'Esc' for Exit");
            r_LoginStatus = new Text(this, k_FontType, string.Empty);
            r_MainMenu = new MainMenuScreen(i_Game);
            r_FacebookManager = new FacebookManager(loadAppId());
        }

        private static string loadAppId()
        {
            string configPath = "appconfig.json";
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                using JsonDocument doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("Facebook").GetProperty("AppId").GetString();
            }
            return string.Empty;
        }

        public override void Initialize()
        {
            base.Initialize();
            
            this.BlendState = BlendState.NonPremultiplied;
            r_MainMenu.DefineSettings += onDefineSettings;
            setWelcomeMsg();
            setInfoMsg();
            setLoginStatusMsg();
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

        private void setLoginStatusMsg()
        {
            r_LoginStatus.TintColor = Color.DarkBlue;
            r_LoginStatus.Position = r_InfoMessage.Position + new Vector2(0, r_InfoMessage.Height + 20);
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

            if (InputManager.KeyPressed(r_FacebookTrigger))
            {
                r_FacebookManager.Login();
                if (r_FacebookManager.UserName != null)
                {
                    r_LoginStatus.Content = $"Logged in as: {r_FacebookManager.UserName}";
                    // Pass the name to PlayersManager if needed
                    Invaders.Managers.PlayersManager.SetPlayerNames(new string[] { r_FacebookManager.UserName });
                }
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
