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

            DatabaseSetup.Initialize(connectionString);
            app.MapEventRoutes(connectionString);

            app.Run();
        }
    }
}
