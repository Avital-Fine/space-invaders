using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using Infrastructure.ObjectModel;
using Infrastructure.ObjectModel.Animators;
using Infrastructure.ObjectModel.Animators.ConcreteAnimators;
using Infrastructure.ObjectModel.Screens;
using Infrastructure.ServiceInterfaces;
using Invaders.Interfaces;

namespace Invaders.Sprites
{
    public class Ship : Sprite, ICollidable2D
    {
        public event Action<IHostile> HostileHitByShip; // the enemy or ms was hitted 
        public event EventHandler<EventArgs> ShipHitByEnemy; //the ship was hitted

        private const int k_MaxBullets = 2;
        private readonly Vector2 r_Velocity = new Vector2(140f, 0);
        private readonly HashSet<Bullet> r_BulletsPool;
        private readonly eShipType r_ShipType;
        private bool m_Alive = true;
        private int m_ActiveBullets;
        private bool m_FirstMovement = false;
        private Vector2 m_StartPosition;
        // Animations Information
        private const int k_RotationsPerSecond = 6;
        private const int k_BlinkPerSeconde = 8;
        private readonly TimeSpan r_BlinkLenght = TimeSpan.FromSeconds(1) / k_BlinkPerSeconde;
        private readonly TimeSpan r_BlinkAnimationLenght = TimeSpan.FromSeconds(2);
        private readonly TimeSpan r_TerminationAnimationLenght = TimeSpan.FromSeconds(2.6);
        // Keys
        private readonly Keys r_LeftButton;
        private readonly Keys r_RightButton;
        private readonly Keys r_ShootButton;
        // Sound
        private static readonly string sr_GunShotAssetSound = @"Sounds\SSGunShot";
        private static readonly string sr_LifeDieAssetSound = @"Sounds\LifeDie";
        private static SoundEffectInstance s_GunShotSound;        
        private static SoundEffectInstance s_LifeDieSound;
        private static bool s_SoundLoaded = false;

        public bool Alive
        {
            get { return m_Alive; }
            set
            {
                m_Alive = value;
                Velocity = Vector2.Zero;
            }
        }
        public eShipType ShipType { get { return r_ShipType; } }

        public Ship(GameScreen i_GameScreen, eShipType i_ShipType ,string i_AssertName, Keys i_RightTrigger, Keys i_LeftTrigger, Keys i_ShootTrigger) : base(i_AssertName, i_GameScreen)
        {
            r_BulletsPool = newBulletsPool();
            r_ShipType = i_ShipType;
            r_RightButton = i_RightTrigger;
            r_LeftButton = i_LeftTrigger;
            r_ShootButton = i_ShootTrigger;
        }

        private HashSet<Bullet> newBulletsPool()
        {
            HashSet<Bullet> poolBullet = new HashSet<Bullet>(k_MaxBullets);
            
            for (int i = 0; i < k_MaxBullets; i++)
            {
                Bullet newBullet = new Bullet(GameScreen, eBulletType.Ship);
                newBullet.RouteFinished += bullet_RouteFinished;
                newBullet.HostileCollided += bullet_HostileCollided;
                poolBullet.Add(newBullet);
            }

            return poolBullet;
        }

        public override void Initialize()
        {
            base.Initialize();

            m_ActiveBullets = 0;
            BaseGame.Window.ClientSizeChanged += window_ClientSizeChanged;
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            if (!s_SoundLoaded)
            {
                s_GunShotSound = BaseGame.SoundsManager.LoadSoundEffect(sr_GunShotAssetSound);
                s_LifeDieSound = BaseGame.SoundsManager.LoadSoundEffect(sr_LifeDieAssetSound);
                s_SoundLoaded = true;
            }
        }

        protected override void InitOrigins()
        {
            InitOriginsToCenter();
        }

        protected override void InitBounds()
        {
            base.InitBounds();

            m_StartPosition = new Vector2((int)ShipType * Width, Game.GraphicsDevice.Viewport.Height - Height);
            TopLeftPosition = m_StartPosition;
        }

