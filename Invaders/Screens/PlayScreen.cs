using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Infrastructure;
using Infrastructure.ObjectModel.Screens;
using Invaders.Sprites;
using Invaders.Managers;

namespace Invaders.Screens
{
    public class PlayScreen : GameScreen
    {
        public event Action<bool> EndLevel;
        public event Action BackToDashboard;

        private static readonly string sr_GameOverAssetSound = @"Sounds\GameOver";
        private static SoundEffectInstance s_GameOverSound;
        private static bool s_SoundLoaded = false;
        private readonly Keys r_PauseTriggerP   = Keys.P;
        private readonly Keys r_PauseTriggerEsc = Keys.Escape;
        private readonly Keys r_MuteTrigger     = Keys.M;
        
        private int m_NumberOfPlayers = 1;
        private const int k_NumberOfLevels = 4;
        private const int k_NumberOfBarriers = 4;
        private const int k_ColsOfEnemies = 9;
        private const int k_RowsOfEnemies = 5;

        private readonly eLevel r_Level;
        private readonly GamePauseScreen r_GameInstructionsScreen;
        private readonly PlayersManager r_PlayersManager;
        private readonly EnemiesForce r_EnemiesForce;
        private readonly MotherShip r_MotherShip;
        private readonly BarriersRow r_BarriersRow;

        public PlayScreen(Game i_Game, int i_Level, eNumberOfPlayers i_NumberOfPlayers) 
            : base(i_Game)
        {
            m_NumberOfPlayers = (int)i_NumberOfPlayers;

            if (i_Level == 1)
            {
                PlayersManager.InitPlayersManager(m_NumberOfPlayers);
            }

            r_PlayersManager = new PlayersManager(this);
            r_Level = (eLevel)((i_Level - 1) % k_NumberOfLevels);
            r_EnemiesForce = new EnemiesForce(this, k_RowsOfEnemies, k_ColsOfEnemies + (int)r_Level);
            r_MotherShip = new MotherShip(this);
            r_BarriersRow = new BarriersRow(this, k_NumberOfBarriers);
            r_GameInstructionsScreen = new GamePauseScreen(i_Game);
        }


        public override void Initialize()
        {
            initEnemiesValues();
            r_EnemiesForce.EndLevel += enemiesForce_EndLevel;
            r_GameInstructionsScreen.BackToDashboard += onBackToDashboard;

            if (r_Level > eLevel.Level1)
            {
                Barrier.CommonVelocity += new Vector2((float)(35f * Math.Pow(1.06, (int)r_Level - 1)), Barrier.CommonVelocity.Y);
            }
            else
            {
                Barrier.CommonVelocity = Vector2.Zero;
            }

            base.Initialize();

            initEnemyForceLowerLimit();
            initBarriersRowPosition();
            Game.Window.ClientSizeChanged += window_ClientSizeChanged;
            this.BlendState = BlendState.NonPremultiplied;
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            if (!s_SoundLoaded)
            {
                s_GameOverSound = (Game as BaseGame).SoundsManager.LoadSoundEffect(sr_GameOverAssetSound);
                s_SoundLoaded = true;
            }
        }

        private void initEnemiesValues()
        {
            Enemy.PinkEnemyValue = 300 + 100 * ((int)r_Level);
            Enemy.BlueEnemyValue = 200 + 100 * ((int)r_Level);
            Enemy.YellowEnemyValue = 70 + 100 * ((int)r_Level);
        }

        private void initBarriersRowPosition()
        {
            Barrier TempBarrier = r_BarriersRow.Barriers[0];
            float centerScreen = GraphicsDevice.Viewport.Width / 2;
            float barrierDistance = TempBarrier.Width * (2.3f * k_NumberOfBarriers - 1.3f);
            float startXPosition = centerScreen - barrierDistance / 2;
            float startYPosition = GraphicsDevice.Viewport.Height - Player.ShipHeigth * 2 - TempBarrier.Height;

            r_BarriersRow.StartRowPosition = new Vector2(startXPosition, startYPosition);
            r_BarriersRow.PlaceBarriers();
        }

        private void initEnemyForceLowerLimit()
        {
            r_EnemiesForce.LowerLimit = GraphicsDevice.Viewport.Height - Player.ShipHeigth;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (InputManager.KeyPressed(r_PauseTriggerP) || InputManager.KeyPressed(r_PauseTriggerEsc))
            {
                ScreensManager.SetCurrentScreen(r_GameInstructionsScreen);
            }

            if (InputManager.KeyPressed(r_MuteTrigger))
            {
                (Game as BaseGame).SoundsManager.MuteToggle();
            }
        }

        private void window_ClientSizeChanged(object sender, EventArgs e)
        {
            initBarriersRowPosition();
            initEnemyForceLowerLimit();
        }

        private void enemiesForce_EndLevel(bool i_IsWin)
        {
            onEndLevel(i_IsWin);
        }

        public void GameOver()
        {
            s_GameOverSound.Play();
            const bool v_Win = true;
            onEndLevel(!v_Win);
        }

        private void onEndLevel(bool i_IsWin)
        {
            r_PlayersManager.UpdateInfo();
            ExitScreen();

            if (EndLevel != null)
            {
                EndLevel.Invoke(i_IsWin);
            }
        }

        private void onBackToDashboard()
        {
            ExitScreen();
            BackToDashboard?.Invoke();
        }
    }
}
