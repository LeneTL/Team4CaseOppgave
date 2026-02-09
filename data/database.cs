namespace CaseOppgaveTeam4.data
{
    public sealed class database
    {
        public database()
        {
            var dbPath = Path.Combine(AppContext.BaseDirectory, "data", "app.db");
            var connectionString = $"Data Source={dbPath}";
        }
    }
}
