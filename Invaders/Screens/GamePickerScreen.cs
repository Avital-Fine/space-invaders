using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Infrastructure.ObjectModel;
using Infrastructure.ObjectModel.Screens;
using Infrastructure.Managers;

namespace Invaders.Screens
{
    public class GamePickerScreen : MenuScreen
    {
        public event Action SpaceInvadersSelected;

        private const string k_Title    = "Game Dashboard";
        private const string k_FontType = "Consolas";

        // Colors for the dark dashboard theme
        private static readonly Color s_TitleColor       = Color.Gold;
        private static readonly Color s_TextActive       = Color.White;
        private static readonly Color s_TextDimmed       = new Color(160, 160, 180);
        private static readonly Color s_BtnSpaceInvaders = new Color(30, 140, 50);   // arcade green
        private static readonly Color s_BtnFacebook      = new Color(24, 119, 242);  // Facebook blue
        private static readonly Color s_BtnDimmed        = new Color(45, 45, 70);    // muted navy — coming soon
        private static readonly Color s_BtnQuit          = new Color(140, 35, 35);   // dark red

        private readonly FacebookManager r_FacebookManager;
        private readonly MenuItem r_SpaceInvaders;
        private readonly MenuItem r_Tetris;
        private readonly MenuItem r_Snake;
        private readonly MenuItem r_PacMan;
        private readonly MenuItem r_FacebookLogin;
        private readonly MenuItem r_Quit;
        private readonly Text r_LoginStatus;

        public GamePickerScreen(Game i_Game) : base(i_Game, k_Title)
        {
            r_SpaceInvaders = new MenuItem(this, "Space Invaders");
            r_Tetris        = new MenuItem(this, "Tetris          [Coming Soon]");
            r_Snake         = new MenuItem(this, "Snake           [Coming Soon]");
            r_PacMan        = new MenuItem(this, "Pac-Man         [Coming Soon]");
            r_FacebookLogin = new MenuItem(this, "Sign in with Facebook");
            r_Quit          = new MenuItem(this, "Quit");
            r_LoginStatus   = new Text(this, k_FontType, string.Empty);
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

            // Style the title
            r_Title.TintColor = s_TitleColor;

            // Space Invaders — available, vibrant green
            r_SpaceInvaders.ButtonTintColor = s_BtnSpaceInvaders;
            r_SpaceInvaders.TextColor       = s_TextActive;
            r_SpaceInvaders.Clicked        += spaceInvaders_Clicked;

            // Placeholder games — dimmed to signal unavailability
            styleComingSoon(r_Tetris);
            styleComingSoon(r_Snake);
            styleComingSoon(r_PacMan);

            // Facebook login — blue
            r_FacebookLogin.ButtonTintColor = s_BtnFacebook;
            r_FacebookLogin.TextColor       = s_TextActive;
            r_FacebookLogin.Clicked        += facebookLogin_Clicked;

            // Quit — dark red
            r_Quit.ButtonTintColor = s_BtnQuit;
            r_Quit.TextColor       = s_TextActive;
            r_Quit.Clicked        += () => Game.Exit();

            // Login status — shown below the title
            r_LoginStatus.TintColor = new Color(180, 210, 255);
            r_LoginStatus.Position  = new Vector2(
                r_Title.Position.X,
                r_Title.Position.Y + r_Title.Height + 6);
        }

        private void styleComingSoon(MenuItem i_Item)
        {
            i_Item.ButtonTintColor = s_BtnDimmed;
            i_Item.TextColor       = s_TextDimmed;
            i_Item.Clicked        += comingSoon_Clicked;
        }

        private void spaceInvaders_Clicked()
        {
            SpaceInvadersSelected?.Invoke();
        }

        private void comingSoon_Clicked()
        {
            ScreensManager.SetCurrentScreen(new ComingSoonScreen(Game));
        }

        private void facebookLogin_Clicked()
        {
            r_FacebookManager.Login();
            if (r_FacebookManager.UserName != null)
            {
                r_LoginStatus.Content = $"Signed in as: {r_FacebookManager.UserName}";
                Invaders.Managers.PlayersManager.SetPlayerNames(new string[] { r_FacebookManager.UserName });
            }
        }
    }
}
