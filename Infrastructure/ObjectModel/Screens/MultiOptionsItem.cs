using Microsoft.Xna.Framework.Input;
using Infrastructure.ServiceInterfaces;

namespace Infrastructure.ObjectModel.Screens
{
    public class MultiOptionsItem : MenuItem
    {
        private readonly Keys r_NextTrigger = Keys.Right;
        private readonly Keys r_PrevTrigger = Keys.Left;
        private readonly Keys r_EnterTrigger = Keys.Enter;
        private readonly eInputButtons r_ClickTrigger = eInputButtons.Left;
        private readonly string[] r_Options;
        private int m_CurrentIdx = 0;

        public string CurrentOption 
        { 
            get { return r_Options[m_CurrentIdx]; }
            set 
            { 
                for (int i = 0; i < r_Options.Length; i++)
                {
                    if (r_Options[i] == value)
                    {
                        currentIdx = i;
                        break;
                    }
                }
            }
        }
        private int currentIdx
        {
            set
            {
                m_CurrentIdx = value;
                this.Content = r_Title + r_Options[m_CurrentIdx];
            }
        }

        public MultiOptionsItem(MenuScreen i_MenuScreen, string i_OptionTitle, params string[] i_Options)
            : base(i_MenuScreen, i_OptionTitle)
        {
            r_Options = i_Options;
        }

        public MultiOptionsItem(MenuScreen i_MenuScreen, string i_OptionTitle) 
            : base (i_MenuScreen, i_OptionTitle)
        {
            r_Options = new string[] { "On", "Off"};
        }

        protected override void DoWhenActive()
        {
            int scrollWheelDelta = r_MenuScreen.InputManager.ScrollWheelDelta;
            bool changeState = false;

            if (changeState = r_MenuScreen.InputManager.KeyPressed(r_PrevTrigger))
            {
                m_CurrentIdx--;
            }
            else if (changeState = r_MenuScreen.InputManager.KeyPressed(r_NextTrigger)
                || r_MenuScreen.InputManager.KeyPressed(r_EnterTrigger)
                || r_MenuScreen.InputManager.ButtonPressed(r_ClickTrigger))
            {
                m_CurrentIdx++;
            }
            else if (changeState = scrollWheelDelta != 0)
            {
                m_CurrentIdx += scrollWheelDelta / 120;
            }

            if (changeState)
            {
                currentIdx = m_CurrentIdx.Mod(r_Options.Length);
                OnClicked();
            }
        }
    }
}
