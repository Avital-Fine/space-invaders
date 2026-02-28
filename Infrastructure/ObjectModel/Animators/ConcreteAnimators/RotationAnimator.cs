using Microsoft.Xna.Framework;
using System;

namespace Infrastructure.ObjectModel.Animators.ConcreteAnimators
{
    public class RotationAnimator : SpriteAnimator
    {
        private int m_RotationsPerSecond;

        // CTORs
        public RotationAnimator(string i_Name, int i_RotationsPerSecond, TimeSpan i_AnimationLength)
            : base(i_Name, i_AnimationLength)
        {
            m_RotationsPerSecond = i_RotationsPerSecond;
        }

        public RotationAnimator(int i_RotationsPerSecond, TimeSpan i_AnimationLength)
            : this("Rotate",i_RotationsPerSecond, i_AnimationLength)
        {
        }

        protected override void DoFrame(GameTime i_GameTime)
        {
            this.BoundSprite.Rotation += m_RotationsPerSecond * MathHelper.TwoPi 
                * (float)i_GameTime.ElapsedGameTime.TotalSeconds;
        }

        protected override void RevertToOriginal()
        {
            this.BoundSprite.Rotation = m_OriginalSpriteInfo.Rotation;
        }
    }
}
