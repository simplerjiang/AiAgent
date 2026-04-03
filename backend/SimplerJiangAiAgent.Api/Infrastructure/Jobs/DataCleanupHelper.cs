using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data;

namespace SimplerJiangAiAgent.Api.Infrastructure.Jobs;

/// <summary>
/// Idempotent data cleanup executed on every startup.
/// Fixes legacy dirty data: "示例名称" placeholders and extra spaces in short stock names.
/// </summary>
public static class DataCleanupHelper
{
    public static async Task CleanStockNamesAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        // Clean StockQueryHistories – remove "示例名称" placeholder names
        var badNames = await dbContext.StockQueryHistories
            .Where(h => h.Name.Contains("示例名称"))
            .ToListAsync(cancellationToken);
        foreach (var h in badNames)
            h.Name = string.Empty;

        // Clean StockQueryHistories – remove extra spaces in short Chinese names (≤10 chars)
        var spacedNames = await dbContext.StockQueryHistories
            .Where(h => h.Name.Contains(" ") && h.Name.Length <= 10)
            .ToListAsync(cancellationToken);
        foreach (var h in spacedNames)
            h.Name = h.Name.Replace(" ", "");

        if (badNames.Count > 0 || spacedNames.Count > 0)
            await dbContext.SaveChangesAsync(cancellationToken);
    }
}
