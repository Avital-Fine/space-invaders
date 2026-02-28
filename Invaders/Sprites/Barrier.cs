using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Infrastructure.ObjectModel;
using Infrastructure.ObjectModel.Screens;
using Infrastructure.ServiceInterfaces;
using static Infrastructure.Enums;

namespace Invaders.Sprites
{
    public class Barrier : Sprite, ICollidable2D
    {
        private const float k_BitePercentage = 0.35f;
        private static readonly string sr_AssetName = @"Sprites\Barrier_44x32";
        private static readonly string sr_BarrierHitAssetSound = @"Sounds\BarrierHit";
        private static SoundEffectInstance s_BarrierHitSound;
        private static Vector2 s_Velocity;
        private static bool s_SoundLoaded = false;
        private readonly int r_BarrierIdx;
        private float m_RightLimit = 0;
        private float m_LeftLimit = 0;

        public float RightLimit { get { return m_RightLimit; } set { m_RightLimit = value; } }
        public float LeftLimit { get { return m_LeftLimit; } set { m_LeftLimit = value; } }
        public static Vector2 CommonVelocity { get { return s_Velocity; } set { s_Velocity = value; } }

        public Barrier(GameScreen i_GameScreen, int i_Idx) : base(sr_AssetName, i_GameScreen)
        {
            r_BarrierIdx = i_Idx;
            CollisionBased = eCollisionBased.PixelsBased;
        }

        public override void Initialize()
        {
            base.Initialize();

            Velocity = CommonVelocity;
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            if (!s_SoundLoaded)
            {
                s_BarrierHitSound = BaseGame.SoundsManager.LoadSoundEffect(sr_BarrierHitAssetSound);
                s_SoundLoaded = true;
            }

            Texture = new Texture2D(Game.GraphicsDevice, Texture.Width, Texture.Height);
            Texture.SetData(PixelsData);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (TopLeftPosition.X >= RightLimit || TopLeftPosition.X <= LeftLimit)
            {
                Velocity *= -1;
            }
        }

        public override void Collided(ICollidable i_Collidable)
        {
            if (i_Collidable is Bullet)
            {
                bulletCollision(i_Collidable as Bullet);
            }
            else if (i_Collidable is Enemy)
            {
                enemyCollision(i_Collidable as Enemy);
            }
        }

        private void enemyCollision(Enemy i_Enemy)
        {
            int leftLimit = Math.Max(this.Bounds.X, i_Enemy.Bounds.X) - (int)this.TopLeftPosition.X;
            int rightLimit = Math.Min(this.Bounds.X + this.Bounds.Width, i_Enemy.Bounds.X + i_Enemy.Bounds.Width) - (int)this.TopLeftPosition.X;
            int topLimit = Math.Max(this.Bounds.Y, i_Enemy.Bounds.Y) - (int)this.TopLeftPosition.Y;
            int downLimit = Math.Min(this.Bounds.Y + this.Bounds.Height, i_Enemy.Bounds.Y + i_Enemy.Bounds.Height) - (int)this.TopLeftPosition.Y;

            for (int y = topLimit; y < downLimit; ++y)
            {
                for (int x = leftLimit; x < rightLimit; ++x)
                {
                    ErasePixel(y, x);
                }
            }

            Texture.SetData(PixelsData);
        }

        private void bulletCollision(Bullet i_Bullet)
        {
            int biteHeight = (int)(i_Bullet.Height * k_BitePercentage);
            int leftLimit = MathHelper.Clamp((int)(i_Bullet.TopLeftPosition.X - TopLeftPosition.X), 0, (int)Width);
            int rightLimit = MathHelper.Clamp((int)(i_Bullet.TopLeftPosition.X + i_Bullet.Width - TopLeftPosition.X), 0, (int)Width);
            int topLimit = 0;
            int downLimit = (int)this.Height;
            bool found = false;

            if (i_Bullet.FallingDown)
            {
                for (int x = leftLimit; x < rightLimit; ++x)
                {
                    for (int y = topLimit; y < downLimit; ++y)
                    {
                        if (!found && GetPixelAt(y, x).A != 0)
                        { 
                            downLimit = Math.Min(downLimit, y + biteHeight);
                            found = true;
                        }

                        ErasePixel(y, x);
                    }
                }
            }
            else
            {
                for (int x = leftLimit; x < rightLimit; ++x)
                {
                    for (int y = downLimit - 1; y >= topLimit; --y)
                    {
                        if (!found && GetPixelAt(y, x).A != 0)
                        {
                            topLimit = MathHelper.Clamp(Math.Min(downLimit, y - biteHeight),0, downLimit);
                            found = true;
                        }

                        ErasePixel(y, x);
                    }
                }
            }

            if (found)
            {
                s_BarrierHitSound.Play();
                Texture.SetData(PixelsData);
                i_Bullet.Deactivate();
            }
        }
    }
}
