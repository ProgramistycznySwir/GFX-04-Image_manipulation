using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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
/// Interaction logic for EffectItem.xaml
/// </summary>
public partial class EffectItem : UserControl
{
    EffectItemVM _model;

    public EffectItem()
    {
        InitializeComponent();
        //base.DataContext = _model;
    }
}

public class EffectItemVM : INotifyPropertyChanged
{
    public EffectItemVM(string name)
    {
        if(Effect.Effects.ContainsKey(name) is false)
        {
            EffectData.Name = "INVALID EFFECT";
            return;
        }
        EffectData.Name = name;
        (Type, _) = Effect.Effects.GetValueOrDefault(EffectData.Name)!;
        //PropertyChanged(this, new PropertyChangedEventArgs(nameof(TextBox_IsVisible)));
        //PropertyChanged(this, new PropertyChangedEventArgs(nameof(Slider_IsVisible)));
    }
    public EffectItemVM(Effect eff)
    {
        if (Effect.Effects.ContainsKey(eff.Name) is false)
        {
            EffectData.Name = "INVALID EFFECT";
            return;
        }
        EffectData = eff;
        (Type, _) = Effect.Effects.GetValueOrDefault(EffectData.Name)!;
        //PropertyChanged(this, new PropertyChangedEventArgs(nameof(TextBox_IsVisible)));
        //PropertyChanged(this, new PropertyChangedEventArgs(nameof(Slider_IsVisible)));
    }

    public Effect EffectData { get; set; } = new();

    public Effect.Type Type { get; set; }

    public Visibility TextBox_IsVisible => Type is not Effect.Type.Switch ? Visibility.Visible : Visibility.Collapsed;
    public Visibility Slider_IsVisible => Type is Effect.Type.Percent ? Visibility.Visible : Visibility.Collapsed;

    public event PropertyChangedEventHandler PropertyChanged;
}

public class Effect
{
    public string Name { get; set; }
    public bool IsActive { get; set; } = false;
    public float Value { get; set; } = 1f;

    public enum Type { Switch, Value, Percent }

    /// <summary>
    /// All effects work by mautating input byte-array, creating copy at the beginning of pipeline is advised.<br/>
    /// Intensity should be in range 0-1.
    /// </summary>
    public static Dictionary<string, (Type, Action<byte[], int, float>)> Effects { get; } = new()
    {
        { "Add_R", (Type.Percent, (bmp, _, value) => Add(bmp, ChaR,   invLerp(value))) },
        { "Add_G", (Type.Percent, (bmp, _, value) => Add(bmp, ChaG,   invLerp(value))) },
        { "Add_B", (Type.Percent, (bmp, _, value) => Add(bmp, ChaB,   invLerp(value))) },
        { "Sub_R", (Type.Percent, (bmp, _, value) => Add(bmp, ChaR, - invLerp(value))) },
        { "Sub_G", (Type.Percent, (bmp, _, value) => Add(bmp, ChaG, - invLerp(value))) },
        { "Sub_B", (Type.Percent, (bmp, _, value) => Add(bmp, ChaB, - invLerp(value))) },
        { "Mul_R", (Type.Percent, (bmp, _, value) => Mul(bmp, ChaR,   value)) },
        { "Mul_G", (Type.Percent, (bmp, _, value) => Mul(bmp, ChaG,   value)) },
        { "Mul_B", (Type.Percent, (bmp, _, value) => Mul(bmp, ChaB,   value)) },
        { "Div_R", (Type.Percent, (bmp, _, value) => Mul(bmp, ChaR, 1/value)) },
        { "Div_G", (Type.Percent, (bmp, _, value) => Mul(bmp, ChaG, 1/value)) },
        { "Div_B", (Type.Percent, (bmp, _, value) => Mul(bmp, ChaB, 1/value)) },

        { "Lum", (Type.Value, (bmp, _, value) => Lum(bmp, value)) },

        { "Gray_R",   (Type.Switch, (bmp, _, _) => Gray_Cha(bmp, Cha.R)) },
        { "Gray_G",   (Type.Switch, (bmp, _, _) => Gray_Cha(bmp, Cha.G)) },
        { "Gray_B",   (Type.Switch, (bmp, _, _) => Gray_Cha(bmp, Cha.B)) },
        { "Gray_RG",  (Type.Switch, (bmp, _, _) => Gray_Cha(bmp, Cha.RG)) },
        { "Gray_RB",  (Type.Switch, (bmp, _, _) => Gray_Cha(bmp, Cha.RB)) },
        { "Gray_GB",  (Type.Switch, (bmp, _, _) => Gray_Cha(bmp, Cha.GB)) },
        { "Gray_RGB", (Type.Switch, (bmp, _, _) => Gray_Cha(bmp, Cha.RGB)) },


        { "Filter_Avg",  (Type.Switch, (bmp, width, _) => Filter_Mask(bmp, width, Mask_Avg)) },
        { "Filter_Avg2", (Type.Switch, (bmp, width, _) => Filter_Mask(bmp, width, Mask_Avg2)) },

        { "Filter_Median", (Type.Switch, (bmp, width, _) => Filter_Median(bmp, width, Mask_Avg)) },
    };

    #region >>> Point Transforms <<<

