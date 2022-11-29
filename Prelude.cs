using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFX_04_ImageManipulation;

public static class Prelude
{
    public static byte clampByte(int num) => num > byte.MaxValue ? (byte)255 : num < 0 ? (byte)0 : (byte)num;
    public static byte invLerp(float t) => (byte)(t * byte.MaxValue);

    // Chanels:
    public const int ChaR = 2;
    public const int ChaG = 1;
    public const int ChaB = 0;

    public static int R(int i) => i + ChaR;
    public static int G(int i) => i + ChaG;
    public static int B(int i) => i + ChaB;

    [Flags]
    public enum Cha {
        R   = 1,
        G   = 2, 
        RG  = 3,
        B   = 4,
        RB  = 5,
        GB  = 6,
        RGB = 7
    }
}
