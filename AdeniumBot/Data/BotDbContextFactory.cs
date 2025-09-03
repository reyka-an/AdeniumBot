using System.IO;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Adenium.Data
{
    public class BotDbContextFactory : IDesignTimeDbContextFactory<BotDbContext>
    {
        public BotDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
            
            var conn = config["ConnectionStrings:Default"];
            
            if (string.IsNullOrWhiteSpace(conn))
            {
                conn = "Host=localhost;Port=5432;Database=discordbot;Username=botuser;Password=adeniumbotreyka;";
            }

            return new BotDbContext(conn);
        }
    }
}