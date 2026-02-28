using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Infrastructure.ObjectModel;
using Infrastructure.ObjectModel.Animators;
using Infrastructure.ObjectModel.Animators.ConcreteAnimators;
using Infrastructure.ObjectModel.Screens;
using Infrastructure.ServiceInterfaces;
using Invaders.Interfaces;

namespace Invaders.Sprites
{
    public class MotherShip : Sprite , ICollidable2D, IHostile
    {
        private static readonly string sr_AssertName = @"Sprites\MotherShip_32x120";
        private static readonly string sr_MotherShipAssetSound = @"Sounds\MotherShipKill";
        private static SoundEffectInstance s_MotherShipSound;
        private readonly int r_Value = 600;
        private readonly Vector2 r_Velocity = new Vector2(95f, 0f);
        private readonly Color r_TintColor = Color.Red;
        // Animations Information
        private const int k_BlinkPerSeconde = 8;
        private readonly TimeSpan r_BlinkLenght = TimeSpan.FromSeconds(1) / k_BlinkPerSeconde;
        private readonly TimeSpan r_TerminationAnimationLenght = TimeSpan.FromSeconds(3);
        // Random Information
        private float m_TotalSecondsForArrival;
        private readonly Random r_RandomMachine = new Random();
        private const float k_ArrivalTime = 3f;
        private const int k_RandomRangeForArrival = 2;

        public int Value { get { return r_Value; } }

        public MotherShip(GameScreen i_GameScreen) : base(sr_AssertName, i_GameScreen)
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            TintColor = r_TintColor;
            Velocity = r_Velocity;
            m_TotalSecondsForArrival = 0;
            Visible = false;
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            s_MotherShipSound = BaseGame.SoundsManager.LoadSoundEffect(sr_MotherShipAssetSound);
        }

        protected override void InitOrigins()
        {
            InitOriginsToCenter();
        }

        protected override void InitAnimations()
        {
            ShrinkAnimator shrinkAnimator = new ShrinkAnimator(r_TerminationAnimationLenght);
            BlinkAnimator blinkAnimator = new BlinkAnimator(r_BlinkLenght, r_TerminationAnimationLenght);
            FadeAnimator fadeAnimator = new FadeAnimator(r_TerminationAnimationLenght);
            CompositeAnimator terminationAnimation = new CompositeAnimator("Ternimation", r_TerminationAnimationLenght, this, shrinkAnimator, blinkAnimator, fadeAnimator);
            AnimationsManager.AddAndPause(terminationAnimation);
            
            base.InitAnimations();
        }

        public override void Update(GameTime i_GameTime)
        {
            base.Update(i_GameTime);

            if (!Visible)
            {
                m_TotalSecondsForArrival += (float)i_GameTime.ElapsedGameTime.TotalSeconds;
                randomArrival();
            }
            else
            {
                checkRouteFinished();
            }
        }

        private void randomArrival()
        {
            bool timeForArrival = m_TotalSecondsForArrival >= k_ArrivalTime;
            
            if (timeForArrival)
            {
                m_TotalSecondsForArrival -= k_ArrivalTime;

                if (r_RandomMachine.Next(k_RandomRangeForArrival) == 1)
                {
                    startArrival();
                }
            }
        }

        private void checkRouteFinished()
        {
            bool routeFinished = TopLeftPosition.X >= Game.GraphicsDevice.Viewport.Width;
            
            if (routeFinished)
            {
                Visible = false;
                Velocity = Vector2.Zero;
            }
        }

        public override void Collided(ICollidable i_Collidable)
        {
            if(i_Collidable is Bullet && (i_Collidable as Bullet).Type == eBulletType.Ship)
            {
                s_MotherShipSound.Play();
                AnimationsManager["Ternimation"].Restart();
            }
        }

        private void startArrival()
        {
            Position = new Vector2(-WidthBeforeScale, HeightBeforeScale);
            Velocity = r_Velocity;
            Visible = true;
        }
    }
}
