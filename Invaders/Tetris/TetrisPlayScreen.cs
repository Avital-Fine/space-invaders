using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Infrastructure;
using Infrastructure.Managers;
using Infrastructure.ObjectModel.Screens;
using Invaders.Screens;
using Invaders.Managers;

namespace Invaders.Tetris
{
    public class TetrisPlayScreen : GameScreen
    {
        public event Action BackToDashboard;

        private const string k_FontName = @"Fonts\Consolas";
        private const int k_CellSize = 27; // Cell pixel size (10x20 = 270x540)
        private const int k_PreviewCellSize = 18; // Preview box cell size

        // Sound asset names
        private const string k_SoundMove       = @"Sounds\MenuMove";
        private const string k_SoundRotate     = @"Sounds\SSGunShot";
        private const string k_SoundLock       = @"Sounds\BarrierHit";
        private const string k_SoundClear      = @"Sounds\EnemyKill";
        private const string k_SoundTetris     = @"Sounds\LevelWin";
        private const string k_SoundGameOver   = @"Sounds\GameOver";

        // Sound effect instances
        private SoundEffectInstance m_SoundMoveInstance;
        private SoundEffectInstance m_SoundRotateInstance;
        private SoundEffectInstance m_SoundLockInstance;
        private SoundEffectInstance m_SoundClearInstance;
        private SoundEffectInstance m_SoundTetrisInstance;
        private SoundEffectInstance m_SoundGameOverInstance;

        // Core Game Components
        private readonly TetrisBoard m_Board;
        private readonly TetrisPieceBag m_Bag;
        private readonly ScoresDatabase m_ScoresDatabase;

        private Tetromino m_CurrentPiece;
        private TetrominoType? m_HeldPieceType;
        private bool m_CanHold;
        private bool m_IsGameOver;
        private bool m_IsPaused;
        private bool m_IsClearingLines;

        // In-game pause menu
        private int m_PauseMenuIndex = 0;
        private static readonly string[] s_PauseMenuItems = new string[]
        {
            "Resume",
            "Mute / Unmute",
            "Leaderboard",
            "Restart Game",
            "Quit to Dashboard"
        };

        // Direct hardware input polling (ensures keyboard & gamepad work under all focus states)
        private KeyboardState m_CurrKeyboard;
        private KeyboardState m_PrevKeyboard;
        private GamePadState m_CurrGamePad;
        private GamePadState m_PrevGamePad;

        // Scoring & Progression
        private int m_Score;
        private int m_HighScore;
        private int m_Level;
        private int m_LinesCleared;
        private string m_PlayerName = "Player";

        // Timing & Input Handling
        private float m_FallTimer;
        private float m_LockTimer;
        private const float k_LockDelay = 0.5f;
        private int m_LockMoveCount;
        private const int k_MaxLockMoves = 15;

        // Line clear flash animation
        private List<int> m_ClearingLines = new List<int>();
        private float m_LineClearFlashTimer;
        private const float k_LineClearFlashDuration = 0.18f;

        // DAS (Delayed Auto Shift) for smooth movement
        private float m_DasLeftTimer;
        private float m_DasRightTimer;
        private float m_DasDownTimer;
        private const float k_DasInitialDelay = 0.16f;
        private const float k_DasRepeatRate = 0.045f;
        private const float k_SoftDropRate = 0.04f;

        // Graphics resources
        private Texture2D m_PixelTexture;
        private SpriteFont m_Font;

        public TetrisPlayScreen(Game i_Game) : base(i_Game)
        {
            m_Board = new TetrisBoard();
            m_Bag = new TetrisPieceBag();
            m_ScoresDatabase = new ScoresDatabase();
        }

        public override void Initialize()
        {
            base.Initialize();

            m_CurrKeyboard = m_PrevKeyboard = Keyboard.GetState();
            m_CurrGamePad = m_PrevGamePad = GamePad.GetState(PlayerIndex.One);

            loadHighScore();

            if (PlayersManager.PlayerNames != null && PlayersManager.PlayerNames.Length > 0 && !string.IsNullOrEmpty(PlayersManager.PlayerNames[0]))
            {
                m_PlayerName = PlayersManager.PlayerNames[0];
            }

            restartGame();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            m_Font = Game.Content.Load<SpriteFont>(k_FontName);

            // Create 1x1 white texture for procedural rendering
            m_PixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            m_PixelTexture.SetData(new[] { Color.White });

            // Load sounds
            SoundsManager sm = (Game as BaseGame)?.SoundsManager;
            if (sm != null)
            {
                m_SoundMoveInstance     = sm.LoadSoundEffect(k_SoundMove);
                m_SoundRotateInstance   = sm.LoadSoundEffect(k_SoundRotate);
                m_SoundLockInstance     = sm.LoadSoundEffect(k_SoundLock);
                m_SoundClearInstance    = sm.LoadSoundEffect(k_SoundClear);
                m_SoundTetrisInstance   = sm.LoadSoundEffect(k_SoundTetris);
                m_SoundGameOverInstance = sm.LoadSoundEffect(k_SoundGameOver);
            }
        }

