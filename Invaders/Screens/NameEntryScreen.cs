using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Infrastructure.ObjectModel;
using Infrastructure.ObjectModel.Screens;

namespace Invaders.Screens
{
    public class NameEntryScreen : GameScreen
    {
        public event Action<string[]> NamesEntered;
        public event Action Cancelled;

        private const string k_FontType = "Consolas";
        private const int k_MaxNameLength = 12;
        private const float k_CursorBlinkInterval = 0.4f;

        private readonly eNumberOfPlayers r_NumberOfPlayers;
        private readonly Text r_PromptMessage;
        private readonly Text r_TypedNameDisplay;
        private readonly Text r_InfoMessage;
        private readonly string[] r_PlayerNames;

        private int m_CurrentPlayerIndex;
        private string m_CurrentInput;
        private float m_CursorBlinkTimer;
        private bool m_ShowCursor;
        private bool m_EnterWasReleased;
        private bool m_HasFinished;

        private KeyboardState m_CurrKeyboard;
        private KeyboardState m_PrevKeyboard;
        private MouseState m_CurrMouse;
        private MouseState m_PrevMouse;

        private Texture2D m_Pixel;
        private SpriteFont m_Font;

        public NameEntryScreen(Game i_Game, eNumberOfPlayers i_NumberOfPlayers)
            : base(i_Game)
        {
            r_NumberOfPlayers = i_NumberOfPlayers;
            r_PlayerNames = new string[(int)r_NumberOfPlayers];
            m_CurrentPlayerIndex = 0;
            m_CurrentInput = "";
            m_ShowCursor = true;
            m_EnterWasReleased = false;
            m_HasFinished = false;

            r_PromptMessage = new Text(this, k_FontType);
            r_TypedNameDisplay = new Text(this, k_FontType);
            r_InfoMessage = new Text(this, k_FontType, @"Type your name and press [ENTER]
Press [ENTER] to skip (default name: Player 1)
[ESC] to Cancel  |  [Backspace] to delete");
        }

        public override void Initialize()
        {
            base.Initialize();

            this.BlendState = BlendState.NonPremultiplied;
            m_CurrKeyboard = m_PrevKeyboard = Keyboard.GetState();
            m_CurrMouse = m_PrevMouse = Mouse.GetState();

            m_Pixel = new Texture2D(GraphicsDevice, 1, 1);
            m_Pixel.SetData(new[] { Color.White });

            try
            {
                m_Font = Game.Content.Load<SpriteFont>(@"Fonts\Consolas");
            }
            catch { }

            updatePrompt();
            updateTypedDisplay();
            positionTexts();
        }

        private void positionTexts()
        {
            r_PromptMessage.TintColor = Color.Cyan;
            r_PromptMessage.Scales = new Vector2(1.5f, 1.5f);
            r_PromptMessage.Position = CenterOfViewPort - new Vector2(r_PromptMessage.Width / 2, 120);

            r_TypedNameDisplay.TintColor = Color.Gold;
            r_TypedNameDisplay.Scales = new Vector2(2f, 2f);

            r_InfoMessage.TintColor = new Color(170, 185, 210);
            r_InfoMessage.Position = CenterOfViewPort + new Vector2(-r_InfoMessage.Width / 2, 60);
        }

        private void centerTypedDisplay()
        {
            r_TypedNameDisplay.Position = CenterOfViewPort - new Vector2(r_TypedNameDisplay.Width / 2, -10);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            m_PrevKeyboard = m_CurrKeyboard;
            m_CurrKeyboard = Keyboard.GetState();

            m_PrevMouse = m_CurrMouse;
            m_CurrMouse = Mouse.GetState();

            if (m_HasFinished) return;

            if (m_CurrKeyboard.IsKeyUp(Keys.Enter))
            {
                m_EnterWasReleased = true;
            }

            // Blink cursor
            m_CursorBlinkTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (m_CursorBlinkTimer >= k_CursorBlinkInterval)
            {
                m_CursorBlinkTimer -= k_CursorBlinkInterval;
                m_ShowCursor = !m_ShowCursor;
                updateTypedDisplay();
            }

            // Cancel / Back to Dashboard
            if (isKeyPressed(Keys.Escape))
            {
                m_HasFinished = true;
                ExitScreen();
                Cancelled?.Invoke();
                return;
            }

            // Letters A-Z
            for (Keys key = Keys.A; key <= Keys.Z; key++)
            {
                if (isKeyPressed(key) && m_CurrentInput.Length < k_MaxNameLength)
                {
                    m_CurrentInput += key.ToString();
                    updateTypedDisplay();
                }
            }

            // Numbers 0-9 (top row)
            for (Keys key = Keys.D0; key <= Keys.D9; key++)
            {
                if (isKeyPressed(key) && m_CurrentInput.Length < k_MaxNameLength)
                {
                    m_CurrentInput += ((int)key - (int)Keys.D0).ToString();
                    updateTypedDisplay();
                }
            }

            // Numpad 0-9
            for (Keys key = Keys.NumPad0; key <= Keys.NumPad9; key++)
            {
                if (isKeyPressed(key) && m_CurrentInput.Length < k_MaxNameLength)
                {
                    m_CurrentInput += ((int)key - (int)Keys.NumPad0).ToString();
                    updateTypedDisplay();
                }
            }

            // Space
            if (isKeyPressed(Keys.Space) && m_CurrentInput.Length > 0 && m_CurrentInput.Length < k_MaxNameLength)
            {
                m_CurrentInput += " ";
                updateTypedDisplay();
            }

            // Backspace
            if (isKeyPressed(Keys.Back) && m_CurrentInput.Length > 0)
            {
                m_CurrentInput = m_CurrentInput.Substring(0, m_CurrentInput.Length - 1);
                updateTypedDisplay();
            }

            // Click Start Button
            Rectangle startBtnRect = new Rectangle((int)CenterOfViewPort.X - 110, (int)CenterOfViewPort.Y + 140, 220, 44);
            bool mouseClickedBtn = m_CurrMouse.LeftButton == ButtonState.Pressed &&
                                   m_PrevMouse.LeftButton == ButtonState.Released &&
                                   startBtnRect.Contains(m_CurrMouse.Position);

            // Confirm on Enter (once released) or Button click
            bool enterPressed = m_EnterWasReleased && (isKeyPressed(Keys.Enter) || (m_CurrKeyboard.IsKeyDown(Keys.Enter) && m_PrevKeyboard.IsKeyUp(Keys.Enter)));

            if (enterPressed || mouseClickedBtn)
            {
                confirmCurrentName();
            }
        }

        private bool isKeyPressed(Keys i_Key)
        {
            bool direct = m_CurrKeyboard.IsKeyDown(i_Key) && m_PrevKeyboard.IsKeyUp(i_Key);
            bool viaManager = InputManager != null && InputManager.KeyPressed(i_Key);
            return direct || viaManager;
        }

        private void confirmCurrentName()
        {
            if (m_HasFinished) return;

            if (string.IsNullOrWhiteSpace(m_CurrentInput))
            {
                r_PlayerNames[m_CurrentPlayerIndex] = $"Player {m_CurrentPlayerIndex + 1}";
            }
            else
            {
                r_PlayerNames[m_CurrentPlayerIndex] = m_CurrentInput.Trim();
            }

            m_CurrentPlayerIndex++;

            if (m_CurrentPlayerIndex >= (int)r_NumberOfPlayers)
            {
                m_HasFinished = true;
                ExitScreen();
                onNamesEntered();
            }
            else
            {
                m_CurrentInput = "";
                updatePrompt();
                updateTypedDisplay();
            }
        }

        private void updatePrompt()
        {
            r_PromptMessage.Content = $"Player {m_CurrentPlayerIndex + 1}, enter your name:";
            r_PromptMessage.Position = CenterOfViewPort - new Vector2(r_PromptMessage.Width / 2, 120);
        }

        private void updateTypedDisplay()
        {
            string cursor = m_ShowCursor ? "_" : " ";
            r_TypedNameDisplay.Content = m_CurrentInput + cursor;
            centerTypedDisplay();
        }

        private void onNamesEntered()
        {
            NamesEntered?.Invoke(r_PlayerNames);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (m_Pixel == null || m_Font == null) return;

            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // Draw clickable Start Button
            Rectangle startBtnRect = new Rectangle((int)CenterOfViewPort.X - 110, (int)CenterOfViewPort.Y + 140, 220, 44);
            bool isHovered = startBtnRect.Contains(m_CurrMouse.Position);
            Color btnColor = isHovered ? new Color(50, 180, 80) : new Color(30, 140, 50);

            SpriteBatch.Draw(m_Pixel, startBtnRect, btnColor);
            drawRectOutline(startBtnRect, Color.Gold, 2);

            string btnText = "START GAME";
            Vector2 textSize = m_Font.MeasureString(btnText) * 0.9f;
            Vector2 textPos = new Vector2(startBtnRect.X + (startBtnRect.Width - textSize.X) / 2, startBtnRect.Y + (startBtnRect.Height - textSize.Y) / 2);
            SpriteBatch.DrawString(m_Font, btnText, textPos, Color.White, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);

            SpriteBatch.End();
        }

        private void drawRectOutline(Rectangle i_Rect, Color i_Color, int i_Thickness)
        {
            SpriteBatch.Draw(m_Pixel, new Rectangle(i_Rect.X, i_Rect.Y, i_Rect.Width, i_Thickness), i_Color);
            SpriteBatch.Draw(m_Pixel, new Rectangle(i_Rect.X, i_Rect.Bottom - i_Thickness, i_Rect.Width, i_Thickness), i_Color);
            SpriteBatch.Draw(m_Pixel, new Rectangle(i_Rect.X, i_Rect.Y, i_Thickness, i_Rect.Height), i_Color);
            SpriteBatch.Draw(m_Pixel, new Rectangle(i_Rect.Right - i_Thickness, i_Rect.Y, i_Thickness, i_Rect.Height), i_Color);
        }
    }
}