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

namespace Invaders.PacMan
{
    public enum ePacManGameState
    {
        Ready,
        Playing,
        PacManDying,
        LevelClear,
        GameOver
    }

    public class PacManPlayScreen : GameScreen
    {
        public event Action BackToDashboard;

        private const string k_FontName = @"Fonts\Consolas";

        // Sounds
        private const string k_SoundChomp     = @"Sounds\EnemyKill";
        private const string k_SoundEatGhost  = @"Sounds\MotherShipKill";
        private const string k_SoundLevelWin  = @"Sounds\LevelWin";
        private const string k_SoundDeath     = @"Sounds\LifeDie";
        private const string k_SoundGameOver  = @"Sounds\GameOver";
        private const string k_SoundMenuMove  = @"Sounds\MenuMove";

        private SoundEffectInstance m_SoundChompInstance;
        private SoundEffectInstance m_SoundEatGhostInstance;
        private SoundEffectInstance m_SoundLevelWinInstance;
        private SoundEffectInstance m_SoundDeathInstance;
        private SoundEffectInstance m_SoundGameOverInstance;
        private SoundEffectInstance m_SoundMenuMoveInstance;

        // Core Components
        private PacManMaze m_Maze;
        private PacManPlayer m_PacMan;
        private Ghost[] m_Ghosts;
        private readonly ScoresDatabase m_ScoresDatabase;

        // State Machine & Timers
        private ePacManGameState m_GameState = ePacManGameState.Ready;
        private float m_ReadyTimer;
        private float m_LevelClearTimer;
        private float m_GlobalModeTimer;
        private eGhostState m_GlobalGhostState = eGhostState.Scatter;
        private float m_FrightenedTimer;
        private int m_GhostsEatenInCurrentFright;

        // Fruit System
        private bool m_FruitActive;
        private float m_FruitTimer;
        private int m_FruitScoreValue;
        private string m_FruitName = "Cherry";

        // Floating score popup (e.g. 200, 400, 800, 1600)
        private string m_PopupText = string.Empty;
        private Vector2 m_PopupPos;
        private float m_PopupTimer;

        // Scoring & Progression
        private int m_Score;
        private int m_HighScore;
        private int m_Level = 1;
        private bool m_ExtraLifeAwarded;
        private string m_PlayerName = "Player";

        private bool m_IsPaused;
        private int m_PauseMenuIndex = 0;
        private int m_GameOverMenuIndex = 0;

        private static readonly string[] s_PauseMenuItems = new string[]
        {
            "Resume",
            "Mute / Unmute",
            "Leaderboard",
            "Restart Game",
            "Quit to Dashboard"
        };

        private static readonly string[] s_GameOverMenuItems = new string[]
        {
            "Play Again",
            "Leaderboard",
            "Quit to Dashboard"
        };

        // Direct hardware input polling
        private KeyboardState m_CurrKeyboard;
        private KeyboardState m_PrevKeyboard;
        private GamePadState m_CurrGamePad;
        private GamePadState m_PrevGamePad;

        private Texture2D m_PixelTexture;
        private SpriteFont m_Font;

        public PacManPlayScreen(Game i_Game) : base(i_Game)
        {
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

            restartFullGame();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            m_Font = Game.Content.Load<SpriteFont>(k_FontName);

            m_PixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            m_PixelTexture.SetData(new[] { Color.White });

            SoundsManager sm = (Game as BaseGame)?.SoundsManager;
            if (sm != null)
            {
                m_SoundChompInstance    = sm.LoadSoundEffect(k_SoundChomp);
                m_SoundEatGhostInstance = sm.LoadSoundEffect(k_SoundEatGhost);
                m_SoundLevelWinInstance = sm.LoadSoundEffect(k_SoundLevelWin);
                m_SoundDeathInstance    = sm.LoadSoundEffect(k_SoundDeath);
                m_SoundGameOverInstance = sm.LoadSoundEffect(k_SoundGameOver);
                m_SoundMenuMoveInstance = sm.LoadSoundEffect(k_SoundMenuMove);
            }
        }

