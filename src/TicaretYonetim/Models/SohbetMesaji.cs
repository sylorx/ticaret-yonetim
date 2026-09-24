namespace TicaretYonetim.Models;

public class SohbetMesaji
{
    public int Id { get; set; }

    public string Rol { get; set; } = "user";

    public string Icerik { get; set; } = string.Empty;

    public DateTime Tarih { get; set; } = DateTime.Now;
}
