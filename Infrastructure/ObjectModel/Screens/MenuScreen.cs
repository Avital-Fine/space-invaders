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
        private readonly Keys r_NextTrigger = Keys.Up;
        private readonly Keys r_PrevTrigger = Keys.Down;
        private readonly Keys r_BackTrigger = Keys.Escape;
        protected readonly Headline r_Title;
        private int m_ActiveItemIndex = 0;
        private static SoundEffectInstance s_TransitionSoundEffect;

        public static SoundEffectInstance TransitionSoundEffect { set { s_TransitionSoundEffect = value; } }

        public MenuScreen(Game i_Game, string i_Title) : base(i_Game)
        {
            r_Options = new List<MenuItem>();
            r_Title = new Headline(this,k_Font, i_Title);
        }

        public override void Initialize()
        {
            base.Initialize();
            
            this.BlendState = BlendState.NonPremultiplied;
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

                if (i_Option.ListIndex == 1)
                {
                    i_Option.HasFocus = true;
                }
            }
        }

        private void setItemPosition(MenuItem i_MenuItem)
        {
            float newYPosition = r_Title.TopLeftPosition.Y + r_Title.Height + (i_MenuItem.ListIndex) * i_MenuItem.Height * 1.2f + 20;
            float newXPosition =  this.CenterOfViewPort.X;
            i_MenuItem.Position = new Vector2(newXPosition, newYPosition);
        }

        public override void Update(GameTime i_GameTime)
        {
            base.Update(i_GameTime);

            if (InputManager.KeyPressed(r_PrevTrigger))
            {
                updateActiveOption(m_ActiveItemIndex + 1);
            }
            else if (InputManager.KeyPressed(r_NextTrigger))
            {
                updateActiveOption(m_ActiveItemIndex - 1);
            }
            else if (InputManager.KeyPressed(r_BackTrigger))
            {
                ExitScreen();
            }
        }

        private void updateActiveOption(int i_newActiveOption)
        {
            r_Options[i_newActiveOption.Mod(r_Options.Count)].HasFocus = true;
        }

        private void Option_FocusChange(object sender, EventArgs e)
        {
            if ((sender as MenuItem).ListIndex != m_ActiveItemIndex)
            {
                if (s_TransitionSoundEffect != null)
                {
                    s_TransitionSoundEffect.Play();
                }

                r_Options[m_ActiveItemIndex].HasFocus = false;
                m_ActiveItemIndex = (sender as MenuItem).ListIndex;
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