        private void loadHighScore()
        {
            try
            {
                List<ScoreEntry> topScores = m_ScoresDatabase.GetTopScores(1, "Pac-Man");
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

        private void restartFullGame()
        {
            m_Maze = new PacManMaze();
            m_Score = 0;
            m_Level = 1;
            m_ExtraLifeAwarded = false;
            m_IsPaused = false;
            m_PauseMenuIndex = 0;
            m_GameOverMenuIndex = 0;

            float tileSize = PacManMaze.k_TileSize;
            Vector2 pacSpawn = new Vector2(13f * tileSize + tileSize / 2f, 23f * tileSize + tileSize / 2f);
            m_PacMan = new PacManPlayer(pacSpawn) { Lives = 3 };

            initGhosts();
            startReadyState();
        }

        private void initGhosts()
        {
            float tileSize = PacManMaze.k_TileSize;
            Vector2 blinkySpawn = new Vector2(13f * tileSize + tileSize / 2f, 11f * tileSize + tileSize / 2f);
            Vector2 pinkySpawn  = new Vector2(13f * tileSize + tileSize / 2f, 14f * tileSize + tileSize / 2f);
            Vector2 inkySpawn   = new Vector2(11f * tileSize + tileSize / 2f, 14f * tileSize + tileSize / 2f);
            Vector2 clydeSpawn  = new Vector2(15f * tileSize + tileSize / 2f, 14f * tileSize + tileSize / 2f);

            m_Ghosts = new Ghost[]
            {
                new Ghost(eGhostType.Blinky, blinkySpawn, 0f),
                new Ghost(eGhostType.Pinky,  pinkySpawn,  1.5f),
                new Ghost(eGhostType.Inky,   inkySpawn,   3.5f),
                new Ghost(eGhostType.Clyde,  clydeSpawn,  5.5f)
            };
        }

        private void resetRoundPositions()
        {
            float tileSize = PacManMaze.k_TileSize;
            Vector2 pacSpawn = new Vector2(13f * tileSize + tileSize / 2f, 23f * tileSize + tileSize / 2f);
            m_PacMan.ResetPosition(pacSpawn);

            Vector2 blinkySpawn = new Vector2(13f * tileSize + tileSize / 2f, 11f * tileSize + tileSize / 2f);
            Vector2 pinkySpawn  = new Vector2(13f * tileSize + tileSize / 2f, 14f * tileSize + tileSize / 2f);
            Vector2 inkySpawn   = new Vector2(11f * tileSize + tileSize / 2f, 14f * tileSize + tileSize / 2f);
            Vector2 clydeSpawn  = new Vector2(15f * tileSize + tileSize / 2f, 14f * tileSize + tileSize / 2f);

            m_Ghosts[0].Reset(blinkySpawn, 0f);
            m_Ghosts[1].Reset(pinkySpawn,  1.5f);
            m_Ghosts[2].Reset(inkySpawn,   3.5f);
            m_Ghosts[3].Reset(clydeSpawn,  5.5f);

            m_FrightenedTimer = 0f;
            m_GlobalModeTimer = 0f;
            m_GlobalGhostState = eGhostState.Scatter;
            m_FruitActive = false;
        }

        private void startReadyState()
        {
            m_GameState = ePacManGameState.Ready;
            m_ReadyTimer = 1.6f;
            resetRoundPositions();
        }

        private void triggerGameOver()
        {
            if (m_GameState == ePacManGameState.GameOver) return;

            m_GameState = ePacManGameState.GameOver;
            m_GameOverMenuIndex = 0;
            m_SoundGameOverInstance?.Play();

            if (m_Score > m_HighScore)
            {
                m_HighScore = m_Score;
            }

            try
            {
                m_ScoresDatabase.SaveScore(m_PlayerName, m_Score, m_Level, "Pac-Man");
            }
            catch { }
        }

        #region Input Helper Methods

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

        private bool isConfirmPressed()
        {
            bool key = isKeyPressed(Keys.Enter) || isKeyPressed(Keys.Space);
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
            if (isKeyPressed(Keys.M))
            {
                (Game as BaseGame)?.SoundsManager.MuteToggle();
            }

            // 1. GAME OVER STATE
            if (m_GameState == ePacManGameState.GameOver)
            {
                updateGameOverMenu();
                return;
            }

            // 2. PAUSED STATE
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
                m_SoundMenuMoveInstance?.Play();
                return;
            }

            // 4. Update Popup timer
            if (m_PopupTimer > 0)
            {
                m_PopupTimer -= dt;
            }

            // 5. State Machine Dispatch
            switch (m_GameState)
            {
                case ePacManGameState.Ready:
                    m_ReadyTimer -= dt;
                    if (m_ReadyTimer <= 0 || isKeyPressed(Keys.Space) || isKeyPressed(Keys.Enter) ||
                        isKeyPressed(Keys.Up) || isKeyPressed(Keys.Down) || isKeyPressed(Keys.Left) || isKeyPressed(Keys.Right) ||
                        isKeyPressed(Keys.W) || isKeyPressed(Keys.S) || isKeyPressed(Keys.A) || isKeyPressed(Keys.D))
                    {
                        m_GameState = ePacManGameState.Playing;
                    }
                    break;

                case ePacManGameState.LevelClear:
                    m_LevelClearTimer -= dt;
                    if (m_LevelClearTimer <= 0)
                    {
                        m_Level++;
                        m_Maze.ResetDots();
                        startReadyState();
                    }
                    break;

                case ePacManGameState.PacManDying:
                    m_PacMan.Update(dt, m_Maze);
                    if (m_PacMan.DeathAnimProgress >= 1.0f)
                    {
                        m_PacMan.Lives--;
                        if (m_PacMan.Lives <= 0)
                        {
                            triggerGameOver();
                        }
                        else
                        {
                            startReadyState();
                        }
                    }
                    break;

                case ePacManGameState.Playing:
                    updateActiveGameplay(dt);
                    break;
            }
        }

        private void updateActiveGameplay(float i_Dt)
        {
            // 1. Read Directional Input for Pac-Man
            handlePacManInput();

            // 2. Update Pac-Man
            m_PacMan.Update(i_Dt, m_Maze);

            // 3. Check Dot & Energizer Eating
            Point pacTile = m_PacMan.TilePos;
            int dotPoints = m_Maze.EatDot(pacTile.X, pacTile.Y);

            if (dotPoints > 0)
            {
                m_Score += dotPoints;
                m_SoundChompInstance?.Play();

                // Extra life check at 10,000 pts
                if (m_Score >= 10000 && !m_ExtraLifeAwarded)
                {
                    m_ExtraLifeAwarded = true;
                    m_PacMan.Lives++;
                    m_SoundLevelWinInstance?.Play();
                }

                if (dotPoints == 50)
                {
                    // Energizer eaten -> Trigger Frightened Mode!
                    m_FrightenedTimer = Math.Max(4f, 8.5f - (m_Level * 0.5f));
                    m_GhostsEatenInCurrentFright = 0;

                    foreach (var ghost in m_Ghosts)
                    {
                        if (ghost.State != eGhostState.InHouse && ghost.State != eGhostState.EatenEyes)
                        {
                            ghost.State = eGhostState.Frightened;
                            ghost.CurrentDir = new Point(-ghost.CurrentDir.X, -ghost.CurrentDir.Y);
                        }
                    }
                }

                // Check Bonus Fruit spawn at 70 and 170 dots eaten
                int dotsEaten = m_Maze.TotalDots - m_Maze.DotsRemaining;
                if ((dotsEaten == 70 || dotsEaten == 170) && !m_FruitActive)
                {
                    spawnBonusFruit();
                }

                // Check Level Win
                if (m_Maze.DotsRemaining == 0)
                {
                    m_GameState = ePacManGameState.LevelClear;
                    m_LevelClearTimer = 1.8f;
                    m_SoundLevelWinInstance?.Play();
                    return;
                }
            }

            // 4. Update Fruit Timer & Collision
            if (m_FruitActive)
            {
                m_FruitTimer -= i_Dt;
                if (m_FruitTimer <= 0)
                {
                    m_FruitActive = false;
                }
                else
                {
                    // Check if Pac-Man collects fruit (col 13-14, row 17)
                    if ((pacTile.X == 13 || pacTile.X == 14) && pacTile.Y == 17)
                    {
                        m_Score += m_FruitScoreValue;
                        m_FruitActive = false;
                        showPopup($"+{m_FruitScoreValue}", new Vector2(13.5f * PacManMaze.k_TileSize, 17f * PacManMaze.k_TileSize));
                        m_SoundEatGhostInstance?.Play();
                    }
                }
            }

            // 5. Update Global Ghost Scatter / Chase Cycle
            if (m_FrightenedTimer > 0)
            {
                m_FrightenedTimer -= i_Dt;
            }
            else
            {
                m_GlobalModeTimer += i_Dt;
                float cycle = m_GlobalModeTimer % 27f;
                m_GlobalGhostState = (cycle < 7f) ? eGhostState.Scatter : eGhostState.Chase;
            }

            // 6. Update Ghosts & Check Collisions
            Ghost blinky = m_Ghosts[0];
            foreach (var ghost in m_Ghosts)
            {
                ghost.Update(i_Dt, m_Maze, m_PacMan, blinky, m_GlobalGhostState, m_FrightenedTimer);

                // Collision with Pac-Man
                if (Vector2.Distance(m_PacMan.Position, ghost.Position) < (PacManPlayer.k_Radius + Ghost.k_Radius - 2f))
                {
                    if (ghost.State == eGhostState.Frightened)
                    {
                        // Pac-Man eats ghost!
                        ghost.State = eGhostState.EatenEyes;
                        int points = (int)(200 * Math.Pow(2, m_GhostsEatenInCurrentFright));
                        m_GhostsEatenInCurrentFright++;
                        m_Score += points;

                        showPopup($"{points}", ghost.Position);
                        m_SoundEatGhostInstance?.Play();
                    }
                    else if (ghost.State == eGhostState.Chase || ghost.State == eGhostState.Scatter)
                    {
                        // Pac-Man dies!
                        m_GameState = ePacManGameState.PacManDying;
                        m_PacMan.IsDead = true;
                        m_SoundDeathInstance?.Play();
                        return;
                    }
                }
            }
        }

        private void spawnBonusFruit()
        {
            m_FruitActive = true;
            m_FruitTimer = 9.5f;

            switch (m_Level)
            {
                case 1:
                    m_FruitName = "Cherry";
                    m_FruitScoreValue = 100;
                    break;
                case 2:
                    m_FruitName = "Strawberry";
                    m_FruitScoreValue = 300;
                    break;
                case 3:
                    m_FruitName = "Orange";
                    m_FruitScoreValue = 500;
                    break;
                case 4:
                    m_FruitName = "Apple";
                    m_FruitScoreValue = 700;
                    break;
                default:
                    m_FruitName = "Melon";
                    m_FruitScoreValue = 1000;
                    break;
            }
        }

        private void showPopup(string i_Text, Vector2 i_Pos)
        {
            m_PopupText = i_Text;
            m_PopupPos = i_Pos;
            m_PopupTimer = 1.0f;
        }

        private void handlePacManInput()
        {
            KeyboardState kstate = Keyboard.GetState();
            GamePadState pstate = GamePad.GetState(PlayerIndex.One);

            if (kstate.IsKeyDown(Keys.Up) || kstate.IsKeyDown(Keys.W) || pstate.IsButtonDown(Buttons.DPadUp) || pstate.ThumbSticks.Left.Y > 0.3f)
            {
                m_PacMan.QueuedDir = new Point(0, -1);
            }
            else if (kstate.IsKeyDown(Keys.Down) || kstate.IsKeyDown(Keys.S) || pstate.IsButtonDown(Buttons.DPadDown) || pstate.ThumbSticks.Left.Y < -0.3f)
            {
                m_PacMan.QueuedDir = new Point(0, 1);
            }
            else if (kstate.IsKeyDown(Keys.Left) || kstate.IsKeyDown(Keys.A) || pstate.IsButtonDown(Buttons.DPadLeft) || pstate.ThumbSticks.Left.X < -0.3f)
            {
                m_PacMan.QueuedDir = new Point(-1, 0);
            }
            else if (kstate.IsKeyDown(Keys.Right) || kstate.IsKeyDown(Keys.D) || pstate.IsButtonDown(Buttons.DPadRight) || pstate.ThumbSticks.Left.X > 0.3f)
            {
                m_PacMan.QueuedDir = new Point(1, 0);
            }
        }

        private void updatePauseMenu()
        {
            if (isKeyPressed(Keys.Escape) || isKeyPressed(Keys.P) || isButtonPressed(Buttons.Start))
            {
                m_IsPaused = false;
                m_SoundMenuMoveInstance?.Play();
                return;
            }

            if (isKeyPressed(Keys.Q))
            {
                onBackToDashboard();
                return;
            }

            if (isKeyPressed(Keys.Up) || isKeyPressed(Keys.W) || isButtonPressed(Buttons.DPadUp))
            {
                m_PauseMenuIndex = (m_PauseMenuIndex + s_PauseMenuItems.Length - 1) % s_PauseMenuItems.Length;
                m_SoundMenuMoveInstance?.Play();
            }
            else if (isKeyPressed(Keys.Down) || isKeyPressed(Keys.S) || isButtonPressed(Buttons.DPadDown))
            {
                m_PauseMenuIndex = (m_PauseMenuIndex + 1) % s_PauseMenuItems.Length;
                m_SoundMenuMoveInstance?.Play();
            }

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
                    m_SoundMenuMoveInstance?.Play();
                    break;
                case 1: // Mute / Unmute
                    (Game as BaseGame)?.SoundsManager.MuteToggle();
                    m_SoundMenuMoveInstance?.Play();
                    break;
                case 2: // Leaderboard
                    ScreensManager.SetCurrentScreen(new LeaderboardScreen(Game, 4)); // Tab 4 = Pac-Man
                    break;
                case 3: // Restart Game
                    restartFullGame();
                    break;
                case 4: // Quit to Dashboard
                    onBackToDashboard();
                    break;
            }
        }

