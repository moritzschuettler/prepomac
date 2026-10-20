// PrePoMax for macOS - ZedGraph: the API of the ZedGraph charting library that PrePoMax uses, drawn with System.Drawing.
// The original library draws with GDI+ on Windows only; this implementation has the same classes and members.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace ZedGraph
{
    public enum LegendPos { Top, Left, Right, Bottom, InsideTopLeft, InsideTopRight, InsideBotLeft, InsideBotRight, Float, TopCenter, BottomCenter, TopFlushLeft, BottomFlushLeft }
    public enum SymbolType { Square, Diamond, Triangle, Circle, XCross, Plus, Star, TriangleDown, HDash, VDash, UserDefined, Default, None }
    public enum AxisType { Linear, Log, Date, Text, Ordinal, DateAsOrdinal, LinearAsOrdinal, Exponent }
    public enum FillType { None, Solid, Brush, Range, GradientByX, GradientByY, GradientByZ, GradientByColorValue }
    public enum AlignH { Left, Center, Right }
    public enum AlignV { Top, Center, Bottom }
    public enum CoordType { ChartFraction, PaneFraction, XScaleYChartFraction, XChartFractionYScale, AxisXYScale, AxisXY2Scale, XChartFractionYPaneFraction, XPaneFractionYChartFraction }
    public enum ZOrder { A_InFront, B_BehindLegend, C_BehindChartBorder, D_BehindAxis, E_BehindCurves, F_BehindGrid, G_BehindChartFill, H_BehindAll }

    [Serializable]
    public class Fill : ICloneable
    {
        public Color Color { get; set; }
        public Color SecondaryValueGradientColor { get; set; }
        public Brush Brush { get; set; }
        public FillType Type { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsScaled { get; set; }
        public float Angle { get; set; }
        public Fill() { Color = Color.White; Type = FillType.None; }
        public Fill(Color color) { Color = color; Type = FillType.Solid; }
        public Fill(Color color1, Color color2) : this(color1) { SecondaryValueGradientColor = color2; Type = FillType.Brush; }
        public Fill(Color color1, Color color2, float angle) : this(color1, color2) { Angle = angle; }
        public Fill(Brush brush) { Brush = brush; Type = FillType.Brush; }
        public Fill(Fill rhs) { Color = rhs.Color; SecondaryValueGradientColor = rhs.SecondaryValueGradientColor; Type = rhs.Type; Angle = rhs.Angle; IsVisible = rhs.IsVisible; }
        public bool IsGradientValueType { get { return false; } }
        public object Clone() { return new Fill(this); }
        internal void Draw(Graphics g, RectangleF rect)
        {
            if (!IsVisible || Type == FillType.None) return;
            if (Type == FillType.Brush && !SecondaryValueGradientColor.IsEmpty && rect.Width > 0 && rect.Height > 0)
            {
                using (LinearGradientBrush b = new LinearGradientBrush(rect, Color, SecondaryValueGradientColor, Angle)) g.FillRectangle(b, rect);
                return;
            }
            using (SolidBrush b = new SolidBrush(Color)) g.FillRectangle(b, rect);
        }
    }

    [Serializable]
    public class Border : ICloneable
    {
        public Color Color { get; set; } = Color.Black;
        public float Width { get; set; } = 1;
        public bool IsVisible { get; set; } = true;
        public float InflateFactor { get; set; }
        public Border() { }
        public Border(Color color, float width) { Color = color; Width = width; }
        public Border(bool isVisible, Color color, float width) : this(color, width) { IsVisible = isVisible; }
        public Border(Border rhs) { Color = rhs.Color; Width = rhs.Width; IsVisible = rhs.IsVisible; }
        public object Clone() { return new Border(this); }
        internal void Draw(Graphics g, RectangleF rect)
        {
            if (!IsVisible) return;
            using (Pen p = new Pen(Color, Width)) g.DrawRectangle(p, rect.X, rect.Y, rect.Width, rect.Height);
        }
    }

    [Serializable]
    public class FontSpec : ICloneable
    {
        public string Family { get; set; } = "Arial";
        public float Size { get; set; } = 12;
        public Color FontColor { get; set; } = Color.Black;
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public bool IsUnderline { get; set; }
        public bool IsAntiAlias { get; set; } = true;
        public bool IsDropShadow { get; set; }
        public float Angle { get; set; }
        public StringAlignment StringAlignment { get; set; } = StringAlignment.Center;
        public Fill Fill { get; set; } = new Fill(Color.White) { IsVisible = false };
        public Border Border { get; set; } = new Border { IsVisible = false };
        public FontSpec() { }
        public FontSpec(string family, float size, Color color, bool isBold, bool isItalic, bool isUnderline)
        {
            Family = family; Size = size; FontColor = color; IsBold = isBold; IsItalic = isItalic; IsUnderline = isUnderline;
        }
        public FontSpec(string family, float size, Color color, bool isBold, bool isItalic, bool isUnderline, Color fillColor, Brush fillBrush, FillType fillType)
            : this(family, size, color, isBold, isItalic, isUnderline) { Fill = new Fill(fillColor) { Type = fillType }; }
        public FontSpec(FontSpec rhs)
        {
            Family = rhs.Family; Size = rhs.Size; FontColor = rhs.FontColor; IsBold = rhs.IsBold; IsItalic = rhs.IsItalic; IsUnderline = rhs.IsUnderline;
            Angle = rhs.Angle; Fill = new Fill(rhs.Fill); Border = new Border(rhs.Border); StringAlignment = rhs.StringAlignment; IsAntiAlias = rhs.IsAntiAlias;
        }
        public object Clone() { return new FontSpec(this); }
        // ZedGraph sizes are points at the base dimension of the pane; the fonts are scaled with the pane like in ZedGraph
        public Font GetFont(float scaleFactor)
        {
            FontStyle style = (IsBold ? FontStyle.Bold : 0) | (IsItalic ? FontStyle.Italic : 0) | (IsUnderline ? FontStyle.Underline : 0);
            return new Font(Family, Math.Max(1f, Size * scaleFactor), style, GraphicsUnit.Pixel);
        }
        internal SizeF Measure(Graphics g, string text, float scale)
        {
            using (Font font = GetFont(scale)) return g.MeasureString(text ?? "", font);
        }
        internal void Draw(Graphics g, string text, float x, float y, AlignH alignH, AlignV alignV, float scale, float angle = 0)
        {
            if (string.IsNullOrEmpty(text)) return;
            using (Font font = GetFont(scale))
            using (SolidBrush brush = new SolidBrush(FontColor))
            {
                SizeF size = g.MeasureString(text, font);
                GraphicsState state = g.Save();
                g.TranslateTransform(x, y);
                if (angle != 0) g.RotateTransform(-angle);
                float dx = alignH == AlignH.Left ? 0 : alignH == AlignH.Center ? -size.Width / 2 : -size.Width;
                float dy = alignV == AlignV.Top ? 0 : alignV == AlignV.Center ? -size.Height / 2 : -size.Height;
                if (Fill.IsVisible && Fill.Type != FillType.None) Fill.Draw(g, new RectangleF(dx, dy, size.Width, size.Height));
                g.DrawString(text, font, brush, dx, dy);
                g.Restore(state);
            }
        }
    }

    [Serializable]
    public class Label : ICloneable
    {
        public string Text { get; set; }
        public FontSpec FontSpec { get; set; }
        public bool IsVisible { get; set; } = true;
        public Label(string text, string fontFamily, float fontSize, Color color, bool isBold, bool isItalic, bool isUnderline)
        {
            Text = text;
            FontSpec = new FontSpec(fontFamily, fontSize, color, isBold, isItalic, isUnderline);
        }
        public Label(string text, FontSpec fontSpec) { Text = text; FontSpec = fontSpec; }
        public Label(Label rhs) { Text = rhs.Text; FontSpec = rhs.FontSpec == null ? null : new FontSpec(rhs.FontSpec); IsVisible = rhs.IsVisible; }
        public object Clone() { return new Label(this); }
    }

    [Serializable]
    public class GapLabel : Label
    {
        public float Gap { get; set; } = 0.3f;
        public GapLabel(string text, string fontFamily, float fontSize, Color color, bool isBold, bool isItalic, bool isUnderline)
            : base(text, fontFamily, fontSize, color, isBold, isItalic, isUnderline) { }
        public GapLabel(GapLabel rhs) : base(rhs) { Gap = rhs.Gap; }
    }

    [Serializable]
    public class AxisLabel : GapLabel
    {
        public bool IsOmitMag { get; set; }
        public bool IsTitleAtCross { get; set; } = true;
        public AxisLabel(string text, string fontFamily, float fontSize, Color color, bool isBold, bool isItalic, bool isUnderline)
            : base(text, fontFamily, fontSize, color, isBold, isItalic, isUnderline) { }
    }

    [Serializable]
    public class Line : ICloneable
    {
        public Color Color { get; set; } = Color.Red;
        public float Width { get; set; } = 1;
        public DashStyle Style { get; set; } = DashStyle.Solid;
        public bool IsVisible { get; set; } = true;
        public bool IsSmooth { get; set; }
        public float SmoothTension { get; set; } = 0.5f;
        public bool IsAntiAlias { get; set; }
        public bool IsOptimizedDraw { get; set; }
        public Fill Fill { get; set; } = new Fill();
        public float DashOn { get; set; } = 1;
        public float DashOff { get; set; } = 1;
        public Line() { }
        public Line(Color color) { Color = color; }
        public Line(Line rhs) { Color = rhs.Color; Width = rhs.Width; Style = rhs.Style; IsVisible = rhs.IsVisible; IsSmooth = rhs.IsSmooth; IsAntiAlias = rhs.IsAntiAlias; }
        public object Clone() { return new Line(this); }
    }

    [Serializable]
    public class Symbol : ICloneable
    {
        public SymbolType Type { get; set; } = SymbolType.Square;
        public float Size { get; set; } = 7;
        public bool IsVisible { get; set; } = true;
        public bool IsAntiAlias { get; set; }
        public Fill Fill { get; set; } = new Fill(Color.White);
        public Border Border { get; set; } = new Border();
        public Symbol() { }
        public Symbol(SymbolType type, Color color) { Type = type; Border = new Border(color, 1); }
        public Symbol(Symbol rhs) { Type = rhs.Type; Size = rhs.Size; IsVisible = rhs.IsVisible; Fill = new Fill(rhs.Fill); Border = new Border(rhs.Border); }
        public object Clone() { return new Symbol(this); }
        internal void Draw(Graphics g, float x, float y, float scale)
        {
            if (!IsVisible || Type == SymbolType.None) return;
            float s = Size * scale / 2;
            RectangleF r = new RectangleF(x - s, y - s, 2 * s, 2 * s);
            SmoothingMode mode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush fill = new SolidBrush(Fill.Color))
            using (Pen pen = new Pen(Border.Color, Border.Width))
            {
                switch (Type)
                {
                    case SymbolType.Circle:
                        if (Fill.IsVisible && Fill.Type != FillType.None) g.FillEllipse(fill, r);
                        if (Border.IsVisible) g.DrawEllipse(pen, r);
                        break;
                    case SymbolType.Diamond:
                        PointF[] d = { new PointF(x, y - s), new PointF(x + s, y), new PointF(x, y + s), new PointF(x - s, y) };
                        if (Fill.IsVisible && Fill.Type != FillType.None) g.FillPolygon(fill, d);
                        if (Border.IsVisible) g.DrawPolygon(pen, d);
                        break;
                    case SymbolType.Triangle:
                    case SymbolType.TriangleDown:
                        float sign = Type == SymbolType.Triangle ? 1 : -1;
                        PointF[] t = { new PointF(x, y - sign * s), new PointF(x + s, y + sign * s), new PointF(x - s, y + sign * s) };
                        if (Fill.IsVisible && Fill.Type != FillType.None) g.FillPolygon(fill, t);
                        if (Border.IsVisible) g.DrawPolygon(pen, t);
                        break;
                    case SymbolType.XCross:
                        g.DrawLine(pen, x - s, y - s, x + s, y + s);
                        g.DrawLine(pen, x - s, y + s, x + s, y - s);
                        break;
                    case SymbolType.Plus:
                        g.DrawLine(pen, x - s, y, x + s, y);
                        g.DrawLine(pen, x, y - s, x, y + s);
                        break;
                    case SymbolType.Star:
                        g.DrawLine(pen, x - s, y, x + s, y);
                        g.DrawLine(pen, x, y - s, x, y + s);
                        g.DrawLine(pen, x - s, y - s, x + s, y + s);
                        g.DrawLine(pen, x - s, y + s, x + s, y - s);
                        break;
                    case SymbolType.HDash: g.DrawLine(pen, x - s, y, x + s, y); break;
                    case SymbolType.VDash: g.DrawLine(pen, x, y - s, x, y + s); break;
                    default:
                        if (Fill.IsVisible && Fill.Type != FillType.None) g.FillRectangle(fill, r);
                        if (Border.IsVisible) g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
                        break;
                }
            }
            g.SmoothingMode = mode;
        }
    }

    // Points
    [Serializable]
    public class PointPairBase
    {
        public const double Missing = double.MaxValue;
        public double X { get; set; }
        public double Y { get; set; }
        public bool IsMissing { get { return X == Missing || Y == Missing || double.IsNaN(X) || double.IsNaN(Y); } }
        public bool IsInvalid { get { return IsMissing || double.IsInfinity(X) || double.IsInfinity(Y); } }
        public static bool IsValueInvalid(double value) { return value == Missing || double.IsNaN(value) || double.IsInfinity(value); }
    }

    [Serializable]
    public class PointPair : PointPairBase, ICloneable
    {
        public double Z { get; set; }
        public object Tag { get; set; }
        public PointPair() { }
        public PointPair(double x, double y) { X = x; Y = y; }
        public PointPair(double x, double y, string label) : this(x, y) { Tag = label; }
        public PointPair(double x, double y, double z) : this(x, y) { Z = z; }
        public PointPair(double x, double y, double z, object tag) : this(x, y, z) { Tag = tag; }
        public PointPair(PointF pt) : this(pt.X, pt.Y) { }
        public PointPair(PointPair rhs) : this(rhs.X, rhs.Y, rhs.Z, rhs.Tag) { }
        public object Clone() { return new PointPair(this); }
        public override string ToString() { return "( " + X.ToString("G4") + ", " + Y.ToString("G4") + " )"; }
    }

    public interface IPointList : ICloneable
    {
        PointPair this[int index] { get; }
        int Count { get; }
    }

    public interface IPointListEdit : IPointList
    {
        new PointPair this[int index] { get; set; }
        void Add(PointPair point);
        void Add(double x, double y);
        void RemoveAt(int index);
        void Clear();
    }

    [Serializable]
    public class PointPairList : List<PointPair>, IPointListEdit
    {
        public PointPairList() { }
        public PointPairList(double[] x, double[] y)
        {
            int n = Math.Min(x?.Length ?? 0, y?.Length ?? 0);
            for (int i = 0; i < n; i++) Add(x[i], y[i]);
        }
        public PointPairList(double[] x, double[] y, double[] baseVal)
        {
            int n = Math.Min(x.Length, y.Length);
            for (int i = 0; i < n; i++) Add(new PointPair(x[i], y[i], baseVal[i]));
        }
        public PointPairList(IPointList list) { for (int i = 0; i < list.Count; i++) Add(new PointPair(list[i])); }
        public void Add(double x, double y) { Add(new PointPair(x, y)); }
        public void Add(double x, double y, string tag) { Add(new PointPair(x, y, tag)); }
        public void Add(double x, double y, double z) { Add(new PointPair(x, y, z)); }
        public void Add(double[] x, double[] y) { for (int i = 0; i < Math.Min(x.Length, y.Length); i++) Add(x[i], y[i]); }
        public object Clone() { return new PointPairList(this); }
        public PointPairList Clone_() { return new PointPairList(this); }
        public void Sort(SortType type)
        {
            if (type == SortType.XValues) Sort((a, b) => a.X.CompareTo(b.X));
            else Sort((a, b) => a.Y.CompareTo(b.Y));
        }
        public void Interpolate(double x) { }
    }

    public enum SortType { XValues, YValues }

    // Curves
    [Serializable]
    public abstract class CurveItem : ICloneable
    {
        public Label Label { get; set; }
        public IPointList Points { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsY2Axis { get; set; }
        public bool IsX2Axis { get; set; }
        public int YAxisIndex { get; set; }
        public object Tag { get; set; }
        public bool IsSelected { get; set; }
        public bool IsSelectable { get; set; } = true;
        public bool IsOverrideOrdinal { get; set; }
        public Link Link { get; set; } = new Link();
        public abstract Color Color { get; set; }
        public int NPts { get { return Points?.Count ?? 0; } }
        public bool IsLine { get { return this is LineItem; } }
        public bool IsBar { get { return false; } }
        protected CurveItem(string label) { Label = new Label(label, "Arial", 12, Color.Black, false, false, false); Points = new PointPairList(); }
        protected CurveItem(string label, IPointList points) : this(label) { Points = points ?? new PointPairList(); }
        public void AddPoint(double x, double y) { (Points as IPointListEdit)?.Add(x, y); }
        public void AddPoint(PointPair point) { (Points as IPointListEdit)?.Add(point); }
        public void RemovePoint(int index) { (Points as IPointListEdit)?.RemoveAt(index); }
        public void Clear() { (Points as IPointListEdit)?.Clear(); }
        public abstract object Clone();
        public Axis GetYAxis(GraphPane pane) { return IsY2Axis ? pane.Y2Axis : pane.YAxis; }
        public Axis GetXAxis(GraphPane pane) { return pane.XAxis; }
        public void GetRange(out double xMin, out double xMax, out double yMin, out double yMax, bool ignoreInitial, bool isBoundedRanges, GraphPane pane)
        {
            xMin = yMin = double.MaxValue;
            xMax = yMax = double.MinValue;
            for (int i = 0; i < NPts; i++)
            {
                PointPair p = Points[i];
                if (p.IsInvalid) continue;
                xMin = Math.Min(xMin, p.X); xMax = Math.Max(xMax, p.X);
                yMin = Math.Min(yMin, p.Y); yMax = Math.Max(yMax, p.Y);
            }
        }
        internal abstract void Draw(Graphics g, GraphPane pane, float scale);
        internal abstract void DrawLegendKey(Graphics g, RectangleF rect, float scale);
    }

    [Serializable]
    public class Link
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public bool IsEnabled { get; set; }
    }

    [Serializable]
    public class LineItem : CurveItem
    {
        public Line Line { get; set; } = new Line();
        public Symbol Symbol { get; set; } = new Symbol();
        public override Color Color
        {
            get { return Line.Color; }
            set { Line.Color = value; Symbol.Border.Color = value; }
        }
        public LineItem(string label) : base(label) { }
        public LineItem(string label, double[] x, double[] y, Color color, SymbolType symbolType, float lineWidth)
            : this(label, new PointPairList(x, y), color, symbolType, lineWidth) { }
        public LineItem(string label, double[] x, double[] y, Color color, SymbolType symbolType)
            : this(label, new PointPairList(x, y), color, symbolType, 1) { }
        public LineItem(string label, IPointList points, Color color, SymbolType symbolType, float lineWidth) : base(label, points)
        {
            Line = new Line(color) { Width = lineWidth };
            Symbol = new Symbol(symbolType, color);
        }
        public LineItem(string label, IPointList points, Color color, SymbolType symbolType) : this(label, points, color, symbolType, 1) { }
        public LineItem(LineItem rhs) : base(rhs.Label.Text, rhs.Points?.Clone() as IPointList)
        {
            Label = new Label(rhs.Label);
            Line = new Line(rhs.Line);
            Symbol = new Symbol(rhs.Symbol);
            IsVisible = rhs.IsVisible;
            IsY2Axis = rhs.IsY2Axis;
            Tag = rhs.Tag;
        }
        public override object Clone() { return new LineItem(this); }
        internal override void Draw(Graphics g, GraphPane pane, float scale)
        {
            if (!IsVisible || Points == null) return;
            Axis yAxis = GetYAxis(pane);
            List<PointF> segment = new List<PointF>();
            using (Pen pen = new Pen(Line.Color, Math.Max(1, pane.IsPenWidthScaled ? Line.Width * scale : Line.Width)) { DashStyle = Line.Style })
            {
                SmoothingMode mode = g.SmoothingMode;
                g.SmoothingMode = Line.IsAntiAlias || true ? SmoothingMode.AntiAlias : SmoothingMode.None;
                void Flush()
                {
                    if (Line.IsVisible && segment.Count > 1) g.DrawLines(pen, segment.ToArray());
                    segment.Clear();
                }
                for (int i = 0; i < Points.Count; i++)
                {
                    PointPair p = Points[i];
                    if (p.IsInvalid || (pane.XAxis.Type == AxisType.Log && p.X <= 0) || (yAxis.Type == AxisType.Log && p.Y <= 0))
                    {
                        Flush();
                        continue;
                    }
                    segment.Add(new PointF(pane.XAxis.Scale.Transform(p.X), yAxis.Scale.Transform(p.Y)));
                }
                Flush();
                g.SmoothingMode = mode;
            }
            if (Symbol.IsVisible && Symbol.Type != SymbolType.None)
            {
                for (int i = 0; i < Points.Count; i++)
                {
                    PointPair p = Points[i];
                    if (p.IsInvalid) continue;
                    Symbol.Draw(g, pane.XAxis.Scale.Transform(p.X), yAxis.Scale.Transform(p.Y), scale);
                }
            }
        }
        internal override void DrawLegendKey(Graphics g, RectangleF rect, float scale)
        {
            float y = rect.Y + rect.Height / 2;
            if (Line.IsVisible)
                using (Pen pen = new Pen(Line.Color, Math.Max(1, Line.Width)) { DashStyle = Line.Style }) g.DrawLine(pen, rect.Left, y, rect.Right, y);
            if (Symbol.IsVisible && Symbol.Type != SymbolType.None) Symbol.Draw(g, rect.X + rect.Width / 2, y, scale);
        }
    }

    public class CurveList : List<CurveItem>, ICloneable
    {
        public CurveItem this[string label] { get { return this.FirstOrDefault(c => c.Label.Text == label); } }
        public int IndexOf(string label) { return FindIndex(c => c.Label.Text == label); }
        public int IndexOfTag(string tag) { return FindIndex(c => Equals(c.Tag, tag)); }
        public int Move(int index, int relativePos)
        {
            if (index < 0 || index >= Count) return -1;
            CurveItem curve = this[index];
            RemoveAt(index);
            int newIndex = Math.Max(0, Math.Min(Count, index + relativePos));
            Insert(newIndex, curve);
            return newIndex;
        }
        public object Clone() { CurveList list = new CurveList(); foreach (CurveItem c in this) list.Add((CurveItem)c.Clone()); return list; }
        public bool HasData() { return this.Any(c => c.NPts > 0); }
    }

    // Graph objects
    public class Location : ICloneable
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public CoordType CoordinateFrame { get; set; }
        public AlignH AlignH { get; set; }
        public AlignV AlignV { get; set; }
        public Location() { }
        public Location(double x, double y, CoordType coordType) { X = (float)x; Y = (float)y; CoordinateFrame = coordType; }
        public Location(double x, double y, CoordType coordType, AlignH alignH, AlignV alignV) : this(x, y, coordType) { AlignH = alignH; AlignV = alignV; }
        public object Clone() { return MemberwiseClone(); }
    }

    public abstract class GraphObj : ICloneable
    {
        public Location Location { get; set; } = new Location();
        public bool IsVisible { get; set; } = true;
        public object Tag { get; set; }
        public ZOrder ZOrder { get; set; } = ZOrder.A_InFront;
        public bool IsClippedToChartRect { get; set; }
        public abstract object Clone();
        internal abstract void Draw(Graphics g, GraphPane pane, float scale);
        internal PointF Position(GraphPane pane, float x, float y)
        {
            RectangleF chart = pane.Chart.Rect;
            RectangleF rect = pane.Rect;
            switch (Location.CoordinateFrame)
            {
                case CoordType.AxisXYScale: return new PointF(pane.XAxis.Scale.Transform(x), pane.YAxis.Scale.Transform(y));
                case CoordType.AxisXY2Scale: return new PointF(pane.XAxis.Scale.Transform(x), pane.Y2Axis.Scale.Transform(y));
                case CoordType.PaneFraction: return new PointF(rect.X + x * rect.Width, rect.Y + y * rect.Height);
                case CoordType.XScaleYChartFraction: return new PointF(pane.XAxis.Scale.Transform(x), chart.Y + y * chart.Height);
                case CoordType.XChartFractionYScale: return new PointF(chart.X + x * chart.Width, pane.YAxis.Scale.Transform(y));
                default: return new PointF(chart.X + x * chart.Width, chart.Y + y * chart.Height);
            }
        }
    }

    public class TextObj : GraphObj
    {
        public string Text { get; set; }
        public FontSpec FontSpec { get; set; } = new FontSpec("Arial", 12, Color.Black, false, false, false);
        public TextObj() { }
        public TextObj(string text, double x, double y) { Text = text; Location = new Location(x, y, CoordType.AxisXYScale); }
        public TextObj(string text, double x, double y, CoordType coordType) { Text = text; Location = new Location(x, y, coordType); }
        public TextObj(string text, double x, double y, CoordType coordType, AlignH alignH, AlignV alignV) { Text = text; Location = new Location(x, y, coordType, alignH, alignV); }
        public override object Clone() { return new TextObj(Text, Location.X, Location.Y, Location.CoordinateFrame, Location.AlignH, Location.AlignV) { FontSpec = new FontSpec(FontSpec) }; }
        internal override void Draw(Graphics g, GraphPane pane, float scale)
        {
            if (!IsVisible) return;
            PointF p = Position(pane, Location.X, Location.Y);
            FontSpec.Draw(g, Text, p.X, p.Y, Location.AlignH, Location.AlignV, scale, FontSpec.Angle);
        }
    }

    public class LineObj : GraphObj
    {
        public Line Line { get; set; } = new Line(Color.Black);
        public LineObj() { }
        public LineObj(Color color, double x1, double y1, double x2, double y2)
        {
            Line = new Line(color);
            Location = new Location(x1, y1, CoordType.AxisXYScale) { Width = (float)(x2 - x1), Height = (float)(y2 - y1) };
        }
        public override object Clone() { return new LineObj { Line = new Line(Line), Location = (Location)Location.Clone() }; }
        internal override void Draw(Graphics g, GraphPane pane, float scale)
        {
            if (!IsVisible) return;
            PointF a = Position(pane, Location.X, Location.Y), b = Position(pane, Location.X + Location.Width, Location.Y + Location.Height);
            using (Pen pen = new Pen(Line.Color, Line.Width) { DashStyle = Line.Style }) g.DrawLine(pen, a, b);
        }
    }

    public class GraphObjList : List<GraphObj>, ICloneable
    {
        public GraphObj this[string tag] { get { return this.FirstOrDefault(o => Equals(o.Tag, tag)); } }
        public object Clone() { GraphObjList l = new GraphObjList(); foreach (GraphObj o in this) l.Add((GraphObj)o.Clone()); return l; }
    }

    // Axes
    [Serializable]
    public class Scale
    {
        private readonly Axis _owner;
        internal float MinPix, MaxPix;      // pixel positions of Min and Max
        public double Min { get; set; }
        public double Max { get; set; } = 1;
        public bool MinAuto { get; set; } = true;
        public bool MaxAuto { get; set; } = true;
        public double MajorStep { get; set; } = 0.1;
        public double MinorStep { get; set; } = 0.02;
        public bool MajorStepAuto { get; set; } = true;
        public bool MinorStepAuto { get; set; } = true;
        public double BaseTic { get; set; } = double.MaxValue;
        public string Format { get; set; } = "g";
        public bool FormatAuto { get; set; } = true;
        public int Mag { get; set; }
        public bool MagAuto { get; set; } = true;
        public double MinGrace { get; set; } = 0.1;
        public double MaxGrace { get; set; } = 0.1;
        public bool IsReverse { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsLabelsInside { get; set; }
        public bool IsSkipFirstLabel { get; set; }
        public bool IsSkipLastLabel { get; set; }
        public bool IsUseTenPower { get; set; }
        public bool IsPreventLabelOverlap { get; set; } = true;
        public float LabelGap { get; set; } = 0.3f;
        public AlignP Align { get; set; }
        public AlignH AlignH { get; set; } = AlignH.Center;
        public FontSpec FontSpec { get; set; } = new FontSpec("Arial", 14, Color.Black, false, false, false);
        public string[] TextLabels { get; set; }
        internal Scale(Axis owner) { _owner = owner; }
        internal Scale(Axis owner, Scale rhs) : this(owner)
        {
            Min = rhs.Min; Max = rhs.Max; MinAuto = rhs.MinAuto; MaxAuto = rhs.MaxAuto; MajorStep = rhs.MajorStep; MinorStep = rhs.MinorStep;
            MajorStepAuto = rhs.MajorStepAuto; MinorStepAuto = rhs.MinorStepAuto; Format = rhs.Format; FontSpec = new FontSpec(rhs.FontSpec); IsReverse = rhs.IsReverse;
        }
        private bool IsLog { get { return _owner.Type == AxisType.Log; } }
        private double Linearize(double value) { return IsLog ? Math.Log10(Math.Max(value, double.Epsilon)) : value; }
        public float Transform(double value)
        {
            double min = Linearize(Min), max = Linearize(Max);
            if (max == min) return MinPix;
            double ratio = (Linearize(value) - min) / (max - min);
            if (IsReverse) ratio = 1 - ratio;
            return (float)(MinPix + ratio * (MaxPix - MinPix));
        }
        public float Transform(bool isOverrideOrdinal, int i, double x) { return Transform(x); }
        public double ReverseTransform(float pixVal)
        {
            double min = Linearize(Min), max = Linearize(Max);
            if (MaxPix == MinPix) return Min;
            double ratio = (pixVal - MinPix) / (MaxPix - MinPix);
            if (IsReverse) ratio = 1 - ratio;
            double v = min + ratio * (max - min);
            return IsLog ? Math.Pow(10, v) : v;
        }
        public double Linearize_(double value) { return Linearize(value); }
        // Range of the data with grace and nice values like ZedGraph
        internal void PickScale(double dataMin, double dataMax)
        {
            if (dataMin > dataMax) { dataMin = 0; dataMax = 1; }
            if (IsLog)
            {
                if (dataMin <= 0) dataMin = dataMax > 0 ? dataMax / 1000 : 1;
                if (dataMax <= 0) dataMax = 10;
                if (MinAuto) Min = Math.Pow(10, Math.Floor(Math.Log10(dataMin)));
                if (MaxAuto) Max = Math.Pow(10, Math.Ceiling(Math.Log10(dataMax)));
                if (Max <= Min) Max = Min * 10;
                if (MajorStepAuto) MajorStep = 1;
                if (MinorStepAuto) MinorStep = 1;
                return;
            }
            double range = dataMax - dataMin;
            if (range == 0)
            {
                double d = dataMin == 0 ? 1 : Math.Abs(dataMin) * 0.2;
                dataMin -= d;
                dataMax += d;
                range = dataMax - dataMin;
            }
            double min = dataMin, max = dataMax;
            if (MinAuto && !(dataMin >= 0 && dataMin - MinGrace * range < 0)) min = dataMin - MinGrace * range;
            else if (MinAuto) min = 0;
            if (MaxAuto && !(dataMax <= 0 && dataMax + MaxGrace * range > 0)) max = dataMax + MaxGrace * range;
            else if (MaxAuto) max = 0;
            double useMin = MinAuto ? min : Min, useMax = MaxAuto ? max : Max;
            if (MajorStepAuto) MajorStep = NiceStep(Math.Abs(useMax - useMin), 7);
            if (MinorStepAuto) MinorStep = MajorStep / (Math.Abs(MajorStep / Math.Pow(10, Math.Floor(Math.Log10(MajorStep))) - 2) < 1e-9 ? 4 : 5);
            if (MinAuto) Min = Math.Floor(useMin / MajorStep + 1e-9) * MajorStep;
            if (MaxAuto) Max = Math.Ceiling(useMax / MajorStep - 1e-9) * MajorStep;
            if (Max <= Min) Max = Min + MajorStep;
        }
        internal static double NiceStep(double range, double targetSteps)
        {
            if (range <= 0 || double.IsNaN(range) || double.IsInfinity(range)) return 1;
            double rough = range / targetSteps;
            double mag = Math.Pow(10, Math.Floor(Math.Log10(rough)));
            double norm = rough / mag;
            double nice = norm < 1.5 ? 1 : norm < 3.5 ? 2 : norm < 7.5 ? 5 : 10;
            return nice * mag;
        }
        internal IEnumerable<double> MajorTics()
        {
            if (IsLog)
            {
                for (double e = Math.Floor(Math.Log10(Min)); e <= Math.Ceiling(Math.Log10(Max)) + 1e-9; e += Math.Max(1, MajorStep))
                {
                    double v = Math.Pow(10, e);
                    if (v >= Min * (1 - 1e-9) && v <= Max * (1 + 1e-9)) yield return v;
                }
                yield break;
            }
            if (MajorStep <= 0 || double.IsNaN(MajorStep)) yield break;
            double start = BaseTic != double.MaxValue ? BaseTic - Math.Floor((BaseTic - Min) / MajorStep) * MajorStep : Math.Ceiling(Min / MajorStep - 1e-9) * MajorStep;
            int count = 0;
            for (double v = start; v <= Max + MajorStep * 1e-9 && count < 1000; v += MajorStep, count++)
                yield return Math.Abs(v) < MajorStep * 1e-10 ? 0 : v;
        }
        internal IEnumerable<double> MinorTics()
        {
            if (IsLog)
            {
                for (double e = Math.Floor(Math.Log10(Min)); e <= Math.Ceiling(Math.Log10(Max)); e++)
                    for (int k = 2; k < 10; k++)
                    {
                        double v = k * Math.Pow(10, e);
                        if (v > Min && v < Max) yield return v;
                    }
                yield break;
            }
            if (MinorStep <= 0 || double.IsNaN(MinorStep)) yield break;
            double start = Math.Ceiling(Min / MinorStep - 1e-9) * MinorStep;
            int count = 0;
            for (double v = start; v <= Max + MinorStep * 1e-9 && count < 5000; v += MinorStep, count++) yield return v;
        }
        internal string FormatLabel(double value)
        {
            if (IsLog) return value.ToString("g", CultureInfo.CurrentCulture);
            double magnitude = Math.Max(Math.Abs(Min), Math.Abs(Max));
            if (!FormatAuto) return value.ToString(Format, CultureInfo.CurrentCulture);
            if (magnitude >= 1e5 || (magnitude < 1e-3 && magnitude > 0)) return value.ToString("0.###E+0", CultureInfo.CurrentCulture);
            int decimals = MajorStep > 0 ? Math.Max(0, (int)-Math.Floor(Math.Log10(MajorStep) + 1e-9)) : 0;
            if (MajorStep / Math.Pow(10, Math.Floor(Math.Log10(MajorStep))) > 1.01 && MajorStep / Math.Pow(10, Math.Floor(Math.Log10(MajorStep))) < 2.01 && decimals > 0) { }
            return value.ToString("F" + Math.Min(decimals, 10), CultureInfo.CurrentCulture);
        }
    }

    public enum AlignP { Inside, Center, Outside }

    [Serializable]
    public class MajorGrid
    {
        public bool IsVisible { get; set; }
        public Color Color { get; set; } = Color.Black;
        public float DashOn { get; set; } = 1;
        public float DashOff { get; set; } = 5;
        public float PenWidth { get; set; } = 1;
        public bool IsZeroLine { get; set; }
    }

    [Serializable]
    public class MinorGrid
    {
        public bool IsVisible { get; set; }
        public Color Color { get; set; } = Color.Gray;
        public float DashOn { get; set; } = 1;
        public float DashOff { get; set; } = 10;
        public float PenWidth { get; set; } = 1;
    }

    [Serializable]
    public class MinorTic
    {
        public bool IsOutside { get; set; } = true;
        public bool IsInside { get; set; } = true;
        public bool IsOpposite { get; set; } = true;
        public bool IsCrossOutside { get; set; }
        public bool IsCrossInside { get; set; }
        public float Size { get; set; } = 2.5f;
        public float PenWidth { get; set; } = 1;
        public Color Color { get; set; } = Color.Black;
        public bool IsAllTics
        {
            set { IsOutside = IsInside = IsOpposite = value; }
        }
    }

    [Serializable]
    public class MajorTic : MinorTic
    {
        public bool IsBetweenLabels { get; set; }
        public MajorTic() { Size = 5; }
    }

    [Serializable]
    public abstract class Axis
    {
        public AxisLabel Title { get; set; }
        public Scale Scale { get; }
        public MajorGrid MajorGrid { get; } = new MajorGrid();
        public MinorGrid MinorGrid { get; } = new MinorGrid();
        public MajorTic MajorTic { get; } = new MajorTic();
        public MinorTic MinorTic { get; } = new MinorTic();
        public bool IsVisible { get; set; } = true;
        public bool IsAxisSegmentVisible { get; set; } = true;
        public Color Color { get; set; } = Color.Black;
        public AxisType Type { get; set; } = AxisType.Linear;
        public double Cross { get; set; }
        public bool CrossAuto { get; set; } = true;
        public float MinSpace { get; set; }
        public object Tag { get; set; }
        protected Axis(string title)
        {
            Title = new AxisLabel(title, "Arial", 14, Color.Black, true, false, false);
            Scale = new Scale(this);
        }
        public void ResetAutoScale(GraphPane pane, Graphics g) { Scale.MinAuto = Scale.MaxAuto = Scale.MajorStepAuto = Scale.MinorStepAuto = true; }
    }

    [Serializable]
    public class XAxis : Axis
    {
        public XAxis() : base("X Axis") { }
        public XAxis(string title) : base(title) { }
    }

    [Serializable]
    public class YAxis : Axis
    {
        public YAxis() : base("Y Axis") { }
        public YAxis(string title) : base(title) { }
    }

    [Serializable]
    public class Y2Axis : Axis
    {
        public Y2Axis() : base("Y2 Axis") { IsVisible = false; }
        public Y2Axis(string title) : base(title) { IsVisible = false; }
    }

    public class YAxisList : List<YAxis> { }
    public class Y2AxisList : List<Y2Axis> { }

    [Serializable]
    public class Legend
    {
        public bool IsVisible { get; set; } = true;
        public LegendPos Position { get; set; } = LegendPos.Top;
        public FontSpec FontSpec { get; set; } = new FontSpec("Arial", 12, Color.Black, false, false, false);
        public Fill Fill { get; set; } = new Fill(Color.White);
        public Border Border { get; set; } = new Border();
        public Location Location { get; set; } = new Location();
        public bool IsHStack { get; set; } = true;
        public bool IsShowLegendSymbols { get; set; } = true;
        public bool IsReverse { get; set; }
        public float Gap { get; set; } = 0.5f;
        public RectangleF Rect { get; internal set; }
    }

    [Serializable]
    public class Chart
    {
        public Fill Fill { get; set; } = new Fill(Color.White, Color.LightGoldenrodYellow, 45);
        public Border Border { get; set; } = new Border();
        public RectangleF Rect { get; set; }
        public bool IsRectAuto { get; set; } = true;
    }

    public class Margin
    {
        public float Left { get; set; } = 10;
        public float Right { get; set; } = 10;
        public float Top { get; set; } = 10;
        public float Bottom { get; set; } = 10;
        public float All { set { Left = Right = Top = Bottom = value; } }
    }

    // Panes
    [Serializable]
    public abstract class PaneBase
    {
        public GapLabel Title { get; set; }
        public Fill Fill { get; set; } = new Fill(Color.White, Color.LightGray, 45);
        public Border Border { get; set; } = new Border();
        public RectangleF Rect { get; set; }
        public Margin Margin { get; set; } = new Margin();
        public GraphObjList GraphObjList { get; set; } = new GraphObjList();
        public object Tag { get; set; }
        public bool IsFontsScaled { get; set; } = true;
        public bool IsPenWidthScaled { get; set; }
        public float BaseDimension { get; set; } = 8;
        public float TitleGap { get; set; } = 0.5f;
        protected PaneBase(string title, RectangleF rect)
        {
            Title = new GapLabel(title, "Arial", 16, Color.Black, true, false, false);
            Rect = rect;
        }
        // ZedGraph scales the fonts with the size of the pane (base dimension in inches at 72 points per inch)
        public float CalcScaleFactor()
        {
            if (!IsFontsScaled) return 96f / 72f;
            float length = Math.Min(Rect.Width, Rect.Height);
            float scale = length / (BaseDimension * 72f);
            return Math.Max(0.1f, Math.Min(scale, 2f)) * 96f / 72f;
        }
        public void ReSize(Graphics g, RectangleF rect) { Rect = rect; }
    }

    [Serializable]
    public class GraphPane : PaneBase
    {
        public XAxis XAxis { get; } = new XAxis();
        public YAxis YAxis { get; } = new YAxis();
        public Y2Axis Y2Axis { get; } = new Y2Axis();
        public YAxisList YAxisList { get; } = new YAxisList();
        public Y2AxisList Y2AxisList { get; } = new Y2AxisList();
        public CurveList CurveList { get; } = new CurveList();
        public Legend Legend { get; } = new Legend();
        public Chart Chart { get; } = new Chart();
        public ZoomStateStack ZoomStack { get; } = new ZoomStateStack();
        public bool IsIgnoreInitial { get; set; }
        public bool IsBoundedRanges { get; set; }
        public bool IsAlignGrids { get; set; }
        public bool IsIgnoreMissing { get; set; }
        public LineType LineType { get; set; }
        public GraphPane() : this(new RectangleF(0, 0, 500, 375), "", "", "") { }
        public GraphPane(RectangleF rect, string title, string xTitle, string yTitle) : base(title, rect)
        {
            XAxis.Title.Text = xTitle;
            YAxis.Title.Text = yTitle;
            YAxisList.Add(YAxis);
            Y2AxisList.Add(Y2Axis);
        }
        public LineItem AddCurve(string label, double[] x, double[] y, Color color)
        {
            return AddCurve(label, new PointPairList(x, y), color, SymbolType.Default);
        }
        public LineItem AddCurve(string label, double[] x, double[] y, Color color, SymbolType symbolType)
        {
            return AddCurve(label, new PointPairList(x, y), color, symbolType);
        }
        public LineItem AddCurve(string label, IPointList points, Color color) { return AddCurve(label, points, color, SymbolType.Default); }
        public LineItem AddCurve(string label, IPointList points, Color color, SymbolType symbolType)
        {
            LineItem curve = new LineItem(label, points, color, symbolType == SymbolType.Default ? SymbolType.Square : symbolType);
            CurveList.Add(curve);
            return curve;
        }
        // Updates the automatic scale ranges from the data of the curves
        public void AxisChange()
        {
            double xMin = double.MaxValue, xMax = double.MinValue, yMin = double.MaxValue, yMax = double.MinValue, y2Min = double.MaxValue, y2Max = double.MinValue;
            foreach (CurveItem c in CurveList)
            {
                if (!c.IsVisible && IsIgnoreMissing) continue;
                c.GetRange(out double x0, out double x1, out double y0, out double y1, IsIgnoreInitial, IsBoundedRanges, this);
                if (x0 > x1) continue;
                if (XAxis.Type == AxisType.Log)
                {
                    for (int i = 0; i < c.NPts; i++) if (c.Points[i].X > 0) { x0 = Math.Min(x0 <= 0 ? double.MaxValue : x0, c.Points[i].X); }
                }
                xMin = Math.Min(xMin, x0); xMax = Math.Max(xMax, x1);
                if (c.IsY2Axis) { y2Min = Math.Min(y2Min, y0); y2Max = Math.Max(y2Max, y1); }
                else { yMin = Math.Min(yMin, y0); yMax = Math.Max(yMax, y1); }
            }
            if (xMin > xMax) { xMin = 0; xMax = 1; }
            if (yMin > yMax) { yMin = 0; yMax = 1; }
            if (y2Min > y2Max) { y2Min = yMin; y2Max = yMax; }
            double graceX = XAxis.Scale.MinGrace;
            XAxis.Scale.MinGrace = 0;
            XAxis.Scale.MaxGrace = 0;
            XAxis.Scale.PickScale(xMin, xMax);
            XAxis.Scale.MinGrace = graceX;
            YAxis.Scale.PickScale(yMin, yMax);
            Y2Axis.Scale.PickScale(y2Min, y2Max);
        }
        public void AxisChange(Graphics g) { AxisChange(); }
        public bool FindNearestObject(PointF mousePt, Graphics g, out object nearestObj, out int index)
        {
            nearestObj = null;
            index = -1;
            float best = 7;
            foreach (CurveItem c in CurveList)
            {
                if (!c.IsVisible) continue;
                Axis yAxis = c.GetYAxis(this);
                for (int i = 0; i < c.NPts; i++)
                {
                    PointPair p = c.Points[i];
                    if (p.IsInvalid) continue;
                    float x = XAxis.Scale.Transform(p.X), y = yAxis.Scale.Transform(p.Y);
                    float d = (float)Math.Sqrt((x - mousePt.X) * (x - mousePt.X) + (y - mousePt.Y) * (y - mousePt.Y));
                    if (d < best) { best = d; nearestObj = c; index = i; }
                }
            }
            if (nearestObj == null && Legend.IsVisible && Legend.Rect.Contains(mousePt)) nearestObj = Legend;
            if (nearestObj == null && Chart.Rect.Contains(mousePt)) nearestObj = this;
            return nearestObj != null;
        }
        public bool FindNearestPoint(PointF mousePt, out CurveItem nearestCurve, out int iNearest)
        {
            bool found = FindNearestObject(mousePt, null, out object obj, out iNearest);
            nearestCurve = obj as CurveItem;
            return found && nearestCurve != null;
        }
        public void ReverseTransform(PointF ptF, out double x, out double y)
        {
            x = XAxis.Scale.ReverseTransform(ptF.X);
            y = YAxis.Scale.ReverseTransform(ptF.Y);
        }
        public void ReverseTransform(PointF ptF, out double x, out double x2, out double y, out double y2)
        {
            x = x2 = XAxis.Scale.ReverseTransform(ptF.X);
            y = YAxis.Scale.ReverseTransform(ptF.Y);
            y2 = Y2Axis.Scale.ReverseTransform(ptF.Y);
        }
        public PointF GeneralTransform(double x, double y, CoordType coord)
        {
            return new PointF(XAxis.Scale.Transform(x), (coord == CoordType.AxisXY2Scale ? (Axis)Y2Axis : YAxis).Scale.Transform(y));
        }

        // Drawing
        public void Draw(Graphics g)
        {
            float scale = CalcScaleFactor();
            RectangleF rect = Rect;
            Fill.Draw(g, rect);
            if (Border.IsVisible) Border.Draw(g, new RectangleF(rect.X, rect.Y, rect.Width - 1, rect.Height - 1));
            float top = rect.Y + Margin.Top * scale;
            if (Title.IsVisible && !string.IsNullOrEmpty(Title.Text))
            {
                SizeF size = Title.FontSpec.Measure(g, Title.Text, scale);
                Title.FontSpec.Draw(g, Title.Text, rect.X + rect.Width / 2, top, AlignH.Center, AlignV.Top, scale);
                top += size.Height * (1 + TitleGap);
            }
            // Legend size
            float legendHeight = 0, legendWidth = 0;
            List<CurveItem> legendCurves = CurveList.Where(c => c.Label.IsVisible && !string.IsNullOrEmpty(c.Label.Text)).ToList();
            float keyWidth = 30 * scale / 1.33f;
            float entryHeight = 0;
            List<float> entryWidths = new List<float>();
            if (Legend.IsVisible && legendCurves.Count > 0)
            {
                foreach (CurveItem c in legendCurves)
                {
                    SizeF s = (c.Label.FontSpec ?? Legend.FontSpec).Measure(g, c.Label.Text, scale);
                    entryWidths.Add(s.Width + keyWidth + 8);
                    entryHeight = Math.Max(entryHeight, s.Height);
                }
            }
            float left = rect.X + Margin.Left * scale;
            float right = rect.Right - Margin.Right * scale;
            float bottom = rect.Bottom - Margin.Bottom * scale;
            int legendColumns = 1, legendRows = 0;
            bool legendOutsideTop = Legend.Position == LegendPos.Top || Legend.Position == LegendPos.TopCenter || Legend.Position == LegendPos.TopFlushLeft;
            bool legendOutsideBottom = Legend.Position == LegendPos.Bottom || Legend.Position == LegendPos.BottomCenter || Legend.Position == LegendPos.BottomFlushLeft;
            bool legendOutsideSide = Legend.Position == LegendPos.Left || Legend.Position == LegendPos.Right;
            if (entryWidths.Count > 0)
            {
                float maxEntry = entryWidths.Max();
                if (Legend.IsHStack && !legendOutsideSide)
                {
                    legendColumns = Math.Max(1, Math.Min(entryWidths.Count, (int)((right - left - 8) / maxEntry)));
                }
                legendRows = (entryWidths.Count + legendColumns - 1) / legendColumns;
                legendWidth = legendColumns * maxEntry + 8;
                legendHeight = legendRows * entryHeight + 6;
                if (legendOutsideTop) top += legendHeight + 4;
                else if (legendOutsideBottom) bottom -= legendHeight + 4;
                else if (Legend.Position == LegendPos.Left) left += legendWidth + 4;
                else if (Legend.Position == LegendPos.Right) right -= legendWidth + 4;
            }
            // Axis space
            float xLabelHeight = XAxis.IsVisible && XAxis.Scale.IsVisible ? XAxis.Scale.FontSpec.Measure(g, "0", scale).Height : 0;
            float xTitleHeight = XAxis.IsVisible && XAxis.Title.IsVisible && !string.IsNullOrEmpty(XAxis.Title.Text) ? XAxis.Title.FontSpec.Measure(g, XAxis.Title.Text, scale).Height : 0;
            float yTitleHeight = YAxis.IsVisible && YAxis.Title.IsVisible && !string.IsNullOrEmpty(YAxis.Title.Text) ? YAxis.Title.FontSpec.Measure(g, YAxis.Title.Text, scale).Height : 0;
            float yLabelWidth = 0;
            if (YAxis.IsVisible && YAxis.Scale.IsVisible)
                foreach (double v in YAxis.Scale.MajorTics()) yLabelWidth = Math.Max(yLabelWidth, YAxis.Scale.FontSpec.Measure(g, YAxis.Scale.FormatLabel(v), scale).Width);
            float y2Width = 0;
            if (Y2Axis.IsVisible)
            {
                foreach (double v in Y2Axis.Scale.MajorTics()) y2Width = Math.Max(y2Width, Y2Axis.Scale.FontSpec.Measure(g, Y2Axis.Scale.FormatLabel(v), scale).Width);
                y2Width += 8 + (Y2Axis.Title.IsVisible ? Y2Axis.Title.FontSpec.Measure(g, Y2Axis.Title.Text ?? "", scale).Height : 0);
            }
            float tic = XAxis.MajorTic.Size * scale;
            RectangleF chart = Chart.IsRectAuto ? RectangleF.FromLTRB(left + yTitleHeight + yLabelWidth + tic + 6, top + 4,
                                                                      right - Math.Max(y2Width, 8), bottom - xLabelHeight - xTitleHeight - tic - 6) : Chart.Rect;
            if (chart.Width < 10 || chart.Height < 10) chart = RectangleF.FromLTRB(left, top, Math.Max(left + 10, right), Math.Max(top + 10, bottom));
            Chart.Rect = chart;
            XAxis.Scale.MinPix = chart.Left; XAxis.Scale.MaxPix = chart.Right;
            YAxis.Scale.MinPix = chart.Bottom; YAxis.Scale.MaxPix = chart.Top;
            Y2Axis.Scale.MinPix = chart.Bottom; Y2Axis.Scale.MaxPix = chart.Top;
            Chart.Fill.Draw(g, chart);
            DrawGrids(g, chart);
            foreach (GraphObj o in GraphObjList) if (o.ZOrder >= ZOrder.E_BehindCurves) o.Draw(g, this, scale);
            GraphicsState state = g.Save();
            g.SetClip(chart);
            for (int i = CurveList.Count - 1; i >= 0; i--) CurveList[i].Draw(g, this, scale);
            g.Restore(state);
            if (Chart.Border.IsVisible) Chart.Border.Draw(g, chart);
            DrawAxis(g, XAxis, chart, scale, true, false);
            DrawAxis(g, YAxis, chart, scale, false, false);
            if (Y2Axis.IsVisible) DrawAxis(g, Y2Axis, chart, scale, false, true);
            foreach (GraphObj o in GraphObjList) if (o.ZOrder < ZOrder.E_BehindCurves) o.Draw(g, this, scale);
            // Legend
            if (entryWidths.Count > 0)
            {
                float lx, ly;
                switch (Legend.Position)
                {
                    case LegendPos.Top: case LegendPos.TopCenter: lx = chart.X + (chart.Width - legendWidth) / 2; ly = top - legendHeight - 4; break;
                    case LegendPos.TopFlushLeft: lx = left; ly = top - legendHeight - 4; break;
                    case LegendPos.Bottom: case LegendPos.BottomCenter: lx = chart.X + (chart.Width - legendWidth) / 2; ly = bottom + 4; break;
                    case LegendPos.BottomFlushLeft: lx = left; ly = bottom + 4; break;
                    case LegendPos.Left: lx = rect.X + Margin.Left * scale; ly = chart.Y; break;
                    case LegendPos.Right: lx = right + 4; ly = chart.Y; break;
                    case LegendPos.InsideTopLeft: lx = chart.X + 4; ly = chart.Y + 4; break;
                    case LegendPos.InsideTopRight: lx = chart.Right - legendWidth - 4; ly = chart.Y + 4; break;
                    case LegendPos.InsideBotLeft: lx = chart.X + 4; ly = chart.Bottom - legendHeight - 4; break;
                    case LegendPos.InsideBotRight: lx = chart.Right - legendWidth - 4; ly = chart.Bottom - legendHeight - 4; break;
                    default: lx = chart.X + Legend.Location.X * chart.Width; ly = chart.Y + Legend.Location.Y * chart.Height; break;
                }
                RectangleF legendRect = new RectangleF(lx, ly, legendWidth, legendHeight);
                Legend.Rect = legendRect;
                Legend.Fill.Draw(g, legendRect);
                if (Legend.Border.IsVisible) Legend.Border.Draw(g, legendRect);
                float maxEntry = entryWidths.Max();
                for (int i = 0; i < legendCurves.Count; i++)
                {
                    int column = i % legendColumns, row = i / legendColumns;
                    float ex = lx + 4 + column * maxEntry, ey = ly + 3 + row * entryHeight;
                    CurveItem c = legendCurves[i];
                    if (Legend.IsShowLegendSymbols) c.DrawLegendKey(g, new RectangleF(ex, ey, keyWidth, entryHeight), scale);
                    (c.Label.FontSpec ?? Legend.FontSpec).Draw(g, c.Label.Text, ex + keyWidth + 4, ey + entryHeight / 2, AlignH.Left, AlignV.Center, scale);
                }
            }
            else Legend.Rect = RectangleF.Empty;
        }
        private void DrawGrids(Graphics g, RectangleF chart)
        {
            void Grid(Axis axis, bool horizontalAxis, bool major)
            {
                bool visible = major ? axis.MajorGrid.IsVisible : axis.MinorGrid.IsVisible;
                if (!visible || !axis.IsVisible) return;
                Color color = major ? axis.MajorGrid.Color : axis.MinorGrid.Color;
                float on = major ? axis.MajorGrid.DashOn : axis.MinorGrid.DashOn, off = major ? axis.MajorGrid.DashOff : axis.MinorGrid.DashOff;
                using (Pen pen = new Pen(color, major ? axis.MajorGrid.PenWidth : axis.MinorGrid.PenWidth))
                {
                    if (off > 0) { pen.DashStyle = DashStyle.Custom; pen.DashPattern = new[] { Math.Max(0.1f, on), off }; }
                    foreach (double v in major ? axis.Scale.MajorTics() : axis.Scale.MinorTics())
                    {
                        float p = axis.Scale.Transform(v);
                        if (horizontalAxis) { if (p > chart.Left + 0.5f && p < chart.Right - 0.5f) g.DrawLine(pen, p, chart.Top, p, chart.Bottom); }
                        else if (p > chart.Top + 0.5f && p < chart.Bottom - 0.5f) g.DrawLine(pen, chart.Left, p, chart.Right, p);
                    }
                }
            }
            Grid(XAxis, true, false);
            Grid(YAxis, false, false);
            Grid(XAxis, true, true);
            Grid(YAxis, false, true);
        }
        private void DrawAxis(Graphics g, Axis axis, RectangleF chart, float scale, bool horizontal, bool opposite)
        {
            if (!axis.IsVisible) return;
            using (Pen pen = new Pen(axis.Color))
            using (Pen ticPen = new Pen(axis.MajorTic.Color))
            {
                float majorSize = axis.MajorTic.Size * scale, minorSize = axis.MinorTic.Size * scale;
                foreach (double v in axis.Scale.MinorTics())
                {
                    float p = axis.Scale.Transform(v);
                    if (horizontal)
                    {
                        if (axis.MinorTic.IsOutside) g.DrawLine(ticPen, p, chart.Bottom, p, chart.Bottom + minorSize);
                        if (axis.MinorTic.IsInside) g.DrawLine(ticPen, p, chart.Bottom, p, chart.Bottom - minorSize);
                        if (axis.MinorTic.IsOpposite) g.DrawLine(ticPen, p, chart.Top, p, chart.Top + minorSize);
                    }
                    else
                    {
                        float x = opposite ? chart.Right : chart.Left;
                        float sign = opposite ? -1 : 1;
                        if (axis.MinorTic.IsOutside) g.DrawLine(ticPen, x, p, x - sign * minorSize, p);
                        if (axis.MinorTic.IsInside) g.DrawLine(ticPen, x, p, x + sign * minorSize, p);
                        if (axis.MinorTic.IsOpposite && !opposite && !Y2Axis.IsVisible) g.DrawLine(ticPen, chart.Right, p, chart.Right - minorSize, p);
                    }
                }
                float labelExtent = 0;
                foreach (double v in axis.Scale.MajorTics())
                {
                    float p = axis.Scale.Transform(v);
                    string label = axis.Scale.FormatLabel(v);
                    if (horizontal)
                    {
                        if (axis.MajorTic.IsOutside) g.DrawLine(ticPen, p, chart.Bottom, p, chart.Bottom + majorSize);
                        if (axis.MajorTic.IsInside) g.DrawLine(ticPen, p, chart.Bottom, p, chart.Bottom - majorSize);
                        if (axis.MajorTic.IsOpposite) g.DrawLine(ticPen, p, chart.Top, p, chart.Top + majorSize);
                        if (axis.Scale.IsVisible)
                        {
                            axis.Scale.FontSpec.Draw(g, label, p, chart.Bottom + majorSize + 2, AlignH.Center, AlignV.Top, scale);
                            labelExtent = Math.Max(labelExtent, axis.Scale.FontSpec.Measure(g, label, scale).Height);
                        }
                    }
                    else
                    {
                        float x = opposite ? chart.Right : chart.Left;
                        float sign = opposite ? -1 : 1;
                        if (axis.MajorTic.IsOutside) g.DrawLine(ticPen, x, p, x - sign * majorSize, p);
                        if (axis.MajorTic.IsInside) g.DrawLine(ticPen, x, p, x + sign * majorSize, p);
                        if (axis.MajorTic.IsOpposite && !opposite && !Y2Axis.IsVisible) g.DrawLine(ticPen, chart.Right, p, chart.Right - majorSize, p);
                        if (axis.Scale.IsVisible)
                        {
                            axis.Scale.FontSpec.Draw(g, label, x - sign * (majorSize + 3), p, opposite ? AlignH.Left : AlignH.Right, AlignV.Center, scale);
                            labelExtent = Math.Max(labelExtent, axis.Scale.FontSpec.Measure(g, label, scale).Width);
                        }
                    }
                }
                if (axis.Title.IsVisible && !string.IsNullOrEmpty(axis.Title.Text))
                {
                    if (horizontal) axis.Title.FontSpec.Draw(g, axis.Title.Text, chart.X + chart.Width / 2, chart.Bottom + majorSize + labelExtent + 4, AlignH.Center, AlignV.Top, scale);
                    else
                    {
                        float x = opposite ? chart.Right + majorSize + labelExtent + 6 : chart.Left - majorSize - labelExtent - 6;
                        axis.Title.FontSpec.Draw(g, axis.Title.Text, x, chart.Y + chart.Height / 2, AlignH.Center, opposite ? AlignV.Top : AlignV.Bottom, scale, opposite ? -90 : 90);
                    }
                }
            }
        }
    }

    public enum LineType { Normal, Stack }

    [Serializable]
    public class MasterPane : PaneBase
    {
        public List<GraphPane> PaneList { get; } = new List<GraphPane>();
        public MasterPane() : base("", new RectangleF(0, 0, 500, 375)) { Fill = new Fill(Color.White); Border = new Border { IsVisible = false }; }
        public GraphPane this[int index] { get { return PaneList[index]; } }
        public void Add(GraphPane pane) { PaneList.Add(pane); }
        public void SetLayout(Graphics g, PaneLayout layout) { }
        public void AxisChange() { foreach (GraphPane p in PaneList) p.AxisChange(); }
        public void AxisChange(Graphics g) { AxisChange(); }
        public Bitmap GetImage() { return GetImage((int)Rect.Width, (int)Rect.Height, 96); }
        public Bitmap GetImage(int width, int height, float dpi)
        {
            Bitmap bitmap = new Bitmap(Math.Max(1, width), Math.Max(1, height));
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                RectangleF old = Rect;
                Rect = new RectangleF(0, 0, width, height);
                foreach (GraphPane p in PaneList)
                {
                    RectangleF oldPane = p.Rect;
                    p.Rect = Rect;
                    p.Draw(g);
                    p.Rect = oldPane;
                }
                Rect = old;
            }
            return bitmap;
        }
    }

    public enum PaneLayout { ForceSquare, SquareColPreferred, SquareRowPreferred, SingleRow, SingleColumn, ExplicitCol12, ExplicitCol21, ExplicitCol23, ExplicitCol32, ExplicitRow12, ExplicitRow21, ExplicitRow23, ExplicitRow32 }

    // Zoom states
    public class ZoomState : ICloneable
    {
        public enum StateType { Zoom, WheelZoom, Pan, Scroll }
        private readonly double _xMin, _xMax, _yMin, _yMax;
        private readonly bool _xMinAuto, _xMaxAuto, _yMinAuto, _yMaxAuto;
        public StateType Type { get; }
        public string TypeString { get { return Type == StateType.Pan ? "Pan" : Type == StateType.Scroll ? "Scroll" : "Zoom"; } }
        public ZoomState(GraphPane pane, StateType type)
        {
            Type = type;
            _xMin = pane.XAxis.Scale.Min; _xMax = pane.XAxis.Scale.Max; _yMin = pane.YAxis.Scale.Min; _yMax = pane.YAxis.Scale.Max;
            _xMinAuto = pane.XAxis.Scale.MinAuto; _xMaxAuto = pane.XAxis.Scale.MaxAuto; _yMinAuto = pane.YAxis.Scale.MinAuto; _yMaxAuto = pane.YAxis.Scale.MaxAuto;
        }
        public void ApplyState(GraphPane pane)
        {
            pane.XAxis.Scale.Min = _xMin; pane.XAxis.Scale.Max = _xMax; pane.YAxis.Scale.Min = _yMin; pane.YAxis.Scale.Max = _yMax;
            pane.XAxis.Scale.MinAuto = _xMinAuto; pane.XAxis.Scale.MaxAuto = _xMaxAuto; pane.YAxis.Scale.MinAuto = _yMinAuto; pane.YAxis.Scale.MaxAuto = _yMaxAuto;
        }
        public bool IsChanged(GraphPane pane) { return pane.XAxis.Scale.Min != _xMin || pane.XAxis.Scale.Max != _xMax || pane.YAxis.Scale.Min != _yMin || pane.YAxis.Scale.Max != _yMax; }
        public object Clone() { return MemberwiseClone(); }
    }

    public class ZoomStateStack : List<ZoomState>
    {
        public bool IsEmpty { get { return Count == 0; } }
        public ZoomState Top { get { return Count > 0 ? this[Count - 1] : null; } }
        public ZoomState Push(GraphPane pane, ZoomState.StateType type)
        {
            ZoomState state = new ZoomState(pane, type);
            Add(state);
            return state;
        }
        public ZoomState Pop(GraphPane pane)
        {
            if (Count == 0) return null;
            ZoomState state = this[Count - 1];
            RemoveAt(Count - 1);
            state.ApplyState(pane);
            return state;
        }
        public ZoomState PopAll(GraphPane pane)
        {
            if (Count == 0) return null;
            ZoomState state = this[0];
            Clear();
            state.ApplyState(pane);
            return state;
        }
    }

    // The control
    public class ZedGraphControl : UserControl
    {
        private MasterPane _masterPane = new MasterPane();
        private Point _dragStart;
        private Rectangle _zoomRect;
        private bool _zooming, _panning;
        private double _panX0, _panX1, _panY0, _panY1;
        private readonly ContextMenuStrip _contextMenu = new ContextMenuStrip();
        private string _pointValueTip;

        public delegate bool ZedMouseEventHandler(ZedGraphControl sender, MouseEventArgs e);
        public delegate void ContextMenuBuilderEventHandler(ZedGraphControl sender, ContextMenuStrip menuStrip, Point mousePt, ContextMenuObjectState objState);
        public delegate void ZoomEventHandler(ZedGraphControl sender, ZoomState oldState, ZoomState newState);
        public delegate string PointValueHandler(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt);
        public delegate bool PointEditHandler(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt);
        public enum ContextMenuObjectState { InactiveSelection, ActiveSelection, Background }

        public event ZedMouseEventHandler MouseDownEvent;
        public event ZedMouseEventHandler MouseUpEvent;
        public event ZedMouseEventHandler MouseMoveEvent;
        public event ZedMouseEventHandler DoubleClickEvent;
        public event ContextMenuBuilderEventHandler ContextMenuBuilder;
        public event ZoomEventHandler ZoomEvent;
        public event PointValueHandler PointValueEvent;
        public event PointEditHandler PointEditEvent;

        public ZedGraphControl()
        {
            GraphPane pane = new GraphPane(new RectangleF(0, 0, Width, Height), "Title", "X Axis", "Y Axis");
            _masterPane.Add(pane);
            SetStyle(ControlStyles.Selectable, true);
            BackColor = Color.White;
        }
        protected override Size DefaultSize { get { return new Size(150, 150); } }
        public GraphPane GraphPane
        {
            get { return _masterPane.PaneList.Count > 0 ? _masterPane.PaneList[0] : null; }
            set
            {
                if (_masterPane.PaneList.Count > 0) _masterPane.PaneList[0] = value;
                else _masterPane.Add(value);
            }
        }
        public MasterPane MasterPane { get { return _masterPane; } set { _masterPane = value; } }
        public bool IsShowPointValues { get; set; }
        public bool IsShowContextMenu { get; set; } = true;
        public bool IsShowCopyMessage { get; set; } = true;
        public bool IsEnableZoom { set { IsEnableHZoom = IsEnableVZoom = value; } }
        public bool IsEnableHZoom { get; set; } = true;
        public bool IsEnableVZoom { get; set; } = true;
        public bool IsEnableWheelZoom { get; set; } = true;
        public bool IsEnableHPan { get; set; } = true;
        public bool IsEnableVPan { get; set; } = true;
        public bool IsZoomOnMouseCenter { get; set; }
        public bool IsShowHScrollBar { get; set; }
        public bool IsShowVScrollBar { get; set; }
        public bool IsAutoScrollRange { get; set; }
        public bool IsScrollY2 { get; set; }
        public bool IsSynchronizeXAxes { get; set; }
        public bool IsSynchronizeYAxes { get; set; }
        public bool IsPrintFillPage { get; set; } = true;
        public bool IsPrintKeepAspectRatio { get; set; } = true;
        public bool IsPrintScaleAll { get; set; } = true;
        public bool IsEnableSelection { get; set; }
        public bool IsAntiAlias { get; set; }
        public bool UseExtendedPrintDialog { get; set; }
        public double ScrollMinX { get; set; }
        public double ScrollMaxX { get; set; }
        public double ScrollMinY { get; set; }
        public double ScrollMaxY { get; set; }
        public double ScrollMinY2 { get; set; }
        public double ScrollMaxY2 { get; set; }
        public double ScrollGrace { get; set; }
        public double ZoomStepFraction { get; set; } = 0.1;
        public string PointValueFormat { get; set; } = "G";
        public string PointDateFormat { get; set; } = "g";
        public MouseButtons ZoomButtons { get; set; } = MouseButtons.Left;
        public Keys ZoomModifierKeys { get; set; } = Keys.None;
        public MouseButtons PanButtons { get; set; } = MouseButtons.Left;
        public Keys PanModifierKeys { get; set; } = Keys.Control;
        public MouseButtons PanButtons2 { get; set; } = MouseButtons.Middle;
        public Keys PanModifierKeys2 { get; set; } = Keys.None;

        public void AxisChange() { _masterPane.AxisChange(); }
        public void ZoomOut(GraphPane pane)
        {
            ZoomState old = pane.ZoomStack.Top;
            pane.ZoomStack.Pop(pane);
            Invalidate();
            ZoomEvent?.Invoke(this, old, null);
        }
        public void ZoomOutAll(GraphPane pane)
        {
            pane.ZoomStack.PopAll(pane);
            pane.AxisChange();
            Invalidate();
        }
        public void RestoreScale(GraphPane pane)
        {
            pane.ZoomStack.Clear();
            pane.XAxis.ResetAutoScale(pane, null);
            pane.YAxis.ResetAutoScale(pane, null);
            pane.Y2Axis.ResetAutoScale(pane, null);
            pane.AxisChange();
            Invalidate();
        }
        public void ZoomPane(GraphPane pane, double zoomFraction, PointF centerPt, bool isZoomOnCenter)
        {
            double x = pane.XAxis.Scale.ReverseTransform(centerPt.X), y = pane.YAxis.Scale.ReverseTransform(centerPt.Y);
            Zoom(pane.XAxis.Scale, x, zoomFraction);
            Zoom(pane.YAxis.Scale, y, zoomFraction);
            Invalidate();
        }
        private static void Zoom(Scale scale, double center, double fraction)
        {
            scale.MinAuto = scale.MaxAuto = false;
            scale.Min = center - (center - scale.Min) * fraction;
            scale.Max = center + (scale.Max - center) * fraction;
            scale.MajorStep = ZedGraph.Scale.NiceStep(scale.Max - scale.Min, 7);
            scale.MinorStep = scale.MajorStep / 5;
        }
        public void Copy(bool isShowMessage)
        {
            using (Bitmap image = _masterPane.GetImage(Width, Height, 96)) Clipboard.SetImage(image);
        }
        public void SaveAs() { SaveAs(null); }
        public string SaveAs(string defaultFileName)
        {
            using (SaveFileDialog dialog = new SaveFileDialog { Filter = "PNG Format (*.png)|*.png|JPEG Format (*.jpg)|*.jpg|Bmp Format (*.bmp)|*.bmp", FileName = defaultFileName ?? "" })
            {
                if (dialog.ShowDialog() != DialogResult.OK) return "";
                SaveAs(dialog.FileName, dialog.FilterIndex == 2 ? ImageFormat.Jpeg : dialog.FilterIndex == 3 ? ImageFormat.Bmp : ImageFormat.Png);
                return dialog.FileName;
            }
        }
        public void SaveAs(string fileName, ImageFormat format)
        {
            using (Bitmap image = _masterPane.GetImage(Math.Max(1, Width), Math.Max(1, Height), 96)) image.Save(fileName, format);
        }
        public void SaveAsBitmap() { SaveAs(); }
        public void SaveAsEmf() { SaveAs(); }
        public void DoPrint() { }
        public void DoPrintPreview() { }
        public void DoPageSetup() { }
        public GraphPane PaneFromPoint(Point mousePt) { return GraphPane; }

        // Painting
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            RectangleF rect = new RectangleF(0, 0, ClientSize.Width, ClientSize.Height);
            _masterPane.Rect = rect;
            _masterPane.Fill.Draw(g, rect);
            foreach (GraphPane pane in _masterPane.PaneList)
            {
                pane.Rect = rect;
                pane.Draw(g);
            }
            if (_zooming && _zoomRect.Width > 0 && _zoomRect.Height > 0)
            {
                using (Pen pen = new Pen(Color.Gray) { DashStyle = DashStyle.Dash }) g.DrawRectangle(pen, _zoomRect);
            }
            base.OnPaint(e);
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            Invalidate();
        }

        // Mouse
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (MouseDownEvent != null && MouseDownEvent(this, e)) { base.OnMouseDown(e); return; }
            GraphPane pane = GraphPane;
            if (e.Clicks == 2 && e.Button == MouseButtons.Left)
            {
                if (DoubleClickEvent == null || !DoubleClickEvent(this, e)) { }
                base.OnMouseDown(e);
                return;
            }
            if (pane != null && pane.Chart.Rect.Contains(e.Location))
            {
                bool pan = (e.Button == PanButtons && (ModifierKeys & PanModifierKeys) == PanModifierKeys && PanModifierKeys != Keys.None) ||
                           (e.Button == PanButtons2 && (ModifierKeys & PanModifierKeys2) == PanModifierKeys2);
                if (pan && (IsEnableHPan || IsEnableVPan))
                {
                    _panning = true;
                    _dragStart = e.Location;
                    _panX0 = pane.XAxis.Scale.Min; _panX1 = pane.XAxis.Scale.Max; _panY0 = pane.YAxis.Scale.Min; _panY1 = pane.YAxis.Scale.Max;
                    pane.ZoomStack.Push(pane, ZoomState.StateType.Pan);
                }
                else if (e.Button == ZoomButtons && (IsEnableHZoom || IsEnableVZoom))
                {
                    _zooming = true;
                    _dragStart = e.Location;
                    _zoomRect = Rectangle.Empty;
                }
            }
            base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (MouseMoveEvent != null && MouseMoveEvent(this, e)) { base.OnMouseMove(e); return; }
            GraphPane pane = GraphPane;
            if (_zooming)
            {
                _zoomRect = Rectangle.FromLTRB(Math.Min(_dragStart.X, e.X), Math.Min(_dragStart.Y, e.Y), Math.Max(_dragStart.X, e.X), Math.Max(_dragStart.Y, e.Y));
                if (!IsEnableVZoom) _zoomRect = new Rectangle(_zoomRect.X, (int)pane.Chart.Rect.Y, _zoomRect.Width, (int)pane.Chart.Rect.Height);
                if (!IsEnableHZoom) _zoomRect = new Rectangle((int)pane.Chart.Rect.X, _zoomRect.Y, (int)pane.Chart.Rect.Width, _zoomRect.Height);
                Invalidate();
            }
            else if (_panning && pane != null)
            {
                double dx = pane.XAxis.Scale.ReverseTransform(_dragStart.X) - pane.XAxis.Scale.ReverseTransform(e.X);
                double dy = pane.YAxis.Scale.ReverseTransform(_dragStart.Y) - pane.YAxis.Scale.ReverseTransform(e.Y);
                if (IsEnableHPan) { pane.XAxis.Scale.MinAuto = pane.XAxis.Scale.MaxAuto = false; pane.XAxis.Scale.Min = _panX0 + dx; pane.XAxis.Scale.Max = _panX1 + dx; }
                if (IsEnableVPan) { pane.YAxis.Scale.MinAuto = pane.YAxis.Scale.MaxAuto = false; pane.YAxis.Scale.Min = _panY0 + dy; pane.YAxis.Scale.Max = _panY1 + dy; }
                Invalidate();
            }
            else if (IsShowPointValues && pane != null)
            {
                string tip = null;
                if (pane.FindNearestPoint(e.Location, out CurveItem curve, out int index))
                {
                    tip = PointValueEvent?.Invoke(this, pane, curve, index);
                    if (tip == null)
                    {
                        PointPair p = curve.Points[index];
                        tip = (string.IsNullOrEmpty(curve.Label.Text) ? "" : curve.Label.Text + ": ") +
                              p.X.ToString(PointValueFormat, CultureInfo.CurrentCulture) + ", " + p.Y.ToString(PointValueFormat, CultureInfo.CurrentCulture);
                    }
                }
                if (tip != _pointValueTip)
                {
                    _pointValueTip = tip;
                    Avalonia.Controls.ToolTip.SetTip(NativeControl, tip);
                    Avalonia.Controls.ToolTip.SetIsOpen(NativeControl, tip != null);
                }
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (MouseUpEvent != null && MouseUpEvent(this, e)) { base.OnMouseUp(e); return; }
            GraphPane pane = GraphPane;
            if (_zooming && pane != null)
            {
                _zooming = false;
                if (_zoomRect.Width > 4 && _zoomRect.Height > 4)
                {
                    ZoomState old = pane.ZoomStack.Push(pane, ZoomState.StateType.Zoom);
                    if (IsEnableHZoom)
                    {
                        double x0 = pane.XAxis.Scale.ReverseTransform(_zoomRect.Left), x1 = pane.XAxis.Scale.ReverseTransform(_zoomRect.Right);
                        SetRange(pane.XAxis.Scale, Math.Min(x0, x1), Math.Max(x0, x1));
                    }
                    if (IsEnableVZoom)
                    {
                        double y0 = pane.YAxis.Scale.ReverseTransform(_zoomRect.Bottom), y1 = pane.YAxis.Scale.ReverseTransform(_zoomRect.Top);
                        SetRange(pane.YAxis.Scale, Math.Min(y0, y1), Math.Max(y0, y1));
                    }
                    ZoomEvent?.Invoke(this, old, new ZoomState(pane, ZoomState.StateType.Zoom));
                }
                Invalidate();
            }
            _panning = false;
            if (e.Button == MouseButtons.Right && IsShowContextMenu) ShowContextMenu(e.Location);
            base.OnMouseUp(e);
        }
        private static void SetRange(Scale scale, double min, double max)
        {
            scale.MinAuto = scale.MaxAuto = false;
            scale.Min = min;
            scale.Max = max;
            scale.MajorStep = ZedGraph.Scale.NiceStep(max - min, 7);
            scale.MinorStep = scale.MajorStep / 5;
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            GraphPane pane = GraphPane;
            if (IsEnableWheelZoom && pane != null && pane.Chart.Rect.Contains(e.Location) && e.Delta != 0)
            {
                pane.ZoomStack.Push(pane, ZoomState.StateType.WheelZoom);
                double fraction = 1 + (e.Delta < 0 ? 1.0 : -1.0) * ZoomStepFraction;
                ZoomPane(pane, fraction, e.Location, IsZoomOnMouseCenter);
                if (e is HandledMouseEventArgs h) h.Handled = true;
            }
            base.OnMouseWheel(e);
        }
        private void ShowContextMenu(Point location)
        {
            GraphPane pane = GraphPane;
            _contextMenu.Items.Clear();
            ToolStripMenuItem copy = new ToolStripMenuItem("Copy") { Name = "copy", Tag = "copy" };
            copy.Click += (s, e) => Copy(IsShowCopyMessage);
            ToolStripMenuItem save = new ToolStripMenuItem("Save Image As...") { Name = "save_as", Tag = "save_as" };
            save.Click += (s, e) => SaveAs();
            ToolStripMenuItem pageSetup = new ToolStripMenuItem("Page Setup...") { Name = "page_setup", Tag = "page_setup", Enabled = false };
            ToolStripMenuItem print = new ToolStripMenuItem("Print...") { Name = "print", Tag = "print", Enabled = false };
            ToolStripMenuItem showValues = new ToolStripMenuItem("Show Point Values") { Name = "show_val", Tag = "show_val", Checked = IsShowPointValues };
            showValues.Click += (s, e) => IsShowPointValues = !IsShowPointValues;
            ToolStripMenuItem unzoom = new ToolStripMenuItem("Un-Zoom") { Name = "unzoom", Tag = "unzoom", Enabled = pane != null && !pane.ZoomStack.IsEmpty };
            unzoom.Click += (s, e) => ZoomOut(pane);
            ToolStripMenuItem undoAll = new ToolStripMenuItem("Undo All Zoom/Pan") { Name = "undo_all", Tag = "undo_all", Enabled = pane != null && !pane.ZoomStack.IsEmpty };
            undoAll.Click += (s, e) => ZoomOutAll(pane);
            ToolStripMenuItem setDefault = new ToolStripMenuItem("Set Scale to Default") { Name = "set_default", Tag = "set_default" };
            setDefault.Click += (s, e) => RestoreScale(pane);
            _contextMenu.Items.AddRange(new ToolStripItem[] { copy, save, pageSetup, print, showValues, unzoom, undoAll, setDefault });
            ContextMenuBuilder?.Invoke(this, _contextMenu, location, ContextMenuObjectState.Background);
            if (_contextMenu.Items.Count > 0) _contextMenu.Show(this, location);
        }
    }
}
