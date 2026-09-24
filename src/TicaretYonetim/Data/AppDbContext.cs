using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
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
    public DbSet<Filament> Filamentler => Set<Filament>();

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
        modelBuilder.Entity<Urun>().Property(u => u.FilamentGram).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Filament>().Property(f => f.ToplamGram).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Filament>().Property(f => f.KalanGram).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Filament>().Property(f => f.BirimFiyat).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Urun>()
            .HasOne(u => u.Filament)
            .WithMany(f => f.Urunler)
            .HasForeignKey(u => u.FilamentId)
            .OnDelete(DeleteBehavior.SetNull);

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

    /// <summary>
    /// EnsureCreated ile olusturulmus eski veritabanlarina, yeni eklenen kolonlari
    /// veri kaybi olmadan ekler.
    /// </summary>
    public void SemaGuncelle()
    {
        Database.EnsureCreated();

        using var baglanti = new SqliteConnection($"Data Source={VeriTabaniYolu}");
        baglanti.Open();

        void KolonEkle(string tablo, string kolon, string tip)
        {
            using var kontrol = baglanti.CreateCommand();
            kontrol.CommandText = $"PRAGMA table_info({tablo});";
            using var okuyucu = kontrol.ExecuteReader();
            var varMi = false;
            while (okuyucu.Read())
            {
                if (string.Equals(okuyucu.GetString(1), kolon, StringComparison.OrdinalIgnoreCase))
                {
                    varMi = true;
                    break;
                }
            }
            okuyucu.Close();

            if (!varMi)
            {
                using var ekle = baglanti.CreateCommand();
                ekle.CommandText = $"ALTER TABLE {tablo} ADD COLUMN {kolon} {tip};";
                ekle.ExecuteNonQuery();
            }
        }

        KolonEkle("Satislar", "SiparisNo", "TEXT");
        KolonEkle("Urunler", "FilamentGram", "REAL");
        KolonEkle("Urunler", "FilamentId", "INTEGER");

        using (var tablo = baglanti.CreateCommand())
        {
            tablo.CommandText = @"
                CREATE TABLE IF NOT EXISTS Filamentler (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Ad TEXT NOT NULL,
                    Renk TEXT,
                    Marka TEXT,
                    ToplamGram REAL NOT NULL DEFAULT 0,
                    KalanGram REAL NOT NULL DEFAULT 0,
                    BirimFiyat REAL NOT NULL DEFAULT 0,
                    Tedarikci TEXT,
                    Tarih TEXT NOT NULL,
                    Aktif INTEGER NOT NULL DEFAULT 1
                );";
            tablo.ExecuteNonQuery();
        }
    }
}
