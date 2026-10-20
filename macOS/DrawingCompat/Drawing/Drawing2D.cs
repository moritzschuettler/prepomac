// PrePoMax for macOS - System.Drawing.Drawing2D and System.Drawing.Imaging on SkiaSharp.

using System.Collections.Generic;
using SkiaSharp;

namespace System.Drawing.Drawing2D
{
    public enum SmoothingMode { Invalid = -1, Default = 0, HighSpeed = 1, HighQuality = 2, None = 3, AntiAlias = 4 }
    public enum InterpolationMode
    {
        Invalid = -1, Default = 0, Low = 1, High = 2, Bilinear = 3, Bicubic = 4, NearestNeighbor = 5, HighQualityBilinear = 6,
        HighQualityBicubic = 7
    }
    public enum PixelOffsetMode { Invalid = -1, Default = 0, HighSpeed = 1, HighQuality = 2, None = 3, Half = 4 }
    public enum CompositingQuality { Invalid = -1, Default = 0, HighSpeed = 1, HighQuality = 2, GammaCorrected = 3, AssumeLinear = 4 }
    public enum CompositingMode { SourceOver = 0, SourceCopy = 1 }
    public enum DashStyle { Solid = 0, Dash = 1, Dot = 2, DashDot = 3, DashDotDot = 4, Custom = 5 }
    public enum DashCap { Flat = 0, Round = 2, Triangle = 3 }
    public enum LineCap
    {
        Flat = 0, Square = 1, Round = 2, Triangle = 3, NoAnchor = 16, SquareAnchor = 17, RoundAnchor = 18, DiamondAnchor = 19,
        ArrowAnchor = 20, Custom = 255, AnchorMask = 240
    }
    public enum LineJoin { Miter = 0, Bevel = 1, Round = 2, MiterClipped = 3 }
    public enum PenAlignment { Center = 0, Inset = 1, Outset = 2, Left = 3, Right = 4 }
    public enum FillMode { Alternate = 0, Winding = 1 }
    public enum CombineMode { Replace = 0, Intersect = 1, Union = 2, Xor = 3, Exclude = 4, Complement = 5 }
    public enum MatrixOrder { Prepend = 0, Append = 1 }
    public enum FlushIntention { Flush = 0, Sync = 1 }
    public enum LinearGradientMode { Horizontal = 0, Vertical = 1, ForwardDiagonal = 2, BackwardDiagonal = 3 }
    public enum WrapMode { Tile = 0, TileFlipX = 1, TileFlipY = 2, TileFlipXY = 3, Clamp = 4 }
    public enum HatchStyle { Horizontal = 0, Vertical = 1, ForwardDiagonal = 2, BackwardDiagonal = 3, Cross = 4, DiagonalCross = 5 }

    public sealed class GraphicsState : MarshalByRefObject
    {
        internal int Count;
        internal RectangleF Clip;
        internal GraphicsState(int count, RectangleF clip) { Count = count; Clip = clip; }
    }

    public sealed class GraphicsContainer : MarshalByRefObject
    {
        internal GraphicsState State;
        internal GraphicsContainer(GraphicsState state) { State = state; }
    }

    public class CustomLineCap : MarshalByRefObject, ICloneable, IDisposable
    {
        public object Clone() { return MemberwiseClone(); }
        public void Dispose() { }
    }

