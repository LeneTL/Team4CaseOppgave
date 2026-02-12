using Dapper;
using Microsoft.Data.Sqlite;

namespace CaseOppgaveTeam4
{
    public static class DatabaseSetup
    {
        public static void Initialize(string connectionString)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Execute("""
                    CREATE TABLE IF NOT EXISTS students (
                        student_id TEXT PRIMARY KEY,
                        event_id TEXT UNIQUE,
                        occurred_utc TIMESTAMP NOT NULL,
                        recorded_utc TIMESTAMP NOT NULL,
                        type TEXT NOT NULL,
                        name TEXT NOT NULL,
                        birthdate TEXT NOT NULL,
                        city TEXT NOT NULL 
                    );
                """);

                connection.Execute("""
                    CREATE TABLE IF NOT EXISTS events (
                        event_id TEXT PRIMARY KEY,
                        occurred_utc TIMESTAMP NOT NULL,
                        recorded_utc TIMESTAMP NOT NULL,
                        type TEXT NOT NULL,
                        course TEXT NULL,
                        year INTEGER  NULL,
                        semester INTEGER  NULL,
                        student_id TEXT,
                        FOREIGN KEY(student_id) REFERENCES students(student_id)
                    );
                """);

                connection.Execute("""
                    CREATE INDEX IF NOT EXISTS idx_events_student ON events(student_id);
                """);
            }
        }
    }
}