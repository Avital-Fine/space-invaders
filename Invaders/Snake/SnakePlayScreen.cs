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

namespace Invaders.Snake
{
    public class SnakePlayScreen : GameScreen
    {
        public event Action BackToDashboard;

        private const string k_FontName = @"Fonts\Consolas";
        public const int k_GridCols = 32;
        public const int k_GridRows = 24;
        public const int k_CellSize = 22;

        // Sounds
        private const string k_SoundEatApple   = @"Sounds\EnemyKill";
        private const string k_SoundEatGolden  = @"Sounds\LevelWin";
        private const string k_SoundGameOver   = @"Sounds\GameOver";
        private const string k_SoundMenuMove   = @"Sounds\MenuMove";

        private SoundEffectInstance m_SoundEatAppleInstance;
        private SoundEffectInstance m_SoundEatGoldenInstance;
        private SoundEffectInstance m_SoundGameOverInstance;
        private SoundEffectInstance m_SoundMenuMoveInstance;

        // Core Game Components
        private readonly List<Point> m_Snake = new List<Point>();
        private readonly Queue<Point> m_InputBuffer = new Queue<Point>();
        private readonly ScoresDatabase m_ScoresDatabase;
        private readonly Random m_Random = new Random();

        private Point m_CurrentDirection = new Point(1, 0); // Moving East
        private SnakeFood m_CurrentApple;
        private SnakeFood m_GoldenFruit;
        private int m_ApplesEaten;
        private int m_GrowPending;
        private float m_MoveTimer;
        private float m_MoveInterval;

        // Balanced comfortable speed
        private const float k_InitialMoveInterval = 0.175f;
        private const float k_MinMoveInterval = 0.080f;

        // Border mode setting (Solid vs Wrap)
        private bool m_WrapBorders = false;

        private float m_TongueTimer;
        private bool m_IsGameOver;
        private bool m_IsPaused;

        // Scoring & Progression
        private int m_Score;
        private int m_HighScore;
        private int m_Level = 1;
        private string m_PlayerName = "Player";

        // In-game menus
        private int m_PauseMenuIndex = 0;
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

        private Texture2D m_PixelTexture;
        private SpriteFont m_Font;

