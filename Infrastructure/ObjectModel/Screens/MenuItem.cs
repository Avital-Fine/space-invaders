using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Infrastructure.ServiceInterfaces;
using Infrastructure.ObjectModel.Animators.ConcreteAnimators;

namespace Infrastructure.ObjectModel.Screens
{
    public class MenuItem : GameComponent
    {
        public Action Clicked;
        public EventHandler<EventArgs> FocusChange;

        private const string k_AssetName = @"Sprites\Button";
        private const string k_Font = "Consolas";
        private const float k_InactiveOpacity = 0.5f;
        private const float k_ActiveOpacity = 1;
        protected readonly MenuScreen r_MenuScreen;
        protected readonly string r_Title;
        private readonly Keys r_SelectTrigger1 = Keys.Enter;
        private readonly eInputButtons r_SelectTrigger2 = eInputButtons.Left;
        private readonly Sprite r_Button;
        private readonly Text r_Text;
        private bool m_HasFocus;
        private int m_ListIndex;

        protected string Content 
        { 
            get { return r_Text.Content; }
            set 
            { 
                r_Text.Content = value;
                r_Text.InitOriginsToCenter();
                r_Text.Position = r_Button.Position;
            }
        }
        public float Height {  get {  return r_Button.Height; }}
        public float Width {  get {  return r_Button.Width; }}
        public int ListIndex { get { return m_ListIndex; } set { m_ListIndex = value; } }
        public bool HasFocus 
        { 
            get { return m_HasFocus; } 
            set 
            {
                if (m_HasFocus != value)
                {
                    m_HasFocus = value;
                    onFocusChange();
                }
            } 
        }
        public Vector2 Position
        {
            get { return r_Text.Position;  }
            set
            {
                r_Text.Position = value;
                r_Button.Position = value;
            }
        }

        public MenuItem(MenuScreen i_MenuScreen, string i_OptionTitle): base (i_MenuScreen.Game)
        {
            r_Title = i_OptionTitle;
            r_MenuScreen = i_MenuScreen;
            r_Button = new Sprite(k_AssetName, i_MenuScreen);
            r_Text = new Text(i_MenuScreen, k_Font, i_OptionTitle);
            r_MenuScreen.Add(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            initButtonAnimations();
            centralize();
            deactivate() ;
            r_Text.TintColor = Color.Black;
            r_MenuScreen.AddOption(this);
        }

        private void centralize()
        {
            r_Button.Centralize();
            r_Text.Centralize();
        }

        private void initButtonAnimations()
        {
            PulseAnimator pulseAnimator = new PulseAnimator(TimeSpan.FromSeconds(0), 1.1f, 1);
            r_Button.AnimationsManager.AddAndPause(pulseAnimator);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if(r_MenuScreen.InputManager.MousePositionDelta != Vector2.Zero)
            {
                HasFocus = r_MenuScreen.InputManager.MouseHover(r_Button.Bounds);
            }

            if (HasFocus) 
            {
                DoWhenActive();
            }
        }

        protected virtual void DoWhenActive()
        {
            if (TriggersActivated())
            {
                OnClicked();
            }
        }

        protected virtual bool TriggersActivated()
        {
            return r_MenuScreen.InputManager.ButtonPressed(r_SelectTrigger2) || r_MenuScreen.InputManager.KeyPressed(r_SelectTrigger1);
        }

        private void deactivate()
        {
            r_Button.Opacity = k_InactiveOpacity;
            r_Button.AnimationsManager["Pulse"].Pause();
            r_Button.AnimationsManager["Pulse"].Reset();
        }

        private void activate()
        {
            r_Button.Opacity = k_ActiveOpacity;
            r_Button.AnimationsManager["Pulse"].Resume();
        }

        protected void OnClicked()
        {
            if (Clicked != null)
            {
                Clicked.Invoke();
            }
        }

        private void onFocusChange()
        {
            if (FocusChange != null)
            {
                FocusChange.Invoke(this, EventArgs.Empty);
            }

            if (m_HasFocus)
            {
                activate();
            }
            else
            {
                deactivate();
            }
        }
    }
}