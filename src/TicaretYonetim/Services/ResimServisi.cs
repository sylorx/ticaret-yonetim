using System.IO;
using System.Linq;
using TicaretYonetim.Data;

namespace TicaretYonetim.Services;

public static class ResimServisi
{
    private static readonly string[] GecerliUzantilar = { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".gif" };

    public static string? Kopyala(string kaynakDosyaYolu)
    {
        if (string.IsNullOrWhiteSpace(kaynakDosyaYolu) || !File.Exists(kaynakDosyaYolu))
            return null;

        var uzanti = Path.GetExtension(kaynakDosyaYolu).ToLowerInvariant();
        if (!GecerliUzantilar.Contains(uzanti))
            throw new InvalidOperationException("Desteklenmeyen resim formatı. Lütfen jpg, png, bmp, webp veya gif seçin.");

        Directory.CreateDirectory(AppDbContext.ResimKlasoru);

        var yeniAd = $"{Guid.NewGuid():N}{uzanti}";
        var hedefYol = Path.Combine(AppDbContext.ResimKlasoru, yeniAd);
        File.Copy(kaynakDosyaYolu, hedefYol, overwrite: true);

        return hedefYol;
    }

    public static void Sil(string? resimYolu)
    {
        if (string.IsNullOrWhiteSpace(resimYolu))
            return;

        try
        {
            if (File.Exists(resimYolu))
                File.Delete(resimYolu);
        }
        catch
        {
            // Silme başarısız olursa sessizce geç.
        }
    }
}
