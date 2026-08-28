using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Invaders.IcyTower
{
    public class IcyTowerPlayer
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Width { get; } = 26f;
        public float Height { get; } = 38f;

        public bool IsGrounded { get; set; }
        public bool IsSuperJumping { get; set; }
        public bool FacingRight { get; set; } = true;
        public float SpinAngle { get; set; }
        public int LastFloorIndex { get; set; } = 0;

        // Physics Constants
        public const float k_Acceleration = 1200f;
        public const float k_MaxSpeed = 460f;
        public const float k_GroundFriction = 1000f;
        public const float k_AirFriction = 120f;
        public const float k_Gravity = 1350f;
        public const float k_BaseJumpSpeed = 540f;
        public const float k_SuperJumpSpeedMax = 920f;

        // Animation tracking
        private float m_AnimTimer;
        private float m_TrailTimer;
        private readonly Vector2[] m_GhostTrail = new Vector2[4];
        private int m_TrailIndex = 0;

        public Rectangle Bounds => new Rectangle((int)(Position.X - Width / 2f), (int)(Position.Y - Height), (int)Width, (int)Height);

        public IcyTowerPlayer(Vector2 i_SpawnPos)
        {
            Position = i_SpawnPos;
            Velocity = Vector2.Zero;
            IsGrounded = true;
            IsSuperJumping = false;
            SpinAngle = 0f;
            LastFloorIndex = 0;

            for (int i = 0; i < m_GhostTrail.Length; i++)
            {
                m_GhostTrail[i] = i_SpawnPos;
            }
        }

        public void Update(float i_Dt, float i_MoveInput, bool i_JumpPressed, float i_LeftWall, float i_RightWall, Action i_OnBounce, Action<bool> i_OnJump)
        {
            // 1. Horizontal Acceleration & Friction
            if (Math.Abs(i_MoveInput) > 0.05f)
            {
                Velocity.X += i_MoveInput * k_Acceleration * i_Dt;
                Velocity.X = Math.Clamp(Velocity.X, -k_MaxSpeed, k_MaxSpeed);
                FacingRight = i_MoveInput > 0;
            }
            else
            {
                float friction = IsGrounded ? k_GroundFriction : k_AirFriction;
                if (Velocity.X > 0)
                {
                    Velocity.X = Math.Max(0, Velocity.X - friction * i_Dt);
                }
                else if (Velocity.X < 0)
                {
                    Velocity.X = Math.Min(0, Velocity.X + friction * i_Dt);
                }
            }

            // 2. Jumping
            if (i_JumpPressed && IsGrounded)
            {
                float speedRatio = Math.Abs(Velocity.X) / k_MaxSpeed;
                bool isSuper = speedRatio > 0.5f;

                float jumpPower = MathHelper.Lerp(k_BaseJumpSpeed, k_SuperJumpSpeedMax, speedRatio);
                Velocity.Y = -jumpPower;
                IsGrounded = false;
                IsSuperJumping = isSuper;
                SpinAngle = 0f;

                i_OnJump?.Invoke(isSuper);
            }

            // 3. Gravity
            if (!IsGrounded)
            {
                Velocity.Y += k_Gravity * i_Dt;
                if (IsSuperJumping)
                {
                    float spinSpeed = (FacingRight ? 1f : -1f) * MathHelper.TwoPi * (3f + (Math.Abs(Velocity.X) / k_MaxSpeed) * 3.5f);
                    SpinAngle += spinSpeed * i_Dt;
                }
            }
            else
            {
                SpinAngle = 0f;
                IsSuperJumping = false;
            }

            // 4. Apply Velocity to Position
            Position += Velocity * i_Dt;

            // 5. Wall Collisions & Bouncing
            float halfW = Width / 2f;
            if (Position.X - halfW < i_LeftWall)
            {
                Position.X = i_LeftWall + halfW;
                if (Velocity.X < -60f)
                {
                    Velocity.X = -Velocity.X * 1.08f; // Wall bounce boost
                    FacingRight = true;
                    i_OnBounce?.Invoke();
                }
                else
                {
                    Velocity.X = 0;
                }
            }
            else if (Position.X + halfW > i_RightWall)
            {
                Position.X = i_RightWall - halfW;
                if (Velocity.X > 60f)
                {
                    Velocity.X = -Velocity.X * 1.08f; // Wall bounce boost
                    FacingRight = false;
                    i_OnBounce?.Invoke();
                }
                else
                {
                    Velocity.X = 0;
                }
            }

            // 6. Animation and Trail Updates
            m_AnimTimer += i_Dt * (Math.Abs(Velocity.X) / 25f);

            m_TrailTimer += i_Dt;
            if (m_TrailTimer >= 0.035f)
            {
                m_TrailTimer = 0f;
                m_GhostTrail[m_TrailIndex] = Position;
                m_TrailIndex = (m_TrailIndex + 1) % m_GhostTrail.Length;
            }
        }

        public void LandOn(IcyTowerPlatform i_Platform)
        {
            Position.Y = i_Platform.Y;
            Velocity.Y = 0;
            IsGrounded = true;
            IsSuperJumping = false;
            SpinAngle = 0f;
            LastFloorIndex = i_Platform.FloorIndex;
        }

        public void Draw(SpriteBatch i_Batch, Texture2D i_Pixel, Vector2 i_ScreenPos)
        {
            // Draw speed trail when super jumping or sprinting
            if (IsSuperJumping || Math.Abs(Velocity.X) > 260f)
            {
                for (int i = 0; i < m_GhostTrail.Length; i++)
                {
                    float alpha = (i + 1f) / (m_GhostTrail.Length + 1f) * 0.35f;
                    Vector2 trailOffset = m_GhostTrail[i] - Position;
                    drawCharacterModel(i_Batch, i_Pixel, i_ScreenPos + trailOffset, alpha * 0.6f, SpinAngle);
                }
            }

            // Draw Main Character
            drawCharacterModel(i_Batch, i_Pixel, i_ScreenPos, 1.0f, SpinAngle);
        }

        private void drawCharacterModel(SpriteBatch i_Batch, Texture2D i_Pixel, Vector2 i_Pos, float i_Alpha, float i_Angle)
        {
            Color capColor = new Color(230, 45, 45) * i_Alpha;       // Red beanie / cap
            Color skinColor = new Color(255, 205, 160) * i_Alpha;    // Face skin
            Color shirtColor = new Color(30, 140, 220) * i_Alpha;    // Cyan/blue jacket
            Color pantsColor = new Color(40, 50, 75) * i_Alpha;      // Dark navy jeans
            Color shoesColor = new Color(240, 240, 240) * i_Alpha;   // White sneakers
            Color outlineColor = new Color(15, 15, 20) * i_Alpha;

            int w = (int)Width;
            int h = (int)Height;
            int left = (int)(i_Pos.X - w / 2f);
            int top = (int)(i_Pos.Y - h);

            if (Math.Abs(i_Angle) > 0.05f)
            {
                // Somersault rotation (draw rolling orb)
                Rectangle ballRect = new Rectangle((int)(i_Pos.X - 15), (int)(i_Pos.Y - h / 2f - 15), 30, 30);
                i_Batch.Draw(i_Pixel, ballRect, shirtColor);
                drawRectOutline(i_Batch, i_Pixel, ballRect, capColor, 3);
                return;
            }

            // Legs running animation stride offset
            int legStride = (IsGrounded && Math.Abs(Velocity.X) > 20f) ? (int)(Math.Sin(m_AnimTimer) * 4f) : 0;

            // 1. Cap / Beanie (Top 10px)
            Rectangle capRect = new Rectangle(left + 2, top, w - 4, 10);
            i_Batch.Draw(i_Pixel, capRect, capColor);
            drawRectOutline(i_Batch, i_Pixel, capRect, outlineColor, 1);

            // 2. Face / Head (Next 10px)
            Rectangle faceRect = new Rectangle(left + 3, top + 10, w - 6, 10);
            i_Batch.Draw(i_Pixel, faceRect, skinColor);

            // Eyes looking in facing direction
            int eyeX = FacingRight ? (left + w - 8) : (left + 5);
            i_Batch.Draw(i_Pixel, new Rectangle(eyeX, top + 13, 3, 3), Color.Black * i_Alpha);
            drawRectOutline(i_Batch, i_Pixel, faceRect, outlineColor, 1);

            // 3. Jacket / Torso (Next 12px)
            Rectangle torsoRect = new Rectangle(left + 1, top + 20, w - 2, 11);
            i_Batch.Draw(i_Pixel, torsoRect, shirtColor);
            drawRectOutline(i_Batch, i_Pixel, torsoRect, outlineColor, 1);

            // 4. Pants & Shoes (Bottom 7px)
            Rectangle leftLeg = new Rectangle(left + 3 - legStride, top + 31, 8, 5);
            Rectangle rightLeg = new Rectangle(left + w - 11 + legStride, top + 31, 8, 5);
            i_Batch.Draw(i_Pixel, leftLeg, pantsColor);
            i_Batch.Draw(i_Pixel, rightLeg, pantsColor);

            Rectangle leftShoe = new Rectangle(left + 2 - legStride, top + 35, 9, 3);
            Rectangle rightShoe = new Rectangle(left + w - 12 + legStride, top + 35, 9, 3);
            i_Batch.Draw(i_Pixel, leftShoe, shoesColor);
            i_Batch.Draw(i_Pixel, rightShoe, shoesColor);
        }

        private void drawRectOutline(SpriteBatch i_Batch, Texture2D i_Pixel, Rectangle i_Rect, Color i_Color, int i_Thickness)
        {
            i_Batch.Draw(i_Pixel, new Rectangle(i_Rect.X, i_Rect.Y, i_Rect.Width, i_Thickness), i_Color);
            i_Batch.Draw(i_Pixel, new Rectangle(i_Rect.X, i_Rect.Bottom - i_Thickness, i_Rect.Width, i_Thickness), i_Color);
            i_Batch.Draw(i_Pixel, new Rectangle(i_Rect.X, i_Rect.Y, i_Thickness, i_Rect.Height), i_Color);
            i_Batch.Draw(i_Pixel, new Rectangle(i_Rect.Right - i_Thickness, i_Rect.Y, i_Thickness, i_Rect.Height), i_Color);
        }
    }
}
