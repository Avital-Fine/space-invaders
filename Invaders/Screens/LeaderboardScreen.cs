using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Infrastructure.ObjectModel.Screens;
using Infrastructure.Managers;

namespace Invaders.Screens
{
    public class LeaderboardScreen : GameScreen
    {
        private const string k_FontType = "Consolas";
        private const int k_TopScoresCount = 8;

        private readonly ScoresDatabase r_ScoresDatabase;
        private SpriteFont m_Font;
        private Texture2D m_PixelTexture;

        private int m_CurrentTab = 0; // 0 = All, 1 = Tetris, 2 = Space Invaders
        private readonly string[] r_Tabs = new string[] { "ALL GAMES", "TETRIS", "SPACE INVADERS" };

        private KeyboardState m_CurrKeyboard;
        private KeyboardState m_PrevKeyboard;
        private GamePadState m_CurrGamePad;
        private GamePadState m_PrevGamePad;

        public LeaderboardScreen(Game i_Game, int i_InitialTab = 0) : base(i_Game)
        {
            r_ScoresDatabase = new ScoresDatabase();
            m_CurrentTab = Math.Clamp(i_InitialTab, 0, r_Tabs.Length - 1);
        }

        public override void Initialize()
        {
            base.Initialize();
            this.BlendState = BlendState.AlphaBlend;

            m_CurrKeyboard = m_PrevKeyboard = Keyboard.GetState();
            m_CurrGamePad = m_PrevGamePad = GamePad.GetState(PlayerIndex.One);
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            m_Font = Game.Content.Load<SpriteFont>($"Fonts/{k_FontType}");
            m_PixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            m_PixelTexture.SetData(new[] { Color.White });
        }

        public override void Update(GameTime i_GameTime)
        {
            base.Update(i_GameTime);

            m_PrevKeyboard = m_CurrKeyboard;
            m_CurrKeyboard = Keyboard.GetState();
            m_PrevGamePad = m_CurrGamePad;
            m_CurrGamePad = GamePad.GetState(PlayerIndex.One);

            // Exit back to previous screen
            if (isPressed(Keys.Escape) || isPressed(Keys.Back) || isPadPressed(Buttons.Back) || isPadPressed(Buttons.B))
            {
                ExitScreen();
                return;
            }

            // Tab navigation
            if (isPressed(Keys.Left) || isPressed(Keys.A) || isPadPressed(Buttons.DPadLeft) || isPadPressed(Buttons.LeftShoulder))
            {
                m_CurrentTab = (m_CurrentTab + r_Tabs.Length - 1) % r_Tabs.Length;
            }
            else if (isPressed(Keys.Right) || isPressed(Keys.D) || isPressed(Keys.Tab) || isPadPressed(Buttons.DPadRight) || isPadPressed(Buttons.RightShoulder))
            {
                m_CurrentTab = (m_CurrentTab + 1) % r_Tabs.Length;
            }
        }

        private bool isPressed(Keys i_Key)
        {
            bool direct = m_CurrKeyboard.IsKeyDown(i_Key) && m_PrevKeyboard.IsKeyUp(i_Key);
            bool viaManager = InputManager != null && InputManager.KeyPressed(i_Key);
            return direct || viaManager;
        }

        private bool isPadPressed(Buttons i_Button)
        {
            return m_CurrGamePad.IsButtonDown(i_Button) && m_PrevGamePad.IsButtonUp(i_Button);
        }

        public override void Draw(GameTime i_GameTime)
        {
            base.Draw(i_GameTime);

            Viewport vp = GraphicsDevice.Viewport;

            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // Background dark overlay
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(0, 0, vp.Width, vp.Height), new Color(8, 10, 18, 245));

            int cardWidth = Math.Min(720, vp.Width - 40);
            int cardHeight = Math.Min(520, vp.Height - 40);
            int cardX = (vp.Width - cardWidth) / 2;
            int cardY = (vp.Height - cardHeight) / 2;

            Rectangle cardRect = new Rectangle(cardX, cardY, cardWidth, cardHeight);
            SpriteBatch.Draw(m_PixelTexture, cardRect, new Color(18, 22, 38, 250));
            drawRectOutline(cardRect, new Color(60, 80, 140, 220), 2);

