// PrePoMax for macOS - System.Drawing (GDI+) on SkiaSharp.
// Color, Point, Size, Rectangle, SystemColors and ColorTranslator come from System.Drawing.Primitives of .NET.

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using SkiaSharp;

namespace System.Drawing
{
    public enum FontStyle { Regular = 0, Bold = 1, Italic = 2, Underline = 4, Strikeout = 8 }

    public enum GraphicsUnit { World = 0, Display = 1, Pixel = 2, Point = 3, Inch = 4, Document = 5, Millimeter = 6 }

    public enum ContentAlignment
    {
        TopLeft = 1, TopCenter = 2, TopRight = 4,
        MiddleLeft = 16, MiddleCenter = 32, MiddleRight = 64,
        BottomLeft = 256, BottomCenter = 512, BottomRight = 1024
    }

    public enum StringAlignment { Near = 0, Center = 1, Far = 2 }

    public enum StringTrimming { None = 0, Character = 1, Word = 2, EllipsisCharacter = 3, EllipsisWord = 4, EllipsisPath = 5 }

    [Flags]
    public enum StringFormatFlags
    {
        DirectionRightToLeft = 1, DirectionVertical = 2, FitBlackBox = 4, DisplayFormatControl = 32, NoFontFallback = 1024,
        MeasureTrailingSpaces = 2048, NoWrap = 4096, LineLimit = 8192, NoClip = 16384
    }

    public enum RotateFlipType
    {
        RotateNoneFlipNone = 0, Rotate90FlipNone = 1, Rotate180FlipNone = 2, Rotate270FlipNone = 3,
        RotateNoneFlipX = 4, Rotate90FlipX = 5, Rotate180FlipX = 6, Rotate270FlipX = 7,
        RotateNoneFlipY = Rotate180FlipX, Rotate90FlipY = Rotate270FlipX, Rotate180FlipY = RotateNoneFlipX,
        Rotate270FlipY = Rotate90FlipX, RotateNoneFlipXY = Rotate180FlipNone, Rotate90FlipXY = Rotate270FlipNone,
        Rotate180FlipXY = RotateNoneFlipNone, Rotate270FlipXY = Rotate90FlipNone
    }

    public enum CopyPixelOperation { SourceCopy = 13369376 }

