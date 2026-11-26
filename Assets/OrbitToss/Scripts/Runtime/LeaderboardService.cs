using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OrbitToss
{
    /// <summary>One saved round in "My Best Scores".</summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public int score;
        /// <summary>Best (closest) ball of the round in metres; negative if no ball stayed in play.</summary>
        public float bestDistance = -1f;
        /// <summary>Date saved, "yyyy-MM-dd".</summary>
        public string date;
        /// <summary>Time saved, "HH:mm".</summary>
        public string time;
    }

    /// <summary>
    /// "My Best Scores": the player's 10 best practice rounds, saved as JSON on the device
    /// (Application.persistentDataPath), so it works offline with no accounts.
    /// Ranking: higher score first, then closer best ball.
    /// </summary>
    public class LeaderboardService
    {
        public const int MaxEntries = 10;
        private const string FileName = "orbittoss_leaderboard.json";

        [Serializable]
        private class SaveData { public List<LeaderboardEntry> entries = new List<LeaderboardEntry>(); }

        private SaveData data = new SaveData();
        private readonly string path;

        public IReadOnlyList<LeaderboardEntry> Entries => data.entries;

        public LeaderboardService(string folder = null)
        {
            path = Path.Combine(folder ?? Application.persistentDataPath, FileName);
            Load();
        }

        /// <summary>True if this score would appear in the best 10.</summary>
        public bool Qualifies(int score)
        {
            return data.entries.Count < MaxEntries || score > data.entries[data.entries.Count - 1].score;
        }

        /// <summary>Adds a round, keeps the best 10, saves, and returns its rank index (0 = best), or -1 if it did not make the list.</summary>
        public int Add(int score, float bestDistance)
        {
            var entry = new LeaderboardEntry
            {
                score = score,
                bestDistance = bestDistance,
                date = DateTime.Now.ToString("yyyy-MM-dd"),
                time = DateTime.Now.ToString("HH:mm")
            };
            data.entries.Add(entry);
            data.entries.Sort(Compare);
            if (data.entries.Count > MaxEntries) data.entries.RemoveRange(MaxEntries, data.entries.Count - MaxEntries);
            Save();
            return data.entries.IndexOf(entry);
        }

        public void Clear()
        {
            data.entries.Clear();
            Save();
        }

        private static int Compare(LeaderboardEntry a, LeaderboardEntry b)
        {
            int byScore = b.score.CompareTo(a.score);
            if (byScore != 0) return byScore;
            float da = a.bestDistance < 0f ? float.MaxValue : a.bestDistance;
            float db = b.bestDistance < 0f ? float.MaxValue : b.bestDistance;
            return da.CompareTo(db); // List.Sort is not stable, but ties on both are effectively equal rows
        }

        private void Load()
        {
            try
            {
                if (File.Exists(path)) data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path)) ?? new SaveData();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[OrbitToss] Could not read leaderboard, starting empty: " + e.Message);
                data = new SaveData();
            }
            if (data.entries == null) data.entries = new List<LeaderboardEntry>();
        }

        private void Save()
        {
            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[OrbitToss] Could not save leaderboard: " + e.Message);
            }
        }
    }
}
