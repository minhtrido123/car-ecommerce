using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Seed;

public static class Program
{
    public static void Main(string[] args)
    {
        var connection = args.Length > 0
            ? args[0]
            : Environment.GetEnvironmentVariable("CONNECTION_STRING")
              ?? "Host=localhost;Port=5432;Database=car ecommerce;Username=admin;Password=123456";

        var options = new DbContextOptionsBuilder<SorchaDbContext>()
            .UseNpgsql(connection)
            .Options;

        using var db = new SorchaDbContext(options);
        db.Database.Migrate();

        var result = SeedData.Run(db);
        Console.WriteLine($"Seeded: {result.Brands} brands, {result.Models} models, {result.Categories} categories, {result.Cars} cars, {result.Images} images, {result.Parts} parts");
    }
}
