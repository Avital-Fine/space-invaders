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

namespace Invaders.IcyTower
{
    public class IcyTowerPlayScreen : GameScreen
    {
        public event Action BackToDashboard;

        private const string k_FontName = @"Fonts\Consolas";
        private const float k_LeftWall = 180f;
        private const float k_RightWall = 780f;
        private const float k_FloorSpacing = 72f;
        private const float k_GroundY = 620f;

        // Sounds
        private const string k_SoundJump     = @"Sounds\SSGunShot";
        private const string k_SoundBounce   = @"Sounds\BarrierHit";
        private const string k_SoundCombo    = @"Sounds\LevelWin";
        private const string k_SoundGameOver = @"Sounds\GameOver";
        private const string k_SoundMenuMove = @"Sounds\MenuMove";

        private SoundEffectInstance m_SoundJumpInstance;
        private SoundEffectInstance m_SoundBounceInstance;
        private SoundEffectInstance m_SoundComboInstance;
        private SoundEffectInstance m_SoundGameOverInstance;
        private SoundEffectInstance m_SoundMenuMoveInstance;

        // Core Game Components
        private IcyTowerPlayer m_Player;
        private readonly List<IcyTowerPlatform> m_Platforms = new List<IcyTowerPlatform>();
        private readonly ScoresDatabase m_ScoresDatabase;
        private readonly Random m_Random = new Random();

        private float m_CameraY;
        private float m_AutoScrollSpeed;
        private bool m_IsGameOver;
        private bool m_IsPaused;

        // Scoring & Combos
        private int m_Score;
        private int m_HighScore;
        private int m_HighestFloorReached;
        private int m_JumpStartFloor;
        private int m_ComboCount;
        private int m_ComboBonusScore;
        private float m_ComboTimer;
        private const float k_MaxComboTime = 3.5f;

        // Combo Shoutout Banner
        private string m_ComboShoutoutText = string.Empty;
        private Color m_ComboShoutoutColor = Color.Gold;
        private float m_ComboShoutoutTimer = 0f;
        private const float k_ShoutoutDuration = 1.4f;

        // In-game menus
        private int m_PauseMenuIndex = 0;
        private static readonly string[] s_PauseMenuItems = new string[]
        {
            "Resume",
            "Mute / Unmute",
            "Leaderboard",
            "Restart Game",
            "Quit to Dashboard"
        };

        private int m_GameOverMenuIndex = 0;
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

        private string m_PlayerName = "Player";
        private Texture2D m_PixelTexture;
        private SpriteFont m_Font;

        public IcyTowerPlayScreen(Game i_Game) : base(i_Game)
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

