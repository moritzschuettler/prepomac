// PrePoMax for macOS - draws the renderers of a render window with OpenGL: the layers in order, the 3D props lit by the
// lights of the renderer as the fixed function pipeline of VTK did, the translucent props after the opaque ones and the
// 2D props over them in display coordinates.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Kitware.VTK.Gl
{
    internal class SceneRenderer
    {
        // Shaders
        private const string VertexTemplate = @"#version 330 core
in vec3 aPosition;
in vec3 aNormal;
in vec4 aColor;
in float aT;
uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProj;
uniform mat4 uNormalMatrix;
uniform vec4 uClipPlanes[6];
uniform int uNumClip;
uniform float uPointSize;
uniform int uNumLights;
uniform vec3 uLightDir[8];
uniform vec3 uLightColor[8];
uniform float uPower;
uniform int uParallel;
out vec3 {P}Eye;
out vec3 {P}DiffF;
out vec3 {P}SpecF;
out vec3 {P}DiffB;
out vec3 {P}SpecB;
out vec3 {P}Normal;
out vec4 {P}Color;
out float {P}T;
out float {P}Clip[6];
void main()
{
    vec4 world = uModel * vec4(aPosition, 1.0);
    vec4 eye = uView * world;
    {P}Eye = eye.xyz;
    {P}Normal = (uNormalMatrix * vec4(aNormal, 0.0)).xyz;
    vec3 n = length({P}Normal) > 0.0 ? normalize({P}Normal) : vec3(0.0, 0.0, 1.0);
    vec3 view = uParallel == 1 ? vec3(0.0, 0.0, 1.0) : normalize(-eye.xyz);
    vec3 df = vec3(0.0), sf = vec3(0.0), db = vec3(0.0), sb = vec3(0.0);
    for (int i = 0; i < 8; i++)
    {
        if (i >= uNumLights) break;
        vec3 l = uLightDir[i];
        vec3 h = normalize(l + view);
        float d1 = max(dot(n, l), 0.0);
        float d2 = max(dot(-n, l), 0.0);
        df += uLightColor[i] * d1;
        db += uLightColor[i] * d2;
        if (uPower > 0.0)
        {
            if (d1 > 0.0) sf += uLightColor[i] * pow(max(dot(n, h), 0.0), uPower);
            if (d2 > 0.0) sb += uLightColor[i] * pow(max(dot(-n, h), 0.0), uPower);
        }
    }
    {P}DiffF = df; {P}SpecF = sf; {P}DiffB = db; {P}SpecB = sb;
    {P}Color = aColor;
    {P}T = aT;
    for (int i = 0; i < 6; i++) {P}Clip[i] = i < uNumClip ? dot(uClipPlanes[i].xyz, world.xyz) + uClipPlanes[i].w : 1.0;
    gl_Position = uProj * eye;
    gl_PointSize = uPointSize;
}";
        private const string LineGeometry = @"#version 330 core
layout(lines) in;
layout(triangle_strip, max_vertices = 4) out;
in vec3 vEye[];
in vec3 vNormal[];
in vec3 vDiffF[];
in vec3 vSpecF[];
in vec3 vDiffB[];
in vec3 vSpecB[];
in vec4 vColor[];
in float vT[];
in float vClip[][6];
out vec3 fEye;
out vec3 fNormal;
out vec3 fDiffF;
out vec3 fSpecF;
out vec3 fDiffB;
out vec3 fSpecB;
out vec4 fColor;
out float fT;
out float fClip[6];
uniform vec2 uViewportPx;
uniform float uLineWidthPx;
void Emit(int i, vec2 offset)
{
    vec4 p = gl_in[i].gl_Position;
    gl_Position = p + vec4(offset * p.w, 0.0, 0.0);
    fEye = vEye[i];
    fNormal = vNormal[i];
    fDiffF = vDiffF[i]; fSpecF = vSpecF[i]; fDiffB = vDiffB[i]; fSpecB = vSpecB[i];
    fColor = vColor[i];
    fT = vT[i];
    for (int k = 0; k < 6; k++) fClip[k] = vClip[i][k];
    EmitVertex();
}
void main()
{
    vec4 p0 = gl_in[0].gl_Position;
    vec4 p1 = gl_in[1].gl_Position;
    vec2 s0 = p0.xy / max(abs(p0.w), 1e-20) * uViewportPx * 0.5;
    vec2 s1 = p1.xy / max(abs(p1.w), 1e-20) * uViewportPx * 0.5;
    vec2 d = s1 - s0;
    float l = length(d);
    vec2 dir = l > 1e-6 ? d / l : vec2(1.0, 0.0);
    vec2 n = vec2(-dir.y, dir.x) * uLineWidthPx * 0.5 / (uViewportPx * 0.5);
    Emit(0, n);
    Emit(0, -n);
    Emit(1, n);
    Emit(1, -n);
    EndPrimitive();
}";
        private const string Fragment = @"#version 330 core
in vec3 fEye;
in vec3 fNormal;
in vec3 fDiffF;
in vec3 fSpecF;
in vec3 fDiffB;
in vec3 fSpecB;
in vec4 fColor;
in float fT;
in float fClip[6];
out vec4 fragColor;
uniform int uLighting;
uniform int uFlat;
uniform int uHasNormals;
uniform int uColorMode;      // 0 property, 1 vertex replaces diffuse, 2 vertex replaces ambient, 3 texture, 4 vertex unlit
uniform int uTwoSided;
uniform int uRoundPoints;
uniform int uParallel;
uniform vec3 uAmbient;       // coefficient times color
uniform vec3 uDiffuse;
uniform vec3 uSpecular;
uniform float uPower;
uniform float uOpacity;
uniform vec3 uUnlitColor;
uniform int uNumLights;
uniform vec3 uLightDir[8];
uniform vec3 uLightColor[8];
uniform sampler2D uLut;
void main()
{
    for (int i = 0; i < 6; i++) if (fClip[i] < 0.0) discard;
    if (uRoundPoints == 1)
    {
        vec2 c = gl_PointCoord - vec2(0.5);
        if (dot(c, c) > 0.25) discard;
    }
    vec4 vertexColor = fColor;
    vec3 ambient = uAmbient;
    vec3 diffuse = uDiffuse;
    float alpha = uOpacity;
    if (uColorMode == 1) { diffuse = vertexColor.rgb; alpha *= vertexColor.a; }
    else if (uColorMode == 2) { ambient = vertexColor.rgb; alpha *= vertexColor.a; }
    vec3 color;
    if (uLighting == 0)
    {
        color = (uColorMode == 1 || uColorMode == 2 || uColorMode == 4) ? vertexColor.rgb : uUnlitColor;
        if (uColorMode == 4) alpha *= vertexColor.a;
    }
    else
    {
        vec3 diffLight;
        vec3 specLight;
        if (uFlat == 1 || uHasNormals == 0)
        {
            // Lighting of the face
            vec3 view = uParallel == 1 ? vec3(0.0, 0.0, 1.0) : normalize(-fEye);
            vec3 n = normalize(cross(dFdx(fEye), dFdy(fEye)));
            if (dot(n, view) < 0.0) n = -n;
            diffLight = vec3(0.0);
            specLight = vec3(0.0);
            for (int i = 0; i < 8; i++)
            {
                if (i >= uNumLights) break;
                vec3 l = uLightDir[i];
                float d = max(dot(n, l), 0.0);
                diffLight += uLightColor[i] * d;
                if (d > 0.0 && uPower > 0.0) specLight += uLightColor[i] * pow(max(dot(n, normalize(l + view)), 0.0), uPower);
            }
        }
        else if (uTwoSided == 1 && !gl_FrontFacing) { diffLight = fDiffB; specLight = fSpecB; }
        else { diffLight = fDiffF; specLight = fSpecF; }
        color = ambient + diffuse * diffLight;
        vec3 spec = uSpecular * specLight;
        color += spec;
    }
    if (uColorMode == 3)
    {
        vec4 t = texture(uLut, vec2(clamp(fT, 0.0005, 0.9995), 0.5));
        color *= t.rgb;
        alpha *= t.a;
    }
    fragColor = vec4(clamp(color, 0.0, 1.0), alpha);
}";
        private const string TextVertex = @"#version 330 core
in vec2 aPosition;
in vec2 aUv;
uniform mat4 uProj;
out vec2 vUv;
void main() { vUv = aUv; gl_Position = uProj * vec4(aPosition, 0.0, 1.0); }";
        private const string TextFragment = @"#version 330 core
in vec2 vUv;
out vec4 fragColor;
uniform sampler2D uTexture;
uniform float uAlpha;
void main() { fragColor = texture(uTexture, vUv) * uAlpha; }";
        private const string BackgroundVertex = @"#version 330 core
in vec2 aPosition;
uniform vec3 uBottom;
uniform vec3 uTop;
out vec3 vColor;
void main() { vColor = mix(uBottom, uTop, (aPosition.y + 1.0) * 0.5); gl_Position = vec4(aPosition, 0.0, 1.0); }";
        private const string BackgroundFragment = @"#version 330 core
in vec3 vColor;
out vec4 fragColor;
void main() { fragColor = vec4(vColor, 1.0); }";

        private static readonly string[] MeshAttributes = { "aPosition", "aNormal", "aColor", "aT" };

        // Resources of the context
        private GL _gl;
        private uint _meshProgram, _lineProgram, _textProgram, _backgroundProgram;
        private readonly Dictionary<uint, Dictionary<string, int>> _uniforms = new Dictionary<uint, Dictionary<string, int>>();
        private uint _quadVao, _quadVbo;
        private uint _fbo, _colorRb, _depthRb;
        private int _fboWidth, _fboHeight, _samples;
        private uint _whiteTexture;
        private readonly Dictionary<object, GpuMesh> _meshes = new Dictionary<object, GpuMesh>(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<object, LutTexture> _luts = new Dictionary<object, LutTexture>(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<string, TextTexture> _texts = new Dictionary<string, TextTexture>();
        private int _frame;

        public bool Initialized { get { return _gl != null; } }
        public GL Gl { get { return _gl; } }

        public void Initialize(GL gl)
        {
            _gl = gl;
            _meshProgram = gl.CompileProgram(VertexTemplate.Replace("{P}", "f"), null, Fragment, MeshAttributes);
            _lineProgram = gl.CompileProgram(VertexTemplate.Replace("{P}", "v"), LineGeometry, Fragment, MeshAttributes);
            _textProgram = gl.CompileProgram(TextVertex, null, TextFragment, new[] { "aPosition", "aUv" });
            _backgroundProgram = gl.CompileProgram(BackgroundVertex, null, BackgroundFragment, new[] { "aPosition" });
            _quadVao = gl.GenVertexArray();
            _quadVbo = gl.GenBuffer();
            _whiteTexture = gl.GenTexture();
            gl.BindTexture(GL.TEXTURE_2D, _whiteTexture);
            gl.TexImage2D(GL.TEXTURE_2D, 0, GL.RGBA8, 1, 1, GL.RGBA, GL.UNSIGNED_BYTE, new byte[] { 255, 255, 255, 255 });
            gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, GL.NEAREST);
            gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, GL.NEAREST);
            int maxSamples = gl.GetInteger(GL.MAX_SAMPLES);
            _samples = Math.Max(0, Math.Min(4, maxSamples));
        }
        private int U(uint program, string name)
        {
            if (!_uniforms.TryGetValue(program, out var map)) _uniforms[program] = map = new Dictionary<string, int>();
            if (!map.TryGetValue(name, out int location)) map[name] = location = _gl.GetUniformLocation(program, name);
            return location;
        }

        // Frame
        // Renders the window into the target framebuffer of the size in device pixels; scale is device pixels per display pixel
        public void Render(vtkRenderWindow window, uint targetFbo, int width, int height, double scale)
        {
            GL gl = _gl;
            if (width <= 0 || height <= 0) return;
            _frame++;
            EnsureFramebuffer(width, height);
            gl.BindFramebuffer(GL.FRAMEBUFFER, _fbo);
            gl.Viewport(0, 0, width, height);
            gl.Disable(GL.SCISSOR_TEST);
            gl.ColorMask(true, true, true, true);
            gl.DepthMask(true);
            gl.ClearColor(0, 0, 0, 1);
            gl.ClearDepth(1);
            gl.Clear(GL.COLOR_BUFFER_BIT | GL.DEPTH_BUFFER_BIT);
            if (_samples > 0) gl.Enable(GL.MULTISAMPLE);
            int[] logical = window.GetSize();
            foreach (vtkRenderer renderer in window.GetRenderers().Items.OrderBy(r => r.GetLayer()).ToArray())
            {
                try
                {
                    RenderRenderer(renderer, width, height, scale, logical);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("The view could not draw a layer: " + ex);
                }
            }
            gl.Disable(GL.SCISSOR_TEST);
            gl.BindFramebuffer(GL.READ_FRAMEBUFFER, _fbo);
            gl.BindFramebuffer(GL.DRAW_FRAMEBUFFER, targetFbo);
            gl.BlitFramebuffer(0, 0, width, height, 0, 0, width, height, GL.COLOR_BUFFER_BIT, GL.NEAREST);
            gl.BindFramebuffer(GL.FRAMEBUFFER, targetFbo);
            // Unused meshes and texts are released after a while
            if (_frame % 120 == 0) ReleaseUnused();
        }
        private void EnsureFramebuffer(int width, int height)
        {
            GL gl = _gl;
            if (_fbo != 0 && _fboWidth == width && _fboHeight == height) return;
            gl.DeleteFramebuffer(_fbo);
            gl.DeleteRenderbuffer(_colorRb);
            gl.DeleteRenderbuffer(_depthRb);
            _fbo = gl.GenFramebuffer();
            gl.BindFramebuffer(GL.FRAMEBUFFER, _fbo);
            _colorRb = gl.GenRenderbuffer();
            gl.BindRenderbuffer(GL.RENDERBUFFER, _colorRb);
            if (_samples > 0) gl.RenderbufferStorageMultisample(GL.RENDERBUFFER, _samples, GL.RGBA8, width, height);
            else gl.RenderbufferStorage(GL.RENDERBUFFER, GL.RGBA8, width, height);
            gl.FramebufferRenderbuffer(GL.FRAMEBUFFER, GL.COLOR_ATTACHMENT0, GL.RENDERBUFFER, _colorRb);
            _depthRb = gl.GenRenderbuffer();
            gl.BindRenderbuffer(GL.RENDERBUFFER, _depthRb);
            if (_samples > 0) gl.RenderbufferStorageMultisample(GL.RENDERBUFFER, _samples, GL.DEPTH24_STENCIL8, width, height);
            else gl.RenderbufferStorage(GL.RENDERBUFFER, GL.DEPTH24_STENCIL8, width, height);
            gl.FramebufferRenderbuffer(GL.FRAMEBUFFER, GL.DEPTH_STENCIL_ATTACHMENT, GL.RENDERBUFFER, _depthRb);
            if (gl.CheckFramebufferStatus(GL.FRAMEBUFFER) != GL.FRAMEBUFFER_COMPLETE && _samples > 0)
            {
                // Without multisampling
                _samples = 0;
                _fbo = 0;
                EnsureFramebuffer(width, height);
                return;
            }
            _fboWidth = width;
            _fboHeight = height;
        }

        // A renderer
        private struct FrameState
        {
            public vtkRenderer Renderer;
            public vtkCamera Camera;
            public float[] View, Proj;
            public double[] ViewD;
            public int Vx, Vy, Vw, Vh;      // device viewport
            public double Scale;
            public int[] Logical;
            public float[] LightDirs, LightColors;
            public int NumLights;
        }
        private void RenderRenderer(vtkRenderer renderer, int width, int height, double scale, int[] logical)
        {
            GL gl = _gl;
            double[] vp = renderer.GetViewport();
            FrameState fs = new FrameState
            {
                Renderer = renderer,
                Scale = scale,
                Logical = logical,
                Vx = (int)Math.Round(vp[0] * width),
                Vy = (int)Math.Round(vp[1] * height),
            };
            fs.Vw = (int)Math.Round(vp[2] * width) - fs.Vx;
            fs.Vh = (int)Math.Round(vp[3] * height) - fs.Vy;
            if (fs.Vw <= 0 || fs.Vh <= 0) return;
            gl.Viewport(fs.Vx, fs.Vy, fs.Vw, fs.Vh);
            gl.Enable(GL.SCISSOR_TEST);
            gl.Scissor(fs.Vx, fs.Vy, fs.Vw, fs.Vh);
            // Clear: the first layer gets the background, every layer its own depth
            if (!renderer.Transparent())
            {
                double[] b = renderer.GetBackground();
                if (renderer.GetGradientBackground()) DrawGradient(b, renderer.GetBackground2());
                else
                {
                    gl.ClearColor((float)b[0], (float)b[1], (float)b[2], 1);
                    gl.Clear(GL.COLOR_BUFFER_BIT);
                }
                gl.DepthMask(true);
                gl.Clear(GL.DEPTH_BUFFER_BIT);
            }
            else if (!renderer.PreserveDepthBuffer)
            {
                gl.DepthMask(true);
                gl.Clear(GL.DEPTH_BUFFER_BIT);
            }
            vtkCamera camera = renderer.GetActiveCamera();
            fs.Camera = camera;
            double aspect = renderer.GetTiledAspectRatio();
            fs.ViewD = camera.ViewMatrix();
            fs.View = ToFloat(fs.ViewD);
            fs.Proj = ToFloat(camera.ProjectionMatrix(aspect, -1, 1));
            SetupLights(ref fs);
            List<vtkProp> props = renderer.Props.ToList();
            // 2D props in the background
            foreach (vtkProp prop in props)
                if (prop is vtkActor2D a2 && a2.Visible && a2.GetProperty()._displayLocation == 0) Draw2D(a2, ref fs);
            // Opaque and translucent geometry
            gl.Enable(GL.DEPTH_TEST);
            gl.DepthFunc(GL.LEQUAL);
            gl.Disable(GL.BLEND);
            gl.DepthMask(true);
            List<vtkActor> translucent = new List<vtkActor>();
            foreach (vtkProp prop in props)
            {
                if (!(prop is vtkActor actor) || !actor.Visible || actor.GetMapper() == null) continue;
                if (actor.GetProperty().GetOpacity() < 1) { translucent.Add(actor); continue; }
                DrawActor(actor, ref fs, false);
            }
            if (translucent.Count > 0)
            {
                gl.Enable(GL.BLEND);
                gl.BlendFuncSeparate(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA, GL.ONE, GL.ONE_MINUS_SRC_ALPHA);
                gl.DepthMask(false);
                foreach (vtkActor actor in translucent) DrawActor(actor, ref fs, true);
                gl.DepthMask(true);
            }
            // Overlay
            gl.Disable(GL.DEPTH_TEST);
            gl.Disable(GL.CULL_FACE);
            gl.Enable(GL.BLEND);
            gl.BlendFuncSeparate(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA, GL.ONE, GL.ONE_MINUS_SRC_ALPHA);
            foreach (vtkProp prop in props)
            {
                if (!prop.Visible) continue;
                if (prop is vtkActor2D a2 && a2.GetProperty()._displayLocation != 0) Draw2D(a2, ref fs);
                else if (prop is vtkCaptionRepresentation cr && cr.GetCaptionActor2D() != null) Draw2D(cr.GetCaptionActor2D(), ref fs);
            }
            gl.Disable(GL.BLEND);
        }
        private void SetupLights(ref FrameState fs)
        {
            List<vtkLight> lights = fs.Renderer.Lights.Where(l => l._switch).ToList();
            if (lights.Count == 0 && fs.Renderer.AutomaticLightCreation)
            {
                vtkLight head = new vtkLight();
                head.SetLightTypeToHeadlight();
                lights.Add(head);
            }
            int n = Math.Min(8, lights.Count);
            fs.LightDirs = new float[24];
            fs.LightColors = new float[24];
            for (int i = 0; i < n; i++)
            {
                double[] d = lights[i].EyeDirection(fs.Camera);
                for (int c = 0; c < 3; c++)
                {
                    fs.LightDirs[3 * i + c] = (float)d[c];
                    fs.LightColors[3 * i + c] = (float)(lights[i]._color[c] * lights[i]._intensity);
                }
            }
            fs.NumLights = n;
        }
        private void DrawGradient(double[] bottom, double[] top)
        {
            GL gl = _gl;
            gl.Disable(GL.DEPTH_TEST);
            gl.Disable(GL.BLEND);
            gl.Disable(GL.CULL_FACE);
            gl.UseProgram(_backgroundProgram);
            gl.Uniform3f(U(_backgroundProgram, "uBottom"), (float)bottom[0], (float)bottom[1], (float)bottom[2]);
            gl.Uniform3f(U(_backgroundProgram, "uTop"), (float)top[0], (float)top[1], (float)top[2]);
            float[] quad = { -1, -1, 1, -1, -1, 1, 1, 1 };
            gl.BindVertexArray(_quadVao);
            gl.BindBuffer(GL.ARRAY_BUFFER, _quadVbo);
            gl.BufferData(GL.ARRAY_BUFFER, quad, quad.Length, GL.DYNAMIC_DRAW);
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, GL.FLOAT, false, 8, 0);
            gl.DisableVertexAttribArray(1);
            gl.DrawArrays(GL.TRIANGLE_STRIP, 0, 4);
            gl.BindVertexArray(0);
        }

        // 3D actors
        private void DrawActor(vtkActor actor, ref FrameState fs, bool translucent)
        {
            vtkMapper mapper = actor.GetMapper();
            vtkPolyData pd = mapper.GetRenderPolyData();
            if (pd == null || pd.GetPoints() == null || pd.GetNumberOfPoints() == 0) return;
            vtkProperty property = actor.GetProperty();
            GpuMesh mesh = GetMesh(mapper, pd, property);
            if (mesh == null) return;
            double[] model = actor is vtkFollower f ? f.FollowerMatrix(fs.Camera) : actor.ModelMatrix;
            vtkProperty back = actor.GetBackfaceProperty();
            if (back != null && !property._backfaceCulling && mesh.TriangleCount > 0)
            {
                // The back faces with the back face property
                DrawMesh(mesh, mapper, property, model, ref fs, translucent, cull: GL.BACK, linesAndPoints: true);
                DrawMesh(mesh, mapper, back, model, ref fs, translucent, cull: GL.FRONT, linesAndPoints: false);
            }
            else
            {
                int cull = property._backfaceCulling ? GL.BACK : property._frontfaceCulling ? GL.FRONT : 0;
                DrawMesh(mesh, mapper, property, model, ref fs, translucent, cull, true);
            }
        }
        private void DrawMesh(GpuMesh mesh, vtkMapper mapper, vtkProperty p, double[] model, ref FrameState fs, bool translucent,
                              int cull, bool linesAndPoints)
        {
            GL gl = _gl;
            bool wire = p._representation == 1, points = p._representation == 0;
            // Triangles
            if (mesh.TriangleCount > 0 && !wire && !points)
            {
                if (cull != 0) { gl.Enable(GL.CULL_FACE); gl.CullFace(cull); } else gl.Disable(GL.CULL_FACE);
                if (vtkMapper.PolygonOffset) { gl.Enable(GL.POLYGON_OFFSET_FILL); gl.PolygonOffset(1, 1); }
                SetupProgram(_meshProgram, mesh, mapper, p, model, ref fs, false, 0, false);
                gl.BindVertexArray(mesh.Vao);
                if (mesh.Indexed) { gl.BindBuffer(GL.ELEMENT_ARRAY_BUFFER, mesh.TriangleEbo); gl.DrawElements(GL.TRIANGLES, mesh.TriangleCount, 0); }
                else gl.DrawArrays(GL.TRIANGLES, mesh.TriangleFirst, mesh.TriangleCount);
                gl.Disable(GL.POLYGON_OFFSET_FILL);
                gl.Disable(GL.CULL_FACE);
            }
            if (!linesAndPoints) { gl.BindVertexArray(0); return; }
            // Lines: the lines of the data and, for a wireframe, the edges of the polygons
            int lineCount = mesh.LineCount + (wire ? mesh.EdgeCount : 0);
            if (lineCount > 0 && !points)
            {
                gl.Disable(GL.CULL_FACE);
                float width = (float)(Math.Max(1, p._lineWidth) * fs.Scale);
                SetupProgram(_lineProgram, mesh, mapper, p, model, ref fs, true, width, false);
                gl.BindVertexArray(mesh.Vao);
                if (mesh.Indexed)
                {
                    if (mesh.LineCount > 0) { gl.BindBuffer(GL.ELEMENT_ARRAY_BUFFER, mesh.LineEbo); gl.DrawElements(GL.LINES, mesh.LineCount, 0); }
                    if (wire && mesh.EdgeCount > 0) { gl.BindBuffer(GL.ELEMENT_ARRAY_BUFFER, mesh.EdgeEbo); gl.DrawElements(GL.LINES, mesh.EdgeCount, 0); }
                }
                else
                {
                    if (mesh.LineCount > 0) gl.DrawArrays(GL.LINES, mesh.LineFirst, mesh.LineCount);
                    if (wire && mesh.EdgeCount > 0) gl.DrawArrays(GL.LINES, mesh.EdgeFirst, mesh.EdgeCount);
                }
            }
            // Points: the vertices of the data or all points
            int pointCount = points ? mesh.AllPointCount : mesh.PointCount;
            if (pointCount > 0)
            {
                gl.Disable(GL.CULL_FACE);
                gl.Enable(GL.PROGRAM_POINT_SIZE);
                SetupProgram(_meshProgram, mesh, mapper, p, model, ref fs, false, 0, true);
                gl.BindVertexArray(mesh.Vao);
                if (mesh.Indexed)
                {
                    gl.BindBuffer(GL.ELEMENT_ARRAY_BUFFER, points ? mesh.AllPointEbo : mesh.PointEbo);
                    gl.DrawElements(GL.POINTS, pointCount, 0);
                }
                else gl.DrawArrays(GL.POINTS, points ? mesh.AllPointFirst : mesh.PointFirst, pointCount);
            }
            gl.BindVertexArray(0);
        }
        private void SetupProgram(uint program, GpuMesh mesh, vtkMapper mapper, vtkProperty p, double[] model, ref FrameState fs,
                                  bool lines, float lineWidth, bool pointsDraw)
        {
            GL gl = _gl;
            gl.UseProgram(program);
            gl.UniformMatrix4(U(program, "uModel"), ToFloat(model));
            gl.UniformMatrix4(U(program, "uView"), fs.View);
            gl.UniformMatrix4(U(program, "uProj"), fs.Proj);
            double[] mv = vtkMatrix4x4.Multiply(fs.ViewD, model);
            double[] inv = vtkMatrix4x4.Inverse(mv);
            double[] normalMatrix = new double[16];
            for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) normalMatrix[4 * i + j] = inv[4 * j + i];
            gl.UniformMatrix4(U(program, "uNormalMatrix"), ToFloat(normalMatrix));
            // Clipping planes in world coordinates
            List<vtkPlane> planes = mapper.ClippingPlanes;
            int numClip = Math.Min(6, planes.Count);
            float[] clip = new float[24];
            for (int i = 0; i < numClip; i++)
            {
                double[] n = planes[i].GetNormal(), o = planes[i].GetOrigin();
                clip[4 * i] = (float)n[0]; clip[4 * i + 1] = (float)n[1]; clip[4 * i + 2] = (float)n[2];
                clip[4 * i + 3] = (float)-(n[0] * o[0] + n[1] * o[1] + n[2] * o[2]);
            }
            gl.Uniform4fv(U(program, "uClipPlanes"), clip);
            gl.Uniform1i(U(program, "uNumClip"), numClip);
            float pointSize = (float)(Math.Max(1, p._pointSize) * fs.Scale);
            gl.Uniform1f(U(program, "uPointSize"), pointSize);
            gl.Uniform1i(U(program, "uRoundPoints"), pointsDraw && pointSize > 2 && (fs.Renderer.GetRenderWindow()?.PointSmoothing ?? true) ? 1 : 0);
            if (lines)
            {
                gl.Uniform2f(U(program, "uViewportPx"), fs.Vw, fs.Vh);
                gl.Uniform1f(U(program, "uLineWidthPx"), lineWidth);
            }
            // Material
            bool lighting = p._lighting && !(lines || pointsDraw) || (p._lighting && mesh.HasNormals && !pointsDraw && !lines);
            // Lines and points are lit like VTK lit them only with normals; otherwise they are drawn in their color
            if (lines || pointsDraw) lighting = false;
            int colorMode = 0;
            if (mesh.ColorMode == ColorKind.Texture) colorMode = 3;
            else if (mesh.ColorMode == ColorKind.Vertex)
                colorMode = !lighting ? 4 : p._ambient > p._diffuse ? 2 : 1;
            gl.Uniform1i(U(program, "uLighting"), lighting ? 1 : 0);
            gl.Uniform1i(U(program, "uFlat"), p._interpolation == 0 ? 1 : 0);
            gl.Uniform1i(U(program, "uHasNormals"), mesh.HasNormals ? 1 : 0);
            gl.Uniform1i(U(program, "uColorMode"), colorMode);
            gl.Uniform1i(U(program, "uTwoSided"), fs.Renderer.TwoSidedLighting ? 1 : 0);
            gl.Uniform1i(U(program, "uParallel"), fs.Camera.GetParallelProjection());
            gl.Uniform3f(U(program, "uAmbient"), (float)(p._ambient * p._ambientColor[0]), (float)(p._ambient * p._ambientColor[1]),
                         (float)(p._ambient * p._ambientColor[2]));
            gl.Uniform3f(U(program, "uDiffuse"), (float)(p._diffuse * p._diffuseColor[0]), (float)(p._diffuse * p._diffuseColor[1]),
                         (float)(p._diffuse * p._diffuseColor[2]));
            gl.Uniform3f(U(program, "uSpecular"), (float)(p._specular * p._specularColor[0]),
                         (float)(p._specular * p._specularColor[1]), (float)(p._specular * p._specularColor[2]));
            gl.Uniform1f(U(program, "uPower"), p._specular > 0 ? (float)Math.Min(128, Math.Max(1, p._specularPower)) : 0);
            gl.Uniform1f(U(program, "uOpacity"), (float)p._opacity);
            double[] unlit = p.GetColor();
            gl.Uniform3f(U(program, "uUnlitColor"), (float)unlit[0], (float)unlit[1], (float)unlit[2]);
            gl.Uniform1i(U(program, "uNumLights"), fs.NumLights);
            gl.Uniform3fv(U(program, "uLightDir"), fs.LightDirs);
            gl.Uniform3fv(U(program, "uLightColor"), fs.LightColors);
            gl.ActiveTexture(GL.TEXTURE0);
            gl.BindTexture(GL.TEXTURE_2D, mesh.ColorMode == ColorKind.Texture && mesh.Lut != null ? mesh.Lut.Texture : _whiteTexture);
            gl.Uniform1i(U(program, "uLut"), 0);
        }

        // Meshes
        internal enum ColorKind { None, Vertex, Texture }
        internal class LutTexture
        {
            public uint Texture;
            public ulong Time;
            public double[] Range;
        }
        internal class GpuMesh
        {
            public uint Vao, Vbo, TriangleEbo, LineEbo, EdgeEbo, PointEbo, AllPointEbo;
            public bool Indexed, HasNormals;
            public int TriangleCount, LineCount, EdgeCount, PointCount, AllPointCount;
            public int TriangleFirst, LineFirst, EdgeFirst, PointFirst, AllPointFirst;
            public ColorKind ColorMode;
            public LutTexture Lut;
            public vtkPolyData Data;
            public ulong DataTime, MapperTime, LutTime;
            public int Representation, Interpolation;
            public int LastUsedFrame;
        }
        private GpuMesh GetMesh(vtkMapper mapper, vtkPolyData pd, vtkProperty p)
        {
            vtkScalarsToColors lut = mapper._lookupTable;
            ulong lutTime = lut != null ? lut.MTime : 0;
            if (_meshes.TryGetValue(mapper, out GpuMesh mesh))
            {
                if (mesh.Data == pd && mesh.DataTime == pd.DataTime && mesh.MapperTime == mapper.MTime && mesh.LutTime == lutTime &&
                    mesh.Representation == p._representation && mesh.Interpolation == p._interpolation)
                {
                    mesh.LastUsedFrame = _frame;
                    return mesh;
                }
                DeleteMesh(mesh);
                _meshes.Remove(mapper);
            }
            mesh = BuildMesh(mapper, pd, p);
            if (mesh == null) return null;
            mesh.Data = pd;
            mesh.DataTime = pd.DataTime;
            mesh.MapperTime = mapper.MTime;
            mesh.LutTime = lutTime;
            mesh.Representation = p._representation;
            mesh.Interpolation = p._interpolation;
            mesh.LastUsedFrame = _frame;
            _meshes[mapper] = mesh;
            return mesh;
        }
        private void DeleteMesh(GpuMesh mesh)
        {
            GL gl = _gl;
            gl.DeleteVertexArray(mesh.Vao);
            gl.DeleteBuffer(mesh.Vbo);
            gl.DeleteBuffer(mesh.TriangleEbo);
            gl.DeleteBuffer(mesh.LineEbo);
            gl.DeleteBuffer(mesh.EdgeEbo);
            gl.DeleteBuffer(mesh.PointEbo);
            gl.DeleteBuffer(mesh.AllPointEbo);
            if (mesh.Lut != null) gl.DeleteTexture(mesh.Lut.Texture);
        }
        private void ReleaseUnused()
        {
            foreach (var entry in _meshes.Where(e => _frame - e.Value.LastUsedFrame > 600).ToList())
            {
                DeleteMesh(entry.Value);
                _meshes.Remove(entry.Key);
            }
            foreach (var entry in _texts.Where(e => _frame - e.Value.LastUsedFrame > 600).ToList())
            {
                _gl.DeleteTexture(entry.Value.Texture);
                _texts.Remove(entry.Key);
            }
        }
        // The vertex layout: position (3 floats), normal (3 floats), color (4 bytes), t (1 float) = 32 bytes
        private const int Stride = 32;
        private GpuMesh BuildMesh(vtkMapper mapper, vtkPolyData pd, vtkProperty p)
        {
            GL gl = _gl;
            long numPoints = pd.GetNumberOfPoints();
            double[] pts = pd.GetPoints().Raw;
            vtkDataArray normals = pd.GetPointData().GetNormals();
            bool hasNormals = normals != null && normals.GetNumberOfTuples() >= numPoints && normals.GetNumberOfComponents() == 3;
            // Colors
            vtkDataArray scalars = mapper.GetColorScalars(pd, out bool cellScalars);
            ColorKind colorKind = ColorKind.None;
            byte[] pointColors = null;
            float[] pointT = null;
            byte[] cellColors = null;
            LutTexture lutTexture = null;
            if (scalars != null && scalars.GetNumberOfTuples() > 0)
            {
                bool direct = mapper._colorMode == 0 && scalars is vtkUnsignedCharArray;
                vtkScalarsToColors lut = mapper.GetLookupTable();
                double[] range = mapper.ColorRange();
                if (!cellScalars && !direct && mapper._interpolateScalarsBeforeMapping && lut is vtkLookupTable)
                {
                    colorKind = ColorKind.Texture;
                    lutTexture = BuildLutTexture((vtkLookupTable)lut);
                    pointT = new float[numPoints];
                    double span = range[1] - range[0];
                    for (long i = 0; i < numPoints && i < scalars.GetNumberOfTuples(); i++)
                    {
                        double v = ScalarValue(scalars, i);
                        pointT[i] = double.IsNaN(v) ? float.NaN : span > 0 ? (float)((v - range[0]) / span) : 0;
                    }
                }
                else
                {
                    colorKind = ColorKind.Vertex;
                    byte[] colors = MapColors(scalars, lut, range, direct);
                    if (cellScalars) cellColors = colors;
                    else pointColors = colors;
                }
            }
            // Cells
            List<int> triangles = new List<int>();
            List<long> triangleCells = new List<long>();
            List<int> lines = new List<int>();
            List<long> lineCells = new List<long>();
            List<int> edges = new List<int>();
            List<int> vertices = new List<int>();
            List<long> vertexCells = new List<long>();
            for (int kind = 0; kind < 4; kind++)
            {
                vtkCellArray ca = pd.Array(kind);
                if (ca.Count == 0) continue;
                long[] cellIds = cellScalars ? pd.CellIdsOfKind(kind) : null;
                List<long> conn = ca.Connectivity;
                for (int c = 0; c < ca.Count; c++)
                {
                    int start = (int)ca.CellStart(c), size = ca.CellSize(c);
                    long cellId = cellIds != null ? cellIds[c] : -1;
                    if (kind == 0)
                    {
                        for (int j = 0; j < size; j++) { vertices.Add((int)conn[start + j]); vertexCells.Add(cellId); }
                    }
                    else if (kind == 1)
                    {
                        for (int j = 0; j + 1 < size; j++)
                        {
                            lines.Add((int)conn[start + j]);
                            lines.Add((int)conn[start + j + 1]);
                            lineCells.Add(cellId);
                        }
                    }
                    else if (kind == 2)
                    {
                        for (int j = 1; j + 1 < size; j++)
                        {
                            triangles.Add((int)conn[start]);
                            triangles.Add((int)conn[start + j]);
                            triangles.Add((int)conn[start + j + 1]);
                            triangleCells.Add(cellId);
                        }
                        if (p._representation == 1)
                            for (int j = 0; j < size; j++) { edges.Add((int)conn[start + j]); edges.Add((int)conn[start + (j + 1) % size]); }
                    }
                    else
                    {
                        for (int j = 0; j + 2 < size; j++)
                        {
                            int a = (int)conn[start + j], b = (int)conn[start + j + 1], d = (int)conn[start + j + 2];
                            if (j % 2 == 0) { triangles.Add(a); triangles.Add(b); triangles.Add(d); }
                            else { triangles.Add(b); triangles.Add(a); triangles.Add(d); }
                            triangleCells.Add(cellId);
                        }
                    }
                }
            }
            GpuMesh mesh = new GpuMesh { HasNormals = hasNormals, ColorMode = colorKind, Lut = lutTexture };
            mesh.Vao = gl.GenVertexArray();
            mesh.Vbo = gl.GenBuffer();
            gl.BindVertexArray(mesh.Vao);
            gl.BindBuffer(GL.ARRAY_BUFFER, mesh.Vbo);
            byte[] vertexData;
            bool expanded = cellColors != null;
            if (!expanded)
            {
                vertexData = new byte[numPoints * Stride];
                for (long i = 0; i < numPoints; i++)
                    WriteVertex(vertexData, i, pts, i, hasNormals ? normals : null, pointColors, pointColors != null ? i : -1, pointT, i);
                mesh.Indexed = true;
                mesh.TriangleCount = triangles.Count;
                mesh.LineCount = lines.Count;
                mesh.EdgeCount = edges.Count;
                mesh.PointCount = vertices.Count;
                mesh.AllPointCount = (int)numPoints;
                mesh.TriangleEbo = Upload(triangles);
                mesh.LineEbo = Upload(lines);
                mesh.EdgeEbo = Upload(edges);
                mesh.PointEbo = Upload(vertices);
                if (p._representation == 0) mesh.AllPointEbo = Upload(Enumerable.Range(0, (int)numPoints).ToList());
            }
            else
            {
                // Each cell has its own color, so the vertices are written per cell
                int total = triangles.Count + lines.Count + edges.Count + vertices.Count;
                vertexData = new byte[(long)total * Stride];
                long k = 0;
                mesh.TriangleFirst = 0;
                for (int i = 0; i < triangles.Count; i++)
                    WriteVertex(vertexData, k++, pts, triangles[i], hasNormals ? normals : null, cellColors, triangleCells[i / 3], null, 0);
                mesh.LineFirst = (int)k;
                for (int i = 0; i < lines.Count; i++)
                    WriteVertex(vertexData, k++, pts, lines[i], hasNormals ? normals : null, cellColors, lineCells[i / 2], null, 0);
                mesh.EdgeFirst = (int)k;
                for (int i = 0; i < edges.Count; i++)
                    WriteVertex(vertexData, k++, pts, edges[i], hasNormals ? normals : null, null, -1, null, 0);
                mesh.PointFirst = (int)k;
                for (int i = 0; i < vertices.Count; i++)
                    WriteVertex(vertexData, k++, pts, vertices[i], hasNormals ? normals : null, cellColors, vertexCells[i], null, 0);
                mesh.TriangleCount = triangles.Count;
                mesh.LineCount = lines.Count;
                mesh.EdgeCount = edges.Count;
                mesh.PointCount = vertices.Count;
                mesh.AllPointFirst = mesh.TriangleFirst;
                mesh.AllPointCount = p._representation == 0 ? triangles.Count : 0;
            }
            gl.BufferData(GL.ARRAY_BUFFER, vertexData, vertexData.Length, GL.STATIC_DRAW);
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 3, GL.FLOAT, false, Stride, 0);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 3, GL.FLOAT, false, Stride, 12);
            gl.EnableVertexAttribArray(2);
            gl.VertexAttribPointer(2, 4, GL.UNSIGNED_BYTE, true, Stride, 24);
            gl.EnableVertexAttribArray(3);
            gl.VertexAttribPointer(3, 1, GL.FLOAT, false, Stride, 28);
            gl.BindVertexArray(0);
            return mesh;
        }
        private uint Upload(List<int> indices)
        {
            if (indices.Count == 0) return 0;
            uint ebo = _gl.GenBuffer();
            _gl.BindBuffer(GL.ELEMENT_ARRAY_BUFFER, ebo);
            int[] data = indices.ToArray();
            _gl.BufferData(GL.ELEMENT_ARRAY_BUFFER, data, data.Length, GL.STATIC_DRAW);
            return ebo;
        }
        private static void WriteVertex(byte[] data, long index, double[] pts, long point, vtkDataArray normals, byte[] colors,
                                        long colorIndex, float[] t, long tIndex)
        {
            long o = index * Stride;
            float x = (float)pts[3 * point], y = (float)pts[3 * point + 1], z = (float)pts[3 * point + 2];
            BitConverter.TryWriteBytes(new Span<byte>(data, (int)o, 4), x);
            BitConverter.TryWriteBytes(new Span<byte>(data, (int)o + 4, 4), y);
            BitConverter.TryWriteBytes(new Span<byte>(data, (int)o + 8, 4), z);
            if (normals != null)
            {
                double[] raw = normals.RawData;
                BitConverter.TryWriteBytes(new Span<byte>(data, (int)o + 12, 4), (float)raw[3 * point]);
                BitConverter.TryWriteBytes(new Span<byte>(data, (int)o + 16, 4), (float)raw[3 * point + 1]);
                BitConverter.TryWriteBytes(new Span<byte>(data, (int)o + 20, 4), (float)raw[3 * point + 2]);
            }
            if (colors != null && colorIndex >= 0 && 4 * colorIndex + 3 < colors.Length)
            {
                data[o + 24] = colors[4 * colorIndex];
                data[o + 25] = colors[4 * colorIndex + 1];
                data[o + 26] = colors[4 * colorIndex + 2];
                data[o + 27] = colors[4 * colorIndex + 3];
            }
            else
            {
                data[o + 24] = data[o + 25] = data[o + 26] = data[o + 27] = 255;
            }
            if (t != null && tIndex < t.Length)
                BitConverter.TryWriteBytes(new Span<byte>(data, (int)o + 28, 4), t[tIndex]);
        }
        private static double ScalarValue(vtkDataArray scalars, long i)
        {
            int nc = scalars.GetNumberOfComponents();
            if (nc == 1) return scalars.GetTuple1(i);
            double s = 0;
            for (int c = 0; c < nc; c++) { double v = scalars.GetComponent(i, c); s += v * v; }
            return Math.Sqrt(s);
        }
        // The colors of the scalars: RGBA bytes per tuple
        internal static byte[] MapColors(vtkDataArray scalars, vtkScalarsToColors lut, double[] range, bool direct)
        {
            long n = scalars.GetNumberOfTuples();
            byte[] colors = new byte[4 * n];
            int nc = scalars.GetNumberOfComponents();
            if (direct)
            {
                for (long i = 0; i < n; i++)
                {
                    if (nc >= 3)
                    {
                        colors[4 * i] = (byte)scalars.GetComponent(i, 0);
                        colors[4 * i + 1] = (byte)scalars.GetComponent(i, 1);
                        colors[4 * i + 2] = (byte)scalars.GetComponent(i, 2);
                        colors[4 * i + 3] = nc >= 4 ? (byte)scalars.GetComponent(i, 3) : (byte)255;
                    }
                    else
                    {
                        byte v = (byte)scalars.GetComponent(i, 0);
                        colors[4 * i] = colors[4 * i + 1] = colors[4 * i + 2] = v;
                        colors[4 * i + 3] = nc == 2 ? (byte)scalars.GetComponent(i, 1) : (byte)255;
                    }
                }
                return colors;
            }
            vtkLookupTable table = lut as vtkLookupTable;
            double[][] entries = table?.Table;
            for (long i = 0; i < n; i++)
            {
                double v = ScalarValue(scalars, i);
                double[] c;
                if (table != null && entries.Length > 0)
                {
                    if (double.IsNaN(v)) c = table.NanColor;
                    else
                    {
                        double span = range[1] - range[0];
                        double t = span > 0 ? (v - range[0]) / span : 0;
                        int index = (int)Math.Floor(t * entries.Length);
                        if (index < 0) index = 0;
                        if (index >= entries.Length) index = entries.Length - 1;
                        c = entries[index];
                    }
                }
                else
                {
                    double[] rgb = lut.GetColor(v);
                    c = new[] { rgb[0], rgb[1], rgb[2], lut.GetOpacity(v) };
                }
                colors[4 * i] = (byte)Math.Round(255 * Clamp(c[0]));
                colors[4 * i + 1] = (byte)Math.Round(255 * Clamp(c[1]));
                colors[4 * i + 2] = (byte)Math.Round(255 * Clamp(c[2]));
                colors[4 * i + 3] = (byte)Math.Round(255 * Clamp(c.Length > 3 ? c[3] : 1));
            }
            return colors;
        }
        private static double Clamp(double v) { return v < 0 ? 0 : v > 1 ? 1 : v; }
        private LutTexture BuildLutTexture(vtkLookupTable lut)
        {
            double[][] table = lut.Table;
            int n = Math.Max(1, table.Length);
            byte[] data = new byte[4 * n];
            for (int i = 0; i < table.Length; i++)
                for (int c = 0; c < 4; c++) data[4 * i + c] = (byte)Math.Round(255 * Clamp(table[i][c]));
            uint texture = _gl.GenTexture();
            _gl.BindTexture(GL.TEXTURE_2D, texture);
            _gl.PixelStorei(GL.UNPACK_ALIGNMENT, 1);
            _gl.TexImage2D(GL.TEXTURE_2D, 0, GL.RGBA8, n, 1, GL.RGBA, GL.UNSIGNED_BYTE, data);
            _gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, GL.NEAREST);
            _gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, GL.NEAREST);
            _gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, GL.CLAMP_TO_EDGE);
            _gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, GL.CLAMP_TO_EDGE);
            return new LutTexture { Texture = texture, Time = lut.MTime };
        }

        // 2D actors
        private void Draw2D(vtkActor2D actor, ref FrameState fs)
        {
            vtkMapper2D mapper = actor.GetMapper();
            vtkRenderer renderer = fs.Renderer;
            if (actor is vtkTextActor ta)
            {
                double[] pos = actor.DisplayPosition(renderer);
                DrawText(ta.GetInput(), ta.GetTextProperty(), pos[0], pos[1], ref fs);
                return;
            }
            if (actor is vtkCaptionActor2D ca)
            {
                DrawCaption(ca, ref fs);
                return;
            }
            if (mapper is vtkTextMapper tm)
            {
                double[] pos = actor.DisplayPosition(renderer);
                DrawText(tm.GetInput(), tm.GetTextProperty(), pos[0], pos[1], ref fs);
                return;
            }
            if (mapper is vtkLabeledDataMapper lm)
            {
                foreach (var label in lm.Labels())
                {
                    double[] d = renderer.WorldToDisplayPoint(label.position);
                    if (d[2] < 0 || d[2] > 1) continue;
                    DrawText(label.label, lm.GetLabelTextProperty(), d[0], d[1], ref fs);
                }
                return;
            }
            if (mapper is vtkPolyDataMapper2D pm) DrawPolyData2D(actor, pm, ref fs);
        }
        private float[] DisplayProjection(ref FrameState fs)
        {
            // Display coordinates of the viewport to clip coordinates
            double[] vp = fs.Renderer.GetViewport();
            double w = fs.Logical[0], h = fs.Logical[1];
            double x0 = vp[0] * w, y0 = vp[1] * h, x1 = vp[2] * w, y1 = vp[3] * h;
            return new[] {
                (float)(2 / (x1 - x0)), 0, 0, (float)(-(x1 + x0) / (x1 - x0)),
                0, (float)(2 / (y1 - y0)), 0, (float)(-(y1 + y0) / (y1 - y0)),
                0, 0, -1, 0,
                0, 0, 0, 1 };
        }
        private void DrawPolyData2D(vtkActor2D actor, vtkPolyDataMapper2D mapper, ref FrameState fs)
        {
            vtkPolyData pd = mapper.RenderPolyData();
            if (pd == null || pd.GetPoints() == null || pd.GetNumberOfPoints() == 0) return;
            vtkRenderer renderer = fs.Renderer;
            vtkProperty2D prop = actor.GetProperty();
            long n = pd.GetNumberOfPoints();
            double[] origin = renderer.ViewportOriginDisplay();
            double[] actorPos = actor.GetPositionCoordinate().GetComputedDoubleViewportValue(renderer);
            double[] display = new double[3 * n];
            vtkCoordinate tc = mapper.GetTransformCoordinate();
            for (long i = 0; i < n; i++)
            {
                double[] p = pd.GetPoint(i);
                double x, y;
                if (tc != null)
                {
                    tc.SetValue(p[0], p[1], p[2]);
                    double[] d = tc.GetComputedDoubleDisplayValue(renderer);
                    x = d[0];
                    y = d[1];
                }
                else
                {
                    x = p[0] + actorPos[0] + origin[0];
                    y = p[1] + actorPos[1] + origin[1];
                }
                display[3 * i] = x;
                display[3 * i + 1] = y;
            }
            // Colors
            byte[] colors = null;
            bool cellColors = false;
            if (mapper._scalarVisibility)
            {
                vtkDataArray scalars = mapper._scalarMode == 2 ? pd.GetCellData().GetScalars() : pd.GetPointData().GetScalars();
                cellColors = mapper._scalarMode == 2 || (scalars == null && pd.GetCellData().GetScalars() != null);
                if (scalars == null) scalars = pd.GetCellData().GetScalars();
                if (scalars != null)
                {
                    bool direct = scalars is vtkUnsignedCharArray;
                    vtkScalarsToColors lut = mapper._lookupTable ?? DefaultLut();
                    colors = MapColors(scalars, lut, mapper._scalarRange, direct);
                }
            }
            // Triangles, lines and points written per cell
            List<float> data = new List<float>();
            List<byte> cols = new List<byte>();
            int triCount = 0, lineCount = 0, pointCount = 0;
            List<(int kind, long[] ids, long cell)> cells = new List<(int, long[], long)>();
            for (int kind = 0; kind < 4; kind++)
            {
                vtkCellArray ca = pd.Array(kind);
                long[] ids = pd.CellIdsOfKind(kind);
                for (int c = 0; c < ca.Count; c++) cells.Add((kind, ca.GetCell(c), ids[c]));
            }
            void Add(long point, long cell)
            {
                data.Add((float)display[3 * point]);
                data.Add((float)display[3 * point + 1]);
                long ci = cellColors ? cell : point;
                if (colors != null && ci >= 0 && 4 * ci + 3 < colors.Length)
                    for (int k = 0; k < 4; k++) cols.Add(colors[4 * ci + k]);
                else
                {
                    cols.Add((byte)(255 * prop._color[0])); cols.Add((byte)(255 * prop._color[1]));
                    cols.Add((byte)(255 * prop._color[2])); cols.Add(255);
                }
            }
            foreach (var c in cells.Where(x => x.kind >= 2))
            {
                if (c.kind == 2) for (int j = 1; j + 1 < c.ids.Length; j++) { Add(c.ids[0], c.cell); Add(c.ids[j], c.cell); Add(c.ids[j + 1], c.cell); triCount += 3; }
                else for (int j = 0; j + 2 < c.ids.Length; j++) { Add(c.ids[j], c.cell); Add(c.ids[j + 1], c.cell); Add(c.ids[j + 2], c.cell); triCount += 3; }
            }
            foreach (var c in cells.Where(x => x.kind == 1))
                for (int j = 0; j + 1 < c.ids.Length; j++) { Add(c.ids[j], c.cell); Add(c.ids[j + 1], c.cell); lineCount += 2; }
            foreach (var c in cells.Where(x => x.kind == 0))
                foreach (long id in c.ids) { Add(id, c.cell); pointCount++; }
            if (data.Count == 0) return;
            // Upload as 3D vertices in the mesh layout
            int count = data.Count / 2;
            byte[] vertexData = new byte[count * Stride];
            for (int i = 0; i < count; i++)
            {
                long o = i * Stride;
                BitConverter.TryWriteBytes(new Span<byte>(vertexData, (int)o, 4), data[2 * i]);
                BitConverter.TryWriteBytes(new Span<byte>(vertexData, (int)o + 4, 4), data[2 * i + 1]);
                for (int k = 0; k < 4; k++) vertexData[o + 24 + k] = cols[4 * i + k];
            }
            GL gl = _gl;
            uint vao = gl.GenVertexArray();
            uint vbo = gl.GenBuffer();
            gl.BindVertexArray(vao);
            gl.BindBuffer(GL.ARRAY_BUFFER, vbo);
            gl.BufferData(GL.ARRAY_BUFFER, vertexData, vertexData.Length, GL.DYNAMIC_DRAW);
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 3, GL.FLOAT, false, Stride, 0);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 3, GL.FLOAT, false, Stride, 12);
            gl.EnableVertexAttribArray(2);
            gl.VertexAttribPointer(2, 4, GL.UNSIGNED_BYTE, true, Stride, 24);
            gl.EnableVertexAttribArray(3);
            gl.VertexAttribPointer(3, 1, GL.FLOAT, false, Stride, 28);
            float[] proj = DisplayProjection(ref fs);
            float alpha = (float)prop._opacity;
            if (triCount > 0)
            {
                Setup2D(_meshProgram, proj, alpha, 1, false, ref fs);
                gl.DrawArrays(GL.TRIANGLES, 0, triCount);
            }
            if (lineCount > 0)
            {
                Setup2D(_lineProgram, proj, alpha, (float)(Math.Max(1, prop._lineWidth) * fs.Scale), false, ref fs);
                gl.DrawArrays(GL.LINES, triCount, lineCount);
            }
            if (pointCount > 0)
            {
                gl.Enable(GL.PROGRAM_POINT_SIZE);
                Setup2D(_meshProgram, proj, alpha, (float)(Math.Max(1, prop._pointSize) * fs.Scale), true, ref fs);
                gl.DrawArrays(GL.POINTS, triCount + lineCount, pointCount);
            }
            gl.BindVertexArray(0);
            gl.DeleteBuffer(vbo);
            gl.DeleteVertexArray(vao);
        }
        private static vtkLookupTable _defaultLut;
        private static vtkLookupTable DefaultLut()
        {
            if (_defaultLut == null) { _defaultLut = new vtkLookupTable(); _defaultLut.Build(); }
            return _defaultLut;
        }
        private static readonly float[] Identity = { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
        private void Setup2D(uint program, float[] proj, float alpha, float size, bool points, ref FrameState fs)
        {
            GL gl = _gl;
            gl.UseProgram(program);
            gl.UniformMatrix4(U(program, "uModel"), Identity);
            gl.UniformMatrix4(U(program, "uView"), Identity);
            gl.UniformMatrix4(U(program, "uProj"), proj);
            gl.UniformMatrix4(U(program, "uNormalMatrix"), Identity);
            gl.Uniform1i(U(program, "uNumClip"), 0);
            gl.Uniform1f(U(program, "uPointSize"), size);
            gl.Uniform1i(U(program, "uRoundPoints"), points && size > 2 ? 1 : 0);
            gl.Uniform2f(U(program, "uViewportPx"), fs.Vw, fs.Vh);
            gl.Uniform1f(U(program, "uLineWidthPx"), size);
            gl.Uniform1i(U(program, "uLighting"), 0);
            gl.Uniform1i(U(program, "uColorMode"), 4);
            gl.Uniform1f(U(program, "uOpacity"), alpha);
            gl.Uniform1i(U(program, "uNumLights"), 0);
            gl.ActiveTexture(GL.TEXTURE0);
            gl.BindTexture(GL.TEXTURE_2D, _whiteTexture);
            gl.Uniform1i(U(program, "uLut"), 0);
        }

        // Text
        private class TextTexture
        {
            public uint Texture;
            public int Width, Height;
            public int LastUsedFrame;
        }
        private TextTexture GetText(string text, vtkTextProperty p, double scale)
        {
            string key = string.Join("\u0001", text, p._fontFamily, p._fontSize, p._bold, p._italic, p._shadow, p._justification,
                                     p._lineSpacing, p._color[0], p._color[1], p._color[2], p._opacity, scale);
            if (_texts.TryGetValue(key, out TextTexture t)) { t.LastUsedFrame = _frame; return t; }
            byte[] pixels = TextRenderer.Rasterize(text, p, (float)scale, out int w, out int h);
            uint texture = _gl.GenTexture();
            _gl.BindTexture(GL.TEXTURE_2D, texture);
            _gl.PixelStorei(GL.UNPACK_ALIGNMENT, 1);
            _gl.TexImage2D(GL.TEXTURE_2D, 0, GL.RGBA8, w, h, GL.RGBA, GL.UNSIGNED_BYTE, pixels);
            _gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, GL.LINEAR);
            _gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, GL.LINEAR);
            _gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, GL.CLAMP_TO_EDGE);
            _gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, GL.CLAMP_TO_EDGE);
            t = new TextTexture { Texture = texture, Width = w, Height = h, LastUsedFrame = _frame };
            if (_texts.Count > 4000) { foreach (var e in _texts) _gl.DeleteTexture(e.Value.Texture); _texts.Clear(); }
            _texts[key] = t;
            return t;
        }
        // Draws the text at the display position with the justification of the text property
        private void DrawText(string text, vtkTextProperty p, double x, double y, ref FrameState fs)
        {
            if (string.IsNullOrEmpty(text)) return;
            TextTexture t = GetText(text, p, fs.Scale);
            double w = t.Width / fs.Scale, h = t.Height / fs.Scale;
            int[] size = TextRenderer.Measure(text, p);
            // The image has a margin of a pixel around the measured text
            double margin = 1 / fs.Scale;
            double x0 = x - margin, y0 = y - (h - size[1] - margin);
            if (p._justification == 1) x0 -= size[0] / 2.0;
            else if (p._justification == 2) x0 -= size[0];
            if (p._verticalJustification == 1) y0 -= size[1] / 2.0;
            else if (p._verticalJustification == 2) y0 -= size[1];
            double[][] corners = { new[] { x0, y0 }, new[] { x0 + w, y0 }, new[] { x0, y0 + h }, new[] { x0 + w, y0 + h } };
            if (Math.Abs(p._orientation) > 1e-6)
            {
                double a = p._orientation * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a);
                foreach (double[] q in corners)
                {
                    double dx = q[0] - x, dy = q[1] - y;
                    q[0] = x + c * dx - s * dy;
                    q[1] = y + s * dx + c * dy;
                }
            }
            // Pixel aligned at the device resolution
            if (Math.Abs(p._orientation) < 1e-6)
            {
                double ax = Math.Round(corners[0][0] * fs.Scale) / fs.Scale - corners[0][0];
                double ay = Math.Round(corners[0][1] * fs.Scale) / fs.Scale - corners[0][1];
                foreach (double[] q in corners) { q[0] += ax; q[1] += ay; }
            }
            float[] quad = {
                (float)corners[0][0], (float)corners[0][1], 0, 1,
                (float)corners[1][0], (float)corners[1][1], 1, 1,
                (float)corners[2][0], (float)corners[2][1], 0, 0,
                (float)corners[3][0], (float)corners[3][1], 1, 0 };
            GL gl = _gl;
            gl.Disable(GL.DEPTH_TEST);
            gl.Enable(GL.BLEND);
            gl.BlendFuncSeparate(GL.ONE, GL.ONE_MINUS_SRC_ALPHA, GL.ONE, GL.ONE_MINUS_SRC_ALPHA);
            gl.UseProgram(_textProgram);
            gl.UniformMatrix4(U(_textProgram, "uProj"), DisplayProjection(ref fs));
            gl.Uniform1f(U(_textProgram, "uAlpha"), 1);
            gl.ActiveTexture(GL.TEXTURE0);
            gl.BindTexture(GL.TEXTURE_2D, t.Texture);
            gl.Uniform1i(U(_textProgram, "uTexture"), 0);
            gl.BindVertexArray(_quadVao);
            gl.BindBuffer(GL.ARRAY_BUFFER, _quadVbo);
            gl.BufferData(GL.ARRAY_BUFFER, quad, quad.Length, GL.DYNAMIC_DRAW);
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, GL.FLOAT, false, 16, 0);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 2, GL.FLOAT, false, 16, 8);
            gl.DrawArrays(GL.TRIANGLE_STRIP, 0, 4);
            gl.DisableVertexAttribArray(1);
            gl.BindVertexArray(0);
            gl.BlendFuncSeparate(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA, GL.ONE, GL.ONE_MINUS_SRC_ALPHA);
        }
        private void DrawCaption(vtkCaptionActor2D actor, ref FrameState fs)
        {
            vtkRenderer renderer = fs.Renderer;
            double[] pos = actor.GetPositionCoordinate().GetComputedDoubleDisplayValue(renderer);
            if (actor.GetPositionCoordinate().GetCoordinateSystem() == 5)
            {
                double[] d = renderer.WorldToDisplayPoint(actor.GetPositionCoordinate().GetValue());
                if (d[2] < 0 || d[2] > 1) return;
            }
            vtkTextProperty p = actor.GetCaptionTextProperty();
            DrawText(actor.GetCaption(), p, pos[0] + actor.Padding, pos[1] + actor.Padding, ref fs);
        }

        private static float[] ToFloat(double[] m)
        {
            float[] f = new float[m.Length];
            for (int i = 0; i < m.Length; i++) f[i] = (float)m[i];
            return f;
        }
    }
}