        private void loadHighScore()
        {
            try
            {
                List<ScoreEntry> topScores = m_ScoresDatabase.GetTopScores(1, "Tetris");
                if (topScores != null && topScores.Count > 0)
                {
                    m_HighScore = topScores[0].Score;
                }
            }
            catch
            {
                m_HighScore = 0;
            }
        }

        private void restartGame()
        {
            m_Board.Clear();
            m_Bag.Reset();
            m_HeldPieceType = null;
            m_CanHold = true;
            m_Score = 0;
            m_Level = 1;
            m_LinesCleared = 0;
            m_IsGameOver = false;
            m_IsPaused = false;
            m_PauseMenuIndex = 0;
            m_IsClearingLines = false;
            m_ClearingLines.Clear();
            m_LockMoveCount = 0;
            m_LockTimer = 0f;
            m_FallTimer = 0f;

            spawnNextPiece();
        }

        private void spawnNextPiece()
        {
            m_CurrentPiece = m_Bag.NextPiece();
            m_CurrentPiece.ResetSpawnPosition();
            m_CanHold = true;
            m_LockTimer = 0f;
            m_LockMoveCount = 0;

            // Check immediate collision on spawn -> Game Over
            if (!m_Board.IsValidPosition(m_CurrentPiece, m_CurrentPiece.Position, m_CurrentPiece.RotationIndex))
            {
                triggerGameOver();
            }
        }

        private void triggerGameOver()
        {
            m_IsGameOver = true;
            m_SoundGameOverInstance?.Play();

            if (m_Score > m_HighScore)
            {
                m_HighScore = m_Score;
            }

            try
            {
                m_ScoresDatabase.SaveScore(m_PlayerName, m_Score, m_Level, "Tetris");
            }
            catch { }
        }

        private float getGravityInterval()
        {
            // Level 1: ~0.8s, accelerating smoothly as level rises
            return Math.Max(0.045f, (float)Math.Pow(0.85f - ((m_Level - 1) * 0.007f), m_Level - 1) * 0.8f);
        }

        #region Input Helper Methods (Keyboard & GamePad)

        private void pollInputStates()
        {
            m_PrevKeyboard = m_CurrKeyboard;
            m_CurrKeyboard = Keyboard.GetState();

            m_PrevGamePad = m_CurrGamePad;
            m_CurrGamePad = GamePad.GetState(PlayerIndex.One);
        }

        private bool isKeyPressed(Keys i_Key)
        {
            bool direct = m_CurrKeyboard.IsKeyDown(i_Key) && m_PrevKeyboard.IsKeyUp(i_Key);
            bool viaManager = InputManager != null && InputManager.KeyPressed(i_Key);
            return direct || viaManager;
        }

        private bool isKeyHeld(Keys i_Key)
        {
            bool direct = m_CurrKeyboard.IsKeyDown(i_Key);
            bool viaManager = InputManager != null && InputManager.KeyHeld(i_Key);
            return direct || viaManager;
        }

        private bool isButtonPressed(Buttons i_Button)
        {
            return m_CurrGamePad.IsButtonDown(i_Button) && m_PrevGamePad.IsButtonUp(i_Button);
        }

        private bool isButtonHeld(Buttons i_Button)
        {
            return m_CurrGamePad.IsButtonDown(i_Button);
        }

        private bool isMoveLeftPressed()
        {
            bool key = isKeyPressed(Keys.Left) || isKeyPressed(Keys.A) || isKeyPressed(Keys.NumPad4);
            bool pad = isButtonPressed(Buttons.DPadLeft) || (m_CurrGamePad.ThumbSticks.Left.X < -0.5f && m_PrevGamePad.ThumbSticks.Left.X >= -0.5f);
            return key || pad;
        }

        private bool isMoveLeftHeld()
        {
            bool key = isKeyHeld(Keys.Left) || isKeyHeld(Keys.A) || isKeyHeld(Keys.NumPad4);
            bool pad = isButtonHeld(Buttons.DPadLeft) || (m_CurrGamePad.ThumbSticks.Left.X < -0.5f);
            return key || pad;
        }

        private bool isMoveRightPressed()
        {
            bool key = isKeyPressed(Keys.Right) || isKeyPressed(Keys.D) || isKeyPressed(Keys.NumPad6);
            bool pad = isButtonPressed(Buttons.DPadRight) || (m_CurrGamePad.ThumbSticks.Left.X > 0.5f && m_PrevGamePad.ThumbSticks.Left.X <= 0.5f);
            return key || pad;
        }

        private bool isMoveRightHeld()
        {
            bool key = isKeyHeld(Keys.Right) || isKeyHeld(Keys.D) || isKeyHeld(Keys.NumPad6);
            bool pad = isButtonHeld(Buttons.DPadRight) || (m_CurrGamePad.ThumbSticks.Left.X > 0.5f);
            return key || pad;
        }

        private bool isSoftDropPressed()
        {
            bool key = isKeyPressed(Keys.Down) || isKeyPressed(Keys.S) || isKeyPressed(Keys.NumPad2);
            bool pad = isButtonPressed(Buttons.DPadDown) || (m_CurrGamePad.ThumbSticks.Left.Y < -0.5f && m_PrevGamePad.ThumbSticks.Left.Y >= -0.5f);
            return key || pad;
        }

