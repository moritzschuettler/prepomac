// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Runtime.InteropServices;
using CaeGlobals;

namespace vtkControl
{
    // The port of vtkAVIWriter: a video of RGB frames through the Video for Windows library, compressed by the
    // compressor the user picks in its dialog or by Microsoft Video 1
    public sealed class AviWriter : IDisposable
    {
        // Variables
        private IntPtr _file;
        private IntPtr _stream;
        private IntPtr _compressedStream;
        private int _width;
        private int _height;
        private int _rowLength;
        private byte[] _frame;
        private int _time;
        private bool _libraryOpen;


        // Constructors
        // Returns false when the user cancels the dialog of the compression options
        public bool Start(string fileName, int width, int height, int rate, int quality, bool promptCompressionOptions)
        {
            _width = width;
            _height = height;
            _rowLength = (width * 3 + 3) / 4 * 4;
            _frame = new byte[_rowLength * height];
            _time = 0;
            //
            AVIFileInit();
            _libraryOpen = true;
            if (AVIFileOpen(out _file, fileName, OF_WRITE | OF_CREATE, IntPtr.Zero) != 0)
                throw new CaeException("Unable to open " + fileName + ".");
            //
            AviStreamInfo info = new AviStreamInfo();
            info.FccType = StreamTypeVideo;
            info.Scale = 1;
            info.Rate = rate;
            info.Quality = -1;
            info.SuggestedBufferSize = width * height * 3;
            info.FrameRight = width;
            info.FrameBottom = height;
            info.Name = "";
            if (AVIFileCreateStream(_file, out _stream, ref info) != 0)
                throw new CaeException("Unable to create the video stream of " + fileName + ".");
            //
            AviCompressOptions options = new AviCompressOptions();
            options.FccHandler = FourCC("MSVC");
            options.Quality = quality == 0 ? 2500 : quality == 1 ? 5000 : 10000;
            options.Flags = AVICOMPRESSF_VALID;
            if (promptCompressionOptions)
            {
                IntPtr optionsPointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(AviCompressOptions)));
                IntPtr optionsArray = Marshal.AllocHGlobal(IntPtr.Size);
                IntPtr streamArray = Marshal.AllocHGlobal(IntPtr.Size);
                try
                {
                    Marshal.StructureToPtr(options, optionsPointer, false);
                    Marshal.WriteIntPtr(optionsArray, optionsPointer);
                    Marshal.WriteIntPtr(streamArray, _stream);
                    if (!AVISaveOptions(IntPtr.Zero, 0, 1, streamArray, optionsArray)) return false;
                    options = (AviCompressOptions)Marshal.PtrToStructure(optionsPointer, typeof(AviCompressOptions));
                }
                finally
                {
                    Marshal.FreeHGlobal(optionsPointer);
                    Marshal.FreeHGlobal(optionsArray);
                    Marshal.FreeHGlobal(streamArray);
                }
            }
            int error = AVIMakeCompressedStream(out _compressedStream, _stream, ref options, IntPtr.Zero);
            if (error != 0) throw new CaeException("Unable to compress " + fileName + ".");
            //
            BitmapInfoHeader header = new BitmapInfoHeader();
            header.Size = Marshal.SizeOf(typeof(BitmapInfoHeader));
            header.Width = width;
            header.Height = height;
            header.Planes = 1;
            header.BitCount = 24;
            header.SizeImage = _frame.Length;
            if (AVIStreamSetFormat(_compressedStream, 0, ref header, header.Size) != 0)
                throw new CaeException("Unable to format " + fileName + ". The video compression scheme could not " +
                                       "handle the data; select a different compression scheme.");
            return true;
        }


        // Methods
        // RGB rows, bottom row first, as the native view reads them
        public void Write(byte[] rgb)
        {
            if (_compressedStream == IntPtr.Zero) return;
            for (int y = 0; y < _height; y++)
            {
                int read = y * _width * 3;
                int write = y * _rowLength;
                for (int x = 0; x < _width; x++)
                {
                    _frame[write++] = rgb[read + 2];
                    _frame[write++] = rgb[read + 1];
                    _frame[write++] = rgb[read];
                    read += 3;
                }
            }
            AVIStreamWrite(_compressedStream, _time++, 1, _frame, _frame.Length, AVIIF_KEYFRAME, IntPtr.Zero,
                           IntPtr.Zero);
        }
        public void Dispose()
        {
            if (_stream != IntPtr.Zero) AVIStreamRelease(_stream);
            if (_compressedStream != IntPtr.Zero) AVIStreamRelease(_compressedStream);
            if (_file != IntPtr.Zero) AVIFileRelease(_file);
            _stream = IntPtr.Zero;
            _compressedStream = IntPtr.Zero;
            _file = IntPtr.Zero;
            if (_libraryOpen) AVIFileExit();
            _libraryOpen = false;
        }
        private static int FourCC(string code)
        {
            return code[0] | code[1] << 8 | code[2] << 16 | code[3] << 24;
        }


        // Video for Windows
        private const int OF_WRITE = 0x0001;
        private const int OF_CREATE = 0x1000;
        private const int AVICOMPRESSF_VALID = 0x0008;
        private const int AVIIF_KEYFRAME = 0x0010;
        private static readonly int StreamTypeVideo = FourCC("vids");
        //
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        private struct AviStreamInfo
        {
            public int FccType;
            public int FccHandler;
            public int Flags;
            public int Caps;
            public short Priority;
            public short Language;
            public int Scale;
            public int Rate;
            public int Start;
            public int Length;
            public int InitialFrames;
            public int SuggestedBufferSize;
            public int Quality;
            public int SampleSize;
            public int FrameLeft;
            public int FrameTop;
            public int FrameRight;
            public int FrameBottom;
            public int EditCount;
            public int FormatChangeCount;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string Name;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct AviCompressOptions
        {
            public int FccType;
            public int FccHandler;
            public int KeyFrameEvery;
            public int Quality;
            public int BytesPerSecond;
            public int Flags;
            public IntPtr Format;
            public int FormatSize;
            public IntPtr Parameters;
            public int ParametersSize;
            public int Interleave;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BitmapInfoHeader
        {
            public int Size;
            public int Width;
            public int Height;
            public short Planes;
            public short BitCount;
            public int Compression;
            public int SizeImage;
            public int XPelsPerMeter;
            public int YPelsPerMeter;
            public int ClrUsed;
            public int ClrImportant;
        }
        [DllImport("avifil32.dll")]
        private static extern void AVIFileInit();
        [DllImport("avifil32.dll")]
        private static extern void AVIFileExit();
        [DllImport("avifil32.dll", CharSet = CharSet.Unicode, EntryPoint = "AVIFileOpenW")]
        private static extern int AVIFileOpen(out IntPtr file, string fileName, int mode, IntPtr handler);
        [DllImport("avifil32.dll", CharSet = CharSet.Unicode, EntryPoint = "AVIFileCreateStreamW")]
        private static extern int AVIFileCreateStream(IntPtr file, out IntPtr stream, ref AviStreamInfo info);
        [DllImport("avifil32.dll")]
        private static extern bool AVISaveOptions(IntPtr window, int flags, int streamCount, IntPtr streams,
                                                  IntPtr options);
        [DllImport("avifil32.dll")]
        private static extern int AVIMakeCompressedStream(out IntPtr compressed, IntPtr source,
                                                          ref AviCompressOptions options, IntPtr handler);
        [DllImport("avifil32.dll")]
        private static extern int AVIStreamSetFormat(IntPtr stream, int position, ref BitmapInfoHeader format,
                                                     int formatSize);
        [DllImport("avifil32.dll")]
        private static extern int AVIStreamWrite(IntPtr stream, int start, int samples, byte[] buffer, int bufferSize,
                                                 int flags, IntPtr samplesWritten, IntPtr bytesWritten);
        [DllImport("avifil32.dll")]
        private static extern int AVIStreamRelease(IntPtr stream);
        [DllImport("avifil32.dll")]
        private static extern int AVIFileRelease(IntPtr file);
    }
}