    public static void Add(Span<byte> bmp, int channel, int amount)
    {
        for (int i = channel; i < bmp.Length; i += 3)
            bmp[i] = clampByte(bmp[i] + amount);
    }
    public static void Mul(Span<byte> bmp, int channel, float amount)
    {
        for (int i = channel; i < bmp.Length; i += 3)
            bmp[i] = clampByte((int)(bmp[i] * amount));
    }
    public static void Lum(Span<byte> bmp, float amount)
    {
        Add(bmp, ChaR, invLerp(amount));
        Add(bmp, ChaG, invLerp(amount));
        Add(bmp, ChaB, invLerp(amount));
    }
    public static void Gray_Cha(Span<byte> bmp, Cha cha)
    {
        for (int i = 0; i < bmp.Length; i += 3)
        {
            int sum =
                  (((cha & Cha.R) is not 0) ? bmp[R(i)] : 0)
                + (((cha & Cha.G) is not 0) ? bmp[G(i)] : 0)
                + (((cha & Cha.B) is not 0) ? bmp[B(i)] : 0);
            int chaCount =
                  (((cha & Cha.R) is not 0) ? 1 : 0)
                + (((cha & Cha.G) is not 0) ? 1 : 0)
                + (((cha & Cha.B) is not 0) ? 1 : 0);
            byte avg = (byte)(sum / chaCount);

            bmp[R(i)] = avg;
            bmp[G(i)] = avg;
            bmp[B(i)] = avg;
        }
    }

    #endregion >>> Point Transforms <<<

    public static readonly (int X, int Y, int Ratio)[] Mask_Avg = {
            (-1, -1, 1), ( 0, -1, 1), ( 1,  -1, 1),
            (-1,  0, 1), ( 0,  0, 1), ( 1,   0, 1),
            (-1,  1, 1), ( 0,  1, 1), ( 1,   1, 1),
        };
    public static readonly (int X, int Y, int Ratio)[] Mask_Avg2 = {
            (-1, -1, 1), ( 0, -1, 2), ( 1, -1, 1),
            (-1,  0, 2), ( 0,  0, 4), ( 1,  0, 2),
            (-1,  1, 1), ( 0,  1, 2), ( 1,  1, 1),
        };

    public static void Filter_Mask(Span<byte> bmp, int width, (int X, int Y, int Ratio)[] mask)
    {
        Span<byte> result = new byte[bmp.Length];
        bmp.CopyTo(result);

        int height = (bmp.Length / 3) / width;
        Func<int, int, int> xy = (x, y) => (x + y * width) * 3;
        // Loop over all pixels:
        for (int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
                // Loop over channels:
                for(int cha = 0; cha < 3; cha++)
                {
                    int sum = 0, sumRatio = 0;

                    // Loop over mask pixels:
                    for(int i = 0; i < mask.Length; i++)
                    {
                        var(maskX, maskY) = (x + mask[i].X, y + mask[i].Y);
                        if (maskX < 0 || maskX >= width || maskY < 0 || maskY >= height)
                            continue;

                        sum      += bmp[xy(maskX, maskY) + cha] * mask[i].Ratio;
                        sumRatio += mask[i].Ratio;
                    }

                    result[xy(x, y) + cha] = clampByte(sum / sumRatio);
                }

        result.CopyTo(bmp);
    }

    public static void Filter_Median(Span<byte> bmp, int width, (int X, int Y, int)[] selector)
    {
        Span<byte> result = new byte[bmp.Length];
        bmp.CopyTo(result);

        int height = (bmp.Length / 3) / width;
        Func<int, int, int> xy = (x, y) => (x + y * width) * 3;
        Span<byte> buffer = stackalloc byte[selector.Length];
        // Loop over all pixels:
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                // Loop over channels:
                for (int cha = 0; cha < 3; cha++)
                {
                    int buffer_idx = 0;
                    // Loop over mask pixels:
                    for (int i = 0; i < selector.Length; i++)
                    {
                        var (maskX, maskY) = (x + selector[i].X, y + selector[i].Y);
                        if (maskX < 0 || maskX >= width || maskY < 0 || maskY >= height)
                            continue;

                        buffer[buffer_idx++] = bmp[xy(maskX, maskY) + cha];
                    }

                    result[xy(x, y) + cha] = Median(buffer.Slice(0, buffer_idx));
                }

        result.CopyTo(bmp);
    }

    private static byte Median(Span<byte> arr)
    {
        arr.Sort();
        int half = arr.Length / 2;
        return
            arr.Length % 2 == 0
                ? clampByte((arr[half - 1] + arr[half]) / 2)
                : arr[half];
    }


    public static void Filter_Median(Span<byte> bmp, int width, (int X, int Y, int)[] selector)
    {
        Span<byte> result = new byte[bmp.Length];
        bmp.CopyTo(result);

        int height = (bmp.Length / 3) / width;
        Func<int, int, int> xy = (x, y) => (x + y * width) * 3;
        Span<byte> buffer = stackalloc byte[selector.Length];
        // Loop over all pixels:
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                // Loop over channels:
                for (int cha = 0; cha < 3; cha++)
                {
                    int buffer_idx = 0;
                    // Loop over mask pixels:
                    for (int i = 0; i < selector.Length; i++)
                    {
                        var (maskX, maskY) = (x + selector[i].X, y + selector[i].Y);
                        if (maskX < 0 || maskX >= width || maskY < 0 || maskY >= height)
                            continue;

                        buffer[buffer_idx++] = bmp[xy(maskX, maskY) + cha];
                    }

                    result[xy(x, y) + cha] = Median(buffer.Slice(0, buffer_idx));
                }

        result.CopyTo(bmp);
    }

}