        private bool isSoftDropHeld()
        {
            bool key = isKeyHeld(Keys.Down) || isKeyHeld(Keys.S) || isKeyHeld(Keys.NumPad2);
            bool pad = isButtonHeld(Buttons.DPadDown) || (m_CurrGamePad.ThumbSticks.Left.Y < -0.5f);
            return key || pad;
        }

        private bool isRotateCWPressed()
        {
            bool key = isKeyPressed(Keys.Up) || isKeyPressed(Keys.X) || isKeyPressed(Keys.W) || isKeyPressed(Keys.E) || isKeyPressed(Keys.NumPad1) || isKeyPressed(Keys.NumPad9);
            bool pad = isButtonPressed(Buttons.A) || isButtonPressed(Buttons.DPadUp);
            return key || pad;
        }

        private bool isRotateCCWPressed()
        {
            bool key = isKeyPressed(Keys.Z) || isKeyPressed(Keys.Q) || isKeyPressed(Keys.NumPad3) || isKeyPressed(Keys.NumPad7);
            bool pad = isButtonPressed(Buttons.B);
            return key || pad;
        }

        private bool isHardDropPressed()
        {
            bool key = isKeyPressed(Keys.Space) || isKeyPressed(Keys.NumPad8);
            bool pad = isButtonPressed(Buttons.Y) || isButtonPressed(Buttons.RightTrigger);
            return key || pad;
        }

        private bool isHoldPressed()
        {
            bool key = isKeyPressed(Keys.C) || isKeyPressed(Keys.LeftShift) || isKeyPressed(Keys.RightShift) || isKeyPressed(Keys.LeftControl);
            bool pad = isButtonPressed(Buttons.LeftShoulder) || isButtonPressed(Buttons.RightShoulder) || isButtonPressed(Buttons.X);
            return key || pad;
        }

        private bool isPausePressed()
        {
            bool key = isKeyPressed(Keys.P) || isKeyPressed(Keys.Escape);
            bool pad = isButtonPressed(Buttons.Start) || isButtonPressed(Buttons.Back);
            return key || pad;
        }

        private bool isEscOrBack()
        {
            bool key = isKeyPressed(Keys.Escape) || isKeyPressed(Keys.Back) || isKeyPressed(Keys.Q);
            bool pad = isButtonPressed(Buttons.Back) || isButtonPressed(Buttons.B);
            return key || pad;
        }

        private bool isMutePressed()
        {
            return isKeyPressed(Keys.M);
        }

        private bool isConfirmPressed()
        {
            bool key = isKeyPressed(Keys.Enter) || isKeyPressed(Keys.Space);
            bool pad = isButtonPressed(Buttons.A) || isButtonPressed(Buttons.Start);
            return key || pad;
        }

        private bool isRestartPressed()
        {
            bool key = isKeyPressed(Keys.Enter) || isKeyPressed(Keys.Space) || isKeyPressed(Keys.R);
            bool pad = isButtonPressed(Buttons.A) || isButtonPressed(Buttons.Start);
            return key || pad;
        }

        #endregion

        public override void Update(GameTime i_GameTime)
        {
            base.Update(i_GameTime);

            pollInputStates();

            float dt = (float)i_GameTime.ElapsedGameTime.TotalSeconds;

            // Global mute shortcut
            if (isMutePressed())
            {
                (Game as BaseGame)?.SoundsManager.MuteToggle();
            }

            // 1. GAME OVER STATE
            if (m_IsGameOver)
            {
                if (isKeyPressed(Keys.L) || isButtonPressed(Buttons.Y) || isButtonPressed(Buttons.X))
                {
                    ScreensManager.SetCurrentScreen(new LeaderboardScreen(Game, 1));
                    return;
                }
                if (isEscOrBack())
                {
                    onBackToDashboard();
                    return;
                }
                if (isRestartPressed())
                {
                    restartGame();
                    return;
                }
                return;
            }

            // 2. PAUSE MENU STATE
            if (m_IsPaused)
            {
                updatePauseMenu();
                return;
            }

            // 3. PAUSE TRIGGER
            if (isPausePressed())
            {
                m_IsPaused = true;
                m_PauseMenuIndex = 0;
                m_SoundMoveInstance?.Play();
                return;
            }

            // 4. LINE CLEAR FLASH ANIMATION
            if (m_IsClearingLines)
            {
                m_LineClearFlashTimer -= dt;
                if (m_LineClearFlashTimer <= 0)
                {
                    m_Board.ClearLines(m_ClearingLines);
                    m_ClearingLines.Clear();
                    m_IsClearingLines = false;
                    spawnNextPiece();
                }
                return;
            }

            // 5. ACTIVE GAMEPLAY
            handlePlayerInput(dt);
            updatePiecePhysics(dt);
        }

