using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System;

namespace Infrastructure.Managers
{
    public class ScoresDatabase
    {
        private readonly string m_ConnectionString;

        // A constructor that creates/opens a SQLite database file and ensures the table exists
        public ScoresDatabase(string i_DbPath = "scores.db")
        {
            m_ConnectionString = $"Data Source={i_DbPath}";
            using (var connection = new SqliteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "CREATE TABLE IF NOT EXISTS Scores (Name TEXT, Score INTEGER, Level INTEGER, Date TEXT, GameName TEXT)";
                    command.ExecuteNonQuery();

                    try
                    {
                        command.CommandText = "ALTER TABLE Scores ADD COLUMN GameName TEXT";
                        command.ExecuteNonQuery();
                    }
                    catch
                    {
                        // Column already exists
                    }
                }
            }
        }

        public void SaveScore(string playerName, int score, int level, string gameName = "Space Invaders")
        {
            using (var connection = new SqliteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO Scores (Name, Score, Level, Date, GameName) VALUES (@Name, @Score, @Level, @Date, @GameName)";
                    command.Parameters.AddWithValue("@Name", playerName ?? "Player");
                    command.Parameters.AddWithValue("@Score", score);
                    command.Parameters.AddWithValue("@Level", level);
                    command.Parameters.AddWithValue("@Date", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    command.Parameters.AddWithValue("@GameName", gameName ?? "Space Invaders");
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<ScoreEntry> GetTopScores(int count = 5, string gameName = null)
        {
            List<ScoreEntry> scores = new List<ScoreEntry>();
            using (var connection = new SqliteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    if (string.IsNullOrEmpty(gameName))
                    {
                        command.CommandText = "SELECT Name, Score, Level, Date, GameName FROM Scores ORDER BY Score DESC LIMIT @Count";
                    }
                    else
                    {
                        command.CommandText = "SELECT Name, Score, Level, Date, GameName FROM Scores WHERE GameName = @GameName OR (GameName IS NULL AND @GameName = 'Space Invaders') ORDER BY Score DESC LIMIT @Count";
                        command.Parameters.AddWithValue("@GameName", gameName);
                    }
                    command.Parameters.AddWithValue("@Count", count);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string name = reader.IsDBNull(0) ? "Player" : reader.GetString(0);
                            int score = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                            int level = reader.IsDBNull(2) ? 1 : reader.GetInt32(2);
                            string date = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            string game = reader.FieldCount > 4 && !reader.IsDBNull(4) ? reader.GetString(4) : "Space Invaders";
                            scores.Add(new ScoreEntry(name, score, level, date, game));
                        }
                    }
                }
            }
            return scores;
        }
    }
}
