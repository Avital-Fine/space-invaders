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
                    command.CommandText = "CREATE TABLE IF NOT EXISTS Scores (Name TEXT, Score INTEGER, Level INTEGER, Date TEXT)";
                    command.ExecuteNonQuery();
                }
            }
        }

        public void SaveScore(string playerName, int score, int level) {
            using (var connection = new SqliteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO Scores (Name, Score, Level, Date) VALUES (@Name, @Score, @Level, @Date)";
                    command.Parameters.AddWithValue("@Name", playerName);
                    command.Parameters.AddWithValue("@Score", score);
                    command.Parameters.AddWithValue("@Level", level);
                    command.Parameters.AddWithValue("@Date", DateTime.Now.ToString());
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<ScoreEntry> GetTopScores(int count = 5) {
            List<ScoreEntry> scores = new List<ScoreEntry>();
            using (var connection = new SqliteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Scores ORDER BY Score DESC LIMIT @Count";
                    command.Parameters.AddWithValue("@Count", count);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            scores.Add(new ScoreEntry(reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetString(3)));
                        }
                    }
                }
            }
            return scores;
        }
    }
}
