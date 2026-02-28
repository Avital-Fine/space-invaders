using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Infrastructure.ObjectModel;

namespace Infrastructure.Managers
{
    public class SoundsManager : GameService
    {
        private List<SoundEffectInstance> r_SoundEffect;
        private SoundEffectInstance m_BackgroundSound;
        private const float k_DefaultVolume = 0.3f;
        private bool m_Mute;
        private float m_BGVolume;
        private float m_SEVolume;
        
        public float BGVolume
        {
            get
            {
                return m_BGVolume;
            }
            private set
            {
                m_BGVolume = value;
                m_BackgroundSound.Volume = m_BGVolume;
            }
        }

        public float SEVolume
        {
            get
            {
                return m_SEVolume;
            }
            private set
            {
                m_SEVolume = value;

                foreach (SoundEffectInstance soundEffect in r_SoundEffect)
                {
                    soundEffect.Volume = m_SEVolume;
                }
            }
        }

        public bool Mute
        {
            get 
            { 
                return m_Mute; 
            }
            set
            {
                if (m_Mute != value)
                {
                    m_Mute = value;
                    muteStateChanged();
                }
            }
        }

        public SoundsManager(Game i_Game) : base(i_Game)
        {
            r_SoundEffect = new List<SoundEffectInstance>();
        }

        public override void Initialize()
        {
            base.Initialize();

            SoundEffect.MasterVolume = k_DefaultVolume;
            m_BGVolume = 0f;
            m_SEVolume = SoundEffect.MasterVolume;
            m_Mute = false;
        }

        protected override void RegisterAsService()
        {
            this.Game.Services.AddService(typeof(SoundsManager), this);
        }

        public SoundEffectInstance LoadBackgroundEffect(string i_AssetName)
        {
            SoundEffect soundEffect = Game.Content.Load<SoundEffect>(i_AssetName);
            SoundEffectInstance soundEffectInstance = soundEffect.CreateInstance();
            m_BackgroundSound = soundEffectInstance;
            m_BackgroundSound.Volume = BGVolume;
            m_BackgroundSound.IsLooped = true;
            m_BackgroundSound.Play();

            return soundEffectInstance;
        }

        public SoundEffectInstance LoadSoundEffect(string i_AssetName)
        {
            SoundEffect soundEffect = Game.Content.Load<SoundEffect>(i_AssetName);
            SoundEffectInstance soundEffectInstance = soundEffect.CreateInstance();
            this.AddSoundEffect(soundEffectInstance);

            return soundEffectInstance;
        }

        public void AddSoundEffect(SoundEffectInstance i_SoundEffect)
        {
            if (!r_SoundEffect.Contains(i_SoundEffect))
            {
                i_SoundEffect.Volume = SEVolume;
                r_SoundEffect.Add(i_SoundEffect);
            }
            else
            {
                throw new ArgumentException("Duplicate SoundEffect are not allowed in the same SoundsManager.");
            }
        }

        public bool RemoveSoundEffect(SoundEffectInstance i_SoundEffect)
        {
            return r_SoundEffect.Remove(i_SoundEffect);
        }

        public void MuteToggle()
        {
            Mute = !Mute;
        }

        public void ChangeBackgroundVolume(float i_Volume)
        {
            if (i_Volume.IsInRange(0,1))
            {
                BGVolume = i_Volume;
            }
        }

        public void ChangeSoundEffectsVolume(float i_Volume)
        {
            if (i_Volume.IsInRange(0, 1))
            {
                SEVolume = i_Volume;
            }
        }

        private void muteStateChanged()
        {
            if (m_Mute)
            {
                SoundEffect.MasterVolume = 0;
            }
            else
            {
                SoundEffect.MasterVolume = k_DefaultVolume;
                BGVolume = m_BGVolume;
                SEVolume = m_SEVolume;
            }
        }
    }
}
