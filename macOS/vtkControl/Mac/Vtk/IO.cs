// PrePoMax for macOS - images and files of the VTK compatible library: the image of a render window, PNG, STL and legacy VTK
// files, AVI videos (motion JPEG) and the OBJ, VRML and X3D exporters of a scene.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SkiaSharp;

namespace Kitware.VTK
{
    public partial class vtkImageData
    {
        private IntPtr _scalarPointer;

        public int GetNumberOfScalarComponents() { return Components; }
        public IntPtr GetScalarPointer()
        {
            if (_scalarPointer != IntPtr.Zero) Marshal.FreeHGlobal(_scalarPointer);
            _scalarPointer = Marshal.AllocHGlobal(Math.Max(1, Pixels?.Length ?? 1));
            if (Pixels != null) Marshal.Copy(Pixels, 0, _scalarPointer, Pixels.Length);
            return _scalarPointer;
        }
        ~vtkImageData()
        {
            if (_scalarPointer != IntPtr.Zero) Marshal.FreeHGlobal(_scalarPointer);
        }
        // The image as a Skia bitmap, top row first
        internal SKBitmap ToBitmap()
        {
            int w = GetDimensions()[0], h = GetDimensions()[1];
            int c = Math.Max(1, Components);
            // An image without alpha is opaque, which a PNG then writes as RGB, as VTK does
            SKBitmap bitmap = new SKBitmap(new SKImageInfo(w, h, SKColorType.Rgba8888,
                                                           c == 4 ? SKAlphaType.Unpremul : SKAlphaType.Opaque));
            byte[] rgba = new byte[w * h * 4];
            for (int y = 0; y < h; y++)
            {
                int src = (h - 1 - y) * w * c, dst = y * w * 4;
                for (int x = 0; x < w; x++)
                {
                    rgba[dst + 4 * x] = Pixels[src + c * x];
                    rgba[dst + 4 * x + 1] = Pixels[src + c * x + (c > 1 ? 1 : 0)];
                    rgba[dst + 4 * x + 2] = Pixels[src + c * x + (c > 2 ? 2 : 0)];
                    rgba[dst + 4 * x + 3] = c == 4 ? Pixels[src + c * x + 3] : (byte)255;
                }
            }
            Marshal.Copy(rgba, 0, bitmap.GetPixels(), rgba.Length);
            return bitmap;
        }
    }

    // The pixels of a render window, rendered offscreen
    public partial class vtkWindowToImageFilter
    {
        private vtkRenderWindow _window;
        private int _magnification = 1;
        private bool _rgba;

        public void SetInput(vtkWindow window) { _window = window as vtkRenderWindow; Modified(); }
        public void SetMagnification(int m) { _magnification = Math.Max(1, m); Modified(); }
        public int GetMagnification() { return _magnification; }
        public void SetInputBufferTypeToRGB() { _rgba = false; Modified(); }
        public void SetInputBufferTypeToRGBA() { _rgba = true; Modified(); }
        public void ReadFrontBufferOff() { }
        public void ReadFrontBufferOn() { }
        public void ShouldRerenderOn() { }
        public void ShouldRerenderOff() { }
        public void FixBoundaryOn() { }
        protected override void Execute()
        {
            vtkImageData image = GetOutput();
            if (_window == null) return;
            byte[] pixels = _window.ReadPixels(_magnification, _rgba, out int width, out int height);
            image.SetDimensions(width, height, 1);
            image.Pixels = pixels;
            image.Components = _rgba ? 4 : 3;
        }
    }

    public partial class vtkWriter
    {
        protected string _fileName;
        protected bool _binary = true;
        public void SetFileName(string fileName) { _fileName = fileName; Modified(); }
        public string GetFileName() { return _fileName; }
        public void SetFileTypeToBinary() { _binary = true; }
        public void SetFileTypeToASCII() { _binary = false; }
        public virtual int Write()
        {
            // The input is brought up to date and written
            foreach (vtkDataObject input in InputDataObjects(0)) input._producer?.UpdatePipeline();
            int n = GetNumberOfInputConnections(0);
            for (int i = 0; i < n; i++)
            {
                vtkDataObject data = GetInputDataObject(0, i);
                if (data?.GetProducer() != null) data.GetProducer().UpdatePipeline();
            }
            UpdateInputs();
            WriteData();
            return 1;
        }
        private void UpdateInputs()
        {
            // The connections of the inputs update their producers
            for (int i = 0; i < GetNumberOfInputConnections(0); i++)
            {
                object input = InputPort(0)[i];
                if (input is vtkAlgorithmOutput output) output.Producer.UpdatePipeline();
            }
        }
        protected virtual void WriteData() { }
    }

