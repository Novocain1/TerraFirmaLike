using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using System.Drawing.Imaging;

namespace TerraFirmaLike.Utility
{
    class ColorUtilMore : ColorUtil
    {
        
        public static int MultiplyColor(int a, int b)
        {
            Vec4f rgba1 = new Vec4f();
            Vec4f rgba2 = new Vec4f();
            ToRGBAVec4f(a, ref rgba1);
            ToRGBAVec4f(b, ref rgba2);

            rgba1.A = Math.Min(rgba1.A + rgba2.A, 1.0f);
            rgba1.R *= rgba2.R * rgba2.A;
            rgba1.G *= rgba2.G * rgba2.A;
            rgba1.B *= rgba2.B * rgba2.A;

            return Vec2RGBA(rgba1);
        }

        public static int DivideColor(int a, int b)
        {
            Vec4f rgba1 = new Vec4f();
            Vec4f rgba2 = new Vec4f();
            ToRGBAVec4f(a, ref rgba1);
            ToRGBAVec4f(b, ref rgba2);

            rgba1.A = Math.Min(rgba1.A + rgba2.A, 1.0f);
            rgba1.R = Math.Min(rgba2.R / rgba1.R, 1.0f);
            rgba1.G = Math.Min(rgba2.G / rgba1.G, 1.0f);
            rgba1.B = Math.Min(rgba2.B / rgba1.B, 1.0f);
            return Vec2RGBA(rgba1);
        }

        public static int Vec2RGBA(Vec4f input)
        {
            return ToRgba((int)(input.A * 255), (int)(input.R * 255), (int)(input.G * 255), (int)(input.B * 255));
        }
    }
}
