// PrePoMax for macOS - interaction of the VTK compatible library: the render window interactor, the interactor styles,
// the pickers and the caption widget. The pickers cast rays through the scene on the CPU instead of picking with the
// graphics hardware.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Kitware.VTK
{
    public partial class vtkRenderWindowInteractor
    {
        private vtkRenderWindow _renderWindow;
        private vtkInteractorObserver _style;
        private int[] _eventPosition = new int[2];
        private int[] _lastEventPosition = new int[2];
        private int _controlKey, _shiftKey, _altKey;
        private sbyte _keyCode;
        private string _keySym = "";
        private int _repeatCount;
        private bool _lightFollowCamera = true;
        private bool _enabled = true;
        private vtkAbstractPicker _picker;
        private int[] _size = new int[2];

        public static new vtkRenderWindowInteractor New() { return new vtkGenericRenderWindowInteractor(); }

        public void SetRenderWindow(vtkRenderWindow window)
        {
            _renderWindow = window;
            if (window != null && window.GetInteractor() != this) window.SetInteractor(this);
        }
        public vtkRenderWindow GetRenderWindow() { return _renderWindow; }
        public void SetInteractorStyle(vtkInteractorObserver style)
        {
            if (_style == style) return;
            if (_style is vtkInteractorStyle old) old.SetInteractor(null);
            _style = style;
            if (style is vtkInteractorStyle s) s.SetInteractor(this);
        }
        public vtkInteractorObserver GetInteractorStyle() { return _style; }
        public void SetPicker(vtkAbstractPicker picker) { _picker = picker; }
        public vtkAbstractPicker GetPicker() { return _picker ?? (_picker = new vtkCellPicker()); }
        public void Initialize() { }
        public void Enable() { _enabled = true; }
        public void Disable() { _enabled = false; }
        public int GetEnabled() { return _enabled ? 1 : 0; }
        public void Start() { }
        public void TerminateApp() { }
        public void Render()
        {
            _renderWindow?.Render();
        }
        public int GetLightFollowCamera() { return _lightFollowCamera ? 1 : 0; }
        public void SetLightFollowCamera(int v) { _lightFollowCamera = v != 0; }
        public void LightFollowCameraOn() { _lightFollowCamera = true; }
        public void LightFollowCameraOff() { _lightFollowCamera = false; }
        // Event information
        public int[] GetEventPosition() { return (int[])_eventPosition.Clone(); }
        public int[] GetLastEventPosition() { return (int[])_lastEventPosition.Clone(); }
        public void SetEventPosition(int x, int y)
        {
            _lastEventPosition = _eventPosition;
            _eventPosition = new[] { x, y };
        }
        public void SetEventPositionFlipY(int x, int y)
        {
            int height = _renderWindow != null ? _renderWindow.GetSize()[1] : 0;
            SetEventPosition(x, height - y - 1);
        }
        public int GetControlKey() { return _controlKey; }
        public int GetShiftKey() { return _shiftKey; }
        public int GetAltKey() { return _altKey; }
        public void SetControlKey(int v) { _controlKey = v; }
        public void SetShiftKey(int v) { _shiftKey = v; }
        public void SetAltKey(int v) { _altKey = v; }
        public sbyte GetKeyCode() { return _keyCode; }
        public void SetKeyCode(sbyte c) { _keyCode = c; }
        public string GetKeySym() { return _keySym; }
        public void SetKeySym(string s) { _keySym = s ?? ""; }
        public int GetRepeatCount() { return _repeatCount; }
        public void SetRepeatCount(int c) { _repeatCount = c; }
        public void SetEventInformation(int x, int y, int ctrl, int shift, sbyte keycode, int repeatcount, string keysym)
        {
            SetEventPosition(x, y);
            _controlKey = ctrl;
            _shiftKey = shift;
            _keyCode = keycode;
            _repeatCount = repeatcount;
            _keySym = keysym ?? "";
        }
        public void SetEventInformationFlipY(int x, int y, int ctrl, int shift, sbyte keycode, int repeatcount, string keysym)
        {
            int height = _renderWindow != null ? _renderWindow.GetSize()[1] : 0;
            SetEventInformation(x, height - y - 1, ctrl, shift, keycode, repeatcount, keysym);
        }
        public void SetKeyEventInformation(int ctrl, int shift, sbyte keycode, int repeatcount, string keysym)
        {
            _controlKey = ctrl;
            _shiftKey = shift;
            _keyCode = keycode;
            _repeatCount = repeatcount;
            _keySym = keysym ?? "";
        }
        public void SetSize(int w, int h) { _size = new[] { w, h }; }
        public int[] GetSize() { return _renderWindow?.GetSize() ?? (int[])_size.Clone(); }
        // The renderer at the position: the top most interactive renderer whose viewport contains it
        public vtkRenderer FindPokedRenderer(int x, int y)
        {
            if (_renderWindow == null) return null;
            vtkRenderer found = null;
            foreach (vtkRenderer r in _renderWindow.GetRenderers().Items)
            {
                if (r.GetInteractive() == 0) continue;
                if (r.IsInViewport(x, y) && (found == null || r.GetLayer() >= found.GetLayer())) found = r;
            }
            if (found == null)
            {
                foreach (vtkRenderer r in _renderWindow.GetRenderers().Items)
                    if (r.GetInteractive() != 0) { found = r; break; }
            }
            return found ?? _renderWindow.GetRenderers().GetFirstRenderer();
        }
        // Events
        private void Dispatch(uint eventId)
        {
            if (!_enabled) return;
            InvokeEvent(eventId, IntPtr.Zero);
            if (_style is vtkInteractorStyle style) style.ProcessInteractorEvent(eventId);
        }
        public void LeftButtonPressEvent() { Dispatch(vtkCommandIds.LeftButtonPressEvent); }
        public void LeftButtonReleaseEvent() { Dispatch(vtkCommandIds.LeftButtonReleaseEvent); }
        public void MiddleButtonPressEvent() { Dispatch(vtkCommandIds.MiddleButtonPressEvent); }
        public void MiddleButtonReleaseEvent() { Dispatch(vtkCommandIds.MiddleButtonReleaseEvent); }
        public void RightButtonPressEvent() { Dispatch(vtkCommandIds.RightButtonPressEvent); }
        public void RightButtonReleaseEvent() { Dispatch(vtkCommandIds.RightButtonReleaseEvent); }
        public void MouseMoveEvent() { Dispatch(vtkCommandIds.MouseMoveEvent); }
        public void MouseWheelForwardEvent() { Dispatch(vtkCommandIds.MouseWheelForwardEvent); }
        public void MouseWheelBackwardEvent() { Dispatch(vtkCommandIds.MouseWheelBackwardEvent); }
        public void KeyPressEvent() { Dispatch(vtkCommandIds.KeyPressEvent); }
        public void KeyReleaseEvent() { Dispatch(vtkCommandIds.KeyReleaseEvent); }
        public void CharEvent() { Dispatch(vtkCommandIds.CharEvent); }
        public void EnterEvent() { Dispatch(vtkCommandIds.EnterEvent); }
        public void LeaveEvent() { Dispatch(vtkCommandIds.LeaveEvent); }
        public void ExposeEvent() { Dispatch(vtkCommandIds.ExposeEvent); }
        public void ConfigureEvent() { Dispatch(vtkCommandIds.ConfigureEvent); }
        public void ExitEvent() { Dispatch(vtkCommandIds.ExitEvent); }
        public int CreateTimer(int type) { return 1; }
        public int DestroyTimer() { return 1; }
        public int CreateRepeatingTimer(uint duration) { return 1; }
        public int DestroyTimer(int id) { return 1; }
        public void SetDesiredUpdateRate(double rate) { }
        public void SetStillUpdateRate(double rate) { }
        public void UpdateSize(int x, int y) { _renderWindow?.SetSize(x, y); }
    }

    public partial class vtkGenericRenderWindowInteractor
    {
        public void SetTimerEventResetsTimer(int v) { }
        public void TimerEvent() { }
    }

    public partial class vtkInteractorObserver
    {
        protected vtkRenderWindowInteractor _interactor;
        protected vtkRenderer _currentRenderer;
        protected vtkRenderer _defaultRenderer;
        protected bool _enabled;

        public virtual void SetInteractor(vtkRenderWindowInteractor interactor) { _interactor = interactor; }
        public vtkRenderWindowInteractor GetInteractor() { return _interactor; }
        public vtkRenderer GetCurrentRenderer() { return _currentRenderer; }
        public void SetCurrentRenderer(vtkRenderer renderer) { _currentRenderer = renderer; }
        public vtkRenderer GetDefaultRenderer() { return _defaultRenderer; }
        public void SetDefaultRenderer(vtkRenderer renderer) { _defaultRenderer = renderer; }
        public virtual void SetEnabled(int v) { _enabled = v != 0; }
        public int GetEnabled() { return _enabled ? 1 : 0; }
        public void EnabledOn() { SetEnabled(1); }
        public void EnabledOff() { SetEnabled(0); }
        public void On() { SetEnabled(1); }
        public void Off() { SetEnabled(0); }
        public void SetPriority(float p) { }
        public void GrabFocus(vtkObject mouseEvents, vtkObject keypressEvents) { }
        public void ReleaseFocus() { }
        public void FindPokedRenderer(int x, int y)
        {
            if (_defaultRenderer != null) _currentRenderer = _defaultRenderer;
            else _currentRenderer = _interactor?.FindPokedRenderer(x, y);
        }
        // Display and world coordinates of a renderer
        public static void ComputeWorldToDisplay(vtkRenderer ren, double x, double y, double z, IntPtr displayPt)
        {
            double[] d = ren.WorldToDisplayPoint(new[] { x, y, z });
            Marshal.Copy(d, 0, displayPt, 3);
        }
        public static void ComputeWorldToDisplay(vtkRenderer ren, double x, double y, double z, double[] displayPt)
        {
            double[] d = ren.WorldToDisplayPoint(new[] { x, y, z });
            Array.Copy(d, displayPt, 3);
        }
        public static void ComputeDisplayToWorld(vtkRenderer ren, double x, double y, double z, IntPtr worldPt)
        {
            double[] w = ren.DisplayToWorldPoint(new[] { x, y, z });
            Marshal.Copy(new[] { w[0], w[1], w[2], 1.0 }, 0, worldPt, 4);
        }
        public static void ComputeDisplayToWorld(vtkRenderer ren, double x, double y, double z, double[] worldPt)
        {
            double[] w = ren.DisplayToWorldPoint(new[] { x, y, z });
            worldPt[0] = w[0]; worldPt[1] = w[1]; worldPt[2] = w[2];
            if (worldPt.Length > 3) worldPt[3] = 1;
        }
    }

    // The interactor style: the interactor events go to the observers of the style when the style has any, and to the
    // default handlers otherwise, as in VTK
    public partial class vtkInteractorStyle
    {
        protected int _state;
        protected int _animState;
        protected bool _autoAdjustCameraClippingRange = true;
        protected double _mouseWheelMotionFactor = 1;
        protected bool _handleObservers = true;
        protected double _motionFactor = 10;

        public int GetState() { return _state; }
        public void StartState(int state) { _state = state; }
        public void StopState() { _state = 0; }
        public void StartRotate() { if (_state == 0) StartState(1); }
        public void EndRotate() { if (_state == 1) StopState(); }
        public void StartPan() { if (_state == 0) StartState(2); }
        public void EndPan() { if (_state == 2) StopState(); }
        public void StartSpin() { if (_state == 0) StartState(3); }
        public void EndSpin() { if (_state == 3) StopState(); }
        public void StartDolly() { if (_state == 0) StartState(4); }
        public void EndDolly() { if (_state == 4) StopState(); }
        public void StartZoom() { if (_state == 0) StartState(5); }
        public void EndZoom() { if (_state == 5) StopState(); }
        public void StartAnimate() { _animState = 1; }
        public void StopAnimate() { _animState = 0; }
        public void SetAutoAdjustCameraClippingRange(int v) { _autoAdjustCameraClippingRange = v != 0; }
        public int GetAutoAdjustCameraClippingRange() { return _autoAdjustCameraClippingRange ? 1 : 0; }
        public void AutoAdjustCameraClippingRangeOn() { _autoAdjustCameraClippingRange = true; }
        public void AutoAdjustCameraClippingRangeOff() { _autoAdjustCameraClippingRange = false; }
        public void SetMouseWheelMotionFactor(double f) { _mouseWheelMotionFactor = f; }
        public double GetMouseWheelMotionFactor() { return _mouseWheelMotionFactor; }
        public void SetHandleObservers(int v) { _handleObservers = v != 0; }
        public void HandleObserversOn() { _handleObservers = true; }
        public void HandleObserversOff() { _handleObservers = false; }
        public void SetUseTimers(int v) { }
        public void HighlightProp(vtkProp prop) { }
        public void HighlightActor2D(vtkActor2D actor) { }
        public void HighlightProp3D(vtkProp3D prop) { }
        public void SetPickColor(double r, double g, double b) { }

        internal void ProcessInteractorEvent(uint eventId)
        {
            if (_handleObservers && HasObserver(eventId) != 0)
            {
                InvokeEvent(eventId, IntPtr.Zero);
                return;
            }
            switch (eventId)
            {
                case vtkCommandIds.LeftButtonPressEvent: OnLeftButtonDown(); break;
                case vtkCommandIds.LeftButtonReleaseEvent: OnLeftButtonUp(); break;
                case vtkCommandIds.MiddleButtonPressEvent: OnMiddleButtonDown(); break;
                case vtkCommandIds.MiddleButtonReleaseEvent: OnMiddleButtonUp(); break;
                case vtkCommandIds.RightButtonPressEvent: OnRightButtonDown(); break;
                case vtkCommandIds.RightButtonReleaseEvent: OnRightButtonUp(); break;
                case vtkCommandIds.MouseMoveEvent: OnMouseMove(); break;
                case vtkCommandIds.MouseWheelForwardEvent: OnMouseWheelForward(); break;
                case vtkCommandIds.MouseWheelBackwardEvent: OnMouseWheelBackward(); break;
                case vtkCommandIds.KeyPressEvent: OnKeyPress(); break;
                case vtkCommandIds.KeyReleaseEvent: OnKeyRelease(); break;
                case vtkCommandIds.CharEvent: OnChar(); break;
                case vtkCommandIds.EnterEvent: OnEnter(); break;
                case vtkCommandIds.LeaveEvent: OnLeave(); break;
                case vtkCommandIds.ConfigureEvent: OnConfigure(); break;
            }
        }
        public virtual void OnLeftButtonDown() { }
        public virtual void OnLeftButtonUp() { }
        public virtual void OnMiddleButtonDown() { }
        public virtual void OnMiddleButtonUp() { }
        public virtual void OnRightButtonDown() { }
        public virtual void OnRightButtonUp() { }
        public virtual void OnMouseMove() { }
        public virtual void OnMouseWheelForward() { }
        public virtual void OnMouseWheelBackward() { }
        public virtual void OnKeyPress() { }
        public virtual void OnKeyRelease() { }
        public virtual void OnChar() { }
        public virtual void OnEnter() { }
        public virtual void OnLeave() { }
        public virtual void OnConfigure() { }
        public virtual void Rotate() { }
        public virtual void Spin() { }
        public virtual void Pan() { }
        public virtual void Dolly() { }
        public virtual void Zoom() { }
        public virtual void UniformScale() { }
        public void OnTimer() { }
    }

    public partial class vtkInteractorStyleTrackballCamera
    {
        public override void OnLeftButtonDown()
        {
            int[] p = _interactor.GetEventPosition();
            FindPokedRenderer(p[0], p[1]);
            if (_currentRenderer == null) return;
            if (_interactor.GetShiftKey() != 0)
            {
                if (_interactor.GetControlKey() != 0) StartDolly();
                else StartPan();
            }
            else
            {
                if (_interactor.GetControlKey() != 0) StartSpin();
                else StartRotate();
            }
        }
        public override void OnLeftButtonUp()
        {
            switch (_state)
            {
                case 4: EndDolly(); break;
                case 2: EndPan(); break;
                case 3: EndSpin(); break;
                case 1: EndRotate(); break;
            }
        }
        public override void OnMiddleButtonDown()
        {
            int[] p = _interactor.GetEventPosition();
            FindPokedRenderer(p[0], p[1]);
            if (_currentRenderer == null) return;
            StartPan();
        }
        public override void OnMiddleButtonUp() { if (_state == 2) EndPan(); }
        public override void OnRightButtonDown()
        {
            int[] p = _interactor.GetEventPosition();
            FindPokedRenderer(p[0], p[1]);
            if (_currentRenderer == null) return;
            StartDolly();
        }
        public override void OnRightButtonUp() { if (_state == 4) EndDolly(); }
        public override void OnMouseMove()
        {
            int[] p = _interactor.GetEventPosition();
            switch (_state)
            {
                case 1: FindPokedRenderer(p[0], p[1]); Rotate(); break;
                case 2: FindPokedRenderer(p[0], p[1]); Pan(); break;
                case 4: FindPokedRenderer(p[0], p[1]); Dolly(); break;
                case 3: FindPokedRenderer(p[0], p[1]); Spin(); break;
            }
        }
        public override void OnMouseWheelForward()
        {
            int[] p = _interactor.GetEventPosition();
            FindPokedRenderer(p[0], p[1]);
            if (_currentRenderer == null) return;
            StartDolly();
            Dolly(Math.Pow(1.1, _motionFactor * 0.2 * _mouseWheelMotionFactor));
            EndDolly();
        }
        public override void OnMouseWheelBackward()
        {
            int[] p = _interactor.GetEventPosition();
            FindPokedRenderer(p[0], p[1]);
            if (_currentRenderer == null) return;
            StartDolly();
            Dolly(Math.Pow(1.1, -_motionFactor * 0.2 * _mouseWheelMotionFactor));
            EndDolly();
        }
        public override void Rotate()
        {
            if (_currentRenderer == null) return;
            int[] p = _interactor.GetEventPosition(), last = _interactor.GetLastEventPosition();
            int[] size = _currentRenderer.GetRenderWindow().GetSize();
            double deltaElevation = -20.0 / size[1], deltaAzimuth = -20.0 / size[0];
            double rxf = (p[0] - last[0]) * deltaAzimuth * _motionFactor;
            double ryf = (p[1] - last[1]) * deltaElevation * _motionFactor;
            vtkCamera camera = _currentRenderer.GetActiveCamera();
            camera.Azimuth(rxf);
            camera.Elevation(ryf);
            camera.OrthogonalizeViewUp();
            if (_autoAdjustCameraClippingRange) _currentRenderer.ResetCameraClippingRange();
            _interactor.Render();
        }
        public override void Spin()
        {
            if (_currentRenderer == null) return;
            int[] p = _interactor.GetEventPosition(), last = _interactor.GetLastEventPosition();
            double[] center = _currentRenderer.WorldToDisplayPoint(_currentRenderer.GetActiveCamera().GetFocalPoint());
            double newAngle = Math.Atan2(p[1] - center[1], p[0] - center[0]) * 180 / Math.PI;
            double oldAngle = Math.Atan2(last[1] - center[1], last[0] - center[0]) * 180 / Math.PI;
            vtkCamera camera = _currentRenderer.GetActiveCamera();
            camera.Roll(newAngle - oldAngle);
            camera.OrthogonalizeViewUp();
            _interactor.Render();
        }
        public override void Pan()
        {
            if (_currentRenderer == null) return;
            int[] p = _interactor.GetEventPosition(), last = _interactor.GetLastEventPosition();
            vtkCamera camera = _currentRenderer.GetActiveCamera();
            double[] fp = camera.GetFocalPoint();
            double[] fpd = _currentRenderer.WorldToDisplayPoint(fp);
            double[] newPick = _currentRenderer.DisplayToWorldPoint(new double[] { p[0], p[1], fpd[2] });
            double[] oldPick = _currentRenderer.DisplayToWorldPoint(new double[] { last[0], last[1], fpd[2] });
            double[] motion = { oldPick[0] - newPick[0], oldPick[1] - newPick[1], oldPick[2] - newPick[2] };
            double[] pos = camera.GetPosition();
            camera.SetFocalPoint(fp[0] + motion[0], fp[1] + motion[1], fp[2] + motion[2]);
            camera.SetPosition(pos[0] + motion[0], pos[1] + motion[1], pos[2] + motion[2]);
            _interactor.Render();
        }
        public override void Dolly()
        {
            if (_currentRenderer == null) return;
            int[] p = _interactor.GetEventPosition(), last = _interactor.GetLastEventPosition();
            double[] center = { _currentRenderer.GetSize()[0] / 2.0, _currentRenderer.GetSize()[1] / 2.0 };
            double dy = p[1] - last[1];
            double factor = _motionFactor * dy / center[1];
            Dolly(Math.Pow(1.1, factor));
        }
        public virtual void Dolly(double factor)
        {
            if (_currentRenderer == null) return;
            vtkCamera camera = _currentRenderer.GetActiveCamera();
            if (camera.GetParallelProjection() != 0) camera.SetParallelScale(camera.GetParallelScale() / factor);
            else
            {
                camera.Dolly(factor);
                if (_autoAdjustCameraClippingRange) _currentRenderer.ResetCameraClippingRange();
            }
            _interactor.Render();
        }
    }

    public partial class vtkInteractorStyleSwitch
    {
        public void SetCurrentStyleToTrackballCamera() { }
        public void SetCurrentStyleToJoystickCamera() { }
    }

    // Pickers
    public partial class vtkAbstractPicker
    {
        protected vtkRenderer _renderer;
        protected double[] _selectionPoint = new double[3];
        protected double[] _pickPosition = new double[3];
        protected bool _pickFromList;
        protected readonly List<vtkProp> _pickList = new List<vtkProp>();

        public vtkRenderer GetRenderer() { return _renderer; }
        public double[] GetSelectionPoint() { return (double[])_selectionPoint.Clone(); }
        public double[] GetPickPosition() { return (double[])_pickPosition.Clone(); }
        public void SetPickFromList(int v) { _pickFromList = v != 0; }
        public int GetPickFromList() { return _pickFromList ? 1 : 0; }
        public void PickFromListOn() { _pickFromList = true; }
        public void PickFromListOff() { _pickFromList = false; }
        public void InitializePickList() { _pickList.Clear(); }
        public void AddPickList(vtkProp prop) { if (prop != null && !_pickList.Contains(prop)) _pickList.Add(prop); }
        public void DeletePickList(vtkProp prop) { _pickList.Remove(prop); }
        public vtkPropCollection GetPickList()
        {
            vtkPropCollection c = new vtkPropCollection();
            foreach (vtkProp p in _pickList) c.AddItem(p);
            return c;
        }
        public virtual int Pick(double x, double y, double z, vtkRenderer renderer) { return 0; }
        public int Pick(double[] p, vtkRenderer renderer) { return Pick(p[0], p[1], p[2], renderer); }

        // The pickable visible actors of the renderer, or of the pick list
        protected IEnumerable<vtkActor> Candidates(vtkRenderer renderer)
        {
            IEnumerable<vtkProp> props = _pickFromList ? _pickList : (IEnumerable<vtkProp>)renderer.Props;
            foreach (vtkProp prop in props)
            {
                if (!(prop is vtkActor actor)) continue;
                if (!actor.Visible || actor.GetPickable() == 0) continue;
                if (_pickFromList && !renderer.Props.Contains(prop)) continue;
                if (actor.GetMapper() == null) continue;
                yield return actor;
            }
        }
        // The ray through the display point from the near to the far plane
        internal static void DisplayRay(vtkRenderer renderer, double x, double y, out double[] p0, out double[] p1)
        {
            p0 = renderer.DisplayToWorldPoint(new[] { x, y, 0.0 });
            p1 = renderer.DisplayToWorldPoint(new[] { x, y, 1.0 });
        }
        // The world size of a display pixel at the focal plane
        internal static double PixelSize(vtkRenderer renderer)
        {
            double[] fp = renderer.GetActiveCamera().GetFocalPoint();
            double[] d = renderer.WorldToDisplayPoint(fp);
            double[] a = renderer.DisplayToWorldPoint(new[] { d[0], d[1], d[2] });
            double[] b = renderer.DisplayToWorldPoint(new[] { d[0] + 1, d[1], d[2] });
            return Math.Sqrt(CellGeometry.Dist2(a, b));
        }
        // The first hit of the ray on the actor: the ray parameter, the cell and the point; respects the clipping planes
        // and the culling of the back faces
        internal static double RayCast(vtkActor actor, double[] p0, double[] p1, double tolerance, out long cellId,
                                       out double[] point, vtkCamera camera)
        {
            cellId = -1;
            point = null;
            vtkMapper mapper = actor.GetMapper();
            vtkPolyData pd = mapper.GetRenderPolyData();
            if (pd == null || pd.GetNumberOfCells() == 0) return -1;
            // The ray in the coordinates of the data
            double[] q0 = p0, q1 = p1;
            if (!actor.IsIdentity)
            {
                double[] inv = vtkMatrix4x4.Inverse(actor is vtkFollower f ? f.FollowerMatrix(camera) : actor.ModelMatrix);
                q0 = vtkMatrix4x4.TransformPoint(inv, p0[0], p0[1], p0[2]);
                q1 = vtkMatrix4x4.TransformPoint(inv, p1[0], p1[1], p1[2]);
            }
            CellQueryData query = PickCache.Get(pd);
            double[] d = CellGeometry.Sub(q1, q0);
            bool cull = actor.GetProperty()._backfaceCulling;
            List<vtkPlane> planes = mapper.ClippingPlanes;
            double best = double.MaxValue;
            foreach (int c in query.Tree.AlongRay(q0, d, 1, tolerance))
            {
                int type = query.CellTypes[c];
                int dim = CellType.Dimension(type);
                // Lines and vertices are hit within the tolerance, surfaces exactly
                double s = query.IntersectRay(c, q0, d, dim < 2 ? Math.Max(tolerance, 1e-12) : 0);
                if (s < 0 || s > 1 || s >= best) continue;
                double[] hit = { q0[0] + s * d[0], q0[1] + s * d[1], q0[2] + s * d[2] };
                if (cull && dim == 2)
                {
                    long[] ids = query.CellPoints[c];
                    double[] n = new double[3];
                    vtkTriangle.ComputeNormal(query.Point(ids[0]), query.Point(ids[1]), query.Point(ids[2]), n);
                    if (CellGeometry.Dot(n, d) > 0) continue;
                }
                if (planes.Count > 0)
                {
                    double[] world = actor.IsIdentity ? hit : vtkMatrix4x4.TransformPoint(actor.ModelMatrix, hit[0], hit[1], hit[2]);
                    bool clipped = false;
                    foreach (vtkPlane plane in planes) if (plane.EvaluateFunction(world) < 0) { clipped = true; break; }
                    if (clipped) continue;
                }
                best = s;
                cellId = c;
                point = hit;
            }
            if (cellId < 0) return -1;
            if (!actor.IsIdentity) point = vtkMatrix4x4.TransformPoint(actor.ModelMatrix, point[0], point[1], point[2]);
            return best;
        }
    }

    // The ray cast geometry of the picked poly data is kept while the data does not change
    internal static class PickCache
    {
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<vtkPolyData, CellQueryData> _cache =
            new System.Runtime.CompilerServices.ConditionalWeakTable<vtkPolyData, CellQueryData>();

        public static CellQueryData Get(vtkDataSet data)
        {
            vtkPolyData pd = data as vtkPolyData;
            if (pd == null) return new CellQueryData(data);
            if (_cache.TryGetValue(pd, out CellQueryData query) && query.Time == pd.DataTime) return query;
            query = new CellQueryData(pd);
            _cache.AddOrUpdate(pd, query);
            return query;
        }
    }

    public partial class vtkAbstractPropPicker
    {
        protected vtkProp _prop;
        public vtkActor GetActor() { return _prop as vtkActor; }
        public vtkProp GetViewProp() { return _prop; }
        public vtkProp GetProp() { return _prop; }
        public vtkActor2D GetActor2D() { return _prop as vtkActor2D; }
        public vtkProp3D GetProp3D() { return _prop as vtkProp3D; }
    }

    // Picks the front most actor at the display point, like the hardware picking of VTK
    public partial class vtkPropPicker
    {
        public override int Pick(double x, double y, double z, vtkRenderer renderer)
        {
            _renderer = renderer;
            _prop = null;
            _selectionPoint = new[] { x, y, z };
            DisplayRay(renderer, x, y, out double[] p0, out double[] p1);
            double tolerance = 1.5 * PixelSize(renderer);
            double best = double.MaxValue;
            double[] bestPoint = null;
            vtkCamera camera = renderer.GetActiveCamera();
            foreach (vtkActor actor in Candidates(renderer))
            {
                double s = RayCast(actor, p0, p1, tolerance, out _, out double[] point, camera);
                if (s >= 0 && s < best)
                {
                    best = s;
                    _prop = actor;
                    bestPoint = point;
                }
            }
            if (_prop == null)
            {
                renderer.SetPickedZ(1);
                return 0;
            }
            _pickPosition = bestPoint;
            renderer.SetPickedZ(renderer.WorldToDisplayPoint(bestPoint)[2]);
            return 1;
        }
        public int PickProp(double x, double y, vtkRenderer renderer) { return Pick(x, y, 0, renderer); }
    }

    public partial class vtkPicker
    {
        protected double _tolerance = 0.025;
        protected long _cellId = -1;
        protected long _pointId = -1;
        protected vtkDataSet _dataSet;
        protected readonly List<vtkAbstractCellLocator> _locators = new List<vtkAbstractCellLocator>();
        protected readonly vtkProp3DCollection _prop3Ds = new vtkProp3DCollection();
        protected readonly vtkPoints _pickedPositions = new vtkPoints();

        public void SetTolerance(double t) { _tolerance = t; }
        public double GetTolerance() { return _tolerance; }
        public long GetCellId() { return _cellId; }
        public long GetPointId() { return _pointId; }
        public vtkDataSet GetDataSet() { return _dataSet; }
        public vtkMapper GetMapper() { return (_prop as vtkActor)?.GetMapper(); }
        public vtkProp3DCollection GetProp3Ds() { return _prop3Ds; }
        public vtkActorCollection GetActors()
        {
            vtkActorCollection c = new vtkActorCollection();
            foreach (vtkProp p in _prop3Ds.Items) if (p is vtkActor a) c.AddItem(a);
            return c;
        }
        public vtkPoints GetPickedPositions() { return _pickedPositions; }
        public void AddLocator(vtkAbstractCellLocator locator) { if (!_locators.Contains(locator)) _locators.Add(locator); }
        public void RemoveLocator(vtkAbstractCellLocator locator) { _locators.Remove(locator); }
        public void RemoveAllLocators() { _locators.Clear(); }
        public override int Pick(double x, double y, double z, vtkRenderer renderer)
        {
            _renderer = renderer;
            _prop = null;
            _cellId = -1;
            _pointId = -1;
            _dataSet = null;
            _prop3Ds.RemoveAllItems();
            _pickedPositions.Reset();
            _selectionPoint = new[] { x, y, z };
            DisplayRay(renderer, x, y, out double[] p0, out double[] p1);
            // The tolerance is a fraction of the window diagonal
            int[] size = renderer.GetRenderWindow()?.GetSize() ?? renderer.GetSize();
            double diagonal = Math.Sqrt((double)size[0] * size[0] + (double)size[1] * size[1]);
            double tolerance = Math.Max(_tolerance * diagonal, 0.5) * PixelSize(renderer);
            double best = double.MaxValue;
            double[] bestPoint = null;
            vtkCamera camera = renderer.GetActiveCamera();
            foreach (vtkActor actor in Candidates(renderer))
            {
                double s = RayCast(actor, p0, p1, 0, out long cellId, out double[] point, camera);
                if (s < 0)
                {
                    // Within the tolerance of the edges of the surfaces
                    s = RayCast(actor, p0, p1, tolerance, out cellId, out point, camera);
                    if (s >= 0)
                    {
                        // A tolerance hit is behind an exact hit of another actor at the same depth
                        s += 1e-9;
                    }
                }
                if (s < 0) continue;
                _prop3Ds.AddItem(actor);
                _pickedPositions.InsertNextPoint(point);
                if (s < best)
                {
                    best = s;
                    _prop = actor;
                    _cellId = cellId;
                    bestPoint = point;
                    _dataSet = actor.GetMapper().GetRenderPolyData();
                }
            }
            if (_prop == null) return 0;
            _pickPosition = bestPoint;
            renderer.SetPickedZ(renderer.WorldToDisplayPoint(bestPoint)[2]);
            return 1;
        }
    }

    public partial class vtkPointPicker
    {
        public override int Pick(double x, double y, double z, vtkRenderer renderer)
        {
            int result = base.Pick(x, y, z, renderer);
            if (result == 0 || _dataSet == null) return result;
            _pointId = _dataSet.FindPoint(_pickPosition);
            return result;
        }
    }

    public partial class vtkCellPicker
    {
        public double[] GetPickNormal()
        {
            if (_dataSet == null || _cellId < 0) return new double[] { 0, 0, 1 };
            long[] ids = _dataSet.GetCellPointIds(_cellId);
            if (ids.Length < 3) return new double[] { 0, 0, 1 };
            double[] n = new double[3];
            vtkTriangle.ComputeNormal(_dataSet.GetPoint(ids[0]), _dataSet.GetPoint(ids[1]), _dataSet.GetPoint(ids[2]), n);
            return n;
        }
        public double[] GetPCoords() { return new double[3]; }
        public int GetSubId() { return 0; }
    }

    // Picks the props in a rectangle of the display
    public partial class vtkAreaPicker
    {
        protected vtkPlanes _frustum = new vtkPlanes();
        protected readonly vtkProp3DCollection _prop3Ds = new vtkProp3DCollection();
        protected vtkPoints _clipPoints = new vtkPoints();

        public vtkPlanes GetFrustum() { return _frustum; }
        public vtkProp3DCollection GetProp3Ds() { return _prop3Ds; }
        public vtkPoints GetClipPoints() { return _clipPoints; }
        public int AreaPick(double x0, double y0, double x1, double y1, vtkRenderer renderer)
        {
            _renderer = renderer;
            _prop = null;
            _prop3Ds.RemoveAllItems();
            double xmin = Math.Min(x0, x1), xmax = Math.Max(x0, x1), ymin = Math.Min(y0, y1), ymax = Math.Max(y0, y1);
            // A rectangle smaller than a pixel picks the pixel
            if (xmax - xmin < 1) { xmin -= 0.5; xmax += 0.5; }
            if (ymax - ymin < 1) { ymin -= 0.5; ymax += 0.5; }
            DefineFrustum(renderer, xmin, ymin, xmax, ymax);
            foreach (vtkActor actor in Candidates(renderer))
            {
                if (IntersectsFrustum(actor, renderer, xmin, ymin, xmax, ymax))
                {
                    _prop3Ds.AddItem(actor);
                    if (_prop == null) _prop = actor;
                }
            }
            return _prop != null ? 1 : 0;
        }
        // The planes left, right, bottom, top, near and far with outward normals, through the corners of the rectangle
        private void DefineFrustum(vtkRenderer renderer, double xmin, double ymin, double xmax, double ymax)
        {
            double[][] verts = new double[8][];
            double[][] display = { new[] { xmin, ymin }, new[] { xmin, ymax }, new[] { xmax, ymin }, new[] { xmax, ymax } };
            for (int i = 0; i < 4; i++)
            {
                verts[2 * i] = renderer.DisplayToWorldPoint(new[] { display[i][0], display[i][1], 0.0 });
                verts[2 * i + 1] = renderer.DisplayToWorldPoint(new[] { display[i][0], display[i][1], 1.0 });
            }
            _clipPoints = new vtkPoints();
            _clipPoints.SetDataTypeToDouble();
            foreach (double[] v in verts) _clipPoints.InsertNextPoint(v);
            vtkPoints points = new vtkPoints();
            points.SetDataTypeToDouble();
            vtkDoubleArray normals = new vtkDoubleArray();
            normals.SetNumberOfComponents(3);
            // Each plane through three corners: 0 (xmin,ymin), 2 (xmin,ymax), 4 (xmax,ymin), 6 (xmax,ymax); odd ones far
            double[] center = new double[3];
            foreach (double[] v in verts) for (int c = 0; c < 3; c++) center[c] += v[c] / 8;
            int[][] planes = {
                new[] { 0, 1, 2 },      // left
                new[] { 4, 6, 5 },      // right
                new[] { 0, 4, 1 },      // bottom
                new[] { 2, 3, 6 },      // top
                new[] { 0, 2, 4 },      // near
                new[] { 1, 5, 3 } };    // far
            foreach (int[] pl in planes)
            {
                double[] a = verts[pl[0]], b = verts[pl[1]], c = verts[pl[2]];
                double[] n = vtkMath.Cross(CellGeometry.Sub(b, a), CellGeometry.Sub(c, a));
                vtkMath.Normalize(n);
                // Outward: away from the center of the frustum
                if (CellGeometry.Dot(n, CellGeometry.Sub(center, a)) > 0) { n[0] = -n[0]; n[1] = -n[1]; n[2] = -n[2]; }
                points.InsertNextPoint(a);
                normals.InsertNextTuple3(n[0], n[1], n[2]);
            }
            _frustum = new vtkPlanes();
            _frustum.SetPoints(points);
            _frustum.SetNormals(normals);
        }
        private bool IntersectsFrustum(vtkActor actor, vtkRenderer renderer, double xmin, double ymin, double xmax, double ymax)
        {
            double[] b = actor.GetBounds();
            if (b == null) return false;
            // Bounds outside one of the planes
            for (int i = 0; i < _frustum.GetNumberOfPlanes(); i++)
            {
                vtkPlane plane = _frustum.GetPlane(i);
                double[] n = plane.GetNormal(), o = plane.GetOrigin();
                double[] near = { n[0] >= 0 ? b[0] : b[1], n[1] >= 0 ? b[2] : b[3], n[2] >= 0 ? b[4] : b[5] };
                if (n[0] * (near[0] - o[0]) + n[1] * (near[1] - o[1]) + n[2] * (near[2] - o[2]) > 0) return false;
            }
            // A point of the actor inside the rectangle, or the rectangle over the surface of the actor
            vtkPolyData pd = actor.GetMapper().GetRenderPolyData();
            if (pd == null || pd.GetPoints() == null) return false;
            double[] m = actor is vtkFollower f ? f.FollowerMatrix(renderer.GetActiveCamera()) : actor.ModelMatrix;
            double[] raw = pd.GetPoints().Raw;
            long n2 = pd.GetNumberOfPoints();
            double[] composite = vtkMatrix4x4.Multiply(renderer.GetActiveCamera().CompositeMatrix(renderer.GetTiledAspectRatio(), 0, 1), m);
            int[] ws = renderer.GetWindowSize();
            double[] vp = renderer.GetViewport();
            for (long i = 0; i < n2; i++)
            {
                double x = raw[3 * i], y = raw[3 * i + 1], z = raw[3 * i + 2];
                double w = composite[12] * x + composite[13] * y + composite[14] * z + composite[15];
                if (w == 0) continue;
                double vx = (composite[0] * x + composite[1] * y + composite[2] * z + composite[3]) / w;
                double vy = (composite[4] * x + composite[5] * y + composite[6] * z + composite[7]) / w;
                double vz = (composite[8] * x + composite[9] * y + composite[10] * z + composite[11]) / w;
                if (vz < 0 || vz > 1) continue;
                double dx = (vx + 1) * ws[0] * (vp[2] - vp[0]) / 2 + ws[0] * vp[0];
                double dy = (vy + 1) * ws[1] * (vp[3] - vp[1]) / 2 + ws[1] * vp[1];
                if (dx >= xmin && dx <= xmax && dy >= ymin && dy <= ymax) return true;
            }
            // The rays through the corners and the center
            vtkCamera camera = renderer.GetActiveCamera();
            foreach (double[] c in new[] { new[] { (xmin + xmax) / 2, (ymin + ymax) / 2 }, new[] { xmin, ymin },
                                           new[] { xmin, ymax }, new[] { xmax, ymin }, new[] { xmax, ymax } })
            {
                DisplayRay(renderer, c[0], c[1], out double[] p0, out double[] p1);
                if (RayCast(actor, p0, p1, 0, out _, out _, camera) >= 0) return true;
            }
            return false;
        }
    }

    public partial class vtkRenderedAreaPicker
    {
    }

    // The hardware selector of VTK; the view selects with the area picker
    public partial class vtkHardwareSelector
    {
        public void SetFieldAssociation(int f) { }
        public void SetRenderer(vtkRenderer r) { }
        public void SetArea(uint x0, uint y0, uint x1, uint y1) { }
        public vtkSelection Select() { return new vtkSelection(); }
    }

    // Widgets
    public partial class vtkAbstractWidget
    {
        protected vtkWidgetRepresentation _representation;
        public void SetRepresentation(vtkWidgetRepresentation r) { _representation = r; }
        public vtkWidgetRepresentation GetRepresentation() { return _representation; }
        public void CreateDefaultRepresentation() { }
        public void ProcessEventsOn() { }
        public void ProcessEventsOff() { }
        public override void SetEnabled(int v)
        {
            base.SetEnabled(v);
            vtkRenderer renderer = _currentRenderer ?? _defaultRenderer ??
                                   _interactor?.GetRenderWindow()?.GetRenderers().GetFirstRenderer();
            if (renderer == null || _representation == null) return;
            if (v != 0) renderer.AddViewProp(_representation);
            else renderer.RemoveViewProp(_representation);
        }
    }

    public partial class vtkCaptionWidget
    {
        public void SetCaptionActor2D(vtkCaptionActor2D actor)
        {
            if (_representation is vtkCaptionRepresentation r) r.SetCaptionActor2D(actor);
        }
        public vtkCaptionActor2D GetCaptionActor2D() { return (_representation as vtkCaptionRepresentation)?.GetCaptionActor2D(); }
    }

    public partial class vtkWidgetRepresentation
    {
        public void SetRenderer(vtkRenderer r) { _renderer = r; }
        public void BuildRepresentation() { }
        public void SetPlaceFactor(double f) { }
        public void PlaceWidget(double[] bounds) { }
        public void SetHandleSize(double s) { }
        public void SetNeedToRender(int v) { }
    }

    public partial class vtkCaptionRepresentation
    {
        private vtkCaptionActor2D _captionActor = new vtkCaptionActor2D();
        public void SetCaptionActor2D(vtkCaptionActor2D actor) { _captionActor = actor; }
        public vtkCaptionActor2D GetCaptionActor2D() { return _captionActor; }
        public void SetAnchorPosition(double[] p) { _captionActor.SetAttachmentPoint(p); }
        public void SetAnchorPosition(IntPtr p)
        {
            double[] v = new double[3];
            Marshal.Copy(p, v, 0, 3);
            SetAnchorPosition(v);
        }
        public void GetVolumes(vtkPropCollection collection) { }
        public void GetAnchorPosition(double[] p) { Array.Copy(_captionActor.GetAttachmentPoint(), p, 3); }
        public void SetPosition(double x, double y) { _captionActor.SetPosition(x, y); }
        public void SetPosition2(double x, double y) { _captionActor.SetPosition2(x, y); }
        public void SetFontFactor(double f) { }
        public void SetShowBorder(int v) { }
        public void SetMoving(int v) { }
        public void SetMinimumSize(int w, int h) { }
        public void SetMaximumSize(int w, int h) { }
        public vtkPointHandleRepresentation3D GetAnchorRepresentation() { return new vtkPointHandleRepresentation3D(); }
    }

    public partial class vtkPointHandleRepresentation3D
    {
        public void AllOff() { }
        public void AllOn() { }
        public void SetWorldPosition(double[] p) { }
        public double[] GetWorldPosition() { return new double[3]; }
        public vtkProperty GetProperty() { return new vtkProperty(); }
        public void SetHandleSize(double s) { }
    }
}
