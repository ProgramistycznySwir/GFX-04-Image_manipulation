using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using static GFX_04_ImageManipulation.Prelude;
namespace GFX_04_ImageManipulation;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    MainWindowVM _model;

    public MainWindow()
    {
        InitializeComponent();

        base.DataContext = _model = new();

        EffectList.ItemsSource = _model.Effects;
        EffectList.UpdateLayout();
    }

    private void LoadImage_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new(){
                CheckFileExists = true,
                Title = "Load Image",
            };
        if (dialog.ShowDialog() == true)
        {
            _model.Original = new Bitmap(dialog.FileName);
            image.Source = BitmapToImageSource(_model.Original);
        }
    }
    BitmapImage BitmapToImageSource(Bitmap bitmap)
    {
        using (MemoryStream memory = new MemoryStream())
        {
            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
            memory.Position = 0;
            BitmapImage bitmapimage = new BitmapImage();
            bitmapimage.BeginInit();
            bitmapimage.StreamSource = memory;
            bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapimage.EndInit();

            return bitmapimage;
        }
    }

    private void LoadPipeline_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new() {
                CheckFileExists = true,
                Filter = "Pipeline|*.json",
                Title = "Load pipeline settings",
            };
        if (dialog.ShowDialog() == true)
        {
            _model.Effects = JsonSerializer.Deserialize<List<Effect>>(
                    File.ReadAllText(dialog.FileName),
                    new JsonSerializerOptions { AllowTrailingCommas = true, WriteIndented = true }
                )!
                .Select(e => new EffectItemVM(e))
                .ToList();

            EffectList.ItemsSource = _model.Effects;
            EffectList.UpdateLayout();
        }
    }

    private void SavePipeline_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog dialog = new() {
                Filter = "Pipeline|*.json",
                Title = "Save pipeline settings",
            };
        if (dialog.ShowDialog() == true)
        {
            File.WriteAllText(dialog.FileName,
                JsonSerializer.Serialize<List<Effect>>(
                        _model.Effects.Select(e => e.EffectData).ToList(),
                        new JsonSerializerOptions { AllowTrailingCommas = true, WriteIndented= true}
                    ));
        }
    }

    public void ApplyPipeline()
    {
        Bitmap bmp = (Bitmap)_model.Original.Clone();
        System.Drawing.Rectangle rect_ = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
        BitmapData data = bmp.LockBits(rect_, ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        int lenght = data.Width * bmp.Height * 3;
        byte[] bytes = new byte[lenght];
        Marshal.Copy(data.Scan0, bytes, 0, lenght);
        foreach(EffectItemVM eff in _model.Effects)
        {
            string name = eff.EffectData.Name;
            if (GFX_04_ImageManipulation.Effect.Effects.ContainsKey(name) is false)
                continue;
            if (eff.EffectData.IsActive is false)
                continue;
            var (_, func) = GFX_04_ImageManipulation.Effect.Effects.GetValueOrDefault(name)!;
            func(bytes, data.Width, eff.EffectData.Value);
        }
        Marshal.Copy(bytes, 0, data.Scan0, lenght);
        image.Source = BitmapToImageSource(bmp);
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (_model.Original is null)
            return;
        ApplyPipeline();
    }
}

public class MainWindowVM
{
    public Bitmap Original { get; set; }

    public List<EffectItemVM> Effects { get; set; } = new();
}