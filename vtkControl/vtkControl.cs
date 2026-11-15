// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using vtkControl.Native;
using Octree;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Management.Instrumentation;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace vtkControl
{
    // Delegates                                                                                                                    
    public delegate vtkMaxActorData GetSurfaceEdgesActorDataFromElementIdDelegate
        (int nodeIds, int[] elementIds, out string noEdgePartName);
    public delegate vtkMaxActorData GetSurfaceEdgesActorDataFromNodeAndElementIdsDelegate
        (int[] nodeIds, int[] elementIds, bool completelyInside, out string[] noEdgePartNames);
    public delegate vtkMaxActorData GetGeometryActorDataDelegate
        (double[] point, int elementId, int[] edgeNodeIds, int[] cellFaceNodeIds, out string noEdgePartName);

    /// <summary>
    /// The 3D view of PrePoMax: a UserControl whose client area is filled with the native VTK 9 view.
    /// </summary>
    [ComVisible(true), ClassInterface(ClassInterfaceType.AutoDual)]
    public partial class vtkControl : UserControl, IMessageFilter
    {
        int countError;
        // Variables                                                                                                                        
        public const int MaxImageMagnification = 4;
        private bool _renderingOn;
        private int _cameraAdjustmentSuspendCount;
        private bool _cameraAdjustmentPending;
        private bool _ambientOcclusion;
        private double _ambientOcclusionRelativeRadius = 0.05;
        private int _ambientOcclusionKernelSize = 64;
        private bool _userPick;     // used to determine if the selection distance should be limited
        private VtkNativeView _nativeMirror;
        private HashSet<string> _nativeMirrorNames;
        private VtkNativeScalarBar _nativeMirrorScalarBar;
        private VtkNativeArea _nativeAreas;
        private List<vtkMaxBorderWidget> _nativeMirrorScalarBarHiddenWidgets;
        // Symbols resolution
        private int _arrowTipResolution = 21;
        private int _arrowShaftResolution = 15;
        // Widgets
        private const int CoorSysWidgetMargin = 24;
        private vtkMaxCoorSysWidget _coorSysWidget;
        private vtkMaxScaleWidget _scaleWidget;
        private bool _scalarBarVisible;
        private string _scalarBarLabelFormat;
        private bool _scalarBarBackgroundVisible;
        private bool _scalarBarBorderVisible;
        private string[] _scalarBarText;
        private double[] _scalarBarRange;
        private vtkMaxColorBarWidget _colorBarWidget;
        private vtkMaxStatusBlockWidget _statusBlockWidget;
        private bool _widgetInteractionEnabled;
        private bool _widgetHighlightEnabled;
        private bool _widgetHighlightLocked;
        private bool _keepScalarBarVisible;
        private bool _drawCoorSys;
        private bool _drawStatusBlock;
        private vtkEdgesVisibility _edgesVisibility;
        //
        private vtkMaxColorSpectrum _colorSpectrum;
        //
        private Dictionary<string, vtkMaxTextWithArrowWidget> _arrowWidgets;
        private vtkMaxTextWidget _probeWidget;
        //
        private Dictionary<string, vtkMaxActor> _actors;
        private List<vtkMaxActor> _selectedActors;
        private Dictionary<string, vtkMaxActor> _overlayActors;
        private Dictionary<string, vtkMaxActor> _animationActors;
        private Dictionary<int, vtkMaxActor> _animationNodeActors;
        private Dictionary<string, vtkMaxAnimationArrowAnchor> _animationArrowAnchors;
        private Dictionary<string, Tuple<double[], string[]>> _animationArrowAnchorRequests;
        private Dictionary<string, int> _measurementArrowPointCounts;
        private Dictionary<string, double> _measurementArrowSymbolSizes;
        private vtkMaxAnimationFrameData _animationFrameData;
        private int _currentAnimationFrameNumber;
        private Color _primaryHighlightColor;
        private Color _secondaryHighlightColor;
        private double _maxSymbolSize;
        private bool _drawSymbolEdges;
        //
        private bool _animating;
        private bool _viewAnimationEnabled;
        //
        private List<Tuple<double[], int>> _transforms;
        // Section view
        private SectionViewData _sectionViewData;
        private object myLock = new object();
        // Selection
        private bool _querySelectionInProgress;
        private vtkSelectBy _selectBy;
        private vtkSelectItem _selectItem;
        private int _selectionDistance;
        private bool _selectionBoxEnabled;
        //
        private HashSet<string> _selectableActorsFilter;
        // Follower view
        private string[] _followerViewPartNames;
        private int[] _followerViewPointIds;
        private double[][] _followerViewPointCoor;
        // Camera path
        private bool _drawCameraPath;
        private List<double[]> _positions;
        private List<double[]> _focalPoints;
        // Trackpad
        private TrackpadNavigation _trackpadNavigation;
        private double _rotateSensitivity;
        private double _zoomSensitivity;
        private bool _invertZoom;
        private bool _smoothZoom;
        private double _trackpadDragX;
        private double _trackpadDragY;
        private double _trackpadZoom;
        private int _trackpadCenterX;
        private int _trackpadCenterY;
        private bool _trackpadShift;
        private bool _trackpadPending;
        private bool _trackpadGestureActive;
        private Timer _trackpadGestureEndTimer;
        private PrecisionTouchpad _precisionTouchpad;
        private TrackpadGestureRecognizer _gestureRecognizer;


        // Properties                                                                                                               
        public bool RenderingOn
        {
            get { return _renderingOn; }
            set
            {
                if (value != _renderingOn)
                {
                    _renderingOn = value;
                    if (_renderingOn) RenderScene();
                }
            }
        }
        public bool UserPick { get { return _userPick; } set { _userPick = value; } }
        public bool ViewAnimationEnabled { get { return _viewAnimationEnabled; } set { _viewAnimationEnabled = value; } }
        public vtkEdgesVisibility EdgesVisibility
        {
            get { return _edgesVisibility; }
            set
            {
                if (_edgesVisibility != value)
                {
                    _edgesVisibility = value;
                    RunNative("EdgesVisibility", () => _nativeMirror.SetEdgesVisibility(value));
                    RenderScene();
                }
            }
        }
        public bool QuerySelectionInProgress
        {
            get { return _querySelectionInProgress; }
            set
            {
                _querySelectionInProgress = value;
                _selectionBoxEnabled = !value;
                MirrorInteractionSettings();
            }
        }
        public vtkSelectBy SelectBy
        {
            get { return _selectBy; }
            set
            {
                if (_selectBy != value)
                {
                    _selectBy = value;
                    //
                    switch (_selectBy)
                    {
                        // General
                        case vtkSelectBy.Off:
                            _selectionBoxEnabled = false;
                            break;
                        case vtkSelectBy.Default:
                        // Mesh based
                        case vtkSelectBy.Node:
                        case vtkSelectBy.Element:
                        case vtkSelectBy.Part:
                        // Geometry
                        case vtkSelectBy.Geometry:
                        case vtkSelectBy.GeometryPart:
                            _selectionBoxEnabled = true;
                            if (_querySelectionInProgress) _selectionBoxEnabled = false;
                            break;
                        // Mesh based
                        case vtkSelectBy.Id:
                        case vtkSelectBy.Edge:
                        case vtkSelectBy.Surface:
                        case vtkSelectBy.EdgeAngle:
                        case vtkSelectBy.SurfaceAngle:
                        // Geometry
                        case vtkSelectBy.GeometryVertex:
                        case vtkSelectBy.GeometryEdge:
                        case vtkSelectBy.GeometrySurface:
                        case vtkSelectBy.GeometryEdgeAngle:
                        case vtkSelectBy.GeometrySurfaceAngle:
                            _selectionBoxEnabled = false;
                            break;
                        // Widget
                        case vtkSelectBy.Widget:
                            _selectionBoxEnabled = false;
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    MirrorInteractionSettings();
                    ClearCurrentMouseSelection();
                }
            }
        }
        public vtkSelectItem SelectItem { get { return _selectItem; } set { if (_selectItem != value) { _selectItem = value; } } }


        // Setters                                                                                                                  
        public void SetSelectBy(vtkSelectBy selectBy)
        {
            SelectBy = selectBy;
        }
        public void SetAnimationAcceleration(bool animationAcceleration)
        {
            // Retained for API compatibility. The persistent VTK animation pipeline is always enabled.
        }
        public void SetSelectableActorsFilter(string[] selectableActors)
        {
            if (selectableActors != null) _selectableActorsFilter = new HashSet<string>(selectableActors);
            else _selectableActorsFilter = null;
        }
        public void SetNavigationSensitivity(double rotateSensitivity, double zoomSensitivity, bool invertZoom, bool smoothZoom)
        {
            _rotateSensitivity = rotateSensitivity;
            _zoomSensitivity = zoomSensitivity;
            _invertZoom = invertZoom;
            _smoothZoom = smoothZoom;
            //
            ApplyNavigationSensitivity();
        }
        public void SetTrackpadNavigation(bool enabled, bool useWheelFallback, bool twoFingerDragPans,
                                          double panSensitivity, bool invertX, bool invertY)
        {
            _trackpadNavigation.Enabled = enabled;
            _trackpadNavigation.UseWheelFallback = useWheelFallback;
            _trackpadNavigation.TwoFingerDragPans = twoFingerDragPans;
            _trackpadNavigation.PanSensitivity = panSensitivity;
            _trackpadNavigation.InvertX = invertX;
            _trackpadNavigation.InvertY = invertY;
        }
        private void ApplyNavigationSensitivity()
        {
            MirrorInteractionSettings();
        }
        public void StartBoxZoom()
        {
            MirrorStartBoxZoom();
        }


        // Getters
        public TrackpadNavigation TrackpadNavigation { get { return _trackpadNavigation; } }
        public bool IsBoxSelectionActive
        {
            get { return _nativeMirror != null && _nativeMirror.GetInteractionState()[1] != 0; }
        }


        // Callbacks                                                                                                                
        public Func<string, string> Controller_GetAnnotationText;
        public Func<string, string> Controller_GetSectionViewHatchGroup;
        public Action<int, bool> Controller_AnimationFrameChanged;
        public Func<string, int, NodesExchangeData> Controller_GetAnimationLocatorNodes;
        public Func<string, int, PartExchangeData> Controller_GetVolumeGridData;
        public Func<string, int, int[], NodesExchangeData> Controller_GetAnimationNodes;
        public Func<int[], vtkMaxActorData> Controller_GetNodeActorData;
        public Func<int[], int[], vtkMaxActorData> Controller_GetCellActorData;
        public Func<int, int[], vtkMaxActorData> Controller_GetCellFaceActorData;
        public Func<int, int[], vtkMaxActorData> Controller_GetEdgeActorData;
        public GetSurfaceEdgesActorDataFromElementIdDelegate Controller_GetSurfaceEdgesActorDataFromElementId;
        public GetSurfaceEdgesActorDataFromNodeAndElementIdsDelegate Controller_GetSurfaceEdgesActorDataFromNodeAndElementIds;
        public Func<int[], vtkMaxActorData> Controller_GetPartActorData;
        public GetGeometryActorDataDelegate Controller_GetGeometryActorData;
        public Func<double[], int, int[], int[], vtkMaxActorData> Controller_GetGeometryVertexActorData;
        //
        public Action<MouseEventArgs, Keys, string[]> Controller_ActorsPicked;
        public Action<MouseEventArgs, Keys, string, Rectangle> Form_WidgetPicked;
        public Action<bool, Rectangle, string, double, double> Form_EditLegendValue;
        public Action<vtkResultWidgetContextMenuTypeEnum, bool, System.Drawing.Point> Form_ShowResultWidgetContextMenu;
        public Action<System.Drawing.Point> Form_ShowViewportContextMenu;
        public Action Form_ShowColorBarSettings;
        public Action Form_ShowLegendSettings;
        public Action Form_ShowStatusBlockSettings;
        public Func<bool> Form_CommitViewportEdit;
        //
        public Action Form_BoxZoomEnded;


        // Events                                                                                                                   
        public event Action<double[], double[], double[][], bool, vtkSelectOperation, string[]> OnMouseLeftButtonUpSelection;
        public event Action<double[]> RotationPointChanged;


        // Constructors                                                                                                             
        public vtkControl()
        {
            InitializeComponent();
            //
            Controller_GetNodeActorData = null;
            Controller_GetCellActorData = null;
            //
            _renderingOn = true;
            _keepScalarBarVisible = false;
            _drawCoorSys = true;
            _drawStatusBlock = true;
            _widgetInteractionEnabled = true;
            _widgetHighlightEnabled = true;
            _edgesVisibility = vtkEdgesVisibility.ElementEdges;
            _colorSpectrum = new vtkMaxColorSpectrum();
            _trackpadNavigation = new TrackpadNavigation();
            _rotateSensitivity = 1;
            _zoomSensitivity = 1;
            _invertZoom = false;
            _smoothZoom = true;
            _trackpadZoom = 1;
            _trackpadGestureEndTimer = new Timer();
            _trackpadGestureEndTimer.Interval = 250;
            _trackpadGestureEndTimer.Tick += trackpadGestureEndTimer_Tick;
            _precisionTouchpad = new PrecisionTouchpad();
            _gestureRecognizer = new TrackpadGestureRecognizer();
            _gestureRecognizer.GestureUpdated += gestureRecognizer_GestureUpdated;
            _gestureRecognizer.GestureEnded += gestureRecognizer_GestureEnded;
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                Application.AddMessageFilter(this);
                Application.Idle += Application_Idle;
                HandleCreated += vtkControl_HandleCreated;
                Disposed += vtkControl_Disposed;
            }
            //
            _actors = new Dictionary<string, vtkMaxActor>();
            _selectedActors = new List<vtkMaxActor>();
            _overlayActors = new Dictionary<string, vtkMaxActor>();
            _animationActors = new Dictionary<string, vtkMaxActor>();
            _animationNodeActors = new Dictionary<int, vtkMaxActor>();
            _animationArrowAnchors = new Dictionary<string, vtkMaxAnimationArrowAnchor>();
            _animationArrowAnchorRequests = new Dictionary<string, Tuple<double[], string[]>>();
            _measurementArrowPointCounts = new Dictionary<string, int>();
            _measurementArrowSymbolSizes = new Dictionary<string, double>();
            _animationFrameData = null;
            _currentAnimationFrameNumber = -1;
            _primaryHighlightColor = Color.Red;
            _secondaryHighlightColor = Color.Violet;
            _drawSymbolEdges = true;
            //
            _animating = false;
            _viewAnimationEnabled = true;
            //
            _transforms = new List<Tuple<double[], int>>();
            //
            _sectionViewData = new SectionViewData();
            //
            //SelectBy = vtkSelectBy.Default;
            _selectItem = vtkSelectItem.None;
            _selectionBoxEnabled = true;
            //
            _selectableActorsFilter = null;
            //
            _followerViewPartNames = null;
            _followerViewPointIds = null;
            _followerViewPointCoor = null;
            //
            _drawCameraPath = false;
        }


        // Event handlers
        private void vtkControl_Resize(object sender, EventArgs e)
        {
            if (_coorSysWidget != null) ResetCoorSysWidgetPosition();
        }
        private void vtkControl_Load(object sender, EventArgs e)
        {
            InitializeControl();
        }
        private void vtkControl_EnabledChanged(object sender, EventArgs e)
        {
        }
        //
        private void colorBarWidget_DoubleClicked(object sender, MouseEventArgs e)
        {
            if (!_widgetInteractionEnabled) return;
            //
            Form_ShowColorBarSettings?.Invoke();
        }
        private void scalarBarWidget_DoubleClicked(object sender, MouseEventArgs e)
        {
            if (!_widgetInteractionEnabled) return;
            //
            if (TryGetScalarBarValueEditData(e.X, e.Y, out bool editMinimum, out Rectangle rectangle,
                                             out string valueText, out double minimum, out double maximum))
            {
                Form_EditLegendValue?.Invoke(editMinimum, rectangle, valueText, minimum, maximum);
            }
            else Form_ShowLegendSettings?.Invoke();
        }
        private void statusBlockWidget_DoubleClicked(object sender, MouseEventArgs e)
        {
            if (!_widgetInteractionEnabled) return;
            //
            Form_ShowStatusBlockSettings?.Invoke();
        }
        private void arrowWidget_DoubleClicked(object sender, MouseEventArgs e)
        {
            if (!_widgetInteractionEnabled) return;
            //
            if (sender is vtkMaxTextWithArrowWidget aw)
            {
                Form_WidgetPicked(e, Keys.None, aw.GetName(), aw.GetRectangle());
            }
        }
        private void coorSysWidget_TargetClicked(vtkMaxCoorSysTarget target)
        {
            switch (target)
            {
                case vtkMaxCoorSysTarget.XPositive: SetAxisView(true, new double[] { 1, 0, 0 }, false); break;
                case vtkMaxCoorSysTarget.XNegative: SetAxisView(true, new double[] { -1, 0, 0 }, false); break;
                case vtkMaxCoorSysTarget.YPositive: SetAxisView(true, new double[] { 0, 1, 0 }, false); break;
                case vtkMaxCoorSysTarget.YNegative: SetAxisView(true, new double[] { 0, -1, 0 }, false); break;
                case vtkMaxCoorSysTarget.ZPositive: SetAxisView(true, new double[] { 0, 0, 1 }, false); break;
                case vtkMaxCoorSysTarget.ZNegative: SetAxisView(true, new double[] { 0, 0, -1 }, false); break;
                case vtkMaxCoorSysTarget.XPositiveVertical: SetVerticalAxisView(true, new double[] { 1, 0, 0 }, false); break;
                case vtkMaxCoorSysTarget.XNegativeVertical: SetVerticalAxisView(true, new double[] { -1, 0, 0 }, false); break;
                case vtkMaxCoorSysTarget.YPositiveVertical: SetVerticalAxisView(true, new double[] { 0, 1, 0 }, false); break;
                case vtkMaxCoorSysTarget.YNegativeVertical: SetVerticalAxisView(true, new double[] { 0, -1, 0 }, false); break;
                case vtkMaxCoorSysTarget.ZPositiveVertical: SetVerticalAxisView(true, new double[] { 0, 0, 1 }, false); break;
                case vtkMaxCoorSysTarget.ZNegativeVertical: SetVerticalAxisView(true, new double[] { 0, 0, -1 }, false); break;
                case vtkMaxCoorSysTarget.Origin: SetIsometricView(true, true, false); break;
                default: break;
            }
        }

        // Trackpad                                                                                                                 
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_MOUSEHWHEEL = 0x020E;
        private const int MK_SHIFT = 0x0004;
        private const int MK_CONTROL = 0x0008;
        private const int WM_INPUT = 0x00FF;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        //
        // VTK creates its own child window and its Win32 interactor consumes WM_MOUSEWHEEL inside it,
        // so the message never reaches this control and has to be taken from the message pump instead
        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != WM_MOUSEWHEEL && m.Msg != WM_MOUSEHWHEEL) return false;
            if (_trackpadNavigation == null || !_trackpadNavigation.Enabled) return false;
            // While the contacts drive the camera the synthesised scrolling, its inertia included, is dropped
            if (_gestureRecognizer.Active || _trackpadGestureEndTimer.Enabled) return true;
            // With a touchpad present the contacts are the only source of gestures, which leaves
            // the mouse wheel free to keep zooming the way it always has
            if (_precisionTouchpad.Found || !_trackpadNavigation.UseWheelFallback) return false;
            //
            return HandleTrackpadWheel(ref m);
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_INPUT) HandleRawInput(m.LParam);
            else if ((m.Msg == WM_KEYDOWN || m.Msg == WM_KEYUP) && ForwardKeyToNativeWindow(ref m)) return;
            //
            base.WndProc(ref m);
        }
        // The main form hands the arrow keys to this control while it holds the focus, so that they do not move the
        // focus; they turn the native view
        private bool ForwardKeyToNativeWindow(ref Message m)
        {
            if (_nativeHost == null) return false;
            IntPtr viewWindow = _nativeHost.ViewWindow;
            if (viewWindow == IntPtr.Zero) return false;
            m.Result = SendMessage(viewWindow, m.Msg, m.WParam, m.LParam);
            return true;
        }
        private void vtkControl_HandleCreated(object sender, EventArgs e)
        {
            _precisionTouchpad.Register(Handle);
        }
        private void HandleRawInput(IntPtr rawInputHandle)
        {
            if (_trackpadNavigation == null || !_trackpadNavigation.Enabled) return;
            if (!_precisionTouchpad.Found || _nativeMirror == null) return;
            // Raw input arrives even while another application is in front - no form of this
            // application is active in that case, whichever of them the user was last working in
            if (Form.ActiveForm == null)
            {
                _gestureRecognizer.End();
                return;
            }
            // A two finger gesture leaves the pointer where the last single finger movement put it,
            // so hit testing it once at the start keeps the gesture bound to the viewport throughout
            if (!_gestureRecognizer.Active && !ClientRectangle.Contains(PointToClient(Cursor.Position))) return;
            //
            _gestureRecognizer.Update(_precisionTouchpad.GetContacts(rawInputHandle));
        }
        private void gestureRecognizer_GestureUpdated(double dragXMm, double dragYMm, double spreadRatio)
        {
            // The touchpad y axis grows downwards while the display coordinates grow upwards, and fingers
            // moving apart have to make the model larger, which means a smaller parallel scale
            _trackpadDragX += TrackpadNavigation.MmToPixels(dragXMm);
            _trackpadDragY -= TrackpadNavigation.MmToPixels(dragYMm);
            _trackpadZoom /= spreadRatio;
            //
            System.Drawing.Point clientPos = PointToClient(Cursor.Position);
            _trackpadCenterX = clientPos.X;
            _trackpadCenterY = ClientRectangle.Height - 1 - clientPos.Y;
            _trackpadShift = (ModifierKeys & Keys.Shift) == Keys.Shift;
            _trackpadPending = true;
            //
            _trackpadGestureEndTimer.Stop();
            _trackpadGestureEndTimer.Start();
        }
        private void gestureRecognizer_GestureEnded()
        {
            FlushTrackpadGesture();
            EndTrackpadGesture();
        }
        private void vtkControl_Disposed(object sender, EventArgs e)
        {
            Application.RemoveMessageFilter(this);
            Application.Idle -= Application_Idle;
            _trackpadGestureEndTimer.Stop();
            _trackpadGestureEndTimer.Dispose();
        }
        // A diagonal gesture arrives as two interleaved message streams - vertical and horizontal -
        // so the deltas are accumulated and turned into a single camera update once the queue drains
        private void Application_Idle(object sender, EventArgs e)
        {
            FlushTrackpadGesture();
        }
        private void trackpadGestureEndTimer_Tick(object sender, EventArgs e)
        {
            _trackpadGestureEndTimer.Stop();
            FlushTrackpadGesture();
            EndTrackpadGesture();
        }
        private void EndTrackpadGesture()
        {
            if (!_trackpadGestureActive) return;
            //
            _trackpadGestureActive = false;
            MirrorEndTrackpadGesture();
        }
        private void FlushTrackpadGesture()
        {
            if (!_trackpadPending) return;
            //
            double dragX = _trackpadDragX;
            double dragY = _trackpadDragY;
            double zoom = _trackpadZoom;
            _trackpadDragX = 0;
            _trackpadDragY = 0;
            _trackpadZoom = 1;
            _trackpadPending = false;
            _trackpadGestureActive = true;
            //
            MirrorTrackpadGesture(dragX, dragY, zoom);
        }
        private bool HandleTrackpadWheel(ref Message m)
        {
            if (_trackpadNavigation == null || !_trackpadNavigation.Enabled) return false;
            if (_nativeMirror == null) return false;
            //
            int wParam = unchecked((int)m.WParam.ToInt64());
            double wheelDelta = (short)((wParam >> 16) & 0xFFFF);
            bool control = (wParam & MK_CONTROL) != 0;
            bool shift = (wParam & MK_SHIFT) != 0;
            // The wheel messages carry screen coordinates and are sent to the focused window,
            // so the viewport must be hit tested before the gesture is consumed
            long lParam = m.LParam.ToInt64();
            System.Drawing.Point screenPos = new System.Drawing.Point((short)(lParam & 0xFFFF),
                                                                      (short)((lParam >> 16) & 0xFFFF));
            System.Drawing.Point clientPos = PointToClient(screenPos);
            if (!ClientRectangle.Contains(clientPos)) return false;
            //
            int x = clientPos.X;
            int y = ClientRectangle.Height - 1 - clientPos.Y;
            //
            double dragX = 0;
            double dragY = 0;
            double zoomFactor = 1;
            //
            if (control)
            {
                if (m.Msg == WM_MOUSEWHEEL) zoomFactor = 1 / TrackpadNavigation.WheelDeltaToZoomFactor(wheelDelta);
            }
            else
            {
                if (m.Msg == WM_MOUSEWHEEL) dragY = -TrackpadNavigation.WheelDeltaToPixels(wheelDelta);
                else dragX = -TrackpadNavigation.WheelDeltaToPixels(wheelDelta);
            }
            //
            _trackpadDragX += dragX;
            _trackpadDragY += dragY;
            _trackpadZoom *= zoomFactor;
            _trackpadCenterX = x;
            _trackpadCenterY = y;
            _trackpadShift = shift;
            _trackpadPending = true;
            //
            _trackpadGestureEndTimer.Stop();
            _trackpadGestureEndTimer.Start();
            //
            return true;
        }


        // Mouse Events - Style
        // Picks as a left click at the given point does, with the keys that are held. A right click on a part
        // that a dialog has not picked yet picks it this way
        public void Pick(int x1, int y1, bool boxSelection, int x2, int y2)
        {
            PickWithNativeMirror(x1, y1, boxSelection, x2, y2);
        }
        private void style_RightButtonPressEvent(object sender, MouseEventArgs e)
        {
            // Off
            if (_selectBy == vtkSelectBy.Off) return;
            else if (_selectBy == vtkSelectBy.Widget) return;
            else if (!_widgetInteractionEnabled && (sender is vtkMaxBorderWidget || sender is VtkNativeScalarBar)) return;
            //
            if (sender != null && sender is vtkMaxTextWithArrowWidget widget)
            {
                Form_WidgetPicked(e, Keys.None, widget.GetName(), widget.GetRectangle());
            }
            else if (sender is VtkNativeScalarBar)
            {
                vtkResultWidgetContextMenuTypeEnum contextMenuType;
                bool minimum = false;
                //
                if (TryGetScalarBarValueEditData(e.X, e.Y, out minimum, out Rectangle valueRectangle,
                                                 out string valueText, out double min, out double max))
                {
                    contextMenuType = vtkResultWidgetContextMenuTypeEnum.LegendValue;
                }
                else contextMenuType = vtkResultWidgetContextMenuTypeEnum.Legend;
                //
                Form_ShowResultWidgetContextMenu?.Invoke(contextMenuType, minimum, e.Location);
            }
            else if (sender is vtkMaxStatusBlockWidget)
            {
                Form_ShowResultWidgetContextMenu?.Invoke(vtkResultWidgetContextMenuTypeEnum.StatusBlock,
                                                         false, e.Location);
            }
            else
            {
                string pickedActorName = NativeMirrorPickPoint(e.Location.X, e.Location.Y, out _, out _);
                if (pickedActorName != null)
                {
                    Controller_ActorsPicked?.Invoke(e, ModifierKeys, new string[] { pickedActorName });
                }
                else Form_ShowViewportContextMenu?.Invoke(e.Location);
            }
        }

        #region Selection  #########################################################################################################
        private void GetClosestEdgeCell(double[] pickedPoint, vtkMaxActorData cellFaceData, out int[] nodeIds,
                                        out double[][] nodeCoor, out int[] edgeCell, out int edgeCellType)
        {
            nodeIds = null;
            nodeCoor = null;
            edgeCell = null;
            edgeCellType = -1;
            if (cellFaceData == null) return;
            //
            double[][] allNodeCoor = cellFaceData.Geometry.Nodes.Coor;
            int[] allNodeIds = cellFaceData.Geometry.Nodes.Ids;
            //
            int[][] cellEdges = GetVisualizationCellEdges(cellFaceData.Geometry.Nodes.Ids.Length);
            //
            double[] d = new double[cellEdges.Length];
            for (int i = 0; i < cellEdges.Length; i++)
            {
                if (cellEdges[i].Length == 2)       // linear edge
                {
                    d[i] = PointToLineSegmentDistance(pickedPoint, allNodeCoor[cellEdges[i][0]], allNodeCoor[cellEdges[i][1]]);
                }
                else if (cellEdges[i].Length == 3)  // parabolic edge
                {
                    d[i] = Math.Min(PointToLineSegmentDistance(pickedPoint, allNodeCoor[cellEdges[i][0]], allNodeCoor[cellEdges[i][1]]),
                                    PointToLineSegmentDistance(pickedPoint, allNodeCoor[cellEdges[i][1]], allNodeCoor[cellEdges[i][2]]));
                }
            }
            //
            double min = d[0];
            int minId = 0;
            for (int i = 1; i < d.Length; i++)
            {
                if (d[i] < min)
                {
                    min = d[i];
                    minId = i;
                }
            }
            //
            int numOfNodes = cellEdges[minId].Length;
            nodeIds = new int[numOfNodes];
            nodeCoor = new double[numOfNodes][];
            for (int i = 0; i < nodeIds.Length; i++)
            {
                nodeIds[i] = allNodeIds[cellEdges[minId][i]];
                nodeCoor[i] = allNodeCoor[cellEdges[minId][i]];
            }
            if (numOfNodes == 2)
            {
                edgeCell = new int[] { 0, 1 };
                edgeCellType = (int)vtkCellType.VTK_LINE;
            }
            else if (numOfNodes == 3)
            {
                edgeCell = new int[] { 0, 2, 1 };
                edgeCellType = (int)vtkCellType.VTK_QUADRATIC_EDGE;
            }
            else throw new NotSupportedException();
            // Swap edge node ids and coor so that first node is the closest
            double[] firstCoor = allNodeCoor[cellEdges[minId].First()];
            double[] lastCoor = allNodeCoor[cellEdges[minId].Last()];
            //
            double d1 = Math.Pow(pickedPoint[0] - firstCoor[0], 2) +
                        Math.Pow(pickedPoint[1] - firstCoor[1], 2) +
                        Math.Pow(pickedPoint[2] - firstCoor[2], 2);
            //
            double d2 = Math.Pow(pickedPoint[0] - lastCoor[0], 2) +
                        Math.Pow(pickedPoint[1] - lastCoor[1], 2) +
                        Math.Pow(pickedPoint[2] - lastCoor[2], 2);
            //
            bool swap = d2 < d1;
            int tmpId;
            double[] tmpCoor;
            //
            if (swap)
            {
                tmpId = nodeIds[0];
                nodeIds[0] = nodeIds[numOfNodes - 1];
                nodeIds[numOfNodes - 1] = tmpId;
                //
                tmpCoor = nodeCoor[0];
                nodeCoor[0] = nodeCoor[numOfNodes - 1];
                nodeCoor[numOfNodes - 1] = tmpCoor;
            }
        }
        public int[][] GetVisualizationCellEdges(int numOfNodes)
        {
            // lookup - leave here for future searches

            int[] nodeIds = Enumerable.Range(0, numOfNodes).ToArray();
            if (nodeIds.Length == 3)
            {
                return new int[][] {    new int[] { nodeIds[0], nodeIds[1] },
                                        new int[] { nodeIds[1], nodeIds[2] },
                                        new int[] { nodeIds[2], nodeIds[0] } };
            }
            else if (nodeIds.Length == 4)
            {
                return new int[][] {    new int[] { nodeIds[0], nodeIds[1] },
                                        new int[] { nodeIds[1], nodeIds[2] },
                                        new int[] { nodeIds[2], nodeIds[3] },
                                        new int[] { nodeIds[3], nodeIds[0] } };
            }
            else if (nodeIds.Length == 6)
            {
                return new int[][] {    new int[] { nodeIds[0], nodeIds[3], nodeIds[1] },
                                        new int[] { nodeIds[1], nodeIds[4], nodeIds[2] },
                                        new int[] { nodeIds[2], nodeIds[5], nodeIds[0] } };
            }
            else if (nodeIds.Length == 8)
            {
                return new int[][] {    new int[] { nodeIds[0], nodeIds[4], nodeIds[1] },
                                        new int[] { nodeIds[1], nodeIds[5], nodeIds[2] },
                                        new int[] { nodeIds[2], nodeIds[6], nodeIds[3] },
                                        new int[] { nodeIds[3], nodeIds[7], nodeIds[0] } };
            }
            else throw new NotSupportedException();
        }
        private double PointToLineSegmentDistance(double[] p, double[] l1, double[] l2)
        {
            double[] n = new double[] { l2[0] - l1[0], l2[1] - l1[1], l2[2] - l1[2] };
            double d = Math.Sqrt(Math.Pow(n[0], 2) + Math.Pow(n[1], 2) + Math.Pow(n[2], 2));
            if (d <= 0) return 0;
            // normalize
            n[0] /= d;
            n[1] /= d;
            n[2] /= d;

            double[] a = new double[] { p[0] - l1[0], p[1] - l1[1], p[2] - l1[2] };
            double aProj = a[0] * n[0] + a[1] * n[1] + a[2] * n[2];

            if (aProj <= 0)
            {
                d = Math.Sqrt(Math.Pow(a[0], 2) + Math.Pow(a[1], 2) + Math.Pow(a[2], 2));
                return d;
            }
            else if (aProj >= d)
            {
                double[] b = new double[] { l2[0] - p[0], l2[1] - p[1], l2[2] - p[2] };
                d = Math.Sqrt(Math.Pow(b[0], 2) + Math.Pow(b[1], 2) + Math.Pow(b[2], 2));
                return d;
            }
            else
            {
                double[] c = new double[] { a[0] - aProj * n[0], a[1] - aProj * n[1], a[2] - aProj * n[2] };
                d = Math.Sqrt(Math.Pow(c[0], 2) + Math.Pow(c[1], 2) + Math.Pow(c[2], 2));
                return d;
            }
        }
        private static bool TryAddWholePart(vtkMaxActor actor, HashSet<int> globalPointIds, HashSet<int> globalCellIds)
        {
            // A part completely inside the frustum contributes all its nodes and elements without a grid
            if (actor.PartNodeIds == null || actor.PartElementIds == null) return false;
            globalPointIds.UnionWith(actor.PartNodeIds);
            globalCellIds.UnionWith(actor.PartElementIds);
            return true;
        }
        /// <summary>
        /// The distance from the point to the closest cell of each named actor and the normal of that cell, whether
        /// the actor is visible or not. The distance is -1 for a name no actor carries.
        /// </summary>
        public void GetClosestCellsOnActors(double[] point, string[] actorNames, out double[] distances,
                                            out double[][] normals)
        {
            _nativeMirror.GetClosestCellsOnParts(point, actorNames, out distances, out normals);
        }
        public void GetGeometryPickProperties(double[] point, out int elementId, out int[] edgeNodeIds, out int[] cellFaceNodeIds)
        {
            NativeGetGeometryPickProperties(point, out elementId, out edgeNodeIds, out cellFaceNodeIds);
        }
        public double GetSelectionPrecision()
        {
            return _nativeMirror.GetSelectionPrecision(_selectionDistance);
        }
        public void SetSelectionDistance(int distanceInPixels)
        {
            _selectionDistance = Math.Max(1, distanceInPixels);
        }

        // Item selection - Frustum                                                                                                 
        public void GetPointAndCellIdsInsideFrustum(double[][] planeParameters, out int[] pointIds, out int[] cellIds)
        {
            NativeGetPointAndCellIdsInsideFrustum(planeParameters, out pointIds, out cellIds);
        }
        #endregion #################################################################################################################

        // Private methods                                                                                                          
        private void InitializeControl()
        {
            // Scale bar
            _scaleWidget = new vtkMaxScaleWidget();
            _scaleWidget.SetWidth(400);
            _scaleWidget.SetHorizontallyRelativePosition(320, 0);
            _scaleWidget.SetBorderColor(0, 0, 0);
            _scaleWidget.SetTextStyle(VtkNativeTextStyle.CreateWidgetStyle());
            _scaleWidget.SetPadding(5);
            _scaleWidget.SetBackgroundColor(1, 1, 1);
            _scaleWidget.VisibilityOn();
            _scaleWidget.BackgroundVisibilityOff();
            _scaleWidget.BorderVisibilityOff();
            // Scalar bar
            InitializeScalarBar();
            // Color bar
            _colorBarWidget = new vtkMaxColorBarWidget();
            _colorBarWidget.SetTextStyle(VtkNativeTextStyle.CreateWidgetStyle());
            _colorBarWidget.SetPadding(15);
            _colorBarWidget.VisibilityOn();
            _colorBarWidget.BackgroundVisibilityOn();
            _colorBarWidget.BorderVisibilityOn();
            _colorBarWidget.SetBackgroundColor(1, 1, 1);
            // Status block
            _statusBlockWidget = new vtkMaxStatusBlockWidget();
            _statusBlockWidget.SetTextStyle(VtkNativeTextStyle.CreateWidgetStyle());
            _statusBlockWidget.SetPadding(5);
            _statusBlockWidget.SetBackgroundColor(1, 1, 1);
            _statusBlockWidget.BackgroundVisibilityOff();
            _statusBlockWidget.VisibilityOff();
            // Probe widget
            _probeWidget = new vtkMaxTextWidget();
            _probeWidget.SetBorderColor(0, 0, 0);
            _probeWidget.SetTextStyle(VtkNativeTextStyle.CreateWidgetStyle());
            _probeWidget.SetPadding(5);
            _probeWidget.SetBackgroundColor(1, 1, 1);
            _probeWidget.VisibilityOff();
            // Arrow widgets
            _arrowWidgets = new Dictionary<string, vtkMaxTextWithArrowWidget>();
            // Coordinate system
            _coorSysWidget = new vtkMaxCoorSysWidget();
            _coorSysWidget.SetTextStyle(VtkNativeTextStyle.CreateWidgetStyle());
            _coorSysWidget.BackgroundVisibilityOff();
            _coorSysWidget.BorderVisibilityOff();
            SetCoorSysVisibility(_drawCoorSys);
            // The native view
            SetNativeAreas(VtkNativeArea.All);

            // Add the actors to the scene
            //Hexahedron();
            //Actor2D();
            //Timer timer = new Timer();
            //timer.Tick += Timer_Tick;
            //timer.Interval = 10 * 1000;
            //timer.Enabled = true;
        }
        private void InitializeScalarBar()
        {
            // Scalar bar: the native view makes it from this state when it shows the widgets
            _scalarBarRange = new double[] { 0, 1, double.NaN, double.NaN };
            _scalarBarLabelFormat = "E3";
            _scalarBarVisible = true;
            _scalarBarBackgroundVisible = true;
            _scalarBarBorderVisible = true;
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            ((Timer)sender).Enabled = false;
        }

        // Formatting
        private void ApplySymbolFormattingToActor(vtkMaxActor actor)
        {
            actor.BackfaceCulling = true;
            actor.Ambient = 0.6;
            actor.Diffuse = 0.6;
        }

        // Public methods                                                                                                          
        #region Views  #############################################################################################################
        public void GetViewParameters(out double[] position, out double[] focalPoint, out double[] viewUp, out double parallelScale)
        {
            double[] values = _nativeMirror.GetCamera();
            position = new double[] { values[0], values[1], values[2] };
            focalPoint = new double[] { values[3], values[4], values[5] };
            viewUp = new double[] { values[6], values[7], values[8] };
            parallelScale = values[9];
        }
        public void SetViewParameters(bool animate, double[] position, double[] focalPoint, double[] viewUp, double parallelScale)
        {
            if (_animating) return;
            _animating = animate;
            //
            double[] values = new double[] { position[0], position[1], position[2], focalPoint[0], focalPoint[1],
                                             focalPoint[2], viewUp[0], viewUp[1], viewUp[2], parallelScale };
            RunNativeViewChange(animate, () => _nativeMirror.SetViewParameters(values));
        }
        public void SetFrontBackView(bool animate, bool front)
        {
            if (_animating) return;
            _animating = animate;
            //
            VtkNativeOrientation orientation = front ? VtkNativeOrientation.Front : VtkNativeOrientation.Back;
            RunNativeViewChange(animate, () => _nativeMirror.SetStandardView(orientation, true));
        }
        public void SetTopBottomView(bool animate, bool top)
        {
            if (_animating) return;
            _animating = animate;
            //
            VtkNativeOrientation orientation = top ? VtkNativeOrientation.Top : VtkNativeOrientation.Bottom;
            RunNativeViewChange(animate, () => _nativeMirror.SetStandardView(orientation, true));
        }
        public void SetLeftRightView(bool animate, bool left)
        {
            if (_animating) return;
            _animating = animate;
            //
            VtkNativeOrientation orientation = left ? VtkNativeOrientation.Left : VtkNativeOrientation.Right;
            RunNativeViewChange(animate, () => _nativeMirror.SetStandardView(orientation, true));
        }
        public void SetAxisView(bool animate, double[] axis, bool zoomToFit = true)
        {
            if (_animating) return;
            _animating = animate;
            //
            RunNativeViewChange(animate, () => _nativeMirror.SetAxisView(axis, zoomToFit));
        }
        public void SetNormalView(bool animate)
        {
            if (_animating) return;
            _animating = animate;
            //
            RunNativeViewChange(animate, () => _nativeMirror.SetStandardView(VtkNativeOrientation.Normal, true));
        }
        public void SetVerticalAxisView(bool animate, double[] axis, bool zoomToFit = true)
        {
            if (_animating) return;
            _animating = animate;
            //
            RunNativeViewChange(animate, () => _nativeMirror.SetVerticalAxisView(axis, zoomToFit));
        }
        public void SetVerticalView(bool animate, bool updateView)
        {
            if (updateView)
            {
                if (_animating) return;
                _animating = animate;
            }
            //
            RunNativeViewChange(animate && updateView,
                                () => _nativeMirror.SetStandardView(VtkNativeOrientation.Vertical, true));
        }
        public double[] GetClosestUpView()
        {
            double[] values = _nativeMirror.GetCamera();
            double[] up = new double[] { values[6], values[7], values[8] };
            double[] upAbs = new double[3];
            double[] closestUp = null;
            upAbs[0] = Math.Abs(up[0]);
            upAbs[1] = Math.Abs(up[1]);
            upAbs[2] = Math.Abs(up[2]);
            //
            if (Math.Max(Math.Max(upAbs[0], upAbs[1]), Math.Max(upAbs[1], upAbs[2])) == upAbs[0])
                closestUp = new double[] { Math.Sign(up[0]), 0, 0 };
            else if (Math.Max(Math.Max(upAbs[0], upAbs[1]), Math.Max(upAbs[1], upAbs[2])) == upAbs[1])
                closestUp = new double[] { 0, Math.Sign(up[1]), 0 };
            else if (Math.Max(Math.Max(upAbs[0], upAbs[1]), Math.Max(upAbs[1], upAbs[2])) == upAbs[2])
                closestUp = new double[] { 0, 0, Math.Sign(up[2]) };
            //
            return closestUp;
        }
        public void SetIsometricView(bool animate, bool positive, bool zoomToFit = true)
        {
            if (_animating) return;
            _animating = animate;
            //
            VtkNativeOrientation orientation = positive ? VtkNativeOrientation.Isometric :
                                                          VtkNativeOrientation.IsometricNegative;
            RunNativeViewChange(animate, () => _nativeMirror.SetStandardView(orientation, zoomToFit));
        }
        public void SetZoomFactor(double factor)
        {
            if (_nativeMirror == null) return;
            _nativeMirror.SetZoomFactor(factor);
        }
        public void AnimateZoomToFactor(double factor, int timeMs)
        {
            if (_animating) return;
            _animating = true;
            //
            RunNativeViewChange(true, () => SetZoomFactor(factor), timeMs);
        }
        public void SetZoomToFit(bool animate)
        {
            if (_animating) return;
            _animating = animate;
            //
            RunNativeViewChange(animate, () => _nativeMirror.ZoomToFit());
        }
        public void SetBoxZoom(int[] point1, int[] point2, bool animate)
        {
            RunNativeViewChange(animate, () => _nativeMirror.BoxZoom(point1[0], point1[1], point2[0], point2[1]));
        }
        public void BoxZoomEnded()
        {
            Form_BoxZoomEnded?.Invoke();
        }
        //
        public void SetRotationPoint(double[] rotationPoint)
        {
            if (_nativeMirror == null) return;
            _nativeMirror.SetRotationPoint(rotationPoint);
        }
        //
        public double[] GetViewPlaneNormal()
        {
            return GetNativeViewPlaneNormal();
        }
        public double GetAngle(double[] v1, double[] v2)
        {
            double angle = v1[0] * v2[0] + v1[1] * v2[1] + v1[2] * v2[2];
            double abs1 = Math.Sqrt(Math.Pow(v1[0], 2) + Math.Pow(v1[1], 2) + Math.Pow(v1[2], 2));
            double abs2 = Math.Sqrt(Math.Pow(v2[0], 2) + Math.Pow(v2[1], 2) + Math.Pow(v2[2], 2));
            if (abs1 == 0 || abs2 == 0) return 0;
            else return Math.Acos(Math.Min(1, Math.Abs(angle / (abs1 * abs2))));
        }
        public double[] Subtract(double[] v1, double[] v2)
        {
            double[] v = new double[3];
            v[0] = v1[0] - v2[0];
            v[1] = v1[1] - v2[1];
            v[2] = v1[2] - v2[2];
            return v;
        }
        //
        private void DrawCameraPath()
        {
            ClearOverlay();
            //
            vtkMaxActorData data = new vtkMaxActorData();
            data.Name = "positions" + Globals.NameSeparator + "nodes";
            data.NodeSize = 3;
            data.Color = Color.Red;
            data.Layer = vtkRendererLayer.Overlay;
            data.Geometry.Nodes.Coor = _positions.ToArray();
            AddPoints(data);

            data = new vtkMaxActorData();
            data.Name = "focalPoints" + Globals.NameSeparator + "nodes";
            data.NodeSize = 3;
            data.Color = Color.Black;
            data.Layer = vtkRendererLayer.Overlay;
            data.Geometry.Nodes.Coor = _focalPoints.ToArray();
            AddPoints(data);
        }
        //
        // The native view keeps its camera distance and clipping range on the visible geometry by itself
        public void AdjustCameraDistanceAndClippingRedraw()
        {
            RenderScene();
        }
        public void UpdateScalarsAndRedraw()
        {
            // Update scalar field
            UpdateScalarFormatting();
            //
            RenderScene();
        }
        public void UpdateScalarsAndCameraAndRedraw()
        {
            // Update scalar field
            UpdateScalarFormatting();
            //
            RenderScene();
        }
        //
        public void Rotate(double azimuthAngle, double elevationAngle, double rollAngle)
        {
            _nativeMirror.Navigate(VtkNativeNavigation.Rotate, azimuthAngle, elevationAngle, rollAngle);
            OnNativeCameraChanged();
        }

        #endregion  ################################################################################################################

        #region Add geometry  ######################################################################################################
        private bool AreAnyActorsVisible()
        {
            if (_actors != null && _actors.Count > 0)
            {
                foreach (var entry in _actors)
                {
                    if (entry.Value.VtkMaxActorVisible) return true;
                }
            }
            if (_overlayActors != null && _overlayActors.Count > 0)
            {
                foreach (var entry in _overlayActors)
                {
                    if (entry.Value.VtkMaxActorVisible) return true;
                }
            }
            if (_selectedActors != null && _selectedActors.Count > 0)
            {
                foreach (var entry in _selectedActors)
                {
                    if (entry.VtkMaxActorVisible) return true;
                }
            }
            return false;
        }
        // Drawing thousands of symbols adjusts the camera once at the end, because every adjustment measures every actor
        public void SuspendCameraAdjustment()
        {
            _cameraAdjustmentSuspendCount++;
            if (_cameraAdjustmentSuspendCount == 1)
                RunNative("SuspendCameraAdjustment", () => _nativeMirror.SetCameraAdjustmentSuspended(true));
        }
        public void ResumeCameraAdjustment()
        {
            if (_cameraAdjustmentSuspendCount <= 0)
                throw new InvalidOperationException("The camera adjustment is not suspended.");
            //
            _cameraAdjustmentSuspendCount--;
            if (_cameraAdjustmentSuspendCount == 0)
                RunNative("ResumeCameraAdjustment", () => _nativeMirror.SetCameraAdjustmentSuspended(false));
            if (_cameraAdjustmentSuspendCount > 0 || !_cameraAdjustmentPending) return;
            //
            _cameraAdjustmentPending = false;
            AdjustCameraAfterAdd();
        }
        private void AdjustCameraAfterAdd()
        {
            if (_cameraAdjustmentSuspendCount > 0)
            {
                _cameraAdjustmentPending = true;
                return;
            }
            //
            if (!AreAnyActorsVisible()) AdjustCameraDistanceAndClippingRedraw();
        }
        public vtkMaxActor AddPoints(vtkMaxActorData data)
        {
            vtkMaxActor actor = new vtkMaxActor(data, false, true);
            AddActorGeometry(actor, data.Layer);
            AddNativeActor(actor);
            //
            AdjustCameraAfterAdd();
            //
            return actor;
        }
        public vtkMaxActor AddCells(vtkMaxActorData data)
        {
            vtkMaxActor actor = new vtkMaxActor(data);
            AddActorGeometry(actor, actor.Layer);
            AddNativeActor(actor);
            //
            AdjustCameraAfterAdd();
            //
            return actor;
        }
        public void AddCells(vtkMaxActorData[] data)
        {
            vtkMaxActor[] actors = new vtkMaxActor[data.Length];
            for (int i = 0; i < data.Length; i++) actors[i] = new vtkMaxActor(data[i]);
            //
            foreach (var actor in actors) AddActorGeometry(actor, actor.Layer);
            foreach (var actor in actors) AddNativeActor(actor);
            //
            AdjustCameraAfterAdd();
        }
        //
        public void AddSphereActor(vtkMaxActorData data, double symbolSize)
        {
            AddSymbolActor(data, "sphere", symbolSize, VtkNativeSymbol.Sphere);
        }
        public void AddCoordinateSystemSymbol(double[] origin, double[][] axisDirections, string[] labels,
                                              double symbolSize, vtkRendererLayer layer)
        {
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;
            //
            AddNativeCoordinateSystemSymbol(origin, axisDirections, labels, symbolSize, layer, false);
        }
        public void AddMaterialAxesSymbol(double[] origin, double[][] axisDirections, string[] labels, double symbolSize)
        {
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;
            //
            AddNativeCoordinateSystemSymbol(origin, axisDirections, labels, symbolSize, vtkRendererLayer.Selection, true);
        }
        public void AddCaptionActor(string name, string caption, Color color, double[] position,
                                    double[] offsetVector, double fontScaleFactor, vtkRendererLayer layer)
        {
            vtkMaxCaptionActor actor = new vtkMaxCaptionActor(name, color);
            actor.Layer = layer;
            actor.Position = position;
            if (offsetVector != null && offsetVector.Length == 3) actor.OffsetVector = offsetVector;
            //
            AddActorGeometry(actor, layer);
            AddNativeCaption(actor, caption, position, actor.OffsetVector, fontScaleFactor, layer);
        }
        public void AddLabelActor(string name, double[][] positions, int[] labels, Color color,
                                  double fontScaleFactor, vtkRendererLayer layer)
        {
            if (positions == null || labels == null || positions.Length == 0 || positions.Length != labels.Length)
                throw new ArgumentException("The label positions and values must have the same non-zero length.");
            //
            string[] labelTexts = labels.Select(label => label.ToString(System.Globalization.CultureInfo.InvariantCulture))
                                        .ToArray();
            AddLabelActor(name, positions, labelTexts, color, fontScaleFactor, layer);
        }
        public void AddLabelActor(string name, double[][] positions, string[] labels, Color color,
                                  double fontScaleFactor, vtkRendererLayer layer)
        {
            if (positions == null || labels == null || positions.Length == 0 || positions.Length != labels.Length)
                throw new ArgumentException("The label positions and values must have the same non-zero length.");
            // All labels are one actor
            vtkMaxCaptionActor actor = new vtkMaxCaptionActor(name, color);
            actor.Layer = layer;
            actor.Position = positions[0];
            AddActorGeometry(actor, layer);
            AddNativeLabels(actor, positions, labels, color, fontScaleFactor, layer);
        }
        public void AddOrientedDisplacementConstraintActor(vtkMaxActorData data, double symbolSize)
        {
            AddSymbolActor(data, "cones", symbolSize, VtkNativeSymbol.DisplacementCone);
        }
        public void AddOrientedConeActors(vtkMaxActorData data, double symbolSize)
        {
            AddSymbolActor(data, "cones", symbolSize, VtkNativeSymbol.Cone);
        }
        public void AddOrientedRotationalConstraintActor(vtkMaxActorData data, double symbolSize)
        {
            AddSymbolActor(data, "cylinder", symbolSize, VtkNativeSymbol.RotationalConstraint);
        }
        public void AddOrientedJointActor(vtkMaxActorData data, double symbolSize, vtkJointSymbolEnum symbol)
        {
            AddSymbolActor(data, "joint", symbolSize, VtkNativeSymbol.Joint, (int)symbol);
        }
        public void AddOrientedArrowsActor(vtkMaxActorData data, double symbolSize, bool invert, double relativeSize = 1)
        {
            data.Geometry.RemoveZeroLengthNormals();
            AddSymbolActor(data, "arrow", symbolSize, VtkNativeSymbol.Arrow, 0, invert, false, relativeSize);
        }
        public void AddOrientedDoubleArrowsActor(vtkMaxActorData data, double symbolSize)
        {
            data.Geometry.RemoveZeroLengthNormals();
            AddSymbolActor(data, "doubleArrow", symbolSize, VtkNativeSymbol.DoubleArrow);
        }
        public void AddOrientedSpringActor(vtkMaxActorData data, double symbolSize, bool invert)
        {
            AddSymbolActor(data, "spring", symbolSize, VtkNativeSymbol.Spring);
        }
        public void AddOrientedThermoActor(vtkMaxActorData data, double symbolSize, bool invert)
        {
            AddSymbolActor(data, "thermo", symbolSize, VtkNativeSymbol.Thermo, 0, invert);
        }
        public void AddOrientedFluxActor(vtkMaxActorData data, double symbolSize, bool center, bool invert)
        {
            AddSymbolActor(data, "flux", symbolSize, VtkNativeSymbol.Flux, 0, invert, center);
        }
        // A symbol glyphed onto the points of the data, named after its kind, and its silhouette edges
        private void AddSymbolActor(vtkMaxActorData data, string suffix, double symbolSize, VtkNativeSymbol kind,
                                    int joint = 0, bool invert = false, bool center = false, double relativeSize = 1)
        {
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;
            //
            data.Name += Globals.NameSeparator + suffix;
            vtkMaxActor actor = new vtkMaxActor(data, vtkMaxActor.GetPointCount(data.Geometry.Nodes));
            ApplySymbolFormattingToActor(actor);
            AddActorGeometry(actor, data.Layer);
            //
            vtkMaxActor edges = _drawSymbolEdges ? AddSymbolEdges(data) : null;
            AddNativeSymbol(actor, data, kind, symbolSize, joint, invert, center, relativeSize);
            if (edges != null && _nativeActors.Contains(actor)) _nativeActors.Add(edges);
        }
        private vtkMaxActor AddSymbolEdges(vtkMaxActorData data)
        {
            data.Name += Globals.NameSeparator + "silhouette";
            vtkMaxActor actor = new vtkMaxActor(data, vtkMaxActor.GetPointCount(data.Geometry.Nodes));
            actor.Pickable = false;
            // Backface color is used for annotateWithColor
            if (data.BackfaceColor != Color.Empty) actor.Color = data.BackfaceColor;
            //
            double k = 0.2;
            actor.Color = Color.FromArgb((int)(actor.Color.R + (255 - actor.Color.R) * k),
                                         (int)(actor.Color.G + (255 - actor.Color.G) * k),
                                         (int)(actor.Color.B + (255 - actor.Color.B) * k));
            AddActorGeometry(actor, data.Layer);
            return actor;
        }
        //
        public void AddActor(vtkMaxActor actor)
        {
            AddActorGeometry(actor, actor.Layer);
            AddNativeActor(actor);
        }
        private PartExchangeData GetVolumeGridData(string sourceName)
        {
            if (Controller_GetVolumeGridData == null) return null;
            // An animated part is built for the displayed frame
            int frameNumber = _animationActors.ContainsKey(sourceName) ? _currentAnimationFrameNumber : -1;
            return Controller_GetVolumeGridData(sourceName, frameNumber);
        }
        private void AddActorGeometry(vtkMaxActor actor, vtkRendererLayer layer)
        {
            if (layer == vtkRendererLayer.Base)
            {
                if (actor.Name == null) actor.Name = (_actors.Count + 1).ToString();
                _actors.Add(actor.Name, actor);
                if (actor.VolumeGridAvailable)
                {
                    actor.VolumeGridProvider = GetVolumeGridData;
                    actor.AnimationNodesProvider = Controller_GetAnimationNodes;
                }
            }
            else if (layer == vtkRendererLayer.Overlay)
            {
                if (actor.Name == null) actor.Name = (_overlayActors.Count + 1).ToString();
                _overlayActors.Add(actor.Name, actor);
            }
            else if (layer == vtkRendererLayer.Selection)
            {
                if (actor.Name == null) actor.Name = (_selectedActors.Count + 1).ToString();
                _selectedActors.Add(actor);
            }
        }

        #endregion  ################################################################################################################

        #region Highlight geometry  ################################################################################################
        // The model edges of the part, or its silhouette, and the outline of its section faces, in the selection layer
        public void HighlightActor(string actorName)
        {
            if (!_actors.ContainsKey(actorName)) return;
            //
            vtkMaxActor highlightActor = new vtkMaxActor();
            highlightActor.Layer = vtkRendererLayer.Selection;
            AddActorGeometry(highlightActor, vtkRendererLayer.Selection);
            RunNative("HighlightActor", () =>
            {
                _nativeMirror.HighlightActor(actorName);
                _nativeActors.Add(highlightActor);
            });
            RenderScene();
        }

        #endregion  ################################################################################################################

        #region Hide/Show geometry  ################################################################################################
        public void HideActors(string[] actorNames, bool updateColorContours)
        {
            HideShowActors(actorNames, updateColorContours, false);
        }
        public void ShowActors(string[] actorNames, bool updateColorContours)
        {
            HideShowActors(actorNames, updateColorContours, true);
        }
        private void HideShowActors(string[] actorNames, bool updateColorContours, bool visible)
        {
            string[] tmp;
            string[] splitter = new string[] { "_animation-frame-", "_undeformed" };
            HashSet<string> actorNamesHash = new HashSet<string>(actorNames);
            foreach (var entry in _actors)
            {
                // Check for animated actors
                tmp = entry.Key.Split(splitter, StringSplitOptions.None);
                if (tmp.Length == 2)
                {
                    if (actorNamesHash.Contains(tmp[0]))
                    {
                        entry.Value.VtkMaxActorVisible = visible;
                        // Animated actors
                        _animationFrameData.ActorVisible[tmp[0]] = visible;
                    }
                }
                // Non animated actors
                else
                {
                    if (actorNamesHash.Contains(entry.Key) ||
                        (entry.Value.ParentName != null && actorNamesHash.Contains(entry.Value.ParentName)))
                        HideShowActor(entry.Value, visible);
                }
            }
            // Overlay actors
            vtkMaxActor actor;
            foreach (var name in actorNames)
            {
                if (_overlayActors.TryGetValue(name, out actor)) actor.VtkMaxActorVisible = true;
            }
            //
            RunNative("HideShowActors", () => _nativeMirror.HideShowActors(actorNames, visible));
            if (updateColorContours) UpdateScalarFormatting();
            //
            RenderScene();
        }
        private void HideShowActor(vtkMaxActor actor, bool visible)
        {
            actor.VtkMaxActorVisible = visible;
            // Transformed copies
            foreach (var copy in actor.Copies) HideShowActor(copy, visible);
        }
        public void UpdateResultRepresentationVisibility(string[] partNames, bool visible, bool update)
        {
            HashSet<string> partNameSet = new HashSet<string>(partNames);
            foreach (vtkMaxActor actor in _actors.Values)
            {
                if (actor.ParentName != null && partNameSet.Contains(actor.ParentName))
                {
                    bool actorVisible = visible;
                    if (actorVisible && _actors.TryGetValue(actor.ParentName, out vtkMaxActor parentActor))
                        actorVisible = parentActor.VtkMaxActorVisible;
                    HideShowActor(actor, actorVisible);
                }
            }
            RunNative("UpdateResultRepresentationVisibility",
                      () => _nativeMirror.UpdateResultRepresentationVisibility(partNames, visible, update));
            //
            if (update)
            {
                UpdateScalarFormatting();
                RenderScene();
            }
        }

        #endregion  ################################################################################################################

        #region Section view  ######################################################################################################
        public void CreateSectionView(double[] point, double[] normal, bool lightenColors, Color sectionColor,
                                      bool elementWise, bool hatch)
        {
            CreateSectionView(point, normal, lightenColors, sectionColor, elementWise, hatch, GetDefaultHatchSpacing());
        }
        private void CreateSectionView(double[] point, double[] normal, bool lightenColors, Color sectionColor,
                                       bool elementWise, bool hatch, double hatchSpacing)
        {
            lock (myLock)
            {
                if (!_sectionViewData.Active)
                {
                    _sectionViewData.Active = true;
                    _sectionViewData.Point = point.ToArray();
                    _sectionViewData.Normal = normal.ToArray();
                    _sectionViewData.LightenColors = lightenColors;
                    _sectionViewData.SectionColor = sectionColor;
                    _sectionViewData.ElementWise = elementWise;
                    _sectionViewData.Hatch = hatch;
                    _sectionViewData.HatchSpacing = hatchSpacing;
                    CreateNativeSectionView(point, normal);
                    //
                    UpdateScalarFormatting();
                    //
                    RenderScene();
                }
            }
        }
        public void UpdateSectionView(double[] point, double[] normal, bool lightenColors, Color sectionColor,
                                      bool elementWise, bool hatch)
        {
            lock (myLock)
            {
                if (_sectionViewData.Active)
                {
                    _sectionViewData.Point = point.ToArray();
                    _sectionViewData.Normal = normal.ToArray();
                    _sectionViewData.LightenColors = lightenColors;
                    _sectionViewData.SectionColor = sectionColor;
                    _sectionViewData.ElementWise = elementWise;
                    _sectionViewData.Hatch = hatch;
                    CreateNativeSectionView(point, normal);
                    //
                    UpdateScalarFormatting();
                    //
                    RenderScene();
                }
            }
        }
        public void RemoveSectionView(bool releaseVolumeGrids = false)
        {
            lock (myLock)
            {
                if (_sectionViewData.Active)
                {
                    foreach (var entry in _actors) entry.Value.GeometryHiddenBySectionView = false;
                    //
                    _sectionViewData.Reset();
                    RunNative("RemoveSectionView", () => _nativeMirror.RemoveSectionView());
                    //
                    UpdateScalarFormatting();
                    //
                    RenderScene();
                }
            }
        }
        private double GetDefaultHatchSpacing()
        {
            return GetBoundingBoxDiagonal() / 100;
        }

        #endregion  ################################################################################################################

        #region Exploded view  #####################################################################################################
        public void PreviewExplodedView(Dictionary<string, double[]> partOffsets, bool animate, int timeMs)
        {
            vtkMaxActor actor;
            double[] offsetFinal;
            double[] offsetStart;
            //
            Dictionary<string, double[]> partOffsetsAnimation = new Dictionary<string, double[]>();
            Dictionary<string, List<vtkMaxActor>> partActors = new Dictionary<string, List<vtkMaxActor>>();
            foreach (var partOffset in partOffsets)
            {
                offsetFinal = partOffset.Value;
                if (_actors.TryGetValue(partOffset.Key, out actor))
                {
                    offsetStart = actor.ExplodedOffset;
                    partOffsetsAnimation.Add(partOffset.Key, new double[] { offsetFinal[0] - offsetStart[0],
                                                                            offsetFinal[1] - offsetStart[1],
                                                                            offsetFinal[2] - offsetStart[2]});
                    partActors.Add(partOffset.Key,
                                   _actors.Where(entry => entry.Key == partOffset.Key ||
                                                          entry.Value.ParentName == partOffset.Key)
                                          .Select(entry => entry.Value).ToList());
                }
            }
            //
            DateTime start = DateTime.Now;
            int delta = timeMs; //ms
            double currentDelta;
            double t;
            double tPrev = 0;
            Vec3D position = new Vec3D();
            do
            {
                currentDelta = (DateTime.Now - start).TotalMilliseconds;
                t = currentDelta / delta;
                if (t > 1 || !animate) t = 1;
                t = 0.5 + 0.5 * Math.Atan(t * 6 - 3) / Math.Atan(3);
                //
                foreach (var partOffset in partOffsetsAnimation)
                {
                    position.Coor = partOffset.Value;
                    position *= (t - tPrev);
                    foreach (vtkMaxActor partActor in partActors[partOffset.Key])
                    {
                        Vec3D actorPosition = new Vec3D(partActor.ExplodedOffset);
                        actorPosition += position;
                        partActor.ExplodedOffset = actorPosition.Coor;
                    }
                }
                tPrev = t;
                //
                // The native view has the positions before the window is painted: a preview without animation,
                // which puts the parts back where an exploded view had them, showed them unexploded for a frame
                if (t < 1) SetNativeExplodedViewOffsets(partOffsetsAnimation.Keys);
                else RunNative("PreviewExplodedView", () =>
                    _nativeMirror.SetExplodedViewOffsets(partOffsets.Keys.ToArray(), partOffsets.Values.ToArray()));
                //
                RenderScene();
                //
                Application.DoEvents();
            }
            while (t < 1);
        }
        public void RemovePreviewedExplodedView(string[] partNames)
        {
            HashSet<string> partNameSet = new HashSet<string>(partNames);
            //
            foreach (var entry in _actors)
            {
                if (partNameSet.Contains(entry.Key) ||
                    (entry.Value.ParentName != null && partNameSet.Contains(entry.Value.ParentName)))
                    entry.Value.ExplodedOffset = new double[3];
            }
            RunNative("RemovePreviewedExplodedView", () =>
                _nativeMirror.SetExplodedViewOffsets(partNames, partNames.Select(name => new double[3]).ToArray()));
            //
            RenderScene();
        }

        #endregion  ################################################################################################################

        #region Follower view  #####################################################################################################
        public void ApplyFollowerView(string[] partNames, int[] nodeIds)
        {
            if (partNames == null || partNames.Length == 0 || nodeIds == null || partNames.Length != nodeIds.Length) return;
            //
            _followerViewPartNames = partNames;
            _followerViewPointIds = new int[partNames.Length];
            _followerViewPointCoor = new double[partNames.Length][];
            //
            _followerViewPointIds[0] = GetPointId(_followerViewPartNames[0], nodeIds[0]);
            //
            if (_followerViewPartNames.Length == 3)
            {
                _followerViewPointIds[1] = GetPointId(_followerViewPartNames[1], nodeIds[1]);
                _followerViewPointIds[2] = GetPointId(_followerViewPartNames[2], nodeIds[2]);
            }
            //
            UpdateFollowerView();
        }
        private int GetPointId(string partName, int nodeId)
        {
            if (partName == null) return -1;
            //
            vtkMaxActor actor;
            if (_actors.TryGetValue(partName, out actor) && actor.TryGetPointId(nodeId, out int pointId)) return pointId;
            return -1;
        }
        // The point of a part as it is drawn: deformed and at the animation frame shown
        private bool TryGetFollowerViewPoint(int index, out Vec3D point)
        {
            point = null;
            vtkMaxActor actor;
            if (!_actors.TryGetValue(_followerViewPartNames[index], out actor)) return false;
            double[] coor = actor.GetPoint(_followerViewPointIds[index]);
            if (coor == null) return false;
            point = new Vec3D(coor);
            return true;
        }
        public void UpdateFollowerView()
        {
            if (_followerViewPartNames == null) return;
            //
            if (_followerViewPartNames.Length == 1)
            {
                UpdateFollowerViewPint();
            }
            else if (_followerViewPartNames.Length == 3)
            {
                UpdateFollowerViewPlane();
            }
        }
        public void UpdateFollowerViewPint()
        {
            Vec3D centerPoint;
            if (!TryGetFollowerViewPoint(0, out centerPoint)) return;
            //
            if (_followerViewPointCoor[0] == null) _followerViewPointCoor[0] = centerPoint.Coor;
            else if (_nativeMirror != null)
            {
                Vec3D delta = new Vec3D(_followerViewPointCoor[0]) - centerPoint;
                // Move only in the view plane
                Vec3D normal = new Vec3D(GetNativeViewPlaneNormal());
                double t = Vec3D.DotProduct(normal, delta);
                Vec3D projection = t * normal;
                delta -= projection;
                //
                MoveFollowerViewCamera(new double[][] { new double[] { 1, 0, 0 }, new double[] { 0, 1, 0 },
                                                        new double[] { 0, 0, 1 } }, (-1 * delta).Coor);
                //
                _followerViewPointCoor[0] = centerPoint.Coor;
            }
        }
        public void UpdateFollowerViewPlane()
        {
            Vec3D p1;
            Vec3D p2;
            Vec3D p3;
            if (!TryGetFollowerViewPoint(0, out p1) || !TryGetFollowerViewPoint(1, out p2) ||
                !TryGetFollowerViewPoint(2, out p3)) return;
            //
            if (_followerViewPointCoor[0] == null) _followerViewPointCoor[0] = p1.Coor;
            if (_followerViewPointCoor[1] == null) _followerViewPointCoor[1] = p2.Coor;
            if (_followerViewPointCoor[2] == null) _followerViewPointCoor[2] = p3.Coor;
            // Get previous coordinates as vectors
            Vec3D p1Prev = new Vec3D(_followerViewPointCoor[0]);
            Vec3D p2Prev = new Vec3D(_followerViewPointCoor[1]);
            Vec3D p3Prev = new Vec3D(_followerViewPointCoor[2]);
            // Compute pervious centroid
            Vec3D cPrev = (p1Prev + p2Prev + p3Prev) * (1.0 / 3.0);
            // Compute previous basis
            Vec3D e1Prev = p2Prev - p1Prev; e1Prev.Normalize();
            Vec3D nPrev = Vec3D.CrossProduct(p2Prev - p1Prev, p3Prev - p1Prev); nPrev.Normalize();
            Vec3D e2Prev = Vec3D.CrossProduct(nPrev, e1Prev);
            // Compute rotation matrix from previous basis
            double[][] RPrev = new double[3][]
            {
                new double[] { e1Prev.X, e2Prev.X, nPrev.X },
                new double[] { e1Prev.Y, e2Prev.Y, nPrev.Y },
                new double[] { e1Prev.Z, e2Prev.Z, nPrev.Z }
            };
            // Compute current centroid
            Vec3D cCur = (p1 + p2 + p3) * (1.0 / 3.0);
            // Compute current basis
            Vec3D e1Cur = p2 - p1; e1Cur.Normalize();
            Vec3D nCur = Vec3D.CrossProduct(p2 - p1, p3 - p1); nCur.Normalize();
            Vec3D e2Cur = Vec3D.CrossProduct(nCur, e1Cur);
            // Compute rotation matrix from current basis
            double[][] RCur = new double[3][]
            {
                new double[] { e1Cur.X, e2Cur.X, nCur.X },
                new double[] { e1Cur.Y, e2Cur.Y, nCur.Y },
                new double[] { e1Cur.Z, e2Cur.Z, nCur.Z }
            };
            // Compute rotation delta
            double[][] RPrevT = Matrix.MatrixTranspose(RPrev);
            double[][] RDelta = Matrix.MatrixProduct(RCur, RPrevT);
            // Compute translation delta
            Vec3D Rc = MultiplyMatrixVector(RDelta, cPrev);
            Vec3D tDelta = cCur - Rc;
            // Inverse transform
            double[][] RInv = (RDelta);
            Vec3D tInv = MultiplyMatrixVector(RInv, tDelta);
            // Apply to camera
            MoveFollowerViewCamera(RInv, tInv.Coor);
            //
            _followerViewPointCoor[0] = p1.Coor;
            _followerViewPointCoor[1] = p2.Coor;
            _followerViewPointCoor[2] = p3.Coor;
        }
        // The camera of the view moves with the points: its position and focal point by the rotation and the
        // translation, its view up by the rotation
        private void MoveFollowerViewCamera(double[][] rotation, double[] translation)
        {
            MoveNativeCamera(rotation, translation);
            OnNativeCameraChanged();
        }
        // --- Helper: multiply 3x3 matrix with Vec3D
        private Vec3D MultiplyMatrixVector(double[][] M, Vec3D v)
        {
            return new Vec3D(
                M[0][0] * v.X + M[0][1] * v.Y + M[0][2] * v.Z,
                M[1][0] * v.X + M[1][1] * v.Y + M[1][2] * v.Z,
                M[2][0] * v.X + M[2][1] * v.Y + M[2][2] * v.Z
            );
        }
        public void RemoveFollowerView()
        {
            _followerViewPartNames = null;
            _followerViewPointIds = null;
            _followerViewPointCoor = null;
        }

        #endregion  ################################################################################################################

        #region Transformations  ###################################################################################################
        public void AddSymmetry(int symmetryPlane, double[] symmetryPoint)
        {
            double[] scale;
            if (symmetryPlane == 0) scale = new double[] { -1, 1, 1 };
            else if (symmetryPlane == 1) scale = new double[] { 1, -1, 1 };
            else if (symmetryPlane == 2) scale = new double[] { 1, 1, -1 };
            else throw new NotSupportedException();
            //
            _transforms.Add(Tuple.Create(vtkMaxActor.GetScale(symmetryPoint, scale), 2));  // must be 2 for 1 loop
            RunNative("AddSymmetry", () => _nativeMirror.AddSymmetry(symmetryPlane, symmetryPoint));
        }
        public void AddLinearPattern(double[] displacement, int numOfItems)
        {
            _transforms.Add(Tuple.Create(vtkMaxActor.GetTranslation(displacement), numOfItems));
            RunNative("AddLinearPattern", () => _nativeMirror.AddLinearPattern(displacement, numOfItems));
        }
        public void AddCircularPattern(double[] axisPoint, double[] axisNormal, double angle, int numOfItems)
        {
            _transforms.Add(Tuple.Create(vtkMaxActor.GetRotation(axisPoint, axisNormal, angle), numOfItems));
            RunNative("AddCircularPattern",
                      () => _nativeMirror.AddCircularPattern(axisPoint, axisNormal, angle, numOfItems));
        }
        public void ApplyTransforms()
        {
            _nativeTransformIndex = 0;
            foreach (var transform in _transforms)
            {
                TransformAllMaxActors(transform.Item1, transform.Item2);
                _nativeTransformIndex++;
            }
            ApplyNativeTransforms();
            //
            UpdateScalarFormatting();
            //
            RenderScene();
        }
        public void RemoveTransformedActors()
        {
            bool copyExists = true;
            while (copyExists)
            {
                copyExists = false;
                foreach (vtkMaxActor actor in _actors.Values.ToArray())
                {
                    if (actor.Copies != null && actor.Copies.Count > 0)
                    {
                        foreach (var copy in actor.Copies) RemoveActorWithCopies(copy);
                        actor.Copies.Clear();
                        //
                        copyExists = true;
                    }
                }
            }
            RunNative("RemoveTransformedActors", () => _nativeMirror.RemoveTransformedActors());
        }
        private void RemoveActorWithCopies(vtkMaxActor actor)
        {
            // Transformed copies
            foreach (var copy in actor.Copies) RemoveActorWithCopies(copy);
            //
            actor.Copies.Clear();
            //
            _actors.Remove(actor.Name);
            _nativeActors.Remove(actor);
        }
        public void HideTransformedActors()
        {
            foreach (var entry in _actors)
            {
                if (entry.Value.Copies != null)
                {
                    foreach (var actorCopy in entry.Value.Copies) HideShowActor(actorCopy, false);
                }
            }
            RunNative("HideTransformedActors", () => _nativeMirror.ShowTransformedActors(false));
            RenderScene();
        }
        public void ShowTransformedActors()
        {
            foreach (var entry in _actors)
            {
                if (entry.Value.Copies != null)
                {
                    foreach (var actorCopy in entry.Value.Copies) HideShowActor(actorCopy, entry.Value.VtkMaxActorVisible);
                }
            }
            RunNative("ShowTransformedActors", () => _nativeMirror.ShowTransformedActors(true));
            RenderScene();
        }
        //
        private void TransformAllMaxActors(double[] transform, int numOfItems)
        {
            vtkMaxActor baseActor;
            vtkMaxActor transformedActor;
            foreach (var actor in _actors.Values.ToArray())
            {
                baseActor = actor;
                //
                for (int i = 0; i < numOfItems - 1; i++)
                {
                    transformedActor = new vtkMaxActor(baseActor);
                    transformedActor.Name = GetTransformedActorName(transformedActor.Name);
                    NameNativeCopy(transformedActor, actor, i + 1);
                    //
                    baseActor.Copies.Add(transformedActor);
                    //
                    TransformMaxActor(transformedActor, transform);
                    //
                    AddActorGeometry(transformedActor, vtkRendererLayer.Base);
                    //
                    baseActor = transformedActor;
                }
            }
        }
        private void TransformMaxActor(vtkMaxActor vtkMaxActor, double[] transform)
        {
            vtkMaxActor.AppendTransform(transform);
            // Reconstruct the selected directional result component after geometry transformation
            vtkMaxActor.ApplyResultFieldTransformation(vtkMaxActor.GetResultFieldTransformation(transform));
        }
        //
        private string GetTransformedActorName(string actorName)
        {
            string name = actorName + Globals.NameSeparator + Globals.TransformationSuffix;
            name = _actors.GetNextNumberedKey(name);
            return name;
        }

        #endregion  ################################################################################################################

        #region Settings ###########################################################################################################
        public void SetCoorSysVisibility(bool visibility)
        {
            if (_coorSysWidget == null) return;
            //
            _drawCoorSys = visibility;
            if (_drawCoorSys)
            {
                ResetCoorSysWidgetPosition();
                _coorSysWidget.VisibilityOn();
            }
            else _coorSysWidget.VisibilityOff();
        }
        public void ResetCoorSysWidgetPosition()
        {
            if (_coorSysWidget == null) return;
            //
            _coorSysWidget.SetPosition(Width - _coorSysWidget.GetWidth() - CoorSysWidgetMargin, CoorSysWidgetMargin);
        }
        // Scale bar
        public void SetScaleWidgetVisibility(bool visibility)
        {
            if (visibility) _scaleWidget.VisibilityOn();
            else _scaleWidget.VisibilityOff();
        }
        public void SetScaleWidgetUnit(string unit)
        {
            _scaleWidget.SetUnit(unit);
            //
            RenderScene();
        }
        // Scalar bar
        public void InitializeResultWidgetPositions()
        {
            if (_nativeMirrorScalarBar != null) _nativeMirrorScalarBar.SetPosition(VtkNativeWidgetAnchor.TopLeft, 20, 20);
            _statusBlockWidget.SetTopLeftPosition(Width - _statusBlockWidget.GetWidth() - 20, 20);
        }
        public void SetScalarBarColorSpectrum(vtkMaxColorSpectrum colorSpectrum)
        {
            _colorSpectrum.DeepCopy(colorSpectrum);
            MirrorScalarBarColorSpectrum(colorSpectrum);
        }
        // Below-min / above-max limit color bands only exist when the manual limit clips the data
        public bool ScalarBarAddMinColor
        {
            get { return _nativeMirrorScalarBar != null && _nativeMirrorScalarBar.AddMinColor; }
        }
        public bool ScalarBarAddMaxColor
        {
            get { return _nativeMirrorScalarBar != null && _nativeMirrorScalarBar.AddMaxColor; }
        }
        public void SetScalarBarVisibility(bool visibility)
        {
            _keepScalarBarVisible = visibility;
            if (!visibility) SetScalarBarWidgetVisibility(false);
        }
        public bool TryGetScalarBarValueEditData(bool editMinimum, out Rectangle rectangle, out string valueText,
                                                 out double minimum, out double maximum)
        {
            if (_nativeMirrorScalarBar != null)
                return _nativeMirrorScalarBar.TryGetValueEditData(editMinimum, out rectangle, out valueText,
                                                                  out minimum, out maximum);
            rectangle = Rectangle.Empty;
            valueText = null;
            minimum = double.NaN;
            maximum = double.NaN;
            return false;
        }
        private bool TryGetScalarBarValueEditData(int x, int y, out bool editMinimum, out Rectangle rectangle,
                                                  out string valueText, out double minimum, out double maximum)
        {
            if (_nativeMirrorScalarBar != null)
                return _nativeMirrorScalarBar.TryGetValueEditData(x, y, out editMinimum, out rectangle, out valueText,
                                                                  out minimum, out maximum);
            editMinimum = false;
            return TryGetScalarBarValueEditData(false, out rectangle, out valueText, out minimum, out maximum);
        }
        public void SetScalarBarNumberFormat(string numberFormat)
        {
            _scalarBarLabelFormat = numberFormat;
            if (_nativeMirrorScalarBar != null) _nativeMirrorScalarBar.SetLabelFormat(numberFormat);
        }
        public void SetScalarBarText(string fieldName, string componentName, string unitAbbreviation, string complexComponent,
                                     string minMaxType)
        {
            _scalarBarText = new string[] { fieldName, componentName, unitAbbreviation, complexComponent, minMaxType };
            if (_nativeMirrorScalarBar != null)
                _nativeMirrorScalarBar.SetText(fieldName, componentName, unitAbbreviation, complexComponent, minMaxType);
            //
            UpdateScalarFormatting();
        }
        public void DrawScalarBarBackground(bool drawBackground)
        {
            _scalarBarBackgroundVisible = drawBackground;
            MirrorScalarBarFrame();
        }
        public void DrawScalarBarBorder(bool drawBorder)
        {
            _scalarBarBorderVisible = drawBorder;
            MirrorScalarBarFrame();
        }
        private string GetUnitAbbreviation()
        {
            if (!_scalarBarVisible) return "";
            //
            string unitAbbreviation = _scalarBarText == null ? "" : _scalarBarText[2];
            if (unitAbbreviation == "/") unitAbbreviation = "";
            else unitAbbreviation = " " + unitAbbreviation;
            return unitAbbreviation;
        }
        // Color bar
        public void InitializeColorBarWidgetPosition()
        {
            _colorBarWidget.SetTopLeftPosition(20, 20);
        }
        public void SetColorBarColorsAndLabels(Color[] colors, string[] labels)
        {
            _colorBarWidget.SetColorsAndLabels(colors, labels);
            _colorBarWidget.VisibilityOn();
        }
        public void AddColorBarColorsAndLabels(Color[] colors, string[] labels)
        {
            _colorBarWidget.AddColorsAndLabels(colors, labels);
            _colorBarWidget.VisibilityOn();
        }
        public void DrawColorBarBackground(bool drawBackground)
        {
            if (_colorBarWidget.GetVisibility() == 1)
            {
                _colorBarWidget.SetBackgroundColor(1, 1, 1);
                if (drawBackground) _colorBarWidget.BackgroundVisibilityOn();
                else _colorBarWidget.BackgroundVisibilityOff();
            }
        }
        public void DrawColorBarBorder(bool drawBorder)
        {
            if (_colorBarWidget.GetVisibility() == 1)
            {
                if (drawBorder) _colorBarWidget.BorderVisibilityOn();
                else _colorBarWidget.BorderVisibilityOff();
            }
        }
        public void HideColorBar()
        {
            _colorBarWidget.VisibilityOff();
            _colorBarWidget.ClearColorsAndLabels();
            //
            RenderScene();
        }
        // Status bar
        public void SetStatusBlockVisibility(bool visibility)
        {
            _drawStatusBlock = visibility;
        }
        public void SetStatusBlockNumberFormat(string numberFormat)
        {
            _statusBlockWidget.SetNumberFormat(numberFormat);
        }
        public void DrawStatusBlockBackground(bool drawBackground)
        {
            _statusBlockWidget.SetBackgroundColor(1, 1, 1);
            if (drawBackground) _statusBlockWidget.BackgroundVisibilityOn();
            else _statusBlockWidget.BackgroundVisibilityOff();
        }
        public void DrawStatusBlockBorder(bool drawBorder)
        {
            if (drawBorder) _statusBlockWidget.BorderVisibilityOn();
            else _statusBlockWidget.BorderVisibilityOff();
        }
        public void SetStatusBlock(string name, DateTime dateTime, float analysisTimeOrFrequency, string unit,
                                   string deformationVariable, float scaleFactor, vtkMaxFieldDataType fieldType, int stepNumber,
                                   int incrementNumber, int nodalDiameter)
        {
            if (_statusBlockWidget == null) return;
            _statusBlockWidget.Name = name;
            _statusBlockWidget.AnalysisTime = analysisTimeOrFrequency;
            _statusBlockWidget.AnalysisTimeUnit = unit;
            _statusBlockWidget.DateTime = dateTime;
            _statusBlockWidget.DeformationVariable = deformationVariable;
            _statusBlockWidget.DeformationScaleFactor = scaleFactor;
            _statusBlockWidget.FieldType = fieldType;
            _statusBlockWidget.AnimationScaleFactor = -1;
            _statusBlockWidget.StepNumber = stepNumber;
            _statusBlockWidget.IncrementNumber = incrementNumber;
            _statusBlockWidget.NodalDiameter = nodalDiameter;
            if (_drawStatusBlock) _statusBlockWidget.VisibilityOn();
            else _statusBlockWidget.VisibilityOff();
        }
        // General
        public void SetBackground(bool gradient, Color topColor, Color bottomColor, bool redraw)
        {
            _gradientBackground = gradient;
            _backgroundTop = new double[] { topColor.R / 255.0, topColor.G / 255.0, topColor.B / 255.0 };
            _backgroundBottom = new double[] { bottomColor.R / 255.0, bottomColor.G / 255.0, bottomColor.B / 255.0 };
            RunNative("SetBackground", SyncNativeMirror);
            if (redraw) RenderScene();
        }
        public void SetLighting(double ambient, double diffuse, bool redraw)
        {
            foreach (var entry in _actors)
            {
                if (entry.Value.UseGlobalLighting)
                {
                    entry.Value.Ambient = ambient; // calls update color
                    entry.Value.Diffuse = diffuse; // calls update color
                }
            }
            if (_nativeMirror != null) _nativeMirror.SetLighting(ambient, diffuse);
            if (redraw) RenderScene();
        }
        public void SetAmbientOcclusion(bool enabled, double relativeRadius, int kernelSize, bool redraw)
        {
            _ambientOcclusion = enabled;
            _ambientOcclusionRelativeRadius = relativeRadius;
            _ambientOcclusionKernelSize = kernelSize;
            if (_nativeMirror != null) _nativeMirror.SetAmbientOcclusion(enabled, relativeRadius, kernelSize);
            if (redraw) RenderScene();
        }
        public void SetSmoothing(bool pointSmoothing, bool lineSmoothing, bool redraw)
        {
            _pointSmoothing = pointSmoothing;
            _lineSmoothing = lineSmoothing;
            if (_nativeMirror != null) _nativeMirror.SetSmoothing(pointSmoothing, lineSmoothing);
            //
            if (redraw) RenderScene();
        }
        // Highlight
        public void SetHighlightColor(Color primaryHighlightColor, Color secondaryHighlightColor)
        {
            _primaryHighlightColor = primaryHighlightColor;
            _secondaryHighlightColor = secondaryHighlightColor;
            //
            RunNative("SetHighlightColor",
                           () => _nativeMirror.SetHighlightColors(primaryHighlightColor, secondaryHighlightColor));
            RenderScene();
        }
        public void SetMouseHighlightColor(Color mouseHighlightColor)
        {
            Globals.CurrentMouseHighlightColor = mouseHighlightColor;
        }
        // Symbols
        public void SetDrawSymbolEdges(bool drawSymbolEdges)
        {
            _drawSymbolEdges = drawSymbolEdges;
        }
        
        #endregion #################################################################################################################

        #region Scalar fields ######################################################################################################
        private const int _numPointsPerLineArrow = 5;

        public void AddVectorFieldActor(vtkMaxActorData data, double maxMagnitude, double maxArrowLength, double arrowSize)
        {
            if (data?.Geometry?.Nodes?.Coor == null || data.Geometry.Nodes.Normals == null ||
                data.Geometry.Nodes.Values == null) return;
            bool drawArrows = maxMagnitude > 0 && maxArrowLength > 0 && arrowSize > 0;
            //
            double tolerance = drawArrows ? maxMagnitude * 1E-10 : 0;
            int numArrows = 0;
            int numNodalPoints = 0;
            for (int i = 0; i < data.Geometry.Nodes.Values.Length; i++)
            {
                float scalarValue = data.Geometry.Nodes.Values[i];
                double[] vector = data.Geometry.Nodes.Normals[i];
                bool validScalar = !float.IsNaN(scalarValue) && !float.IsInfinity(scalarValue);
                if (validScalar) numNodalPoints++;
                if (drawArrows && validScalar &&
                    vector != null && vector.Length == 3)
                {
                    double vectorLength = Math.Sqrt(vector[0] * vector[0] +
                                                    vector[1] * vector[1] +
                                                    vector[2] * vector[2]);
                    if (!double.IsNaN(vectorLength) && !double.IsInfinity(vectorLength) &&
                        vectorLength > tolerance) numArrows++;
                }
            }
            if (numArrows == 0 && numNodalPoints == 0)
            {
                AddEmptyScalarFieldActor(data);
                return;
            }
            //
            int numArrowPoints = _numPointsPerLineArrow * numArrows;
            int numPoints = numArrowPoints + numNodalPoints;
            float[] coordinates = new float[3 * numPoints];
            float[] values = new float[numPoints];
            //
            double headLength = 0.25 * arrowSize;
            double headHalfWidth = 0.06 * arrowSize;
            int arrowId = 0;
            for (int i = 0; i < data.Geometry.Nodes.Values.Length; i++)
            {
                float scalarValue = data.Geometry.Nodes.Values[i];
                double[] vector = data.Geometry.Nodes.Normals[i];
                if (!drawArrows || float.IsNaN(scalarValue) || float.IsInfinity(scalarValue) ||
                    vector == null || vector.Length != 3) continue;
                //
                double vectorLength = Math.Sqrt(vector[0] * vector[0] + vector[1] * vector[1] + vector[2] * vector[2]);
                if (vectorLength <= tolerance || double.IsNaN(vectorLength) || double.IsInfinity(vectorLength)) continue;
                //
                double ux = vector[0] / vectorLength;
                double uy = vector[1] / vectorLength;
                double uz = vector[2] / vectorLength;
                double shaftLength = maxArrowLength * vectorLength / maxMagnitude;
                double[] origin = data.Geometry.Nodes.Coor[i];
                AddLineArrow(coordinates, values, arrowId, scalarValue,
                             origin[0], origin[1], origin[2],
                             origin[0] + shaftLength * ux,
                             origin[1] + shaftLength * uy,
                             origin[2] + shaftLength * uz,
                             headLength, headHalfWidth);
                arrowId++;
            }
            // Draw a scalar-colored point at every retained nodal position, including zero vectors
            AddNodalPoints(data, coordinates, values, numArrowPoints);
            AddPlotActor(data, coordinates, values, numArrows, numNodalPoints);
        }
        public void AddTensorFieldActor(vtkMaxActorData data, double maxAbsValue, double maxGlyphLength, double arrowSize)
        {
            if (data?.Geometry?.Nodes?.Coor == null || data.Geometry.Nodes.Normals == null ||
                data.Geometry.Nodes.Values == null) return;
            bool drawArrows = maxAbsValue > 0 && maxGlyphLength > 0 && arrowSize > 0;
            //
            double tolerance = drawArrows ? maxAbsValue * 1E-10 : 0;
            int numGlyphs = 0;
            int numNodalPoints = 0;
            for (int i = 0; i < data.Geometry.Nodes.Values.Length; i++)
            {
                float scalarValue = data.Geometry.Nodes.Values[i];
                double[] direction = data.Geometry.Nodes.Normals[i];
                bool validScalar = !float.IsNaN(scalarValue) && !float.IsInfinity(scalarValue);
                if (validScalar) numNodalPoints++;
                if (drawArrows && validScalar &&
                    Math.Abs(scalarValue) > tolerance && direction != null && direction.Length == 3)
                {
                    double directionLength = Math.Sqrt(direction[0] * direction[0] +
                                                       direction[1] * direction[1] +
                                                       direction[2] * direction[2]);
                    if (!double.IsNaN(directionLength) && !double.IsInfinity(directionLength) &&
                        directionLength > 0) numGlyphs++;
                }
            }
            if (numGlyphs == 0 && numNodalPoints == 0)
            {
                AddEmptyScalarFieldActor(data);
                return;
            }
            //
            const int numArrowsPerGlyph = 2;
            int numArrows = numArrowsPerGlyph * numGlyphs;
            int numArrowPoints = _numPointsPerLineArrow * numArrows;
            int numPoints = numArrowPoints + numNodalPoints;
            float[] coordinates = new float[3 * numPoints];
            float[] values = new float[numPoints];
            //
            double headLength = 0.125 * arrowSize;
            double headHalfWidth = 0.03 * arrowSize;
            int arrowId = 0;
            for (int i = 0; i < data.Geometry.Nodes.Values.Length; i++)
            {
                float scalarValue = data.Geometry.Nodes.Values[i];
                double[] direction = data.Geometry.Nodes.Normals[i];
                if (!drawArrows || float.IsNaN(scalarValue) || float.IsInfinity(scalarValue) ||
                    Math.Abs(scalarValue) <= tolerance || direction == null || direction.Length != 3) continue;
                //
                double directionLength = Math.Sqrt(direction[0] * direction[0] +
                                                   direction[1] * direction[1] +
                                                   direction[2] * direction[2]);
                if (directionLength <= 0 || double.IsNaN(directionLength) ||
                    double.IsInfinity(directionLength)) continue;
                //
                double ux = direction[0] / directionLength;
                double uy = direction[1] / directionLength;
                double uz = direction[2] / directionLength;
                double halfShaftLength = 0.5 * maxGlyphLength * Math.Abs(scalarValue) / maxAbsValue;
                double[] origin = data.Geometry.Nodes.Coor[i];
                //
                if (scalarValue > 0)
                {
                    AddLineArrow(coordinates, values, arrowId++, scalarValue,
                                 origin[0], origin[1], origin[2],
                                 origin[0] - halfShaftLength * ux,
                                 origin[1] - halfShaftLength * uy,
                                 origin[2] - halfShaftLength * uz,
                                 headLength, headHalfWidth);
                    AddLineArrow(coordinates, values, arrowId++, scalarValue,
                                 origin[0], origin[1], origin[2],
                                 origin[0] + halfShaftLength * ux,
                                 origin[1] + halfShaftLength * uy,
                                 origin[2] + halfShaftLength * uz,
                                 headLength, headHalfWidth);
                }
                else
                {
                    double tailDistance = headLength + halfShaftLength;
                    AddLineArrow(coordinates, values, arrowId++, scalarValue,
                                 origin[0] - tailDistance * ux,
                                 origin[1] - tailDistance * uy,
                                 origin[2] - tailDistance * uz,
                                 origin[0] - headLength * ux,
                                 origin[1] - headLength * uy,
                                 origin[2] - headLength * uz,
                                 headLength, headHalfWidth);
                    AddLineArrow(coordinates, values, arrowId++, scalarValue,
                                 origin[0] + tailDistance * ux,
                                 origin[1] + tailDistance * uy,
                                 origin[2] + tailDistance * uz,
                                 origin[0] + headLength * ux,
                                 origin[1] + headLength * uy,
                                 origin[2] + headLength * uz,
                                 headLength, headHalfWidth);
                }
            }
            // Draw a scalar-colored point at every retained nodal position, including zero values
            AddNodalPoints(data, coordinates, values, numArrowPoints);
            AddPlotActor(data, coordinates, values, numArrows, numNodalPoints);
        }
        private static void AddNodalPoints(vtkMaxActorData data, float[] coordinates, float[] values, int firstPointId)
        {
            int nodalPointId = firstPointId;
            for (int i = 0; i < data.Geometry.Nodes.Values.Length; i++)
            {
                float scalarValue = data.Geometry.Nodes.Values[i];
                if (float.IsNaN(scalarValue) || float.IsInfinity(scalarValue)) continue;
                double[] origin = data.Geometry.Nodes.Coor[i];
                int coordinateId = 3 * nodalPointId;
                coordinates[coordinateId] = (float)origin[0];
                coordinates[coordinateId + 1] = (float)origin[1];
                coordinates[coordinateId + 2] = (float)origin[2];
                values[nodalPointId++] = scalarValue;
            }
        }
        // A vector or a tensor plot: its line arrows and the points of its nodes, with the extreme nodes of the field
        private void AddPlotActor(vtkMaxActorData data, float[] coordinates, float[] values, int numArrows,
                                  int numNodalPoints)
        {
            vtkMaxActor actor = new vtkMaxActor(data, values.Length);
            if (data.Geometry.ExtremeNodes != null)
            {
                NodesExchangeData extremes = data.Geometry.ExtremeNodes;
                actor.MinNode = new vtkMaxExtreemeNode(extremes.Ids[0], extremes.Coor[0], extremes.Values[0]);
                actor.MaxNode = new vtkMaxExtreemeNode(extremes.Ids[1], extremes.Coor[1], extremes.Values[1]);
            }
            AddActorGeometry(actor, data.Layer);
            AddNativeGlyphActor(actor, data, coordinates, values, numArrows, _numPointsPerLineArrow, numNodalPoints);
        }
        public bool AddAnimatedVectorFieldActor(vtkMaxActorData data, double maxMagnitude, double maxArrowLength, double arrowSize)
        {
            return AddAnimatedGlyphFieldActor(data, maxMagnitude, maxArrowLength, arrowSize, false);
        }
        public bool AddAnimatedTensorFieldActor(vtkMaxActorData data, double maxAbsValue, double maxGlyphLength, double arrowSize)
        {
            return AddAnimatedGlyphFieldActor(data, maxAbsValue, maxGlyphLength, arrowSize, true);
        }
        private bool AddAnimatedGlyphFieldActor(vtkMaxActorData data, double maxValue, double maxLength,
                                                double arrowSize, bool tensorGlyph)
        {
            if (data?.Geometry?.NodesAnimation == null || data.Geometry.NodesAnimation.Length == 0 ||
                maxLength <= 0 || arrowSize <= 0) return true;
            //
            NodesExchangeData[] sourceFrames = data.Geometry.NodesAnimation;
            int numSourceNodes = sourceFrames[0]?.Coor?.Length ?? 0;
            int numArrowsPerNode = tensorGlyph ? 2 : 1;
            NodesExchangeData[] glyphFrames = new NodesExchangeData[sourceFrames.Length];
            double normalizationValue = maxValue > 0 ? maxValue : 1;
            for (int i = 0; i < sourceFrames.Length; i++)
            {
                NodesExchangeData sourceFrame = sourceFrames[i] ?? data.Geometry.Nodes;
                if ((sourceFrame?.Coor?.Length ?? 0) != numSourceNodes)
                    throw new CaeException("The animated glyph node count is not constant.");
                //
                glyphFrames[i] = CreateAnimatedGlyphNodes(sourceFrame, normalizationValue, maxLength, arrowSize, tensorGlyph);
            }
            //
            data.Geometry.Nodes = glyphFrames[0];
            data.Geometry.NodesAnimation = glyphFrames;
            //
            vtkMaxActor actor = new vtkMaxActor(data, glyphFrames[0].Coor.Length);
            AddNativeGlyphActor(actor, data, GetFloatCoordinates(glyphFrames[0]), glyphFrames[0].Values,
                                numArrowsPerNode * numSourceNodes, _numPointsPerLineArrow, numSourceNodes);
            //
            return InitializeAndAddAnimatedActor(actor, data, false);
        }
        private static NodesExchangeData CreateAnimatedGlyphNodes(NodesExchangeData sourceNodes, double maxValue,
                                                                  double maxLength, double arrowSize, bool tensorGlyph)
        {
            int numSourceNodes = sourceNodes?.Coor?.Length ?? 0;
            int numArrowsPerNode = tensorGlyph ? 2 : 1;
            int numArrowPoints = _numPointsPerLineArrow * numArrowsPerNode * numSourceNodes;
            int numPoints = numArrowPoints + numSourceNodes;
            NodesExchangeData glyphNodes = new NodesExchangeData();
            glyphNodes.Coor = new double[numPoints][];
            glyphNodes.Values = new float[numPoints];
            //
            double tolerance = maxValue * 1E-10;
            double headLength = (tensorGlyph ? 0.125 : 0.25) * arrowSize;
            double headHalfWidth = (tensorGlyph ? 0.03 : 0.06) * arrowSize;
            for (int i = 0; i < numSourceNodes; i++)
            {
                double[] origin = sourceNodes.Coor[i] ?? new double[3];
                double[] direction = sourceNodes.Normals != null && i < sourceNodes.Normals.Length
                    ? sourceNodes.Normals[i] : null;
                float scalarValue = sourceNodes.Values != null && i < sourceNodes.Values.Length
                    ? sourceNodes.Values[i] : float.NaN;
                int nodalPointId = numArrowPoints + i;
                glyphNodes.Coor[nodalPointId] = new double[] { origin[0], origin[1], origin[2] };
                glyphNodes.Values[nodalPointId] = scalarValue;
                //
                double directionLength = direction != null && direction.Length == 3
                    ? Math.Sqrt(direction[0] * direction[0] + direction[1] * direction[1] +
                                direction[2] * direction[2])
                    : double.NaN;
                bool valid = !float.IsNaN(scalarValue) && !float.IsInfinity(scalarValue) &&
                             !double.IsNaN(directionLength) && !double.IsInfinity(directionLength) &&
                             directionLength > (tensorGlyph ? 0 : tolerance);
                if (tensorGlyph) valid &= Math.Abs(scalarValue) > tolerance;
                //
                if (!valid)
                {
                    for (int j = 0; j < numArrowsPerNode; j++)
                        SetDegenerateLineArrow(glyphNodes.Coor, glyphNodes.Values,
                                               numArrowsPerNode * i + j, scalarValue, origin);
                    continue;
                }
                //
                double ux = direction[0] / directionLength;
                double uy = direction[1] / directionLength;
                double uz = direction[2] / directionLength;
                if (tensorGlyph)
                {
                    double halfShaftLength = 0.5 * maxLength * Math.Abs(scalarValue) / maxValue;
                    int arrowId = numArrowsPerNode * i;
                    if (scalarValue > 0)
                    {
                        SetLineArrow(glyphNodes.Coor, glyphNodes.Values, arrowId, scalarValue,
                                     origin[0], origin[1], origin[2],
                                     origin[0] - halfShaftLength * ux,
                                     origin[1] - halfShaftLength * uy,
                                     origin[2] - halfShaftLength * uz,
                                     headLength, headHalfWidth);
                        SetLineArrow(glyphNodes.Coor, glyphNodes.Values, arrowId + 1, scalarValue,
                                     origin[0], origin[1], origin[2],
                                     origin[0] + halfShaftLength * ux,
                                     origin[1] + halfShaftLength * uy,
                                     origin[2] + halfShaftLength * uz,
                                     headLength, headHalfWidth);
                    }
                    else
                    {
                        double tailDistance = headLength + halfShaftLength;
                        SetLineArrow(glyphNodes.Coor, glyphNodes.Values, arrowId, scalarValue,
                                     origin[0] - tailDistance * ux,
                                     origin[1] - tailDistance * uy,
                                     origin[2] - tailDistance * uz,
                                     origin[0] - headLength * ux,
                                     origin[1] - headLength * uy,
                                     origin[2] - headLength * uz,
                                     headLength, headHalfWidth);
                        SetLineArrow(glyphNodes.Coor, glyphNodes.Values, arrowId + 1, scalarValue,
                                     origin[0] + tailDistance * ux,
                                     origin[1] + tailDistance * uy,
                                     origin[2] + tailDistance * uz,
                                     origin[0] + headLength * ux,
                                     origin[1] + headLength * uy,
                                     origin[2] + headLength * uz,
                                     headLength, headHalfWidth);
                    }
                }
                else
                {
                    double shaftLength = maxLength * directionLength / maxValue;
                    SetLineArrow(glyphNodes.Coor, glyphNodes.Values, i, scalarValue,
                                 origin[0], origin[1], origin[2],
                                 origin[0] + shaftLength * ux,
                                 origin[1] + shaftLength * uy,
                                 origin[2] + shaftLength * uz,
                                 headLength, headHalfWidth);
                }
            }
            return glyphNodes;
        }
        private static void SetDegenerateLineArrow(double[][] coordinates, float[] values, int arrowId,
                                                   float scalarValue, double[] origin)
        {
            int pointId = _numPointsPerLineArrow * arrowId;
            for (int i = 0; i < _numPointsPerLineArrow; i++)
            {
                coordinates[pointId + i] = new double[] { origin[0], origin[1], origin[2] };
                values[pointId + i] = scalarValue;
            }
        }
        private static void SetLineArrow(double[][] coordinates, float[] values, int arrowId, float scalarValue,
                                         double startX, double startY, double startZ,
                                         double headBaseX, double headBaseY, double headBaseZ,
                                         double headLength, double headHalfWidth)
        {
            double ux = headBaseX - startX;
            double uy = headBaseY - startY;
            double uz = headBaseZ - startZ;
            double shaftLength = Math.Sqrt(ux * ux + uy * uy + uz * uz);
            ux /= shaftLength;
            uy /= shaftLength;
            uz /= shaftLength;
            //
            double rx = 0;
            double ry = Math.Abs(uz) < 0.9 ? 0 : 1;
            double rz = Math.Abs(uz) < 0.9 ? 1 : 0;
            double px = uy * rz - uz * ry;
            double py = uz * rx - ux * rz;
            double pz = ux * ry - uy * rx;
            double perpendicularLength = Math.Sqrt(px * px + py * py + pz * pz);
            px /= perpendicularLength;
            py /= perpendicularLength;
            pz /= perpendicularLength;
            //
            double tipX = headBaseX + headLength * ux;
            double tipY = headBaseY + headLength * uy;
            double tipZ = headBaseZ + headLength * uz;
            int pointId = _numPointsPerLineArrow * arrowId;
            coordinates[pointId] = new double[] { startX, startY, startZ };
            coordinates[pointId + 1] = new double[] { headBaseX, headBaseY, headBaseZ };
            coordinates[pointId + 2] = new double[] { tipX, tipY, tipZ };
            coordinates[pointId + 3] = new double[] { headBaseX + headHalfWidth * px,
                                                      headBaseY + headHalfWidth * py,
                                                      headBaseZ + headHalfWidth * pz };
            coordinates[pointId + 4] = new double[] { headBaseX - headHalfWidth * px,
                                                      headBaseY - headHalfWidth * py,
                                                      headBaseZ - headHalfWidth * pz };
            for (int i = 0; i < _numPointsPerLineArrow; i++) values[pointId + i] = scalarValue;
        }
        private void AddEmptyScalarFieldActor(vtkMaxActorData data)
        {
            // A field with no non-zero glyphs still has a valid scalar range. Register an
            // actor without renderable cells so the scalar bar and extreme-value widgets
            // remain available while zero-length arrows are omitted.
            float[] coordinates = new float[0];
            float[] values = new float[0];
            if (data.Geometry.ExtremeNodes != null)
            {
                NodesExchangeData extremes = data.Geometry.ExtremeNodes;
                coordinates = new float[6];
                values = new float[2];
                for (int i = 0; i < 2; i++)
                {
                    coordinates[3 * i] = (float)extremes.Coor[i][0];
                    coordinates[3 * i + 1] = (float)extremes.Coor[i][1];
                    coordinates[3 * i + 2] = (float)extremes.Coor[i][2];
                    values[i] = extremes.Values[i];
                }
            }
            AddPlotActor(data, coordinates, values, 0, 0);
        }
        private static void AddLineArrow(float[] coordinates, float[] values, int arrowId,
                                         float scalarValue, double startX, double startY, double startZ,
                                         double headBaseX, double headBaseY, double headBaseZ, double headLength,
                                         double headHalfWidth)
        {
            double ux = headBaseX - startX;
            double uy = headBaseY - startY;
            double uz = headBaseZ - startZ;
            double shaftLength = Math.Sqrt(ux * ux + uy * uy + uz * uz);
            ux /= shaftLength;
            uy /= shaftLength;
            uz /= shaftLength;
            //
            double rx;
            double ry;
            double rz;
            if (Math.Abs(uz) < 0.9)
            {
                rx = 0;
                ry = 0;
                rz = 1;
            }
            else
            {
                rx = 0;
                ry = 1;
                rz = 0;
            }
            double px = uy * rz - uz * ry;
            double py = uz * rx - ux * rz;
            double pz = ux * ry - uy * rx;
            double perpendicularLength = Math.Sqrt(px * px + py * py + pz * pz);
            px /= perpendicularLength;
            py /= perpendicularLength;
            pz /= perpendicularLength;
            //
            double tipX = headBaseX + headLength * ux;
            double tipY = headBaseY + headLength * uy;
            double tipZ = headBaseZ + headLength * uz;
            int pointId = _numPointsPerLineArrow * arrowId;
            int coordinateId = 3 * pointId;
            coordinates[coordinateId] = (float)startX;
            coordinates[coordinateId + 1] = (float)startY;
            coordinates[coordinateId + 2] = (float)startZ;
            coordinates[coordinateId + 3] = (float)headBaseX;
            coordinates[coordinateId + 4] = (float)headBaseY;
            coordinates[coordinateId + 5] = (float)headBaseZ;
            coordinates[coordinateId + 6] = (float)tipX;
            coordinates[coordinateId + 7] = (float)tipY;
            coordinates[coordinateId + 8] = (float)tipZ;
            coordinates[coordinateId + 9] = (float)(headBaseX + headHalfWidth * px);
            coordinates[coordinateId + 10] = (float)(headBaseY + headHalfWidth * py);
            coordinates[coordinateId + 11] = (float)(headBaseZ + headHalfWidth * pz);
            coordinates[coordinateId + 12] = (float)(headBaseX - headHalfWidth * px);
            coordinates[coordinateId + 13] = (float)(headBaseY - headHalfWidth * py);
            coordinates[coordinateId + 14] = (float)(headBaseZ - headHalfWidth * pz);
            for (int i = 0; i < _numPointsPerLineArrow; i++) values[pointId + i] = scalarValue;
        }
        //
        public void AddScalarFieldOnCells(vtkMaxActorData data, bool update)
        {
            vtkMaxActor actor = new vtkMaxActor(data);
            AddActorGeometry(actor, vtkRendererLayer.Base);
            AddNativeActor(actor);
            //
            if (update) UpdateScalarsAndCameraAndRedraw();
        }
        //
        public bool AddAnimatedScalarFieldOnCells(vtkMaxActorData data)
        {
            return AddAnimatedScalarFieldOnCellsPersistentActor(data);
        }
        public bool AddAnimatedScalarFieldOnCellsAddRemoveActors(vtkMaxActorData data)
        {
            return AddAnimatedScalarFieldOnCellsPersistentActor(data);
        }
        public bool AddAnimatedScalarFieldOnCellsAllActors(vtkMaxActorData data)
        {
            return AddAnimatedScalarFieldOnCellsPersistentActor(data);
        }
        private bool AddAnimatedScalarFieldOnCellsPersistentActor(vtkMaxActorData data)
        {
            vtkMaxActor actor = new vtkMaxActor(data);
            return InitializeAndAddAnimatedActor(actor, data, true);
        }
        private bool InitializeAndAddAnimatedActor(vtkMaxActor actor, vtkMaxActorData data, bool registerNodeIds)
        {
            AddNativeAnimatedActor(data, actor);
            actor.InitializeAnimation(data);
            actor.RecomputeMemorySize();
            _animationFrameData.MemMb += actor.MemorySize / 1024.0;
            //
            AddActorGeometry(actor, data.Layer);
            _animationActors.Add(actor.Name, actor);
            _currentAnimationFrameNumber = 0;
            int[] animationNodeIds = registerNodeIds
                ? data.PartNodeIds ?? data.Geometry.Nodes.Ids
                : null;
            if (animationNodeIds != null)
            {
                foreach (int nodeId in animationNodeIds) _animationNodeActors[nodeId] = actor;
            }
            //
            AdjustCameraDistanceAndClippingRedraw();
            return true;
        }
        //
        private void UpdateScalarFormatting()
        {
            UpdateNativeScalarFormatting();
            vtkMaxActor actor;
            // Legend
            SetScalarBarWidgetVisibility(false);
            //
            vtkMaxTextWithArrowWidget minValueWidget;
            vtkMaxTextWithArrowWidget maxValueWidget;
            _arrowWidgets.TryGetValue(Globals.MinAnnotationName, out minValueWidget);
            _arrowWidgets.TryGetValue(Globals.MaxAnnotationName, out maxValueWidget);
            //
            if (minValueWidget != null && minValueWidget.GetVisibility() == 0) minValueWidget = null;
            if (maxValueWidget != null && maxValueWidget.GetVisibility() == 0) maxValueWidget = null;
            //
            if (minValueWidget != null) minValueWidget.VisibilityOff();
            if (maxValueWidget != null) maxValueWidget.VisibilityOff();
            //
            vtkMaxExtreemeNode minNode = null;
            vtkMaxExtreemeNode maxNode = null;
            vtkMaxExtreemeNode fallbackMinNode = null;
            vtkMaxExtreemeNode fallbackMaxNode = null;
            // Find min and max value on actors
            foreach (var entry in _actors)
            {
                actor = entry.Value;
                if (entry.Value == null) throw new ArgumentNullException("_actors.Actor", "The actor does not exist.");
                // if the part does not have scalar data
                if (actor.MinNode == null || actor.MaxNode == null) continue;
                // Keep a result-wide fallback for an active plot whose representation is disabled on every part.
                if (!float.IsNaN(actor.MinNode.Value) &&
                    (fallbackMinNode == null || actor.MinNode.Value < fallbackMinNode.Value))
                    fallbackMinNode = actor.MinNode;
                if (!float.IsNaN(actor.MaxNode.Value) &&
                    (fallbackMaxNode == null || actor.MaxNode.Value > fallbackMaxNode.Value))
                    fallbackMaxNode = actor.MaxNode;
                //
                if (!actor.VtkMaxActorVisible || !actor.ColorContours) continue;
                //
                if (minNode == null || maxNode == null)     // the first time through
                {
                    minNode = actor.MinNode;
                    maxNode = actor.MaxNode;
                }
                else
                {
                    if (actor.MinNode != null && !float.IsNaN(actor.MinNode.Value) && actor.MinNode.Value < minNode.Value)
                        minNode = actor.MinNode;
                    if (actor.MaxNode != null && !float.IsNaN(actor.MaxNode.Value) && actor.MaxNode.Value > maxNode.Value)
                        maxNode = actor.MaxNode;
                }
            }
            bool showMinMaxWidgets = minNode != null && maxNode != null;
            if (!showMinMaxWidgets)
            {
                if (!_keepScalarBarVisible)
                    return;
                else if (fallbackMinNode == null || fallbackMaxNode == null)
                {
                    SetScalarBarWidgetVisibility(true);
                    return;
                }
                //
                minNode = fallbackMinNode;
                maxNode = fallbackMaxNode;
            }
            // A uniform zero initial frame has no unique extrema and normally selects the first node.
            // Keep its zero values, but use the material nodes that become the first meaningful extrema.
            if (minNode.Value == 0 && maxNode.Value == 0 &&
                TryGetStableInitialAnimationExtremeNodes(out vtkMaxExtreemeNode stableMinNode,
                                                         out vtkMaxExtreemeNode stableMaxNode))
            {
                minNode = stableMinNode;
                maxNode = stableMaxNode;
            }
            // Scalar bar and min/max values of actor scalar range
            if (_colorSpectrum.MinMaxType == vtkColorSpectrumMinMaxType.Automatic)
            {
                if (_animationFrameData != null && _animationFrameData.UseAllFrameData)   // animation from all frames
                {
                    double[] animationRange = _animationFrameData.AllFramesScalarRange;
                    CreateScalarBarLookupTable(animationRange[0], animationRange[1], double.NaN, double.NaN);
                    _scalarRange = new double[] { animationRange[0], animationRange[1] };
                }
                else // min max from current frame
                {
                    CreateScalarBarLookupTable(minNode.Value, maxNode.Value, double.NaN, double.NaN);
                    _scalarRange = new double[] { minNode.Value, maxNode.Value };
                }
            }
            else  // Manual and min max from current frame
            {
                CreateScalarBarLookupTable(minNode.Value, maxNode.Value, _colorSpectrum.MinUserValue,
                                           _colorSpectrum.MaxUserValue);
                _scalarRange = new double[] { minNode.Value, maxNode.Value };
            }
            // Scalar bar
            SetScalarBarWidgetVisibility(true);
            if (!showMinMaxWidgets) return;
            // Min Max widgets
            string format;
            //
            double[] coor;
            bool minVisible = minValueWidget != null;
            bool maxVisible = maxValueWidget != null;
            //
            if (minVisible)
            {
                format = minValueWidget.GetNumberFormat();
                coor = minNode.Coor;
                if (coor != null)
                {
                    minValueWidget.VisibilityOn();
                    minValueWidget.SetText("Min: " + minNode.Value.ToString(format) + GetUnitAbbreviation() + Environment.NewLine +
                                           "Node id: " + minNode.Id);
                    minValueWidget.SetAnchorPoint(coor[0], coor[1], coor[2]);
                }
                else minValueWidget.VisibilityOff();
            }
            //
            if (maxVisible)
            {
                format = maxValueWidget.GetNumberFormat();
                coor = maxNode.Coor;
                if (coor != null)
                {
                    maxValueWidget.VisibilityOn();
                    maxValueWidget.SetText("Max: " + maxNode.Value.ToString(format) + GetUnitAbbreviation() + Environment.NewLine +
                                           "Node id: " + maxNode.Id);
                    maxValueWidget.SetAnchorPoint(coor[0], coor[1], coor[2]);
                }
                else maxValueWidget.VisibilityOff();
            }
        }
        private bool TryGetStableInitialAnimationExtremeNodes(out vtkMaxExtreemeNode minNode,
                                                               out vtkMaxExtreemeNode maxNode)
        {
            minNode = null;
            maxNode = null;
            if (_currentAnimationFrameNumber != 0 || _animationActors.Count == 0) return false;
            //
            int frameCount = int.MaxValue;
            foreach (vtkMaxActor actor in _animationActors.Values)
            {
                int actorFrameCount = actor.GetAnimationFrameCount();
                if (actorFrameCount > 0 && actorFrameCount < frameCount) frameCount = actorFrameCount;
            }
            if (frameCount == int.MaxValue || frameCount < 2) return false;
            //
            for (int frameNumber = 1; frameNumber < frameCount; frameNumber++)
            {
                if (!TryGetAnimationFrameExtremeNodes(frameNumber, out vtkMaxActor minActor,
                                                      out vtkMaxExtreemeNode nextMinNode,
                                                      out vtkMaxActor maxActor,
                                                      out vtkMaxExtreemeNode nextMaxNode))
                    continue;
                if (nextMinNode.Value == 0 && nextMaxNode.Value == 0) continue;
                //
                if (!minActor.TryGetAnimationNodeCoordinates(0, nextMinNode.Id, out double[] minCoordinates))
                    minCoordinates = nextMinNode.Coor?.ToArray();
                if (!maxActor.TryGetAnimationNodeCoordinates(0, nextMaxNode.Id, out double[] maxCoordinates))
                    maxCoordinates = nextMaxNode.Coor?.ToArray();
                //
                minNode = new vtkMaxExtreemeNode(nextMinNode.Id, minCoordinates, 0);
                maxNode = new vtkMaxExtreemeNode(nextMaxNode.Id, maxCoordinates, 0);
                return true;
            }
            return false;
        }
        private bool TryGetAnimationFrameExtremeNodes(int frameNumber, out vtkMaxActor minActor,
                                                       out vtkMaxExtreemeNode minNode,
                                                       out vtkMaxActor maxActor,
                                                       out vtkMaxExtreemeNode maxNode)
        {
            minActor = null;
            minNode = null;
            maxActor = null;
            maxNode = null;
            //
            foreach (vtkMaxActor actor in _animationActors.Values)
            {
                if (!actor.VtkMaxActorVisible || !actor.ColorContours ||
                    !actor.TryGetAnimationExtremeNodes(frameNumber, out vtkMaxExtreemeNode actorMinNode,
                                                       out vtkMaxExtreemeNode actorMaxNode))
                    continue;
                //
                if (!float.IsNaN(actorMinNode.Value) && (minNode == null || actorMinNode.Value < minNode.Value))
                {
                    minActor = actor;
                    minNode = actorMinNode;
                }
                if (!float.IsNaN(actorMaxNode.Value) && (maxNode == null || actorMaxNode.Value > maxNode.Value))
                {
                    maxActor = actor;
                    maxNode = actorMaxNode;
                }
            }
            return minNode != null && maxNode != null;
        }
        public void UpdateActorScalarField(string actorName, float[] values, NodesExchangeData extremeNodes,
                                           ResultFieldExchangeData resultField, bool update)
        {
            if (values != null) UpdateActorScalarField(actorName, values, extremeNodes, resultField);
            else RemoveActorScalarField(actorName);
            //
            vtkMaxActor actor;
            if (_actors.TryGetValue(actorName, out actor)) SetNativeActorScalarField(actor, values, extremeNodes);
            //
            if (update) UpdateScalarsAndRedraw();
        }
        private void UpdateActorScalarField(string actorName, float[] values, NodesExchangeData extremeNodes,
                                            ResultFieldExchangeData resultField)
        {
            vtkMaxActor actor = _actors[actorName];
            // Transformed copies
            foreach (var copy in actor.Copies) UpdateActorScalarField(copy.Name, values, extremeNodes, resultField);
            // Set scalars
            actor.SetValues(values, extremeNodes);
            // Refresh the complete directional field and reconstruct this actor's selected component
            actor.UpdateResultFields(resultField);
        }
        private void RemoveActorScalarField(string actorName)
        {
            if (_actors.TryGetValue(actorName, out vtkMaxActor actor))
            {
                // Transformed copies
                foreach (var copy in actor.Copies) RemoveActorScalarField(copy.Name);
                // Remove scalars
                actor.SetValues(null, null);
                actor.UpdateResultFields(null);
            }
        }
        public void UpdateActorsColorContoursVisibility(string[] actorNames, bool colorContours)
        {
            foreach (var name in actorNames) UpdateActorColorContoursVisibility(name, colorContours);
            RunNative("UpdateActorsColorContoursVisibility",
                      () => _nativeMirror.SetActorsColorContours(actorNames, colorContours));
            //
            UpdateScalarFormatting();
            //
            RenderScene();
        }
        private void UpdateActorColorContoursVisibility(string actorName, bool colorContours)
        {
            vtkMaxActor actor = _actors[actorName];
            actor.ColorContours = colorContours;
            // Transformed copies
            foreach (var copy in actor.Copies) UpdateActorColorContoursVisibility(copy.Name, colorContours);
        }


        #endregion #################################################################################################################

        #region Animation ##########################################################################################################
        public float GetAnimationScaleFactor(int frameNumber)
        {
            if (_animationFrameData != null && _animationFrameData.ScaleFactor != null &&
                _animationFrameData.ScaleFactor.Length > frameNumber) return _animationFrameData.ScaleFactor[frameNumber];
            else return float.NaN;
        }
        public void SetAnimationFrameData(float[] time, int[] stepId, int[] stepIncrementId, int[] modeNumber,
                                          int[] nodalDiameter, float[] scale, double[] allFramesScalarRange, vtkMaxAnimationType animationType)
        {
            _animationFrameData.Time = time;
            _animationFrameData.StepId = stepId;
            _animationFrameData.StepIncrementId = stepIncrementId;
            _animationFrameData.ModeNumber = modeNumber;
            _animationFrameData.NodalDiameter = nodalDiameter;
            _animationFrameData.ScaleFactor = scale;
            _animationFrameData.AllFramesScalarRange = allFramesScalarRange;
            _animationFrameData.AnimationType = animationType;
            UpdateTransformedAnimationScalarRange();
        }
        private void UpdateTransformedAnimationScalarRange()
        {
            if (_animationFrameData?.AllFramesScalarRange == null ||
                _animationFrameData.AllFramesScalarRange.Length != 2 || _transforms.Count == 0) return;
            //
            List<double[]> transformations = GetResultFieldTransformations();
            double minimum = _animationFrameData.AllFramesScalarRange[0];
            double maximum = _animationFrameData.AllFramesScalarRange[1];
            bool found = false;
            foreach (vtkMaxActor actor in _animationActors.Values)
            {
                if (!actor.VtkMaxActorVisible || !actor.ColorContours) continue;
                if (actor.TryGetAnimationResultRange(transformations, out double actorMinimum,
                                                      out double actorMaximum))
                {
                    if (actorMinimum < minimum) minimum = actorMinimum;
                    if (actorMaximum > maximum) maximum = actorMaximum;
                    found = true;
                }
            }
            if (found) _animationFrameData.AllFramesScalarRange = new double[] { minimum, maximum };
        }
        private List<double[]> GetResultFieldTransformations()
        {
            List<double[]> transformations = new List<double[]>();
            transformations.Add(ResultFieldExchangeData.IdentityTransformation());
            foreach (var transformEntry in _transforms)
            {
                double[] incremental = vtkMaxActor.GetResultFieldTransformation(transformEntry.Item1);
                double[][] existing = transformations.ToArray();
                foreach (double[] existingTransformation in existing)
                {
                    double[] current = existingTransformation;
                    for (int i = 1; i < transformEntry.Item2; i++)
                    {
                        current = ResultFieldExchangeData.MultiplyTransformations(incremental, current);
                        transformations.Add(current);
                    }
                }
            }
            return transformations;
        }
        //
        public void SetAnimationFrame(int frameNumber, bool scalarRangeFromAllFrames)
        {
            _animationFrameData.UseAllFrameData = scalarRangeFromAllFrames;
            //
            if (_statusBlockWidget != null && _animationFrameData != null)
            {
                _statusBlockWidget.StepNumber = _animationFrameData.StepId[frameNumber];
                if (_animationFrameData.ModeNumber != null && _animationFrameData.ModeNumber[frameNumber] >= 0)
                    _statusBlockWidget.IncrementNumber = _animationFrameData.ModeNumber[frameNumber];
                else
                    _statusBlockWidget.IncrementNumber = _animationFrameData.StepIncrementId[frameNumber];
                _statusBlockWidget.NodalDiameter = _animationFrameData.NodalDiameter?[frameNumber] ?? -1;
                _statusBlockWidget.AnalysisTime = _animationFrameData.Time[frameNumber];
                _statusBlockWidget.AnimationScaleFactor = _animationFrameData.ScaleFactor[frameNumber];
                _statusBlockWidget.AnimationType = _animationFrameData.AnimationType;
            }
            //
            bool applySectionView = false;
            SectionViewData sectionViewData = null;
            if (_sectionViewData.Active)
            {
                sectionViewData = _sectionViewData.DeepCopy();
                applySectionView = true;
                RemoveSectionView();
            }
            //
            bool applyTransforms = _transforms.Count > 0;
            if (applyTransforms) RemoveTransformedActors();
            //
            foreach (vtkMaxActor actor in _animationActors.Values) actor.SetAnimationFrame(frameNumber);
            RunNative("SetAnimationFrame", () => _nativeMirror.SetAnimationFrame(frameNumber));
            _currentAnimationFrameNumber = frameNumber;
            //
            if (applyTransforms) ApplyTransforms();
            //
            if (applySectionView) CreateSectionView(sectionViewData.Point, sectionViewData.Normal,
                                                   sectionViewData.LightenColors, sectionViewData.SectionColor,
                                                   sectionViewData.ElementWise, sectionViewData.Hatch,
                                                   sectionViewData.HatchSpacing);
            //
            UpdateScalarFormatting();
            //
            bool initializeArrowAnchors = _animationArrowAnchorRequests.Count > 0;
            Controller_AnimationFrameChanged?.Invoke(frameNumber, initializeArrowAnchors);
            UpdateAnimatedArrowWidgetAnchors();
            //
            UpdateFollowerView();
            RenderScene();
        }
        public bool TryGetAnimationNodeData(int nodeId, out double[] coordinates, out float value)
        {
            if (_animationNodeActors.TryGetValue(nodeId, out vtkMaxActor actor))
                return actor.TryGetAnimationNodeData(nodeId, out coordinates, out value);
            coordinates = null;
            value = float.NaN;
            return false;
        }
        //
        public void SaveAnimationAsAVI(string fileName, int[] firstLastFrame, int step, int fps, bool scalarRangeFromAllFrames,
                                       bool swing, bool encoderOptions)
        {
            if (step < 1) step = 1;
            //
            if (System.IO.File.Exists(fileName))
            {
                if (CaeGlobals.Tools.IsFileLocked(fileName))
                {
                    MessageBoxes.ShowWarning("The selected file cannot be replaced.");
                    return;
                }
            }
            //
            if (_nativeMirror == null) return;
            SaveNativeAnimationAsAVI(fileName, firstLastFrame, step, fps, scalarRangeFromAllFrames, swing, encoderOptions);
        }
        public void SaveAnimationAsImages(string fileName, int[] firstLastFrame, int step, bool scalarRangeFromAllFrames, bool swing)
        {
            if (step < 1) step = 1;

            int count = 0;
            for (int i = firstLastFrame[0]; i <= firstLastFrame[1]; i += step) count++;
            if (swing) for (int i = firstLastFrame[1]; i >= firstLastFrame[0]; i -= step) count++;

            // create file names
            string[] fileNames = new string[count];
            int numOfPlaces = (int)Math.Log10(count) + 1;
            string path = System.IO.Path.GetDirectoryName(fileName);
            fileName = System.IO.Path.GetFileName(fileName);

            for (int i = 0; i < fileNames.Length; i++)
            {
                fileNames[i] = System.IO.Path.Combine(path, (i + 1).ToString().PadLeft(numOfPlaces, '0') + "_" + fileName);

                if (System.IO.File.Exists(fileNames[i]))
                {
                    if (CaeGlobals.Tools.IsFileLocked(fileNames[i]))
                    {
                        MessageBoxes.ShowError("The file '" + fileNames[i] + "' cannot be replaced.");
                        return;
                    }
                }
            }

            if (_nativeMirror == null) return;
            count = 0;
            for (int i = firstLastFrame[0]; i <= firstLastFrame[1]; i += step)
            {
                SetAnimationFrame(i, scalarRangeFromAllFrames);
                _nativeMirror.WritePng(fileNames[count++], 1);
            }
            if (swing)
            {
                for (int i = firstLastFrame[1]; i >= firstLastFrame[0]; i -= step)
                {
                    SetAnimationFrame(i, scalarRangeFromAllFrames);
                    _nativeMirror.WritePng(fileNames[count++], 1);
                }
            }
        }
        public void RenderToPNG(string fileName)
        {
            RenderToPNG(fileName, false, 1);
        }
        // The picture of the native view, magnified, with a transparent background when asked
        public void RenderToPNG(string fileName, bool transparentBackground, int magnification)
        {
            if (magnification < 1) magnification = 1;
            else if (magnification > MaxImageMagnification) magnification = MaxImageMagnification;
            //
            if (_nativeMirror != null) _nativeMirror.RenderToPng(fileName, transparentBackground, magnification);
        }

        #endregion #################################################################################################################

        #region Clear ##############################################################################################################
        public void Clear()
        {
            if (_sectionViewData.Active) RemoveSectionView();
            _transforms.Clear();
            //
            _actors.Clear();
            if (_nativeMirror != null)
            {
                _nativeMirror.ClearActors();
                _nativeMirrorNames.Clear();
            }
            _animationActors.Clear();
            _animationNodeActors.Clear();
            _currentAnimationFrameNumber = -1;
            _animationArrowAnchors.Clear();
            _animationArrowAnchorRequests.Clear();
            _animationFrameData = new vtkMaxAnimationFrameData();
            _keepScalarBarVisible = false;
            //
            SetScalarBarWidgetVisibility(false);
            if (_colorBarWidget != null) HideColorBar();
            if (_statusBlockWidget != null) _statusBlockWidget.VisibilityOff();
            // Widgets
            HideAllArrowWidgets();
            //
            ClearSelection();
            ClearOverlay();
            RemoveSectionView();
            ClearNative(VtkNativeClear.All);
        }
        public void ClearButKeepParts()
        {
            vtkMaxActor actor;
            List<string> actorsToRemove = new List<string>();
            //
            foreach (var entry in _actors)
            {
                actor = entry.Value;
                bool isPartChild = actor.ParentName != null &&
                                   _actors.TryGetValue(actor.ParentName, out vtkMaxActor parentActor) &&
                                   parentActor.IsAPart;
                if (!actor.IsAPart && !isPartChild)
                {
                    actorsToRemove.Add(actor.Name);
                    // A buffered actor may be added again, without a section view
                    actor.GeometryHiddenBySectionView = false;
                }
            }
            // Remove actors
            foreach (string name in actorsToRemove) _actors.Remove(name);
            //
            ClearSelection();
            ClearOverlay();
            ClearNative(VtkNativeClear.ButKeepParts);
        }
        public void ClearSelection(bool clearAllMouseSelection = true)
        {
            ClearCurrentMouseSelection(false);
            //
            _selectedActors.Clear();
            ClearNative(VtkNativeClear.Selection);
            //
            RenderScene();
        }
        public void ClearCurrentMouseSelection()
        {
            ClearCurrentMouseSelection(true);
        }
        public void ClearCurrentMouseSelection(bool render)
        {
            ClearNativeMouseSelection();
            //
            if (_probeWidget != null && _probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
            if (render) RenderScene();
        }
        public void ClearOverlay()
        {
            _maxSymbolSize = 0;
            _overlayActors.Clear();
            _measurementArrowPointCounts.Clear();
            _measurementArrowSymbolSizes.Clear();
            ClearNative(VtkNativeClear.Overlay);
            //
            RenderScene();
        }
        public void AddMeasurementAnnotation(string name, vtkMaxActorData nodeData, vtkMaxActorData lineData,
                                             double[][] arrowCoordinates, double[][] arrowNormals,
                                             double symbolSize, bool visible)
        {
            if (CanUpdateMeasurementAnnotation(name, nodeData.Geometry.Nodes.Coor, lineData.Geometry.Nodes.Coor,
                                               arrowCoordinates, arrowNormals, symbolSize))
            {
                UpdateMeasurementAnnotation(name, nodeData.Geometry.Nodes.Coor, lineData.Geometry.Nodes.Coor,
                                            arrowCoordinates, arrowNormals, visible);
                return;
            }
            //
            RemoveMeasurementAnnotation(name);

            nodeData.Name = GetMeasurementActorName(name, "nodes");
            nodeData.Layer = vtkRendererLayer.Overlay;
            vtkMaxActor nodeActor = AddPoints(nodeData);

            lineData.Name = GetMeasurementActorName(name, "lines");
            lineData.Layer = vtkRendererLayer.Overlay;
            vtkMaxActor lineActor = AddCells(lineData);

            nodeActor.VtkMaxActorVisible = visible;
            lineActor.VtkMaxActorVisible = visible;
            SetNativeActorVisible(nodeActor, visible);
            SetNativeActorVisible(lineActor, visible);

            if (arrowCoordinates != null && arrowNormals != null)
            {
                vtkMaxActor arrowActor = AddMeasurementArrowActor(name, arrowCoordinates, arrowNormals,
                                                                  symbolSize, lineData.Color);
                arrowActor.VtkMaxActorVisible = visible;
                SetNativeActorVisible(arrowActor, visible);
            }
        }
        private bool CanUpdateMeasurementAnnotation(string name, double[][] nodeCoordinates,
                                                    double[][] lineCoordinates, double[][] arrowCoordinates,
                                                    double[][] arrowNormals, double symbolSize)
        {
            if (!MeasurementActorHasPointCount(GetMeasurementActorName(name, "nodes"), nodeCoordinates) ||
                !MeasurementActorHasPointCount(GetMeasurementActorName(name, "lines"), lineCoordinates))
                return false;
            //
            string arrowActorName = GetMeasurementActorName(name, "arrows");
            bool hasArrowActor = _overlayActors.ContainsKey(arrowActorName);
            bool hasArrowData = arrowCoordinates != null && arrowNormals != null;
            if (!hasArrowData) return !hasArrowActor;
            //
            return hasArrowActor &&
                   _measurementArrowPointCounts.TryGetValue(name, out int arrowPointCount) &&
                   arrowCoordinates.Length == arrowPointCount &&
                   _measurementArrowSymbolSizes.TryGetValue(name, out double currentSymbolSize) &&
                   currentSymbolSize == symbolSize;
        }
        private bool MeasurementActorHasPointCount(string actorName, double[][] coordinates)
        {
            if (coordinates == null || !_overlayActors.TryGetValue(actorName, out vtkMaxActor actor)) return false;
            return actor.PointCount == coordinates.Length;
        }
        public void UpdateMeasurementAnnotation(string name, double[][] nodeCoordinates, double[][] lineCoordinates,
                                                double[][] arrowCoordinates, double[][] arrowNormals, bool visible)
        {
            UpdateMeasurementActor(GetMeasurementActorName(name, "nodes"), nodeCoordinates, visible);
            UpdateMeasurementActor(GetMeasurementActorName(name, "lines"), lineCoordinates, visible);

            string arrowActorName = GetMeasurementActorName(name, "arrows");
            if (_measurementArrowPointCounts.TryGetValue(name, out int arrowPointCount) &&
                arrowCoordinates != null && arrowNormals != null &&
                arrowCoordinates.Length == arrowPointCount)
            {
                if (_overlayActors.TryGetValue(arrowActorName, out vtkMaxActor arrowActor))
                {
                    arrowActor.VtkMaxActorVisible = visible;
                    SetNativeActorPoints(arrowActor, arrowCoordinates, arrowNormals);
                    SetNativeActorVisible(arrowActor, visible);
                }
            }
        }
        public void UpdateOverlayPointsActor(string actorName, double[][] coordinates)
        {
            if (_overlayActors.TryGetValue(actorName, out vtkMaxActor actor))
                UpdateMeasurementActor(actorName, coordinates, actor.VtkMaxActorVisible);
        }
        public void UpdateOverlayCaptionActorPosition(string actorName, double[] position)
        {
            if (_overlayActors.TryGetValue(actorName, out vtkMaxActor actor) && actor is vtkMaxCaptionActor captionActor)
            {
                captionActor.Position = position;
                SetNativeCaptionPosition(captionActor, position);
            }
        }
        public void RemoveMeasurementAnnotations(string[] names)
        {
            if (names == null) return;
            bool removed = false;
            foreach (string name in names) removed |= RemoveMeasurementAnnotation(name);
            if (removed) RenderScene();
        }
        private bool RemoveMeasurementAnnotation(string name)
        {
            bool removed = false;
            string[] actorNames = new string[]
            {
                GetMeasurementActorName(name, "nodes"),
                GetMeasurementActorName(name, "lines"),
                GetMeasurementActorName(name, "arrows")
            };
            foreach (string actorName in actorNames)
            {
                if (_overlayActors.TryGetValue(actorName, out vtkMaxActor actor))
                {
                    _nativeActors.Remove(actor);
                    _overlayActors.Remove(actorName);
                    removed = true;
                }
            }
            _measurementArrowPointCounts.Remove(name);
            _measurementArrowSymbolSizes.Remove(name);
            if (removed) RunNative("RemoveMeasurementAnnotation", () => _nativeMirror.RemoveOverlayActors(actorNames));
            return removed;
        }
        private void UpdateMeasurementActor(string actorName, double[][] coordinates, bool visible)
        {
            if (coordinates == null || !_overlayActors.TryGetValue(actorName, out vtkMaxActor actor)) return;
            if (actor.PointCount != coordinates.Length) return;

            actor.VtkMaxActorVisible = visible;
            SetNativeActorPoints(actor, coordinates, null);
            SetNativeActorVisible(actor, visible);
        }
        private vtkMaxActor AddMeasurementArrowActor(string name, double[][] coordinates, double[][] normals,
                                                     double symbolSize, Color color)
        {
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;

            vtkMaxActorData data = new vtkMaxActorData();
            data.Name = GetMeasurementActorName(name, "arrows");
            data.Color = color;
            data.Layer = vtkRendererLayer.Overlay;
            data.Pickable = false;
            vtkMaxActor actor = new vtkMaxActor(data, coordinates.Length);
            ApplySymbolFormattingToActor(actor);
            AddActorGeometry(actor, vtkRendererLayer.Overlay);
            AddNativeSymbol(actor, data, VtkNativeSymbol.Cone, symbolSize, coordinates, normals);

            _measurementArrowPointCounts[name] = coordinates.Length;
            _measurementArrowSymbolSizes[name] = symbolSize;
            return actor;
        }
        private static string GetMeasurementActorName(string annotationName, string suffix)
        {
            return "__measurement_annotation__" + annotationName + "__" + suffix;
        }
        public void RemoveOverlayActors(string[] actorNames)
        {
            if (actorNames == null) return;
            //
            bool removed = false;
            foreach (string actorName in actorNames)
            {
                if (_overlayActors.TryGetValue(actorName, out vtkMaxActor actor))
                {
                    _nativeActors.Remove(actor);
                    _overlayActors.Remove(actorName);
                    removed = true;
                }
            }
            RunNative("RemoveOverlayActors", () => _nativeMirror.RemoveOverlayActors(actorNames));
            if (removed) RenderScene();
        }
        #endregion #################################################################################################################

        #region Widgets ############################################################################################################
        public void SetWidgetInteractionEnabled(bool enabled)
        {
            _widgetInteractionEnabled = enabled;
            //
            MirrorWidgetHighlight();
            RenderScene();
        }
        public void SetWidgetHighlightLocked(bool locked)
        {
            _widgetHighlightLocked = locked;
        }
        public void SetWidgetHighlightEnabled(bool enabled)
        {
            _widgetHighlightEnabled = enabled;
            MirrorWidgetHighlight();
        }
        // The scale bar is centered with the width its native twin measures; before the twin exists it is centered
        // when the twin is made
        public void ResetScaleBarPosition()
        {
            _scaleBarPositionResetPending = GetNativeMirrorWidget(NativeScaleBarName) == null;
            _scaleWidget.SetHorizontallyRelativePosition((Width - _scaleWidget.GetWidth()) / 2, 0);
        }
        public void AddArrowWidget(string name, string text, string numberFormat, double[] anchorPoint,
                                   bool drawBackground, bool drawBorder, bool visible)
        {
            AddArrowWidget(name, text, numberFormat, anchorPoint, drawBackground, drawBorder, visible, null);
        }
        public void AddArrowWidget(string name, string text, string numberFormat, double[] anchorPoint,
                                   bool drawBackground, bool drawBorder, bool visible, string[] animationActorNames)
        {
            vtkMaxTextWithArrowWidget arrowWidget;
            bool newWidget = false;
            bool wasVisible = false;
            // Create annotation
            if (!_arrowWidgets.TryGetValue(name, out arrowWidget))
            {
                newWidget = true;
                arrowWidget = new vtkMaxTextWithArrowWidget(name);
                arrowWidget.SetBorderColor(0, 0, 0);
                arrowWidget.SetTextStyle(VtkNativeTextStyle.CreateWidgetStyle());
                arrowWidget.SetPadding(5);
                arrowWidget.SetBackgroundColor(1, 1, 1);
                // Set number format before the widget contents are updated
                arrowWidget.SetNumberFormat(numberFormat);
                // Add
                _arrowWidgets.Add(name, arrowWidget);
                MirrorArrowWidget(name, arrowWidget);
            }
            // Edit annotation
            else
            {
                wasVisible = arrowWidget.GetVisibility() == 1;
                arrowWidget.SetNumberFormat(numberFormat);
            }
            //
            if (visible) arrowWidget.VisibilityOn();
            else arrowWidget.VisibilityOff();
            //
            if (drawBackground) arrowWidget.BackgroundVisibilityOn();
            else arrowWidget.BackgroundVisibilityOff();
            if (drawBorder) arrowWidget.BorderVisibilityOn();
            else arrowWidget.BorderVisibilityOff();
            // Update the contents and visibility of the min/max annotations - due to visibility must be here
            if (visible && (name == Globals.MinAnnotationName || name == Globals.MaxAnnotationName)) UpdateScalarFormatting();
            else
            {
                arrowWidget.SetText(text);
                arrowWidget.SetAnchorPoint(anchorPoint[0], anchorPoint[1], anchorPoint[2]);
                SetAnimationArrowAnchor(name, anchorPoint, animationActorNames);
            }
            // The final text size, visibility and anchor must be known before placement. Existing widgets are fixed.
            if (arrowWidget.GetVisibility() == 1 && (newWidget || !wasVisible)) PositionNewArrowWidget(arrowWidget);
            //
            RenderScene();
        }
        public void UpdateArrowWidgetText(string name, string text)
        {
            if (_arrowWidgets.TryGetValue(name, out vtkMaxTextWithArrowWidget arrowWidget))
                arrowWidget.SetText(text);
        }
        public void UpdateArrowWidgetAnimationData(string name, string text, double[] anchorPoint)
        {
            if (text != null) UpdateArrowWidgetText(name, text);
            if (anchorPoint != null)
            {
                if (_animationArrowAnchorRequests.TryGetValue(name, out Tuple<double[], string[]> request))
                    _animationArrowAnchorRequests[name] =
                        new Tuple<double[], string[]>(anchorPoint.ToArray(), request.Item2);
                else if (!_animationArrowAnchors.ContainsKey(name) &&
                         _arrowWidgets.TryGetValue(name, out vtkMaxTextWithArrowWidget arrowWidget))
                    arrowWidget.SetAnchorPoint(anchorPoint[0], anchorPoint[1], anchorPoint[2]);
            }
        }
        private void SetAnimationArrowAnchor(string widgetName, double[] anchorPoint, string[] animationActorNames)
        {
            _animationArrowAnchors.Remove(widgetName);
            _animationArrowAnchorRequests.Remove(widgetName);
            if (animationActorNames == null) return;

            _animationArrowAnchorRequests.Add(widgetName,
                new Tuple<double[], string[]>(anchorPoint.ToArray(), animationActorNames.ToArray()));
        }
        private bool TryCreateAnimationArrowAnchor(double[] anchorPoint, string[] animationActorNames,
                                                   out vtkMaxAnimationArrowAnchor animationAnchor)
        {
            foreach (string actorName in animationActorNames)
            {
                if (_animationActors.TryGetValue(actorName, out vtkMaxActor actor) &&
                    actor.TryCreateAnimationArrowAnchor(anchorPoint, out animationAnchor)) return true;
            }
            animationAnchor = null;
            return false;
        }
        private void UpdateAnimatedArrowWidgetAnchors()
        {
            foreach (var entry in _animationArrowAnchorRequests.ToArray())
            {
                if (TryCreateAnimationArrowAnchor(entry.Value.Item1, entry.Value.Item2,
                                                  out vtkMaxAnimationArrowAnchor animationAnchor))
                {
                    _animationArrowAnchors.Add(entry.Key, animationAnchor);
                }
                _animationArrowAnchorRequests.Remove(entry.Key);
            }
            foreach (var entry in _animationArrowAnchors)
            {
                if (_arrowWidgets.TryGetValue(entry.Key, out vtkMaxTextWithArrowWidget arrowWidget) &&
                    arrowWidget.GetVisibility() == 1)
                {
                    double[] anchorPoint = entry.Value.GetAnchorPoint();
                    arrowWidget.SetAnchorPoint(anchorPoint[0], anchorPoint[1], anchorPoint[2]);
                }
            }
        }
        public void ArrangeVisibleArrowWidgets()
        {
            List<vtkMaxTextWithArrowWidget> visibleWidgets = new List<vtkMaxTextWithArrowWidget>();
            foreach (var entry in _arrowWidgets)
            {
                if (entry.Value.GetVisibility() == 1) visibleWidgets.Add(entry.Value);
            }
            foreach (vtkMaxTextWithArrowWidget widget in visibleWidgets) widget.ResetPositionFromAnchor();
            visibleWidgets.Sort(delegate (vtkMaxTextWithArrowWidget first, vtkMaxTextWithArrowWidget second)
            {
                CaeMesh.BoundingBox firstBox = first.GetBoundingBox();
                CaeMesh.BoundingBox secondBox = second.GetBoundingBox();
                double firstArea = (firstBox.MaxX - firstBox.MinX) * (firstBox.MaxY - firstBox.MinY);
                double secondArea = (secondBox.MaxX - secondBox.MinX) * (secondBox.MaxY - secondBox.MinY);
                int compare = secondArea.CompareTo(firstArea);
                if (compare == 0) compare = string.CompareOrdinal(first.GetName(), second.GetName());
                return compare;
            });
            List<vtkMaxTextWithArrowWidget> arrangedWidgets = new List<vtkMaxTextWithArrowWidget>();
            foreach (vtkMaxTextWithArrowWidget widget in visibleWidgets)
            {
                PositionArrowWidget(widget, arrangedWidgets);
                arrangedWidgets.Add(widget);
            }
            RefineArrowWidgetPositions(visibleWidgets);
            RenderScene();
        }
        private void RefineArrowWidgetPositions(IList<vtkMaxTextWithArrowWidget> visibleWidgets)
        {
            const int maxPasses = 3;
            for (int pass = 0; pass < maxPasses; pass++)
            {
                bool allCollisionFree = true;
                foreach (vtkMaxTextWithArrowWidget widget in visibleWidgets)
                {
                    List<vtkMaxTextWithArrowWidget> fixedWidgets = new List<vtkMaxTextWithArrowWidget>();
                    foreach (vtkMaxTextWithArrowWidget fixedWidget in visibleWidgets)
                    {
                        if (fixedWidget != widget) fixedWidgets.Add(fixedWidget);
                    }
                    if (!PositionArrowWidget(widget, fixedWidgets)) allCollisionFree = false;
                }
                if (allCollisionFree) break;
            }
        }
        private bool PositionNewArrowWidget(vtkMaxTextWithArrowWidget newWidget)
        {
            List<vtkMaxTextWithArrowWidget> fixedWidgets = new List<vtkMaxTextWithArrowWidget>();
            foreach (var entry in _arrowWidgets)
            {
                if (entry.Value != newWidget && entry.Value.GetVisibility() == 1) fixedWidgets.Add(entry.Value);
            }
            return PositionArrowWidget(newWidget, fixedWidgets);
        }
        private bool PositionArrowWidget(vtkMaxTextWithArrowWidget widget,
                                         IList<vtkMaxTextWithArrowWidget> fixedWidgets)
        {
            const double viewportMargin = 4;
            const double searchStep = 12;
            CaeMesh.BoundingBox originalBox = widget.GetBoundingBox();
            double width = originalBox.MaxX - originalBox.MinX;
            double height = originalBox.MaxY - originalBox.MinY;
            double preferredX = originalBox.MinX;
            double preferredY = originalBox.MinY;
            double[][] widgetLeader = widget.GetLeaderLine();
            double[] leaderEnd = widgetLeader[1];
            CaeMesh.BoundingBox anchorBox = widget.GetAnchorPointBox();
            int[] viewportSize = GetWidgetViewportSize();
            // Snapshot fixed geometry. It must not move while the new widget is being positioned.
            List<CaeMesh.BoundingBox> fixedBoxes = new List<CaeMesh.BoundingBox>();
            List<double[][]> fixedLeaderLines = new List<double[][]>();
            foreach (vtkMaxTextWithArrowWidget fixedWidget in fixedWidgets)
            {
                fixedBoxes.Add(fixedWidget.GetBoundingBox());
                fixedLeaderLines.Add(fixedWidget.GetLeaderLine());
            }
            // Search outwards from the preferred position. If no perfect position exists, retain the least obstructed one.
            HashSet<long> evaluatedPositions = new HashSet<long>();
            double bestX = preferredX;
            double bestY = preferredY;
            double bestScore = double.MaxValue;
            if (EvaluateArrowWidgetPosition(preferredX, preferredY, width, height, preferredX, preferredY,
                                            viewportMargin, viewportSize, anchorBox, fixedBoxes, fixedLeaderLines,
                                            leaderEnd, evaluatedPositions, ref bestX, ref bestY, ref bestScore))
            {
                widget.OffsetPosition(bestX - originalBox.MinX, bestY - originalBox.MinY);
                return true;
            }
            int maxRing = (int)Math.Ceiling(Math.Max(viewportSize[0], viewportSize[1]) / searchStep) + 1;
            for (int ring = 1; ring <= maxRing; ring++)
            {
                for (int x = -ring; x <= ring; x++)
                {
                    if (EvaluateArrowWidgetPosition(preferredX + x * searchStep, preferredY - ring * searchStep,
                                                    width, height, preferredX, preferredY, viewportMargin, viewportSize,
                                                    anchorBox, fixedBoxes, fixedLeaderLines, leaderEnd, evaluatedPositions,
                                                    ref bestX, ref bestY, ref bestScore))
                    {
                        widget.OffsetPosition(bestX - originalBox.MinX, bestY - originalBox.MinY);
                        return true;
                    }
                    if (EvaluateArrowWidgetPosition(preferredX + x * searchStep, preferredY + ring * searchStep,
                                                    width, height, preferredX, preferredY, viewportMargin, viewportSize,
                                                    anchorBox, fixedBoxes, fixedLeaderLines, leaderEnd, evaluatedPositions,
                                                    ref bestX, ref bestY, ref bestScore))
                    {
                        widget.OffsetPosition(bestX - originalBox.MinX, bestY - originalBox.MinY);
                        return true;
                    }
                }
                for (int y = -ring + 1; y <= ring - 1; y++)
                {
                    if (EvaluateArrowWidgetPosition(preferredX - ring * searchStep, preferredY + y * searchStep,
                                                    width, height, preferredX, preferredY, viewportMargin, viewportSize,
                                                    anchorBox, fixedBoxes, fixedLeaderLines, leaderEnd, evaluatedPositions,
                                                    ref bestX, ref bestY, ref bestScore))
                    {
                        widget.OffsetPosition(bestX - originalBox.MinX, bestY - originalBox.MinY);
                        return true;
                    }
                    if (EvaluateArrowWidgetPosition(preferredX + ring * searchStep, preferredY + y * searchStep,
                                                    width, height, preferredX, preferredY, viewportMargin, viewportSize,
                                                    anchorBox, fixedBoxes, fixedLeaderLines, leaderEnd, evaluatedPositions,
                                                    ref bestX, ref bestY, ref bestScore))
                    {
                        widget.OffsetPosition(bestX - originalBox.MinX, bestY - originalBox.MinY);
                        return true;
                    }
                }
            }
            // The viewport can be too crowded for a collision-free result. Use the lowest-penalty candidate in that case.
            widget.OffsetPosition(bestX - originalBox.MinX, bestY - originalBox.MinY);
            return false;
        }
        private int[] GetWidgetViewportSize()
        {
            if (_nativeMirror == null) return new int[] { Width, Height };
            //
            int width;
            int height;
            _nativeMirror.GetSize(out width, out height);
            return new int[] { width, height };
        }
        private bool EvaluateArrowWidgetPosition(double x, double y, double width, double height,
                                                 double preferredX, double preferredY, double viewportMargin,
                                                 int[] viewportSize, CaeMesh.BoundingBox anchorBox,
                                                 IList<CaeMesh.BoundingBox> fixedBoxes,
                                                 IList<double[][]> fixedLeaderLines, double[] leaderEnd,
                                                 HashSet<long> evaluatedPositions, ref double bestX, ref double bestY,
                                                 ref double bestScore)
        {
            double maxX = Math.Max(viewportMargin, viewportSize[0] - viewportMargin - width);
            double maxY = Math.Max(viewportMargin, viewportSize[1] - viewportMargin - height);
            x = Math.Max(viewportMargin, Math.Min(maxX, x));
            y = Math.Max(viewportMargin, Math.Min(maxY, y));
            int pixelX = (int)Math.Round(x, MidpointRounding.AwayFromZero);
            int pixelY = (int)Math.Round(y, MidpointRounding.AwayFromZero);
            long positionKey = ((long)pixelX << 32) | (uint)pixelY;
            if (!evaluatedPositions.Add(positionKey)) return false;
            x = pixelX;
            y = pixelY;
            CaeMesh.BoundingBox candidateBox = CreateArrowWidgetBox(x, y, width, height);
            double[] leaderStart = GetArrowWidgetLeaderStart(candidateBox, leaderEnd);
            bool collisionFree;
            double collisionPenalty = GetArrowWidgetCollisionPenalty(candidateBox, leaderStart, leaderEnd, anchorBox,
                                                                      fixedBoxes, fixedLeaderLines, viewportSize,
                                                                      out collisionFree);
            double dx = x - preferredX;
            double dy = y - preferredY;
            double score = collisionPenalty + dx * dx + dy * dy;
            if (score < bestScore)
            {
                bestScore = score;
                bestX = x;
                bestY = y;
            }
            if (collisionFree)
            {
                bestX = x;
                bestY = y;
                return true;
            }
            return false;
        }
        private double GetArrowWidgetCollisionPenalty(CaeMesh.BoundingBox candidateBox, double[] candidateLeaderStart,
                                                      double[] candidateLeaderEnd, CaeMesh.BoundingBox anchorBox,
                                                      IList<CaeMesh.BoundingBox> fixedBoxes,
                                                      IList<double[][]> fixedLeaderLines, int[] viewportSize,
                                                      out bool collisionFree)
        {
            const double spacing = 6;
            const double collisionCost = 1E+9;
            const double leaderCrossingCost = 1E+6;
            double penalty = 0;
            bool hardCollision = false;
            double outsideWidth = Math.Max(0, -candidateBox.MinX) + Math.Max(0, candidateBox.MaxX - viewportSize[0]);
            double outsideHeight = Math.Max(0, -candidateBox.MinY) + Math.Max(0, candidateBox.MaxY - viewportSize[1]);
            if (outsideWidth > 0 || outsideHeight > 0)
            {
                hardCollision = true;
                penalty += collisionCost * (1 + outsideWidth + outsideHeight);
            }
            // Do not place a box over its own anchor/arrowhead.
            if (ArrowWidgetBoxesIntersect2D(candidateBox, anchorBox, spacing))
            {
                hardCollision = true;
                penalty += collisionCost;
            }
            for (int i = 0; i < fixedBoxes.Count; i++)
            {
                CaeMesh.BoundingBox fixedBox = fixedBoxes[i];
                double[][] fixedLeader = fixedLeaderLines[i];
                if (ArrowWidgetBoxesIntersect2D(candidateBox, fixedBox, spacing))
                {
                    hardCollision = true;
                    penalty += collisionCost + 1000 * GetArrowWidgetOverlapArea(candidateBox, fixedBox, spacing);
                }
                // The new box must not cover an existing leader, and the new leader should not cross an existing box.
                if (ArrowWidgetLineIntersectsBox2D(fixedLeader[0], fixedLeader[1], candidateBox, spacing))
                {
                    hardCollision = true;
                    penalty += collisionCost;
                }
                if (ArrowWidgetLineIntersectsBox2D(candidateLeaderStart, candidateLeaderEnd, fixedBox, spacing))
                {
                    hardCollision = true;
                    penalty += collisionCost;
                }
                // Avoid leader-to-leader crossings where the available screen area permits it.
                if (!ArrowWidgetSegmentsShareEndPoint2D(candidateLeaderStart, candidateLeaderEnd,
                                                        fixedLeader[0], fixedLeader[1], spacing) &&
                    ArrowWidgetSegmentsIntersect2D(candidateLeaderStart[0], candidateLeaderStart[1],
                                                   candidateLeaderEnd[0], candidateLeaderEnd[1],
                                                   fixedLeader[0][0], fixedLeader[0][1],
                                                   fixedLeader[1][0], fixedLeader[1][1])) penalty += leaderCrossingCost;
            }
            collisionFree = !hardCollision;
            return penalty;
        }
        private static CaeMesh.BoundingBox CreateArrowWidgetBox(double x, double y, double width, double height)
        {
            CaeMesh.BoundingBox box = new CaeMesh.BoundingBox();
            box.MinX = x;
            box.MaxX = x + width;
            box.MinY = y;
            box.MaxY = y + height;
            box.MinZ = 0;
            box.MaxZ = 1;
            return box;
        }
        private static double[] GetArrowWidgetLeaderStart(CaeMesh.BoundingBox box, double[] leaderEnd)
        {
            if (leaderEnd[0] < box.MinX) return new double[] { box.MinX, (box.MinY + box.MaxY) / 2 };
            else if (leaderEnd[0] > box.MaxX) return new double[] { box.MaxX, (box.MinY + box.MaxY) / 2 };
            else if (leaderEnd[1] < box.MinY) return new double[] { (box.MinX + box.MaxX) / 2, box.MinY };
            else return new double[] { (box.MinX + box.MaxX) / 2, box.MaxY };
        }
        private static bool ArrowWidgetBoxesIntersect2D(CaeMesh.BoundingBox first, CaeMesh.BoundingBox second,
                                                        double spacing)
        {
            if (first.MaxX + spacing <= second.MinX) return false;
            if (first.MinX - spacing >= second.MaxX) return false;
            if (first.MaxY + spacing <= second.MinY) return false;
            if (first.MinY - spacing >= second.MaxY) return false;
            return true;
        }
        private static double GetArrowWidgetOverlapArea(CaeMesh.BoundingBox first, CaeMesh.BoundingBox second,
                                                        double spacing)
        {
            double width = Math.Min(first.MaxX + spacing, second.MaxX) - Math.Max(first.MinX - spacing, second.MinX);
            double height = Math.Min(first.MaxY + spacing, second.MaxY) - Math.Max(first.MinY - spacing, second.MinY);
            return Math.Max(0, width) * Math.Max(0, height);
        }
        private static bool ArrowWidgetLineIntersectsBox2D(double[] start, double[] end,
                                                           CaeMesh.BoundingBox box, double spacing)
        {
            double minX = box.MinX - spacing;
            double maxX = box.MaxX + spacing;
            double minY = box.MinY - spacing;
            double maxY = box.MaxY + spacing;
            return ArrowWidgetSegmentIntersectsRectangle2D(start[0], start[1], end[0], end[1],
                                                           minX, maxX, minY, maxY);
        }
        private static bool ArrowWidgetPointInsideBox2D(double x, double y, double minX, double maxX,
                                                        double minY, double maxY)
        {
            return x >= minX && x <= maxX && y >= minY && y <= maxY;
        }
        private static bool ArrowWidgetSegmentIntersectsRectangle2D(double ax, double ay, double bx, double by,
                                                                    double minX, double maxX,
                                                                    double minY, double maxY)
        {
            if (ArrowWidgetPointInsideBox2D(ax, ay, minX, maxX, minY, maxY) ||
                ArrowWidgetPointInsideBox2D(bx, by, minX, maxX, minY, maxY)) return true;
            double dx = bx - ax;
            double dy = by - ay;
            double t0 = 0;
            double t1 = 1;
            if (!ArrowWidgetClipSegment2D(-dx, ax - minX, ref t0, ref t1)) return false;
            if (!ArrowWidgetClipSegment2D(dx, maxX - ax, ref t0, ref t1)) return false;
            if (!ArrowWidgetClipSegment2D(-dy, ay - minY, ref t0, ref t1)) return false;
            if (!ArrowWidgetClipSegment2D(dy, maxY - ay, ref t0, ref t1)) return false;
            return t0 <= t1;
        }
        private static bool ArrowWidgetClipSegment2D(double p, double q, ref double t0, ref double t1)
        {
            const double tolerance = 1E-9;
            if (Math.Abs(p) <= tolerance) return q >= -tolerance;
            double r = q / p;
            if (p < 0)
            {
                if (r > t1) return false;
                if (r > t0) t0 = r;
            }
            else
            {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            }
            return true;
        }
        private static bool ArrowWidgetSegmentsShareEndPoint2D(double[] firstStart, double[] firstEnd,
                                                               double[] secondStart, double[] secondEnd,
                                                               double tolerance)
        {
            return ArrowWidgetPointsNear2D(firstStart, secondStart, tolerance) ||
                   ArrowWidgetPointsNear2D(firstStart, secondEnd, tolerance) ||
                   ArrowWidgetPointsNear2D(firstEnd, secondStart, tolerance) ||
                   ArrowWidgetPointsNear2D(firstEnd, secondEnd, tolerance);
        }
        private static bool ArrowWidgetPointsNear2D(double[] first, double[] second, double tolerance)
        {
            double dx = first[0] - second[0];
            double dy = first[1] - second[1];
            return dx * dx + dy * dy <= tolerance * tolerance;
        }
        private static bool ArrowWidgetSegmentsIntersect2D(double ax, double ay, double bx, double by,
                                                           double cx, double cy, double dx, double dy)
        {
            const double tolerance = 1E-7;
            double abC = ArrowWidgetCross2D(ax, ay, bx, by, cx, cy);
            double abD = ArrowWidgetCross2D(ax, ay, bx, by, dx, dy);
            double cdA = ArrowWidgetCross2D(cx, cy, dx, dy, ax, ay);
            double cdB = ArrowWidgetCross2D(cx, cy, dx, dy, bx, by);
            if (((abC > tolerance && abD < -tolerance) || (abC < -tolerance && abD > tolerance)) &&
                ((cdA > tolerance && cdB < -tolerance) || (cdA < -tolerance && cdB > tolerance))) return true;
            if (Math.Abs(abC) <= tolerance && ArrowWidgetPointOnSegment2D(cx, cy, ax, ay, bx, by)) return true;
            if (Math.Abs(abD) <= tolerance && ArrowWidgetPointOnSegment2D(dx, dy, ax, ay, bx, by)) return true;
            if (Math.Abs(cdA) <= tolerance && ArrowWidgetPointOnSegment2D(ax, ay, cx, cy, dx, dy)) return true;
            if (Math.Abs(cdB) <= tolerance && ArrowWidgetPointOnSegment2D(bx, by, cx, cy, dx, dy)) return true;
            return false;
        }
        private static double ArrowWidgetCross2D(double ax, double ay, double bx, double by, double px, double py)
        {
            return (bx - ax) * (py - ay) - (by - ay) * (px - ax);
        }
        private static bool ArrowWidgetPointOnSegment2D(double px, double py, double ax, double ay,
                                                        double bx, double by)
        {
            const double tolerance = 1E-9;
            return px >= Math.Min(ax, bx) - tolerance && px <= Math.Max(ax, bx) + tolerance &&
                   py >= Math.Min(ay, by) - tolerance && py <= Math.Max(ay, by) + tolerance;
        }
        public void RemoveAllArrowWidgets()
        {
            foreach (var entry in _arrowWidgets) UnmirrorWidget(entry.Key);
            //
            _arrowWidgets.Clear();
            _animationArrowAnchors.Clear();
            _animationArrowAnchorRequests.Clear();
        }
        public void RemoveArrowWidgets(string[] widgetNames)
        {
            vtkMaxTextWithArrowWidget widget;
            foreach (var name in widgetNames)
            {
                if (_arrowWidgets.TryGetValue(name, out widget))
                {
                    UnmirrorWidget(name);
                    _arrowWidgets.Remove(name);
                    _animationArrowAnchors.Remove(name);
                    _animationArrowAnchorRequests.Remove(name);
                }
            }
            //
            RenderScene();
        }
        public void HideAllArrowWidgets()
        {
            foreach (var entry in _arrowWidgets) entry.Value.VisibilityOff();
        }
        
        #endregion #################################################################################################################

        #region Native pilot  ######################################################################################################
        // The VTK 9.7 view of this control: the native view in the window draws the scene, takes the mouse, the keys
        // and the trackpad, and picks
        public VtkNativeHost NativeHost { get { return _nativeHost; } }
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);
        // A view change of the native camera: the view is drawn again
        private void OnNativeCameraChanged()
        {
            RenderScene();
        }
        //
        public VtkNativeView NativeMirror { get { return _nativeMirror; } }
        // The native view with every area, which the application shows; the checks switch it on again with it
        public void EnableNativeMirror(bool enable)
        {
            SetNativeAreas(VtkNativeArea.All);
        }
        public VtkNativeArea NativeAreas { get { return _nativeAreas; } }
        // The areas this control sends to the native view: all of them in the application, fewer in the checks of an
        // area. The native view keeps its camera; the caller redraws, so the native view receives the current scene
        public void SetNativeAreas(VtkNativeArea areas)
        {
            if (_nativeMirror != null)
            {
                if (_nativeMirrorScalarBar != null) _nativeMirrorScalarBar.Dispose();
                _nativeMirrorScalarBar = null;
                DisposeNativeMirrorWidgets();
                WireNativeWindowSelection(_nativeMirror, false);
                _nativeMirror.ClearActors();
                _nativeMirror.Render();
                _nativeMirror = null;
                _nativeMirrorNames = null;
            }
            _nativeAreas = areas;
            if (_nativeHost.View == null) return;
            //
            if (ContainsFocus && _nativeHost.ViewWindow != IntPtr.Zero) SetFocus(_nativeHost.ViewWindow);
            _nativeMirror = _nativeHost.View;
            WireNativeWindowSelection(_nativeMirror, (areas & VtkNativeArea.Widgets) == 0);
            _nativeMirrorNames = new HashSet<string>();
            _nativeMirror.SetCameraAdjustmentSuspended(_cameraAdjustmentSuspendCount > 0);
            SyncNativeMirror();
            if ((areas & VtkNativeArea.Widgets) != 0) CreateNativeMirrorScalarBar();
            if ((areas & VtkNativeArea.Widgets) != 0) CreateNativeMirrorWidgets();
            MirrorOwnerBoxZoom();
        }
        public string[] GetNativeMirrorNames()
        {
            return _nativeMirrorNames == null ? new string[0] : _nativeMirrorNames.ToArray();
        }
        // The window size, the background, the smoothing and the edges mode follow this control
        public void SyncNativeMirror()
        {
            if (_nativeMirror == null) return;
            _nativeMirror.SetSize(Math.Max(1, _nativeHost.ClientSize.Width), Math.Max(1, _nativeHost.ClientSize.Height));
            _nativeMirror.SetBackground(_gradientBackground, _backgroundTop, _backgroundBottom);
            _nativeMirror.SetSmoothing(_pointSmoothing, _lineSmoothing);
            _nativeMirror.SetEdgesVisibility(_edgesVisibility);
            _nativeMirror.SetAmbientOcclusion(_ambientOcclusion, _ambientOcclusionRelativeRadius,
                                              _ambientOcclusionKernelSize);
            // The colors are set once at start, before a native view exists
            _nativeMirror.SetHighlightColors(_primaryHighlightColor, _secondaryHighlightColor);
        }
        // The parts only mode of the checks: the native view shows its base layer only, and without the scalar bar
        // and the widgets, unless every actor of the geometry area is to be drawn
        public void SetNativeMirrorPartsOnly(bool partsOnly)
        {
            if (partsOnly && !_nativePartsOnly)
            {
                _nativePartsOnly = true;
                if (!_nativeMirrorAllGeometry) ClearToBaseLayer();
                if (_nativeMirrorScalarBar != null) _nativeMirrorScalarBar.SetVisibility(false);
                ShowNativeMirrorWidgets(false);
            }
            else if (!partsOnly && _nativePartsOnly)
            {
                _nativePartsOnly = false;
                if (_nativeMirrorScalarBar != null) _nativeMirrorScalarBar.SetVisibility(_scalarBarVisible);
                ShowNativeMirrorWidgets(true);
            }
            RenderScene();
        }
        //
        // The scalar bar of the native view: it is made from the state this control keeps of the legend, and every
        // later change of the legend reaches it
        public VtkNativeScalarBar NativeMirrorScalarBar { get { return _nativeMirrorScalarBar; } }
        private void CreateNativeMirrorScalarBar()
        {
            _nativeMirrorScalarBar = new VtkNativeScalarBar(_nativeMirror);
            _nativeMirrorScalarBar.SetTextStyle(VtkNativeTextStyle.CreateWidgetStyle());
            MirrorScalarBarColorSpectrum(_colorSpectrum);
            _nativeMirrorScalarBar.CreateLookupTable(VtkNativeColorSpectrum.FromColorSpectrum(_colorSpectrum).GetColor,
                                                     _scalarBarRange[0], _scalarBarRange[1], _scalarBarRange[2],
                                                     _scalarBarRange[3]);
            _nativeMirrorScalarBar.SetLabelFormat(_scalarBarLabelFormat);
            if (_scalarBarText != null)
                _nativeMirrorScalarBar.SetText(_scalarBarText[0], _scalarBarText[1], _scalarBarText[2], _scalarBarText[3],
                                               _scalarBarText[4]);
            _nativeMirrorScalarBar.SetPadding(15);
            _nativeMirrorScalarBar.SetVisibility(_scalarBarVisible);
            _nativeMirrorScalarBar.MouseDoubleClick += scalarBarWidget_DoubleClicked;
            MirrorScalarBarFrame();
        }
        private void SetScalarBarWidgetVisibility(bool visible)
        {
            _scalarBarVisible = visible;
            if (_nativeMirrorScalarBar != null) _nativeMirrorScalarBar.SetVisibility(visible);
        }
        private void CreateScalarBarLookupTable(double scalarRangeMin, double scalarRangeMax, double minUserValue,
                                                double maxUserValue)
        {
            _scalarBarRange = new double[] { scalarRangeMin, scalarRangeMax, minUserValue, maxUserValue };
            if (_nativeMirrorScalarBar != null)
                _nativeMirrorScalarBar.CreateLookupTable(VtkNativeColorSpectrum.FromColorSpectrum(_colorSpectrum).GetColor,
                                                         scalarRangeMin, scalarRangeMax, minUserValue, maxUserValue);
        }
        // SetScalarBarColorSpectrum takes the visibility and the colors of the added bands from its argument
        private void MirrorScalarBarColorSpectrum(vtkMaxColorSpectrum colorSpectrum)
        {
            if (_nativeMirrorScalarBar == null) return;
            _nativeMirrorScalarBar.SetSmoothColors(_colorSpectrum.Mode == vtkColorSpectrumMode.Smooth);
            _nativeMirrorScalarBar.SetNumberOfColors(_colorSpectrum.NumberOfColors);
            _nativeMirrorScalarBar.MinColorVisible = colorSpectrum.MinColorVisible;
            _nativeMirrorScalarBar.MaxColorVisible = colorSpectrum.MaxColorVisible;
            _nativeMirrorScalarBar.MinColor = colorSpectrum.MinColor;
            _nativeMirrorScalarBar.MaxColor = colorSpectrum.MaxColor;
        }
        private void MirrorScalarBarFrame()
        {
            if (_nativeMirrorScalarBar == null) return;
            _nativeMirrorScalarBar.SetFrame(_scalarBarBackgroundVisible, new double[] { 1, 1, 1 }, _scalarBarBorderVisible,
                                            new double[] { 0, 0, 0 });
        }
        //
        // A pick of the native view: a left click or a box selection at the given points. The events it raises are
        // the ones the Controller resolves into ids
        public void PickWithNativeMirror(int x1, int y1, bool boxSelection, int x2, int y2, int clicks = 1)
        {
            if (_nativeMirror == null || (_nativeAreas & VtkNativeArea.Selection) == 0) return;
            MouseEventArgs mea = new MouseEventArgs(MouseButtons.Left, clicks, x1, y1, 0);
            string[] pickedActorNames;
            if (_selectBy == vtkSelectBy.Off) return;
            else if (_selectBy == vtkSelectBy.Widget)
            {
                if (clicks == 1) Form_CommitViewportEdit?.Invoke();
            }
            else if (_selectBy == vtkSelectBy.Default)
            {
                if (!boxSelection) pickedActorNames = new string[] { NativeMirrorPickPoint(x1, y1, out _, out _) };
                else pickedActorNames = NativeMirrorPickArea(x1, y1, x2, y2, out _);
                Controller_ActorsPicked?.Invoke(mea, ModifierKeys, pickedActorNames.ToArray());
            }
            else
            {
                vtkSelectOperation selectOperation;
                if (Control.ModifierKeys == (Keys.Shift | Keys.Control)) selectOperation = vtkSelectOperation.Intersect;
                else if (Control.ModifierKeys == Keys.Shift) selectOperation = vtkSelectOperation.Add;
                else if (Control.ModifierKeys == Keys.Control) selectOperation = vtkSelectOperation.Subtract;
                else selectOperation = vtkSelectOperation.None;
                bool completelyInside = x1 > x2;
                if (!boxSelection)
                {
                    double[] direction;
                    double[] pickedPoint;
                    string pickedActorName = NativeMirrorPickPoint(x1, y1, out pickedPoint, out direction);
                    pickedActorNames = pickedActorName == null ? new string[0] : new string[] { pickedActorName };
                    OnMouseLeftButtonUpSelection?.Invoke(pickedPoint, direction, null, completelyInside,
                                                         selectOperation, pickedActorNames);
                }
                else
                {
                    double[][] planeParameters;
                    pickedActorNames = NativeMirrorPickArea(x1, y1, x2, y2, out planeParameters);
                    if (pickedActorNames.Length == 0) planeParameters = null;
                    OnMouseLeftButtonUpSelection?.Invoke(null, null, planeParameters, completelyInside,
                                                         selectOperation, pickedActorNames);
                }
            }
        }
        // The part under the point and the point on it; the selection distance applies to a pick by the user. On a
        // section view a part selection resolves the cut to its part, a node or element selection to the cut cells
        private string NativeMirrorPickPoint(int x, int y, out double[] point, out double[] direction)
        {
            string[] selectable = _selectableActorsFilter == null ? null : _selectableActorsFilter.ToArray();
            string partName;
            bool partSelection = _selectBy == vtkSelectBy.Default || _selectBy == vtkSelectBy.Part ||
                                 _selectBy == vtkSelectBy.GeometryPart || _selectItem == vtkSelectItem.Part;
            _nativeMirror.SetSectionViewPickMode(partSelection, _selectBy == vtkSelectBy.Node ||
                                                                _selectBy == vtkSelectBy.Element);
            _nativeMirror.PickPoint(x, y, _selectionDistance, _renderingOn && _userPick, selectable, out point,
                                    out direction, out partName, out _);
            return partName;
        }
        // The parts an area pick reports: the picked parts that have a volume grid, each once
        private string[] NativeMirrorPickArea(int x1, int y1, int x2, int y2, out double[][] planeParameters)
        {
            string[] pickedNames = _nativeMirror.PickArea(x1, y1, x2, y2, out planeParameters);
            List<string> selectedActorNames = new List<string>();
            HashSet<int> selectedPointGlobalIds = new HashSet<int>();
            HashSet<int> selectedCellGlobalIds = new HashSet<int>();
            foreach (string pickedName in pickedNames)
            {
                vtkMaxActor maxActor;
                if (!_actors.TryGetValue(pickedName, out maxActor) || !maxActor.VolumeGridAvailable ||
                    selectedActorNames.Contains(pickedName)) continue;
                if (new CaeMesh.BoundingBox(_nativeMirror.GetPartBounds(pickedName))
                    .TestBoundsAgainstFrustum(planeParameters) == FrustumBoxResultEnum.Inside &&
                    TryAddWholePart(maxActor, selectedPointGlobalIds, selectedCellGlobalIds))
                {
                    selectedActorNames.Add(pickedName);
                }
                else if (SendNativeVolumeGrid(pickedName, maxActor) != null) selectedActorNames.Add(pickedName);
            }
            return selectedActorNames.ToArray();
        }
        // Shows only the scalar bar: the other widgets are switched off and the native view drops its parts
        public void SetNativeMirrorScalarBarOnly(bool scalarBarOnly)
        {
            if (scalarBarOnly && _nativeMirrorScalarBarHiddenWidgets == null)
            {
                List<vtkMaxBorderWidget> widgets = new List<vtkMaxBorderWidget>
                {
                    _coorSysWidget, _scaleWidget, _colorBarWidget, _statusBlockWidget, _probeWidget
                };
                widgets.AddRange(_arrowWidgets.Values);
                _nativeMirrorScalarBarHiddenWidgets = new List<vtkMaxBorderWidget>();
                foreach (vtkMaxBorderWidget widget in widgets)
                {
                    if (widget == null || widget.GetVisibility() == 0) continue;
                    widget.VisibilityOff();
                    _nativeMirrorScalarBarHiddenWidgets.Add(widget);
                }
                if (_nativeMirror != null)
                {
                    _nativeMirror.ClearActors();
                    _nativeMirrorNames.Clear();
                }
            }
            else if (!scalarBarOnly && _nativeMirrorScalarBarHiddenWidgets != null)
            {
                foreach (vtkMaxBorderWidget widget in _nativeMirrorScalarBarHiddenWidgets) widget.VisibilityOn();
                _nativeMirrorScalarBarHiddenWidgets = null;
            }
            RenderScene();
        }
        #endregion  ################################################################################################################

        public bool ContainsActor(string actorName)
        {
            return _actors.ContainsKey(actorName);
        }
        // A sub part of a compound part draws other model edges when the sub parts shown change: the part is drawn
        // again with them
        public void UpdateActorModelEdgesToDraw(string actorName, PartExchangeData modelEdgesToDraw)
        {
            vtkMaxActor actor;
            if (!_actors.TryGetValue(actorName, out actor)) return;
            //
            if (!actor.UpdateModelEdgesToDraw(modelEdgesToDraw) || !_nativeActors.Contains(actor)) return;
            AddNativeActor(actor);
            if (_sectionViewData.Active) CreateNativeSectionView(_sectionViewData.Point, _sectionViewData.Normal);
            RenderScene();
        }
        public void UpdateActor(string oldName, string newName, Color newColor)
        {
            if (!_actors.ContainsKey(oldName)) return;
            //
            vtkMaxActor actor = _actors[oldName];
            actor.Name = newName;
            //
            UpdateActorColor(actor, newColor);
            RenameNativeActor(actor, oldName, newName);
            SetNativeActorColor(actor, newName, newColor);
            //
            _actors.Remove(oldName);
            _actors.Add(newName, actor);
            //
            RenderScene();
        }
        public void UpdateActorColor(vtkMaxActor actor, Color newColor)
        {
            actor.Color = newColor;
            // Transformed copies
            foreach (var copy in actor.Copies) UpdateActorColor(copy, newColor);
        }
        /// <summary>
        /// Recolors actors whose cells are colored through a color table by replacing the tables only, so new
        /// colors need neither the actors nor their cells rebuilt. An actor is shaded and highlighted with the
        /// mean of its cell colors, which keeps the alpha of its own color. Returns false when an actor is not
        /// drawn with a color table of the same length, which only a rebuild can color.
        /// </summary>
        public bool UpdateActorsColorTables(string[] actorNames, Color[][] colorTables)
        {
            vtkMaxActor actor;
            for (int i = 0; i < actorNames.Length; i++)
            {
                if (!_actors.TryGetValue(actorNames[i], out actor) || actor.ColorTable == null ||
                    actor.ColorTable.Length != colorTables[i].Length) return false;
            }
            //
            for (int i = 0; i < actorNames.Length; i++)
            {
                actor = _actors[actorNames[i]];
                UpdateActorColorTable(actor, colorTables[i]);
                SetNativeActorColorTable(actor);
            }
            // The section faces and skins take the colors of the parts they cut
            if (_sectionViewData.Active) CreateNativeSectionView(_sectionViewData.Point, _sectionViewData.Normal);
            //
            RenderScene();
            return true;
        }
        private void UpdateActorColorTable(vtkMaxActor actor, Color[] colorTable)
        {
            double sumR = 0;
            double sumG = 0;
            double sumB = 0;
            foreach (var color in colorTable)
            {
                sumR += color.R;
                sumG += color.G;
                sumB += color.B;
            }
            int count = Math.Max(colorTable.Length, 1);
            Color meanColor = Color.FromArgb(actor.Color.A, (int)(sumR / count), (int)(sumG / count), (int)(sumB / count));
            //
            actor.ColorTable = colorTable;
            foreach (var copy in actor.Copies) UpdateActorColorTable(copy, colorTable);
            UpdateActorColor(actor, meanColor);
        }
        // The color table and the mean color of an actor and of its transformed copies in the native view
        private void SetNativeActorColorTable(vtkMaxActor actor)
        {
            if (!_nativeActors.Contains(actor)) return;
            RunNative("SetActorColorTable " + actor.Name, () =>
            {
                PartExchangeData geometry = actor.Data.Geometry;
                VtkNativeLayer layer = VtkNativePartStyle.ToNativeLayer(actor.Layer);
                _nativeMirror.SetActorCellColors(actor.NativeName, layer, geometry.Cells.Ids != null ?
                                                 geometry.Cells.Values : null, actor.ColorTable);
                _nativeMirror.SetActorColor(actor.NativeName, layer, actor.Color);
            });
            foreach (var copy in actor.Copies) SetNativeActorColorTable(copy);
        }
        public double[] GetBoundingBox()
        {
            // xMin, xMax, yMin, yMax, zMin, zMax
            if (_nativeMirror != null) return GetNativeBounds();
            return new double[] { 1, -1, 1, -1, 1, -1 };
        }
        public double[] GetBoundingBoxSize()
        {
            // xMin, xMax, yMin, yMax, zMin, zMax
            double[] b = GetBoundingBox();
            return new double[] { b[1] - b[0], b[3] - b[2], b[5] - b[4] };
        }
        public double GetBoundingBoxDiagonal()
        {
            double[] b = GetBoundingBoxSize();
            return Math.Sqrt(Math.Pow(b[0], 2) + Math.Pow(b[1], 2) + Math.Pow(b[2], 2));
        }

        // Render
        private void RenderScene()
        {
            if (_renderingOn)
            {
                if (_coorSysWidget != null) _coorSysWidget.UpdateGeometry();
                //
                _nativeHost.Invalidate();
                //
                foreach (var entry in _actors)
                {
                    if (entry.Value.MemorySize == 0) entry.Value.RecomputeMemorySize();
                }
            }
        }

        // Tools
        public void CropPartWithCylinder(string partName, double r, string fileName)
        {
            _nativeMirror.CropPartToStl(partName, 0, new double[] { r }, fileName);
        }
        public void CropPartWithCube(string partName, double x, double y, double z, double a, string fileName)
        {
            _nativeMirror.CropPartToStl(partName, 1, new double[] { x, y, z, a }, fileName);
        }
        public void SmoothPart(string partName, double a, string fileName)
        {
            _nativeMirror.SmoothPartToStl(partName, fileName);
        }
        // The drawn actors of the base and overlay layers for the 3MF export
        public vtkMaxActorData[] GetVtkMaxActorData()
        {
            if (_nativeMirror == null) return new vtkMaxActorData[0];
            return _nativeMirror.GetExportActors();
        }
        public vtkMaxLookupTable GetLookupTable()
        {
            if (_scalarRange == null) return new vtkMaxLookupTable();
            double[] tableRange = GetLookupTableColors(_scalarRange[0], _scalarRange[1], out _);
            double[] range = GetLookupTableColors(tableRange[0], tableRange[1], out Color[] colors);
            return new vtkMaxLookupTable(range[0], range[1], colors, _colorSpectrum.Mode == vtkColorSpectrumMode.Smooth);
        }
    }
}
