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
                    event_id TEXT NOT NULL,
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

            app.MapPost("/events", async (HttpContext context) =>
            {
                using var reader = new StreamReader(context.Request.Body);
                var json = await reader.ReadToEndAsync();
                var root = JsonDocument.Parse(json).RootElement;
                Console.WriteLine($"Motatt data: {json}");


                var type = root.GetProperty("type").GetString();
                string? currstudentId = null;

                //var currEventsId = root.GetProperty("eventId").GetString();

                using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();

                ////---------------------GetEventId
                //var command = connection.CreateCommand();
                //command.CommandText = "SELECT eventId FROM events WHERE currEventsId = $id";
                //command.CommandText = "SELECT eventId FROM students WHERE currEventsId = $id";
                //command.Parameters.AddWithValue("$id", currEventsId);
                //var resultEventId = command.ExecuteScalar();


                //if (currEventsId == resultEventId)
                //{

                //}



                if (type == "student_registrert")
                {
                    var newStudentId = Guid.NewGuid().ToString();

                    await connection.ExecuteAsync("""
                                                      INSERT OR IGNORE INTO students (
                                                      student_id,
                                                      event_id, 
                                                      occurred_utc, 
                                                      recorded_utc,
                                                      type,
                                                      name, 
                                                      birthdate, 
                                                      city)
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
                                                  INSERT OR IGNORE INTO events (
                                                      event_id,
                                                      occurred_utc,
                                                      recorded_utc,
                                                      type,
                                                      course,
                                                      year,
                                                      semester,
                                                      student_id
                                                  )
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

            //if (app.Environment.IsDevelopment())
            //{
            //    app.MapOpenApi();
            //}

            //app.UseHttpsRedirection();

            //app.UseAuthorization();

            app.Run();
        }

        //public string GetEventId(string currEventsId, string connectionString)
        //{

        //    using var connection = new SqliteConnection(connectionString);
        //    connection.Open();
        //    var command = connection.CreateCommand();

        //    command.CommandText = "SELECT eventId FROM events WHERE currEventsId = $id";
        //    command.Parameters.AddWithValue("$id", currEventsId);

        //    var result = command.ExecuteScalar();
        //    return Convert.ToString(result);
        //}
    }
}
