// PrePoMax for macOS - the OpenGL 3.2+ core functions the view uses, loaded from the current context.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Kitware.VTK.Gl
{
    internal unsafe class GL
    {
        // Constants
        public const int DEPTH_BUFFER_BIT = 0x100, COLOR_BUFFER_BIT = 0x4000;
        public const int TRIANGLES = 4, LINES = 1, POINTS = 0, TRIANGLE_STRIP = 5;
        public const int DEPTH_TEST = 0x0B71, BLEND = 0x0BE2, CULL_FACE = 0x0B44, POLYGON_OFFSET_FILL = 0x8037;
        public const int MULTISAMPLE = 0x809D, PROGRAM_POINT_SIZE = 0x8642, CLIP_DISTANCE0 = 0x3000, SCISSOR_TEST = 0x0C11;
        public const int FRAMEBUFFER_SRGB = 0x8DB9;
        public const int LEQUAL = 0x0203, LESS = 0x0201, ALWAYS = 0x0207;
        public const int SRC_ALPHA = 0x0302, ONE_MINUS_SRC_ALPHA = 0x0303, ONE = 1, ZERO = 0;
        public const int FRONT = 0x0404, BACK = 0x0405;
        public const int ARRAY_BUFFER = 0x8892, ELEMENT_ARRAY_BUFFER = 0x8893, STATIC_DRAW = 0x88E4, DYNAMIC_DRAW = 0x88E8;
        public const int FLOAT = 0x1406, UNSIGNED_BYTE = 0x1401, UNSIGNED_INT = 0x1405;
        public const int VERTEX_SHADER = 0x8B31, FRAGMENT_SHADER = 0x8B30, GEOMETRY_SHADER = 0x8DD9;
        public const int COMPILE_STATUS = 0x8B81, LINK_STATUS = 0x8B82, INFO_LOG_LENGTH = 0x8B84;
        public const int TEXTURE_2D = 0x0DE1, TEXTURE0 = 0x84C0, TEXTURE_MIN_FILTER = 0x2801, TEXTURE_MAG_FILTER = 0x2800;
        public const int TEXTURE_WRAP_S = 0x2802, TEXTURE_WRAP_T = 0x2803, NEAREST = 0x2600, LINEAR = 0x2601;
        public const int CLAMP_TO_EDGE = 0x812F, RGBA = 0x1908, RGBA8 = 0x8058, RGB = 0x1907;
        public const int FRAMEBUFFER = 0x8D40, READ_FRAMEBUFFER = 0x8CA8, DRAW_FRAMEBUFFER = 0x8CA9, RENDERBUFFER = 0x8D41;
        public const int COLOR_ATTACHMENT0 = 0x8CE0, DEPTH_ATTACHMENT = 0x8D00, DEPTH_STENCIL_ATTACHMENT = 0x821A;
        public const int DEPTH24_STENCIL8 = 0x88F0, DEPTH_COMPONENT24 = 0x81A6, FRAMEBUFFER_COMPLETE = 0x8CD5;
        public const int FRAMEBUFFER_BINDING = 0x8CA6, MAX_SAMPLES = 0x8D57, UNPACK_ALIGNMENT = 0x0CF5, PACK_ALIGNMENT = 0x0D05;
        public const int VIEWPORT = 0x0BA2;

        private readonly delegate* unmanaged<int, int, int, int, void> _viewport;
        private readonly delegate* unmanaged<int, int, int, int, void> _scissor;
        private readonly delegate* unmanaged<float, float, float, float, void> _clearColor;
        private readonly delegate* unmanaged<double, void> _clearDepth;
        private readonly delegate* unmanaged<int, void> _clear;
        private readonly delegate* unmanaged<int, void> _enable;
        private readonly delegate* unmanaged<int, void> _disable;
        private readonly delegate* unmanaged<int, void> _depthFunc;
        private readonly delegate* unmanaged<byte, void> _depthMask;
        private readonly delegate* unmanaged<byte, byte, byte, byte, void> _colorMask;
        private readonly delegate* unmanaged<int, int, int, int, void> _blendFuncSeparate;
        private readonly delegate* unmanaged<int, void> _cullFace;
        private readonly delegate* unmanaged<float, float, void> _polygonOffset;
        private readonly delegate* unmanaged<int, uint*, void> _genBuffers;
        private readonly delegate* unmanaged<int, uint*, void> _deleteBuffers;
        private readonly delegate* unmanaged<int, uint, void> _bindBuffer;
        private readonly delegate* unmanaged<int, IntPtr, void*, int, void> _bufferData;
        private readonly delegate* unmanaged<int, uint*, void> _genVertexArrays;
        private readonly delegate* unmanaged<int, uint*, void> _deleteVertexArrays;
        private readonly delegate* unmanaged<uint, void> _bindVertexArray;
        private readonly delegate* unmanaged<uint, void> _enableVertexAttribArray;
        private readonly delegate* unmanaged<uint, void> _disableVertexAttribArray;
        private readonly delegate* unmanaged<uint, int, int, byte, int, IntPtr, void> _vertexAttribPointer;
        private readonly delegate* unmanaged<uint, float, float, float, float, void> _vertexAttrib4f;
        private readonly delegate* unmanaged<uint, float, void> _vertexAttrib1f;
        private readonly delegate* unmanaged<int, uint> _createShader;
        private readonly delegate* unmanaged<uint, int, byte**, int*, void> _shaderSource;
        private readonly delegate* unmanaged<uint, void> _compileShader;
        private readonly delegate* unmanaged<uint, int, int*, void> _getShaderiv;
        private readonly delegate* unmanaged<uint, int, int*, byte*, void> _getShaderInfoLog;
        private readonly delegate* unmanaged<uint, void> _deleteShader;
        private readonly delegate* unmanaged<uint> _createProgram;
        private readonly delegate* unmanaged<uint, uint, void> _attachShader;
        private readonly delegate* unmanaged<uint, uint, byte*, void> _bindAttribLocation;
        private readonly delegate* unmanaged<uint, uint, byte*, void> _bindFragDataLocation;
        private readonly delegate* unmanaged<uint, void> _linkProgram;
        private readonly delegate* unmanaged<uint, int, int*, void> _getProgramiv;
        private readonly delegate* unmanaged<uint, int, int*, byte*, void> _getProgramInfoLog;
        private readonly delegate* unmanaged<uint, void> _useProgram;
        private readonly delegate* unmanaged<uint, void> _deleteProgram;
        private readonly delegate* unmanaged<uint, byte*, int> _getUniformLocation;
        private readonly delegate* unmanaged<int, int, void> _uniform1i;
        private readonly delegate* unmanaged<int, float, void> _uniform1f;
        private readonly delegate* unmanaged<int, float, float, void> _uniform2f;
        private readonly delegate* unmanaged<int, float, float, float, void> _uniform3f;
        private readonly delegate* unmanaged<int, float, float, float, float, void> _uniform4f;
        private readonly delegate* unmanaged<int, int, float*, void> _uniform3fv;
        private readonly delegate* unmanaged<int, int, float*, void> _uniform4fv;
        private readonly delegate* unmanaged<int, int, byte, float*, void> _uniformMatrix4fv;
        private readonly delegate* unmanaged<int, int, int, void> _drawArrays;
        private readonly delegate* unmanaged<int, int, int, IntPtr, void> _drawElements;
        private readonly delegate* unmanaged<int, uint*, void> _genTextures;
        private readonly delegate* unmanaged<int, uint*, void> _deleteTextures;
        private readonly delegate* unmanaged<int, uint, void> _bindTexture;
        private readonly delegate* unmanaged<int, void> _activeTexture;
        private readonly delegate* unmanaged<int, int, int, int, int, int, int, int, void*, void> _texImage2D;
        private readonly delegate* unmanaged<int, int, int, void> _texParameteri;
        private readonly delegate* unmanaged<int, int, void> _pixelStorei;
        private readonly delegate* unmanaged<int, uint*, void> _genFramebuffers;
        private readonly delegate* unmanaged<int, uint*, void> _deleteFramebuffers;
        private readonly delegate* unmanaged<int, uint, void> _bindFramebuffer;
        private readonly delegate* unmanaged<int, int, int, uint, int, void> _framebufferTexture2D;
        private readonly delegate* unmanaged<int, int, int, uint, void> _framebufferRenderbuffer;
        private readonly delegate* unmanaged<int, int> _checkFramebufferStatus;
        private readonly delegate* unmanaged<int, uint*, void> _genRenderbuffers;
        private readonly delegate* unmanaged<int, uint*, void> _deleteRenderbuffers;
        private readonly delegate* unmanaged<int, uint, void> _bindRenderbuffer;
        private readonly delegate* unmanaged<int, int, int, int, void> _renderbufferStorage;
        private readonly delegate* unmanaged<int, int, int, int, int, void> _renderbufferStorageMultisample;
        private readonly delegate* unmanaged<int, int, int, int, int, int, int, int, int, int, void> _blitFramebuffer;
        private readonly delegate* unmanaged<int, int, int, int, int, int, void*, void> _readPixels;
        private readonly delegate* unmanaged<int, int*, void> _getIntegerv;
        private readonly delegate* unmanaged<int> _getError;
        private readonly delegate* unmanaged<void> _finish;

        public GL(Func<string, IntPtr> load)
        {
            void* P(string name)
            {
                IntPtr p = load(name);
                if (p == IntPtr.Zero) throw new InvalidOperationException("The OpenGL function " + name + " is not available.");
                return (void*)p;
            }
            _viewport = (delegate* unmanaged<int, int, int, int, void>)P("glViewport");
            _scissor = (delegate* unmanaged<int, int, int, int, void>)P("glScissor");
            _clearColor = (delegate* unmanaged<float, float, float, float, void>)P("glClearColor");
            _clearDepth = (delegate* unmanaged<double, void>)P("glClearDepth");
            _clear = (delegate* unmanaged<int, void>)P("glClear");
            _enable = (delegate* unmanaged<int, void>)P("glEnable");
            _disable = (delegate* unmanaged<int, void>)P("glDisable");
            _depthFunc = (delegate* unmanaged<int, void>)P("glDepthFunc");
            _depthMask = (delegate* unmanaged<byte, void>)P("glDepthMask");
            _colorMask = (delegate* unmanaged<byte, byte, byte, byte, void>)P("glColorMask");
            _blendFuncSeparate = (delegate* unmanaged<int, int, int, int, void>)P("glBlendFuncSeparate");
            _cullFace = (delegate* unmanaged<int, void>)P("glCullFace");
            _polygonOffset = (delegate* unmanaged<float, float, void>)P("glPolygonOffset");
            _genBuffers = (delegate* unmanaged<int, uint*, void>)P("glGenBuffers");
            _deleteBuffers = (delegate* unmanaged<int, uint*, void>)P("glDeleteBuffers");
            _bindBuffer = (delegate* unmanaged<int, uint, void>)P("glBindBuffer");
            _bufferData = (delegate* unmanaged<int, IntPtr, void*, int, void>)P("glBufferData");
            _genVertexArrays = (delegate* unmanaged<int, uint*, void>)P("glGenVertexArrays");
            _deleteVertexArrays = (delegate* unmanaged<int, uint*, void>)P("glDeleteVertexArrays");
            _bindVertexArray = (delegate* unmanaged<uint, void>)P("glBindVertexArray");
            _enableVertexAttribArray = (delegate* unmanaged<uint, void>)P("glEnableVertexAttribArray");
            _disableVertexAttribArray = (delegate* unmanaged<uint, void>)P("glDisableVertexAttribArray");
            _vertexAttribPointer = (delegate* unmanaged<uint, int, int, byte, int, IntPtr, void>)P("glVertexAttribPointer");
            _vertexAttrib4f = (delegate* unmanaged<uint, float, float, float, float, void>)P("glVertexAttrib4f");
            _vertexAttrib1f = (delegate* unmanaged<uint, float, void>)P("glVertexAttrib1f");
            _createShader = (delegate* unmanaged<int, uint>)P("glCreateShader");
            _shaderSource = (delegate* unmanaged<uint, int, byte**, int*, void>)P("glShaderSource");
            _compileShader = (delegate* unmanaged<uint, void>)P("glCompileShader");
            _getShaderiv = (delegate* unmanaged<uint, int, int*, void>)P("glGetShaderiv");
            _getShaderInfoLog = (delegate* unmanaged<uint, int, int*, byte*, void>)P("glGetShaderInfoLog");
            _deleteShader = (delegate* unmanaged<uint, void>)P("glDeleteShader");
            _createProgram = (delegate* unmanaged<uint>)P("glCreateProgram");
            _attachShader = (delegate* unmanaged<uint, uint, void>)P("glAttachShader");
            _bindAttribLocation = (delegate* unmanaged<uint, uint, byte*, void>)P("glBindAttribLocation");
            _bindFragDataLocation = (delegate* unmanaged<uint, uint, byte*, void>)P("glBindFragDataLocation");
            _linkProgram = (delegate* unmanaged<uint, void>)P("glLinkProgram");
            _getProgramiv = (delegate* unmanaged<uint, int, int*, void>)P("glGetProgramiv");
            _getProgramInfoLog = (delegate* unmanaged<uint, int, int*, byte*, void>)P("glGetProgramInfoLog");
            _useProgram = (delegate* unmanaged<uint, void>)P("glUseProgram");
            _deleteProgram = (delegate* unmanaged<uint, void>)P("glDeleteProgram");
            _getUniformLocation = (delegate* unmanaged<uint, byte*, int>)P("glGetUniformLocation");
            _uniform1i = (delegate* unmanaged<int, int, void>)P("glUniform1i");
            _uniform1f = (delegate* unmanaged<int, float, void>)P("glUniform1f");
            _uniform2f = (delegate* unmanaged<int, float, float, void>)P("glUniform2f");
            _uniform3f = (delegate* unmanaged<int, float, float, float, void>)P("glUniform3f");
            _uniform4f = (delegate* unmanaged<int, float, float, float, float, void>)P("glUniform4f");
            _uniform3fv = (delegate* unmanaged<int, int, float*, void>)P("glUniform3fv");
            _uniform4fv = (delegate* unmanaged<int, int, float*, void>)P("glUniform4fv");
            _uniformMatrix4fv = (delegate* unmanaged<int, int, byte, float*, void>)P("glUniformMatrix4fv");
            _drawArrays = (delegate* unmanaged<int, int, int, void>)P("glDrawArrays");
            _drawElements = (delegate* unmanaged<int, int, int, IntPtr, void>)P("glDrawElements");
            _genTextures = (delegate* unmanaged<int, uint*, void>)P("glGenTextures");
            _deleteTextures = (delegate* unmanaged<int, uint*, void>)P("glDeleteTextures");
            _bindTexture = (delegate* unmanaged<int, uint, void>)P("glBindTexture");
            _activeTexture = (delegate* unmanaged<int, void>)P("glActiveTexture");
            _texImage2D = (delegate* unmanaged<int, int, int, int, int, int, int, int, void*, void>)P("glTexImage2D");
            _texParameteri = (delegate* unmanaged<int, int, int, void>)P("glTexParameteri");
            _pixelStorei = (delegate* unmanaged<int, int, void>)P("glPixelStorei");
            _genFramebuffers = (delegate* unmanaged<int, uint*, void>)P("glGenFramebuffers");
            _deleteFramebuffers = (delegate* unmanaged<int, uint*, void>)P("glDeleteFramebuffers");
            _bindFramebuffer = (delegate* unmanaged<int, uint, void>)P("glBindFramebuffer");
            _framebufferTexture2D = (delegate* unmanaged<int, int, int, uint, int, void>)P("glFramebufferTexture2D");
            _framebufferRenderbuffer = (delegate* unmanaged<int, int, int, uint, void>)P("glFramebufferRenderbuffer");
            _checkFramebufferStatus = (delegate* unmanaged<int, int>)P("glCheckFramebufferStatus");
            _genRenderbuffers = (delegate* unmanaged<int, uint*, void>)P("glGenRenderbuffers");
            _deleteRenderbuffers = (delegate* unmanaged<int, uint*, void>)P("glDeleteRenderbuffers");
            _bindRenderbuffer = (delegate* unmanaged<int, uint, void>)P("glBindRenderbuffer");
            _renderbufferStorage = (delegate* unmanaged<int, int, int, int, void>)P("glRenderbufferStorage");
            _renderbufferStorageMultisample = (delegate* unmanaged<int, int, int, int, int, void>)P("glRenderbufferStorageMultisample");
            _blitFramebuffer = (delegate* unmanaged<int, int, int, int, int, int, int, int, int, int, void>)P("glBlitFramebuffer");
            _readPixels = (delegate* unmanaged<int, int, int, int, int, int, void*, void>)P("glReadPixels");
            _getIntegerv = (delegate* unmanaged<int, int*, void>)P("glGetIntegerv");
            _getError = (delegate* unmanaged<int>)P("glGetError");
            _finish = (delegate* unmanaged<void>)P("glFinish");
        }

        public void Viewport(int x, int y, int w, int h) { _viewport(x, y, w, h); }
        public void Scissor(int x, int y, int w, int h) { _scissor(x, y, w, h); }
        public void ClearColor(float r, float g, float b, float a) { _clearColor(r, g, b, a); }
        public void ClearDepth(double d) { _clearDepth(d); }
        public void Clear(int mask) { _clear(mask); }
        public void Enable(int cap) { _enable(cap); }
        public void Disable(int cap) { _disable(cap); }
        public void DepthFunc(int f) { _depthFunc(f); }
        public void DepthMask(bool m) { _depthMask((byte)(m ? 1 : 0)); }
        public void ColorMask(bool r, bool g, bool b, bool a)
        {
            _colorMask((byte)(r ? 1 : 0), (byte)(g ? 1 : 0), (byte)(b ? 1 : 0), (byte)(a ? 1 : 0));
        }
        public void BlendFuncSeparate(int a, int b, int c, int d) { _blendFuncSeparate(a, b, c, d); }
        public void CullFace(int m) { _cullFace(m); }
        public void PolygonOffset(float f, float u) { _polygonOffset(f, u); }
        public uint GenBuffer() { uint b; _genBuffers(1, &b); return b; }
        public void DeleteBuffer(uint b) { if (b != 0) _deleteBuffers(1, &b); }
        public void BindBuffer(int target, uint b) { _bindBuffer(target, b); }
        public void BufferData<T>(int target, T[] data, int count, int usage) where T : unmanaged
        {
            fixed (T* p = data) _bufferData(target, (IntPtr)(count * sizeof(T)), p, usage);
        }
        public uint GenVertexArray() { uint b; _genVertexArrays(1, &b); return b; }
        public void DeleteVertexArray(uint b) { if (b != 0) _deleteVertexArrays(1, &b); }
        public void BindVertexArray(uint b) { _bindVertexArray(b); }
        public void EnableVertexAttribArray(uint i) { _enableVertexAttribArray(i); }
        public void DisableVertexAttribArray(uint i) { _disableVertexAttribArray(i); }
        public void VertexAttribPointer(uint i, int size, int type, bool normalized, int stride, int offset)
        {
            _vertexAttribPointer(i, size, type, (byte)(normalized ? 1 : 0), stride, (IntPtr)offset);
        }
        public void VertexAttrib4f(uint i, float a, float b, float c, float d) { _vertexAttrib4f(i, a, b, c, d); }
        public void VertexAttrib1f(uint i, float a) { _vertexAttrib1f(i, a); }
        public void UseProgram(uint p) { _useProgram(p); }
        public void DeleteProgram(uint p) { _deleteProgram(p); }
        public int GetUniformLocation(uint program, string name)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(name + "\0");
            fixed (byte* p = bytes) return _getUniformLocation(program, p);
        }
        public void Uniform1i(int l, int v) { _uniform1i(l, v); }
        public void Uniform1f(int l, float v) { _uniform1f(l, v); }
        public void Uniform2f(int l, float a, float b) { _uniform2f(l, a, b); }
        public void Uniform3f(int l, float a, float b, float c) { _uniform3f(l, a, b, c); }
        public void Uniform4f(int l, float a, float b, float c, float d) { _uniform4f(l, a, b, c, d); }
        public void Uniform3fv(int l, float[] v) { fixed (float* p = v) _uniform3fv(l, v.Length / 3, p); }
        public void Uniform4fv(int l, float[] v) { fixed (float* p = v) _uniform4fv(l, v.Length / 4, p); }
        // The matrices are row major; OpenGL transposes them
        public void UniformMatrix4(int l, float[] m) { fixed (float* p = m) _uniformMatrix4fv(l, 1, 1, p); }
        public void DrawArrays(int mode, int first, int count) { _drawArrays(mode, first, count); }
        public void DrawElements(int mode, int count, int offset) { _drawElements(mode, count, UNSIGNED_INT, (IntPtr)offset); }
        public uint GenTexture() { uint t; _genTextures(1, &t); return t; }
        public void DeleteTexture(uint t) { if (t != 0) _deleteTextures(1, &t); }
        public void BindTexture(int target, uint t) { _bindTexture(target, t); }
        public void ActiveTexture(int unit) { _activeTexture(unit); }
        public void TexImage2D(int target, int level, int internalFormat, int w, int h, int format, int type, byte[] data)
        {
            fixed (byte* p = data) _texImage2D(target, level, internalFormat, w, h, 0, format, type, p);
        }
        public void TexParameteri(int target, int name, int value) { _texParameteri(target, name, value); }
        public void PixelStorei(int name, int value) { _pixelStorei(name, value); }
        public uint GenFramebuffer() { uint f; _genFramebuffers(1, &f); return f; }
        public void DeleteFramebuffer(uint f) { if (f != 0) _deleteFramebuffers(1, &f); }
        public void BindFramebuffer(int target, uint f) { _bindFramebuffer(target, f); }
        public void FramebufferTexture2D(int target, int attachment, int texTarget, uint tex, int level)
        {
            _framebufferTexture2D(target, attachment, texTarget, tex, level);
        }
        public void FramebufferRenderbuffer(int target, int attachment, int rbTarget, uint rb)
        {
            _framebufferRenderbuffer(target, attachment, rbTarget, rb);
        }
        public int CheckFramebufferStatus(int target) { return _checkFramebufferStatus(target); }
        public uint GenRenderbuffer() { uint r; _genRenderbuffers(1, &r); return r; }
        public void DeleteRenderbuffer(uint r) { if (r != 0) _deleteRenderbuffers(1, &r); }
        public void BindRenderbuffer(int target, uint r) { _bindRenderbuffer(target, r); }
        public void RenderbufferStorage(int target, int format, int w, int h) { _renderbufferStorage(target, format, w, h); }
        public void RenderbufferStorageMultisample(int target, int samples, int format, int w, int h)
        {
            _renderbufferStorageMultisample(target, samples, format, w, h);
        }
        public void BlitFramebuffer(int sx0, int sy0, int sx1, int sy1, int dx0, int dy0, int dx1, int dy1, int mask, int filter)
        {
            _blitFramebuffer(sx0, sy0, sx1, sy1, dx0, dy0, dx1, dy1, mask, filter);
        }
        public void ReadPixels(int x, int y, int w, int h, int format, int type, byte[] data)
        {
            fixed (byte* p = data) _readPixels(x, y, w, h, format, type, p);
        }
        public int GetInteger(int name) { int v; _getIntegerv(name, &v); return v; }
        public int[] GetIntegers(int name, int count)
        {
            int[] v = new int[Math.Max(4, count)];
            fixed (int* p = v) _getIntegerv(name, p);
            return v;
        }
        public int GetError() { return _getError(); }
        public void Finish() { _finish(); }

        // Programs
        public uint CompileProgram(string vertex, string geometry, string fragment, string[] attributes)
        {
            uint program = _createProgram();
            uint vs = Compile(VERTEX_SHADER, vertex);
            uint fs = Compile(FRAGMENT_SHADER, fragment);
            uint gs = geometry != null ? Compile(GEOMETRY_SHADER, geometry) : 0;
            _attachShader(program, vs);
            _attachShader(program, fs);
            if (gs != 0) _attachShader(program, gs);
            for (int i = 0; i < attributes.Length; i++)
            {
                byte[] name = Encoding.ASCII.GetBytes(attributes[i] + "\0");
                fixed (byte* p = name) _bindAttribLocation(program, (uint)i, p);
            }
            byte[] output = Encoding.ASCII.GetBytes("fragColor\0");
            fixed (byte* p = output) _bindFragDataLocation(program, 0, p);
            _linkProgram(program);
            int status;
            _getProgramiv(program, LINK_STATUS, &status);
            if (status == 0)
            {
                int length;
                _getProgramiv(program, INFO_LOG_LENGTH, &length);
                byte[] log = new byte[Math.Max(1, length)];
                fixed (byte* p = log) _getProgramInfoLog(program, log.Length, &length, p);
                throw new InvalidOperationException("The OpenGL program could not be linked: " + Encoding.ASCII.GetString(log));
            }
            _deleteShader(vs);
            _deleteShader(fs);
            if (gs != 0) _deleteShader(gs);
            return program;
        }
        private uint Compile(int type, string source)
        {
            uint shader = _createShader(type);
            byte[] bytes = Encoding.ASCII.GetBytes(source + "\0");
            fixed (byte* p = bytes)
            {
                byte* sources = p;
                _shaderSource(shader, 1, &sources, null);
            }
            _compileShader(shader);
            int status;
            _getShaderiv(shader, COMPILE_STATUS, &status);
            if (status == 0)
            {
                int length;
                _getShaderiv(shader, INFO_LOG_LENGTH, &length);
                byte[] log = new byte[Math.Max(1, length)];
                fixed (byte* l = log) _getShaderInfoLog(shader, log.Length, &length, l);
                throw new InvalidOperationException("The OpenGL shader could not be compiled: " + Encoding.ASCII.GetString(log));
            }
            return shader;
        }
    }
}
