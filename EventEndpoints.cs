using Dapper;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace CaseOppgaveTeam4
{
    public static class EventEndpoints
    {
        public static void MapEventRoutes(this IEndpointRouteBuilder app, string connectionString)
        {
            app.MapPost("/events", async (HttpContext context) =>
            {
                using var reader = new StreamReader(context.Request.Body);
                var json = await reader.ReadToEndAsync();
                var root = JsonDocument.Parse(json).RootElement;
                Console.WriteLine($"Motatt data: {json}");

                var type = root.GetProperty("type").GetString();

                using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();

                if (type == "student_registrert")
                {
                    var newStudentId = Guid.NewGuid().ToString();

                    await connection.ExecuteAsync("""
                        INSERT OR IGNORE INTO students (student_id, event_id, occurred_utc,  recorded_utc, type, name, birthdate, city)
                        VALUES (@studentId, @eventId, @occurred, @recorded, @type, @name, @birth, @city)
                    """,
                    new
                    {
                        studentId = newStudentId,
                        eventId = root.GetProperty("eventId").GetString(),
                        occurred = root.GetProperty("occurredUtc").GetString(),
                        recorded = root.GetProperty("recordedUtc").GetString(),
                        type = root.GetProperty("type").GetString(),
                        name = root.GetProperty("name").GetString(),
                        birth = root.GetProperty("birthdate").GetString(),
                        city = root.GetProperty("city").GetString()
                    });
                    return Results.Ok(new { ok = true, studentId = newStudentId });
                }

                if (type != "student_registrert")
                {
                    await connection.ExecuteAsync("""
                        INSERT OR IGNORE INTO events (event_id, occurred_utc, recorded_utc, type, course, year, semester, student_id)
                        VALUES (@eventId, @occurred, @recorded, @type, @course, @year, @semester, @studentid)
                    """,
                    new
                    {
                        eventId = root.GetProperty("eventId").GetString(),
                        occurred = root.GetProperty("occurredUtc").GetString(),
                        recorded = root.GetProperty("recordedUtc").GetString(),
                        type = root.GetProperty("type").GetString(),
                        course = root.GetProperty("course").GetString(),
                        year = root.GetProperty("year").GetInt32(),
                        semester = root.GetProperty("semester").GetInt32(),
                        studentid = root.GetProperty("studentId").GetString(),
                    });

                    return Results.Ok(new { ok = true });
                }

                return null;
            });

            app.MapGet("/events/count", async () =>
            {
                using var connection = new SqliteConnection(connectionString);
                var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(student_id) FROM events");
                return Results.Ok(new { count });
            });
        }
    }
}