        private void updateGameOverMenu()
        {
            if (isKeyPressed(Keys.L) || isButtonPressed(Buttons.Y) || isButtonPressed(Buttons.X))
            {
                ScreensManager.SetCurrentScreen(new LeaderboardScreen(Game, 4));
                return;
            }

            if (isEscOrBack())
            {
                onBackToDashboard();
                return;
            }

            if (isKeyPressed(Keys.R))
            {
                restartFullGame();
                return;
            }

            if (isKeyPressed(Keys.Up) || isKeyPressed(Keys.W) || isButtonPressed(Buttons.DPadUp))
            {
                m_GameOverMenuIndex = (m_GameOverMenuIndex + s_GameOverMenuItems.Length - 1) % s_GameOverMenuItems.Length;
                m_SoundMenuMoveInstance?.Play();
            }
            else if (isKeyPressed(Keys.Down) || isKeyPressed(Keys.S) || isButtonPressed(Buttons.DPadDown))
            {
                m_GameOverMenuIndex = (m_GameOverMenuIndex + 1) % s_GameOverMenuItems.Length;
                m_SoundMenuMoveInstance?.Play();
            }

            if (isConfirmPressed())
            {
                executeGameOverMenuOption(m_GameOverMenuIndex);
            }
        }

        private void executeGameOverMenuOption(int i_Index)
        {
            switch (i_Index)
            {
                case 0: // Play Again
                    restartFullGame();
                    break;
                case 1: // Leaderboard
                    ScreensManager.SetCurrentScreen(new LeaderboardScreen(Game, 4));
                    break;
                case 2: // Quit to Dashboard
                    onBackToDashboard();
                    break;
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
            int mazeWidth = PacManMaze.k_Cols * PacManMaze.k_TileSize;
            int mazeHeight = PacManMaze.k_Rows * PacManMaze.k_TileSize;

            int arenaX = (vp.Width - mazeWidth) / 2;
            int arenaY = (vp.Height - mazeHeight) / 2 + 18;

            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // 1. Dark Backdrop
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(0, 0, vp.Width, vp.Height), new Color(5, 7, 14, 255));

            // 2. Draw Maze Wall Geometry, Dots, and Energizers
            bool energizerFlash = ((int)(i_GameTime.TotalGameTime.TotalSeconds * 4) % 2 == 0);
            bool mazeFlashWhite = (m_GameState == ePacManGameState.LevelClear && ((int)(m_LevelClearTimer * 8) % 2 == 0));
            m_Maze.Draw(SpriteBatch, m_PixelTexture, arenaX, arenaY, energizerFlash, mazeFlashWhite);

            // 3. Draw Bonus Fruit (if active)
            if (m_FruitActive)
            {
                drawBonusFruit(arenaX + 13 * PacManMaze.k_TileSize + 9, arenaY + 17 * PacManMaze.k_TileSize + 9);
            }

            // 4. Draw Pac-Man
            if (m_GameState != ePacManGameState.LevelClear)
            {
                Vector2 pacScreenPos = new Vector2(arenaX + m_PacMan.Position.X, arenaY + m_PacMan.Position.Y);
                m_PacMan.Draw(SpriteBatch, m_PixelTexture, pacScreenPos);
            }

            // 5. Draw Ghosts
            if (m_GameState != ePacManGameState.LevelClear && m_GameState != ePacManGameState.PacManDying)
            {
                foreach (var ghost in m_Ghosts)
                {
                    Vector2 ghostScreenPos = new Vector2(arenaX + ghost.Position.X, arenaY + ghost.Position.Y);
                    ghost.Draw(SpriteBatch, m_PixelTexture, ghostScreenPos, m_FrightenedTimer);
                }
            }

            // 6. Draw Floating Score Popup
            if (m_PopupTimer > 0)
            {
                Vector2 popupScreenPos = new Vector2(arenaX + m_PopupPos.X, arenaY + m_PopupPos.Y);
                Vector2 textSize = m_Font.MeasureString(m_PopupText) * 0.65f;
                SpriteBatch.DrawString(m_Font, m_PopupText, popupScreenPos - textSize / 2f, Color.Cyan, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
            }

            // 7. Draw In-Game HUD (Score, High Score, Level, Lives, Fruit)
            drawHUD(vp, arenaX, arenaY, mazeWidth, mazeHeight);

            // 8. Ready Banner
            if (m_GameState == ePacManGameState.Ready)
            {
                string ready = "READY!";
                Vector2 readySize = m_Font.MeasureString(ready) * 0.9f;
                Vector2 readyPos = new Vector2(arenaX + (mazeWidth - readySize.X) / 2, arenaY + 17 * PacManMaze.k_TileSize + 2);
                SpriteBatch.DrawString(m_Font, ready, readyPos, Color.Gold, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
            }

            // 9. Pause Overlay
            if (m_IsPaused)
            {
                drawPauseOverlay(arenaX, arenaY, mazeWidth, mazeHeight);
            }

            // 10. Game Over Overlay
            if (m_GameState == ePacManGameState.GameOver)
            {
                drawGameOverOverlay(arenaX, arenaY, mazeWidth, mazeHeight);
            }

            SpriteBatch.End();
        }

        private void drawBonusFruit(int i_X, int i_Y)
        {
            Rectangle cherry = new Rectangle(i_X - 6, i_Y - 6, 12, 12);
            SpriteBatch.Draw(m_PixelTexture, cherry, Color.Red);
            // Stem
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_X - 2, i_Y - 9, 3, 4), Color.Green);
        }