    public partial class vtkPNGWriter
    {
        protected override void WriteData()
        {
            vtkImageData image = GetInputDataObject(0, 0) as vtkImageData;
            if (image == null || image.Pixels == null || _fileName == null) return;
            using (SKBitmap bitmap = image.ToBitmap())
            using (SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100))
            using (FileStream stream = File.Create(_fileName))
            {
                data.SaveTo(stream);
            }
        }
    }

    public partial class vtkSTLWriter
    {
        protected override void WriteData()
        {
            vtkPolyData pd = GetInputDataObject(0, 0) as vtkPolyData;
            if (pd == null || _fileName == null) return;
            List<long[]> triangles = new List<long[]>();
            foreach (var c in FilterTools.Cells(pd))
                if (CellType.Dimension(c.type) == 2)
                    foreach (int[] t in CellType.Triangulate(c.type, c.ids.Length, null))
                        triangles.Add(new[] { c.ids[t[0]], c.ids[t[1]], c.ids[t[2]] });
            using (BinaryWriter w = new BinaryWriter(File.Create(_fileName)))
            {
                byte[] header = new byte[80];
                Encoding.ASCII.GetBytes("Visualization Toolkit generated SLA File").CopyTo(header, 0);
                w.Write(header);
                w.Write((uint)triangles.Count);
                foreach (long[] t in triangles)
                {
                    double[] a = pd.GetPoint(t[0]), b = pd.GetPoint(t[1]), c = pd.GetPoint(t[2]);
                    double[] n = new double[3];
                    vtkTriangle.ComputeNormal(a, b, c, n);
                    foreach (double v in n) w.Write((float)v);
                    foreach (double[] p in new[] { a, b, c }) foreach (double v in p) w.Write((float)v);
                    w.Write((ushort)0);
                }
            }
        }
    }

    public partial class vtkPolyDataWriter
    {
        protected override void WriteData()
        {
            vtkPolyData pd = GetInputDataObject(0, 0) as vtkPolyData;
            if (pd == null || _fileName == null) return;
            CultureInfo ci = CultureInfo.InvariantCulture;
            using (StreamWriter w = new StreamWriter(_fileName))
            {
                w.WriteLine("# vtk DataFile Version 3.0");
                w.WriteLine("vtk output");
                w.WriteLine("ASCII");
                w.WriteLine("DATASET POLYDATA");
                long n = pd.GetNumberOfPoints();
                w.WriteLine("POINTS " + n + " float");
                for (long i = 0; i < n; i++)
                {
                    double[] p = pd.GetPoint(i);
                    w.WriteLine(string.Format(ci, "{0} {1} {2}", p[0], p[1], p[2]));
                }
                string[] names = { "VERTICES", "LINES", "POLYGONS", "TRIANGLE_STRIPS" };
                for (int kind = 0; kind < 4; kind++)
                {
                    vtkCellArray ca = pd.Array(kind);
                    if (ca.Count == 0) continue;
                    w.WriteLine(names[kind] + " " + ca.Count + " " + ca.GetNumberOfConnectivityEntries());
                    for (int c = 0; c < ca.Count; c++) w.WriteLine(ca.CellSize(c) + " " + string.Join(" ", ca.GetCell(c)));
                }
            }
        }
    }

    // AVI files of motion JPEG frames
    public partial class vtkAVIWriter
    {
        private int _rate = 10;
        private int _quality = 2;
        private FileStream _stream;
        private BinaryWriter _writer;
        private readonly List<(long offset, int size)> _frames = new List<(long, int)>();
        private int _width, _height;
        private long _moviStart, _riffSizePos, _moviSizePos, _framesPos, _streamFramesPos;

        public void SetRate(int rate) { _rate = Math.Max(1, rate); }
        public void SetQuality(int q) { _quality = q; }
        public void SetCompressorFourCC(string fourCC) { }
        public void PromptCompressionOptionsOn() { }
        public void PromptCompressionOptionsOff() { }
        public void Start()
        {
            _frames.Clear();
            _width = _height = 0;
            _stream = File.Create(_fileName);
            _writer = new BinaryWriter(_stream);
        }
        public override int Write()
        {
            for (int i = 0; i < GetNumberOfInputConnections(0); i++)
                if (InputPort(0)[i] is vtkAlgorithmOutput output) output.Producer.UpdatePipeline();
            vtkImageData image = GetInputDataObject(0, 0) as vtkImageData;
            if (image == null || image.Pixels == null || _writer == null) return 0;
            int[] d = image.GetDimensions();
            // The frames of a video have the size of the first one
            if (_width == 0)
            {
                _width = d[0] - d[0] % 2;
                _height = d[1] - d[1] % 2;
                WriteHeader();
            }
            byte[] jpeg;
            using (SKBitmap bitmap = image.ToBitmap())
            using (SKBitmap frame = bitmap.Width == _width && bitmap.Height == _height ? null : bitmap.Resize(new SKImageInfo(_width, _height), SKFilterQuality.High))
            using (SKData data = (frame ?? bitmap).Encode(SKEncodedImageFormat.Jpeg, _quality >= 2 ? 95 : _quality == 1 ? 85 : 70))
            {
                jpeg = data.ToArray();
            }
            long offset = _stream.Position;
            _writer.Write(Encoding.ASCII.GetBytes("00dc"));
            _writer.Write(jpeg.Length);
            _writer.Write(jpeg);
            if (jpeg.Length % 2 == 1) _writer.Write((byte)0);
            _frames.Add((offset - _moviStart, jpeg.Length));
            return 1;
        }
        public void End()
        {
            if (_writer == null) return;
            if (_width == 0) { _writer.Dispose(); _writer = null; return; }
            long moviEnd = _stream.Position;
            // Index
            _writer.Write(Encoding.ASCII.GetBytes("idx1"));
            _writer.Write(_frames.Count * 16);
            foreach (var f in _frames)
            {
                _writer.Write(Encoding.ASCII.GetBytes("00dc"));
                _writer.Write(0x10);
                _writer.Write((int)f.offset);
                _writer.Write(f.size);
            }
            long end = _stream.Position;
            Patch(_riffSizePos, (int)(end - 8));
            Patch(_moviSizePos, (int)(moviEnd - _moviSizePos - 4));
            Patch(_framesPos, _frames.Count);
            Patch(_streamFramesPos, _frames.Count);
            _writer.Dispose();
            _writer = null;
            _stream = null;
        }
        private void Patch(long position, int value)
        {
            long current = _stream.Position;
            _stream.Position = position;
            _writer.Write(value);
            _stream.Position = current;
        }
        private void WriteHeader()
        {
            BinaryWriter w = _writer;
            w.Write(Encoding.ASCII.GetBytes("RIFF"));
            _riffSizePos = _stream.Position;
            w.Write(0);
            w.Write(Encoding.ASCII.GetBytes("AVI "));
            // hdrl
            w.Write(Encoding.ASCII.GetBytes("LIST"));
            w.Write(4 + 8 + 56 + 8 + 4 + 8 + 56 + 8 + 40);
            w.Write(Encoding.ASCII.GetBytes("hdrl"));
            w.Write(Encoding.ASCII.GetBytes("avih"));
            w.Write(56);
            w.Write(1000000 / _rate);              // microseconds per frame
            w.Write(_width * _height * 3 * _rate); // max bytes per second
            w.Write(0);                            // padding
            w.Write(0x10);                         // has index
            _framesPos = _stream.Position;
            w.Write(0);                            // total frames
            w.Write(0);                            // initial frames
            w.Write(1);                            // streams
            w.Write(_width * _height * 3);         // suggested buffer size
            w.Write(_width);
            w.Write(_height);
            w.Write(0); w.Write(0); w.Write(0); w.Write(0);
            // strl
            w.Write(Encoding.ASCII.GetBytes("LIST"));
            w.Write(4 + 8 + 56 + 8 + 40);
            w.Write(Encoding.ASCII.GetBytes("strl"));
            w.Write(Encoding.ASCII.GetBytes("strh"));
            w.Write(56);
            w.Write(Encoding.ASCII.GetBytes("vids"));
            w.Write(Encoding.ASCII.GetBytes("MJPG"));
            w.Write(0);                            // flags
            w.Write((short)0); w.Write((short)0);  // priority, language
            w.Write(0);                            // initial frames
            w.Write(1);                            // scale
            w.Write(_rate);                        // rate
            w.Write(0);                            // start
            _streamFramesPos = _stream.Position;
            w.Write(0);                            // length
            w.Write(_width * _height * 3);         // suggested buffer size
            w.Write(-1);                           // quality
            w.Write(0);                            // sample size
            w.Write((short)0); w.Write((short)0); w.Write((short)_width); w.Write((short)_height);
            w.Write(Encoding.ASCII.GetBytes("strf"));
            w.Write(40);
            w.Write(40);                           // BITMAPINFOHEADER
            w.Write(_width);
            w.Write(_height);
            w.Write((short)1);
            w.Write((short)24);
            w.Write(Encoding.ASCII.GetBytes("MJPG"));
            w.Write(_width * _height * 3);
            w.Write(0); w.Write(0); w.Write(0); w.Write(0);
            // movi
            w.Write(Encoding.ASCII.GetBytes("LIST"));
            _moviSizePos = _stream.Position;
            w.Write(0);
            _moviStart = _stream.Position;
            w.Write(Encoding.ASCII.GetBytes("movi"));
        }
    }

    // Exporters of the props of a render window
    public partial class vtkExporter
    {
        protected vtkRenderWindow _window;
        protected string _fileName;
        protected string _filePrefix;
        public void SetRenderWindow(vtkRenderWindow window) { _window = window; }
        public void SetFileName(string fileName) { _fileName = fileName; }
        public void SetFilePrefix(string prefix) { _filePrefix = prefix; }
        public string GetFilePrefix() { return _filePrefix; }
        public void Update() { }
        public virtual void Write() { }
        // The surfaces of the visible actors in world coordinates
        protected IEnumerable<(double[][] points, List<long[]> polys, double[] color)> Surfaces()
        {
            if (_window == null) yield break;
            foreach (vtkRenderer r in _window.GetRenderers().Items)
                foreach (vtkProp prop in r.Props)
                {
                    if (!(prop is vtkActor actor) || !actor.Visible || actor.GetMapper() == null) continue;
                    vtkPolyData pd = actor.GetMapper().GetRenderPolyData();
                    if (pd == null || pd.GetPoints() == null) continue;
                    double[] m = actor.ModelMatrix;
                    double[][] pts = new double[pd.GetNumberOfPoints()][];
                    for (long i = 0; i < pts.Length; i++)
                    {
                        double[] p = pd.GetPoint(i);
                        pts[i] = vtkMatrix4x4.TransformPoint(m, p[0], p[1], p[2]);
                    }
                    List<long[]> polys = new List<long[]>();
                    foreach (var c in FilterTools.Cells(pd))
                        if (CellType.Dimension(c.type) == 2)
                            foreach (int[] t in CellType.Triangulate(c.type, c.ids.Length, null))
                                polys.Add(new[] { c.ids[t[0]], c.ids[t[1]], c.ids[t[2]] });
                    if (polys.Count > 0) yield return (pts, polys, actor.GetProperty().GetColor());
                }
        }
    }

    public partial class vtkOBJExporter
    {
        public override void Write()
        {
            string file = (_filePrefix ?? Path.ChangeExtension(_fileName, null)) + ".obj";
            CultureInfo ci = CultureInfo.InvariantCulture;
            using (StreamWriter w = new StreamWriter(file))
            {
                long offset = 1;
                int group = 0;
                foreach (var s in Surfaces())
                {
                    w.WriteLine("g actor" + group++);
                    foreach (double[] p in s.points) w.WriteLine(string.Format(ci, "v {0} {1} {2}", p[0], p[1], p[2]));
                    foreach (long[] t in s.polys) w.WriteLine("f " + string.Join(" ", t.Select(i => (i + offset).ToString())));
                    offset += s.points.Length;
                }
            }
        }
    }

    public partial class vtkVRMLExporter
    {
        public override void Write()
        {
            string file = _fileName ?? (_filePrefix + ".wrl");
            CultureInfo ci = CultureInfo.InvariantCulture;
            using (StreamWriter w = new StreamWriter(file))
            {
                w.WriteLine("#VRML V2.0 utf8");
                foreach (var s in Surfaces())
                {
                    w.WriteLine(string.Format(ci, "Shape {{ appearance Appearance {{ material Material {{ diffuseColor {0} {1} {2} }} }}",
                                              s.color[0], s.color[1], s.color[2]));
                    w.WriteLine("geometry IndexedFaceSet { coord Coordinate { point [");
                    foreach (double[] p in s.points) w.WriteLine(string.Format(ci, "{0} {1} {2},", p[0], p[1], p[2]));
                    w.WriteLine("] } coordIndex [");
                    foreach (long[] t in s.polys) w.WriteLine(string.Join(", ", t) + ", -1,");
                    w.WriteLine("] } }");
                }
            }
        }
    }

    public partial class vtkX3DExporter
    {
        public override void Write()
        {
            string file = _fileName ?? (_filePrefix + ".x3d");
            CultureInfo ci = CultureInfo.InvariantCulture;
            using (StreamWriter w = new StreamWriter(file))
            {
                w.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                w.WriteLine("<X3D profile=\"Immersive\" version=\"3.0\"><Scene>");
                foreach (var s in Surfaces())
                {
                    w.WriteLine(string.Format(ci, "<Shape><Appearance><Material diffuseColor=\"{0} {1} {2}\"/></Appearance>",
                                              s.color[0], s.color[1], s.color[2]));
                    w.WriteLine("<IndexedFaceSet coordIndex=\"" + string.Join(" ", s.polys.Select(t => string.Join(" ", t) + " -1")) + "\">");
                    w.WriteLine("<Coordinate point=\"" + string.Join(" ", s.points.Select(p => string.Format(ci, "{0} {1} {2}", p[0], p[1], p[2]))) + "\"/>");
                    w.WriteLine("</IndexedFaceSet></Shape>");
                }
                w.WriteLine("</Scene></X3D>");
            }
        }
    }
}
