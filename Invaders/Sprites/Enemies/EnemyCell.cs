using System;
using Microsoft.Xna.Framework;
using Infrastructure;
using Infrastructure.ObjectModel.Animators.ConcreteAnimators;
using Infrastructure.ObjectModel.Screens;

namespace Invaders.Sprites
{
    public class EnemyCell : Enemy
    {
        private readonly int r_RowIndex;
        private readonly int r_ColIndex;
        private CellAnimator m_CellAnimator;

        public EnemyCell(GameScreen i_GameScreen, eEnemyType i_Type, int i_RowIndex, int i_ColIndex) : base(i_GameScreen, i_Type)
        {
            r_RowIndex = i_RowIndex;
            r_ColIndex = i_ColIndex;
        }

        public override void Initialize()
        {
            base.Initialize();

            TopLeftPosition = new Vector2(r_ColIndex * Width * 1.6f, Height * 3 + r_RowIndex * Height * 1.6f);
        }

        protected override void InitAnimations()
        {
            base.InitAnimations();

            m_CellAnimator = new CellAnimator(TimeSpan.FromSeconds(StartJumpingTime), r_RowIndex % 2, sr_NumberOfCells, TimeSpan.Zero, Enums.eDirection.Horizontal);
            AnimationsManager.Add(m_CellAnimator);
        }

        public void SynchronizCellAnimator()
        {
            m_CellAnimator.CellTime = TimeSpan.FromSeconds(JumpingTime);
        }
    }
}
