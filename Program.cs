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
                    CREATE TABLE IF NOT EXISTS students (
                    student_id UUID PRIMARY KEY,
                    event_id UUID NOT NULL UNIQUE,
                    occured_utc TIMESTAMP NOT NULL,
                    recorded_utc TIMESTAMP NOT NULL,
                    name TEXT NOT NULL,
                    birthdate TEXT NOT NULL,
                    city TEXT NOT NULL 
                );
                """);

                connection.Execute("""
                    CREATE TABLE IF NOT EXISTS events (
                        event_id UUID PRIMARY KEY,
                        occured_utc TIMESTAMP NOT NULL,
                        recorded_utc TIMESTAMP NOT NULL,
                        type TEXT NOT NULL,
                        course TEXT NULL,
                        year INT NULL,
                        semester INT NULL,
                        student_id TEXT,
                        FOREIGN KEY(student_id) REFERENCES students(student_id)
                        
                    );
                """);

                

                connection.Execute("""
                    CREATE INDEX IF NOT EXISTS idx_events_student ON events(student_id);
                """);
            }

            app.MapPost("/events", async (HttpContext context) =>
            {
                using var reader = new StreamReader(context.Request.Body);
                var json = await reader.ReadToEndAsync();
                var root = JsonDocument.Parse(json).RootElement;
                Console.WriteLine($"Motatt data: {json}");


                var type = root.GetProperty("type").GetString();
                string? currstudentId = null;
                

                using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();
                if (type == "student_registrert")
                {
                    var newStudentId = Guid.NewGuid().ToString();

                    await connection.ExecuteAsync("""
                                                      INSERT INTO students (student_id,
                                                      event_id, 
                                                      occured_utc, 
                                                      recorded_utc, 
                                                      name, 
                                                      birthdate, 
                                                      city)
                                                      VALUES (@studentId, @eventId, @occured, @recorded, @name, @birth, @city)
                                                  """,
                        new
                        {
                            studentId = newStudentId,
                            eventId = root.GetProperty("eventId").GetString(),
                            occured = root.GetProperty("occurredUtc").GetString(),
                            recorded = root.GetProperty("recordedUtc").GetString(),
                            name = root.GetProperty("name").GetString(),
                            birth = root.GetProperty("birthdate").GetString(),
                            city = root.GetProperty("city").GetString()
                    });
                    return Results.Ok(new { ok = true, currstudentId = newStudentId });

                }

                if (type != "student_registrert")
                {
                    await connection.ExecuteAsync("""
                                                  INSERT INTO events(
                                                      event_id,
                                                      occured_utc,
                                                      recorded_utc,
                                                      type,
                                                      course,
                                                      year,
                                                      semester,
                                                      student_id
                                                  )
                                                  VALUES (@eventid, @occured, @recorded, @type, @course, @year, @semester, @studentid)
                                                  """,
                        new
                        {
                            eventid = root.GetProperty("eventId").GetString(),
                            occured = root.GetProperty("occured_utc").GetString(),
                            recorded = root.GetProperty("recorded_utc").GetString(),
                            type = root.GetProperty("type").GetString(),
                            course = root.GetProperty("course").GetString(),
                            year = root.GetProperty("year").GetString(),
                            semester = root.GetProperty("semester").GetString(),
                            studentid = currstudentId

                        });
                            
                            
                    return Results.Ok(new {ok = true});
                }
                return null;

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
