using Class3;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form3 : Form, IInputScreen
    {
        private readonly SnakeGameLogic gameLogic;
        private readonly Timer gameTimer = new Timer();
        private readonly List<Timer> transientTimers = new List<Timer>();
        private readonly string assetsPath;

        private Rectangle playArea;
        private int cellSize = 25;
        private bool isCursorVisible;
        private bool bodySegmentsVisible = true;

        private Image snakeHeadImg;
        private Image snakeBodyImg;
        private Image snakeBodyAltImg;
        private Image foodImg;
        private Image specialFoodImg;
        private Image obstacleImg;
        private Image portalImg;
        private Image exitImg;

        private readonly PictureBox heartIcon;
        private readonly Label lblScore;
        private readonly Label lblLevel;
        private readonly Label lblEffect;
        private readonly PictureBox btnExit;

        private Timer flashTimer;
        private Label lblEnter;

        public Form3()
        {
            gameLogic = new SnakeGameLogic();

            KeyPreview = true;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            UpdateStyles();
            BackColor = Color.Black;

            assetsPath = Path.Combine(Application.StartupPath, "Assets");
            LoadAssets();

            heartIcon = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Width = 48,
                Height = 48,
                BackColor = Color.Transparent,
                Visible = false,
                Image = AssetCache.GetOrLoad(assetsPath, "heart.png")
            };
            Controls.Add(heartIcon);

            lblScore = new Label
            {
                Text = "SCORE: 0",
                Font = new Font("Consolas", 24, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true
            };
            Controls.Add(lblScore);

            lblLevel = new Label
            {
                Text = "LEVEL: 1",
                Font = new Font("Consolas", 24, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true
            };
            Controls.Add(lblLevel);

            btnExit = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = exitImg,
                BackColor = Color.Transparent,
                Width = 64,
                Height = 64,
                Cursor = Cursors.Hand
            };
            btnExit.Click += (s, e) => FadeToForm2();
            Controls.Add(btnExit);

            lblEffect = new Label
            {
                Font = new Font("Consolas", 36, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Visible = false
            };
            Controls.Add(lblEffect);

            gameLogic.GameStateChanged += (s, e) => Invalidate();
            gameLogic.GameStatsUpdated += GameLogic_GameStatsUpdated;
            gameLogic.ShowEffectRequested += GameLogic_ShowEffectRequested;
            gameLogic.GameOverOccurred += GameLogic_GameOverOccurred;
            gameLogic.ExtraLifeStatusChanged += GameLogic_ExtraLifeStatusChanged;
            gameLogic.SpeedChanged += GameLogic_SpeedChanged;

            gameTimer.Tick += GameTimer_Tick;

            Load += (s, e) => LayoutHudAndPlayArea();
            Resize += (s, e) => LayoutHudAndPlayArea();
            KeyDown += Form3_KeyDown;
            Disposed += Form3_Disposed;

            gameLogic.StartGame();

            Cursor.Hide();
            lblEffect.Text = "Press any ARROW key to Start";
            lblEffect.Font = new Font("Consolas", 36, FontStyle.Bold);
            lblEffect.Visible = true;
            Invalidate();
        }

        private void LoadAssets()
        {
            snakeHeadImg = AssetCache.GetOrLoad(assetsPath, "snake_head.png");
            snakeBodyImg = AssetCache.GetOrLoad(assetsPath, "snake_body1.png");
            snakeBodyAltImg = AssetCache.GetOrLoad(assetsPath, "snake_body2.png");
            foodImg = AssetCache.GetOrLoad(assetsPath, "food.png");
            specialFoodImg = AssetCache.GetOrLoad(assetsPath, "food_special.png");
            obstacleImg = AssetCache.GetOrLoad(assetsPath, "obstacle.png");
            portalImg = AssetCache.GetOrLoad(assetsPath, "portal.png");
            exitImg = AssetCache.GetOrLoad(assetsPath, "x.png");
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            gameLogic.GameLoopTick();
        }

        private void GameLogic_GameStatsUpdated(object sender, SnakeGameLogic.GameUpdateEventArgs e)
        {
            lblScore.Text = "SCORE: " + e.Score;
            lblLevel.Text = $"LEVEL: {e.Level} - {e.Difficulty}";
            LayoutHudAndPlayArea();
        }

        private void GameLogic_ShowEffectRequested(object sender, SnakeGameLogic.EffectEventArgs e)
        {
            if (e.Message == null)
            {
                StopFlashTimer();
                bodySegmentsVisible = true;
                lblEffect.Visible = false;
                gameTimer.Start();
                return;
            }

            if (e.Message == "-1 LIFE")
            {
                gameTimer.Stop();
                lblEffect.Text = e.Message;
                lblEffect.Visible = true;
                LayoutHudAndPlayArea();

                StopFlashTimer();
                flashTimer = new Timer { Interval = 200 };
                int flashes = 0;
                flashTimer.Tick += (s, args) =>
                {
                    bodySegmentsVisible = flashes % 2 != 0;
                    flashes++;
                    if (flashes > 8)
                    {
                        bodySegmentsVisible = true;
                        StopFlashTimer();
                    }

                    Invalidate();
                };
                flashTimer.Start();
                return;
            }

            if (e.Message == "RESPAWN")
            {
                StartOneShotTimer(e.DurationMs, () => gameLogic.RespawnSpecialFood());
                return;
            }

            string effectMessage = e.Message;
            lblEffect.Text = effectMessage;
            lblEffect.Visible = true;
            LayoutHudAndPlayArea();

            StartOneShotTimer(e.DurationMs, () =>
            {
                if (!gameLogic.IsGameOver && lblEffect.Text == effectMessage)
                    lblEffect.Visible = false;
            });
        }

        private void GameLogic_GameOverOccurred(object sender, EventArgs e)
        {
            gameTimer.Stop();
            StopFlashTimer();
            DisposeTransientTimers();

            lblEffect.Font = new Font("Consolas", 64, FontStyle.Bold);
            lblEffect.Text = "GAME OVER";
            lblEffect.Visible = true;

            if (lblEnter == null)
            {
                lblEnter = new Label
                {
                    Text = "Press ENTER to Continue",
                    Font = new Font("Consolas", 28, FontStyle.Regular),
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    AutoSize = true
                };
                Controls.Add(lblEnter);
                lblEnter.BringToFront();
            }

            LayoutHudAndPlayArea();
        }

        private void GameLogic_ExtraLifeStatusChanged(object sender, bool hasLife)
        {
            heartIcon.Visible = hasLife;
        }

        private void GameLogic_SpeedChanged(object sender, int newInterval)
        {
            gameTimer.Interval = newInterval;
        }

        private void LayoutHudAndPlayArea()
        {
            int margin = 24;
            int topBarHeight = 90;
            int playMargin = 40;

            btnExit.Location = new Point(margin, margin);
            heartIcon.Location = new Point(btnExit.Right + 10, btnExit.Top + 8);
            lblLevel.Location = new Point((ClientSize.Width - lblLevel.Width) / 2, margin + 8);
            lblScore.Location = new Point(ClientSize.Width - lblScore.Width - margin, margin + 8);

            int availableWidth = Math.Max(1, ClientSize.Width - (playMargin * 2));
            int availableHeight = Math.Max(1, ClientSize.Height - topBarHeight - playMargin);

            cellSize = Math.Max(1, Math.Min(availableWidth / gameLogic.PlayCols, availableHeight / gameLogic.PlayRows));
            int gridWidth = cellSize * gameLogic.PlayCols;
            int gridHeight = cellSize * gameLogic.PlayRows;

            int left = (ClientSize.Width - gridWidth) / 2;
            int top = topBarHeight + Math.Max(0, (availableHeight - gridHeight) / 2);
            playArea = new Rectangle(left, top, gridWidth, gridHeight);

            lblEffect.Left = (ClientSize.Width - lblEffect.Width) / 2;
            lblEffect.Top = (ClientSize.Height - lblEffect.Height) / 2;

            if (lblEnter != null)
            {
                lblEnter.Left = (ClientSize.Width - lblEnter.Width) / 2;
                lblEnter.Top = lblEffect.Bottom + 20;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (playArea.Width <= 0 || playArea.Height <= 0 || cellSize <= 0)
                return;

            Graphics g = e.Graphics;
            var snake = gameLogic.Snake;
            var foods = gameLogic.Foods;
            var specialFoods = gameLogic.SpecialFoods;
            var obstacles = gameLogic.Obstacles;
            var portals = gameLogic.Portals;

            Point ToDrawingPoint(GamePoint p) => new Point(playArea.Left + (p.X * cellSize), playArea.Top + (p.Y * cellSize));

            using (Pen border = new Pen(Color.White, 3))
            {
                g.DrawRectangle(border, playArea);
            }

            using (Pen gridPen = new Pen(Color.FromArgb(60, Color.White)))
            {
                for (int x = 0; x <= gameLogic.PlayCols; x++)
                {
                    int px = playArea.Left + (x * cellSize);
                    g.DrawLine(gridPen, px, playArea.Top, px, playArea.Bottom);
                }

                for (int y = 0; y <= gameLogic.PlayRows; y++)
                {
                    int py = playArea.Top + (y * cellSize);
                    g.DrawLine(gridPen, playArea.Left, py, playArea.Right, py);
                }
            }

            foreach (GamePoint obstacle in obstacles)
            {
                Point dp = ToDrawingPoint(obstacle);
                if (obstacleImg != null)
                    g.DrawImage(obstacleImg, dp.X, dp.Y, cellSize, cellSize);
                else
                    g.FillRectangle(Brushes.DarkRed, dp.X, dp.Y, cellSize, cellSize);
            }

            foreach (GamePoint portal in portals)
            {
                Point dp = ToDrawingPoint(portal);
                if (portalImg != null)
                    g.DrawImage(portalImg, dp.X, dp.Y, cellSize, cellSize);
                else
                    g.FillEllipse(Brushes.Blue, dp.X, dp.Y, cellSize, cellSize);
            }

            foreach (GamePoint food in foods)
            {
                Point dp = ToDrawingPoint(food);
                if (foodImg != null)
                    g.DrawImage(foodImg, dp.X, dp.Y, cellSize, cellSize);
                else
                    g.FillEllipse(Brushes.Green, dp.X, dp.Y, cellSize, cellSize);
            }

            foreach (GamePoint specialFood in specialFoods)
            {
                Point dp = ToDrawingPoint(specialFood);
                if (specialFoodImg != null)
                    g.DrawImage(specialFoodImg, dp.X, dp.Y, cellSize, cellSize);
                else
                    g.FillEllipse(Brushes.Gold, dp.X, dp.Y, cellSize, cellSize);
            }

            for (int i = 0; i < snake.Count; i++)
            {
                GamePoint segment = snake[i];
                Point dp = ToDrawingPoint(segment);

                if (i == 0)
                {
                    if (snakeHeadImg != null)
                        g.DrawImage(snakeHeadImg, dp.X, dp.Y, cellSize, cellSize);
                    else
                        g.FillRectangle(Brushes.White, dp.X, dp.Y, cellSize, cellSize);

                    continue;
                }

                if (!bodySegmentsVisible)
                    continue;

                Image bodyImage = ((segment.X + segment.Y) % 2 == 0) ? snakeBodyImg : snakeBodyAltImg;
                if (bodyImage != null)
                    g.DrawImage(bodyImage, dp.X, dp.Y, cellSize, cellSize);
                else
                    g.FillRectangle(Brushes.White, dp.X, dp.Y, cellSize, cellSize);
            }
        }

        private void Form3_KeyDown(object sender, KeyEventArgs e)
        {
            HandleGameKey(e.KeyCode);
        }

        public bool HandleKeyInput(Keys keyData)
        {
            Keys keyCode = keyData & Keys.KeyCode;
            return HandleGameKey(keyCode);
        }

        private bool HandleGameKey(Keys keyCode)
        {
            if (keyCode == Keys.Escape)
            {
                isCursorVisible = !isCursorVisible;
                if (isCursorVisible)
                    Cursor.Show();
                else
                    Cursor.Hide();

                return true;
            }

            if (gameLogic.IsGameOver && keyCode == Keys.Enter)
            {
                FadeToForm6();
                return true;
            }

            string intendedDirection = string.Empty;
            if (keyCode == Keys.Up) intendedDirection = "UP";
            else if (keyCode == Keys.Down) intendedDirection = "DOWN";
            else if (keyCode == Keys.Left) intendedDirection = "LEFT";
            else if (keyCode == Keys.Right) intendedDirection = "RIGHT";

            if (string.IsNullOrEmpty(intendedDirection))
                return false;

            if (!gameTimer.Enabled && !gameLogic.IsGameOver && !gameLogic.IsPausedAfterLifeLoss)
            {
                gameLogic.SetInitialDirection(intendedDirection);
                gameTimer.Start();
                lblEffect.Visible = false;
                return true;
            }

            gameLogic.SetNextDirection(intendedDirection);
            return true;
        }

        private void FadeToForm2()
        {
            gameTimer.Stop();
            Cursor.Show();
            AppNavigator.Navigate(() => new Form2());
        }

        private void FadeToForm6()
        {
            gameTimer.Stop();
            Cursor.Show();
            AppNavigator.Navigate(() => new Form6(gameLogic.Score, gameLogic.Level));
        }

        private void StartOneShotTimer(int durationMs, Action action)
        {
            Timer timer = new Timer { Interval = Math.Max(1, durationMs) };
            EventHandler tickHandler = null;
            tickHandler = (s, e) =>
            {
                timer.Stop();
                timer.Tick -= tickHandler;
                transientTimers.Remove(timer);
                timer.Dispose();

                if (!IsDisposed)
                    action();
            };

            timer.Tick += tickHandler;
            transientTimers.Add(timer);
            timer.Start();
        }

        private void StopFlashTimer()
        {
            if (flashTimer == null)
                return;

            flashTimer.Stop();
            flashTimer.Dispose();
            flashTimer = null;
        }

        private void DisposeTransientTimers()
        {
            foreach (Timer timer in transientTimers.ToArray())
            {
                timer.Stop();
                timer.Dispose();
            }

            transientTimers.Clear();
        }

        private void Form3_Disposed(object sender, EventArgs e)
        {
            gameTimer.Stop();
            gameTimer.Dispose();
            StopFlashTimer();
            DisposeTransientTimers();
            Cursor.Show();
        }
    }
}