        private void drawHUD(Viewport i_Vp, int i_ArenaX, int i_ArenaY, int i_Width, int i_Height)
        {
            // Top HUD Bar
            Rectangle hudBar = new Rectangle(i_ArenaX, i_ArenaY - 48, i_Width, 38);
            SpriteBatch.Draw(m_PixelTexture, hudBar, new Color(14, 18, 30, 240));
            drawRectOutline(hudBar, new Color(40, 70, 110, 200), 1);

            string scoreStr = $"1UP {m_Score:N0}";
            SpriteBatch.DrawString(m_Font, scoreStr, new Vector2(i_ArenaX + 10, i_ArenaY - 40), Color.White, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

            string highStr = $"HIGH {m_HighScore:N0}";
            SpriteBatch.DrawString(m_Font, highStr, new Vector2(i_ArenaX + 190, i_ArenaY - 40), Color.Gold, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

            string lvlStr = $"LVL {m_Level}";
            SpriteBatch.DrawString(m_Font, lvlStr, new Vector2(i_ArenaX + 390, i_ArenaY - 40), Color.Cyan, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

            // Bottom HUD: Lives & Fruit symbols
            int bottomY = i_ArenaY + i_Height + 10;
            SpriteBatch.DrawString(m_Font, "LIVES:", new Vector2(i_ArenaX + 10, bottomY + 2), new Color(170, 180, 200), 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);

            // Draw Pac-Man lives icons
            for (int i = 0; i < m_PacMan.Lives - 1; i++)
            {
                Rectangle lifeRect = new Rectangle(i_ArenaX + 70 + i * 20, bottomY + 4, 12, 12);
                SpriteBatch.Draw(m_PixelTexture, lifeRect, Color.Yellow);
            }

            // Fruit symbol
            string fruitInfo = $"FRUIT: {m_FruitName} (+{m_FruitScoreValue})";
            Vector2 fruitSize = m_Font.MeasureString(fruitInfo) * 0.62f;
            SpriteBatch.DrawString(m_Font, fruitInfo, new Vector2(i_ArenaX + i_Width - fruitSize.X - 10, bottomY + 2), Color.Gold, 0f, Vector2.Zero, 0.62f, SpriteEffects.None, 0f);

            // Left Side Controls Card
            drawSideControlsCard(25, 140);
        }

        private void drawSideControlsCard(int i_X, int i_Y)
        {
            Rectangle card = new Rectangle(i_X, i_Y, 180, 290);
            SpriteBatch.Draw(m_PixelTexture, card, new Color(14, 18, 30, 230));
            drawRectOutline(card, new Color(50, 70, 120, 180), 1);

            SpriteBatch.DrawString(m_Font, "PAC-MAN", new Vector2(i_X + 45, i_Y + 12), Color.Gold, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

            string[] tips = new string[]
            {
                "Arrows / WASD",
                "to Move Pac-Man",
                "",
                "Dots : 10 pts",
                "Pellet : 50 pts",
                "Blue Ghost :",
                "200-1600 pts",
                "P / Esc : Pause",
                "M : Mute"
            };

            float y = i_Y + 42;
            foreach (var tip in tips)
            {
                SpriteBatch.DrawString(m_Font, tip, new Vector2(i_X + 12, y), new Color(180, 195, 220), 0f, Vector2.Zero, 0.58f, SpriteEffects.None, 0f);
                y += 24;
            }
        }

        private void drawPauseOverlay(int i_ArenaX, int i_ArenaY, int i_Width, int i_Height)
        {
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_ArenaX, i_ArenaY, i_Width, i_Height), new Color(5, 8, 18, 225));

            int cardWidth = 260;
            int cardHeight = 255;
            int cardX = i_ArenaX + (i_Width - cardWidth) / 2;
            int cardY = i_ArenaY + (i_Height - cardHeight) / 2;

            Rectangle cardRect = new Rectangle(cardX, cardY, cardWidth, cardHeight);
            SpriteBatch.Draw(m_PixelTexture, cardRect, new Color(20, 24, 40, 250));
            drawRectOutline(cardRect, Color.Cyan, 2);

            string title = "GAME PAUSED";
            Vector2 titleSize = m_Font.MeasureString(title) * 1.0f;
            SpriteBatch.DrawString(m_Font, title, new Vector2(cardX + (cardWidth - titleSize.X) / 2, cardY + 16), Color.Gold, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);

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

            string hint = "[Esc] Resume  |  [Enter] Select";
            Vector2 hintSize = m_Font.MeasureString(hint) * 0.55f;
            SpriteBatch.DrawString(m_Font, hint, new Vector2(cardX + (cardWidth - hintSize.X) / 2, cardY + cardHeight - 18), new Color(140, 155, 180), 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
        }

        private void drawGameOverOverlay(int i_ArenaX, int i_ArenaY, int i_Width, int i_Height)
        {
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_ArenaX, i_ArenaY, i_Width, i_Height), new Color(5, 5, 10, 225));

            int cardWidth = 270;
            int cardHeight = 250;
            int cardX = i_ArenaX + (i_Width - cardWidth) / 2;
            int cardY = i_ArenaY + (i_Height - cardHeight) / 2;

            Rectangle cardRect = new Rectangle(cardX, cardY, cardWidth, cardHeight);
            SpriteBatch.Draw(m_PixelTexture, cardRect, new Color(25, 28, 45, 250));
            drawRectOutline(cardRect, Color.Crimson, 2);

            // Title
            string title = "GAME OVER";
            Vector2 titleSize = m_Font.MeasureString(title) * 1.1f;
            SpriteBatch.DrawString(m_Font, title, new Vector2(cardX + (cardWidth - titleSize.X) / 2, cardY + 16), Color.Crimson, 0f, Vector2.Zero, 1.1f, SpriteEffects.None, 0f);

            // Summary Stats
            string scoreStr = $"Score: {m_Score:N0}   Level: {m_Level}";
            Vector2 scoreSize = m_Font.MeasureString(scoreStr) * 0.72f;
            SpriteBatch.DrawString(m_Font, scoreStr, new Vector2(cardX + (cardWidth - scoreSize.X) / 2, cardY + 48), Color.Gold, 0f, Vector2.Zero, 0.72f, SpriteEffects.None, 0f);

            // Interactive Options
            int startItemY = cardY + 80;
            for (int i = 0; i < s_GameOverMenuItems.Length; i++)
            {
                bool isSelected = (i == m_GameOverMenuIndex);
                Rectangle itemRect = new Rectangle(cardX + 15, startItemY + i * 34, cardWidth - 30, 28);

                if (isSelected)
                {
                    SpriteBatch.Draw(m_PixelTexture, itemRect, new Color(60, 75, 140, 240));
                    drawRectOutline(itemRect, Color.Gold, 1);
                }

                string label = isSelected ? $"> {s_GameOverMenuItems[i]}" : $"  {s_GameOverMenuItems[i]}";
                Color textColor = isSelected ? Color.Gold : new Color(200, 210, 230);
                SpriteBatch.DrawString(m_Font, label, new Vector2(itemRect.X + 8, itemRect.Y + 5), textColor, 0f, Vector2.Zero, 0.72f, SpriteEffects.None, 0f);
            }

            // Footer hint
            string hint = "[Up/Down] Choose  |  [Enter] Select";
            Vector2 hintSize = m_Font.MeasureString(hint) * 0.55f;
            SpriteBatch.DrawString(m_Font, hint, new Vector2(cardX + (cardWidth - hintSize.X) / 2, cardY + cardHeight - 18), new Color(140, 155, 180), 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
        }

        private void drawRectOutline(Rectangle i_Rect, Color i_Color, int i_Thickness)
        {
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_Rect.X, i_Rect.Y, i_Rect.Width, i_Thickness), i_Color);
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_Rect.X, i_Rect.Bottom - i_Thickness, i_Rect.Width, i_Thickness), i_Color);
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_Rect.X, i_Rect.Y, i_Thickness, i_Rect.Height), i_Color);
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_Rect.Right - i_Thickness, i_Rect.Y, i_Thickness, i_Rect.Height), i_Color);
        }

        #endregion
    }
}
