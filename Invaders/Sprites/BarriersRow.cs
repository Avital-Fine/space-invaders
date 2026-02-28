using Microsoft.Xna.Framework;
using Infrastructure.ObjectModel;
using Infrastructure.ObjectModel.Screens;

namespace Invaders.Sprites
{
    public class BarriersRow : RegisteredComponent
    {
        private readonly int r_NumberOfBarriers;
        private readonly Barrier[] r_Barriers;
        private Vector2 m_StartRowPosition = Vector2.Zero; 

        public Vector2 StartRowPosition { get { return m_StartRowPosition; } set { m_StartRowPosition = value; } }
        public Barrier[] Barriers { get { return r_Barriers; } }

        public BarriersRow(GameScreen i_GameScreen, int i_NumberOfBarriers) : base(i_GameScreen.Game)
        {
            r_NumberOfBarriers = i_NumberOfBarriers;
            r_Barriers = newBarriers(i_GameScreen);
        }

        private Barrier[] newBarriers(GameScreen i_GameScreen)
        {
            Barrier[] newBarriers = new Barrier[r_NumberOfBarriers];

            for (int i = 0; i < r_NumberOfBarriers; i++)
            {
                newBarriers[i] = new Barrier(i_GameScreen, i);
            }

            return newBarriers;
        }

        public void PlaceBarriers()
        {
            float barrierWidth;
            Vector2 newPosition;

            for (int i = 0; i < r_NumberOfBarriers; i++)
            {
                barrierWidth = Barriers[i].Width;
                newPosition = StartRowPosition + new Vector2(i * barrierWidth * 2.3f, 0);
                r_Barriers[i].TopLeftPosition = newPosition;
                r_Barriers[i].RightLimit = newPosition.X + barrierWidth / 2;
                r_Barriers[i].LeftLimit = newPosition.X - barrierWidth / 2;
            }
        }
    }
}
