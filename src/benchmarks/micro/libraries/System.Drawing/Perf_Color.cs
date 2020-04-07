// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using static System.Drawing.Color;

namespace System.Drawing.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Color 
    {
        private static readonly Color[] AllKnownColors;

        private readonly Color _field = DarkSalmon;

        static Perf_Color()
        {
            AllKnownColors = new[]
            {
                AliceBlue, AntiqueWhite, Aqua, Aquamarine, Azure, Beige,
                Bisque, Black, BlanchedAlmond, Blue, BlueViolet,
                Brown, BurlyWood, CadetBlue, Chartreuse, Chocolate,
                Coral, CornflowerBlue, Cornsilk, Crimson, Cyan,
                DarkBlue, DarkCyan, DarkGoldenrod, DarkGray, DarkGreen,
                DarkKhaki, DarkMagenta, DarkOliveGreen, DarkOrange, DarkOrchid,
                DarkRed, DarkSalmon, DarkSeaGreen, DarkSlateBlue, DarkSlateGray,
                DarkTurquoise, DarkViolet, DeepPink, DeepSkyBlue, DimGray,
                DodgerBlue, Firebrick, FloralWhite, ForestGreen, Fuchsia,
                Gainsboro, GhostWhite, Gold, Goldenrod, Gray,
                Green, GreenYellow, Honeydew, HotPink, IndianRed,
                Indigo, Ivory, Khaki, Lavender, LavenderBlush,
                LawnGreen, LemonChiffon, LightBlue, LightCoral, LightCyan,
                LightGoldenrodYellow, LightGray, LightGreen, LightPink, LightSalmon,
                LightSeaGreen, LightSkyBlue, LightSlateGray, LightSteelBlue, LightYellow,
                Lime, LimeGreen, Linen, Magenta, Maroon,
                MediumAquamarine, MediumBlue, MediumOrchid, MediumPurple, MediumSeaGreen,
                MediumSlateBlue, MediumSpringGreen, MediumTurquoise, MediumVioletRed, MidnightBlue,
                MintCream, MistyRose, Moccasin, NavajoWhite, Navy,
                OldLace, Olive, OliveDrab, Orange, OrangeRed,
                Orchid, PaleGoldenrod, PaleGreen, PaleTurquoise, PaleVioletRed,
                PapayaWhip, PeachPuff, Peru, Pink, Plum,
                PowderBlue, Purple, Red, RosyBrown, RoyalBlue,
                SaddleBrown, Salmon, SandyBrown, SeaGreen, SeaShell,
                Sienna, Silver, SkyBlue, SlateBlue, SlateGray,
                Snow, SpringGreen, SteelBlue, Tan, Teal,
                Thistle, Tomato, Transparent, Turquoise, Violet,
                Wheat, White, WhiteSmoke, Yellow, YellowGreen
            };
        }

        [Benchmark]
        public Color FromArgb_Channels() => FromArgb(byte.MaxValue, 0xFF, byte.MinValue, 0xFF);

        [Benchmark]
        public Color FromArgb_AlphaColor() => FromArgb(0xFF, _field);

        [Benchmark]
        public float GetBrightness()
        {
            float brightness = 0.0f;
            var colors = AllKnownColors;

            for (int j = 0; j < colors.Length; j++)
                brightness += colors[j].GetBrightness();

            return brightness;
        }

        [Benchmark]
        public float GetHue()
        {
            float hue = 0.0f;
            var colors = AllKnownColors;

            for (int j = 0; j < colors.Length; j++)
                hue += colors[j].GetHue();

            return hue;
        }

        [Benchmark]
        public float GetSaturation()
        {
            float saturation = 0.0f;
            var colors = AllKnownColors;

            for (int j = 0; j < colors.Length; j++)
                saturation += colors[j].GetSaturation();

            return saturation;
        }
    }
}