        protected override void InitAnimations()
        {
            BlinkAnimator blinkAnimator = new BlinkAnimator(r_BlinkLenght, r_BlinkAnimationLenght);
            RotationAnimator rotationAnimator = new RotationAnimator(k_RotationsPerSecond, r_TerminationAnimationLenght);
            FadeAnimator fadeAnimator = new FadeAnimator(r_TerminationAnimationLenght);
            CompositeAnimator terminationAnimation = new CompositeAnimator("Ternimation", r_TerminationAnimationLenght, this, fadeAnimator, rotationAnimator);

            AnimationsManager.AddAndPause(blinkAnimator);
            blinkAnimator.Finished += blinkAnimator_Finished;
            terminationAnimation.Finished += terminationAnimation_Finished;
            AnimationsManager.AddAndPause(terminationAnimation);

            base.InitAnimations();
        }

        public override void Update(GameTime gameTime)
        {
            if (Alive)
            {
                moveIfNeeded();
                shootBulletIfNeeded();
            }

            base.Update(gameTime);
        }

        private void shootBulletIfNeeded()
        {
            if (BaseGame.InputManager.KeyPressed(r_ShootButton))
            {
                shootBullet();
            }

            if (ShipType == eShipType.Blue)
            {
                if (BaseGame.InputManager.ButtonPressed(eInputButtons.Left))
                {
                    shootBullet();
                }
            }
        }

        private void shootBullet()
        {
            if (m_ActiveBullets < k_MaxBullets)
            {
                foreach (Bullet bullet in r_BulletsPool)
                {
                    if (!bullet.Active)
                    {
                        bullet.Shoot(m_Position);
                        m_ActiveBullets++;
                        s_GunShotSound.Play();
                        break;
                    }
                }
            }
        }

        private void moveIfNeeded()
        {
            if(r_ShipType == eShipType.Blue)
            {
                mouseMovement();
            }

            if (BaseGame.InputManager.KeyHeld(r_LeftButton))
            {
                Velocity = (-1) * r_Velocity;
            }
            else if (BaseGame.InputManager.KeyHeld(r_RightButton))
            {
                Velocity = r_Velocity;
            }
            else
            {
                Velocity = Vector2.Zero;
            }

            float xPosition = MathHelper.Clamp(TopLeftPosition.X, 0, GraphicsDevice.Viewport.Width - WidthBeforeScale);
            TopLeftPosition = new Vector2(xPosition, TopLeftPosition.Y);
        }

        private void mouseMovement()
        {
            if (!m_FirstMovement && BaseGame.InputManager.PrevMouseState.X != m_StartPosition.X)
            {
                m_FirstMovement = true;
            }

            if (m_FirstMovement)
            {
                Vector2 deltaXMovement = new Vector2(BaseGame.InputManager.MousePositionDelta.X, 0);
                TopLeftPosition += deltaXMovement;
            }
        }

        public override void Collided(ICollidable i_Collidable)
        {
            if (Alive)
            {
                if (i_Collidable is Bullet && (i_Collidable as Bullet).Type == eBulletType.Enemy)
                {
                    (i_Collidable as Bullet).Deactivate();
                    onShipHitByEnemy();
                }
            }
        }

        private void bullet_HostileCollided(IHostile i_Hostile)
        {
            onHostileHitByShip(i_Hostile);
        }

        private void bullet_RouteFinished(object sender, EventArgs e)
        {
            if ((sender as Bullet).Type == eBulletType.Ship)
            {
                m_ActiveBullets--;
            }
        }

        private void window_ClientSizeChanged(object sender, EventArgs e)
        {
            float newXPos = Math.Clamp(TopLeftPosition.X, 0, Game.GraphicsDevice.Viewport.Width - Width);
            float newYPos = Game.GraphicsDevice.Viewport.Height - Height;
            m_StartPosition.Y = newYPos;
            TopLeftPosition = new Vector2(newXPos, newYPos);
        }

        private void blinkAnimator_Finished(object sender, EventArgs e)
        {
            Alive = true;
            TopLeftPosition = m_StartPosition;
        }

        private void terminationAnimation_Finished(object sender, EventArgs e)
        {
            foreach (Bullet bullet in r_BulletsPool)
            {
                bullet.Deactivate();
            }

            this.GameScreen.Remove(this);
        }

        private void onHostileHitByShip(IHostile i_Hostile)
        {
            if (HostileHitByShip != null)
            {
                HostileHitByShip.Invoke(i_Hostile);
            }
        }

        private void onShipHitByEnemy()
        {
            Alive = false;
            s_LifeDieSound.Play();

            if (ShipHitByEnemy != null)
            {
                ShipHitByEnemy.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