        private void updatePauseMenu()
        {
            // Unpause / Resume with Esc, P, or Start
            if (isKeyPressed(Keys.Escape) || isKeyPressed(Keys.P) || isButtonPressed(Buttons.Start))
            {
                m_IsPaused = false;
                m_SoundMoveInstance?.Play();
                return;
            }

            // Quick quit with Q
            if (isKeyPressed(Keys.Q))
            {
                onBackToDashboard();
                return;
            }

            // Navigate up
            if (isKeyPressed(Keys.Up) || isKeyPressed(Keys.W) || isButtonPressed(Buttons.DPadUp) || (m_CurrGamePad.ThumbSticks.Left.Y > 0.5f && m_PrevGamePad.ThumbSticks.Left.Y <= 0.5f))
            {
                m_PauseMenuIndex = (m_PauseMenuIndex + s_PauseMenuItems.Length - 1) % s_PauseMenuItems.Length;
                m_SoundMoveInstance?.Play();
            }
            // Navigate down
            else if (isKeyPressed(Keys.Down) || isKeyPressed(Keys.S) || isButtonPressed(Buttons.DPadDown) || (m_CurrGamePad.ThumbSticks.Left.Y < -0.5f && m_PrevGamePad.ThumbSticks.Left.Y >= -0.5f))
            {
                m_PauseMenuIndex = (m_PauseMenuIndex + 1) % s_PauseMenuItems.Length;
                m_SoundMoveInstance?.Play();
            }

            // Select option
            if (isConfirmPressed())
            {
                executePauseMenuOption(m_PauseMenuIndex);
            }
        }

        private void executePauseMenuOption(int i_Index)
        {
            switch (i_Index)
            {
                case 0: // Resume
                    m_IsPaused = false;
                    m_SoundMoveInstance?.Play();
                    break;
                case 1: // Mute / Unmute
                    (Game as BaseGame)?.SoundsManager.MuteToggle();
                    m_SoundMoveInstance?.Play();
                    break;
                case 2: // Leaderboard
                    ScreensManager.SetCurrentScreen(new LeaderboardScreen(Game, 1));
                    break;
                case 3: // Restart Game
                    restartGame();
                    break;
                case 4: // Quit to Dashboard
                    onBackToDashboard();
                    break;
            }
        }

        private void handlePlayerInput(float i_Dt)
        {
            if (m_CurrentPiece == null) return;

            // 1. Hold Piece (C, Shift, LB/RB/X)
            if (isHoldPressed())
            {
                if (m_CanHold)
                {
                    performHold();
                    return;
                }
            }

            // 2. Rotations (CW: Up / X / W / A-Button, CCW: Z / Q / B-Button)
            if (isRotateCWPressed())
            {
                if (m_Board.TryRotate(m_CurrentPiece, true))
                {
                    m_SoundRotateInstance?.Play();
                    onPieceMovedWhileGrounded();
                }
            }
            else if (isRotateCCWPressed())
            {
                if (m_Board.TryRotate(m_CurrentPiece, false))
                {
                    m_SoundRotateInstance?.Play();
                    onPieceMovedWhileGrounded();
                }
            }

            // 3. Hard Drop (Space / Y / RT)
            if (isHardDropPressed())
            {
                performHardDrop();
                return;
            }

            // 4. Horizontal Movement (Left / Right with DAS)
            handleHorizontalMovement(i_Dt);

            // 5. Soft Drop (Down with tap or hold)
            handleSoftDrop(i_Dt);
        }

        private void performHold()
        {
            m_CanHold = false;
            TetrominoType currentType = m_CurrentPiece.Type;

            if (m_HeldPieceType.HasValue)
            {
                m_CurrentPiece = new Tetromino(m_HeldPieceType.Value);
                m_CurrentPiece.ResetSpawnPosition();
            }
            else
            {
                m_CurrentPiece = m_Bag.NextPiece();
                m_CurrentPiece.ResetSpawnPosition();
            }

            m_HeldPieceType = currentType;
            m_LockTimer = 0f;
            m_LockMoveCount = 0;
            m_SoundMoveInstance?.Play();
        }

        private void performHardDrop()
        {
            Point ghost = m_Board.GetGhostPosition(m_CurrentPiece);
            int dropDistance = ghost.Y - m_CurrentPiece.Position.Y;
            m_CurrentPiece.Position = ghost;
            m_Score += dropDistance * 2;

            m_SoundLockInstance?.Play();
            lockCurrentPiece();
        }

        private void handleHorizontalMovement(float i_Dt)
        {
            // Move Left
            if (isMoveLeftPressed())
            {
                if (m_Board.TryMove(m_CurrentPiece, new Point(-1, 0)))
                {
                    m_SoundMoveInstance?.Play();
                    onPieceMovedWhileGrounded();
                }
                m_DasLeftTimer = k_DasInitialDelay;
            }
            else if (isMoveLeftHeld())
            {
                m_DasLeftTimer -= i_Dt;
                if (m_DasLeftTimer <= 0)
                {
                    if (m_Board.TryMove(m_CurrentPiece, new Point(-1, 0)))
                    {
                        m_SoundMoveInstance?.Play();
                        onPieceMovedWhileGrounded();
                    }
                    m_DasLeftTimer = k_DasRepeatRate;
                }
            }

            // Move Right
            if (isMoveRightPressed())
            {
                if (m_Board.TryMove(m_CurrentPiece, new Point(1, 0)))
                {
                    m_SoundMoveInstance?.Play();
                    onPieceMovedWhileGrounded();
                }
                m_DasRightTimer = k_DasInitialDelay;
            }
            else if (isMoveRightHeld())
            {
                m_DasRightTimer -= i_Dt;
                if (m_DasRightTimer <= 0)
                {
                    if (m_Board.TryMove(m_CurrentPiece, new Point(1, 0)))
                    {
                        m_SoundMoveInstance?.Play();
                        onPieceMovedWhileGrounded();
                    }
                    m_DasRightTimer = k_DasRepeatRate;
                }
            }
        }

