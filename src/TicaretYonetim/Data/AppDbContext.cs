using System.IO;
using Microsoft.EntityFrameworkCore;
using TicaretYonetim.Models;

namespace TicaretYonetim.Data;

public class AppDbContext : DbContext
{
    public static string VeriKlasoru { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TicaretYonetim");

    public static string VeriTabaniYolu { get; } = Path.Combine(VeriKlasoru, "ticaret.db");

    public static string ResimKlasoru { get; } = Path.Combine(VeriKlasoru, "Resimler");

    public DbSet<Urun> Urunler => Set<Urun>();
    public DbSet<Alis> Alislar => Set<Alis>();
    public DbSet<Satis> Satislar => Set<Satis>();

    public AppDbContext()
    {
        Directory.CreateDirectory(VeriKlasoru);
        Directory.CreateDirectory(ResimKlasoru);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={VeriTabaniYolu}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Urun>().Property(u => u.BirimMaliyet).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Urun>().Property(u => u.SatisFiyati).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Alis>()
            .HasOne(a => a.Urun)
            .WithMany(u => u.Alislar)
            .HasForeignKey(a => a.UrunId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Satis>()
            .HasOne(s => s.Urun)
            .WithMany(u => u.Satislar)
            .HasForeignKey(s => s.UrunId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
