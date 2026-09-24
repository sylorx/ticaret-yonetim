using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace TicaretYonetim;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var tanimlayici = DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty, typeof(ContentControl));
        tanimlayici?.AddValueChanged(Icerik, Icerik_ContentChanged);
    }

    private void Icerik_ContentChanged(object? sender, EventArgs e)
    {
        var animasyon = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(350),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Icerik.BeginAnimation(OpacityProperty, animasyon);
    }
}
