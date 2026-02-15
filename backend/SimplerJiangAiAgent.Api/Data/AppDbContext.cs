using Microsoft.EntityFrameworkCore;
using SimplerJiangAiAgent.Api.Data.Entities;

namespace SimplerJiangAiAgent.Api.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<StockQuoteSnapshot> StockQuoteSnapshots => Set<StockQuoteSnapshot>();
    public DbSet<MarketIndexSnapshot> MarketIndexSnapshots => Set<MarketIndexSnapshot>();
    public DbSet<KLinePointEntity> KLinePoints => Set<KLinePointEntity>();
    public DbSet<MinuteLinePointEntity> MinuteLinePoints => Set<MinuteLinePointEntity>();
    public DbSet<IntradayMessageEntity> IntradayMessages => Set<IntradayMessageEntity>();
    public DbSet<StockQueryHistory> StockQueryHistories => Set<StockQueryHistory>();
    public DbSet<StockAgentAnalysisHistory> StockAgentAnalysisHistories => Set<StockAgentAnalysisHistory>();
    public DbSet<StockChatSession> StockChatSessions => Set<StockChatSession>();
    public DbSet<StockChatMessage> StockChatMessages => Set<StockChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockQuoteSnapshot>()
            .HasIndex(x => new { x.Symbol, x.Timestamp });

        modelBuilder.Entity<MarketIndexSnapshot>()
            .HasIndex(x => new { x.Symbol, x.Timestamp });

        modelBuilder.Entity<KLinePointEntity>()
            .HasIndex(x => new { x.Symbol, x.Interval, x.Date });

        modelBuilder.Entity<MinuteLinePointEntity>()
            .HasIndex(x => new { x.Symbol, x.Date, x.Time });

        modelBuilder.Entity<IntradayMessageEntity>()
            .HasIndex(x => new { x.Symbol, x.PublishedAt });

        modelBuilder.Entity<StockQueryHistory>()
            .HasIndex(x => x.Symbol)
            .IsUnique();

        modelBuilder.Entity<StockAgentAnalysisHistory>()
            .HasIndex(x => new { x.Symbol, x.CreatedAt });

        modelBuilder.Entity<StockChatSession>()
            .HasIndex(x => x.SessionKey)
            .IsUnique();

        modelBuilder.Entity<StockChatSession>()
            .HasIndex(x => new { x.Symbol, x.UpdatedAt });

        modelBuilder.Entity<StockChatMessage>()
            .HasIndex(x => x.SessionId);

        modelBuilder.Entity<StockChatMessage>()
            .HasOne(x => x.Session)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.SessionId);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                {
                    property.SetPrecision(18);
                    property.SetScale(2);
                }
            }
        }
    }
}
