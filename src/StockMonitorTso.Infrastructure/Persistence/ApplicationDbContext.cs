using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockMonitorTso.Domain.Entities;

namespace StockMonitorTso.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Agen> Agen => Set<Agen>();

    public DbSet<StokEntitas> StokEntitas => Set<StokEntitas>();

    public DbSet<RencanaKedatangan> RencanaKedatangan => Set<RencanaKedatangan>();

    public DbSet<StockTransactionRecord> StockTransactions => Set<StockTransactionRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(64).IsRequired();
            entity.Property(e => e.EntityType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.EntityId).HasMaxLength(64);
            entity.Property(e => e.ActorEmail).HasMaxLength(256);
            entity.Property(e => e.ActorRole).HasMaxLength(64);
            entity.Property(e => e.Detail).HasMaxLength(1024);
        });

        builder.Entity<Agen>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nama).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Wilayah).HasConversion<string>().HasMaxLength(64);
            entity.HasIndex(e => new { e.Wilayah, e.Nama }).IsUnique();
        });

        builder.Entity<StokEntitas>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Wilayah).HasConversion<string>().HasMaxLength(64);
            entity.Property(e => e.Produk).HasConversion<string>().HasMaxLength(32);
            entity.Property(e => e.Tier).HasConversion<string>().HasMaxLength(16);
            entity.HasOne(e => e.Agen)
                .WithMany(a => a.StokEntitas)
                .HasForeignKey(e => e.AgenId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.Wilayah, e.Produk, e.Tier })
                .IsUnique()
                .HasFilter("[AgenId] IS NULL");
            entity.HasIndex(e => new { e.AgenId, e.Produk, e.Tier })
                .IsUnique()
                .HasFilter("[AgenId] IS NOT NULL");
        });

        builder.Entity<RencanaKedatangan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.StokEntitas)
                .WithMany(e => e.RencanaKedatangan)
                .HasForeignKey(e => e.StokEntitasId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.StokEntitasId, e.Urutan }).IsUnique();
        });

        builder.Entity<StockTransactionRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.StokEntitas)
                .WithMany()
                .HasForeignKey(e => e.StokEntitasId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(16);
        });
    }
}