    public sealed class AdjustableArrowCap : CustomLineCap
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public bool Filled { get; set; }
        public AdjustableArrowCap(float width, float height) : this(width, height, true) { }
        public AdjustableArrowCap(float width, float height, bool isFilled) { Width = width; Height = height; Filled = isFilled; }
    }

    public sealed class Matrix : MarshalByRefObject, IDisposable
    {
        private float[] _e;
        public float[] Elements { get { return (float[])_e.Clone(); } }
        public float OffsetX { get { return _e[4]; } }
        public float OffsetY { get { return _e[5]; } }
        public bool IsIdentity { get { return _e[0] == 1 && _e[1] == 0 && _e[2] == 0 && _e[3] == 1 && _e[4] == 0 && _e[5] == 0; } }
        public Matrix() { _e = new float[] { 1, 0, 0, 1, 0, 0 }; }
        public Matrix(float m11, float m12, float m21, float m22, float dx, float dy) { _e = new[] { m11, m12, m21, m22, dx, dy }; }
        private SKMatrix ToSK() { return new SKMatrix(_e[0], _e[2], _e[4], _e[1], _e[3], _e[5], 0, 0, 1); }
        private void FromSK(SKMatrix m) { _e = new[] { m.ScaleX, m.SkewY, m.SkewX, m.ScaleY, m.TransX, m.TransY }; }
        public void Translate(float offsetX, float offsetY) { FromSK(SKMatrix.Concat(ToSK(), SKMatrix.CreateTranslation(offsetX, offsetY))); }
        public void Translate(float offsetX, float offsetY, MatrixOrder order)
        {
            FromSK(order == MatrixOrder.Append ? SKMatrix.Concat(SKMatrix.CreateTranslation(offsetX, offsetY), ToSK())
                                               : SKMatrix.Concat(ToSK(), SKMatrix.CreateTranslation(offsetX, offsetY)));
        }
        public void Scale(float scaleX, float scaleY) { FromSK(SKMatrix.Concat(ToSK(), SKMatrix.CreateScale(scaleX, scaleY))); }
        public void Scale(float scaleX, float scaleY, MatrixOrder order)
        {
            FromSK(order == MatrixOrder.Append ? SKMatrix.Concat(SKMatrix.CreateScale(scaleX, scaleY), ToSK())
                                               : SKMatrix.Concat(ToSK(), SKMatrix.CreateScale(scaleX, scaleY)));
        }
        public void Rotate(float angle) { FromSK(SKMatrix.Concat(ToSK(), SKMatrix.CreateRotationDegrees(angle))); }
        public void Rotate(float angle, MatrixOrder order)
        {
            FromSK(order == MatrixOrder.Append ? SKMatrix.Concat(SKMatrix.CreateRotationDegrees(angle), ToSK())
                                               : SKMatrix.Concat(ToSK(), SKMatrix.CreateRotationDegrees(angle)));
        }
        public void RotateAt(float angle, PointF point)
        {
            FromSK(SKMatrix.Concat(ToSK(), SKMatrix.CreateRotationDegrees(angle, point.X, point.Y)));
        }
        public void Multiply(Matrix matrix) { FromSK(SKMatrix.Concat(ToSK(), matrix.ToSK())); }
        public void Multiply(Matrix matrix, MatrixOrder order)
        {
            FromSK(order == MatrixOrder.Append ? SKMatrix.Concat(matrix.ToSK(), ToSK()) : SKMatrix.Concat(ToSK(), matrix.ToSK()));
        }
        public void Invert() { if (ToSK().TryInvert(out SKMatrix inverse)) FromSK(inverse); }
        public void Reset() { _e = new float[] { 1, 0, 0, 1, 0, 0 }; }
        public void TransformPoints(PointF[] pts)
        {
            SKMatrix m = ToSK();
            for (int i = 0; i < pts.Length; i++)
            {
                SKPoint p = m.MapPoint(pts[i].X, pts[i].Y);
                pts[i] = new PointF(p.X, p.Y);
            }
        }
        public void TransformPoints(Point[] pts)
        {
            SKMatrix m = ToSK();
            for (int i = 0; i < pts.Length; i++)
            {
                SKPoint p = m.MapPoint(pts[i].X, pts[i].Y);
                pts[i] = new Point((int)Math.Round(p.X), (int)Math.Round(p.Y));
            }
        }
        public void TransformVectors(PointF[] pts)
        {
            SKMatrix m = ToSK();
            for (int i = 0; i < pts.Length; i++)
            {
                SKPoint p = m.MapVector(pts[i].X, pts[i].Y);
                pts[i] = new PointF(p.X, p.Y);
            }
        }
        public Matrix Clone() { return new Matrix(_e[0], _e[1], _e[2], _e[3], _e[4], _e[5]); }
        public void Dispose() { }
    }

    public sealed class GraphicsPath : MarshalByRefObject, ICloneable, IDisposable
    {
        private SKPath _path = new SKPath();
        public SKPath SKPath { get { return _path; } }
        public FillMode FillMode
        {
            get { return _path.FillType == SKPathFillType.Winding ? FillMode.Winding : FillMode.Alternate; }
            set { _path.FillType = value == FillMode.Winding ? SKPathFillType.Winding : SKPathFillType.EvenOdd; }
        }
        public int PointCount { get { return _path.PointCount; } }
        public PointF[] PathPoints { get { return Array.ConvertAll(_path.Points, p => new PointF(p.X, p.Y)); } }

        public GraphicsPath() { _path.FillType = SKPathFillType.EvenOdd; }
        public GraphicsPath(FillMode fillMode) : this() { FillMode = fillMode; }

        private void Connect(float x, float y)
        {
            if (_path.IsEmpty || _newFigure) { _path.MoveTo(x, y); _newFigure = false; }
            else _path.LineTo(x, y);
        }
        private bool _newFigure = true;
        public void AddLine(float x1, float y1, float x2, float y2) { Connect(x1, y1); _path.LineTo(x2, y2); }
        public void AddLine(int x1, int y1, int x2, int y2) { AddLine((float)x1, y1, x2, y2); }
        public void AddLine(PointF pt1, PointF pt2) { AddLine(pt1.X, pt1.Y, pt2.X, pt2.Y); }
        public void AddLine(Point pt1, Point pt2) { AddLine(pt1.X, pt1.Y, pt2.X, pt2.Y); }
        public void AddLines(PointF[] points)
        {
            if (points.Length == 0) return;
            Connect(points[0].X, points[0].Y);
            for (int i = 1; i < points.Length; i++) _path.LineTo(points[i].X, points[i].Y);
        }
        public void AddLines(Point[] points) { AddLines(Array.ConvertAll(points, p => (PointF)p)); }
        public void AddRectangle(RectangleF rect) { _path.AddRect(rect.ToSK()); _newFigure = true; }
        public void AddRectangle(Rectangle rect) { AddRectangle((RectangleF)rect); }
        public void AddRectangles(Rectangle[] rects) { foreach (var r in rects) AddRectangle(r); }
        public void AddEllipse(RectangleF rect) { _path.AddOval(rect.ToSK()); _newFigure = true; }
        public void AddEllipse(Rectangle rect) { AddEllipse((RectangleF)rect); }
        public void AddEllipse(float x, float y, float width, float height) { AddEllipse(new RectangleF(x, y, width, height)); }
        public void AddEllipse(int x, int y, int width, int height) { AddEllipse(new RectangleF(x, y, width, height)); }
        public void AddArc(RectangleF rect, float startAngle, float sweepAngle)
        {
            _path.ArcTo(rect.ToSK(), startAngle, sweepAngle, _path.IsEmpty || _newFigure);
            _newFigure = false;
        }
        public void AddArc(Rectangle rect, float startAngle, float sweepAngle) { AddArc((RectangleF)rect, startAngle, sweepAngle); }
        public void AddArc(float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            AddArc(new RectangleF(x, y, width, height), startAngle, sweepAngle);
        }
        public void AddArc(int x, int y, int width, int height, float startAngle, float sweepAngle)
        {
            AddArc(new RectangleF(x, y, width, height), startAngle, sweepAngle);
        }
        public void AddPolygon(PointF[] points)
        {
            if (points.Length == 0) return;
            _path.MoveTo(points[0].X, points[0].Y);
            for (int i = 1; i < points.Length; i++) _path.LineTo(points[i].X, points[i].Y);
            _path.Close();
            _newFigure = true;
        }
        public void AddPolygon(Point[] points) { AddPolygon(Array.ConvertAll(points, p => (PointF)p)); }
        public void AddBezier(PointF pt1, PointF pt2, PointF pt3, PointF pt4)
        {
            Connect(pt1.X, pt1.Y);
            _path.CubicTo(pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y);
        }
        public void AddPath(GraphicsPath addingPath, bool connect) { _path.AddPath(addingPath._path); }
        public void AddString(string s, FontFamily family, int style, float emSize, PointF origin, StringFormat format)
        {
            Font font = new Font(family, emSize, (FontStyle)style, GraphicsUnit.Pixel);
            using (SKPaint paint = new SKPaint { Typeface = font.Typeface, TextSize = font.SizeInPixels })
            using (SKPath text = paint.GetTextPath(s, origin.X, origin.Y + font.SizeInPixels)) _path.AddPath(text);
        }
        public void AddString(string s, FontFamily family, int style, float emSize, RectangleF layoutRect, StringFormat format)
        {
            AddString(s, family, style, emSize, layoutRect.Location, format);
        }
        public void CloseFigure() { _path.Close(); _newFigure = true; }
        public void CloseAllFigures() { _path.Close(); _newFigure = true; }
        public void StartFigure() { _newFigure = true; }
        public void Reset() { _path.Reset(); _newFigure = true; }
        public RectangleF GetBounds() { SKRect b = _path.Bounds; return new RectangleF(b.Left, b.Top, b.Width, b.Height); }
        public bool IsVisible(PointF point) { return _path.Contains(point.X, point.Y); }
        public bool IsVisible(Point point) { return _path.Contains(point.X, point.Y); }
        public bool IsVisible(float x, float y) { return _path.Contains(x, y); }
        public void Transform(Matrix matrix)
        {
            float[] e = matrix.Elements;
            _path.Transform(new SKMatrix(e[0], e[2], e[4], e[1], e[3], e[5], 0, 0, 1));
        }
        public object Clone()
        {
            GraphicsPath path = new GraphicsPath();
            path._path = new SKPath(_path);
            return path;
        }
        public void Dispose() { _path.Dispose(); }
    }

    public sealed class LinearGradientBrush : Brush
    {
        private readonly PointF _p1;
        private readonly PointF _p2;
        public Color[] LinearColors { get; set; }
        public RectangleF Rectangle { get; }
        public WrapMode WrapMode { get; set; }
        public Blend Blend { get; set; }
        public ColorBlend InterpolationColors { get; set; }
        public bool GammaCorrection { get; set; }

        public LinearGradientBrush(PointF point1, PointF point2, Color color1, Color color2)
        {
            _p1 = point1;
            _p2 = point2;
            LinearColors = new[] { color1, color2 };
            Rectangle = RectangleF.FromLTRB(Math.Min(point1.X, point2.X), Math.Min(point1.Y, point2.Y),
                                            Math.Max(point1.X, point2.X), Math.Max(point1.Y, point2.Y));
        }
        public LinearGradientBrush(Point point1, Point point2, Color color1, Color color2)
            : this((PointF)point1, (PointF)point2, color1, color2) { }
        public LinearGradientBrush(RectangleF rect, Color color1, Color color2, LinearGradientMode linearGradientMode)
            : this(rect.Location, End(rect, linearGradientMode), color1, color2) { Rectangle = rect; }
        public LinearGradientBrush(Rectangle rect, Color color1, Color color2, LinearGradientMode linearGradientMode)
            : this((RectangleF)rect, color1, color2, linearGradientMode) { }
        public LinearGradientBrush(RectangleF rect, Color color1, Color color2, float angle)
            : this(rect.Location, new PointF(rect.X + (float)Math.Cos(angle * Math.PI / 180) * rect.Width,
                                             rect.Y + (float)Math.Sin(angle * Math.PI / 180) * rect.Height), color1, color2)
        { Rectangle = rect; }
        public LinearGradientBrush(Rectangle rect, Color color1, Color color2, float angle)
            : this((RectangleF)rect, color1, color2, angle) { }
        public LinearGradientBrush(RectangleF rect, Color color1, Color color2, float angle, bool isAngleScaleable)
            : this(rect, color1, color2, angle) { }
        public LinearGradientBrush(Rectangle rect, Color color1, Color color2, float angle, bool isAngleScaleable)
            : this((RectangleF)rect, color1, color2, angle) { }
        private static PointF End(RectangleF r, LinearGradientMode mode)
        {
            switch (mode)
            {
                case LinearGradientMode.Vertical: return new PointF(r.X, r.Bottom);
                case LinearGradientMode.ForwardDiagonal: return new PointF(r.Right, r.Bottom);
                case LinearGradientMode.BackwardDiagonal: return new PointF(r.Right, r.Y);
                default: return new PointF(r.Right, r.Y);
            }
        }
        internal override void Apply(SKPaint paint, RectangleF bounds)
        {
            SKColor[] colors;
            float[] positions = null;
            if (InterpolationColors != null && InterpolationColors.Colors.Length > 1)
            {
                colors = Array.ConvertAll(InterpolationColors.Colors, c => c.ToSK());
                positions = InterpolationColors.Positions;
            }
            else colors = new[] { LinearColors[0].ToSK(), LinearColors[1].ToSK() };
            paint.Color = SKColors.Black;
            paint.Shader = SKShader.CreateLinearGradient(new SKPoint(_p1.X, _p1.Y), new SKPoint(_p2.X, _p2.Y), colors, positions,
                                                         SKShaderTileMode.Clamp);
        }
        public void SetSigmaBellShape(float focus) { }
        public void SetBlendTriangularShape(float focus) { }
        public override object Clone() { return MemberwiseClone(); }
    }

    public sealed class Blend
    {
        public float[] Factors { get; set; }
        public float[] Positions { get; set; }
        public Blend() { Factors = new float[1]; Positions = new float[1]; }
        public Blend(int count) { Factors = new float[count]; Positions = new float[count]; }
    }

    public sealed class ColorBlend
    {
        public Color[] Colors { get; set; }
        public float[] Positions { get; set; }
        public ColorBlend() { Colors = new Color[1]; Positions = new float[1]; }
        public ColorBlend(int count) { Colors = new Color[count]; Positions = new float[count]; }
    }

    public sealed class HatchBrush : Brush
    {
        public HatchStyle HatchStyle { get; }
        public Color ForegroundColor { get; }
        public Color BackgroundColor { get; }
        public HatchBrush(HatchStyle hatchstyle, Color foreColor) : this(hatchstyle, foreColor, Color.Transparent) { }
        public HatchBrush(HatchStyle hatchstyle, Color foreColor, Color backColor)
        {
            HatchStyle = hatchstyle;
            ForegroundColor = foreColor;
            BackgroundColor = backColor;
        }
        internal override void Apply(SKPaint paint, RectangleF bounds) { paint.Color = ForegroundColor.ToSK(); }
        public override object Clone() { return new HatchBrush(HatchStyle, ForegroundColor, BackgroundColor); }
    }
}

