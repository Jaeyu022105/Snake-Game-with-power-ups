using System;
using System.Collections.Generic;
using System.Linq;

namespace Class3
{
    public struct GamePoint : IEquatable<GamePoint>
    {
        public int X { get; set; }
        public int Y { get; set; }

        public GamePoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(GamePoint other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GamePoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = (hash * 31) + X.GetHashCode();
            hash = (hash * 31) + Y.GetHashCode();
            return hash;
        }

        public static bool operator ==(GamePoint left, GamePoint right) => left.Equals(right);
        public static bool operator !=(GamePoint left, GamePoint right) => !(left == right);
    }

    public class SnakeGameLogic
    {
        private const int BaseGridSize = 20;

        private readonly List<GamePoint> snake = new List<GamePoint>();
        private readonly List<GamePoint> foods = new List<GamePoint>();
        private readonly List<GamePoint> specialFoods = new List<GamePoint>();
        private readonly List<GamePoint> obstacles = new List<GamePoint>();
        private readonly List<GamePoint> portals = new List<GamePoint>();
        private readonly Random rand = new Random();

        private string direction = "RIGHT";
        private string nextDirection = "RIGHT";
        private int score;
        private int level = 1;
        private bool gameOver;
        private bool growPending;
        private bool hasExtraLife;
        private float speedMultiplier = 1f;
        private bool reverseControls;
        private int playCols = BaseGridSize;
        private int playRows = BaseGridSize;
        private int reverseControlsDuration;
        private int speedEffectDuration;
        private int targetNormalFoodCount;
        private int targetSpecialFoodCount;

        public bool IsPausedAfterLifeLoss { get; private set; }

        public event EventHandler GameStateChanged;
        public event EventHandler<GameUpdateEventArgs> GameStatsUpdated;
        public event EventHandler<EffectEventArgs> ShowEffectRequested;
        public event EventHandler GameOverOccurred;
        public event EventHandler<bool> ExtraLifeStatusChanged;
        public event EventHandler<int> SpeedChanged;

        public int Score => score;
        public int Level => level;
        public bool IsGameOver => gameOver;
        public float SpeedMultiplier => speedMultiplier;
        public int PlayCols => playCols;
        public int PlayRows => playRows;
        public bool ReverseControls => reverseControls;
        public bool HasExtraLife => hasExtraLife;

        public IReadOnlyList<GamePoint> Snake => snake;
        public IReadOnlyList<GamePoint> Foods => foods;
        public IReadOnlyList<GamePoint> SpecialFoods => specialFoods;
        public IReadOnlyList<GamePoint> Obstacles => obstacles;
        public IReadOnlyList<GamePoint> Portals => portals;

        public int BaseTimerInterval = 100;
        public int CurrentTimerInterval => Math.Max(30, (int)(BaseTimerInterval / speedMultiplier));

        public void SetInitialDirection(string startDirection)
        {
            if (!string.IsNullOrEmpty(direction))
                return;

            string normalizedDirection = NormalizeDirection(startDirection);
            if (string.IsNullOrEmpty(normalizedDirection))
                return;

            direction = normalizedDirection;
            nextDirection = normalizedDirection;
        }

