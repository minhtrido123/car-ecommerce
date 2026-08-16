using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure;

public class SorchaDbContextFactory : IDesignTimeDbContextFactory<SorchaDbContext>
{
    public SorchaDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "API");
        if (!Directory.Exists(basePath))
            basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("Database");

        var optionsBuilder = new DbContextOptionsBuilder<SorchaDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new SorchaDbContext(optionsBuilder.Options);
    }
}