namespace System.Drawing.Imaging
{
    public enum PixelFormat
    {
        Undefined = 0, DontCare = 0, Indexed = 65536, Gdi = 131072, Alpha = 262144, PAlpha = 524288, Extended = 1048576,
        Canonical = 2097152, Format1bppIndexed = 196865, Format4bppIndexed = 197634, Format8bppIndexed = 198659,
        Format16bppGrayScale = 1052676, Format16bppRgb555 = 135173, Format16bppRgb565 = 135174, Format16bppArgb1555 = 397319,
        Format24bppRgb = 137224, Format32bppRgb = 139273, Format32bppArgb = 2498570, Format32bppPArgb = 925707,
        Format48bppRgb = 1060876, Format64bppArgb = 3424269, Format64bppPArgb = 1851406, Max = 15
    }

    [Flags]
    public enum ImageLockMode { ReadOnly = 1, WriteOnly = 2, ReadWrite = 3, UserInputBuffer = 4 }

    public sealed class BitmapData
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Stride { get; set; }
        public PixelFormat PixelFormat { get; set; }
        public IntPtr Scan0 { get; set; }
        public int Reserved { get; set; }
        internal Rectangle Rect;
    }

    public sealed class ImageFormat
    {
        public Guid Guid { get; }
        private readonly string _name;
        public ImageFormat(Guid guid) { Guid = guid; _name = guid.ToString(); }
        private ImageFormat(string guid, string name) { Guid = new Guid(guid); _name = name; }
        public static ImageFormat MemoryBmp { get; } = new ImageFormat("b96b3caa-0728-11d3-9d7b-0000f81ef32e", "MemoryBMP");
        public static ImageFormat Bmp { get; } = new ImageFormat("b96b3cab-0728-11d3-9d7b-0000f81ef32e", "Bmp");
        public static ImageFormat Emf { get; } = new ImageFormat("b96b3cac-0728-11d3-9d7b-0000f81ef32e", "Emf");
        public static ImageFormat Wmf { get; } = new ImageFormat("b96b3cad-0728-11d3-9d7b-0000f81ef32e", "Wmf");
        public static ImageFormat Jpeg { get; } = new ImageFormat("b96b3cae-0728-11d3-9d7b-0000f81ef32e", "Jpeg");
        public static ImageFormat Png { get; } = new ImageFormat("b96b3caf-0728-11d3-9d7b-0000f81ef32e", "Png");
        public static ImageFormat Gif { get; } = new ImageFormat("b96b3cb0-0728-11d3-9d7b-0000f81ef32e", "Gif");
        public static ImageFormat Tiff { get; } = new ImageFormat("b96b3cb1-0728-11d3-9d7b-0000f81ef32e", "Tiff");
        public static ImageFormat Exif { get; } = new ImageFormat("b96b3cb2-0728-11d3-9d7b-0000f81ef32e", "Exif");
        public static ImageFormat Icon { get; } = new ImageFormat("b96b3cb5-0728-11d3-9d7b-0000f81ef32e", "Icon");
        internal static ImageFormat FromExtension(string extension)
        {
            switch ((extension ?? "").ToLowerInvariant())
            {
                case ".jpg": case ".jpeg": return Jpeg;
                case ".bmp": return Bmp;
                case ".gif": return Gif;
                case ".tif": case ".tiff": return Tiff;
                case ".ico": return Icon;
                default: return Png;
            }
        }
        public override bool Equals(object o) { return o is ImageFormat f && f.Guid == Guid; }
        public override int GetHashCode() { return Guid.GetHashCode(); }
        public override string ToString() { return _name; }
        public static bool operator ==(ImageFormat a, ImageFormat b) { return ReferenceEquals(a, b) || (a is object && a.Equals(b)); }
        public static bool operator !=(ImageFormat a, ImageFormat b) { return !(a == b); }
    }

    public sealed class ImageCodecInfo
    {
        public Guid Clsid { get; set; }
        public Guid FormatID { get; set; }
        public string MimeType { get; set; }
        public string FormatDescription { get; set; }
        public string FilenameExtension { get; set; }
        public static ImageCodecInfo[] GetImageEncoders()
        {
            return new[]
            {
                new ImageCodecInfo { FormatID = ImageFormat.Jpeg.Guid, MimeType = "image/jpeg", FormatDescription = "JPEG", FilenameExtension = "*.JPG;*.JPEG" },
                new ImageCodecInfo { FormatID = ImageFormat.Png.Guid, MimeType = "image/png", FormatDescription = "PNG", FilenameExtension = "*.PNG" },
                new ImageCodecInfo { FormatID = ImageFormat.Bmp.Guid, MimeType = "image/bmp", FormatDescription = "BMP", FilenameExtension = "*.BMP" },
            };
        }
        public static ImageCodecInfo[] GetImageDecoders() { return GetImageEncoders(); }
    }

    public sealed class Encoder
    {
        public Guid Guid { get; }
        public Encoder(Guid guid) { Guid = guid; }
        public static readonly Encoder Quality = new Encoder(new Guid("1d5be4b5-fa4a-452d-9cdd-5db35105e7eb"));
        public static readonly Encoder Compression = new Encoder(new Guid("e09d739d-ccd4-44ee-8eba-3fbf8be4fc58"));
        public static readonly Encoder ColorDepth = new Encoder(new Guid("66087055-ad66-4c7c-9a18-38a2310b8337"));
    }

    public sealed class EncoderParameter : IDisposable
    {
        public Encoder Encoder { get; set; }
        public long Value { get; }
        public EncoderParameter(Encoder encoder, long value) { Encoder = encoder; Value = value; }
        public EncoderParameter(Encoder encoder, int value) { Encoder = encoder; Value = value; }
        public EncoderParameter(Encoder encoder, byte value) { Encoder = encoder; Value = value; }
        public void Dispose() { }
    }

    public sealed class EncoderParameters : IDisposable
    {
        public EncoderParameter[] Param { get; set; }
        public EncoderParameters() : this(1) { }
        public EncoderParameters(int count) { Param = new EncoderParameter[count]; }
        public void Dispose() { }
    }

    public sealed class ColorMatrix
    {
        private readonly float[][] _m;
        public ColorMatrix()
        {
            _m = new float[5][];
            for (int i = 0; i < 5; i++) { _m[i] = new float[5]; _m[i][i] = 1; }
        }
        public ColorMatrix(float[][] newColorMatrix) { _m = newColorMatrix; }
        public float this[int row, int column] { get { return _m[row][column]; } set { _m[row][column] = value; } }
        public float Matrix33 { get { return _m[3][3]; } set { _m[3][3] = value; } }
        // GDI+ multiplies row vectors; Skia expects a 4x5 matrix for column vectors with offsets in 0..255
        internal float[] ToSkia()
        {
            float[] r = new float[20];
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++) r[row * 5 + col] = _m[col][row];
                r[row * 5 + 4] = _m[4][row] * 255;
            }
            return r;
        }
    }

    public enum ColorMatrixFlag { Default = 0, SkipGrays = 1, AltGrays = 2 }
    public enum ColorAdjustType { Default = 0, Bitmap = 1, Brush = 2, Pen = 3, Text = 4, Count = 5, Any = 6 }

    public sealed class ImageAttributes : ICloneable, IDisposable
    {
        internal ColorMatrix ColorMatrix;
        public void SetColorMatrix(ColorMatrix newColorMatrix) { ColorMatrix = newColorMatrix; }
        public void SetColorMatrix(ColorMatrix newColorMatrix, ColorMatrixFlag flags) { ColorMatrix = newColorMatrix; }
        public void SetColorMatrix(ColorMatrix newColorMatrix, ColorMatrixFlag mode, ColorAdjustType type) { ColorMatrix = newColorMatrix; }
        public void SetWrapMode(Drawing2D.WrapMode mode) { }
        public void SetColorKey(Color colorLow, Color colorHigh) { }
        public void SetGamma(float gamma) { }
        public object Clone() { return MemberwiseClone(); }
        public void Dispose() { }
    }
}
