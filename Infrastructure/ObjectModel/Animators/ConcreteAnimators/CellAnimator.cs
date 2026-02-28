//*** Guy Ronen © 2008-2011 ***//
using System;
using Microsoft.Xna.Framework;

namespace Infrastructure.ObjectModel.Animators.ConcreteAnimators
{
    public class CellAnimator : SpriteAnimator
    {
        private TimeSpan m_CellTime;
        private TimeSpan m_TimeLeftForCell;
        private bool m_Loop = true;
        private int m_CurrCellIdx = 0;
        private readonly int r_NumOfCells = 1;
        private readonly Enums.eDirection r_CellOrder;
        
        public TimeSpan CellTime { get { return m_CellTime; } set { m_CellTime = value; } }

        // CTORs
        public CellAnimator(TimeSpan i_CellTime, int i_NumOfCells, TimeSpan i_AnimationLength)
       : this(i_CellTime, i_NumOfCells, i_AnimationLength, Enums.eDirection.Horizontal)
        {
        }

        public CellAnimator(TimeSpan i_CellTime, int i_NumOfCells, TimeSpan i_AnimationLength, Enums.eDirection i_CellOrder)
            : this(i_CellTime, 0, i_NumOfCells, i_AnimationLength, i_CellOrder)
        { 
        }

        public CellAnimator(TimeSpan i_CellTime, int i_currCellIdx, int i_NumOfCells, TimeSpan i_AnimationLength, Enums.eDirection i_CellOrder)
            : base("CellAnimation", i_AnimationLength)
        {
            this.m_CellTime = i_CellTime;
            this.m_CurrCellIdx = i_currCellIdx;
            this.m_TimeLeftForCell = i_CellTime;
            this.r_NumOfCells = i_NumOfCells;
            this.r_CellOrder = i_CellOrder;

            m_Loop = i_AnimationLength == TimeSpan.Zero;
        }

        private void goToNextFrame()
        {
            m_CurrCellIdx++;
            if (m_CurrCellIdx >= r_NumOfCells)
            {
                if (m_Loop)
                {
                    m_CurrCellIdx = 0;
                }
                else
                {
                    m_CurrCellIdx = r_NumOfCells - 1; /// lets stop at the last frame
                    this.IsFinished = true;
                }
            }
        }

        protected override void RevertToOriginal()
        {
            this.BoundSprite.SourceRectangle = m_OriginalSpriteInfo.SourceRectangle;
        }

        protected override void DoFrame(GameTime i_GameTime)
        {
            if (m_CellTime != TimeSpan.Zero)
            {
                m_TimeLeftForCell -= i_GameTime.ElapsedGameTime;
                if (m_TimeLeftForCell.TotalSeconds <= 0)
                {
                    /// we have elapsed, so go to the next frame
                    goToNextFrame();
                    m_TimeLeftForCell = m_CellTime;
                }
            }

            if (r_CellOrder == Enums.eDirection.Horizontal)
            {
                this.BoundSprite.SourceRectangle = new Rectangle(
                    m_CurrCellIdx * this.BoundSprite.SourceRectangle.Width,
                    this.BoundSprite.SourceRectangle.Top,
                    this.BoundSprite.SourceRectangle.Width,
                    this.BoundSprite.SourceRectangle.Height);
            }
            else
            {
                this.BoundSprite.SourceRectangle = new Rectangle(
                    this.BoundSprite.SourceRectangle.Left,
                    m_CurrCellIdx * this.BoundSprite.SourceRectangle.Height,
                    this.BoundSprite.SourceRectangle.Width,
                    this.BoundSprite.SourceRectangle.Height);
            }
        }
    }
}
