// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Drawing;
using MessagePack;

namespace CaeGlobals
{
    // A color keeps whether it is a known or a named color, as equality of colors compares that too:
    //   nil              Color.Empty
    //   int              a color by its ARGB value
    //   [int]            a known color
    //   [string]         a named color that is not a known one
    // A size, a point or a rectangle is the array of its components.
    public sealed class PmxDrawingFormatter<T> : PmxFormatter<T>
    {
        // Methods                                                                                                                  
        public override void Write(ref MessagePackWriter writer, T value, PmxWriteContext context)
        {
            object item = value;
            if (item is Color color)
            {
                if (color.IsEmpty) writer.WriteNil();
                else if (color.IsKnownColor)
                {
                    writer.WriteArrayHeader(1);
                    writer.Write((int)color.ToKnownColor());
                }
                else if (color.IsNamedColor)
                {
                    writer.WriteArrayHeader(1);
                    writer.Write(color.Name);
                }
                else writer.Write(color.ToArgb());
            }
            else if (item is Size size) WriteValues(ref writer, size.Width, size.Height);
            else if (item is Point point) WriteValues(ref writer, point.X, point.Y);
            else if (item is Rectangle rectangle)
                WriteValues(ref writer, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
            else if (item is SizeF sizeF) WriteValues(ref writer, sizeF.Width, sizeF.Height);
            else if (item is PointF pointF) WriteValues(ref writer, pointF.X, pointF.Y);
            else if (item is RectangleF rectangleF)
                WriteValues(ref writer, rectangleF.X, rectangleF.Y, rectangleF.Width, rectangleF.Height);
            else throw new NotSupportedException();
        }
        private static void WriteValues(ref MessagePackWriter writer, params int[] values)
        {
            writer.WriteArrayHeader(values.Length);
            foreach (var value in values) writer.Write(value);
        }
        private static void WriteValues(ref MessagePackWriter writer, params float[] values)
        {
            writer.WriteArrayHeader(values.Length);
            foreach (var value in values) writer.Write(value);
        }
        public override T Read(ref MessagePackReader reader, PmxReadContext context)
        {
            object item;
            if (typeof(T) == typeof(Color)) item = ReadColor(ref reader);
            else if (typeof(T) == typeof(Size) || typeof(T) == typeof(Point) || typeof(T) == typeof(Rectangle))
            {
                int[] v = new int[reader.ReadArrayHeader()];
                for (int i = 0; i < v.Length; i++) v[i] = reader.ReadInt32();
                if (typeof(T) == typeof(Size)) item = new Size(v[0], v[1]);
                else if (typeof(T) == typeof(Point)) item = new Point(v[0], v[1]);
                else item = new Rectangle(v[0], v[1], v[2], v[3]);
            }
            else
            {
                float[] v = new float[reader.ReadArrayHeader()];
                for (int i = 0; i < v.Length; i++) v[i] = reader.ReadSingle();
                if (typeof(T) == typeof(SizeF)) item = new SizeF(v[0], v[1]);
                else if (typeof(T) == typeof(PointF)) item = new PointF(v[0], v[1]);
                else item = new RectangleF(v[0], v[1], v[2], v[3]);
            }
            return (T)item;
        }
        private static Color ReadColor(ref MessagePackReader reader)
        {
            if (reader.TryReadNil()) return Color.Empty;
            if (reader.NextMessagePackType == MessagePackType.Integer) return Color.FromArgb(reader.ReadInt32());
            //
            reader.ReadArrayHeader();
            if (reader.NextMessagePackType == MessagePackType.Integer) return Color.FromKnownColor((KnownColor)reader.ReadInt32());
            return Color.FromName(reader.ReadString());
        }
    }
}
