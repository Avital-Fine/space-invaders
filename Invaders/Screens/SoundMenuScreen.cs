using Microsoft.Xna.Framework;
using Infrastructure.ObjectModel.Screens;

namespace Invaders.Screens
{
    public class SoundMenuScreen : MenuScreen
    {
        private const string k_Title = "Sound Settings";
        private readonly string[] r_VolumeOptions = new string[] { "0", "10", "20", "30", "40", "50", "60", "70", "80", "90", "100" };
        private readonly MultiOptionsItem r_ToggleSound;
        private readonly MultiOptionsItem r_BackgroundMusicVolume;
        private readonly MultiOptionsItem r_SoundsEffectsVolume;
        private readonly MenuItem r_Done;

        public SoundMenuScreen(Game i_Game)
            : base(i_Game, k_Title)
        {
            r_ToggleSound = new MultiOptionsItem(this, "Toggle Sound: ");
            r_BackgroundMusicVolume = new MultiOptionsItem(this, "Background Music Volume: ", r_VolumeOptions);
            r_SoundsEffectsVolume = new MultiOptionsItem(this, "Sounds Effects Volume: ", r_VolumeOptions);
            r_Done = new MenuItem(this, "Done");
        }

        public override void Initialize()
        {
            base.Initialize();

            r_ToggleSound.Clicked += ToggleSound_Clicked;
            r_ToggleSound.CurrentOption = SoundsManager.Mute ? "Off" : "On";
            r_BackgroundMusicVolume.Clicked += BackgroundMusicVolume_Clicked;
            r_BackgroundMusicVolume.CurrentOption = ((int)(SoundsManager.BGVolume * 100)).ToString();
            r_SoundsEffectsVolume.Clicked += SoundsEffectsVolume_Clicked;
            r_SoundsEffectsVolume.CurrentOption = ((int)(SoundsManager.SEVolume * 100)).ToString();
            r_Done.Clicked += Done_Clicked;
        }

        private void ToggleSound_Clicked()
        {
            this.SoundsManager.MuteToggle();
        }

        private void BackgroundMusicVolume_Clicked()
        {
            bool valid = float.TryParse(r_BackgroundMusicVolume.CurrentOption, out float volume);

            if (valid)
            {
                this.SoundsManager.ChangeBackgroundVolume(volume / 100);
            }
        }

        private void SoundsEffectsVolume_Clicked()
        {
            bool valid = float.TryParse(r_SoundsEffectsVolume.CurrentOption, out float volume);

            if (valid)
            {
                this.SoundsManager.ChangeSoundEffectsVolume(volume / 100);
            }
        }

        private void Done_Clicked()
        {
            ExitScreen();
        }
    }
}
