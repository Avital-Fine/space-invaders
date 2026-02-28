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

        private const string k_FontType = "Consolas";
        private const int k_MaxNameLength = 12;
        private const float k_CursorBlinkInterval = 0.5f;

        private readonly eNumberOfPlayers r_NumberOfPlayers;
        private readonly Text r_PromptMessage;
        private readonly Text r_TypedNameDisplay;
        private readonly Text r_InfoMessage;
        private readonly string[] r_PlayerNames;

        private int m_CurrentPlayerIndex;
        private string m_CurrentInput;
        private float m_CursorBlinkTimer;
        private bool m_ShowCursor;

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
Backspace to delete");
        }

        public override void Initialize()
        {
            base.Initialize();

            this.BlendState = BlendState.NonPremultiplied;
            updatePrompt();
            updateTypedDisplay();
            positionTexts();
        }

        private void positionTexts()
        {
            r_PromptMessage.TintColor = Color.CadetBlue;
            r_PromptMessage.Scales = new Vector2(1.5f, 1.5f);
            r_PromptMessage.Position = CenterOfViewPort - new Vector2(r_PromptMessage.Width / 2, 100);

            r_TypedNameDisplay.TintColor = Color.White;
            r_TypedNameDisplay.Scales = new Vector2(2f, 2f);

            r_InfoMessage.TintColor = Color.Gray;
            r_InfoMessage.Position = CenterOfViewPort + new Vector2(-r_InfoMessage.Width / 2, 80);
        }

        private void centerTypedDisplay()
        {
            r_TypedNameDisplay.Position = CenterOfViewPort - new Vector2(r_TypedNameDisplay.Width / 2, 0);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Blink the cursor
            m_CursorBlinkTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (m_CursorBlinkTimer >= k_CursorBlinkInterval)
            {
                m_CursorBlinkTimer -= k_CursorBlinkInterval;
                m_ShowCursor = !m_ShowCursor;
                updateTypedDisplay();
            }

            // Check for letter keys A-Z
            for (Keys key = Keys.A; key <= Keys.Z; key++)
            {
                if (InputManager.KeyPressed(key) && m_CurrentInput.Length < k_MaxNameLength)
                {
                    m_CurrentInput += key.ToString();
                    updateTypedDisplay();
                }
            }

            // Backspace — delete last character
            if (InputManager.KeyPressed(Keys.Back) && m_CurrentInput.Length > 0)
            {
                m_CurrentInput = m_CurrentInput.Substring(0, m_CurrentInput.Length - 1);
                updateTypedDisplay();
            }

            // Enter — confirm name
            if (InputManager.KeyPressed(Keys.Enter))
            {
                confirmCurrentName();
            }
        }

        private void confirmCurrentName()
        {
            // If empty, use default name
            if (string.IsNullOrEmpty(m_CurrentInput))
            {
                r_PlayerNames[m_CurrentPlayerIndex] = $"Player {m_CurrentPlayerIndex + 1}";
            }
            else
            {
                r_PlayerNames[m_CurrentPlayerIndex] = m_CurrentInput;
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
        }

        private void updateTypedDisplay()
        {
            string cursor = m_ShowCursor ? "_" : " ";
            r_TypedNameDisplay.Content = m_CurrentInput + cursor;
            centerTypedDisplay();
        }

        private void onNamesEntered()
        {
            if (NamesEntered != null)
            {
                NamesEntered.Invoke(r_PlayerNames);
            }
        }
    }
}