        private void handleSoftDrop(float i_Dt)
        {
            if (isSoftDropPressed())
            {
                if (m_Board.TryMove(m_CurrentPiece, new Point(0, 1)))
                {
                    m_Score += 1;
                    m_FallTimer = 0f;
                }
                m_DasDownTimer = k_SoftDropRate;
            }
            else if (isSoftDropHeld())
            {
                m_DasDownTimer -= i_Dt;
                if (m_DasDownTimer <= 0)
                {
                    if (m_Board.TryMove(m_CurrentPiece, new Point(0, 1)))
                    {
                        m_Score += 1;
                        m_FallTimer = 0f;
                    }
                    m_DasDownTimer = k_SoftDropRate;
                }
            }
            else
            {
                m_DasDownTimer = 0f;
            }
        }

        private void updatePiecePhysics(float i_Dt)
        {
            if (m_CurrentPiece == null) return;

            bool isGrounded = !m_Board.IsValidPosition(m_CurrentPiece, new Point(m_CurrentPiece.Position.X, m_CurrentPiece.Position.Y + 1), m_CurrentPiece.RotationIndex);

            if (isGrounded)
            {
                m_LockTimer += i_Dt;
                if (m_LockTimer >= k_LockDelay)
                {
                    m_SoundLockInstance?.Play();
                    lockCurrentPiece();
                }
            }
            else
            {
                m_LockTimer = 0f;
                m_FallTimer += i_Dt;
                if (m_FallTimer >= getGravityInterval())
                {
                    m_FallTimer = 0f;
                    m_Board.TryMove(m_CurrentPiece, new Point(0, 1));
                }
            }
        }

        private void onPieceMovedWhileGrounded()
        {
            bool isGrounded = !m_Board.IsValidPosition(m_CurrentPiece, new Point(m_CurrentPiece.Position.X, m_CurrentPiece.Position.Y + 1), m_CurrentPiece.RotationIndex);
            if (isGrounded && m_LockMoveCount < k_MaxLockMoves)
            {
                m_LockTimer = 0f; // Reset lock delay
                m_LockMoveCount++;
            }
        }

        private void lockCurrentPiece()
        {
            bool topOut = m_Board.LockPiece(m_CurrentPiece);
            if (topOut)
            {
                triggerGameOver();
                return;
            }

            // Check completed lines
            m_ClearingLines = m_Board.FindFullLines();
            if (m_ClearingLines.Count > 0)
            {
                int linesCount = m_ClearingLines.Count;
                int lineScore = 0;
                switch (linesCount)
                {
                    case 1: lineScore = 100 * m_Level; break;
                    case 2: lineScore = 300 * m_Level; break;
                    case 3: lineScore = 500 * m_Level; break;
                    case 4: lineScore = 800 * m_Level; break;
                }
                m_Score += lineScore;
                m_LinesCleared += linesCount;

                // Level up every 10 lines
                m_Level = (m_LinesCleared / 10) + 1;

                if (linesCount == 4)
                {
                    m_SoundTetrisInstance?.Play();
                }
                else
                {
                    m_SoundClearInstance?.Play();
                }

                m_IsClearingLines = true;
                m_LineClearFlashTimer = k_LineClearFlashDuration;
            }
            else
            {
                spawnNextPiece();
            }
        }

        private void onBackToDashboard()
        {
            ExitScreen();
            BackToDashboard?.Invoke();
        }

        #region Drawing Logic

        public override void Draw(GameTime i_GameTime)
        {
            base.Draw(i_GameTime);

            Viewport vp = GraphicsDevice.Viewport;
            int boardPixelWidth = TetrisBoard.Columns * k_CellSize;
            int boardPixelHeight = TetrisBoard.Rows * k_CellSize;

            int boardX = (vp.Width - boardPixelWidth) / 2;
            int boardY = (vp.Height - boardPixelHeight) / 2 + 10;

            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // Draw full background gradient/backdrop
            drawDarkBackdrop(vp);

            // Draw Board Frame & Background
            drawBoardBackground(boardX, boardY, boardPixelWidth, boardPixelHeight);

            // Draw Locked Blocks on the Grid
            drawGridBlocks(boardX, boardY);

            // Draw Ghost Piece
            if (m_CurrentPiece != null && !m_IsGameOver && !m_IsClearingLines)
            {
                drawGhostPiece(boardX, boardY);
            }

            // Draw Active Piece
            if (m_CurrentPiece != null && !m_IsGameOver)
            {
                drawActivePiece(boardX, boardY);
            }

            // Draw Line Clear Flash Animation
            if (m_IsClearingLines)
            {
                drawLineClearFlash(boardX, boardY, boardPixelWidth);
            }

            // Draw Left Panel (Hold Box & Controls)
            drawLeftPanel(boardX - 165, boardY);

            // Draw Right Panel (Next Queue & Stats)
            drawRightPanel(boardX + boardPixelWidth + 25, boardY);

            // Draw In-Game Pause Overlay
            if (m_IsPaused)
            {
                drawPauseOverlay(boardX, boardY, boardPixelWidth, boardPixelHeight);
            }

            // Draw Game Over Overlay
            if (m_IsGameOver)
            {
                drawGameOverOverlay(boardX, boardY, boardPixelWidth, boardPixelHeight);
            }

            SpriteBatch.End();
        }

