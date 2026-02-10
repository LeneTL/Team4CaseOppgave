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
                    CREATE TABLE IF NOT EXISTS students (
                    student_id UUID PRIMARY KEY,
                    name TEXT NOT NULL,
                    birthdate TEXT NOT NULL,
                    city TEXT NOT NULL, 
                    last_event_id UUID NOT NULL,
                    last_updated_utc TIMESTAMP NOT NULL
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
                Console.WriteLine($"Motatt data: {json}");

                // 
                
                //using (var connection = new SqliteConnection(connectionString))
                //{
                //    await connection.OpenAsync();
                //    var insertCmd = new SqliteCommand("...");
                //    insertCmd.Parameters.AddWithValue("@...", json);
                //}
                //return Results.Ok(new { ok = true, message = "Data motatt og lagret" });
                return Results.Ok(new { ok = true, message = "Data motatt!" });
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
