using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;
using Xunit;

namespace SimplerJiangAiAgent.Api.Tests;

public sealed class AppDbContextDecimalPrecisionTests
{
    [Fact]
    public void OnModelCreating_AllDecimalPropertiesHavePrecisionAndScale()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "decimal_precision_check")
            .Options;

        using var context = new AppDbContext(options);
        var decimalProperties = context.Model
            .GetEntityTypes()
            .SelectMany(entityType => entityType.GetProperties())
            .Where(property => property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
            .ToArray();

        Assert.NotEmpty(decimalProperties);

        foreach (var property in decimalProperties)
        {
            Assert.Equal(18, property.GetPrecision());
            Assert.Equal(2, property.GetScale());
        }
    }
}
