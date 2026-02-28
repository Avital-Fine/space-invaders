using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Infrastructure.ServiceInterfaces;
using Infrastructure.ObjectModel;
using Infrastructure.ObjectModel.Animators.ConcreteAnimators;
using Infrastructure.ObjectModel.Animators;
using Infrastructure.ObjectModel.Screens;
using Invaders.Interfaces;

namespace Invaders.Sprites
{
    public class Enemy : Sprite, ICollidable2D, IHostile
    {
        public event Action Hit;

        private static readonly string sr_AssetName = @"Sprites\EnemyCollection_64x92";
        private static readonly string sr_GunShotAssetSound = @"Sounds\EnemyGunShot";
        private static readonly string sr_EnemyKillAssetSound = @"Sounds\EnemyKill";
        private static readonly int sr_NumberOfEnemiesTypes = 3;
        protected static readonly int sr_NumberOfCells = 2;

        private readonly eEnemyType r_EnemyType;
        private static int s_PinkEnemyValue;
        private static int s_BlueEnemyValue;
        private static int s_YellowEnemyValue;
        private int m_Value;
        private static SoundEffectInstance s_EnemyKillSound;
        // Shooting Information
        private static bool s_SoundLoaded = false;
        private static SoundEffectInstance s_GunShotSound;
        private readonly Bullet r_Bullet;
        private readonly Random r_RandomMachine = new Random();
        private float m_TotalSecondsForShoot;
        private const float k_ShootingTime = 1f;
        private const int k_RandomRangeForShoot = 20;
        // Jumping Information
        private static readonly float sr_StartJumpingTime = 0.5f;
        private static float s_CurrentJumpingTime;
        private static bool s_MovingRight;
        private static Vector2 s_JumpingRLDistance;
        private static Vector2 s_JumpingDownDistance;
        private float m_TotalSecondsForJump;
        // Animatiom Informatiom
        private static readonly TimeSpan sr_TerminationAnimationLenght = TimeSpan.FromSeconds(1.7);
        private static readonly int sr_RotationsPerMinute = 5;

        public int Value { get { return m_Value; } }
        public static int PinkEnemyValue { get { return s_PinkEnemyValue; } set { s_PinkEnemyValue = value; } }
        public static int BlueEnemyValue { get { return s_BlueEnemyValue; } set { s_BlueEnemyValue = value; } }
        public static int YellowEnemyValue { get { return s_YellowEnemyValue; } set { s_YellowEnemyValue = value; } }
        public static bool MovingRight { get { return s_MovingRight; } set { s_MovingRight = value; } }
        public static Vector2 JumpingRLDistance { get { return s_JumpingRLDistance; } set { s_JumpingRLDistance = value; } }
        public static Vector2 JumpingDownDistance { get { return s_JumpingDownDistance; } set { s_JumpingDownDistance = value; } }
        public static float JumpingTime { get { return s_CurrentJumpingTime; } set { s_CurrentJumpingTime = value; } }
        public static float StartJumpingTime { get { return sr_StartJumpingTime; } }

        public Enemy(GameScreen i_GameScreen, eEnemyType i_Type)
            : base(sr_AssetName, i_GameScreen)
        {
            r_EnemyType = i_Type;
            r_Bullet = new Bullet(i_GameScreen, eBulletType.Enemy);
            
        }
        public override void Initialize()
        {
            base.Initialize();
            m_TotalSecondsForJump = 0;

            switch (r_EnemyType)
            {
                case eEnemyType.Pink:
                    m_Value = PinkEnemyValue;
                    TintColor = Color.Pink;
                    break;
                case eEnemyType.Blue:
                    m_Value = BlueEnemyValue;
                    TintColor = Color.LightSteelBlue;
                    break;
                case eEnemyType.Yellow:
                    m_Value = YellowEnemyValue;
                    TintColor = Color.LightYellow;
                    break;
            }
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            if (!s_SoundLoaded)
            {
                s_GunShotSound = BaseGame.SoundsManager.LoadSoundEffect(sr_GunShotAssetSound);
                s_EnemyKillSound = BaseGame.SoundsManager.LoadSoundEffect(sr_EnemyKillAssetSound);
                s_SoundLoaded = true;
            }
        }

        protected override void InitAnimations()
        {
            ShrinkAnimator shrinkAnimator = new ShrinkAnimator(sr_TerminationAnimationLenght);
            RotationAnimator rotationAnimator = new RotationAnimator(sr_RotationsPerMinute, sr_TerminationAnimationLenght);
            CompositeAnimator terminationAnimation = new CompositeAnimator("Ternimation", sr_TerminationAnimationLenght, this, shrinkAnimator, rotationAnimator);
            
            terminationAnimation.Finished += terminationAnimation_Finished;
            AnimationsManager.AddAndPause(terminationAnimation);

            base.InitAnimations();
        }

        protected override void InitBounds()
        {
            WidthBeforeScale = Texture.Width / sr_NumberOfCells;
            HeightBeforeScale = Texture.Height / sr_NumberOfEnemiesTypes;
            InitSourceRectangle();
            InitOrigins();
        }

        protected override void InitSourceRectangle()
        {
            SourceRectangle = new Rectangle(0, (int)r_EnemyType * (int)HeightBeforeScale, (int)WidthBeforeScale, (int)HeightBeforeScale);
        }

        protected override void InitOrigins()
        {
            InitOriginsToCenter();
            InitOriginsToCenter();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            m_TotalSecondsForJump += (float)gameTime.ElapsedGameTime.TotalSeconds;
            m_TotalSecondsForShoot += (float)gameTime.ElapsedGameTime.TotalSeconds;
            bool timeForJumping = m_TotalSecondsForJump >= JumpingTime;
            
            if (timeForJumping)
            {
                m_TotalSecondsForJump -= JumpingTime;
                Position += MovingRight ? JumpingRLDistance : (-1) * JumpingRLDistance;
            }

            shootBulletIfNeeded();
        }

        private void shootBulletIfNeeded()
        {
            bool timeForCheckRandomMachine = m_TotalSecondsForShoot > k_ShootingTime;

            if (timeForCheckRandomMachine)
            {
                m_TotalSecondsForShoot -= k_ShootingTime;

                if (!r_Bullet.Active && r_RandomMachine.Next(k_RandomRangeForShoot) == 1)
                {
                    r_Bullet.Shoot(Position);
                    s_GunShotSound.Play();
                }
            }
        }

        public void JumpDown()
        {
            Position += JumpingDownDistance;
        }

        public override void Collided(ICollidable i_Collidable)
        {
            if (i_Collidable is Bullet && (i_Collidable as Bullet).Type == eBulletType.Ship)
            {
                Dispose();
                s_EnemyKillSound.Play();
                AnimationsManager.PauseAllComosites();
                AnimationsManager["Ternimation"].Restart();
            }
        }

        private void terminationAnimation_Finished(object sender, EventArgs e)
        {
            Enabled = false;
            Visible = false;
            GameScreen.Remove(this);
            onHit();
        }

        private void onHit()
        {
            if (Hit != null)
            {
                Hit.Invoke();
            }
        }
    }
}
