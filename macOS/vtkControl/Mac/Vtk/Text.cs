// PrePoMax for macOS - text of the VTK compatible library: the size of a text in display pixels and its image, drawn with
// SkiaSharp. The font sizes are pixels, as in VTK.

using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Kitware.VTK
{
    internal static class TextRenderer
    {
        private static readonly Dictionary<string, SKTypeface> _typefaces = new Dictionary<string, SKTypeface>();
        private static readonly Dictionary<string, int[]> _sizes = new Dictionary<string, int[]>();

        private static SKTypeface Typeface(vtkTextProperty p)
        {
            string family = p._fontFamily;
            if (family == "Courier") family = "Menlo";
            else if (family == "Times") family = "Times New Roman";
            else if (string.IsNullOrEmpty(family) || family == "Arial") family = "Arial";
            string key = family + (p._bold ? "|b" : "") + (p._italic ? "|i" : "");
            lock (_typefaces)
            {
                if (!_typefaces.TryGetValue(key, out SKTypeface typeface))
                {
                    SKFontStyle style = new SKFontStyle(p._bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                                                        SKFontStyleWidth.Normal,
                                                        p._italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
                    typeface = SKTypeface.FromFamilyName(family, style) ?? SKTypeface.Default;
                    _typefaces[key] = typeface;
                }
                return typeface;
            }
        }
        internal static SKPaint Paint(vtkTextProperty p, float scale)
        {
            return new SKPaint
            {
                Typeface = Typeface(p),
                TextSize = Math.Max(1, p._fontSize) * scale,
                IsAntialias = true,
                SubpixelText = true,
                LcdRenderText = false,
                Color = SKColors.White
            };
        }
        internal static string[] Lines(string text)
        {
            return (text ?? "").Replace("\r\n", "\n").Split('\n');
        }
        // The line height of VTK: the font size times the line spacing
        internal static float LineHeight(vtkTextProperty p, SKPaint paint, float scale)
        {
            SKFontMetrics m = paint.FontMetrics;
            float natural = m.Descent - m.Ascent;
            return (float)Math.Max(natural, p._fontSize * scale) * (float)Math.Max(0.1, p._lineSpacing);
        }
        // The width and height of the text in display pixels
        public static int[] Measure(string text, vtkTextProperty p)
        {
            if (string.IsNullOrEmpty(text)) return new[] { 0, 0 };
            string key = text + "\u0001" + p._fontFamily + p._fontSize + p._bold + p._italic + p._lineSpacing;
            lock (_sizes)
            {
                if (_sizes.TryGetValue(key, out int[] cached)) return (int[])cached.Clone();
            }
            int[] size;
            using (SKPaint paint = Paint(p, 1))
            {
                string[] lines = Lines(text);
                float width = 0;
                foreach (string line in lines) width = Math.Max(width, paint.MeasureText(line));
                SKFontMetrics m = paint.FontMetrics;
                float natural = m.Descent - m.Ascent;
                float lineHeight = LineHeight(p, paint, 1);
                float height = natural + (lines.Length - 1) * lineHeight;
                size = new[] { (int)Math.Ceiling(width), (int)Math.Ceiling(height) };
            }
            if (Math.Abs(p._orientation) > 1e-6)
            {
                double a = p._orientation * Math.PI / 180;
                double w = Math.Abs(size[0] * Math.Cos(a)) + Math.Abs(size[1] * Math.Sin(a));
                double h = Math.Abs(size[0] * Math.Sin(a)) + Math.Abs(size[1] * Math.Cos(a));
                size = new[] { (int)Math.Ceiling(w), (int)Math.Ceiling(h) };
            }
            lock (_sizes)
            {
                if (_sizes.Count > 20000) _sizes.Clear();
                _sizes[key] = size;
            }
            return (int[])size.Clone();
        }
        // The image of the text: premultiplied RGBA bytes, top row first, with the size in device pixels
        public static byte[] Rasterize(string text, vtkTextProperty p, float scale, out int width, out int height)
        {
            string[] lines = Lines(text);
            using (SKPaint paint = Paint(p, scale))
            {
                float w = 0;
                foreach (string line in lines) w = Math.Max(w, paint.MeasureText(line));
                SKFontMetrics m = paint.FontMetrics;
                float natural = m.Descent - m.Ascent;
                float lineHeight = LineHeight(p, paint, scale);
                float h = natural + (lines.Length - 1) * lineHeight;
                int shadow = p._shadow ? Math.Max(1, (int)Math.Round(scale)) : 0;
                width = Math.Max(1, (int)Math.Ceiling(w) + 2 + shadow);
                height = Math.Max(1, (int)Math.Ceiling(h) + 2 + shadow);
                using (SKBitmap bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul)))
                using (SKCanvas canvas = new SKCanvas(bitmap))
                {
                    canvas.Clear(SKColors.Transparent);
                    SKColor color = new SKColor((byte)(p._color[0] * 255), (byte)(p._color[1] * 255), (byte)(p._color[2] * 255),
                                                (byte)(p._opacity * 255));
                    float y = 1 - m.Ascent;
                    foreach (string line in lines)
                    {
                        float lw = paint.MeasureText(line);
                        float x = 1;
                        if (p._justification == 1) x = 1 + (w - lw) / 2;
                        else if (p._justification == 2) x = 1 + w - lw;
                        if (shadow > 0)
                        {
                            paint.Color = new SKColor(0, 0, 0, (byte)(p._opacity * 255));
                            canvas.DrawText(line, x + shadow, y + shadow, paint);
                        }
                        paint.Color = color;
                        canvas.DrawText(line, x, y, paint);
                        y += lineHeight;
                    }
                    canvas.Flush();
                    return bitmap.Bytes;
                }
            }
        }
    }
}
