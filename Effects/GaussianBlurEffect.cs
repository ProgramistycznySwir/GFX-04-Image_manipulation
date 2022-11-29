using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static GFX_04_ImageManipulation.Prelude;
namespace GFX_04_ImageManipulation.Effects;

public static class GaussianBlurEffect
{
    public static (int X, int Y, float Ratio)[] GenerateMatrix(int size, float weight)
    {
        (int X, int Y, float Ratio)[] mask = new (int X, int Y, float Ratio)[size*size];
        float maskSum = 0;
        int foff = (size - 1) / 2;
        float constant = 1f / (2 * MathF.PI * weight * weight);
        for (int y = -foff, i = 0; y <= foff; y++)
            for (int x = -foff; x <= foff; x++, i++)
            {
                float distance = ((y * y) + (x * x)) / (2 * weight * weight);
                mask[i] = (x, y, constant * MathF.Exp(-distance));
                maskSum += mask[i].Ratio;
            }
        for(int i = 0; i < mask.Length; i++)
            mask[i].Ratio = mask[i].Ratio * 1f / maskSum;

        return mask;
    }

    // Identical as Sobel we have to just multiply by matrix
    public static void Apply(Span<byte> bmp, int width, (int X, int Y, float Ratio)[] mask)
    {
        Span<byte> result = new byte[bmp.Length];
        bmp.CopyTo(result);

        int height = (bmp.Length / 3) / width;
        Func<int, int, int> xy = (x, y) => (x + y * width) * 3;
        // Loop over all pixels:
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                // Loop over channels:
                for (int cha = 0; cha < 3; cha++)
                {
                    float value = 0;
                    // Loop over mask pixels:
                    for (int i = 0; i < mask.Length; i++)
                    {
                        var (maskX, maskY) = (x + mask[i].X, y + mask[i].Y);
                        // Check if position is in image boundaries.
                        if (maskX < 0 || maskX >= width || maskY < 0 || maskY >= height)
                            continue;

                        value += bmp[xy(maskX, maskY) + cha] * mask[i].Ratio;
                    }

                    result[xy(x, y) + cha] = clampByte((int)value);
                }

        result.CopyTo(bmp);
    }
}
