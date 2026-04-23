using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;

namespace Class5
{
    // (OOP: Encapsulation) Defines the data structure for a single leaderboard entry.
    public class PlayerRecord
    {
        public string Name { get; set; }
        public int Score { get; set; }
        public int Level { get; set; }
    }

    // (OOP: Abstraction) The core data management class. Hides all file I/O and data manipulation logic.
    public class LeaderboardManager
    {
        // (OOP: Encapsulation) Internal storage fields are private, protecting the data state.
        private readonly List<PlayerRecord> records = new List<PlayerRecord>();
        private readonly string saveFilePath;
        private HashSet<string> uniqueNamesCache = new HashSet<string>(); // Cache for quick name lookup

        public LeaderboardManager(string path)
        {
            this.saveFilePath = path;
        }

        // (OOP: Abstraction) Provides filter options without exposing internal record structure.
        public List<string> GetAvailableFilters()
        {
            var filters = new List<string> { "Overall" };
            int maxLevel = records.Count > 0 ? records.Max(r => r.Level) : 1;

            for (int i = 1; i <= maxLevel; i++)
                filters.Add("Level " + i);

            return filters;
        }

        // (OOP: Encapsulation) Handles file reading and updates internal data state (records and cache).
        public void LoadScores()
        {
            records.Clear();
            uniqueNamesCache.Clear();

            if (!File.Exists(saveFilePath)) // Uses System.IO
                return;

            foreach (string line in File.ReadAllLines(saveFilePath)) // Uses System.IO
            {
                try
                {
                    string[] parts = line.Split('|');
                    if (parts.Length < 4) continue;

                    string name = parts[1].Trim();
                    string scorePart = parts[2].Trim().Replace("SCORE:", "").Trim();
                    string levelPart = parts[3].Trim().Replace("LEVEL:", "").Trim();

                    if (int.TryParse(scorePart, out int score) && int.TryParse(levelPart, out int level))
                    {
                        records.Add(new PlayerRecord { Name = name, Score = score, Level = level });

                        // Add name to cache for quick lookup
                        uniqueNamesCache.Add(name.ToUpperInvariant());
                    }
                }
                catch
                {
                    // ignore malformed lines
                }
            }
        }

        // (OOP: Abstraction) Provides a simple boolean check without exposing the cache mechanism.
        public bool IsNameTaken(string name)
        {
            // Case-insensitive check
            return uniqueNamesCache.Contains(name.ToUpperInvariant());
        }

        // (OOP: Encapsulation) Handles file writing and updates internal data state.
        public void SaveNewScore(string playerName, int score, int level)
        {
            if (string.IsNullOrEmpty(playerName) || score < 0 || level < 1)
                throw new ArgumentException("Invalid score data provided.");

            if (IsNameTaken(playerName))
                throw new InvalidOperationException("Name is already taken and cannot be saved.");

            try
            {
                string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {playerName} | SCORE: {score} | LEVEL: {level}";
                File.AppendAllText(saveFilePath, line + Environment.NewLine); // Uses System.IO

                // Update cache immediately after successful save
                uniqueNamesCache.Add(playerName.ToUpperInvariant());
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to save score to {Path.GetFileName(saveFilePath)}", ex);
            }
        }

        // (OOP: Abstraction) Takes a filter and returns sorted, processed data.
        // (OOP: Polymorphism) Uses LINQ (Where, OrderByDescending) to process data based on the filter string.
        public List<PlayerRecord> GetLeaderboard(string filter)
        {
            IEnumerable<PlayerRecord> filtered;

            if (string.IsNullOrEmpty(filter) || filter == "Overall")
            {
                filtered = records.OrderByDescending(r => r.Score);
            }
            else
            {
                if (!int.TryParse(filter.Replace("Level ", ""), out int lvl))
                    lvl = 1;

                filtered = records.Where(r => r.Level == lvl).OrderByDescending(r => r.Score);
            }

            return filtered.Take(5).ToList();
        }
    }
}