        public void RespawnSpecialFood()
        {
            if (gameOver || targetSpecialFoodCount <= 0 || specialFoods.Count >= targetSpecialFoodCount)
                return;

            GamePoint? spawnPoint = GetRandomEmptyCell();
            if (!spawnPoint.HasValue)
                return;

            specialFoods.Add(spawnPoint.Value);
            GameStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void StartGame()
        {
            playCols = BaseGridSize;
            playRows = BaseGridSize;

            snake.Clear();
            GamePoint center = new GamePoint(playCols / 2, playRows / 2);
            snake.Add(center);

            direction = string.Empty;
            nextDirection = string.Empty;
            score = 0;
            level = 1;
            gameOver = false;
            growPending = false;
            speedMultiplier = 1f;
            hasExtraLife = false;
            IsPausedAfterLifeLoss = false;
            reverseControls = false;
            reverseControlsDuration = 0;
            speedEffectDuration = 0;
            targetNormalFoodCount = 0;
            targetSpecialFoodCount = 0;

            foods.Clear();
            specialFoods.Clear();
            obstacles.Clear();
            portals.Clear();

            GenerateLevelObjectsForCurrentLevel();
            SpeedChanged?.Invoke(this, CurrentTimerInterval);
            GameStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void GameLoopTick()
        {
            if (gameOver || IsPausedAfterLifeLoss || string.IsNullOrEmpty(direction))
                return;

            HandleEffectDecay();

            direction = nextDirection;
            GamePoint stepPosition = GetNextHead();
            bool tailWillMove = !growPending;

            if (CheckCollision(stepPosition, tailWillMove))
            {
                HandleCollision();
                return;
            }

            GamePoint newHead = ResolvePortalDestination(stepPosition);
            if (newHead != stepPosition && CheckCollision(newHead, tailWillMove))
            {
                HandleCollision();
                return;
            }

            bool ateNormal = foods.Contains(newHead);
            bool ateSpecial = specialFoods.Contains(newHead);

            snake.Insert(0, newHead);
            if (!growPending)
                snake.RemoveAt(snake.Count - 1);
            else
                growPending = false;

            if (ateNormal)
            {
                foods.Remove(newHead);
                EatNormalFood();
            }
            else if (ateSpecial)
            {
                specialFoods.Remove(newHead);
                EatSpecialFood();
            }

            GameStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetNextDirection(string intendedDirection)
        {
            if (gameOver)
                return;

            string finalDirection = ApplyControlEffects(intendedDirection);
            if (string.IsNullOrEmpty(finalDirection))
                return;

            if (IsPausedAfterLifeLoss)
            {
                IsPausedAfterLifeLoss = false;
                direction = finalDirection;
                nextDirection = finalDirection;
                ShowEffectRequested?.Invoke(this, new EffectEventArgs(null, 0));
                GameStateChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (CanChangeDirection(finalDirection))
                nextDirection = finalDirection;
        }

        public void UpdateGridSize(int newCols, int newRows)
        {
            playCols = newCols;
            playRows = newRows;
        }

        private GamePoint GetNextHead()
        {
            GamePoint head = snake[0];
            int dx = 0;
            int dy = 0;

            if (direction == "UP") dy = -1;
            else if (direction == "DOWN") dy = 1;
            else if (direction == "LEFT") dx = -1;
            else if (direction == "RIGHT") dx = 1;

            return new GamePoint(head.X + dx, head.Y + dy);
        }

        private GamePoint ResolvePortalDestination(GamePoint entryPoint)
        {
            if (!portals.Contains(entryPoint))
                return entryPoint;

            int index = portals.IndexOf(entryPoint);
            int pairIndex = index % 2 == 0 ? index + 1 : index - 1;

            if (pairIndex < 0 || pairIndex >= portals.Count)
                return entryPoint;

            return portals[pairIndex];
        }

        private void OnGameStatsUpdated()
        {
            string difficulty;
            if (level <= 3) difficulty = "EASY";
            else if (level <= 6) difficulty = "MODERATE";
            else difficulty = "HARD";

            GameStatsUpdated?.Invoke(this, new GameUpdateEventArgs(score, level, difficulty));
        }

        private bool CheckCollision(GamePoint newHead, bool tailWillMove)
        {
            if (newHead.X < 0 || newHead.Y < 0 || newHead.X >= playCols || newHead.Y >= playRows)
                return true;

            if (obstacles.Contains(newHead))
                return true;

            int bodyCheckLimit = tailWillMove ? snake.Count - 1 : snake.Count;
            for (int i = 1; i < bodyCheckLimit; i++)
            {
                if (snake[i] == newHead)
                    return true;
            }

            return false;
        }

        private void HandleCollision()
        {
            if (hasExtraLife)
            {
                LoseExtraLife();
                return;
            }

            EndGame();
        }

        private void HandleEffectDecay()
        {
            if (reverseControlsDuration > 0)
            {
                reverseControlsDuration--;
                if (reverseControlsDuration == 0)
                    reverseControls = false;
            }

            if (speedEffectDuration > 0)
            {
                speedEffectDuration--;
                if (speedEffectDuration == 0)
                {
                    speedMultiplier = 1f;
                    SpeedChanged?.Invoke(this, CurrentTimerInterval);
                }
            }
        }

        private void EatNormalFood()
        {
            growPending = true;

            int points;
            if (level == 1) points = 2;
            else if (level == 2) points = 3;
            else points = 4;

            score += points;
            EnsureNormalFoodCount();

            bool leveledUp = CheckLevelUp();
            if (!leveledUp)
                OnGameStatsUpdated();
        }

        private void EatSpecialFood()
        {
            growPending = true;

            int roll = rand.Next(100);
            if (roll < 45)
            {
                int bonus = 20;
                score += bonus;
                ShowEffectRequested?.Invoke(this, new EffectEventArgs("BONUS +" + bonus, 2000));
            }
            else if (roll < 75)
            {
                ApplySpeedBoost();
            }
            else if (roll < 91)
            {
                ApplyReverseControls();
            }
            else if (roll < 97)
            {
                ApplySlowMotion();
            }
            else
            {
                GrantExtraLife();
            }

            ShowEffectRequested?.Invoke(this, new EffectEventArgs("RESPAWN", 3000));
            EnsureNormalFoodCount();

            bool leveledUp = CheckLevelUp();
            if (!leveledUp)
                OnGameStatsUpdated();
        }

        private bool CheckLevelUp()
        {
            bool leveledUp = false;

            while (score >= GetLevelThreshold(level))
            {
                level++;
                UpdateLevelGridGrowth();
                leveledUp = true;
            }

            if (!leveledUp)
                return false;

            GenerateLevelObjectsForCurrentLevel();
            return true;
        }

        private int GetLevelThreshold(int currentLevel)
        {
            int levelFactor = 50;
            if (currentLevel > 3)
                levelFactor += (currentLevel - 3) * 5;

            return currentLevel * levelFactor;
        }

        private void UpdateLevelGridGrowth()
        {
            playCols += 1;
            playRows = playCols;
        }

        private void GenerateLevelObjectsForCurrentLevel()
        {
            foods.Clear();
            specialFoods.Clear();
            obstacles.Clear();
            portals.Clear();

            int totalFoodCount;
            int requestedSpecialCount;
            int obstacleCount;
            string difficulty;
            int announcedObstacleGain;

            if (level <= 3)
            {
                difficulty = "EASY";
                announcedObstacleGain = 3;
            }
            else if (level <= 6)
            {
                difficulty = "MODERATE";
                announcedObstacleGain = 5;
            }
            else
            {
                difficulty = "HARD";
                announcedObstacleGain = 10;
            }

            if (level == 1)
            {
                totalFoodCount = 5;
                requestedSpecialCount = 1;
            }
            else if (level == 2)
            {
                totalFoodCount = 7;
                requestedSpecialCount = 2;
            }
            else if (level == 3)
            {
                totalFoodCount = 10;
                requestedSpecialCount = 4;
            }
            else
            {
                int factor = level - 2;
                totalFoodCount = 10 * factor;
                requestedSpecialCount = 4 * factor;
            }

            List<GamePoint> candidateFoods = new List<GamePoint>();
            for (int i = 0; i < totalFoodCount; i++)
            {
                GamePoint? spawnPoint = GetRandomEmptyCell();
                if (!spawnPoint.HasValue)
                    break;

                foods.Add(spawnPoint.Value);
                candidateFoods.Add(spawnPoint.Value);
            }

            int actualSpecialCount = Math.Min(requestedSpecialCount, candidateFoods.Count);
            foreach (GamePoint specialFood in candidateFoods.OrderBy(_ => rand.Next()).Take(actualSpecialCount))
            {
                specialFoods.Add(specialFood);
                foods.Remove(specialFood);
            }

            targetNormalFoodCount = foods.Count;
            targetSpecialFoodCount = specialFoods.Count;

            if (level <= 3) obstacleCount = level * 3;
            else if (level <= 6) obstacleCount = 9 + ((level - 3) * 5);
            else obstacleCount = 24 + ((level - 6) * 10);

            for (int i = 0; i < obstacleCount; i++)
            {
                GamePoint? obstacleSpawn = GetRandomEmptyCell(IsSafeHazardSpawn);
                if (!obstacleSpawn.HasValue)
                    obstacleSpawn = GetRandomEmptyCell();

                if (!obstacleSpawn.HasValue)
                    break;

                obstacles.Add(obstacleSpawn.Value);
            }

            if (level >= 2)
            {
                int pairs = Math.Max(1, level - 1);
                for (int i = 0; i < pairs; i++)
                {
                    GamePoint? firstPortal = GetRandomEmptyCell(IsSafeHazardSpawn);
                    if (!firstPortal.HasValue)
                        break;

                    portals.Add(firstPortal.Value);
                    GamePoint? secondPortal = GetRandomEmptyCell(IsSafeHazardSpawn);
                    if (!secondPortal.HasValue)
                    {
                        portals.RemoveAt(portals.Count - 1);
                        break;
                    }

                    portals.Add(secondPortal.Value);
                }
            }

            ShowEffectRequested?.Invoke(this, new EffectEventArgs($"LEVEL {level} - {difficulty}\n+{announcedObstacleGain} OBSTACLES", 2000));
            OnGameStatsUpdated();
            GameStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private bool IsSafeHazardSpawn(GamePoint point)
        {
            if (snake.Count == 0)
                return true;

            GamePoint head = snake[0];
            return Math.Abs(point.X - head.X) > 2 || Math.Abs(point.Y - head.Y) > 2;
        }

        private GamePoint? GetRandomEmptyCell(Func<GamePoint, bool> extraFilter = null)
        {
            List<GamePoint> availableCells = new List<GamePoint>();

            for (int y = 0; y < playRows; y++)
            {
                for (int x = 0; x < playCols; x++)
                {
                    GamePoint point = new GamePoint(x, y);
                    if (IsOccupied(point))
                        continue;

                    if (extraFilter != null && !extraFilter(point))
                        continue;

                    availableCells.Add(point);
                }
            }

            if (availableCells.Count == 0)
                return null;

            return availableCells[rand.Next(availableCells.Count)];
        }

        private bool IsOccupied(GamePoint point)
        {
            return snake.Contains(point)
                || obstacles.Contains(point)
                || portals.Contains(point)
                || foods.Contains(point)
                || specialFoods.Contains(point);
        }

        private void EnsureNormalFoodCount()
        {
            while (foods.Count < targetNormalFoodCount)
            {
                GamePoint? spawnPoint = GetRandomEmptyCell();
                if (!spawnPoint.HasValue)
                    break;

                foods.Add(spawnPoint.Value);
            }
        }

        private void GrantExtraLife()
        {
            if (hasExtraLife)
                return;

            hasExtraLife = true;
            ExtraLifeStatusChanged?.Invoke(this, true);
            ShowEffectRequested?.Invoke(this, new EffectEventArgs("EXTRA LIFE!", 2000));
        }

        public void LoseExtraLife()
        {
            hasExtraLife = false;
            ExtraLifeStatusChanged?.Invoke(this, false);
            IsPausedAfterLifeLoss = true;
            ShowEffectRequested?.Invoke(this, new EffectEventArgs("-1 LIFE", 0));

            direction = string.Empty;
            nextDirection = string.Empty;
            GameStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ApplySpeedBoost()
        {
            ShowEffectRequested?.Invoke(this, new EffectEventArgs("SPEED BOOST!", 2000));
            speedMultiplier = 2f;
            speedEffectDuration = 30;
            SpeedChanged?.Invoke(this, CurrentTimerInterval);
        }

        private void ApplyReverseControls()
        {
            ShowEffectRequested?.Invoke(this, new EffectEventArgs("REVERSE CONTROLS!", 2000));
            reverseControls = true;
            reverseControlsDuration = 30;
        }

        private void ApplySlowMotion()
        {
            ShowEffectRequested?.Invoke(this, new EffectEventArgs("SLOW MOTION!", 2000));
            speedMultiplier = 0.5f;
            speedEffectDuration = 30;
            SpeedChanged?.Invoke(this, CurrentTimerInterval);
        }

        private void EndGame()
        {
            gameOver = true;
            GameOverOccurred?.Invoke(this, EventArgs.Empty);
        }

        private string ApplyControlEffects(string intendedDirection)
        {
            string normalizedDirection = NormalizeDirection(intendedDirection);
            if (string.IsNullOrEmpty(normalizedDirection))
                return string.Empty;

            if (!reverseControls)
                return normalizedDirection;

            if (normalizedDirection == "UP") return "DOWN";
            if (normalizedDirection == "DOWN") return "UP";
            if (normalizedDirection == "LEFT") return "RIGHT";
            if (normalizedDirection == "RIGHT") return "LEFT";
            return normalizedDirection;
        }

        private string NormalizeDirection(string directionToNormalize)
        {
            if (string.IsNullOrWhiteSpace(directionToNormalize))
                return string.Empty;

            string normalized = directionToNormalize.Trim().ToUpperInvariant();
            if (normalized == "UP" || normalized == "DOWN" || normalized == "LEFT" || normalized == "RIGHT")
                return normalized;

            return string.Empty;
        }

        private bool CanChangeDirection(string candidateDirection)
        {
            if (string.IsNullOrEmpty(direction))
                return true;

            if (candidateDirection == "UP" && direction == "DOWN") return false;
            if (candidateDirection == "DOWN" && direction == "UP") return false;
            if (candidateDirection == "LEFT" && direction == "RIGHT") return false;
            if (candidateDirection == "RIGHT" && direction == "LEFT") return false;
            return true;
        }

        public class GameUpdateEventArgs : EventArgs
        {
            public GameUpdateEventArgs(int score, int level, string difficulty)
            {
                Score = score;
                Level = level;
                Difficulty = difficulty;
            }

            public int Score { get; }
            public int Level { get; }
            public string Difficulty { get; }
        }

        public class EffectEventArgs : EventArgs
        {
            public EffectEventArgs(string message, int durationMs)
            {
                Message = message;
                DurationMs = durationMs;
            }

            public string Message { get; }
            public int DurationMs { get; }
        }
    }
}
