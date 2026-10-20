// PrePoMax for macOS - ImageList and the ImageListStreamer of the form resources (the Win32 image list format).

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using SkiaSharp;

namespace System.Windows.Forms
{
    public enum ColorDepth { Depth4Bit = 4, Depth8Bit = 8, Depth16Bit = 16, Depth24Bit = 24, Depth32Bit = 32 }

    public sealed class ImageList : Component
    {
        private readonly ImageCollection _images;
        private Size _imageSize = new Size(16, 16);

        public ImageList() { _images = new ImageCollection(this); }
        public ImageList(IContainer container) : this() { container?.Add(this); }
        public ImageCollection Images { get { return _images; } }
        public Size ImageSize { get { return _imageSize; } set { _imageSize = value; } }
        public ColorDepth ColorDepth { get; set; } = ColorDepth.Depth8Bit;
        public Color TransparentColor { get; set; } = Color.Transparent;
        public object Tag { get; set; }
        public IntPtr Handle { get { return IntPtr.Zero; } }
        public bool HandleCreated { get { return true; } }
        public event EventHandler RecreateHandle;
        public ImageListStreamer ImageStream
        {
            get { return new ImageListStreamer(this); }
            set
            {
                _images.Clear();
                if (value == null) return;
                if (value.ImageSize.Width > 0) _imageSize = value.ImageSize;
                foreach (Bitmap b in value.Images) _images.Add(b);
            }
        }
        public void Draw(Graphics g, Point pt, int index) { g.DrawImage(_images[index], pt.X, pt.Y); }
        public void Draw(Graphics g, int x, int y, int index) { g.DrawImage(_images[index], x, y); }
        public void Draw(Graphics g, int x, int y, int width, int height, int index) { g.DrawImage(_images[index], x, y, width, height); }
        public override string ToString() { return base.ToString() + " Images.Count: " + _images.Count + ", ImageSize: " + _imageSize; }

        public sealed class ImageCollection : IList
        {
            private readonly ImageList _owner;
            private readonly List<Image> _list = new List<Image>();
            private readonly List<string> _keys = new List<string>();
            internal ImageCollection(ImageList owner) { _owner = owner; }
            public int Count { get { return _list.Count; } }
            public bool Empty { get { return _list.Count == 0; } }
            public bool IsReadOnly { get { return false; } }
            public StringCollection Keys
            {
                get
                {
                    StringCollection keys = new StringCollection();
                    foreach (string k in _keys) keys.Add(k ?? string.Empty);
                    return keys;
                }
            }
            public Image this[int index]
            {
                get { return _list[index]; }
                set { _list[index] = Fit(value); }
            }
            public Image this[string key]
            {
                get
                {
                    int i = IndexOfKey(key);
                    return i < 0 ? null : _list[i];
                }
            }
            // Images keep their own size; the list size is used when an image is drawn in a list or tree
            private Image Fit(Image image) { return image; }
            public void Add(Image value) { _list.Add(Fit(value)); _keys.Add(null); }
            public void Add(Icon value) { Add(value.ToBitmap()); }
            public void Add(string key, Image image) { _list.Add(Fit(image)); _keys.Add(key); }
            public void Add(string key, Icon icon) { Add(key, icon.ToBitmap()); }
            public int Add(Image value, Color transparentColor)
            {
                if (value is Bitmap b && transparentColor != Color.Empty) b.MakeTransparent(transparentColor);
                Add(value);
                return _list.Count - 1;
            }
            public void AddRange(Image[] images) { foreach (Image i in images) Add(i); }
            public int AddStrip(Image value)
            {
                int w = _owner.ImageSize.Width;
                int count = Math.Max(1, value.Width / Math.Max(1, w));
                for (int i = 0; i < count; i++)
                {
                    Bitmap b = new Bitmap(w, value.Height);
                    using (Graphics g = Graphics.FromImage(b)) g.DrawImage(value, new Rectangle(0, 0, w, value.Height), new Rectangle(i * w, 0, w, value.Height), GraphicsUnit.Pixel);
                    Add(b);
                }
                return count;
            }
            public void Clear() { _list.Clear(); _keys.Clear(); }
            public bool Contains(Image image) { return _list.Contains(image); }
            public bool ContainsKey(string key) { return IndexOfKey(key) >= 0; }
            public int IndexOf(Image image) { return _list.IndexOf(image); }
            public int IndexOfKey(string key)
            {
                if (string.IsNullOrEmpty(key)) return -1;
                for (int i = 0; i < _keys.Count; i++) if (string.Equals(_keys[i], key, StringComparison.OrdinalIgnoreCase)) return i;
                return -1;
            }
            public void Remove(Image image)
            {
                int i = _list.IndexOf(image);
                if (i >= 0) RemoveAt(i);
            }
            public void RemoveAt(int index) { _list.RemoveAt(index); _keys.RemoveAt(index); }
            public void RemoveByKey(string key)
            {
                int i = IndexOfKey(key);
                if (i >= 0) RemoveAt(i);
            }
            public void SetKeyName(int index, string name) { _keys[index] = name; }
            public IEnumerator GetEnumerator() { return _list.GetEnumerator(); }
            int IList.Add(object value) { Add((Image)value); return _list.Count - 1; }
            bool IList.Contains(object value) { return value is Image i && Contains(i); }
            int IList.IndexOf(object value) { return value is Image i ? IndexOf(i) : -1; }
            void IList.Insert(int index, object value) { _list.Insert(index, (Image)value); _keys.Insert(index, null); }
            void IList.Remove(object value) { if (value is Image i) Remove(i); }
            bool IList.IsFixedSize { get { return false; } }
            object IList.this[int index] { get { return this[index]; } set { this[index] = (Image)value; } }
            bool ICollection.IsSynchronized { get { return false; } }
            object ICollection.SyncRoot { get { return this; } }
            void ICollection.CopyTo(Array array, int index) { ((ICollection)_list).CopyTo(array, index); }
        }

