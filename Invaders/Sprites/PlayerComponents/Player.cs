using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Infrastructure.ObjectModel;
using Infrastructure.ObjectModel.Screens;
using Invaders.Interfaces;

namespace Invaders.Sprites
{
    public class Player : GameComponent
    {
        public event EventHandler<EventArgs> DonePlaying;

        private const string k_BlueAssetName = @"Sprites\Ship01_32x32";
        private const Keys k_BlueLeftTrigger = Keys.Left;
        private const Keys k_BlueRightTrigger = Keys.Right;
        private const Keys k_BlueShootTrigger = Keys.Space;

        private const string k_GreenAssetName = @"Sprites\Ship02_32x32";
        private const Keys k_GreenLeftTrigger = Keys.W;
        private const Keys k_GreenRightTrigger = Keys.R;
        private const Keys k_GreenShootTrigger = Keys.D3;

        private readonly Ship r_Ship;
        private readonly Live[] r_Lives;
        private readonly int r_Index;
        private readonly Text r_Text;
        private int m_Score;
        private int m_RemainingLives;
        private static float s_ShipHeigth;

        public static float ShipHeigth { get { return s_ShipHeigth; } }
        public int Score { get { return m_Score; } }
        public int Lives { get { return m_RemainingLives; } }
        public Ship Ship { get { return r_Ship; } }

        public Player(GameScreen i_GameScreen, eShipType i_ShipType, int i_NumberOfLives, int i_Index, int i_Score) : base(i_GameScreen.Game)
        {
            m_RemainingLives = i_NumberOfLives;
            m_Score = i_Score;
            r_Ship = m_RemainingLives > 0 ? newShip(i_ShipType, i_GameScreen) : null;
            r_Lives = newLives(i_GameScreen);
            r_Index = i_Index;
            r_Text = new Text(i_GameScreen, "Consolas");
            i_GameScreen.Add(this);
        }

        private Ship newShip(eShipType i_ShipType, GameScreen i_GameScreen)
        {
            Ship ship = null;

            switch (i_ShipType)
            {
                case (eShipType.Blue):
                    {
                        ship = new Ship(i_GameScreen, i_ShipType, k_BlueAssetName, k_BlueRightTrigger, k_BlueLeftTrigger, k_BlueShootTrigger);
                        break;
                    }
                case (eShipType.Green):
                    {
                        ship = new Ship(i_GameScreen, i_ShipType, k_GreenAssetName, k_GreenRightTrigger, k_GreenLeftTrigger, k_GreenShootTrigger);
                        break;
                    }
            }

            ship.HostileHitByShip += ship_HostileEliminated;
            ship.ShipHitByEnemy += ship_ShipHitByEnemy;

            return ship;
        }

        private Live[] newLives(GameScreen i_GameScreen)
        {
            Live[] newLivesArray = new Live[m_RemainingLives];

            for (int i = 0; i < m_RemainingLives; i++)
            {
                newLivesArray[i] = new Live(this, i_GameScreen, i);
            }

            return newLivesArray;
        }

        public override void Initialize()
        {
            initText();

            if(r_Ship != null)
            {
                s_ShipHeigth = r_Ship.Height;
            }
        }

        private void initText()
        {
            if (r_Index == 1)
            {
                r_Text.Position = new Vector2(3, 0);
                r_Text.TintColor = Color.LightSteelBlue;
            }
            else if (r_Index == 2)
            {
                r_Text.Position = new Vector2(3, ShipHeigth / 2);
                r_Text.TintColor = Color.LightGreen;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            r_Text.Content = string.Format("P{0} Score: {1}", r_Index, m_Score);
        }

        private void ship_ShipHitByEnemy(object sender, EventArgs e)
        {
            m_RemainingLives--;
            m_Score = Math.Clamp(m_Score - 600, 0, m_Score);
            r_Lives[m_RemainingLives].RemoveFromScreen();

            if (m_RemainingLives > 0)
            {
                r_Ship.AnimationsManager["Blink"].Restart();
            }
            else
            {
                r_Ship.Dispose();
                r_Ship.AnimationsManager["Ternimation"].Restart();
                onDonePlaying();
            }
        }

        private void ship_HostileEliminated(IHostile i_Hostile)
        {
            m_Score += i_Hostile.Value;
        }

        private void onDonePlaying()
        {
            if (DonePlaying != null)
            {
                DonePlaying.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