            // Title
            string title = "ARCADE HALL OF FAME";
            Vector2 titleSize = m_Font.MeasureString(title) * 1.15f;
            SpriteBatch.DrawString(m_Font, title, new Vector2(cardX + (cardWidth - titleSize.X) / 2, cardY + 20), Color.Gold, 0f, Vector2.Zero, 1.15f, SpriteEffects.None, 0f);

            // Tabs bar
            drawTabs(cardX, cardY + 65, cardWidth);

            // Scores table
            drawScoresTable(cardX + 25, cardY + 120, cardWidth - 50);

            // Footer hint
            string footer = "< / > or Tab : Switch Category   |   [Esc] Back";
            Vector2 footerSize = m_Font.MeasureString(footer) * 0.65f;
            SpriteBatch.DrawString(m_Font, footer, new Vector2(cardX + (cardWidth - footerSize.X) / 2, cardY + cardHeight - 30), new Color(140, 155, 180), 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);

            SpriteBatch.End();
        }

        private void drawTabs(int i_CardX, int i_Y, int i_CardWidth)
        {
            int tabWidth = 180;
            int totalTabsWidth = r_Tabs.Length * tabWidth + (r_Tabs.Length - 1) * 15;
            int startX = i_CardX + (i_CardWidth - totalTabsWidth) / 2;

            for (int i = 0; i < r_Tabs.Length; i++)
            {
                bool isSelected = (i == m_CurrentTab);
                Rectangle tabRect = new Rectangle(startX + i * (tabWidth + 15), i_Y, tabWidth, 32);

                Color tabBg = isSelected ? new Color(40, 70, 140, 240) : new Color(25, 30, 50, 200);
                Color tabBorder = isSelected ? Color.Gold : new Color(50, 60, 90, 180);
                Color textColor = isSelected ? Color.Gold : new Color(170, 180, 205);

                SpriteBatch.Draw(m_PixelTexture, tabRect, tabBg);
                drawRectOutline(tabRect, tabBorder, isSelected ? 2 : 1);

                Vector2 textSize = m_Font.MeasureString(r_Tabs[i]) * 0.68f;
                Vector2 textPos = new Vector2(tabRect.X + (tabRect.Width - textSize.X) / 2, tabRect.Y + (tabRect.Height - textSize.Y) / 2);
                SpriteBatch.DrawString(m_Font, r_Tabs[i], textPos, textColor, 0f, Vector2.Zero, 0.68f, SpriteEffects.None, 0f);
            }
        }