    internal static class SkiaConvert
    {
        public static SKColor ToSK(this Color c) { return new SKColor(c.R, c.G, c.B, c.A); }
        public static Color ToColor(this SKColor c) { return Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue); }
        public static SKRect ToSK(this RectangleF r) { return new SKRect(r.Left, r.Top, r.Right, r.Bottom); }
        public static SKRect ToSK(this Rectangle r) { return new SKRect(r.Left, r.Top, r.Right, r.Bottom); }
        public static SKRectI ToSKI(this Rectangle r) { return new SKRectI(r.Left, r.Top, r.Right, r.Bottom); }
    }

    // Images
    [Serializable]
    [TypeConverter(typeof(ImageConverter))]
    public abstract class Image : MarshalByRefObject, IDisposable, ICloneable, Runtime.Serialization.ISerializable
    {
        internal SKBitmap _bitmap;
        private float _dpiX = 96;
        private float _dpiY = 96;

        public int Width { get { return _bitmap?.Width ?? 0; } }
        public int Height { get { return _bitmap?.Height ?? 0; } }
        public Size Size { get { return new Size(Width, Height); } }
        public SizeF PhysicalDimension { get { return new SizeF(Width, Height); } }
        public float HorizontalResolution { get { return _dpiX; } }
        public float VerticalResolution { get { return _dpiY; } }
        public ImageFormat RawFormat { get; internal set; } = ImageFormat.MemoryBmp;
        public PixelFormat PixelFormat { get { return PixelFormat.Format32bppArgb; } }
        public object Tag { get; set; }
        public int Flags { get { return 0; } }
        // Avalonia bitmap of this image, cached until the pixels change
        internal object NativeCache;
        internal int Version;

        internal Image(SKBitmap bitmap) { _bitmap = bitmap; }

        // Serialization of GDI+: the image file in the entry "Data"
        public void GetObjectData(Runtime.Serialization.SerializationInfo info, Runtime.Serialization.StreamingContext context)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                Save(stream, ImageFormat.Png);
                info.AddValue("Data", stream.ToArray(), typeof(byte[]));
            }
        }
        internal static SKBitmap FromSerializationInfo(Runtime.Serialization.SerializationInfo info)
        {
            foreach (Runtime.Serialization.SerializationEntry entry in info)
            {
                if (entry.Name == "Data" && entry.Value is byte[] data) return Bitmap.Decode(data);
            }
            return new SKBitmap(1, 1);
        }

        public SKBitmap SKBitmap { get { return _bitmap; } }
        internal void Changed() { Version++; NativeCache = null; }

        public static Image FromFile(string filename) { return new Bitmap(filename); }
        public static Image FromFile(string filename, bool useEmbeddedColorManagement) { return new Bitmap(filename); }
        public static Image FromStream(Stream stream) { return new Bitmap(stream); }
        public static Image FromStream(Stream stream, bool useEmbeddedColorManagement) { return new Bitmap(stream); }
        public static Image FromStream(Stream stream, bool useEmbeddedColorManagement, bool validateImageData)
        {
            return new Bitmap(stream);
        }

        public void SetResolution(float xDpi, float yDpi) { _dpiX = xDpi; _dpiY = yDpi; }
        public object Clone() { return new Bitmap(this); }
        public void Save(string filename) { Save(filename, ImageFormat.FromExtension(Path.GetExtension(filename))); }
        public void Save(string filename, ImageFormat format)
        {
            using (FileStream stream = File.Create(filename)) Save(stream, format);
        }
        public void Save(string filename, ImageCodecInfo encoder, EncoderParameters encoderParams)
        {
            using (FileStream stream = File.Create(filename)) Save(stream, encoder, encoderParams);
        }
        public void Save(Stream stream, ImageCodecInfo encoder, EncoderParameters encoderParams)
        {
            int quality = 95;
            if (encoderParams?.Param != null)
            {
                foreach (var p in encoderParams.Param)
                    if (p != null && p.Encoder == Encoder.Quality) quality = (int)p.Value;
            }
            Save(stream, encoder?.FormatID == ImageFormat.Jpeg.Guid ? ImageFormat.Jpeg : ImageFormat.Png, quality);
        }
        public void Save(Stream stream, ImageFormat format) { Save(stream, format, 95); }
        private void Save(Stream stream, ImageFormat format, int quality)
        {
            if (format == ImageFormat.Bmp || format == ImageFormat.MemoryBmp)
            {
                WriteBmp(stream);
                return;
            }
            SKEncodedImageFormat skFormat = SKEncodedImageFormat.Png;
            if (format == ImageFormat.Jpeg) skFormat = SKEncodedImageFormat.Jpeg;
            else if (format == ImageFormat.Gif) skFormat = SKEncodedImageFormat.Gif;
            else if (format == ImageFormat.Icon) skFormat = SKEncodedImageFormat.Ico;
            using (SKImage image = SKImage.FromBitmap(_bitmap))
            using (SKData data = image.Encode(skFormat, quality) ?? image.Encode(SKEncodedImageFormat.Png, 100))
            {
                data.SaveTo(stream);
            }
        }
        private void WriteBmp(Stream stream)
        {
            int w = Width, h = Height, rowSize = w * 4, size = 54 + rowSize * h;
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((byte)'B'); writer.Write((byte)'M'); writer.Write(size); writer.Write(0); writer.Write(54);
            writer.Write(40); writer.Write(w); writer.Write(h); writer.Write((short)1); writer.Write((short)32);
            writer.Write(0); writer.Write(rowSize * h); writer.Write(3780); writer.Write(3780); writer.Write(0); writer.Write(0);
            for (int y = h - 1; y >= 0; y--)
            {
                for (int x = 0; x < w; x++)
                {
                    SKColor c = _bitmap.GetPixel(x, y);
                    writer.Write(c.Blue); writer.Write(c.Green); writer.Write(c.Red); writer.Write(c.Alpha);
                }
            }
            writer.Flush();
        }
        public void RotateFlip(RotateFlipType rotateFlipType)
        {
            int type = (int)rotateFlipType;
            int rotation = type & 3;
            bool flipX = (type & 4) != 0;
            SKBitmap source = _bitmap;
            int w = rotation % 2 == 0 ? source.Width : source.Height;
            int h = rotation % 2 == 0 ? source.Height : source.Width;
            SKBitmap result = new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Premul);
            using (SKCanvas canvas = new SKCanvas(result))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.Translate(w / 2f, h / 2f);
                if (flipX) canvas.Scale(-1, 1);
                canvas.RotateDegrees(90 * rotation);
                canvas.Translate(-source.Width / 2f, -source.Height / 2f);
                canvas.DrawBitmap(source, 0, 0);
            }
            _bitmap = result;
            source.Dispose();
            Changed();
        }
        public Image GetThumbnailImage(int thumbWidth, int thumbHeight, GetThumbnailImageAbort callback, IntPtr callbackData)
        {
            return new Bitmap(this, thumbWidth, thumbHeight);
        }
        public delegate bool GetThumbnailImageAbort();
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        protected virtual void Dispose(bool disposing)
        {
            _bitmap?.Dispose();
            _bitmap = null;
        }
        public static bool IsAlphaPixelFormat(PixelFormat pixfmt) { return true; }
    }

    [Serializable]
    public sealed class Bitmap : Image
    {
        private readonly Dictionary<IntPtr, (BitmapData Data, Runtime.InteropServices.GCHandle Handle, int[] Buffer)> _locks =
            new Dictionary<IntPtr, (BitmapData, Runtime.InteropServices.GCHandle, int[])>();

        public Bitmap(int width, int height)
            : base(new SKBitmap(Math.Max(1, width), Math.Max(1, height), SKColorType.Bgra8888, SKAlphaType.Premul))
        {
            _bitmap.Erase(SKColors.Transparent);
        }
        public Bitmap(int width, int height, PixelFormat format) : this(width, height) { }
        public Bitmap(int width, int height, Graphics g) : this(width, height) { }
        public Bitmap(Image original) : base(original._bitmap.Copy()) { }
        public Bitmap(Image original, Size newSize) : this(original, newSize.Width, newSize.Height) { }
        public Bitmap(Image original, int width, int height) : base(Resize(original._bitmap, width, height)) { }
        public Bitmap(string filename) : base(Decode(File.ReadAllBytes(filename))) { RawFormat = ImageFormat.FromExtension(Path.GetExtension(filename)); }
        public Bitmap(string filename, bool useIcm) : this(filename) { }
        public Bitmap(Stream stream) : base(Decode(ReadAll(stream))) { RawFormat = ImageFormat.Png; }
        public Bitmap(Stream stream, bool useIcm) : this(stream) { }
        public Bitmap(Type type, string resource) : this(type.Assembly.GetManifestResourceStream(type, resource)) { }
        internal Bitmap(SKBitmap bitmap) : base(bitmap) { }
        private Bitmap(Runtime.Serialization.SerializationInfo info, Runtime.Serialization.StreamingContext context)
            : base(FromSerializationInfo(info)) { }

        public static Bitmap FromSKBitmap(SKBitmap bitmap) { return new Bitmap(bitmap); }

        private static byte[] ReadAll(Stream stream)
        {
            if (stream == null) throw new ArgumentException("Parameter is not valid.");
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
        internal static SKBitmap Decode(byte[] data)
        {
            SKBitmap decoded = SKBitmap.Decode(data);
            if (decoded == null)
            {
                decoded = IcoDecoder.Decode(data);
                if (decoded == null) throw new ArgumentException("Parameter is not valid.");
            }
            if (decoded.ColorType != SKColorType.Bgra8888 || decoded.AlphaType != SKAlphaType.Premul)
            {
                SKBitmap converted = new SKBitmap(decoded.Width, decoded.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
                using (SKCanvas canvas = new SKCanvas(converted))
                {
                    canvas.Clear(SKColors.Transparent);
                    canvas.DrawBitmap(decoded, 0, 0);
                }
                decoded.Dispose();
                decoded = converted;
            }
            return decoded;
        }
        private static SKBitmap Resize(SKBitmap source, int width, int height)
        {
            SKBitmap result = new SKBitmap(Math.Max(1, width), Math.Max(1, height), SKColorType.Bgra8888, SKAlphaType.Premul);
            using (SKCanvas canvas = new SKCanvas(result))
            using (SKPaint paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true })
            {
                canvas.Clear(SKColors.Transparent);
                canvas.DrawBitmap(source, new SKRect(0, 0, result.Width, result.Height), paint);
            }
            return result;
        }

        public Color GetPixel(int x, int y) { return _bitmap.GetPixel(x, y).ToColor(); }
        public void SetPixel(int x, int y, Color color) { _bitmap.SetPixel(x, y, color.ToSK()); Changed(); }
        public void MakeTransparent() { MakeTransparent(GetPixel(0, Height - 1)); }
        public void MakeTransparent(Color transparentColor)
        {
            SKColor key = transparentColor.ToSK();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    SKColor c = _bitmap.GetPixel(x, y);
                    if (c.Red == key.Red && c.Green == key.Green && c.Blue == key.Blue) _bitmap.SetPixel(x, y, SKColors.Transparent);
                }
            Changed();
        }
        public Bitmap Clone(Rectangle rect, PixelFormat format)
        {
            SKBitmap result = new SKBitmap(rect.Width, rect.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            _bitmap.ExtractSubset(result, rect.ToSKI());
            return new Bitmap(result.Copy());
        }
        public Bitmap Clone(RectangleF rect, PixelFormat format) { return Clone(Rectangle.Round(rect), format); }
        public IntPtr GetHicon() { return IntPtr.Zero; }
        public IntPtr GetHbitmap() { return IntPtr.Zero; }
        public static Bitmap FromHicon(IntPtr hicon) { return new Bitmap(16, 16); }

        // Pixels are copied to a pinned 32 bpp ARGB buffer (byte order B, G, R, A as in GDI+) and back on UnlockBits
        public BitmapData LockBits(Rectangle rect, ImageLockMode flags, PixelFormat format)
        {
            int[] buffer = new int[rect.Width * rect.Height];
            for (int y = 0; y < rect.Height; y++)
                for (int x = 0; x < rect.Width; x++)
                {
                    SKColor c = _bitmap.GetPixel(rect.X + x, rect.Y + y);
                    buffer[y * rect.Width + x] = (c.Alpha << 24) | (c.Red << 16) | (c.Green << 8) | c.Blue;
                }
            var handle = Runtime.InteropServices.GCHandle.Alloc(buffer, Runtime.InteropServices.GCHandleType.Pinned);
            BitmapData data = new BitmapData
            {
                Width = rect.Width, Height = rect.Height, Stride = rect.Width * 4, PixelFormat = format,
                Scan0 = handle.AddrOfPinnedObject(), Reserved = (int)flags, Rect = rect
            };
            _locks[data.Scan0] = (data, handle, buffer);
            return data;
        }
        public BitmapData LockBits(Rectangle rect, ImageLockMode flags, PixelFormat format, BitmapData bitmapData)
        {
            return LockBits(rect, flags, format);
        }
        public void UnlockBits(BitmapData bitmapdata)
        {
            if (!_locks.TryGetValue(bitmapdata.Scan0, out var entry)) return;
            _locks.Remove(bitmapdata.Scan0);
            if ((entry.Data.Reserved & (int)ImageLockMode.WriteOnly) != 0)
            {
                Rectangle rect = entry.Data.Rect;
                for (int y = 0; y < rect.Height; y++)
                    for (int x = 0; x < rect.Width; x++)
                    {
                        int v = entry.Buffer[y * rect.Width + x];
                        _bitmap.SetPixel(rect.X + x, rect.Y + y,
                                         new SKColor((byte)(v >> 16), (byte)(v >> 8), (byte)v, (byte)(v >> 24)));
                    }
                Changed();
            }
            entry.Handle.Free();
        }
    }

    // Decodes .ico files (also the 4 and 8 bit palette images SkiaSharp does not read)
    internal static class IcoDecoder
    {
        public static SKBitmap Decode(byte[] data) { return Decode(data, 0); }
        public static SKBitmap Decode(byte[] data, int preferredSize)
        {
            if (data.Length < 6 || data[0] != 0 || data[1] != 0 || data[2] != 1) return null;
            int count = BitConverter.ToUInt16(data, 4);
            int best = -1, bestScore = int.MinValue;
            for (int i = 0; i < count; i++)
            {
                int e = 6 + 16 * i;
                if (e + 16 > data.Length) break;
                int w = data[e] == 0 ? 256 : data[e];
                int bpp = BitConverter.ToUInt16(data, e + 6);
                int score = preferredSize > 0 ? -Math.Abs(w - preferredSize) * 100 + bpp : w * 100 + bpp;
                if (score > bestScore) { bestScore = score; best = i; }
            }
            if (best < 0) return null;
            int entry = 6 + 16 * best;
            int size = BitConverter.ToInt32(data, entry + 8);
            int offset = BitConverter.ToInt32(data, entry + 12);
            byte[] image = new byte[size];
            Array.Copy(data, offset, image, 0, Math.Min(size, data.Length - offset));
            if (image.Length > 8 && image[0] == 0x89 && image[1] == (byte)'P') return SKBitmap.Decode(image);
            return DecodeDib(image);
        }
        private static SKBitmap DecodeDib(byte[] d)
        {
            int headerSize = BitConverter.ToInt32(d, 0);
            int width = BitConverter.ToInt32(d, 4);
            int height = BitConverter.ToInt32(d, 8) / 2;
            int bpp = BitConverter.ToUInt16(d, 14);
            int colors = BitConverter.ToInt32(d, 32);
            if (colors == 0 && bpp <= 8) colors = 1 << bpp;
            int paletteOffset = headerSize;
            int pixelOffset = paletteOffset + colors * 4;
            int stride = ((width * bpp + 31) / 32) * 4;
            int maskStride = ((width + 31) / 32) * 4;
            int maskOffset = pixelOffset + stride * height;
            SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
            bool hasAlpha = false;
            for (int y = 0; y < height; y++)
            {
                int row = pixelOffset + (height - 1 - y) * stride;
                for (int x = 0; x < width; x++)
                {
                    byte r, g, b, a = 255;
                    if (bpp == 32)
                    {
                        int p = row + x * 4;
                        b = d[p]; g = d[p + 1]; r = d[p + 2]; a = d[p + 3];
                        if (a != 0) hasAlpha = true;
                    }
                    else if (bpp == 24)
                    {
                        int p = row + x * 3;
                        b = d[p]; g = d[p + 1]; r = d[p + 2];
                    }
                    else
                    {
                        int index;
                        if (bpp == 8) index = d[row + x];
                        else if (bpp == 4) index = (d[row + x / 2] >> (x % 2 == 0 ? 4 : 0)) & 0x0F;
                        else index = (d[row + x / 8] >> (7 - x % 8)) & 1;
                        int p = paletteOffset + index * 4;
                        b = d[p]; g = d[p + 1]; r = d[p + 2];
                    }
                    bitmap.SetPixel(x, y, new SKColor(r, g, b, a));
                }
            }
            if (!(bpp == 32 && hasAlpha) && maskOffset + maskStride * height <= d.Length)
            {
                for (int y = 0; y < height; y++)
                {
                    int row = maskOffset + (height - 1 - y) * maskStride;
                    for (int x = 0; x < width; x++)
                    {
                        bool transparent = ((d[row + x / 8] >> (7 - x % 8)) & 1) != 0;
                        if (transparent) bitmap.SetPixel(x, y, SKColors.Transparent);
                    }
                }
            }
            return bitmap;
        }
    }

    [Serializable]
    [TypeConverter(typeof(IconConverter))]
    public sealed class Icon : MarshalByRefObject, IDisposable, ICloneable, Runtime.Serialization.ISerializable
    {
        private readonly byte[] _data;
        private readonly SKBitmap _bitmap;

        public int Width { get { return _bitmap.Width; } }
        public int Height { get { return _bitmap.Height; } }
        public Size Size { get { return new Size(Width, Height); } }
        public IntPtr Handle { get { return IntPtr.Zero; } }

        public Icon(string fileName) : this(File.ReadAllBytes(fileName), 32) { }
        public Icon(string fileName, Size size) : this(File.ReadAllBytes(fileName), size.Width) { }
        public Icon(string fileName, int width, int height) : this(File.ReadAllBytes(fileName), width) { }
        public Icon(Stream stream) : this(ReadAll(stream), 32) { }
        public Icon(Stream stream, Size size) : this(ReadAll(stream), size.Width) { }
        public Icon(Stream stream, int width, int height) : this(ReadAll(stream), width) { }
        public Icon(Icon original, Size size) : this(original._data, size.Width) { }
        public Icon(Icon original, int width, int height) : this(original._data, width) { }
        public Icon(Type type, string resource) : this(type.Assembly.GetManifestResourceStream(type, resource)) { }
        private Icon(byte[] data, int size)
        {
            _data = data;
            _bitmap = IcoDecoder.Decode(data, size) ?? SKBitmap.Decode(data) ?? new SKBitmap(16, 16);
        }

        private static byte[] ReadAll(Stream stream)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
        private Icon(Runtime.Serialization.SerializationInfo info, Runtime.Serialization.StreamingContext context)
        {
            int size = 32;
            foreach (Runtime.Serialization.SerializationEntry entry in info)
            {
                if (entry.Name == "IconData") _data = (byte[])entry.Value;
                else if (entry.Name == "IconSize" && entry.Value is Size s) size = s.Width;
            }
            _data = _data ?? new byte[0];
            _bitmap = IcoDecoder.Decode(_data, size) ?? SKBitmap.Decode(_data) ?? new SKBitmap(16, 16);
        }
        public void GetObjectData(Runtime.Serialization.SerializationInfo info, Runtime.Serialization.StreamingContext context)
        {
            info.AddValue("IconData", _data, typeof(byte[]));
            info.AddValue("IconSize", Size, typeof(Size));
        }
        internal byte[] Data { get { return _data; } }
        public Bitmap ToBitmap() { return new Bitmap(Bitmap.Decode(Encode())); }
        private byte[] Encode()
        {
            using (SKImage image = SKImage.FromBitmap(_bitmap))
            using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100)) return data.ToArray();
        }
        public void Save(Stream outputStream) { outputStream.Write(_data, 0, _data.Length); }
        public static Icon ExtractAssociatedIcon(string filePath) { return null; }
        public static Icon FromHandle(IntPtr handle) { return null; }
        public object Clone() { return new Icon(_data, Width); }
        public void Dispose() { }
    }

    // Fonts
    public sealed class FontFamily : MarshalByRefObject, IDisposable
    {
        public string Name { get; }
        public FontFamily(string name) { Name = name; }
        public FontFamily(Text.GenericFontFamilies genericFamily)
        {
            Name = genericFamily == Text.GenericFontFamilies.Monospace ? "Menlo" :
                   genericFamily == Text.GenericFontFamilies.Serif ? "Times New Roman" : "Microsoft Sans Serif";
        }
        public static FontFamily GenericSansSerif { get { return new FontFamily("Microsoft Sans Serif"); } }
        public static FontFamily GenericSerif { get { return new FontFamily("Times New Roman"); } }
        public static FontFamily GenericMonospace { get { return new FontFamily("Courier New"); } }
        public static FontFamily[] Families
        {
            get
            {
                List<FontFamily> families = new List<FontFamily>();
                foreach (string name in SKFontManager.Default.FontFamilies) families.Add(new FontFamily(name));
                return families.ToArray();
            }
        }
        public bool IsStyleAvailable(FontStyle style) { return true; }
        public override string ToString() { return "[FontFamily: Name=" + Name + "]"; }
        public override bool Equals(object obj) { return obj is FontFamily f && f.Name == Name; }
        public override int GetHashCode() { return Name.GetHashCode(); }
        public void Dispose() { }
    }

    [Serializable]
    [TypeConverter(typeof(FontConverter))]
    public sealed class Font : MarshalByRefObject, ICloneable, IDisposable, Runtime.Serialization.ISerializable
    {
        private readonly string _name;
        private readonly float _size;
        private readonly FontStyle _style;
        private readonly GraphicsUnit _unit;
        [NonSerialized] private SKTypeface _typeface;

        public string Name { get { return _name; } }
        public string OriginalFontName { get { return _name; } }
        public FontFamily FontFamily { get { return new FontFamily(_name); } }
        public float Size { get { return _size; } }
        public float SizeInPoints
        {
            get
            {
                switch (_unit)
                {
                    case GraphicsUnit.Pixel: return _size * 72f / 96f;
                    case GraphicsUnit.Inch: return _size * 72f;
                    case GraphicsUnit.Millimeter: return _size * 72f / 25.4f;
                    case GraphicsUnit.Document: return _size * 72f / 300f;
                    default: return _size;
                }
            }
        }
        // Size of the font in device independent pixels (96 per inch)
        public float SizeInPixels { get { return SizeInPoints * 96f / 72f; } }
        public FontStyle Style { get { return _style; } }
        public GraphicsUnit Unit { get { return _unit; } }
        public bool Bold { get { return (_style & FontStyle.Bold) != 0; } }
        public bool Italic { get { return (_style & FontStyle.Italic) != 0; } }
        public bool Underline { get { return (_style & FontStyle.Underline) != 0; } }
        public bool Strikeout { get { return (_style & FontStyle.Strikeout) != 0; } }
        public byte GdiCharSet { get { return 1; } }
        public bool GdiVerticalFont { get { return false; } }
        public bool IsSystemFont { get { return false; } }
        public int Height { get { return (int)Math.Ceiling(GetHeight()); } }

        public Font(string familyName, float emSize) : this(familyName, emSize, FontStyle.Regular, GraphicsUnit.Point) { }
        public Font(string familyName, float emSize, FontStyle style) : this(familyName, emSize, style, GraphicsUnit.Point) { }
        public Font(string familyName, float emSize, GraphicsUnit unit) : this(familyName, emSize, FontStyle.Regular, unit) { }
        public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet)
            : this(familyName, emSize, style, unit) { }
        public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet, bool gdiVerticalFont)
            : this(familyName, emSize, style, unit) { }
        public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit)
        {
            _name = string.IsNullOrEmpty(familyName) ? "Microsoft Sans Serif" : familyName;
            _size = emSize;
            _style = style;
            _unit = unit;
        }
        public Font(FontFamily family, float emSize) : this(family.Name, emSize) { }
        public Font(FontFamily family, float emSize, FontStyle style) : this(family.Name, emSize, style) { }
        public Font(FontFamily family, float emSize, GraphicsUnit unit) : this(family.Name, emSize, unit) { }
        public Font(FontFamily family, float emSize, FontStyle style, GraphicsUnit unit) : this(family.Name, emSize, style, unit) { }
        public Font(FontFamily family, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet)
            : this(family.Name, emSize, style, unit) { }
        public Font(Font prototype, FontStyle newStyle) : this(prototype._name, prototype._size, newStyle, prototype._unit) { }
        private Font(Runtime.Serialization.SerializationInfo info, Runtime.Serialization.StreamingContext context)
        {
            _name = info.GetString("Name");
            _size = info.GetSingle("Size");
            _style = (FontStyle)info.GetValue("Style", typeof(FontStyle));
            _unit = (GraphicsUnit)info.GetValue("Unit", typeof(GraphicsUnit));
        }

        // The families of Windows map to the fonts of macOS
        public static string MapFamily(string name)
        {
            switch ((name ?? "").ToLowerInvariant())
            {
                case "microsoft sans serif":
                case "segoe ui":
                case "tahoma":
                case "arial":
                case "verdana":
                case "calibri":
                case "ms sans serif":
                case "sans serif":
                    return null;    // the user interface font of macOS
                case "consolas":
                case "courier new":
                case "lucida console":
                case "cascadia mono":
                case "cascadia code":
                    return "Menlo";
                default:
                    return name;
            }
        }
        public SKTypeface Typeface
        {
            get
            {
                if (_typeface == null)
                {
                    SKFontStyle style = new SKFontStyle(Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                                                        SKFontStyleWidth.Normal,
                                                        Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
                    string family = MapFamily(_name);
                    _typeface = family == null ? SKFontManager.Default.MatchFamily(".AppleSystemUIFont", style) : null;
                    if (_typeface == null && family != null) _typeface = SKTypeface.FromFamilyName(family, style);
                    if (_typeface == null) _typeface = SKTypeface.FromFamilyName("Helvetica Neue", style) ?? SKTypeface.Default;
                }
                return _typeface;
            }
        }
        public SKFont ToSKFont()
        {
            SKFont font = new SKFont(Typeface, SizeInPixels);
            font.Subpixel = true;
            font.Edging = SKFontEdging.SubpixelAntialias;
            return font;
        }
        public float GetHeight()
        {
            using (SKFont font = ToSKFont())
            {
                SKFontMetrics m = font.Metrics;
                return -m.Ascent + m.Descent + m.Leading;
            }
        }
        public float GetHeight(Graphics graphics) { return GetHeight(); }
        public float GetHeight(float dpi) { return GetHeight() * dpi / 96f; }
        public IntPtr ToHfont() { return IntPtr.Zero; }
        public static Font FromHfont(IntPtr hfont) { return SystemFonts.DefaultFont; }
        public object Clone() { return new Font(_name, _size, _style, _unit); }
        public void Dispose() { }
        public void GetObjectData(Runtime.Serialization.SerializationInfo info, Runtime.Serialization.StreamingContext context)
        {
            info.AddValue("Name", _name);
            info.AddValue("Size", _size);
            info.AddValue("Style", _style);
            info.AddValue("Unit", _unit);
        }
        public override bool Equals(object obj)
        {
            return obj is Font f && f._name == _name && f._size == _size && f._style == _style && f._unit == _unit;
        }
        public override int GetHashCode() { return _name.GetHashCode() ^ _size.GetHashCode() ^ (int)_style; }
        public override string ToString()
        {
            return "[Font: Name=" + _name + ", Size=" + _size + ", Units=" + (int)_unit + ", GdiCharSet=1, GdiVerticalFont=False]";
        }
    }

    public class ImageConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(byte[]) || sourceType == typeof(Icon) || base.CanConvertFrom(context, sourceType);
        }
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(byte[]) || base.CanConvertTo(context, destinationType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, Globalization.CultureInfo culture, object value)
        {
            if (value is byte[] bytes) return new Bitmap(new MemoryStream(StripOleHeader(bytes)));
            if (value is Icon icon) return icon.ToBitmap();
            return base.ConvertFrom(context, culture, value);
        }
        public override object ConvertTo(ITypeDescriptorContext context, Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(byte[]) && value is Image image)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    image.Save(stream, ImageFormat.Png);
                    return stream.ToArray();
                }
            }
            if (destinationType == typeof(string)) return value == null ? "(none)" : value.GetType().Name;
            return base.ConvertTo(context, culture, value, destinationType);
        }
        // Images from OLE objects start with a header before the image file
        private static byte[] StripOleHeader(byte[] bytes)
        {
            if (bytes.Length > 78 && bytes[0] == 0x15 && bytes[1] == 0x1C)
            {
                byte[] stripped = new byte[bytes.Length - 78];
                Array.Copy(bytes, 78, stripped, 0, stripped.Length);
                return stripped;
            }
            return bytes;
        }
    }

    public class IconConverter : ExpandableObjectConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(byte[]) || base.CanConvertFrom(context, sourceType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, Globalization.CultureInfo culture, object value)
        {
            if (value is byte[] bytes) return new Icon(new MemoryStream(bytes));
            return base.ConvertFrom(context, culture, value);
        }
        public override object ConvertTo(ITypeDescriptorContext context, Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(byte[]) && value is Icon icon) return icon.Data;
            if (destinationType == typeof(string)) return value == null ? "(none)" : "(Icon)";
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public class FontConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, Globalization.CultureInfo culture, object value)
        {
            if (value is string s)
            {
                string[] parts = s.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                string name = parts.Length > 0 ? parts[0].Trim() : "Microsoft Sans Serif";
                float size = 8.25f;
                if (parts.Length > 1)
                    float.TryParse(parts[1].Trim().Replace("pt", ""), Globalization.NumberStyles.Float,
                                   Globalization.CultureInfo.InvariantCulture, out size);
                FontStyle style = FontStyle.Regular;
                for (int i = 2; i < parts.Length; i++)
                {
                    string p = parts[i].Trim().Replace("style=", "");
                    if (Enum.TryParse(p, true, out FontStyle st)) style |= st;
                }
                return new Font(name, size, style);
            }
            return base.ConvertFrom(context, culture, value);
        }
        public override object ConvertTo(ITypeDescriptorContext context, Globalization.CultureInfo culture, object value,
                                         Type destinationType)
        {
            if (destinationType == typeof(string) && value is Font f)
            {
                string text = f.Name + "; " + f.SizeInPoints.ToString(Globalization.CultureInfo.InvariantCulture) + "pt";
                if (f.Style != FontStyle.Regular) text += "; style=" + f.Style;
                return text;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public static class SystemFonts
    {
        public static Font DefaultFont { get { return new Font("Microsoft Sans Serif", 8.25f); } }
        public static Font DialogFont { get { return new Font("Tahoma", 8f); } }
        public static Font MessageBoxFont { get { return new Font("Segoe UI", 9f); } }
        public static Font MenuFont { get { return new Font("Segoe UI", 9f); } }
        public static Font StatusFont { get { return new Font("Segoe UI", 9f); } }
        public static Font CaptionFont { get { return new Font("Segoe UI", 9f); } }
        public static Font SmallCaptionFont { get { return new Font("Segoe UI", 9f); } }
        public static Font IconTitleFont { get { return new Font("Segoe UI", 9f); } }
        public static Font GetFontByName(string systemFontName) { return DefaultFont; }
    }

    // Brushes and pens
    public abstract class Brush : MarshalByRefObject, ICloneable, IDisposable
    {
        internal abstract void Apply(SKPaint paint, RectangleF bounds);
        public abstract object Clone();
        public void Dispose() { Dispose(true); }
        protected virtual void Dispose(bool disposing) { }
    }

    public sealed class SolidBrush : Brush
    {
        public Color Color { get; set; }
        public SolidBrush(Color color) { Color = color; }
        internal override void Apply(SKPaint paint, RectangleF bounds) { paint.Color = Color.ToSK(); paint.Shader = null; }
        public override object Clone() { return new SolidBrush(Color); }
    }

    public sealed class TextureBrush : Brush
    {
        public Image Image { get; }
        public TextureBrush(Image image) { Image = image; }
        internal override void Apply(SKPaint paint, RectangleF bounds)
        {
            paint.Color = SKColors.Black;
            paint.Shader = SKShader.CreateBitmap(Image.SKBitmap, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);
        }
        public override object Clone() { return new TextureBrush(Image); }
    }

    public static class Brushes
    {
        private static readonly Dictionary<KnownColor, Brush> _brushes = new Dictionary<KnownColor, Brush>();
        private static Brush Get(KnownColor c)
        {
            lock (_brushes)
            {
                if (!_brushes.TryGetValue(c, out Brush b)) _brushes[c] = b = new SolidBrush(Color.FromKnownColor(c));
                return b;
            }
        }
        public static Brush Transparent { get { return Get(KnownColor.Transparent); } }
        public static Brush Black { get { return Get(KnownColor.Black); } }
        public static Brush White { get { return Get(KnownColor.White); } }
        public static Brush Red { get { return Get(KnownColor.Red); } }
        public static Brush Green { get { return Get(KnownColor.Green); } }
        public static Brush Blue { get { return Get(KnownColor.Blue); } }
        public static Brush Gray { get { return Get(KnownColor.Gray); } }
        public static Brush DarkGray { get { return Get(KnownColor.DarkGray); } }
        public static Brush LightGray { get { return Get(KnownColor.LightGray); } }
        public static Brush DimGray { get { return Get(KnownColor.DimGray); } }
        public static Brush Silver { get { return Get(KnownColor.Silver); } }
        public static Brush DarkRed { get { return Get(KnownColor.DarkRed); } }
        public static Brush DarkGreen { get { return Get(KnownColor.DarkGreen); } }
        public static Brush DarkBlue { get { return Get(KnownColor.DarkBlue); } }
        public static Brush DarkCyan { get { return Get(KnownColor.DarkCyan); } }
        public static Brush Brown { get { return Get(KnownColor.Brown); } }
        public static Brush Orange { get { return Get(KnownColor.Orange); } }
        public static Brush Yellow { get { return Get(KnownColor.Yellow); } }
        public static Brush Cyan { get { return Get(KnownColor.Cyan); } }
        public static Brush Magenta { get { return Get(KnownColor.Magenta); } }
        public static Brush Navy { get { return Get(KnownColor.Navy); } }
        public static Brush WhiteSmoke { get { return Get(KnownColor.WhiteSmoke); } }
        public static Brush Gainsboro { get { return Get(KnownColor.Gainsboro); } }
        public static Brush LightBlue { get { return Get(KnownColor.LightBlue); } }
        public static Brush LightYellow { get { return Get(KnownColor.LightYellow); } }
        public static Brush SteelBlue { get { return Get(KnownColor.SteelBlue); } }
        public static Brush Purple { get { return Get(KnownColor.Purple); } }
        public static Brush Olive { get { return Get(KnownColor.Olive); } }
        public static Brush Teal { get { return Get(KnownColor.Teal); } }
        public static Brush Maroon { get { return Get(KnownColor.Maroon); } }
    }

    public static class SystemBrushes
    {
        public static Brush FromSystemColor(Color c) { return new SolidBrush(c); }
        public static Brush Control { get { return new SolidBrush(SystemColors.Control); } }
        public static Brush ControlText { get { return new SolidBrush(SystemColors.ControlText); } }
        public static Brush ControlDark { get { return new SolidBrush(SystemColors.ControlDark); } }
        public static Brush ControlLight { get { return new SolidBrush(SystemColors.ControlLight); } }
        public static Brush Window { get { return new SolidBrush(SystemColors.Window); } }
        public static Brush WindowText { get { return new SolidBrush(SystemColors.WindowText); } }
        public static Brush Highlight { get { return new SolidBrush(SystemColors.Highlight); } }
        public static Brush HighlightText { get { return new SolidBrush(SystemColors.HighlightText); } }
        public static Brush GrayText { get { return new SolidBrush(SystemColors.GrayText); } }
        public static Brush Info { get { return new SolidBrush(SystemColors.Info); } }
        public static Brush InfoText { get { return new SolidBrush(SystemColors.InfoText); } }
        public static Brush ButtonFace { get { return new SolidBrush(SystemColors.ButtonFace); } }
    }

    public sealed class Pen : MarshalByRefObject, ICloneable, IDisposable
    {
        public Color Color { get; set; }
        public float Width { get; set; }
        public Brush Brush { get; set; }
        public DashStyle DashStyle { get; set; } = DashStyle.Solid;
        public float[] DashPattern { get; set; }
        public float DashOffset { get; set; }
        public LineCap StartCap { get; set; } = LineCap.Flat;
        public LineCap EndCap { get; set; } = LineCap.Flat;
        public LineJoin LineJoin { get; set; } = LineJoin.Miter;
        public PenAlignment Alignment { get; set; } = PenAlignment.Center;
        public DashCap DashCap { get; set; }
        public CustomLineCap CustomEndCap { get; set; }
        public CustomLineCap CustomStartCap { get; set; }

        public Pen(Color color) : this(color, 1) { }
        public Pen(Color color, float width) { Color = color; Width = width; }
        public Pen(Brush brush) : this(brush, 1) { }
        public Pen(Brush brush, float width)
        {
            Brush = brush;
            Color = brush is SolidBrush sb ? sb.Color : Color.Black;
            Width = width;
        }

        internal void Apply(SKPaint paint)
        {
            paint.Style = SKPaintStyle.Stroke;
            paint.Color = Color.ToSK();
            paint.StrokeWidth = Width <= 1 ? 1 : Width;
            paint.StrokeCap = StartCap == LineCap.Round || EndCap == LineCap.Round ? SKStrokeCap.Round :
                              StartCap == LineCap.Square || EndCap == LineCap.Square ? SKStrokeCap.Square : SKStrokeCap.Butt;
            paint.StrokeJoin = LineJoin == LineJoin.Round ? SKStrokeJoin.Round : LineJoin == LineJoin.Bevel ? SKStrokeJoin.Bevel :
                               SKStrokeJoin.Miter;
            float w = Math.Max(1, Width);
            float[] intervals = null;
            switch (DashStyle)
            {
                case DashStyle.Dash: intervals = new[] { 3 * w, w }; break;
                case DashStyle.Dot: intervals = new[] { w, w }; break;
                case DashStyle.DashDot: intervals = new[] { 3 * w, w, w, w }; break;
                case DashStyle.DashDotDot: intervals = new[] { 3 * w, w, w, w, w, w }; break;
                case DashStyle.Custom:
                    if (DashPattern != null && DashPattern.Length >= 2)
                    {
                        intervals = new float[DashPattern.Length % 2 == 0 ? DashPattern.Length : DashPattern.Length * 2];
                        for (int i = 0; i < intervals.Length; i++) intervals[i] = DashPattern[i % DashPattern.Length] * w;
                    }
                    break;
            }
            paint.PathEffect = intervals == null ? null : SKPathEffect.CreateDash(intervals, DashOffset * w);
        }
        public object Clone()
        {
            Pen pen = (Pen)MemberwiseClone();
            return pen;
        }
        public void Dispose() { }
    }

    public static class Pens
    {
        public static Pen Black { get { return new Pen(Color.Black); } }
        public static Pen White { get { return new Pen(Color.White); } }
        public static Pen Red { get { return new Pen(Color.Red); } }
        public static Pen Green { get { return new Pen(Color.Green); } }
        public static Pen Blue { get { return new Pen(Color.Blue); } }
        public static Pen Gray { get { return new Pen(Color.Gray); } }
        public static Pen DarkGray { get { return new Pen(Color.DarkGray); } }
        public static Pen LightGray { get { return new Pen(Color.LightGray); } }
        public static Pen Silver { get { return new Pen(Color.Silver); } }
        public static Pen DimGray { get { return new Pen(Color.DimGray); } }
        public static Pen Orange { get { return new Pen(Color.Orange); } }
        public static Pen Transparent { get { return new Pen(Color.Transparent); } }
    }

    public static class SystemPens
    {
        public static Pen FromSystemColor(Color c) { return new Pen(c); }
        public static Pen Control { get { return new Pen(SystemColors.Control); } }
        public static Pen ControlDark { get { return new Pen(SystemColors.ControlDark); } }
        public static Pen Window { get { return new Pen(SystemColors.Window); } }
        public static Pen WindowText { get { return new Pen(SystemColors.WindowText); } }
        public static Pen ButtonShadow { get { return new Pen(SystemColors.ButtonShadow); } }
        public static Pen ControlDarkDark { get { return new Pen(SystemColors.ControlDarkDark); } }
        public static Pen ControlLight { get { return new Pen(SystemColors.ControlLight); } }
        public static Pen ControlText { get { return new Pen(SystemColors.ControlText); } }
        public static Pen WindowFrame { get { return new Pen(SystemColors.WindowFrame); } }
        public static Pen Highlight { get { return new Pen(SystemColors.Highlight); } }
        public static Pen GrayText { get { return new Pen(SystemColors.GrayText); } }
    }

    // Text layout
    public sealed class StringFormat : MarshalByRefObject, ICloneable, IDisposable
    {
        public StringAlignment Alignment { get; set; }
        public StringAlignment LineAlignment { get; set; }
        public StringFormatFlags FormatFlags { get; set; }
        public StringTrimming Trimming { get; set; } = StringTrimming.Character;
        public Text.HotkeyPrefix HotkeyPrefix { get; set; }

        public StringFormat() { }
        public StringFormat(StringFormatFlags options) { FormatFlags = options; }
        public StringFormat(StringFormat format)
        {
            Alignment = format.Alignment;
            LineAlignment = format.LineAlignment;
            FormatFlags = format.FormatFlags;
            Trimming = format.Trimming;
        }
        public static StringFormat GenericDefault { get { return new StringFormat(); } }
        public static StringFormat GenericTypographic
        {
            get { return new StringFormat { FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.LineLimit, Trimming = StringTrimming.None }; }
        }
        public void SetTabStops(float firstTabOffset, float[] tabStops) { }
        public object Clone() { return new StringFormat(this); }
        public void Dispose() { }
    }

    public sealed class Region : MarshalByRefObject, IDisposable
    {
        internal RectangleF Bounds;
        public Region() { Bounds = new RectangleF(-4194304, -4194304, 8388608, 8388608); }
        public Region(Rectangle rect) { Bounds = rect; }
        public Region(RectangleF rect) { Bounds = rect; }
        public Region(GraphicsPath path) { Bounds = path.GetBounds(); }
        public RectangleF GetBounds(Graphics g) { return Bounds; }
        public void Intersect(Rectangle rect) { Bounds = RectangleF.Intersect(Bounds, rect); }
        public void Intersect(RectangleF rect) { Bounds = RectangleF.Intersect(Bounds, rect); }
        public void Union(Rectangle rect) { Bounds = RectangleF.Union(Bounds, rect); }
        public void Exclude(Rectangle rect) { }
        public bool IsVisible(Point point) { return Bounds.Contains(point); }
        public bool IsVisible(Rectangle rect) { return Bounds.IntersectsWith(rect); }
        public Region Clone() { return new Region(Bounds); }
        public void Dispose() { }
    }

    public sealed class Graphics : MarshalByRefObject, IDisposable, IDeviceContext
    {
        private readonly SKCanvas _canvas;
        private readonly bool _ownsCanvas;
        private readonly Image _image;
        private readonly Stack<int> _saveCounts = new Stack<int>();
        private RectangleF _clip;

        public SmoothingMode SmoothingMode { get; set; } = SmoothingMode.None;
        public Text.TextRenderingHint TextRenderingHint { get; set; }
        public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.Bilinear;
        public PixelOffsetMode PixelOffsetMode { get; set; }
        public CompositingQuality CompositingQuality { get; set; }
        public CompositingMode CompositingMode { get; set; }
        public GraphicsUnit PageUnit { get; set; } = GraphicsUnit.Display;
        public float PageScale { get; set; } = 1;
        public float DpiX { get { return 96; } }
        public float DpiY { get { return 96; } }
        public int TextContrast { get; set; }
        public SKCanvas Canvas { get { return _canvas; } }
        public RectangleF ClipBounds { get { return _clip; } }
        public RectangleF VisibleClipBounds { get { return _clip; } }
        public Region Clip
        {
            get { return new Region(_clip); }
            set { SetClip(value.Bounds); }
        }
        public bool IsClipEmpty { get { return _clip.Width <= 0 || _clip.Height <= 0; } }
        public Matrix Transform
        {
            get
            {
                SKMatrix m = _canvas.TotalMatrix;
                return new Matrix(m.ScaleX, m.SkewY, m.SkewX, m.ScaleY, m.TransX, m.TransY);
            }
            set
            {
                float[] e = value.Elements;
                _canvas.SetMatrix(new SKMatrix(e[0], e[2], e[4], e[1], e[3], e[5], 0, 0, 1));
            }
        }

        // A graphics on a canvas of a control or an image; size is the area that is painted
        public Graphics(SKCanvas canvas, SizeF size) : this(canvas, size, false, null) { }
        private int _baseSaveCount;
        private bool _disposedGraphics;
        private Graphics(SKCanvas canvas, SizeF size, bool ownsCanvas, Image image)
        {
            _canvas = canvas;
            _ownsCanvas = ownsCanvas;
            _image = image;
            _clip = new RectangleF(0, 0, size.Width, size.Height);
            // SetClip restores to this state before it clips
            _baseSaveCount = canvas.Save();
            canvas.Save();
        }

        // A graphics that draws on a canvas and calls the action when it is disposed
        public static Graphics FromCanvas(SKCanvas canvas, SizeF size, Action disposed)
        {
            Graphics g = new Graphics(canvas, size, false, null);
            g.Disposed = disposed;
            return g;
        }
        public static Graphics FromImage(Image image)
        {
            return new Graphics(new SKCanvas(image.SKBitmap), new SizeF(image.Width, image.Height), true, image);
        }
        public static Graphics FromHwnd(IntPtr hwnd)
        {
            Bitmap bitmap = new Bitmap(1, 1);
            return FromImage(bitmap);
        }
        public static Graphics FromHdc(IntPtr hdc) { return FromHwnd(IntPtr.Zero); }
        public IntPtr GetHdc() { return IntPtr.Zero; }
        public void ReleaseHdc(IntPtr hdc) { }
        public void ReleaseHdc() { }

        private SKPaint Paint(Brush brush, RectangleF bounds)
        {
            SKPaint paint = new SKPaint { IsAntialias = SmoothingMode == SmoothingMode.AntiAlias || SmoothingMode == SmoothingMode.HighQuality, Style = SKPaintStyle.Fill };
            brush.Apply(paint, bounds);
            return paint;
        }
        private SKPaint Paint(Pen pen)
        {
            SKPaint paint = new SKPaint { IsAntialias = SmoothingMode == SmoothingMode.AntiAlias || SmoothingMode == SmoothingMode.HighQuality };
            pen.Apply(paint);
            return paint;
        }
        // GDI+ draws the lines of width 1 through the pixel centers
        private static float H(Pen pen) { return pen.Width <= 1 ? 0.5f : 0; }

        // Lines
        public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
        {
            float h = H(pen);
            using (SKPaint paint = Paint(pen)) _canvas.DrawLine(x1 + h, y1 + h, x2 + h, y2 + h, paint);
            Changed();
        }
        public void DrawLine(Pen pen, int x1, int y1, int x2, int y2) { DrawLine(pen, (float)x1, y1, x2, y2); }
        public void DrawLine(Pen pen, Point pt1, Point pt2) { DrawLine(pen, pt1.X, pt1.Y, pt2.X, pt2.Y); }
        public void DrawLine(Pen pen, PointF pt1, PointF pt2) { DrawLine(pen, pt1.X, pt1.Y, pt2.X, pt2.Y); }
        public void DrawLines(Pen pen, PointF[] points)
        {
            if (points.Length < 2) return;
            using (SKPath path = new SKPath())
            {
                float h = H(pen);
                path.MoveTo(points[0].X + h, points[0].Y + h);
                for (int i = 1; i < points.Length; i++) path.LineTo(points[i].X + h, points[i].Y + h);
                using (SKPaint paint = Paint(pen)) _canvas.DrawPath(path, paint);
            }
            Changed();
        }
        public void DrawLines(Pen pen, Point[] points) { DrawLines(pen, Array.ConvertAll(points, p => (PointF)p)); }
        public void DrawPolygon(Pen pen, PointF[] points)
        {
            if (points.Length < 2) return;
            using (SKPath path = Polygon(points, H(pen)))
            using (SKPaint paint = Paint(pen)) _canvas.DrawPath(path, paint);
            Changed();
        }
        public void DrawPolygon(Pen pen, Point[] points) { DrawPolygon(pen, Array.ConvertAll(points, p => (PointF)p)); }
        public void FillPolygon(Brush brush, PointF[] points)
        {
            if (points.Length < 3) return;
            using (SKPath path = Polygon(points, 0))
            using (SKPaint paint = Paint(brush, path.Bounds.ToRectangleF())) _canvas.DrawPath(path, paint);
            Changed();
        }
        public void FillPolygon(Brush brush, Point[] points) { FillPolygon(brush, Array.ConvertAll(points, p => (PointF)p)); }
        public void FillPolygon(Brush brush, PointF[] points, FillMode fillMode) { FillPolygon(brush, points); }
        public void FillPolygon(Brush brush, Point[] points, FillMode fillMode) { FillPolygon(brush, points); }
        private static SKPath Polygon(PointF[] points, float h)
        {
            SKPath path = new SKPath();
            path.MoveTo(points[0].X + h, points[0].Y + h);
            for (int i = 1; i < points.Length; i++) path.LineTo(points[i].X + h, points[i].Y + h);
            path.Close();
            return path;
        }
        public void DrawBezier(Pen pen, PointF p1, PointF p2, PointF p3, PointF p4)
        {
            using (SKPath path = new SKPath())
            {
                path.MoveTo(p1.X, p1.Y);
                path.CubicTo(p2.X, p2.Y, p3.X, p3.Y, p4.X, p4.Y);
                using (SKPaint paint = Paint(pen)) _canvas.DrawPath(path, paint);
            }
            Changed();
        }
        public void DrawCurve(Pen pen, PointF[] points) { DrawLines(pen, points); }
        public void DrawCurve(Pen pen, Point[] points) { DrawLines(pen, points); }

        // Rectangles and ellipses
        public void DrawRectangle(Pen pen, float x, float y, float width, float height)
        {
            float h = H(pen);
            using (SKPaint paint = Paint(pen)) _canvas.DrawRect(new SKRect(x + h, y + h, x + width + h, y + height + h), paint);
            Changed();
        }
        public void DrawRectangle(Pen pen, int x, int y, int width, int height) { DrawRectangle(pen, (float)x, y, width, height); }
        public void DrawRectangle(Pen pen, Rectangle rect) { DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height); }
        public void DrawRectangle(Pen pen, RectangleF rect) { DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height); }
        public void DrawRectangles(Pen pen, Rectangle[] rects) { foreach (var r in rects) DrawRectangle(pen, r); }
        public void DrawRectangles(Pen pen, RectangleF[] rects) { foreach (var r in rects) DrawRectangle(pen, r); }
        public void FillRectangle(Brush brush, float x, float y, float width, float height)
        {
            RectangleF r = new RectangleF(x, y, width, height);
            using (SKPaint paint = Paint(brush, r)) { paint.IsAntialias = false; _canvas.DrawRect(r.ToSK(), paint); }
            Changed();
        }
        public void FillRectangle(Brush brush, int x, int y, int width, int height) { FillRectangle(brush, (float)x, y, width, height); }
        public void FillRectangle(Brush brush, Rectangle rect) { FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height); }
        public void FillRectangle(Brush brush, RectangleF rect) { FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height); }
        public void FillRectangles(Brush brush, Rectangle[] rects) { foreach (var r in rects) FillRectangle(brush, r); }
        public void FillRectangles(Brush brush, RectangleF[] rects) { foreach (var r in rects) FillRectangle(brush, r); }
        public void DrawEllipse(Pen pen, float x, float y, float width, float height)
        {
            float h = H(pen);
            using (SKPaint paint = Paint(pen)) { paint.IsAntialias = true; _canvas.DrawOval(new SKRect(x + h, y + h, x + width + h, y + height + h), paint); }
            Changed();
        }
        public void DrawEllipse(Pen pen, int x, int y, int width, int height) { DrawEllipse(pen, (float)x, y, width, height); }
        public void DrawEllipse(Pen pen, Rectangle rect) { DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height); }
        public void DrawEllipse(Pen pen, RectangleF rect) { DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height); }
        public void FillEllipse(Brush brush, float x, float y, float width, float height)
        {
            RectangleF r = new RectangleF(x, y, width, height);
            using (SKPaint paint = Paint(brush, r)) { paint.IsAntialias = true; _canvas.DrawOval(r.ToSK(), paint); }
            Changed();
        }
        public void FillEllipse(Brush brush, int x, int y, int width, int height) { FillEllipse(brush, (float)x, y, width, height); }
        public void FillEllipse(Brush brush, Rectangle rect) { FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height); }
        public void FillEllipse(Brush brush, RectangleF rect) { FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height); }
        public void DrawArc(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            using (SKPath path = new SKPath())
            {
                path.AddArc(new SKRect(x, y, x + width, y + height), startAngle, sweepAngle);
                using (SKPaint paint = Paint(pen)) _canvas.DrawPath(path, paint);
            }
            Changed();
        }
        public void DrawArc(Pen pen, Rectangle rect, float startAngle, float sweepAngle)
        {
            DrawArc(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }
        public void DrawArc(Pen pen, RectangleF rect, float startAngle, float sweepAngle)
        {
            DrawArc(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }
        public void FillPie(Brush brush, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            using (SKPath path = new SKPath())
            {
                SKRect r = new SKRect(x, y, x + width, y + height);
                path.MoveTo(r.MidX, r.MidY);
                path.ArcTo(r, startAngle, sweepAngle, false);
                path.Close();
                using (SKPaint paint = Paint(brush, new RectangleF(x, y, width, height))) _canvas.DrawPath(path, paint);
            }
            Changed();
        }
        public void FillPie(Brush brush, Rectangle rect, float startAngle, float sweepAngle)
        {
            FillPie(brush, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }
        public void DrawPie(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            using (SKPath path = new SKPath())
            {
                SKRect r = new SKRect(x, y, x + width, y + height);
                path.MoveTo(r.MidX, r.MidY);
                path.ArcTo(r, startAngle, sweepAngle, false);
                path.Close();
                using (SKPaint paint = Paint(pen)) _canvas.DrawPath(path, paint);
            }
            Changed();
        }
        public void DrawPie(Pen pen, Rectangle rect, float startAngle, float sweepAngle)
        {
            DrawPie(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }
        public void DrawPath(Pen pen, GraphicsPath path)
        {
            using (SKPaint paint = Paint(pen)) _canvas.DrawPath(path.SKPath, paint);
            Changed();
        }
        public void FillPath(Brush brush, GraphicsPath path)
        {
            using (SKPaint paint = Paint(brush, path.GetBounds())) _canvas.DrawPath(path.SKPath, paint);
            Changed();
        }
        public void FillRegion(Brush brush, Region region) { FillRectangle(brush, region.Bounds); }
        public void Clear(Color color)
        {
            _canvas.Clear(color.ToSK());
            Changed();
        }

        // Text
        public void DrawString(string s, Font font, Brush brush, float x, float y)
        {
            DrawString(s, font, brush, new RectangleF(x, y, 0, 0), null);
        }
        public void DrawString(string s, Font font, Brush brush, PointF point) { DrawString(s, font, brush, point.X, point.Y); }
        public void DrawString(string s, Font font, Brush brush, float x, float y, StringFormat format)
        {
            DrawString(s, font, brush, new RectangleF(x, y, 0, 0), format);
        }
        public void DrawString(string s, Font font, Brush brush, PointF point, StringFormat format)
        {
            DrawString(s, font, brush, new RectangleF(point.X, point.Y, 0, 0), format);
        }
        public void DrawString(string s, Font font, Brush brush, RectangleF layoutRectangle)
        {
            DrawString(s, font, brush, layoutRectangle, null);
        }
        public void DrawString(string s, Font font, Brush brush, RectangleF layoutRectangle, StringFormat format)
        {
            if (string.IsNullOrEmpty(s)) return;
            using (SKFont skFont = font.ToSKFont())
            using (SKPaint paint = Paint(brush, layoutRectangle))
            {
                paint.IsAntialias = true;
                TextDrawing.Draw(_canvas, s, font, skFont, paint, layoutRectangle, format);
            }
            Changed();
        }
        public SizeF MeasureString(string text, Font font) { return MeasureString(text, font, new SizeF(0, 0), null); }
        public SizeF MeasureString(string text, Font font, int width) { return MeasureString(text, font, new SizeF(width, 0), null); }
        public SizeF MeasureString(string text, Font font, SizeF layoutArea) { return MeasureString(text, font, layoutArea, null); }
        public SizeF MeasureString(string text, Font font, int width, StringFormat format)
        {
            return MeasureString(text, font, new SizeF(width, 0), format);
        }
        public SizeF MeasureString(string text, Font font, PointF origin, StringFormat stringFormat)
        {
            return MeasureString(text, font, new SizeF(0, 0), stringFormat);
        }
        public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat stringFormat)
        {
            return TextDrawing.Measure(text, font, layoutArea.Width, stringFormat, true);
        }
        public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat stringFormat, out int charactersFitted,
                                   out int linesFilled)
        {
            SizeF size = MeasureString(text, font, layoutArea, stringFormat);
            charactersFitted = text?.Length ?? 0;
            linesFilled = Math.Max(1, (int)Math.Round(size.Height / Math.Max(1, font.GetHeight())));
            return size;
        }
        public Region[] MeasureCharacterRanges(string text, Font font, RectangleF layoutRect, StringFormat stringFormat)
        {
            return new Region[] { new Region(new RectangleF(layoutRect.Location, MeasureString(text, font))) };
        }

        // Images
        private SKPaint ImagePaint()
        {
            return new SKPaint
            {
                FilterQuality = InterpolationMode == InterpolationMode.NearestNeighbor ? SKFilterQuality.None : SKFilterQuality.High,
                IsAntialias = true
            };
        }
        public void DrawImage(Image image, float x, float y)
        {
            if (image?.SKBitmap == null) return;
            using (SKPaint paint = ImagePaint()) _canvas.DrawBitmap(image.SKBitmap, x, y, paint);
            Changed();
        }
        public void DrawImage(Image image, int x, int y) { DrawImage(image, (float)x, y); }
        public void DrawImage(Image image, Point point) { DrawImage(image, point.X, point.Y); }
        public void DrawImage(Image image, PointF point) { DrawImage(image, point.X, point.Y); }
        public void DrawImage(Image image, float x, float y, float width, float height)
        {
            DrawImage(image, new RectangleF(x, y, width, height));
        }
        public void DrawImage(Image image, int x, int y, int width, int height) { DrawImage(image, new RectangleF(x, y, width, height)); }
        public void DrawImage(Image image, Rectangle rect) { DrawImage(image, (RectangleF)rect); }
        public void DrawImage(Image image, RectangleF rect)
        {
            if (image?.SKBitmap == null) return;
            using (SKPaint paint = ImagePaint()) _canvas.DrawBitmap(image.SKBitmap, rect.ToSK(), paint);
            Changed();
        }
        public void DrawImage(Image image, Rectangle destRect, Rectangle srcRect, GraphicsUnit srcUnit)
        {
            DrawImage(image, (RectangleF)destRect, (RectangleF)srcRect, srcUnit);
        }
        public void DrawImage(Image image, RectangleF destRect, RectangleF srcRect, GraphicsUnit srcUnit)
        {
            if (image?.SKBitmap == null) return;
            using (SKPaint paint = ImagePaint()) _canvas.DrawBitmap(image.SKBitmap, srcRect.ToSK(), destRect.ToSK(), paint);
            Changed();
        }
        public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit)
        {
            DrawImage(image, destRect, new Rectangle(srcX, srcY, srcWidth, srcHeight), srcUnit);
        }
        public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit,
                              ImageAttributes imageAttr)
        {
            if (image?.SKBitmap == null) return;
            using (SKPaint paint = ImagePaint())
            {
                if (imageAttr?.ColorMatrix != null) paint.ColorFilter = SKColorFilter.CreateColorMatrix(imageAttr.ColorMatrix.ToSkia());
                _canvas.DrawBitmap(image.SKBitmap, new SKRect(srcX, srcY, srcX + srcWidth, srcY + srcHeight), destRect.ToSK(), paint);
            }
            Changed();
        }
        public void DrawImage(Image image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight,
                              GraphicsUnit srcUnit, ImageAttributes imageAttr)
        {
            DrawImage(image, destRect, (int)srcX, (int)srcY, (int)srcWidth, (int)srcHeight, srcUnit, imageAttr);
        }
        public void DrawImage(Image image, Rectangle rect, int x, int y, int w, int h, GraphicsUnit unit, ImageAttributes attr,
                              DrawImageAbort callback)
        {
            DrawImage(image, rect, x, y, w, h, unit, attr);
        }
        public delegate bool DrawImageAbort(IntPtr callbackdata);
        public void DrawImageUnscaled(Image image, int x, int y) { DrawImage(image, x, y); }
        public void DrawImageUnscaled(Image image, Point point) { DrawImage(image, point.X, point.Y); }
        public void DrawImageUnscaled(Image image, Rectangle rect) { DrawImage(image, rect.X, rect.Y); }
        public void DrawImageUnscaledAndClipped(Image image, Rectangle rect)
        {
            Save();
            SetClip(rect);
            DrawImage(image, rect.X, rect.Y);
            ResetClip();
        }
        public void DrawIcon(Icon icon, int x, int y) { using (Bitmap b = icon.ToBitmap()) DrawImage(b, x, y); }
        public void DrawIcon(Icon icon, Rectangle rect) { using (Bitmap b = icon.ToBitmap()) DrawImage(b, rect); }
        public void DrawIconUnstretched(Icon icon, Rectangle rect) { using (Bitmap b = icon.ToBitmap()) DrawImage(b, rect.X, rect.Y); }
        public void CopyFromScreen(int sourceX, int sourceY, int destinationX, int destinationY, Size blockRegionSize) { }
        public void CopyFromScreen(Point upperLeftSource, Point upperLeftDestination, Size blockRegionSize) { }
        public void CopyFromScreen(Point upperLeftSource, Point upperLeftDestination, Size blockRegionSize, CopyPixelOperation copyPixelOperation) { }

        // State
        public void SetClip(Rectangle rect) { SetClip((RectangleF)rect); }
        public void SetClip(RectangleF rect)
        {
            SKMatrix matrix = _canvas.TotalMatrix;
            _canvas.RestoreToCount(_baseSaveCount + 1);
            _canvas.Save();
            _canvas.SetMatrix(matrix);
            _canvas.ClipRect(rect.ToSK());
            _clip = rect;
        }
        public void SetClip(Rectangle rect, CombineMode combineMode) { SetClip(rect); }
        public void SetClip(RectangleF rect, CombineMode combineMode) { SetClip(rect); }
        public void SetClip(Region region, CombineMode combineMode) { SetClip(region.Bounds); }
        public void SetClip(GraphicsPath path)
        {
            _canvas.Save();
            _canvas.ClipPath(path.SKPath, SKClipOperation.Intersect, true);
            _clip = path.GetBounds();
        }
        public void SetClip(Graphics g) { SetClip(g._clip); }
        public void ResetClip()
        {
            SKMatrix matrix = _canvas.TotalMatrix;
            _canvas.RestoreToCount(_baseSaveCount + 1);
            _canvas.Save();
            _canvas.SetMatrix(matrix);
            _clip = new RectangleF(-4194304, -4194304, 8388608, 8388608);
        }
        public void IntersectClip(Rectangle rect) { _canvas.ClipRect(((RectangleF)rect).ToSK()); _clip = RectangleF.Intersect(_clip, rect); }
        public void IntersectClip(RectangleF rect) { _canvas.ClipRect(rect.ToSK()); _clip = RectangleF.Intersect(_clip, rect); }
        public void ExcludeClip(Rectangle rect) { _canvas.ClipRect(((RectangleF)rect).ToSK(), SKClipOperation.Difference); }
        public void TranslateTransform(float dx, float dy) { _canvas.Translate(dx, dy); }
        public void TranslateTransform(float dx, float dy, MatrixOrder order) { _canvas.Translate(dx, dy); }
        public void ScaleTransform(float sx, float sy) { _canvas.Scale(sx, sy); }
        public void ScaleTransform(float sx, float sy, MatrixOrder order) { _canvas.Scale(sx, sy); }
        public void RotateTransform(float angle) { _canvas.RotateDegrees(angle); }
        public void RotateTransform(float angle, MatrixOrder order) { _canvas.RotateDegrees(angle); }
        public void ResetTransform() { _canvas.ResetMatrix(); }
        public void MultiplyTransform(Matrix matrix)
        {
            float[] e = matrix.Elements;
            SKMatrix m = new SKMatrix(e[0], e[2], e[4], e[1], e[3], e[5], 0, 0, 1);
            _canvas.Concat(ref m);
        }
        public GraphicsState Save()
        {
            int count = _canvas.Save();
            return new GraphicsState(count, _clip);
        }
        public void Restore(GraphicsState gstate)
        {
            _canvas.RestoreToCount(gstate.Count);
            _clip = gstate.Clip;
        }
        public GraphicsContainer BeginContainer() { return new GraphicsContainer(Save()); }
        public void EndContainer(GraphicsContainer container) { Restore(container.State); }
        public void Flush() { _canvas.Flush(); }
        public void Flush(FlushIntention intention) { _canvas.Flush(); }
        public bool IsVisible(Rectangle rect) { return _clip.IntersectsWith(rect); }
        public bool IsVisible(Point point) { return _clip.Contains(point); }
        private void Changed() { _image?.Changed(); }
        // Called when the graphics is disposed (graphics of CreateGraphics records an overlay of a control)
        internal Action Disposed;
        public void Dispose()
        {
            if (_disposedGraphics) return;
            _disposedGraphics = true;
            _canvas.RestoreToCount(_baseSaveCount);
            Action disposed = Disposed;
            Disposed = null;
            disposed?.Invoke();
            if (_ownsCanvas)
            {
                _canvas.Flush();
                _canvas.Dispose();
            }
            _image?.Changed();
        }
    }

    public interface IDeviceContext : IDisposable
    {
        IntPtr GetHdc();
        void ReleaseHdc();
    }

    internal static class SKRectExtensions
    {
        public static RectangleF ToRectangleF(this SKRect r) { return new RectangleF(r.Left, r.Top, r.Width, r.Height); }
    }

    // Text layout of GDI+: lines are broken at the width of the layout rectangle (if any), aligned by the string format
    internal static class TextDrawing
    {
        public static float Width(SKFont font, string text)
        {
            using (SKPaint paint = new SKPaint { Typeface = font.Typeface, TextSize = font.Size, SubpixelText = true })
                return paint.MeasureText(text ?? string.Empty);
        }
        public static List<string> BreakLines(string text, SKFont font, float maxWidth, bool wrap)
        {
            List<string> lines = new List<string>();
            foreach (string paragraph in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                if (!wrap || maxWidth <= 0 || TextDrawing.Width(font, paragraph) <= maxWidth)
                {
                    lines.Add(paragraph);
                    continue;
                }
                string remaining = paragraph;
                while (remaining.Length > 0)
                {
                    int fit = remaining.Length;
                    while (fit > 1 && TextDrawing.Width(font, remaining.Substring(0, fit)) > maxWidth) fit--;
                    if (fit < remaining.Length)
                    {
                        int space = remaining.LastIndexOf(' ', Math.Max(0, fit - 1), fit);
                        if (space > 0) fit = space + 1;
                    }
                    lines.Add(remaining.Substring(0, fit).TrimEnd());
                    remaining = remaining.Substring(fit).TrimStart();
                }
            }
            return lines;
        }
        public static SizeF Measure(string text, Font font, float maxWidth, StringFormat format, bool gdiPlusPadding)
        {
            if (string.IsNullOrEmpty(text)) return new SizeF(0, font.GetHeight());
            bool wrap = format == null || (format.FormatFlags & StringFormatFlags.NoWrap) == 0;
            using (SKFont skFont = font.ToSKFont())
            {
                List<string> lines = BreakLines(text, skFont, maxWidth, wrap);
                float width = 0;
                foreach (string line in lines) width = Math.Max(width, TextDrawing.Width(skFont, line));
                float lineHeight = font.GetHeight();
                // GDI+ adds 1/6 em to the measured string
                if (gdiPlusPadding && (format == null || (format.FormatFlags & StringFormatFlags.NoClip) == 0))
                    width += font.SizeInPixels / 3f;
                return new SizeF((float)Math.Ceiling(width), lineHeight * lines.Count);
            }
        }
        public static void Draw(SKCanvas canvas, string s, Font font, SKFont skFont, SKPaint paint, RectangleF rect,
                                StringFormat format)
        {
            bool wrap = format == null || (format.FormatFlags & StringFormatFlags.NoWrap) == 0;
            List<string> lines = BreakLines(s, skFont, rect.Width, wrap);
            SKFontMetrics m = skFont.Metrics;
            float lineHeight = -m.Ascent + m.Descent + m.Leading;
            float totalHeight = lineHeight * lines.Count;
            float y = rect.Y;
            if (rect.Height > 0 && format != null)
            {
                if (format.LineAlignment == StringAlignment.Center) y = rect.Y + (rect.Height - totalHeight) / 2;
                else if (format.LineAlignment == StringAlignment.Far) y = rect.Bottom - totalHeight;
            }
            bool clip = rect.Width > 0 && rect.Height > 0 && (format == null || (format.FormatFlags & StringFormatFlags.NoClip) == 0);
            if (clip)
            {
                canvas.Save();
                canvas.ClipRect(rect.ToSK());
            }
            foreach (string line in lines)
            {
                string text = line;
                float w = TextDrawing.Width(skFont, text);
                if (rect.Width > 0 && format != null && format.Trimming >= StringTrimming.EllipsisCharacter && w > rect.Width)
                {
                    while (text.Length > 0 && TextDrawing.Width(skFont, text + "…") > rect.Width) text = text.Substring(0, text.Length - 1);
                    text += "…";
                    w = TextDrawing.Width(skFont, text);
                }
                float x = rect.X + font.SizeInPixels / 6f;
                if (format != null && rect.Width > 0)
                {
                    if (format.Alignment == StringAlignment.Center) x = rect.X + (rect.Width - w) / 2;
                    else if (format.Alignment == StringAlignment.Far) x = rect.Right - w - font.SizeInPixels / 6f;
                }
                else if (format != null && rect.Width == 0)
                {
                    if (format.Alignment == StringAlignment.Center) x = rect.X - w / 2;
                    else if (format.Alignment == StringAlignment.Far) x = rect.X - w;
                }
                canvas.DrawText(text, x, y - m.Ascent, skFont, paint);
                if (font.Underline)
                {
                    float uy = y - m.Ascent + Math.Max(1, m.UnderlinePosition ?? 1);
                    canvas.DrawLine(x, uy, x + w, uy, paint);
                }
                if (font.Strikeout)
                {
                    float sy = y - m.Ascent * 0.65f;
                    canvas.DrawLine(x, sy, x + w, sy, paint);
                }
                y += lineHeight;
            }
            if (clip) canvas.Restore();
        }
    }
}

namespace System.Drawing.Text
{
    public enum TextRenderingHint
    {
        SystemDefault = 0, SingleBitPerPixelGridFit = 1, SingleBitPerPixel = 2, AntiAliasGridFit = 3, AntiAlias = 4,
        ClearTypeGridFit = 5
    }

    public enum HotkeyPrefix { None = 0, Show = 1, Hide = 2 }

    public enum GenericFontFamilies { Serif = 0, SansSerif = 1, Monospace = 2 }

    public class InstalledFontCollection : IDisposable
    {
        public FontFamily[] Families { get { return FontFamily.Families; } }
        public void Dispose() { }
    }

    public sealed class PrivateFontCollection : IDisposable
    {
        private readonly List<FontFamily> _families = new List<FontFamily>();
        public FontFamily[] Families { get { return _families.ToArray(); } }
        public void AddFontFile(string filename) { }
        public void AddMemoryFont(IntPtr memory, int length) { }
        public void Dispose() { }
    }
}
