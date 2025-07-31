using Microsoft.EntityFrameworkCore;
using OT.Assessment.Shared.Models;

namespace OT.Assessment.Shared.Data;

public class CasinoDbContext : DbContext
{
    public CasinoDbContext(DbContextOptions<CasinoDbContext> options) : base(options)
    {
    }

    public DbSet<Player> Players { get; set; }
    public DbSet<Provider> Providers { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<CasinoWager> CasinoWagers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Player configuration
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.AccountId);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
        });

        // Provider configuration
        modelBuilder.Entity<Provider>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
        });

        // Game configuration
        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Theme).HasMaxLength(100);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
            
            entity.HasOne(e => e.Provider)
                  .WithMany(p => p.Games)
                  .HasForeignKey(e => e.ProviderId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            entity.HasIndex(e => new { e.Name, e.ProviderId }).IsUnique();
        });

        // CasinoWager configuration
        modelBuilder.Entity<CasinoWager>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CountryCode).HasMaxLength(2).IsFixedLength();
            entity.Property(e => e.ProcessedDate).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.WagerId).IsUnique();
            
            entity.HasOne(e => e.Player)
                  .WithMany(p => p.CasinoWagers)
                  .HasForeignKey(e => e.AccountId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            entity.HasOne(e => e.Game)
                  .WithMany(g => g.CasinoWagers)
                  .HasForeignKey(e => e.GameId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            // Performance indexes
            entity.HasIndex(e => new { e.AccountId, e.CreatedDateTime })
                  .HasDatabaseName("IX_CasinoWagers_AccountId_CreatedDateTime");
            entity.HasIndex(e => e.CreatedDateTime)
                  .HasDatabaseName("IX_CasinoWagers_CreatedDateTime");
            entity.HasIndex(e => e.Amount)
                  .HasDatabaseName("IX_CasinoWagers_Amount");
        });
    }
}