            restartGame();
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
                m_SoundJumpInstance     = sm.LoadSoundEffect(k_SoundJump);
                m_SoundBounceInstance   = sm.LoadSoundEffect(k_SoundBounce);
                m_SoundComboInstance    = sm.LoadSoundEffect(k_SoundCombo);
                m_SoundGameOverInstance = sm.LoadSoundEffect(k_SoundGameOver);
                m_SoundMenuMoveInstance = sm.LoadSoundEffect(k_SoundMenuMove);
            }
        }

        private void loadHighScore()
        {
            try
            {
                List<ScoreEntry> topScores = m_ScoresDatabase.GetTopScores(1, "Icy Tower");
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
            m_Platforms.Clear();
            m_CameraY = 0f;
            m_AutoScrollSpeed = 0f;
            m_Score = 0;
            m_HighestFloorReached = 0;
            m_JumpStartFloor = 0;
            m_ComboCount = 0;
            m_ComboBonusScore = 0;
            m_ComboTimer = 0f;
            m_ComboShoutoutTimer = 0f;
            m_IsGameOver = false;
            m_IsPaused = false;
            m_PauseMenuIndex = 0;
            m_GameOverMenuIndex = 0;

            // Generate initial ground (Floor 0) spanning the full shaft width
            m_Platforms.Add(new IcyTowerPlatform(0, k_LeftWall, k_GroundY, k_RightWall - k_LeftWall, 25f));

            // Generate first 40 ascending platforms
            for (int i = 1; i <= 40; i++)
            {
                generatePlatform(i);
            }

            // Spawn player standing on the center of the ground
            Vector2 spawnPos = new Vector2((k_LeftWall + k_RightWall) / 2f, k_GroundY);
            m_Player = new IcyTowerPlayer(spawnPos);
        }

        private void generatePlatform(int i_Floor)
        {
            float floorY = k_GroundY - i_Floor * k_FloorSpacing;
            float maxPlatformWidth = Math.Max(100f, 230f - (i_Floor / 30f) * 16f);
            float minPlatformWidth = Math.Max(75f, maxPlatformWidth - 45f);
            float width = (float)(m_Random.NextDouble() * (maxPlatformWidth - minPlatformWidth) + minPlatformWidth);

            float minX = k_LeftWall + 14f;
            float maxX = k_RightWall - width - 14f;
            float x = (float)(m_Random.NextDouble() * (maxX - minX) + minX);

            m_Platforms.Add(new IcyTowerPlatform(i_Floor, x, floorY, width));
        }

        private void triggerGameOver()
        {
            if (m_IsGameOver) return;

            m_IsGameOver = true;
            m_GameOverMenuIndex = 0;
            m_SoundGameOverInstance?.Play();

            if (m_Score > m_HighScore)
            {
                m_HighScore = m_Score;
            }

            try
            {
                m_ScoresDatabase.SaveScore(m_PlayerName, m_Score, m_HighestFloorReached, "Icy Tower");
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

        private float getHorizontalInput()
        {
            float input = 0f;
            if (isKeyHeld(Keys.Left) || isKeyHeld(Keys.A) || isButtonHeld(Buttons.DPadLeft)) input -= 1f;
            if (isKeyHeld(Keys.Right) || isKeyHeld(Keys.D) || isButtonHeld(Buttons.DPadRight)) input += 1f;

            if (Math.Abs(m_CurrGamePad.ThumbSticks.Left.X) > 0.2f)
            {
                input += m_CurrGamePad.ThumbSticks.Left.X;
            }

            return Math.Clamp(input, -1f, 1f);
        }

        private bool isJumpPressed()
        {
            bool key = isKeyPressed(Keys.Space) || isKeyPressed(Keys.Up) || isKeyPressed(Keys.W);
            bool pad = isButtonPressed(Buttons.A) || isButtonPressed(Buttons.DPadUp);
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
            if (m_IsGameOver)
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

            // 4. ACTIVE GAMEPLAY

            // Update combo shoutout timer
            if (m_ComboShoutoutTimer > 0)
            {
                m_ComboShoutoutTimer -= dt;
            }

            // Update combo decay timer
            if (m_Player.IsGrounded && m_ComboCount > 0)
            {
                m_ComboTimer -= dt;
                if (m_ComboTimer <= 0)
                {
                    m_ComboCount = 0;
                }
            }

            // Read Player Controls
            float moveInput = getHorizontalInput();
            bool jumpInput = isJumpPressed();

            // Record jump start floor
            if (jumpInput && m_Player.IsGrounded)
            {
                m_JumpStartFloor = m_Player.LastFloorIndex;
            }

            // Check if player walked off current platform edge
            checkGroundedPlatformSupport();

            // Update Player Physics
            m_Player.Update(
                dt,
                moveInput,
                jumpInput,
                k_LeftWall,
                k_RightWall,
                onPlayerBounce,
                onPlayerJump);

            // Check platform collisions (when falling downward)
            checkPlatformCollisions(dt);

            // Ensure platforms are generated ahead of player
            int highestExistingFloor = m_Platforms[m_Platforms.Count - 1].FloorIndex;
            while (highestExistingFloor < m_HighestFloorReached + 30)
            {
                highestExistingFloor++;
                generatePlatform(highestExistingFloor);
            }

            // Update Camera Tracking & Rising Screen
            updateCameraAndRisingTide(dt);

            // Check Fall Off Screen -> Game Over
            Viewport vp = GraphicsDevice.Viewport;
            if (m_HighestFloorReached >= 4 && m_Player.Position.Y > m_CameraY + vp.Height + 20f)
            {
                triggerGameOver();
            }
        }

        private void updateGameOverMenu()
        {
            // Direct shortcut: Leaderboard (L or Gamepad Y/X)
            if (isKeyPressed(Keys.L) || isButtonPressed(Buttons.Y) || isButtonPressed(Buttons.X))
            {
                ScreensManager.SetCurrentScreen(new LeaderboardScreen(Game, 2)); // Tab 2 = Icy Tower
                return;
            }

            // Direct shortcut: Quit to dashboard (Esc / Q / Gamepad B/Back)
            if (isEscOrBack())
            {
                onBackToDashboard();
                return;
            }

            // Direct shortcut: Quick Restart (R)
            if (isKeyPressed(Keys.R))
            {
                restartGame();
                return;
            }

            // Menu navigation: Up
            if (isKeyPressed(Keys.Up) || isKeyPressed(Keys.W) || isButtonPressed(Buttons.DPadUp))
            {
                m_GameOverMenuIndex = (m_GameOverMenuIndex + s_GameOverMenuItems.Length - 1) % s_GameOverMenuItems.Length;
                m_SoundMenuMoveInstance?.Play();
            }
            // Menu navigation: Down
            else if (isKeyPressed(Keys.Down) || isKeyPressed(Keys.S) || isButtonPressed(Buttons.DPadDown))
            {
                m_GameOverMenuIndex = (m_GameOverMenuIndex + 1) % s_GameOverMenuItems.Length;
                m_SoundMenuMoveInstance?.Play();
            }

            // Menu selection: Enter / Space / Gamepad A
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
                    restartGame();
                    break;
                case 1: // Leaderboard
                    ScreensManager.SetCurrentScreen(new LeaderboardScreen(Game, 2));
                    break;
                case 2: // Quit to Dashboard
                    onBackToDashboard();
                    break;
            }
        }

        private void onPlayerJump(bool i_IsSuper)
        {
            m_SoundJumpInstance?.Play();
        }

        private void onPlayerBounce()
        {
            m_SoundBounceInstance?.Play();
        }

        private void checkGroundedPlatformSupport()
        {
            if (!m_Player.IsGrounded) return;

            // Ground floor (0) spans full width
            if (m_Player.LastFloorIndex == 0)
            {
                if (m_Player.Position.Y < k_GroundY - 2f)
                {
                    m_Player.IsGrounded = false;
                }
                return;
            }

            float halfW = m_Player.Width / 2f;
            bool supported = false;

            foreach (var platform in m_Platforms)
            {
                if (platform.FloorIndex == m_Player.LastFloorIndex)
                {
                    if (Math.Abs(m_Player.Position.Y - platform.Y) < 4f &&
                        m_Player.Position.X + halfW - 4f >= platform.X &&
                        m_Player.Position.X - halfW + 4f <= platform.X + platform.Width)
                    {
                        supported = true;
                    }
                    break;
                }
            }

            if (!supported)
            {
                m_Player.IsGrounded = false;
            }
        }

        private void checkPlatformCollisions(float i_Dt)
        {
            float playerFeetY = m_Player.Position.Y;
            float halfW = m_Player.Width / 2f;

            // 1. Solid Ground Check (Floor 0)
            if (playerFeetY >= k_GroundY && m_Player.Velocity.Y >= 0)
            {
                m_Player.LandOn(m_Platforms[0]);
                return;
            }

            if (m_Player.Velocity.Y <= 0) return; // Only land when falling down

            // 2. Floating Platforms Check
            float playerPrevFeetY = playerFeetY - m_Player.Velocity.Y * i_Dt;

            foreach (var platform in m_Platforms)
            {
                if (platform.FloorIndex == 0) continue;

                // Check falling through the platform line
                if (playerFeetY >= platform.Y && playerPrevFeetY <= platform.Y + 16f)
                {
                    // Check horizontal overlap
                    if (m_Player.Position.X + halfW - 4f >= platform.X && m_Player.Position.X - halfW + 4f <= platform.X + platform.Width)
                    {
                        int landedFloor = platform.FloorIndex;
                        m_Player.LandOn(platform);

                        handleLandingScoring(landedFloor);
                        break;
                    }
                }
            }
        }

        private void handleLandingScoring(int i_LandedFloor)
        {
            int floorsSkipped = i_LandedFloor - m_JumpStartFloor;

            // Track highest floor reached
            if (i_LandedFloor > m_HighestFloorReached)
            {
                m_HighestFloorReached = i_LandedFloor;
            }

            // Combo leap check (skipped 2 or more floors in a single jump)
            if (floorsSkipped >= 2)
            {
                m_ComboCount++;
                m_ComboTimer = k_MaxComboTime;

                int comboPoints = floorsSkipped * floorsSkipped * 50 * m_ComboCount;
                m_ComboBonusScore += comboPoints;

                triggerComboShoutout(floorsSkipped, comboPoints);
                m_SoundComboInstance?.Play();
            }

            // Recalculate total score
            m_Score = (m_HighestFloorReached * 10) + m_ComboBonusScore;
        }

        private void triggerComboShoutout(int i_FloorsSkipped, int i_Points)
        {
            string rank;
            Color col;

            if (i_FloorsSkipped <= 3)
            {
                rank = "GOOD!";
                col = new Color(100, 220, 255);
            }
            else if (i_FloorsSkipped <= 5)
            {
                rank = "SWEET!";
                col = new Color(120, 255, 140);
            }
            else if (i_FloorsSkipped <= 7)
            {
                rank = "GREAT!";
                col = Color.Gold;
            }
            else if (i_FloorsSkipped <= 9)
            {
                rank = "SUPER!";
                col = new Color(255, 140, 40);
            }
            else if (i_FloorsSkipped <= 14)
            {
                rank = "AMAZING!";
                col = new Color(255, 80, 180);
            }
            else
            {
                rank = "UNBELIEVABLE!";
                col = new Color(255, 50, 50);
            }

            m_ComboShoutoutText = $"{rank} +{i_Points:N0} (x{m_ComboCount})";
            m_ComboShoutoutColor = col;
            m_ComboShoutoutTimer = k_ShoutoutDuration;
        }

        private void updateCameraAndRisingTide(float i_Dt)
        {
            Viewport vp = GraphicsDevice.Viewport;
            float targetCameraY = m_Player.Position.Y - vp.Height * 0.52f;

            // Camera smoothly tracks player upward
            if (targetCameraY < m_CameraY)
            {
                m_CameraY = MathHelper.Lerp(m_CameraY, targetCameraY, 12f * i_Dt);
            }

            // Automatic rising screen pressure starts after floor 5
            if (m_HighestFloorReached >= 5)
            {
                float baseSpeed = 30f + Math.Min(160f, (m_HighestFloorReached / 10f) * 10f);
                m_AutoScrollSpeed = baseSpeed;
                m_CameraY -= m_AutoScrollSpeed * i_Dt;
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
                    ScreensManager.SetCurrentScreen(new LeaderboardScreen(Game, 2)); // Tab 2 = Icy Tower
                    break;
                case 3: // Restart Game
                    restartGame();
                    break;
                case 4: // Quit to Dashboard
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

            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // 1. Draw Outer Castle Walls & Tower Shaft Backdrop
            drawTowerArchitecture(vp);

            // 2. Draw Platforms (in Camera Space)
            drawPlatforms(vp);

            // 3. Draw Player Character (in Camera Space)
            if (!m_IsGameOver)
            {
                Vector2 playerScreenPos = new Vector2(m_Player.Position.X, m_Player.Position.Y - m_CameraY);
                m_Player.Draw(SpriteBatch, m_PixelTexture, playerScreenPos);
            }

            // 4. Draw Rising Water / Mist at Bottom of Screen (only when scrolling active)
            if (m_HighestFloorReached >= 5)
            {
                drawRisingTideMist(vp);
            }

            // 5. Draw In-Game HUD (Floors, Score, High Score, Combo Meter)
            drawHUD(vp);

            // 6. Draw Combo Shoutout Callout Text
            if (m_ComboShoutoutTimer > 0)
            {
                drawComboShoutout(vp);
            }

            // 7. Draw In-Game Pause Overlay
            if (m_IsPaused)
            {
                drawPauseOverlay(vp);
            }

            // 8. Draw Interactive Game Over Overlay
            if (m_IsGameOver)
            {
                drawGameOverOverlay(vp);
            }

            SpriteBatch.End();
        }

        private void drawTowerArchitecture(Viewport i_Vp)
        {
            // Center tower background
            int shaftWidth = (int)(k_RightWall - k_LeftWall);
            Rectangle shaftRect = new Rectangle((int)k_LeftWall, 0, shaftWidth, i_Vp.Height);
            SpriteBatch.Draw(m_PixelTexture, shaftRect, new Color(14, 18, 30, 255));

            // Castle Brick Side Walls
            drawBrickWall(0, (int)k_LeftWall, i_Vp.Height);
            drawBrickWall((int)k_RightWall, i_Vp.Width - (int)k_RightWall, i_Vp.Height);

            // Inner Wall Rails
            SpriteBatch.Draw(m_PixelTexture, new Rectangle((int)k_LeftWall - 3, 0, 3, i_Vp.Height), new Color(100, 180, 240, 200));
            SpriteBatch.Draw(m_PixelTexture, new Rectangle((int)k_RightWall, 0, 3, i_Vp.Height), new Color(100, 180, 240, 200));
        }

        private void drawBrickWall(int i_X, int i_Width, int i_Height)
        {
            Rectangle wallRect = new Rectangle(i_X, 0, i_Width, i_Height);
            SpriteBatch.Draw(m_PixelTexture, wallRect, new Color(20, 24, 38, 255));

            int brickH = 26;
            int brickW = 44;
            int yOffset = (int)(-m_CameraY * 0.4f) % brickH;

            Color brickLineColor = new Color(10, 12, 20, 180);
            for (int y = -brickH + yOffset; y < i_Height + brickH; y += brickH)
            {
                SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_X, y, i_Width, 2), brickLineColor);
                int rowIdx = (y - yOffset) / brickH;
                int xOffset = (rowIdx % 2 == 0) ? 0 : brickW / 2;

                for (int x = i_X + xOffset; x < i_X + i_Width; x += brickW)
                {
                    SpriteBatch.Draw(m_PixelTexture, new Rectangle(x, y, 2, brickH), brickLineColor);
                }
            }
        }

        private void drawPlatforms(Viewport i_Vp)
        {
            foreach (var platform in m_Platforms)
            {
                float screenY = platform.Y - m_CameraY;
                if (screenY + platform.Height < -50 || screenY > i_Vp.Height + 50) continue;

                Rectangle rect = new Rectangle((int)platform.X, (int)screenY, (int)platform.Width, (int)platform.Height);

                // Main platform body
                SpriteBatch.Draw(m_PixelTexture, rect, platform.BaseColor);

                // Top highlight bevel
                SpriteBatch.Draw(m_PixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, 3), platform.BevelLightColor);

                // Bottom shadow bevel
                SpriteBatch.Draw(m_PixelTexture, new Rectangle(rect.X, rect.Bottom - 3, rect.Width, 3), platform.BevelDarkColor);

                // Outer border
                drawRectOutline(rect, new Color(10, 15, 25, 230), 1);

                // Floor number label on platform
                string floorLabel = platform.FloorIndex.ToString();
                Vector2 labelSize = m_Font.MeasureString(floorLabel) * 0.55f;
                Vector2 labelPos = new Vector2(rect.X + (rect.Width - labelSize.X) / 2, rect.Y + (rect.Height - labelSize.Y) / 2);
                SpriteBatch.DrawString(m_Font, floorLabel, labelPos, Color.White * 0.9f, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
            }
        }

        private void drawRisingTideMist(Viewport i_Vp)
        {
            Rectangle dangerRect = new Rectangle((int)k_LeftWall, i_Vp.Height - 35, (int)(k_RightWall - k_LeftWall), 35);
            SpriteBatch.Draw(m_PixelTexture, dangerRect, new Color(220, 40, 60, 90));
        }

        private void drawHUD(Viewport i_Vp)
        {
            int hudWidth = (int)(k_RightWall - k_LeftWall);
            Rectangle hudBar = new Rectangle((int)k_LeftWall, 0, hudWidth, 42);
            SpriteBatch.Draw(m_PixelTexture, hudBar, new Color(10, 14, 25, 230));
            drawRectOutline(hudBar, new Color(40, 60, 100, 200), 1);

            // 1. Floor Counter
            string floorStr = $"FLOOR {m_HighestFloorReached}";
            SpriteBatch.DrawString(m_Font, floorStr, new Vector2(k_LeftWall + 15, 10), Color.Gold, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

            // 2. Current Score
            string scoreStr = $"SCORE {m_Score:N0}";
            SpriteBatch.DrawString(m_Font, scoreStr, new Vector2(k_LeftWall + 210, 10), Color.Cyan, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

            // 3. High Score
            string highStr = $"BEST {m_HighScore:N0}";
            SpriteBatch.DrawString(m_Font, highStr, new Vector2(k_LeftWall + 430, 10), new Color(255, 180, 60), 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

            // 4. Combo Meter Bar
            if (m_ComboCount > 0 && m_ComboTimer > 0)
            {
                float comboRatio = Math.Clamp(m_ComboTimer / k_MaxComboTime, 0f, 1f);
                int barWidth = (int)(hudWidth * comboRatio);
                Rectangle comboBarRect = new Rectangle((int)k_LeftWall, 42, barWidth, 4);
                SpriteBatch.Draw(m_PixelTexture, comboBarRect, Color.Orange);
            }

            // Left Side Guide Panel
            drawSideControlsPanel(15, 80);
        }

        private void drawSideControlsPanel(int i_X, int i_Y)
        {
            Rectangle card = new Rectangle(i_X, i_Y, 150, 260);
            SpriteBatch.Draw(m_PixelTexture, card, new Color(15, 18, 30, 230));
            drawRectOutline(card, new Color(50, 70, 110, 180), 1);

            SpriteBatch.DrawString(m_Font, "ICY TOWER", new Vector2(i_X + 25, i_Y + 12), Color.Cyan, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);

            string[] tips = new string[]
            {
                "Left/Right : Run",
                "Space/Up   : Jump",
                "Wall Touch : Bounce",
                "Super Jump : Spin",
                "Skip Steps : COMBO",
                "Esc / P    : Pause",
                "M          : Mute"
            };

            float y = i_Y + 44;
            foreach (var tip in tips)
            {
                SpriteBatch.DrawString(m_Font, tip, new Vector2(i_X + 10, y), new Color(180, 195, 220), 0f, Vector2.Zero, 0.58f, SpriteEffects.None, 0f);
                y += 28;
            }
        }

        private void drawComboShoutout(Viewport i_Vp)
        {
            float progress = m_ComboShoutoutTimer / k_ShoutoutDuration;
            float scale = 0.95f + (1f - progress) * 0.35f;
            Color textColor = m_ComboShoutoutColor * Math.Min(1f, progress * 1.5f);

            Vector2 textSize = m_Font.MeasureString(m_ComboShoutoutText) * scale;
            float x = (k_LeftWall + k_RightWall - textSize.X) / 2f;
            float y = i_Vp.Height * 0.28f;

            SpriteBatch.DrawString(m_Font, m_ComboShoutoutText, new Vector2(x + 2, y + 2), Color.Black * 0.7f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            SpriteBatch.DrawString(m_Font, m_ComboShoutoutText, new Vector2(x, y), textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private void drawPauseOverlay(Viewport i_Vp)
        {
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(0, 0, i_Vp.Width, i_Vp.Height), new Color(5, 8, 18, 225));

            int cardWidth = 260;
            int cardHeight = 255;
            int cardX = (i_Vp.Width - cardWidth) / 2;
            int cardY = (i_Vp.Height - cardHeight) / 2;

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

        private void drawGameOverOverlay(Viewport i_Vp)
        {
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(0, 0, i_Vp.Width, i_Vp.Height), new Color(5, 5, 10, 225));

            int cardWidth = 270;
            int cardHeight = 250;
            int cardX = (i_Vp.Width - cardWidth) / 2;
            int cardY = (i_Vp.Height - cardHeight) / 2;

            Rectangle cardRect = new Rectangle(cardX, cardY, cardWidth, cardHeight);
            SpriteBatch.Draw(m_PixelTexture, cardRect, new Color(25, 28, 45, 250));
            drawRectOutline(cardRect, Color.Crimson, 2);

            // Title
            string title = "GAME OVER";
            Vector2 titleSize = m_Font.MeasureString(title) * 1.1f;
            SpriteBatch.DrawString(m_Font, title, new Vector2(cardX + (cardWidth - titleSize.X) / 2, cardY + 16), Color.Crimson, 0f, Vector2.Zero, 1.1f, SpriteEffects.None, 0f);

            // Summary Stats
            string floorStr = $"Floor: {m_HighestFloorReached}   Score: {m_Score:N0}";
            Vector2 floorSize = m_Font.MeasureString(floorStr) * 0.72f;
            SpriteBatch.DrawString(m_Font, floorStr, new Vector2(cardX + (cardWidth - floorSize.X) / 2, cardY + 48), Color.Gold, 0f, Vector2.Zero, 0.72f, SpriteEffects.None, 0f);

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
