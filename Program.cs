using Dapper;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using Microsoft.OpenApi;


namespace CaseOppgaveTeam4
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Team 4 Event API",
                    Version = "v1"
                });
            });

            var app = builder.Build();
            app.UseSwagger();
            app.UseSwaggerUI();

            var connectionString = builder.Configuration.GetConnectionString("db") ?? "Data Source=app.db";

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Execute("""
                    CREATE TABLE IF NOT EXISTS events (
                        event_id UUID PRIMARY KEY,
                        occured_utc TIMESTAMP NOT NULL,
                        recorded_utc TIMESTAMP NOT NULL,
                        type TEXT NOT NULL,
                        student_id UUID NULL,
                        course TEXT NULL,
                        year INT NULL,
                        semester INT NULL,
                        payload JSONB NOT NULL
                    );
                """);

                connection.Execute("""
                    CREATE INDEX IF NOT EXISTS idx_events_student ON events(student_id);
                """);
            }

            app.MapPost("/events", async (HttpRequest request) =>
            {
                using var document = await JsonDocument.ParseAsync(request.Body);
                var root = document.RootElement;

                //validering :)
                if (!root.TryGetProperty("eventId", out var eventIdProp))
                    return Results.BadRequest("eventId mangler");
                if (!Guid.TryParse(eventIdProp.GetString(), out _))
                    return Results.BadRequest("eventId er ikke gyldig UUID");
                if (!root.TryGetProperty("type", out var typeProp))
                    return Results.BadRequest("type mangler");

                var type = typeProp.GetString()!;
                string? studentId = null;

                if (root.TryGetProperty("studentId", out var sid)) //ikke SIDS
                    studentId = sid.GetString();
                if (type != "student_registrert" && studentId == null)
                    return Results.BadRequest("studentId mangler");
                if (type == "student_registerert")
                    studentId = Guid.NewGuid().ToString();

                var occuredUtc = root.GetProperty("occuredUtc").GetString();
                var recordedUtc = root.GetProperty("recordedUtc").GetString();

                var course = root.TryGetProperty("course", out var c) ? c.GetString() : null;
                var year = root.TryGetProperty("year", out var y) ? y.GetInt32() : (int?)null;
                var semester = root.TryGetProperty("semester", out var s) ? s.GetInt32() : (int?)null;

                var payload = root.GetRawText();

                const string sql = """
                    INSERT OR IGNORE INTO events ( 
                        event_id,
                        occured_utc,
                        recorded_utc,
                        type,
                        student_id,
                        course,
                        year,
                        semester,
                        payload
                    )
                    VALUES (
                        @EventId,
                        @OccuredUtc,
                        @RecordedUtc,
                        @Type,
                        @StudentId,
                        @Course,
                        @Year,
                        @Semester,
                        @Payload
                    );
                """;

                using var connection = new SqliteConnection(connectionString);
                await connection.ExecuteAsync(sql, new
                {
                    EventId = eventIdProp.GetString(),
                    OccuredUtc = occuredUtc,
                    RecordedUtc = recordedUtc,
                    Type = type,
                    StudentId = studentId,
                    Course = course,
                    Year = year,
                    Semester = semester,
                    Payload = payload
                });

                if (type == "student_registrert")
                    return Results.Ok(new { ok = true, studentId });
                return Results.Ok(new { ok = true });
            });

            app.MapGet("/events/count", async () =>
            {
                using var connection = new SqliteConnection(connectionString);
                var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM events");
                return Results.Ok(new { count });
            });

            //if (app.Environment.IsDevelopment())
            //{
            //    app.MapOpenApi();
            //}

            //app.UseHttpsRedirection();

            //app.UseAuthorization();

            app.Run();
        }
    }
}