        public SnakePlayScreen(Game i_Game) : base(i_Game)
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
                m_SoundEatAppleInstance  = sm.LoadSoundEffect(k_SoundEatApple);
                m_SoundEatGoldenInstance = sm.LoadSoundEffect(k_SoundEatGolden);
                m_SoundGameOverInstance  = sm.LoadSoundEffect(k_SoundGameOver);
                m_SoundMenuMoveInstance  = sm.LoadSoundEffect(k_SoundMenuMove);
            }
        }

        private void loadHighScore()
        {
            try
            {
                List<ScoreEntry> topScores = m_ScoresDatabase.GetTopScores(1, "Snake");
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
            m_Snake.Clear();
            m_InputBuffer.Clear();

            // Spawn snake in the center of the grid moving right
            int startX = k_GridCols / 3;
            int startY = k_GridRows / 2;
            m_Snake.Add(new Point(startX, startY));
            m_Snake.Add(new Point(startX - 1, startY));
            m_Snake.Add(new Point(startX - 2, startY));
            m_Snake.Add(new Point(startX - 3, startY));

            m_CurrentDirection = new Point(1, 0);
            m_ApplesEaten = 0;
            m_GrowPending = 0;
            m_Score = 0;
            m_Level = 1;
            m_MoveInterval = k_InitialMoveInterval;
            m_MoveTimer = 0f;
            m_TongueTimer = 0f;
            m_GoldenFruit = null;
            m_IsGameOver = false;
            m_IsPaused = false;
            m_PauseMenuIndex = 0;
            m_GameOverMenuIndex = 0;

            spawnApple();
        }

        private void spawnApple()
        {
            Point pos;
            do
            {
                pos = new Point(m_Random.Next(0, k_GridCols), m_Random.Next(0, k_GridRows));
            }
            while (isOccupiedBySnake(pos) || (m_GoldenFruit != null && m_GoldenFruit.GridPosition == pos));

            m_CurrentApple = new SnakeFood(pos, eFoodType.Apple, m_Level);
        }

        private void spawnGoldenFruit()
        {
            Point pos;
            do
            {
                pos = new Point(m_Random.Next(0, k_GridCols), m_Random.Next(0, k_GridRows));
            }
            while (isOccupiedBySnake(pos) || (m_CurrentApple != null && m_CurrentApple.GridPosition == pos));

            m_GoldenFruit = new SnakeFood(pos, eFoodType.GoldenFruit, m_Level);
        }

        private bool isOccupiedBySnake(Point i_Pos)
        {
            return m_Snake.Contains(i_Pos);
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
                m_ScoresDatabase.SaveScore(m_PlayerName, m_Score, m_ApplesEaten, "Snake");
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

        private bool isButtonPressed(Buttons i_Button)
        {
            return m_CurrGamePad.IsButtonDown(i_Button) && m_PrevGamePad.IsButtonUp(i_Button);
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

            // Toggle borders shortcut (B key or Gamepad X)
            if (isKeyPressed(Keys.B) || isButtonPressed(Buttons.X))
            {
                m_WrapBorders = !m_WrapBorders;
                m_SoundMenuMoveInstance?.Play();
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
            m_TongueTimer += dt;

            // Read directional inputs into buffer
            handleDirectionalInput();

            // Update Golden Fruit timer
            if (m_GoldenFruit != null)
            {
                m_GoldenFruit.Timer -= dt;
                if (m_GoldenFruit.Timer <= 0)
                {
                    m_GoldenFruit = null;
                }
            }

            // Step snake forward on movement tick
            m_MoveTimer += dt;
            if (m_MoveTimer >= m_MoveInterval)
            {
                m_MoveTimer = 0f;
                stepSnakeForward();
            }
        }

        private void handleDirectionalInput()
        {
            Point lastDir = m_InputBuffer.Count > 0 ? m_InputBuffer.ToArray()[m_InputBuffer.Count - 1] : m_CurrentDirection;

            // Up
            if (isKeyPressed(Keys.Up) || isKeyPressed(Keys.W) || isButtonPressed(Buttons.DPadUp) || (m_CurrGamePad.ThumbSticks.Left.Y > 0.5f && m_PrevGamePad.ThumbSticks.Left.Y <= 0.5f))
            {
                if (lastDir.Y != 1 && lastDir.Y != -1 && m_InputBuffer.Count < 2)
                {
                    m_InputBuffer.Enqueue(new Point(0, -1));
                }
            }
            // Down
            else if (isKeyPressed(Keys.Down) || isKeyPressed(Keys.S) || isButtonPressed(Buttons.DPadDown) || (m_CurrGamePad.ThumbSticks.Left.Y < -0.5f && m_PrevGamePad.ThumbSticks.Left.Y >= -0.5f))
            {
                if (lastDir.Y != -1 && lastDir.Y != 1 && m_InputBuffer.Count < 2)
                {
                    m_InputBuffer.Enqueue(new Point(0, 1));
                }
            }
            // Left
            else if (isKeyPressed(Keys.Left) || isKeyPressed(Keys.A) || isButtonPressed(Buttons.DPadLeft) || (m_CurrGamePad.ThumbSticks.Left.X < -0.5f && m_PrevGamePad.ThumbSticks.Left.X >= -0.5f))
            {
                if (lastDir.X != 1 && lastDir.X != -1 && m_InputBuffer.Count < 2)
                {
                    m_InputBuffer.Enqueue(new Point(-1, 0));
                }
            }
            // Right
            else if (isKeyPressed(Keys.Right) || isKeyPressed(Keys.D) || isButtonPressed(Buttons.DPadRight) || (m_CurrGamePad.ThumbSticks.Left.X > 0.5f && m_PrevGamePad.ThumbSticks.Left.X <= 0.5f))
            {
                if (lastDir.X != -1 && lastDir.X != 1 && m_InputBuffer.Count < 2)
                {
                    m_InputBuffer.Enqueue(new Point(1, 0));
                }
            }
        }

        private void stepSnakeForward()
        {
            if (m_InputBuffer.Count > 0)
            {
                m_CurrentDirection = m_InputBuffer.Dequeue();
            }

            Point head = m_Snake[0];
            Point newHead = new Point(head.X + m_CurrentDirection.X, head.Y + m_CurrentDirection.Y);

            // 1. Check Wall Collision or Wrap Around
            if (m_WrapBorders)
            {
                if (newHead.X < 0) newHead.X = k_GridCols - 1;
                else if (newHead.X >= k_GridCols) newHead.X = 0;

                if (newHead.Y < 0) newHead.Y = k_GridRows - 1;
                else if (newHead.Y >= k_GridRows) newHead.Y = 0;
            }
            else
            {
                if (newHead.X < 0 || newHead.X >= k_GridCols || newHead.Y < 0 || newHead.Y >= k_GridRows)
                {
                    triggerGameOver();
                    return;
                }
            }

            // 2. Check Self Collision (ignoring the moving tail if not growing)
            int checkLimit = m_GrowPending > 0 ? m_Snake.Count : m_Snake.Count - 1;
            for (int i = 0; i < checkLimit; i++)
            {
                if (m_Snake[i] == newHead)
                {
                    triggerGameOver();
                    return;
                }
            }

            // 3. Move Snake Head Forward
            m_Snake.Insert(0, newHead);

            // 4. Check Food Collisions

            // Standard Apple
            if (m_CurrentApple != null && newHead == m_CurrentApple.GridPosition)
            {
                m_Score += m_CurrentApple.ScoreValue;
                m_ApplesEaten++;
                m_GrowPending += 1;

                m_SoundEatAppleInstance?.Play();

                // Gentle speed progression
                m_Level = (m_ApplesEaten / 5) + 1;
                m_MoveInterval = Math.Max(k_MinMoveInterval, k_InitialMoveInterval - (m_ApplesEaten * 0.0016f));

                // Spawn new apple
                spawnApple();

                // Spawn Golden Bonus Fruit every 5 apples
                if (m_ApplesEaten % 5 == 0)
                {
                    spawnGoldenFruit();
                }
            }
            // Golden Bonus Fruit
            else if (m_GoldenFruit != null && newHead == m_GoldenFruit.GridPosition)
            {
                m_Score += m_GoldenFruit.ScoreValue;
                m_GrowPending += 2;
                m_GoldenFruit = null;

                m_SoundEatGoldenInstance?.Play();
            }

            // 5. Handle Growth vs Tail Removal
            if (m_GrowPending > 0)
            {
                m_GrowPending--;
            }
            else
            {
                m_Snake.RemoveAt(m_Snake.Count - 1);
            }
        }

        private string[] getPauseMenuItems()
        {
            string borderSetting = m_WrapBorders ? "Borders: Wrap (No Walls)" : "Borders: Solid (Deadly)";
            return new string[]
            {
                "Resume",
                borderSetting,
                "Mute / Unmute",
                "Leaderboard",
                "Restart Game",
                "Quit to Dashboard"
            };
        }

        private void updatePauseMenu()
        {
            string[] items = getPauseMenuItems();

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
                m_PauseMenuIndex = (m_PauseMenuIndex + items.Length - 1) % items.Length;
                m_SoundMenuMoveInstance?.Play();
            }
            else if (isKeyPressed(Keys.Down) || isKeyPressed(Keys.S) || isButtonPressed(Buttons.DPadDown))
            {
                m_PauseMenuIndex = (m_PauseMenuIndex + 1) % items.Length;
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
                case 1: // Toggle Borders
                    m_WrapBorders = !m_WrapBorders;
                    m_SoundMenuMoveInstance?.Play();
                    break;
                case 2: // Mute / Unmute
                    (Game as BaseGame)?.SoundsManager.MuteToggle();
                    m_SoundMenuMoveInstance?.Play();
                    break;
                case 3: // Leaderboard
                    ScreensManager.SetCurrentScreen(new LeaderboardScreen(Game, 3)); // Tab 3 = Snake
                    break;
                case 4: // Restart Game
                    restartGame();
                    break;
                case 5: // Quit to Dashboard
                    onBackToDashboard();
                    break;
            }
        }

        private void updateGameOverMenu()
        {
            if (isKeyPressed(Keys.L) || isButtonPressed(Buttons.Y) || isButtonPressed(Buttons.X))
            {
                ScreensManager.SetCurrentScreen(new LeaderboardScreen(Game, 3));
                return;
            }

            if (isEscOrBack())
            {
                onBackToDashboard();
                return;
            }

            if (isKeyPressed(Keys.R))
            {
                restartGame();
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
                    restartGame();
                    break;
                case 1: // Leaderboard
                    ScreensManager.SetCurrentScreen(new LeaderboardScreen(Game, 3));
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
            int arenaPixelWidth = k_GridCols * k_CellSize;
            int arenaPixelHeight = k_GridRows * k_CellSize;

            int arenaX = (vp.Width - arenaPixelWidth) / 2;
            int arenaY = (vp.Height - arenaPixelHeight) / 2 + 25;

            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // 1. Dark Backdrop
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(0, 0, vp.Width, vp.Height), new Color(10, 14, 24, 255));

            // 2. Arena Board & Grid
            drawArena(arenaX, arenaY, arenaPixelWidth, arenaPixelHeight);

            // 3. Draw Apples & Bonus Fruit
            drawFoodItems(arenaX, arenaY);

            // 4. Draw Snake Body & Head
            drawSnake(arenaX, arenaY);

            // 5. In-Game HUD
            drawHUD(vp, arenaX, arenaY, arenaPixelWidth);

            // 6. Pause Overlay
            if (m_IsPaused)
            {
                drawPauseOverlay(arenaX, arenaY, arenaPixelWidth, arenaPixelHeight);
            }

            // 7. Game Over Overlay
            if (m_IsGameOver)
            {
                drawGameOverOverlay(arenaX, arenaY, arenaPixelWidth, arenaPixelHeight);
            }

            SpriteBatch.End();
        }

        private void drawArena(int i_X, int i_Y, int i_Width, int i_Height)
        {
            // Border color reflects the active mode (Emerald for solid walls, Portal Cyan for wrap-around)
            Color borderColor = m_WrapBorders ? new Color(60, 200, 255, 230) : new Color(30, 200, 100, 220);

            // Outer Neon Border Frame
            drawRectOutline(new Rectangle(i_X - 4, i_Y - 4, i_Width + 8, i_Height + 8), borderColor, 3);

            // Inner Checkerboard Pattern
            for (int col = 0; col < k_GridCols; col++)
            {
                for (int row = 0; row < k_GridRows; row++)
                {
                    Rectangle cellRect = new Rectangle(i_X + col * k_CellSize, i_Y + row * k_CellSize, k_CellSize, k_CellSize);
                    Color cellColor = ((col + row) % 2 == 0) ? new Color(15, 20, 32, 245) : new Color(19, 25, 40, 245);
                    SpriteBatch.Draw(m_PixelTexture, cellRect, cellColor);
                }
            }
        }

        private void drawFoodItems(int i_ArenaX, int i_ArenaY)
        {
            // 1. Standard Red Apple
            if (m_CurrentApple != null)
            {
                Rectangle rect = new Rectangle(i_ArenaX + m_CurrentApple.GridPosition.X * k_CellSize + 2, i_ArenaY + m_CurrentApple.GridPosition.Y * k_CellSize + 2, k_CellSize - 4, k_CellSize - 4);

                // Apple body
                SpriteBatch.Draw(m_PixelTexture, rect, new Color(235, 45, 45));
                drawRectOutline(rect, new Color(140, 15, 15), 1);

                // Leaf
                SpriteBatch.Draw(m_PixelTexture, new Rectangle(rect.X + rect.Width / 2, rect.Y - 2, 4, 3), Color.LightGreen);
                // Highlight gloss
                SpriteBatch.Draw(m_PixelTexture, new Rectangle(rect.X + 3, rect.Y + 3, 3, 3), Color.White * 0.7f);
            }

            // 2. Golden Bonus Fruit
            if (m_GoldenFruit != null)
            {
                Rectangle rect = new Rectangle(i_ArenaX + m_GoldenFruit.GridPosition.X * k_CellSize + 1, i_ArenaY + m_GoldenFruit.GridPosition.Y * k_CellSize + 1, k_CellSize - 2, k_CellSize - 2);

                // Pulsating golden orb
                SpriteBatch.Draw(m_PixelTexture, rect, Color.Gold);
                drawRectOutline(rect, Color.OrangeRed, 2);

                // Sparkle
                SpriteBatch.Draw(m_PixelTexture, new Rectangle(rect.X + 3, rect.Y + 3, 4, 4), Color.White);
            }
        }

        private void drawSnake(int i_ArenaX, int i_ArenaY)
        {
            for (int i = m_Snake.Count - 1; i >= 0; i--)
            {
                Point pos = m_Snake[i];
                Rectangle cellRect = new Rectangle(i_ArenaX + pos.X * k_CellSize, i_ArenaY + pos.Y * k_CellSize, k_CellSize, k_CellSize);

                if (i == 0)
                {
                    // Head
                    drawSnakeHead(cellRect);
                }
                else
                {
                    // Body Segment
                    float ratio = (float)i / m_Snake.Count;
                    Color bodyColor = Color.Lerp(new Color(40, 220, 100), new Color(20, 140, 60), ratio);
                    Color highlightColor = Color.Lerp(new Color(140, 255, 170), new Color(50, 190, 90), ratio);

                    Rectangle bodyRect = new Rectangle(cellRect.X + 1, cellRect.Y + 1, cellRect.Width - 2, cellRect.Height - 2);
                    SpriteBatch.Draw(m_PixelTexture, bodyRect, bodyColor);

                    // Scale pattern bevel
                    SpriteBatch.Draw(m_PixelTexture, new Rectangle(bodyRect.X + 2, bodyRect.Y + 2, bodyRect.Width - 4, 2), highlightColor);
                    drawRectOutline(bodyRect, new Color(10, 50, 20, 200), 1);
                }
            }
        }

        private void drawSnakeHead(Rectangle i_Rect)
        {
            Rectangle headRect = new Rectangle(i_Rect.X + 1, i_Rect.Y + 1, i_Rect.Width - 2, i_Rect.Height - 2);
            SpriteBatch.Draw(m_PixelTexture, headRect, new Color(50, 240, 115));
            drawRectOutline(headRect, new Color(15, 70, 30), 1);

            // Eyes
            int eyeSize = 4;
            int eyeX1, eyeY1, eyeX2, eyeY2;

            if (m_CurrentDirection.X == 1) // Right
            {
                eyeX1 = eyeX2 = headRect.Right - 5;
                eyeY1 = headRect.Top + 3;
                eyeY2 = headRect.Bottom - 7;
            }
            else if (m_CurrentDirection.X == -1) // Left
            {
                eyeX1 = eyeX2 = headRect.Left + 2;
                eyeY1 = headRect.Top + 3;
                eyeY2 = headRect.Bottom - 7;
            }
            else if (m_CurrentDirection.Y == -1) // Up
            {
                eyeX1 = headRect.Left + 3;
                eyeX2 = headRect.Right - 7;
                eyeY1 = eyeY2 = headRect.Top + 2;
            }
            else // Down
            {
                eyeX1 = headRect.Left + 3;
                eyeX2 = headRect.Right - 7;
                eyeY1 = eyeY2 = headRect.Bottom - 5;
            }

            SpriteBatch.Draw(m_PixelTexture, new Rectangle(eyeX1, eyeY1, eyeSize, eyeSize), Color.White);
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(eyeX2, eyeY2, eyeSize, eyeSize), Color.White);
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(eyeX1 + 1, eyeY1 + 1, 2, 2), Color.Black);
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(eyeX2 + 1, eyeY2 + 1, 2, 2), Color.Black);

            // Flicking Tongue
            if (m_TongueTimer % 0.8f < 0.25f)
            {
                Rectangle tongueRect;
                if (m_CurrentDirection.X == 1) tongueRect = new Rectangle(headRect.Right, headRect.Center.Y - 1, 5, 2);
                else if (m_CurrentDirection.X == -1) tongueRect = new Rectangle(headRect.Left - 5, headRect.Center.Y - 1, 5, 2);
                else if (m_CurrentDirection.Y == -1) tongueRect = new Rectangle(headRect.Center.X - 1, headRect.Top - 5, 2, 5);
                else tongueRect = new Rectangle(headRect.Center.X - 1, headRect.Bottom, 2, 5);

                SpriteBatch.Draw(m_PixelTexture, tongueRect, Color.Crimson);
            }
        }

        private void drawHUD(Viewport i_Vp, int i_ArenaX, int i_ArenaY, int i_ArenaWidth)
        {
            // Top HUD Bar
            Rectangle hudBar = new Rectangle(i_ArenaX, i_ArenaY - 50, i_ArenaWidth, 40);
            SpriteBatch.Draw(m_PixelTexture, hudBar, new Color(14, 18, 30, 240));
            drawRectOutline(hudBar, new Color(40, 70, 110, 200), 1);

            // Stats
            string scoreStr = $"SCORE {m_Score:N0}";
            SpriteBatch.DrawString(m_Font, scoreStr, new Vector2(i_ArenaX + 15, i_ArenaY - 42), Color.Gold, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

            string highStr = $"BEST {m_HighScore:N0}";
            SpriteBatch.DrawString(m_Font, highStr, new Vector2(i_ArenaX + 200, i_ArenaY - 42), new Color(255, 170, 50), 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

            string appleStr = $"APPLES {m_ApplesEaten}";
            SpriteBatch.DrawString(m_Font, appleStr, new Vector2(i_ArenaX + 370, i_ArenaY - 42), Color.LightGreen, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

            string lvlStr = $"LVL {m_Level}";
            SpriteBatch.DrawString(m_Font, lvlStr, new Vector2(i_ArenaX + 510, i_ArenaY - 42), Color.Cyan, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

            // Mode Tag in Top HUD
            string modeStr = m_WrapBorders ? "WRAP" : "SOLID";
            Color modeColor = m_WrapBorders ? Color.Cyan : new Color(120, 240, 150);
            SpriteBatch.DrawString(m_Font, $"[{modeStr}]", new Vector2(i_ArenaX + 610, i_ArenaY - 42), modeColor, 0f, Vector2.Zero, 0.82f, SpriteEffects.None, 0f);

            // Bottom Info Bar (Golden Fruit countdown or Border Toggle Hint)
            int bottomY = i_ArenaY + k_GridRows * k_CellSize + 10;
            if (m_GoldenFruit != null)
            {
                string bonusStr = $"BONUS FRUIT: {m_GoldenFruit.Timer:F1}s (+{m_GoldenFruit.ScoreValue:N0})";
                Vector2 size = m_Font.MeasureString(bonusStr) * 0.72f;
                SpriteBatch.DrawString(m_Font, bonusStr, new Vector2(i_ArenaX + (i_ArenaWidth - size.X) / 2, bottomY), Color.Gold, 0f, Vector2.Zero, 0.72f, SpriteEffects.None, 0f);
            }
            else
            {
                string tip = "[B] Toggle Borders Mode  |  [P / Esc] Pause";
                Vector2 size = m_Font.MeasureString(tip) * 0.58f;
                SpriteBatch.DrawString(m_Font, tip, new Vector2(i_ArenaX + (i_ArenaWidth - size.X) / 2, bottomY), new Color(140, 160, 190), 0f, Vector2.Zero, 0.58f, SpriteEffects.None, 0f);
            }
        }

        private void drawPauseOverlay(int i_ArenaX, int i_ArenaY, int i_Width, int i_Height)
        {
            SpriteBatch.Draw(m_PixelTexture, new Rectangle(i_ArenaX, i_ArenaY, i_Width, i_Height), new Color(5, 8, 18, 225));

            string[] items = getPauseMenuItems();

            int cardWidth = 280;
            int cardHeight = 285;
            int cardX = i_ArenaX + (i_Width - cardWidth) / 2;
            int cardY = i_ArenaY + (i_Height - cardHeight) / 2;

            Rectangle cardRect = new Rectangle(cardX, cardY, cardWidth, cardHeight);
            SpriteBatch.Draw(m_PixelTexture, cardRect, new Color(20, 24, 40, 250));
            drawRectOutline(cardRect, Color.Cyan, 2);

            string title = "GAME PAUSED";
            Vector2 titleSize = m_Font.MeasureString(title) * 1.0f;
            SpriteBatch.DrawString(m_Font, title, new Vector2(cardX + (cardWidth - titleSize.X) / 2, cardY + 16), Color.Gold, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);

            int startItemY = cardY + 50;
            for (int i = 0; i < items.Length; i++)
            {
                bool isSelected = (i == m_PauseMenuIndex);
                Rectangle itemRect = new Rectangle(cardX + 15, startItemY + i * 32, cardWidth - 30, 26);

                if (isSelected)
                {
                    SpriteBatch.Draw(m_PixelTexture, itemRect, new Color(50, 70, 130, 240));
                    drawRectOutline(itemRect, Color.Gold, 1);
                }

                string label = isSelected ? $"> {items[i]}" : $"  {items[i]}";
                Color textColor = isSelected ? Color.Gold : new Color(200, 210, 230);
                SpriteBatch.DrawString(m_Font, label, new Vector2(itemRect.X + 6, itemRect.Y + 4), textColor, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
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
            string scoreStr = $"Score: {m_Score:N0}   Apples: {m_ApplesEaten}";
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