        private void drawDarkBackdrop(Viewport i_Vp)
        {
            // Deep cosmic arcade backdrop
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(0, 0, i_Vp.Width, i_Vp.Height), new Color(10, 12, 20, 255));
        }

        private void drawBoardBackground(int i_X, int i_Y, int i_Width, int i_Height)
        {
            // Outer shadow / glow border
            drawRectOutline(new Rectangle(i_X - 3, i_Y - 3, i_Width + 6, i_Height + 6), new Color(60, 80, 140, 180), 3);

            // Matrix background
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_X, i_Y, i_Width, i_Height), new Color(14, 16, 28, 240));

            // Subtle grid lines
            Color gridLineColor = new Color(35, 40, 65, 100);
            for (int col = 1; col < TetrisBoard.Columns; col++)
            {
                SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_X + col * k_CellSize, i_Y, 1, i_Height), gridLineColor);
            }
            for (int row = 1; row < TetrisBoard.Rows; row++)
            {
                SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_X, i_Y + row * k_CellSize, i_Width, 1), gridLineColor);
            }
        }

        private void drawGridBlocks(int i_BoardX, int i_BoardY)
        {
            for (int x = 0; x < TetrisBoard.Columns; x++)
            {
                for (int y = 0; y < TetrisBoard.Rows; y++)
                {
                    Color? blockColor = m_Board.Grid[x, y];
                    if (blockColor.HasValue)
                    {
                        Rectangle blockRect = new Rectangle(i_BoardX + x * k_CellSize, i_BoardY + y * k_CellSize, k_CellSize, k_CellSize);
                        drawBeveledBlock(blockRect, blockColor.Value, 1f);
                    }
                }
            }
        }

        private void drawGhostPiece(int i_BoardX, int i_BoardY)
        {
            Point ghostPos = m_Board.GetGhostPosition(m_CurrentPiece);
            Point[] localBlocks = m_CurrentPiece.GetLocalBlockPositions();

            for (int i = 0; i < 4; i++)
            {
                int x = ghostPos.X + localBlocks[i].X;
                int y = ghostPos.Y + localBlocks[i].Y;

                if (y >= 0 && y < TetrisBoard.Rows && x >= 0 && x < TetrisBoard.Columns)
                {
                    Rectangle blockRect = new Rectangle(i_BoardX + x * k_CellSize, i_BoardY + y * k_CellSize, k_CellSize, k_CellSize);
                    drawGhostBlock(blockRect, m_CurrentPiece.Color);
                }
            }
        }

        private void drawActivePiece(int i_BoardX, int i_BoardY)
        {
            Point[] blocks = m_CurrentPiece.GetCurrentBlockPositions();
            for (int i = 0; i < blocks.Length; i++)
            {
                int x = blocks[i].X;
                int y = blocks[i].Y;

                if (y >= 0 && y < TetrisBoard.Rows && x >= 0 && x < TetrisBoard.Columns)
                {
                    Rectangle blockRect = new Rectangle(i_BoardX + x * k_CellSize, i_BoardY + y * k_CellSize, k_CellSize, k_CellSize);
                    drawBeveledBlock(blockRect, m_CurrentPiece.Color, 1f);
                }
            }
        }

        private void drawLineClearFlash(int i_BoardX, int i_BoardY, int i_BoardWidth)
        {
            foreach (int row in m_ClearingLines)
            {
                Rectangle flashRect = new Rectangle(i_BoardX, i_BoardY + row * k_CellSize, i_BoardWidth, k_CellSize);
                SpriteBatch.Draw(m_PixelTexture, flashRect, Color.White * 0.85f);
            }
        }

        private void drawLeftPanel(int i_X, int i_Y)
        {
            int panelWidth = 145;

            // 1. HOLD Box
            drawCard(new Rectangle(i_X, i_Y, panelWidth, 110), "HOLD");
            if (m_HeldPieceType.HasValue)
            {
                Color pieceColor = TetrominoColors.GetColor(m_HeldPieceType.Value);
                if (!m_CanHold)
                {
                    pieceColor = Color.Gray; // Dimmed when locked
                }
                drawPreviewPiece(m_HeldPieceType.Value, pieceColor, i_X + panelWidth / 2, i_Y + 62);
            }

            // 2. CONTROLS Box
            int controlsY = i_Y + 125;
            drawCard(new Rectangle(i_X, controlsY, panelWidth, 310), "CONTROLS");

            string[] controls = new string[]
            {
                "Left/Right : Move",
                "Down / S   : Drop",
                "Space / Y  : Hard",
                "Up / X / A : Rot CW",
                "Z / B      : Rot CCW",
                "C / Shift  : Hold",
                "Esc / P    : Pause",
                "M          : Mute"
            };

            float lineY = controlsY + 38;
            foreach (string line in controls)
            {
                SpriteBatch.DrawString(m_Font, line, new Vector2(i_X + 10, lineY), new Color(180, 195, 220), 0f, Vector2.Zero, 0.62f, SpriteEffects.None, 0f);
                lineY += 32;
            }
        }

        private void drawRightPanel(int i_X, int i_Y)
        {
            int panelWidth = 150;

            // 1. NEXT QUEUE Box
            drawCard(new Rectangle(i_X, i_Y, panelWidth, 230), "NEXT");
            TetrominoType[] nextPieces = m_Bag.PeekNext(3);
            for (int i = 0; i < nextPieces.Length; i++)
            {
                Color pieceColor = TetrominoColors.GetColor(nextPieces[i]);
                drawPreviewPiece(nextPieces[i], pieceColor, i_X + panelWidth / 2, i_Y + 52 + i * 62);
            }

            // 2. STATS Box
            int statsY = i_Y + 245;
            drawCard(new Rectangle(i_X, statsY, panelWidth, 240), "STATS");

            drawStatEntry("SCORE", m_Score.ToString("N0"), i_X + 12, statsY + 38, Color.Gold);
            drawStatEntry("HIGH", m_HighScore.ToString("N0"), i_X + 12, statsY + 88, new Color(255, 160, 60));
            drawStatEntry("LEVEL", m_Level.ToString(), i_X + 12, statsY + 138, Color.Cyan);
            drawStatEntry("LINES", m_LinesCleared.ToString(), i_X + 12, statsY + 188, Color.LightGreen);
        }

        private void drawStatEntry(string i_Label, string i_Value, int i_X, int i_Y, Color i_ValColor)
        {
            SpriteBatch.DrawString(m_Font, i_Label, new Vector2(i_X, i_Y), new Color(150, 160, 185), 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
            SpriteBatch.DrawString(m_Font, i_Value, new Vector2(i_X, i_Y + 16), i_ValColor, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
        }

        private void drawPreviewPiece(TetrominoType i_Type, Color i_Color, int i_CenterX, int i_CenterY)
        {
            Tetromino dummy = new Tetromino(i_Type);
            Point[] localBlocks = dummy.GetLocalBlockPositions(0);

            // Compute center of bounding box to align perfectly
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (Point p in localBlocks)
            {
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

            float pieceWidth = (maxX - minX + 1) * k_PreviewCellSize;
            float pieceHeight = (maxY - minY + 1) * k_PreviewCellSize;

            float startX = i_CenterX - pieceWidth / 2f - minX * k_PreviewCellSize;
            float startY = i_CenterY - pieceHeight / 2f - minY * k_PreviewCellSize;

            foreach (Point p in localBlocks)
            {
                Rectangle rect = new Rectangle((int)(startX + p.X * k_PreviewCellSize), (int)(startY + p.Y * k_PreviewCellSize), k_PreviewCellSize, k_PreviewCellSize);
                drawBeveledBlock(rect, i_Color, 1f);
            }
        }

        private void drawCard(Rectangle i_Bounds, string i_Header)
        {
            // Dark card body
            SpriteBatch.Draw(m_PixelTexture, i_Bounds, new Color(18, 22, 36, 230));

            // Card border
            drawRectOutline(i_Bounds, new Color(55, 65, 100, 200), 2);

            // Header banner
            Rectangle headerRect = new Rectangle(i_Bounds.X, i_Bounds.Y, i_Bounds.Width, 24);
            SpriteBatch.Draw(m_PixelTexture, headerRect, new Color(30, 36, 58, 240));

            Vector2 textSize = m_Font.MeasureString(i_Header) * 0.7f;
            Vector2 textPos = new Vector2(i_Bounds.X + (i_Bounds.Width - textSize.X) / 2, i_Bounds.Y + 3);
            SpriteBatch.DrawString(m_Font, i_Header, textPos, Color.Gold, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }

        private void drawBeveledBlock(Rectangle i_Rect, Color i_BaseColor, float i_Alpha)
        {
            Color bodyColor = i_BaseColor * i_Alpha;
            Color lightBevel = Color.Lerp(i_BaseColor, Color.White, 0.45f) * i_Alpha;
            Color darkBevel = Color.Lerp(i_BaseColor, Color.Black, 0.45f) * i_Alpha;
            Color borderColor = new Color(10, 10, 10, 220) * i_Alpha;

            // Main body
            SpriteBatch.Draw(m_PixelTexture, i_Rect, bodyColor);

            int bevel = Math.Max(1, i_Rect.Width / 8);

            // Top highlight
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_Rect.X, i_Rect.Y, i_Rect.Width, bevel), lightBevel);
            // Left highlight
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_Rect.X, i_Rect.Y, bevel, i_Rect.Height), lightBevel);
            // Right shadow
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_Rect.Right - bevel, i_Rect.Y, bevel, i_Rect.Height), darkBevel);
            // Bottom shadow
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_Rect.X, i_Rect.Bottom - bevel, i_Rect.Width, bevel), darkBevel);

            // Outer 1px edge
            drawRectOutline(i_Rect, borderColor, 1);
        }

        private void drawGhostBlock(Rectangle i_Rect, Color i_BaseColor)
        {
            // Semi-transparent ghost block with subtle inner tint
            SpriteBatch.Draw(m_PixelTexture, i_Rect, i_BaseColor * 0.2f);
            drawRectOutline(i_Rect, i_BaseColor * 0.7f, 1);
        }

        private void drawRectOutline(Rectangle i_Rect, Color i_Color, int i_Thickness)
        {
            // Top
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_Rect.X, i_Rect.Y, i_Rect.Width, i_Thickness), i_Color);
            // Bottom
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_Rect.X, i_Rect.Bottom - i_Thickness, i_Rect.Width, i_Thickness), i_Color);
            // Left
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_Rect.X, i_Rect.Y, i_Thickness, i_Rect.Height), i_Color);
            // Right
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_Rect.Right - i_Thickness, i_Rect.Y, i_Thickness, i_Rect.Height), i_Color);
        }

        private void drawPauseOverlay(int i_BoardX, int i_BoardY, int i_Width, int i_Height)
        {
            // Dark modal cover
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_BoardX, i_BoardY, i_Width, i_Height), new Color(5, 8, 18, 225));

            int cardWidth = 240;
            int cardHeight = 255;
            int cardX = i_BoardX + (i_Width - cardWidth) / 2;
            int cardY = i_BoardY + (i_Height - cardHeight) / 2;

            Rectangle cardRect = new Rectangle(cardX, cardY, cardWidth, cardHeight);
            SpriteBatch.Draw(m_PixelTexture, cardRect, new Color(20, 24, 40, 250));
            drawRectOutline(cardRect, Color.Cyan, 2);

            // Title
            string title = "GAME PAUSED";
            Vector2 titleSize = m_Font.MeasureString(title) * 1.0f;
            SpriteBatch.DrawString(m_Font, title, new Vector2(cardX + (cardWidth - titleSize.X) / 2, cardY + 16), Color.Gold, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);

            // Options
            int startItemY = cardY + 52;
            for (int i = 0; i < s_PauseMenuItems.Length; i++)
            {
                bool isSelected = (i == m_PauseMenuIndex);
                Rectangle itemRect = new Rectangle(cardX + 15, startItemY + i * 32, cardWidth - 30, 26);

                if (isSelected)
                {
                    SpriteBatch.Draw(m_PixelTexture, itemRect, new Color(50, 70, 130, 240));
                    drawRectOutline(itemRect, Color.Gold, 1);
                }

                string label = isSelected ? $"> {s_PauseMenuItems[i]}" : $"  {s_PauseMenuItems[i]}";
                Color textColor = isSelected ? Color.Gold : new Color(200, 210, 230);
                SpriteBatch.DrawString(m_Font, label, new Vector2(itemRect.X + 8, itemRect.Y + 4), textColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }

            // Footer hint
            string hint = "[Esc] Resume  |  [Enter] Select";
            Vector2 hintSize = m_Font.MeasureString(hint) * 0.55f;
            SpriteBatch.DrawString(m_Font, hint, new Vector2(cardX + (cardWidth - hintSize.X) / 2, cardY + cardHeight - 18), new Color(140, 155, 180), 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
        }

        private void drawGameOverOverlay(int i_BoardX, int i_BoardY, int i_Width, int i_Height)
        {
            // Dark modal cover
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_BoardX, i_BoardY, i_Width, i_Height), new Color(5, 5, 10, 225));

            int cardWidth = 240;
            int cardHeight = 210;
            int cardX = i_BoardX + (i_Width - cardWidth) / 2;
            int cardY = i_BoardY + (i_Height - cardHeight) / 2;

            Rectangle cardRect = new Rectangle(cardX, cardY, cardWidth, cardHeight);
            SpriteBatch.Draw(m_PixelTexture, cardRect, new Color(25, 28, 45, 250));
            drawRectOutline(cardRect, Color.Crimson, 2);

            // Title
            string title = "GAME OVER";
            Vector2 titleSize = m_Font.MeasureString(title) * 1.1f;
            SpriteBatch.DrawString(m_Font, title, new Vector2(cardX + (cardWidth - titleSize.X) / 2, cardY + 18), Color.Crimson, 0f, Vector2.Zero, 1.1f, SpriteEffects.None, 0f);

            // Final Score
            string scoreStr = $"Score: {m_Score:N0}";
            Vector2 scoreSize = m_Font.MeasureString(scoreStr) * 0.8f;
            SpriteBatch.DrawString(m_Font, scoreStr, new Vector2(cardX + (cardWidth - scoreSize.X) / 2, cardY + 58), Color.Gold, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

            // Prompt
            string restartStr = "Press [Enter] to Play";
            Vector2 restartSize = m_Font.MeasureString(restartStr) * 0.65f;
            SpriteBatch.DrawString(m_Font, restartStr, new Vector2(cardX + (cardWidth - restartSize.X) / 2, cardY + 98), Color.White, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);

            string lbStr = "Press [L] Leaderboard";
            Vector2 lbSize = m_Font.MeasureString(lbStr) * 0.65f;
            SpriteBatch.DrawString(m_Font, lbStr, new Vector2(cardX + (cardWidth - lbSize.X) / 2, cardY + 125), Color.Cyan, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);

            string exitStr = "Press [Esc] for Menu";
            Vector2 exitSize = m_Font.MeasureString(exitStr) * 0.65f;
            SpriteBatch.DrawString(m_Font, exitStr, new Vector2(cardX + (cardWidth - exitSize.X) / 2, cardY + 152), new Color(180, 190, 210), 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
        }

        #endregion
    }
}
