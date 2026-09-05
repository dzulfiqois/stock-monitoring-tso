using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockMonitorTso.Domain.Entities;

namespace StockMonitorTso.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Agen> Agen => Set<Agen>();

    public DbSet<Outlet> Outlet => Set<Outlet>();

    public DbSet<MitraTso> MitraTso => Set<MitraTso>();

    public DbSet<MitraTarif> MitraTarifs => Set<MitraTarif>();

    public DbSet<TransportOrder> TransportOrders => Set<TransportOrder>();

    public DbSet<TransportOrderDetail> TransportOrderDetails => Set<TransportOrderDetail>();

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

        builder.Entity<Outlet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nama).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Wilayah).HasConversion<string>().HasMaxLength(64);
            entity.HasOne(e => e.Agen)
                .WithMany()
                .HasForeignKey(e => e.AgenId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.AgenId, e.Nama }).IsUnique();
        });

        builder.Entity<MitraTso>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(32);
            entity.Property(e => e.Nama).HasMaxLength(200).IsRequired();
            entity.Property(e => e.JenisKendaraan).HasMaxLength(64);
            entity.Property(e => e.SatuanKapasitas).HasMaxLength(32);
            entity.Property(e => e.SatuanTarif).HasMaxLength(32);
        });

        builder.Entity<MitraTarif>(entity =>
        {
            entity.ToTable("MitraTarifs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MitraId).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Produk).HasConversion<string>().HasMaxLength(32);
            entity.Property(e => e.SatuanTarif).HasMaxLength(32);
            entity.HasOne(e => e.Mitra).WithMany(m => m.Tarifs).HasForeignKey(e => e.MitraId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.MitraId, e.Produk }).IsUnique();
        });

        builder.Entity<TransportOrderDetail>(entity =>
        {
            entity.ToTable("TransportOrderDetails");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Produk).HasConversion<string>().HasMaxLength(32);
            entity.Property(e => e.SatuanTarifSnapshot).HasMaxLength(32);
            entity.HasOne(e => e.Order).WithMany(o => o.Details).HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TransportOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNo).HasMaxLength(32).IsRequired();
            entity.HasIndex(e => e.OrderNo).IsUnique();
            entity.Property(e => e.MitraId).HasMaxLength(32).IsRequired();
            entity.Property(e => e.MitraNamaSnapshot).HasMaxLength(200);
            entity.Property(e => e.SatuanTarifSnapshot).HasMaxLength(32);
            entity.Property(e => e.WilayahTujuan).HasConversion<string>().HasMaxLength(64);
            entity.Property(e => e.Produk).HasConversion<string>().HasMaxLength(32);
            entity.Property(e => e.Satuan).HasMaxLength(32);
            entity.Property(e => e.RuteAsal).HasMaxLength(64);
            entity.Property(e => e.RuteTujuan).HasMaxLength(64);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(e => e.RowVersion).IsConcurrencyToken();
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone");
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp with time zone");
            entity.Property(e => e.InvoiceGeneratedAt).HasColumnType("timestamp with time zone");
            entity.HasIndex(e => new { e.MitraId, e.WilayahTujuan, e.Produk, e.Kuantitas, e.TanggalKeberangkatan });
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
            entity.HasOne(e => e.Outlet)
                .WithMany(o => o.StokEntitas)
                .HasForeignKey(e => e.OutletId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.Wilayah, e.Produk, e.Tier })
                .IsUnique()
                .HasFilter("\"AgenId\" IS NULL AND \"OutletId\" IS NULL");
            entity.HasIndex(e => new { e.AgenId, e.Produk, e.Tier })
                .IsUnique()
                .HasFilter("\"AgenId\" IS NOT NULL");
            entity.HasIndex(e => new { e.OutletId, e.Produk, e.Tier })
                .IsUnique()
                .HasFilter("\"OutletId\" IS NOT NULL");
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
