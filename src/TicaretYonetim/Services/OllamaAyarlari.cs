using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace TicaretYonetim.Services;

public class OllamaAyarlari
{
    public string GpuModu { get; set; } = "Otomatik (GPU+CPU)";
    public int GpuOverheadMb { get; set; } = 0;
    public int ContextLength { get; set; } = 4096;
    public string KvCacheTipi { get; set; } = "f16 (yüksek kalite)";
    public int ParalelIstek { get; set; } = 1;
    public int MaxYukluModel { get; set; } = 1;
    public int LoadTimeoutSaniye { get; set; } = 600;
    public string KeepAlive { get; set; } = "30m";

    public static readonly string[] GpuModuSecenekleri = { "Otomatik (GPU+CPU)", "Sadece CPU", "Sadece GPU" };
    public static readonly int[] ContextSecenekleri = { 2048, 4096, 8192, 16384 };
    public static readonly string[] KvCacheSecenekleri = { "f16 (yüksek kalite)", "q8_0 (dengeli)", "q4_0 (az RAM)" };
    public static readonly int[] ParalelSecenekleri = { 1, 2, 4 };
    public static readonly int[] MaxModelSecenekleri = { 1, 2, 3 };
    public static readonly string[] KeepAliveSecenekleri = { "5m", "30m", "1h", "5h" };

    private static string DosyaYolu => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TicaretYonetim", "ollama_ayarlari.json");

    public static OllamaAyarlari Yukle()
    {
        try
        {
            if (File.Exists(DosyaYolu))
            {
                var json = File.ReadAllText(DosyaYolu);
                return JsonSerializer.Deserialize<OllamaAyarlari>(json) ?? new OllamaAyarlari();
            }
        }
        catch { }
        return new OllamaAyarlari();
    }

    public void Kaydet()
    {
        try
        {
            var dizin = Path.GetDirectoryName(DosyaYolu);
            if (!string.IsNullOrEmpty(dizin)) Directory.CreateDirectory(dizin);
            File.WriteAllText(DosyaYolu, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    public void Uygula()
    {
        if (GpuModu == "Sadece CPU")
            Environment.SetEnvironmentVariable("OLLAMA_LLM_LIBRARY", "cpu", EnvironmentVariableTarget.User);
        else
            Environment.SetEnvironmentVariable("OLLAMA_LLM_LIBRARY", null, EnvironmentVariableTarget.User);

        if (GpuOverheadMb > 0)
            Environment.SetEnvironmentVariable("OLLAMA_GPU_OVERHEAD", GpuOverheadMb.ToString(), EnvironmentVariableTarget.User);
        else
            Environment.SetEnvironmentVariable("OLLAMA_GPU_OVERHEAD", null, EnvironmentVariableTarget.User);

        Environment.SetEnvironmentVariable("OLLAMA_CONTEXT_LENGTH", ContextLength.ToString(), EnvironmentVariableTarget.User);

        var kv = KvCacheTipi.Split(' ')[0];
        Environment.SetEnvironmentVariable("OLLAMA_KV_CACHE_TYPE", kv, EnvironmentVariableTarget.User);

        Environment.SetEnvironmentVariable("OLLAMA_NUM_PARALLEL", ParalelIstek.ToString(), EnvironmentVariableTarget.User);
        Environment.SetEnvironmentVariable("OLLAMA_MAX_LOADED_MODELS", MaxYukluModel.ToString(), EnvironmentVariableTarget.User);
        Environment.SetEnvironmentVariable("OLLAMA_LOAD_TIMEOUT", LoadTimeoutSaniye.ToString(), EnvironmentVariableTarget.User);
        Environment.SetEnvironmentVariable("OLLAMA_KEEP_ALIVE", KeepAlive, EnvironmentVariableTarget.User);
    }

    public static void OllamaYenidenBaslat()
    {
        foreach (var ad in new[] { "ollama", "ollama app", "ollama_llama_server" })
        {
            foreach (var p in Process.GetProcessesByName(ad))
            {
                try { p.Kill(); } catch { }
            }
        }
        Thread.Sleep(2500);

        var exe = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "Ollama", "ollama.exe");
        if (File.Exists(exe))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = "serve",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
    }
}
