namespace TicaretYonetim.ViewModels;

public class MainViewModel : ViewModelBase
{
    public DashboardViewModel DashboardVM { get; } = new();
    public UrunlerViewModel UrunlerVM { get; } = new();
    public SatislarViewModel SatislarVM { get; }
    public AlislarViewModel AlislarVM { get; } = new();
    public RaporlarViewModel RaporlarVM { get; } = new();

    private object _mevcutGorunum;
    public object MevcutGorunum
    {
        get => _mevcutGorunum;
        set => SetField(ref _mevcutGorunum, value);
    }

    private string _mevcutBaslik = "Panel";
    public string MevcutBaslik
    {
        get => _mevcutBaslik;
        set => SetField(ref _mevcutBaslik, value);
    }

    public RelayCommand NavigateCommand { get; }

    public MainViewModel()
    {
        SatislarVM = new SatislarViewModel(UrunlerVM);
        _mevcutGorunum = DashboardVM;
        NavigateCommand = new RelayCommand(p => Git(p as string ?? string.Empty));
        Yenile();
    }

    public void Yenile()
    {
        DashboardVM.Yukle();
        UrunlerVM.Yukle();
        SatislarVM.Yukle();
        AlislarVM.Yukle();
        RaporlarVM.Yukle();
    }

    private void Git(string hedef)
    {
        switch (hedef)
        {
            case "Dashboard":
                DashboardVM.Yukle();
                MevcutGorunum = DashboardVM;
                MevcutBaslik = "Panel";
                break;
            case "Urunler":
                UrunlerVM.Yukle();
                MevcutGorunum = UrunlerVM;
                MevcutBaslik = "Ürünler / Katalog";
                break;
            case "Satislar":
                SatislarVM.Yukle();
                MevcutGorunum = SatislarVM;
                MevcutBaslik = "Satışlar";
                break;
            case "Alislar":
                AlislarVM.Yukle();
                MevcutGorunum = AlislarVM;
                MevcutBaslik = "Alışlar";
                break;
            case "Raporlar":
                RaporlarVM.Yukle();
                MevcutGorunum = RaporlarVM;
                MevcutBaslik = "Raporlar";
                break;
        }
    }
}
