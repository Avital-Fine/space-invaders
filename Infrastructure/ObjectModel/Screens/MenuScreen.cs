using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace Infrastructure.ObjectModel.Screens
{
    public abstract class MenuScreen : GameScreen
    {
        private const string k_Font = "Consolas";
        protected readonly List<MenuItem> r_Options;
        protected readonly Headline r_Title;
        private int m_ActiveItemIndex = 0;
        private static SoundEffectInstance s_TransitionSoundEffect;

        private KeyboardState m_CurrKeyboard;
        private KeyboardState m_PrevKeyboard;
        private GamePadState m_CurrGamePad;
        private GamePadState m_PrevGamePad;

        public static SoundEffectInstance TransitionSoundEffect { set { s_TransitionSoundEffect = value; } }

        public MenuScreen(Game i_Game, string i_Title) : base(i_Game)
        {
            r_Options = new List<MenuItem>();
            r_Title = new Headline(this, k_Font, i_Title);
        }

        public override void Initialize()
        {
            base.Initialize();
            
            this.BlendState = BlendState.NonPremultiplied;
            m_CurrKeyboard = m_PrevKeyboard = Keyboard.GetState();
            m_CurrGamePad = m_PrevGamePad = GamePad.GetState(PlayerIndex.One);

            Game.Window.ClientSizeChanged += Window_ClientSizeChanged;
        }

        public void AddOption(MenuItem i_Option)
        {
            if (!r_Options.Contains(i_Option))
            {
                r_Options.Add(i_Option);
                i_Option.ListIndex = r_Options.IndexOf(i_Option);
                i_Option.FocusChange += Option_FocusChange;
                setItemPosition(i_Option);

                if (i_Option.ListIndex == 0)
                {
                    i_Option.HasFocus = true;
                }
            }
        }

        private void setItemPosition(MenuItem i_MenuItem)
        {
            float newYPosition = r_Title.TopLeftPosition.Y + r_Title.Height + (i_MenuItem.ListIndex) * i_MenuItem.Height * 1.2f + 20;
            float newXPosition = this.CenterOfViewPort.X;
            i_MenuItem.Position = new Vector2(newXPosition, newYPosition);
        }

        public override void Update(GameTime i_GameTime)
        {
            base.Update(i_GameTime);

            m_PrevKeyboard = m_CurrKeyboard;
            m_CurrKeyboard = Keyboard.GetState();

            m_PrevGamePad = m_CurrGamePad;
            m_CurrGamePad = GamePad.GetState(PlayerIndex.One);

            bool downPressed = isKeyPressed(Keys.Down) || isKeyPressed(Keys.S) || isButtonPressed(Buttons.DPadDown);
            bool upPressed = isKeyPressed(Keys.Up) || isKeyPressed(Keys.W) || isButtonPressed(Buttons.DPadUp);
            bool selectPressed = isKeyPressed(Keys.Enter) || isKeyPressed(Keys.Space) || isButtonPressed(Buttons.A) || isButtonPressed(Buttons.Start);
            bool backPressed = isKeyPressed(Keys.Escape) || isButtonPressed(Buttons.Back) || isButtonPressed(Buttons.B);

            if (downPressed)
            {
                updateActiveOption(m_ActiveItemIndex + 1);
            }
            else if (upPressed)
            {
                updateActiveOption(m_ActiveItemIndex - 1);
            }
            else if (selectPressed)
            {
                if (m_ActiveItemIndex >= 0 && m_ActiveItemIndex < r_Options.Count)
                {
                    r_Options[m_ActiveItemIndex].TriggerClick();
                }
            }
            else if (backPressed)
            {
                ExitScreen();
            }
        }

        private bool isKeyPressed(Keys i_Key)
        {
            bool direct = m_CurrKeyboard.IsKeyDown(i_Key) && m_PrevKeyboard.IsKeyUp(i_Key);
            bool viaManager = InputManager != null && InputManager.KeyPressed(i_Key);
            return direct || viaManager;
        }

        private bool isButtonPressed(Buttons i_Button)
        {
            return m_CurrGamePad.IsButtonDown(i_Button) && m_PrevGamePad.IsButtonUp(i_Button);
        }

        private void updateActiveOption(int i_newActiveOption)
        {
            if (r_Options.Count == 0) return;
            int nextIndex = (i_newActiveOption % r_Options.Count + r_Options.Count) % r_Options.Count;
            r_Options[nextIndex].HasFocus = true;
        }

        private void Option_FocusChange(object sender, EventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item != null && item.ListIndex != m_ActiveItemIndex)
            {
                s_TransitionSoundEffect?.Play();

                if (m_ActiveItemIndex >= 0 && m_ActiveItemIndex < r_Options.Count)
                {
                    r_Options[m_ActiveItemIndex].HasFocus = false;
                }
                m_ActiveItemIndex = item.ListIndex;
            }
        }

        private void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            foreach (MenuItem option in r_Options)
            {
                setItemPosition(option);
            }
        }
    }
}
