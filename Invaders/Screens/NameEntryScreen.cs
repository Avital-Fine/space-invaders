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
        private const float k_CursorBlinkInterval = 0.45f;

        private readonly eNumberOfPlayers r_NumberOfPlayers;
        private readonly Text r_PromptMessage;
        private readonly Text r_TypedNameDisplay;
        private readonly Text r_InfoMessage;
        private readonly string[] r_PlayerNames;

        private int m_CurrentPlayerIndex;
        private string m_CurrentInput;
        private float m_CursorBlinkTimer;
        private bool m_ShowCursor;

        private KeyboardState m_CurrKeyboard;
        private KeyboardState m_PrevKeyboard;

        public NameEntryScreen(Game i_Game, eNumberOfPlayers i_NumberOfPlayers)
            : base(i_Game)
        {
            r_NumberOfPlayers = i_NumberOfPlayers;
            r_PlayerNames = new string[(int)r_NumberOfPlayers];
            m_CurrentPlayerIndex = 0;
            m_CurrentInput = "";
            m_ShowCursor = true;

            r_PromptMessage = new Text(this, k_FontType);
            r_TypedNameDisplay = new Text(this, k_FontType);
            r_InfoMessage = new Text(this, k_FontType, @"Type your name and press Enter
Press Enter to skip (default name)
[Esc] to Cancel  |  [Backspace] to delete");
        }

        public override void Initialize()
        {
            base.Initialize();

            this.BlendState = BlendState.NonPremultiplied;
            m_CurrKeyboard = m_PrevKeyboard = Keyboard.GetState();

            updatePrompt();
            updateTypedDisplay();
            positionTexts();
        }

        private void positionTexts()
        {
            r_PromptMessage.TintColor = Color.Cyan;
            r_PromptMessage.Scales = new Vector2(1.5f, 1.5f);
            r_PromptMessage.Position = CenterOfViewPort - new Vector2(r_PromptMessage.Width / 2, 100);

            r_TypedNameDisplay.TintColor = Color.Gold;
            r_TypedNameDisplay.Scales = new Vector2(2f, 2f);

            r_InfoMessage.TintColor = new Color(170, 185, 210);
            r_InfoMessage.Position = CenterOfViewPort + new Vector2(-r_InfoMessage.Width / 2, 80);
        }

        private void centerTypedDisplay()
        {
            r_TypedNameDisplay.Position = CenterOfViewPort - new Vector2(r_TypedNameDisplay.Width / 2, 0);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            m_PrevKeyboard = m_CurrKeyboard;
            m_CurrKeyboard = Keyboard.GetState();

            // Blink the cursor
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
                ExitScreen();
                Cancelled?.Invoke();
                return;
            }

            // Check for letter keys A-Z
            for (Keys key = Keys.A; key <= Keys.Z; key++)
            {
                if (isKeyPressed(key) && m_CurrentInput.Length < k_MaxNameLength)
                {
                    m_CurrentInput += key.ToString();
                    updateTypedDisplay();
                }
            }

            // Check for number keys 0-9 (top row)
            for (Keys key = Keys.D0; key <= Keys.D9; key++)
            {
                if (isKeyPressed(key) && m_CurrentInput.Length < k_MaxNameLength)
                {
                    m_CurrentInput += ((int)key - (int)Keys.D0).ToString();
                    updateTypedDisplay();
                }
            }

            // Check for numpad 0-9
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

            // Backspace — delete last character
            if (isKeyPressed(Keys.Back) && m_CurrentInput.Length > 0)
            {
                m_CurrentInput = m_CurrentInput.Substring(0, m_CurrentInput.Length - 1);
                updateTypedDisplay();
            }

            // Enter — confirm name
            if (isKeyPressed(Keys.Enter))
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
            // If empty, use default name
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
                // All names collected — fire event and exit
                ExitScreen();
                onNamesEntered();
            }
            else
            {
                // Move to next player
                m_CurrentInput = "";
                updatePrompt();
                updateTypedDisplay();
            }
        }

        private void updatePrompt()
        {
            r_PromptMessage.Content = $"Player {m_CurrentPlayerIndex + 1}, enter your name:";
            r_PromptMessage.Position = CenterOfViewPort - new Vector2(r_PromptMessage.Width / 2, 100);
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
    }
}