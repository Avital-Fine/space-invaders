using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Infrastructure;
using Infrastructure.ObjectModel.Screens;

namespace Invaders.Sprites
{
    public class EnemiesForce : GameComponent
    {
        public event Action<bool> EndLevel;

        private static readonly string sr_LevelWinAssetSound = @"Sounds\LevelWin";
        private static SoundEffectInstance s_LevelWinSound;
        private readonly EnemyCell[,] r_MatrixEnemies;
        private readonly int r_ColsOfEnemies;
        private readonly int r_RowsOfEnemies;
        private EnemyCell m_RightGuard;
        private EnemyCell m_LeftGuard;
        private EnemyCell m_DownGuard;
        private Vector2 m_ForceJumpingDistance;
        private int m_ActiveEnemies;
        private bool m_TouchingTheWall = false;
        private float m_LowerLimit;
        
        public float LowerLimit { get { return m_LowerLimit; } set { m_LowerLimit = value; } }

        public EnemiesForce(GameScreen i_GameScreen, int i_Rows = 5, int i_Cols = 9) : base(i_GameScreen.Game)
        {
            r_RowsOfEnemies = i_Rows;
            r_ColsOfEnemies = i_Cols;
            r_MatrixEnemies = newMatrixEnemies(i_GameScreen);
            i_GameScreen.Add(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            delegateGuards();
            initCommonInfo();
            m_ActiveEnemies = r_ColsOfEnemies * r_RowsOfEnemies;
            m_LowerLimit = Game.GraphicsDevice.Viewport.Height;
            s_LevelWinSound = (Game as BaseGame).SoundsManager.LoadSoundEffect(sr_LevelWinAssetSound);
            Game.Window.ClientSizeChanged += window_ClientSizeChanged;
        }

        private void initCommonInfo()
        {
            m_ForceJumpingDistance = new Vector2(m_RightGuard.WidthBeforeScale / 2, 0);
            Enemy.JumpingRLDistance = m_ForceJumpingDistance;
            Enemy.JumpingDownDistance = new Vector2(0, m_RightGuard.HeightBeforeScale / 2);
            Enemy.MovingRight = true;
            Enemy.JumpingTime = Enemy.StartJumpingTime;
        }

        private void updateEnemiesPos(Vector2 i_DeltaToReduce)
        {
            for (int i = 0; i < r_RowsOfEnemies; i++)
            {
                for (int j = 0; j < r_ColsOfEnemies; j++)
                {
                    if (r_MatrixEnemies[i, j].Enabled)
                    {
                        r_MatrixEnemies[i, j].TopLeftPosition -= i_DeltaToReduce;
                    } 
                }
            }
        }

        private EnemyCell[,] newMatrixEnemies(GameScreen i_GameScreen)
        {
            EnemyCell[,] matrixEnemies = new EnemyCell[r_RowsOfEnemies, r_ColsOfEnemies];
            EnemyCell newEnemy;

            for (int i = 0; i < r_RowsOfEnemies; i++)
            {
                for (int j = 0; j < r_ColsOfEnemies; j++)
                {
                    if (i == 0)
                    {
                        newEnemy = new EnemyCell(i_GameScreen, eEnemyType.Pink, i, j);
                    }
                    else if (i == 1 || i == 2)
                    {
                        newEnemy = new EnemyCell(i_GameScreen, eEnemyType.Blue, i, j);
                    }
                    else
                    {
                        newEnemy = new EnemyCell(i_GameScreen, eEnemyType.Yellow, i, j);
                    }

                    newEnemy.Hit += enemy_Hit;
                    matrixEnemies[i, j] = newEnemy;
                }
            }
            
            return matrixEnemies;
        }

        private void delegateGuards()
        {
            delegateRightGuard();
            delegateLeftGuard();
            delegateDownGuard();
        }

        private void delegateRightGuard()
        {
            EnemyCell rightMostEnemy = null;
            bool found = false;

            for (int j = r_ColsOfEnemies - 1; j >= 0 && !found; j--)
            {
                for (int i = 0; i < r_RowsOfEnemies && !found; i++)
                {
                    rightMostEnemy = delegateGuard(i, j, out found);
                }
            }

            m_RightGuard = rightMostEnemy;
            m_RightGuard.Hit += delegateRightGuard;
        }

        private void delegateLeftGuard()
        {
            EnemyCell leftMostEnemy = null;
            bool found = false;

            for (int j = 0; j < r_ColsOfEnemies && !found; j++)
            {
                for (int i = 0; i < r_RowsOfEnemies && !found; i++)
                {
                    leftMostEnemy = delegateGuard(i, j, out found);
                }
            }

            m_LeftGuard = leftMostEnemy;
            m_LeftGuard.Hit += delegateLeftGuard;
        }

        private void delegateDownGuard()
        {
            EnemyCell downMostEnemy = null;
            bool found = false;

            for (int i = r_RowsOfEnemies - 1; i >= 0 && !found; i--)
            {
                for (int j = 0; j < r_ColsOfEnemies && !found; j++)
                {
                    downMostEnemy = delegateGuard(i, j, out found);
                }
            }

            m_DownGuard = downMostEnemy;
            m_DownGuard.Hit += delegateDownGuard;
            m_DownGuard.PositionChanged += downGuard_CheckLowerLimit;
        }

        private EnemyCell delegateGuard(int i_RowIndex, int i_ColIndex, out bool o_Found)
        {
            EnemyCell currentEnemy = r_MatrixEnemies[i_RowIndex, i_ColIndex];
            o_Found = currentEnemy.Enabled;                
            return currentEnemy;
        }

        public override void Update(GameTime i_gameTime)
        {
            base.Update(i_gameTime);

            if (m_TouchingTheWall)
            {
                m_TouchingTheWall = false;
                touchingTheWallHandle();
            }
            else if(collidedBordersDetection())
            {
                 m_TouchingTheWall = true;
            }
        }

        private bool collidedBordersDetection()
        {
            bool collided = false;

            if (Enemy.MovingRight)
            {
                Vector2 rightGaurdNextPosition = m_RightGuard.TopLeftPosition + Enemy.JumpingRLDistance;

                if (rightGaurdNextPosition.X + m_RightGuard.WidthBeforeScale >= Game.GraphicsDevice.Viewport.Width)
                {
                    collided = true;
                    float newXJumpingStep = Game.GraphicsDevice.Viewport.Width - m_RightGuard.Position.X + m_RightGuard.WidthBeforeScale;
                    Enemy.JumpingRLDistance = new Vector2(newXJumpingStep, Enemy.JumpingRLDistance.Y);
                }
            }
            else
            {
                Vector2 leftGaurdNextPosition = m_LeftGuard.TopLeftPosition - Enemy.JumpingRLDistance;

                if (leftGaurdNextPosition.X < 0)
                {
                    collided = true;
                    float newXJumpingStep = (-1) * m_LeftGuard.TopLeftPosition.X;
                    Enemy.JumpingRLDistance = new Vector2(newXJumpingStep, Enemy.JumpingRLDistance.Y);
                }
            }

            return collided;
        }

        private void touchingTheWallHandle()
        {
            for (int i = 0; i < r_RowsOfEnemies; i++)
            {
                for (int j = 0; j < r_ColsOfEnemies; j++)
                {
                    r_MatrixEnemies[i, j].JumpDown();
                    r_MatrixEnemies[i, j].SynchronizCellAnimator();
                }
            }
            
            Enemy.MovingRight = !Enemy.MovingRight;
            Enemy.JumpingRLDistance = m_ForceJumpingDistance;
            Enemy.JumpingTime *= 0.95f;
        }

        private void downGuard_CheckLowerLimit(object sender)
        {
            bool reachedLowerLimit = m_DownGuard.TopLeftPosition.Y + m_DownGuard.Height >= LowerLimit;

            if (reachedLowerLimit)
            {
                gameOver();
            }
        }

        private void enemy_Hit()
        {
            m_ActiveEnemies--;

            if (m_ActiveEnemies == 0)
            {
                levelUp();
            }
        }

        private void window_ClientSizeChanged(object sender, EventArgs e)
        {
            if (m_RightGuard.TopLeftPosition.X + m_RightGuard.Width >= Game.GraphicsDevice.Viewport.Width)
            {
                float reduceDeltaX = m_RightGuard.TopLeftPosition.X + m_RightGuard.Width - Game.GraphicsDevice.Viewport.Width;
                updateEnemiesPos(new Vector2(reduceDeltaX, 0));
            }
        }

        private void gameOver()
        {
            const bool v_Win = true;
            onEndLevel(!v_Win);
        }

        private void levelUp()
        {
            s_LevelWinSound.Play();
            const bool v_Win = true;
            onEndLevel(v_Win);
        }

        private void onEndLevel(bool i_IsWin)
        {
            this.Game.Components.Remove(this);

            if (EndLevel != null)
            {
                EndLevel.Invoke(i_IsWin);
            }
        }
    }
}