        public sealed class StringCollection : List<string> { }
    }

    /// <summary>Images of an image list as the WinForms designer stores them in the resources.</summary>
    [Serializable]
    public sealed class ImageListStreamer : ISerializable, IDisposable
    {
        internal readonly List<Bitmap> Images = new List<Bitmap>();
        internal Size ImageSize;

        internal ImageListStreamer(ImageList imageList)
        {
            ImageSize = imageList.ImageSize;
            foreach (Image image in imageList.Images) Images.Add(new Bitmap(image));
        }
        private ImageListStreamer(SerializationInfo info, StreamingContext context)
        {
            foreach (SerializationEntry entry in info)
            {
                if (entry.Name == "Data" && entry.Value is byte[] data)
                {
                    try
                    {
                        Read(Decompress(data));
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("ImageListStreamer: " + ex.Message);
                    }
                }
            }
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Data", new byte[0]);
        }
        public void Dispose() { }

        // The run length compression of WinForms: "MSFt" followed by pairs of count and value
        private static byte[] Decompress(byte[] data)
        {
            if (data.Length < 4 || data[0] != 'M' || data[1] != 'S' || data[2] != 'F' || data[3] != 't') return data;
            List<byte> output = new List<byte>(data.Length * 4);
            for (int i = 4; i + 1 < data.Length; i += 2)
            {
                int count = data[i];
                byte value = data[i + 1];
                for (int j = 0; j < count; j++) output.Add(value);
            }
            return output.ToArray();
        }
        // ImageList_Write of comctl32: a header, the image bitmap and the mask bitmap (images in rows of 4)
        private void Read(byte[] data)
        {
            using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
            {
                ushort magic = reader.ReadUInt16();
                if (magic != 0x4C49) throw new InvalidDataException("No image list.");
                ushort version = reader.ReadUInt16();
                int count = reader.ReadUInt16();
                reader.ReadUInt16();    // max
                reader.ReadUInt16();    // grow
                int cx = reader.ReadUInt16();
                int cy = reader.ReadUInt16();
                reader.ReadUInt32();    // background color
                int flags = reader.ReadUInt16();
                for (int i = 0; i < 4; i++) reader.ReadInt16();
                ImageSize = new Size(cx, cy);
                SKBitmap image = ReadBmp(reader, out int imageBpp);
                SKBitmap mask = (flags & 0x0001) != 0 && reader.BaseStream.Position < reader.BaseStream.Length ? ReadBmp(reader, out _) : null;
                bool hasAlpha = false;
                if (imageBpp == 32)
                {
                    for (int y = 0; y < image.Height && !hasAlpha; y++)
                        for (int x = 0; x < image.Width; x++)
                            if (image.GetPixel(x, y).Alpha != 0) { hasAlpha = true; break; }
                }
                int perRow = Math.Max(1, image.Width / Math.Max(1, cx));
                for (int i = 0; i < count; i++)
                {
                    int ox = (i % perRow) * cx, oy = (i / perRow) * cy;
                    SKBitmap b = new SKBitmap(cx, cy, SKColorType.Bgra8888, SKAlphaType.Unpremul);
                    for (int y = 0; y < cy; y++)
                    {
                        for (int x = 0; x < cx; x++)
                        {
                            if (ox + x >= image.Width || oy + y >= image.Height) continue;
                            SKColor c = image.GetPixel(ox + x, oy + y);
                            byte a = 255;
                            if (imageBpp == 32 && hasAlpha) a = c.Alpha;
                            else if (mask != null && ox + x < mask.Width && oy + y < mask.Height && mask.GetPixel(ox + x, oy + y).Red > 127) a = 0;
                            b.SetPixel(x, y, new SKColor(c.Red, c.Green, c.Blue, a));
                        }
                    }
                    Images.Add(Bitmap.FromSKBitmap(Premultiply(b)));
                }
            }
        }
        private static SKBitmap Premultiply(SKBitmap source)
        {
            SKBitmap result = new SKBitmap(source.Width, source.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using (SKCanvas canvas = new SKCanvas(result))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.DrawBitmap(source, 0, 0);
            }
            source.Dispose();
            return result;
        }
        private static SKBitmap ReadBmp(BinaryReader reader, out int bpp)
        {
            long start = reader.BaseStream.Position;
            if (reader.ReadByte() != 'B' || reader.ReadByte() != 'M') throw new InvalidDataException("No bitmap.");
            reader.ReadInt32();         // file size
            reader.ReadInt32();         // reserved
            int offBits = reader.ReadInt32();
            int headerSize = reader.ReadInt32();
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            reader.ReadUInt16();        // planes
            bpp = reader.ReadUInt16();
            int compression = reader.ReadInt32();
            int imageSize = reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            int colors = reader.ReadInt32();
            reader.ReadInt32();
            reader.BaseStream.Position = start + 14 + headerSize;
            if (colors == 0 && bpp <= 8) colors = 1 << bpp;
            SKColor[] palette = new SKColor[colors];
            for (int i = 0; i < colors; i++)
            {
                byte b = reader.ReadByte(), g = reader.ReadByte(), r = reader.ReadByte();
                reader.ReadByte();
                palette[i] = new SKColor(r, g, b, 255);
            }
            if (bpp == 16 && compression == 3) reader.BaseStream.Position += 0;
            bool bottomUp = height > 0;
            height = Math.Abs(height);
            int stride = ((width * bpp + 31) / 32) * 4;
            reader.BaseStream.Position = start + offBits;
            byte[] pixels = reader.ReadBytes(stride * height);
            SKBitmap bitmap = new SKBitmap(Math.Max(1, width), Math.Max(1, height), SKColorType.Bgra8888, SKAlphaType.Unpremul);
            for (int row = 0; row < height; row++)
            {
                int y = bottomUp ? height - 1 - row : row;
                int o = row * stride;
                for (int x = 0; x < width; x++)
                {
                    SKColor c;
                    switch (bpp)
                    {
                        case 32: c = new SKColor(pixels[o + x * 4 + 2], pixels[o + x * 4 + 1], pixels[o + x * 4], pixels[o + x * 4 + 3]); break;
                        case 24: c = new SKColor(pixels[o + x * 3 + 2], pixels[o + x * 3 + 1], pixels[o + x * 3], 255); break;
                        case 16:
                            {
                                int v = pixels[o + x * 2] | (pixels[o + x * 2 + 1] << 8);
                                c = new SKColor((byte)(((v >> 10) & 31) * 255 / 31), (byte)(((v >> 5) & 31) * 255 / 31), (byte)((v & 31) * 255 / 31), 255);
                                break;
                            }
                        case 8: c = palette[pixels[o + x]]; break;
                        case 4: c = palette[(pixels[o + x / 2] >> (x % 2 == 0 ? 4 : 0)) & 0x0F]; break;
                        default: c = palette[(pixels[o + x / 8] >> (7 - x % 8)) & 1]; break;
                    }
                    bitmap.SetPixel(x, y, c);
                }
            }
            return bitmap;
        }
    }
}