        private void drawScoresTable(int i_X, int i_Y, int i_Width)
        {
            // Table Header
            Rectangle headerRect = new Rectangle(i_X, i_Y, i_Width, 28);
            SpriteBatch.Draw(m_PixelTexture, headerRect, new Color(30, 36, 60, 230));
            drawRectOutline(headerRect, new Color(50, 65, 100, 180), 1);

            int colRank = i_X + 15;
            int colName = i_X + 80;
            int colGame = i_X + 240;
            int colScore = i_X + 410;
            int colLevel = i_X + 510;
            int colDate = i_X + 585;

            float headerScale = 0.65f;
            Color headerColor = Color.Cyan;
            SpriteBatch.DrawString(m_Font, "RANK", new Vector2(colRank, i_Y + 6), headerColor, 0f, Vector2.Zero, headerScale, SpriteEffects.None, 0f);
            SpriteBatch.DrawString(m_Font, "PLAYER", new Vector2(colName, i_Y + 6), headerColor, 0f, Vector2.Zero, headerScale, SpriteEffects.None, 0f);
            SpriteBatch.DrawString(m_Font, "GAME", new Vector2(colGame, i_Y + 6), headerColor, 0f, Vector2.Zero, headerScale, SpriteEffects.None, 0f);
            SpriteBatch.DrawString(m_Font, "SCORE", new Vector2(colScore, i_Y + 6), headerColor, 0f, Vector2.Zero, headerScale, SpriteEffects.None, 0f);
            SpriteBatch.DrawString(m_Font, "LVL", new Vector2(colLevel, i_Y + 6), headerColor, 0f, Vector2.Zero, headerScale, SpriteEffects.None, 0f);
            SpriteBatch.DrawString(m_Font, "DATE", new Vector2(colDate, i_Y + 6), headerColor, 0f, Vector2.Zero, headerScale, SpriteEffects.None, 0f);

            // Query scores based on active tab
            string gameFilter = m_CurrentTab == 1 ? "Tetris" : (m_CurrentTab == 2 ? "Space Invaders" : null);
            List<ScoreEntry> scores = r_ScoresDatabase.GetTopScores(k_TopScoresCount, gameFilter);

            int rowY = i_Y + 34;
            float rowScale = 0.72f;

            if (scores.Count == 0)
            {
                string emptyMsg = "No scores recorded yet - play a game to set a high score!";
                Vector2 emptySize = m_Font.MeasureString(emptyMsg) * 0.75f;
                SpriteBatch.DrawString(m_Font, emptyMsg, new Vector2(i_X + (i_Width - emptySize.X) / 2, rowY + 60), Color.Gray, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
                return;
            }

            for (int i = 0; i < scores.Count; i++)
            {
                ScoreEntry entry = scores[i];
                Rectangle rowRect = new Rectangle(i_X, rowY + i * 34, i_Width, 30);

                if (i % 2 == 1)
                {
                    SpriteBatch.Draw(m_PixelTexture, rowRect, new Color(24, 28, 48, 120));
                }

                Color rankColor = i == 0 ? Color.Gold : (i == 1 ? new Color(220, 225, 235) : (i == 2 ? new Color(205, 127, 50) : new Color(160, 175, 200)));
                Color rowTextColor = i < 3 ? Color.White : new Color(200, 210, 230);

                string rankStr = $"#{i + 1}";
                string nameStr = entry.PlayerName.Length > 12 ? entry.PlayerName.Substring(0, 10) + ".." : entry.PlayerName;
                string gameStr = entry.GameName.Length > 14 ? entry.GameName.Substring(0, 12) + ".." : entry.GameName;
                string scoreStr = entry.Score.ToString("N0");
                string lvlStr = entry.Level.ToString();
                string dateStr = entry.Date.Length > 10 ? entry.Date.Substring(0, 10) : entry.Date;

                SpriteBatch.DrawString(m_Font, rankStr, new Vector2(colRank, rowRect.Y + 6), rankColor, 0f, Vector2.Zero, rowScale, SpriteEffects.None, 0f);
                SpriteBatch.DrawString(m_Font, nameStr, new Vector2(colName, rowRect.Y + 6), rowTextColor, 0f, Vector2.Zero, rowScale, SpriteEffects.None, 0f);
                SpriteBatch.DrawString(m_Font, gameStr, new Vector2(colGame, rowRect.Y + 6), new Color(160, 180, 220), 0f, Vector2.Zero, rowScale, SpriteEffects.None, 0f);
                SpriteBatch.DrawString(m_Font, scoreStr, new Vector2(colScore, rowRect.Y + 6), Color.Gold, 0f, Vector2.Zero, rowScale, SpriteEffects.None, 0f);
                SpriteBatch.DrawString(m_Font, lvlStr, new Vector2(colLevel, rowRect.Y + 6), Color.Cyan, 0f, Vector2.Zero, rowScale, SpriteEffects.None, 0f);
                SpriteBatch.DrawString(m_Font, dateStr, new Vector2(colDate, rowRect.Y + 6), new Color(130, 140, 165), 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            }
        }

        private void drawRectOutline(Rectangle i_Rect, Color i_Color, int i_Thickness)
        {
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_Rect.X, i_Rect.Y, i_Rect.Width, i_Thickness), i_Color);
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_Rect.X, i_Rect.Bottom - i_Thickness, i_Rect.Width, i_Thickness), i_Color);
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_Rect.X, i_Rect.Y, i_Thickness, i_Rect.Height), i_Color);
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_Rect.Right - i_Thickness, i_Rect.Y, i_Thickness, i_Rect.Height), i_Color);
        }
    }
}
