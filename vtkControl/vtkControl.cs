// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using Kitware.VTK;
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
    /// UserControl derived implementation of vtkRenderWindow for use
    /// in Windows Forms applications.
    /// The client area of this UserControl is completely filled with
    /// an instance of a vtkRenderWindow.
    /// </summary>
    [ComVisible(true), ClassInterface(ClassInterfaceType.AutoDual)]
    public partial class vtkControl : UserControl, IMessageFilter
    {
        int countError;
        // Variables                                                                                                                        
        private bool _renderingOn;
        private bool _userPick;     // used to determine if the selection distance should be limited
        private vtkRenderer _renderer;
        private vtkRenderer _selectionRenderer;
        private vtkRenderer _overlayRenderer;
        private vtkRenderer _widgetRenderer;
        private vtkRenderWindow _renderWindow;
        private vtkRenderWindowInteractor _renderWindowInteractor;
        private vtkLight _light1;
        private vtkLight _light2;
        private vtkLight _light3;
        // Symbols resolution
        private int _arrowTipResolution = 21;
        private int _arrowShaftResolution = 15;
        // Widgets
        private const int VtkCursorDefault = 0;
        private const int VtkCursorHand = 9;
        private const int CoorSysWidgetMargin = 24;
        private vtkMaxCoorSysWidget _coorSysWidget;
        private vtkMaxScaleWidget _scaleWidget;
        private vtkLookupTable _lookupTable;
        private vtkMaxScalarBarWidget _scalarBarWidget;
        private vtkMaxColorBarWidget _colorBarWidget;
        private vtkMaxStatusBlockWidget _statusBlockWidget;
        private vtkInteractorStyleControl _style;
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
        private Dictionary<string, vtkPolyData> _measurementArrowInputs;
        private Dictionary<string, double> _measurementArrowSymbolSizes;
        private vtkMaxAnimationFrameData _animationFrameData;
        private int _currentAnimationFrameNumber;
        private Color _primaryHighlightColor;
        private Color _secondaryHighlightColor;
        private double _maxSymbolSize;
        private bool _drawSymbolEdges;
        //
        private bool _animating;
        private bool _mouseIn;
        //
        OrderedDictionary<vtkTransform, int> _transforms;
        // Section view
        private SectionViewData _sectionViewData;
        private object myLock = new object();
        // Selection
        private bool _querySelectionInProgress;
        private vtkSelectBy _selectBy;
        private vtkSelectItem _selectItem;
        private vtkPropPicker _propPicker;
        private vtkPointPicker _pointPicker;
        private vtkCellPicker _cellPicker;
        private vtkRenderedAreaPicker _areaPicker;
        private int _selectionDistance;
        //
        private vtkMaxActor _mouseSelectionActorCurrent;
        private HashSet<int> _mouseSelectionAllIds;
        private int[] _mouseSelectionCurrentIds;
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
        // Caption actors
        private vtkActor _captionTmpActor;
        private bool _captionActorAddedToBase;
        private bool _captionActorAddedToSelection;
        private bool _captionActorAddedToOverlay;


        // Properties                                                                                                               
        public bool RenderingOn
        {
            get { return _renderingOn; }
            set
            {
                if (value != _renderingOn)
                {
                    _renderingOn = value;
                    if (_renderingOn)
                    {
                        RenderScene();
                        AdjustCameraDistanceAndClipping();  // Update the scale widget after render
                    }
                }
            }
        }
        public bool UserPick { get { return _userPick; } set { _userPick = value; } }
        public vtkEdgesVisibility EdgesVisibility
        {
            get { return _edgesVisibility; }
            set
            {
                if (_edgesVisibility != value)
                {
                    _edgesVisibility = value;
                    ApplyEdgesVisibilityAndBackfaceCulling();
                }
            }
        }
        public bool QuerySelectionInProgress
        {
            get { return _querySelectionInProgress; }
            set
            {
                _querySelectionInProgress = value;
                _style.SelectionBoxEnabled = !value;
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
                            _style.SelectionBoxEnabled = false;
                            break;
                        case vtkSelectBy.Default:
                        // Mesh based
                        case vtkSelectBy.Node:
                        case vtkSelectBy.Element:
                        case vtkSelectBy.Part:
                        // Geometry
                        case vtkSelectBy.Geometry:
                        case vtkSelectBy.GeometryPart:
                            _style.SelectionBoxEnabled = true;
                            if (_querySelectionInProgress) _style.SelectionBoxEnabled = false;
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
                            _style.SelectionBoxEnabled = false;
                            break;
                        // Widget
                        case vtkSelectBy.Widget:
                            _style.SelectionBoxEnabled = false;
                            break;
                        default:
                            throw new NotSupportedException();
                    }
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
            if (_style == null) return;
            //
            _style.RotateSensitivity = _rotateSensitivity;
            _style.ZoomSensitivity = _zoomSensitivity;
            _style.InvertZoom = _invertZoom;
            _style.SmoothZoom = _smoothZoom;
        }
        public void StartBoxZoom()
        {
            _style.BoxZoomEnabled = true;
        }


        // Getters
        public TrackpadNavigation TrackpadNavigation { get { return _trackpadNavigation; } }
        public bool IsBoxSelectionActive
        {
            get
            {
                vtkInteractorStyleControl style = (vtkInteractorStyleControl)(_renderWindowInteractor.GetInteractorStyle());
                return style.IsBoxSelectionActive;
            }
        }


        // Callbacks                                                                                                                
        public Func<string, string> Controller_GetAnnotationText;
        public Action<int, bool> Controller_AnimationFrameChanged;
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
            Globals.Initialize();
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
            _measurementArrowInputs = new Dictionary<string, vtkPolyData>();
            _measurementArrowSymbolSizes = new Dictionary<string, double>();
            _animationFrameData = null;
            _currentAnimationFrameNumber = -1;
            _primaryHighlightColor = Color.Red;
            _secondaryHighlightColor = Color.Violet;
            _drawSymbolEdges = true;
            //
            _animating = false;
            //
            _transforms = new OrderedDictionary<vtkTransform, int>("Transformations");
            //
            _sectionViewData = new SectionViewData();
            //
            //SelectBy = vtkSelectBy.Default;
            _selectItem = vtkSelectItem.None;
            //
            _propPicker = vtkPropPicker.New();
            _propPicker.PickFromListOn();
            _propPicker.InitializePickList();
            //
            _pointPicker = vtkPointPicker.New();
            _pointPicker.SetTolerance(0.01);
            //
            _cellPicker = vtkCellPicker.New();
            //
            _areaPicker = vtkRenderedAreaPicker.New();
            _mouseSelectionActorCurrent = null;
            _mouseSelectionCurrentIds = null;
            _mouseSelectionAllIds = new HashSet<int>();
            //
            _selectableActorsFilter = null;
            //
            _followerViewPartNames = null;
            _followerViewPointIds = null;
            _followerViewPointCoor = null;
            //
            _drawCameraPath = false;
            //
            _captionActorAddedToBase = false;
            _captionActorAddedToSelection= false;
            _captionActorAddedToOverlay = false;
            _captionTmpActor = GetActorTmp(0, 0, 0);
        }


        // Event handlers                                                                                                           
        private void vtkControl_Resize(object sender, EventArgs e)
        {
            if (_renderWindow != null) _renderWindow.Modified();     // this has to be here in order for the vtkMAx widgets to work on maximize/minimize
            //if (_renderWindowInteractor != null) _renderWindowInteractor.Render();

            if (_coorSysWidget != null) ResetCoorSysWidgetPosition();
            if (_renderer != null)
            {
                SetAllCaptionPositions();
                //float scale = vtkTextActor.GetFontScale(_renderer);
                //Console.WriteLine(scale.ToString());

                //int[] size = _renderer.GetSize();
                //foreach (var actor in _actors)
                //{
                //    if (actor is vtkScalarBarActor)
                //    {
                //        vtkScalarBarActor sb = (vtkScalarBarActor)actor;
                //        int height = 300;
                //        int border = 20;
                //        sb.SetDisplayPosition(border, size[1] - border - height);
                //    }
                //}
            }
        }
        private void vtkControl_Load(object sender, EventArgs e)
        {
            InitializeControl();
        }
        private void vtkControl_EnabledChanged(object sender, EventArgs e)
        {
            if (Enabled) SetAllCaptionPositions();
        }
        //
        void _renderWindowInteractor_ModifiedEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            if (_style.GetState() == vtkInteractorStyleControl.VTKIS_ROTATE && _probeWidget != null &&
                _probeWidget.GetVisibility() == 1)
            {
                _probeWidget.VisibilityOff();
            }
        }
        //
        void style_EnterEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            if (!_mouseIn) _mouseIn = true;
        }
        void style_LeaveEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            if (_mouseIn) _mouseIn = false;
            if (!_widgetHighlightLocked) _style.ClearWidgetHighlights();
            _style.ClearWidgetProximity();
            _renderWindow.SetCurrentCursor(0);  // Default
        }
        private void style_MouseInteractionEvent()
        {
            Form_CommitViewportEdit?.Invoke();
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
            if (_scalarBarWidget.TryGetValueEditData(e.X, e.Y, out bool editMinimum, out Rectangle rectangle,
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
                case vtkMaxCoorSysTarget.XPositive: SetAxisView(true, new double[] { 1, 0, 0 }); break;
                case vtkMaxCoorSysTarget.XNegative: SetAxisView(true, new double[] { -1, 0, 0 }); break;
                case vtkMaxCoorSysTarget.YPositive: SetAxisView(true, new double[] { 0, 1, 0 }); break;
                case vtkMaxCoorSysTarget.YNegative: SetAxisView(true, new double[] { 0, -1, 0 }); break;
                case vtkMaxCoorSysTarget.ZPositive: SetAxisView(true, new double[] { 0, 0, 1 }); break;
                case vtkMaxCoorSysTarget.ZNegative: SetAxisView(true, new double[] { 0, 0, -1 }); break;
                case vtkMaxCoorSysTarget.Origin: SetIsometricView(true, true); break;
                default: break;
            }
        }
        private void coorSysWidget_HoverChanged(bool hovered)
        {
            if (_renderWindow == null) return;
            //
            _renderWindow.SetCurrentCursor(hovered ? VtkCursorHand : VtkCursorDefault);
        }

        // Trackpad                                                                                                                 
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_MOUSEHWHEEL = 0x020E;
        private const int MK_SHIFT = 0x0004;
        private const int MK_CONTROL = 0x0008;
        private const int WM_INPUT = 0x00FF;
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
            //
            base.WndProc(ref m);
        }
        private void vtkControl_HandleCreated(object sender, EventArgs e)
        {
            _precisionTouchpad.Register(Handle);
        }
        private void HandleRawInput(IntPtr rawInputHandle)
        {
            if (_trackpadNavigation == null || !_trackpadNavigation.Enabled) return;
            if (!_precisionTouchpad.Found || _style == null || _renderer == null) return;
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
            if (_style != null) _style.EndMultiTouchGesture();
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
            _trackpadNavigation.Apply(_style, dragX, dragY, zoom, _trackpadCenterX, _trackpadCenterY, _trackpadShift);
        }
        private bool HandleTrackpadWheel(ref Message m)
        {
            if (_trackpadNavigation == null || !_trackpadNavigation.Enabled) return false;
            if (_style == null || _renderer == null) return false;
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
        public void Pick(int x1, int y1, bool boxSelection, int x2, int y2)
        {
            // Test only method
            MouseEventArgs e = new MouseEventArgs(MouseButtons.Left, 1, x1, y1, 0);
            style_PointPickedOnLeftUpEvt(e, boxSelection, x2, y2);
        }
        private void Style_PointPickedOnMouseMoveEvt(int x1, int y1, bool boxSelection, int x2, int y2)
        {
            bool userPick = _userPick;
            try
            {
                _userPick = true;
                // Off
                if (_probeWidget == null || _selectBy == vtkSelectBy.Off) return;
                else if (_selectBy == vtkSelectBy.Widget) return;
                // Default selection of parts
                else if (_selectBy == vtkSelectBy.Default)
                {
                    // Area selection
                    if (boxSelection)
                    {
                        string[] pickedActorNames = null;
                        PickByArea(x1, y1, x2, y2, false, out pickedActorNames);
                        //
                        MouseEventArgs e = new MouseEventArgs(MouseButtons.Left, 1, x1, y1, 0);
                        //
                        Controller_ActorsPicked?.Invoke(e, ModifierKeys, pickedActorNames.ToArray());
                    }
                }
                //
                else if (boxSelection)
                {
                    ClearCurrentMouseSelection();
                    PickByArea(x1, y1, x2, y2, true, out string[] pickedActorNAmes);
                }
                else
                {
                    vtkActor pickedActor = null;
                    //
                    ClearCurrentMouseSelection();
                    //
                    switch (_selectBy)
                    {
                        //case vtkSelectBy.Default:
                        case vtkSelectBy.Id:
                            break;
                        case vtkSelectBy.Node:
                            PickByNode(out pickedActor, x1, y1, _querySelectionInProgress);
                            break;
                        case vtkSelectBy.Element:
                            PickByCell(out pickedActor, x1, y1, _querySelectionInProgress);
                            break;
                        case vtkSelectBy.Edge:
                            PickByEdge(out pickedActor, x1, y1, false);
                            break;
                        case vtkSelectBy.Surface:
                            PickBySurface(out pickedActor, x1, y1, false);
                            break;
                        case vtkSelectBy.EdgeAngle:
                            PickByEdgeAngle(out pickedActor, x1, y1);
                            break;
                        case vtkSelectBy.SurfaceAngle:
                            PickBySurfaceAngle(out pickedActor, x1, y1);
                            break;
                        case vtkSelectBy.Part:
                            PickByActor(out pickedActor, x1, y1, _querySelectionInProgress);
                            break;
                        case vtkSelectBy.Geometry:
                            PickByGeometry(out pickedActor, x1, y1);
                            break;
                        case vtkSelectBy.GeometryVertex:
                            PickByGeometryVertex(out pickedActor, x1, y1);
                            break;
                        case vtkSelectBy.GeometryEdge:
                            PickByEdge(out pickedActor, x1, y1, _querySelectionInProgress);
                            break;
                        case vtkSelectBy.GeometrySurface:
                            PickBySurface(out pickedActor, x1, y1, _querySelectionInProgress);
                            break;
                        case vtkSelectBy.GeometryEdgeAngle:
                            PickByGeometryEdgeAngle(out pickedActor, x1, y1);
                            break;
                        case vtkSelectBy.GeometrySurfaceAngle:
                            PickBySurface(out pickedActor, x1, y1, false);
                            break;
                        case vtkSelectBy.GeometryPart:
                            PickByActor(out pickedActor, x1, y1, false);
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    //
                    if (pickedActor == null) ClearCurrentMouseSelection();
                }
            }
            catch { }
            finally
            {
                _userPick = UserPick;
            }
        }
        private void style_PointPickedOnLeftUpEvt(MouseEventArgs mea, bool boxSelection, int x2, int y2)
        {
            string[] pickedActorNames = null;
            // Off
            if (_selectBy == vtkSelectBy.Off) return;
            else if (_selectBy == vtkSelectBy.Widget)
            {
                if (mea.Clicks == 1) Form_CommitViewportEdit?.Invoke();
            }
            // Default selection of parts during normal use
            else if (_selectBy == vtkSelectBy.Default)
            {
                // Point selection
                if (!boxSelection)
                {
                    vtkActor pickedActor;
                    GetPickPoint(out pickedActor, mea.Location.X, mea.Location.Y);
                    pickedActorNames = new string[] { GetActorName(pickedActor) };
                }
                // Area selection
                else
                {
                    PickByArea(mea.Location.X, mea.Location.Y, x2, y2, false, out pickedActorNames);
                }
                //
                Controller_ActorsPicked?.Invoke(mea, ModifierKeys, pickedActorNames.ToArray());
            }
            else
            {
                vtkSelectOperation selectOperation;
                if (Control.ModifierKeys == (Keys.Shift | Keys.Control)) selectOperation = vtkSelectOperation.Intersect;
                else if (Control.ModifierKeys == Keys.Shift) selectOperation = vtkSelectOperation.Add;
                else if (Control.ModifierKeys == Keys.Control) selectOperation = vtkSelectOperation.Subtract;
                else selectOperation = vtkSelectOperation.None;
                //
                vtkActor pickedActor = null;
                bool completelyInside = mea.Location.X > x2;
                // Point selection
                if (!boxSelection)
                {
                    double[] pickedPoint = GetPickPoint(out pickedActor, mea.Location.X, mea.Location.Y);
                    double[] direction = _renderer.GetActiveCamera().GetDirectionOfProjection();
                    if (pickedActor == null) pickedActorNames = new string[0];
                    else  pickedActorNames = new string[] { GetActorName(pickedActor) };
                    OnMouseLeftButtonUpSelection?.Invoke(pickedPoint, direction, null, completelyInside,
                                                         selectOperation, pickedActorNames);
                }
                // Area selection
                else
                {
                    double[][] planeParameters = null;
                    PickByArea(mea.Location.X, mea.Location.Y, x2, y2, false, out pickedActorNames);
                    //
                    if (!(pickedActorNames == null || pickedActorNames.Length == 0))
                    {
                        vtkPlanes planes = _areaPicker.GetFrustum();
                        vtkPlane plane;
                        double[] origin;
                        double[] normal;
                        planeParameters = new double[planes.GetNumberOfPlanes()][];
                        //
                        for (int i = 0; i < planes.GetNumberOfPlanes(); i++)
                        {
                            plane = planes.GetPlane(i);
                            origin = plane.GetOrigin();
                            normal = plane.GetNormal();
                            planeParameters[i] = new double[] { origin[0], origin[1], origin[2], normal[0], normal[1], normal[2] };
                        }
                    }
                    //
                    OnMouseLeftButtonUpSelection?.Invoke(null, null, planeParameters, completelyInside,
                                                         selectOperation, pickedActorNames);
                }
            }
        }
        private void style_RightButtonPressEvent(object sender, MouseEventArgs e)
        {
            // Off
            if (_selectBy == vtkSelectBy.Off) return;
            else if (_selectBy == vtkSelectBy.Widget) return;
            else if (!_widgetInteractionEnabled && sender is vtkMaxBorderWidget) return;
            //
            if (sender != null && sender is vtkMaxTextWithArrowWidget widget)
            {
                Form_WidgetPicked(e, Keys.None, widget.GetName(), widget.GetRectangle());
            }
            else if (sender is vtkMaxScalarBarWidget scalarBarWidget)
            {
                vtkResultWidgetContextMenuTypeEnum contextMenuType;
                bool minimum = false;
                //
                if (scalarBarWidget.TryGetValueEditData(e.X, e.Y, out minimum, out Rectangle valueRectangle,
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
                vtkActor pickedActor;
                GetPickPoint(out pickedActor, e.Location.X, e.Location.Y);
                //
                if (pickedActor != null)
                {
                    Controller_ActorsPicked?.Invoke(e, ModifierKeys, new string[] { GetActorName(pickedActor) });
                }
            }
        }
        private void style_ZoomChangedEvent()
        {
            SetAllCaptionPositions();
        }

        #region Selection  #########################################################################################################

        // On mouse move selection                                                                                                  
        private void PickByNode(out vtkActor pickedActor, int x, int y, bool showLabel)
        {
            double[] pickedPoint = GetPickPoint(out pickedActor, x, y);
            if (pickedPoint == null)
            {
                if (_probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }
            //
            int globalPointId = GetNodeIdOnCellFaceClosestToPoint(pickedPoint);
            _mouseSelectionCurrentIds = new int[] { globalPointId };
            //
            vtkMaxActorData data = Controller_GetNodeActorData(_mouseSelectionCurrentIds);
            _mouseSelectionActorCurrent = new vtkMaxActor(data, true, true);
            AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
            _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);
            //
            if (showLabel)
            {
                // Probe widget
                _renderer.SetWorldPoint(pickedPoint[0], pickedPoint[1], pickedPoint[2], 1.0);
                _renderer.WorldToDisplay();
                double[] display = _renderer.GetDisplayPoint();
                double w = x + 20d;
                double h = y + 10d;
                //
                _probeWidget.SetPosition(w, h);
                _probeWidget.SetText(Controller_GetAnnotationText(globalPointId.ToString()));
                //
                if (_probeWidget.GetVisibility() == 0) _probeWidget.VisibilityOn();
            }
        }
        private void PickByCell(out vtkActor pickedActor, int x, int y, bool showLabel)
        {
            double[] pickedPoint = GetPickPoint(out pickedActor, x, y);
            if (pickedPoint == null)
            {
                if (_probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }
            //
            vtkCell cell;
            vtkCellLocator cellLocator;
            string actorName = GetActorName(pickedActor);
            int globalCellId = GetGlobalCellIdClosestTo3DPoint(ref pickedPoint, out cell, out cellLocator);
            //
            _mouseSelectionCurrentIds = new int[] { globalCellId };
            //
            vtkMaxActorData actorData = Controller_GetCellActorData(_mouseSelectionCurrentIds, null);
            //
            vtkMaxActor actor = new vtkMaxActor(actorData, true, false);
            actor.GeometryMapper.GetInput().GetPointData().RemoveArray(Globals.ScalarArrayName);
            _mouseSelectionActorCurrent = actor;
            //
            AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
            _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);
            //
            if (actor.ElementEdges != null)
            {
                AddActorEdges(_mouseSelectionActorCurrent, false, vtkRendererLayer.Selection);
                _selectedActors.Remove(_mouseSelectionActorCurrent);
            }
            //
            if (showLabel)
            {
                // Probe widget
                double w = x + 20;
                double h = y + 10;
                //
                _probeWidget.SetPosition(w, h);
                _probeWidget.SetText(Controller_GetAnnotationText(globalCellId.ToString()));
                //
                if (_probeWidget.GetVisibility() == 0) _probeWidget.VisibilityOn();
            }
        }
        private void PickByEdge(out vtkActor pickedActor, int x, int y, bool showLabel)
        {
            double[] pickedPoint = GetPickPoint(out pickedActor, x, y);
            if (pickedPoint == null)
            {
                if (_probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }
            //
            vtkCell cell;
            vtkCellLocator cellLocator;
            int globalPointId = GetNodeIdOnCellFaceClosestToPoint(pickedPoint);
            int globalCellId = GetGlobalCellIdClosestTo3DPoint(ref pickedPoint, out cell, out cellLocator);
            int[] globalCellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);
            vtkMaxActorData cellFaceData = Controller_GetCellFaceActorData(globalCellId, globalCellFaceNodeIds); // works on undeformed mesh
            //
            int[] nodeIds;
            double[][] nodeCoor;
            int[] edgeCell;
            int edgeCellType;
            GetClosestEdgeCell(pickedPoint, cellFaceData, out nodeIds, out nodeCoor, out edgeCell, out edgeCellType);
            //
            vtkMaxActorData actorData = Controller_GetEdgeActorData(globalCellId, nodeIds);
            //
            if (actorData != null)
            {
                vtkMaxActor actor = new vtkMaxActor(actorData);
                _mouseSelectionActorCurrent = actor;
                //
                AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
                _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);
                //
                if (showLabel)
                {
                    // Probe widget
                    _renderer.SetWorldPoint(pickedPoint[0], pickedPoint[1], pickedPoint[2], 1.0);
                    _renderer.WorldToDisplay();
                    double[] display = _renderer.GetDisplayPoint();
                    //
                    double w = x + 20d;
                    double h = y + 10d;
                    //
                    _probeWidget.SetPosition(w, h);
                    _probeWidget.SetText(Controller_GetAnnotationText(actorData.Name));
                    //
                    if (_probeWidget.GetVisibility() == 0) _probeWidget.VisibilityOn();
                }
            }
        }
        private void PickByEdgeAngle(out vtkActor pickedActor, int x, int y)
        {
            double[] pickedPoint = GetPickPoint(out pickedActor, x, y);
            if (pickedPoint == null)
            {
                if (_probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }
            //
            vtkCell cell;
            vtkCellLocator cellLocator;
            int globalCellId = GetGlobalCellIdClosestTo3DPoint(ref pickedPoint, out cell, out cellLocator);
            int[] globalCellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);
            // Works on deformed mesh
            vtkMaxActorData cellFaceData = Controller_GetCellFaceActorData(globalCellId, globalCellFaceNodeIds);
            //
            double[][] nodeCoor;
            int[] edgeCell;
            int edgeCellType;
            GetClosestEdgeCell(pickedPoint, cellFaceData, out _, out nodeCoor, out edgeCell, out edgeCellType);
            //
            vtkPoints p = vtkPoints.New();
            for (int i = 0; i < nodeCoor.Length; i++)
            {
                p.InsertNextPoint(nodeCoor[i][0], nodeCoor[i][1], nodeCoor[i][2]);
            }
            vtkUnstructuredGrid grid = vtkUnstructuredGrid.New();
            grid.SetPoints(p);
            //
            vtkIdList pointIds = vtkIdList.New();
            for (int i = 0; i < edgeCell.Length; i++)  // renumber
            {
                pointIds.InsertNextId(edgeCell[i]);
            }
            grid.InsertNextCell(edgeCellType, pointIds);
            grid.Update();      // must create a new grid, not only grid from cell . is not drawn for some zoom values ???
            //
            vtkMaxActor actor = new vtkMaxActor(grid);
            _mouseSelectionActorCurrent = actor;
            //
            AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
            _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);
        }
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
        private void PickBySurface(out vtkActor pickedActor, int x, int y, bool showLabel)
        {
            double[] pickedPoint = GetPickPoint(out pickedActor, x, y);
            if (pickedPoint == null)
            {
                if (_probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }
            //
            vtkCellLocator cellLocator;
            vtkCell cell;
            int globalCellId = GetGlobalCellIdClosestTo3DPoint(ref pickedPoint, out cell, out cellLocator);
            int[] globalCellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);
            //
            string noEdgePartName;
            vtkMaxActor actor = null;
            vtkMaxActorData actorData = Controller_GetSurfaceEdgesActorDataFromElementId(globalCellId, globalCellFaceNodeIds,
                                                                                         out noEdgePartName);
            if (actorData != null)
            {
                actorData.CanHaveElementEdges = true;
                actor = new vtkMaxActor(actorData);
            }
            else if (noEdgePartName != null)
            {
                actor = GetHighlightActorFromActorName(noEdgePartName);
            }
            _mouseSelectionActorCurrent = actor;
            //
            AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
            _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);
            //
            if (showLabel)
            {
                // Probe widget
                _renderer.SetWorldPoint(pickedPoint[0], pickedPoint[1], pickedPoint[2], 1.0);
                _renderer.WorldToDisplay();
                //
                double w = x + 20d;
                double h = y + 10d;
                //
                _probeWidget.SetPosition(w, h);
                _probeWidget.SetText(Controller_GetAnnotationText(actorData.Name));
                //
                if (_probeWidget.GetVisibility() == 0) _probeWidget.VisibilityOn();
            }
        }
        private void PickBySurfaceAngle(out vtkActor pickedActor, int x, int y)
        {
            double[] pickedPoint = GetPickPoint(out pickedActor, x, y);
            if (pickedPoint == null)
            {
                if (_probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }

            vtkCell cell;
            vtkCellLocator cellLocator;
            int globalCellId = GetGlobalCellIdClosestTo3DPoint(ref pickedPoint, out cell, out cellLocator);
            int[] globalCellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);

            vtkMaxActorData data = Controller_GetCellFaceActorData(globalCellId, globalCellFaceNodeIds); // works on undeformed mesh

            vtkMaxActor actor = new vtkMaxActor(data);
            _mouseSelectionActorCurrent = actor;

            AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
            _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);

            if (actor.ElementEdges != null)
            {
                AddActorEdges(_mouseSelectionActorCurrent, false, vtkRendererLayer.Selection);
                _selectedActors.Remove(_mouseSelectionActorCurrent);
            }
        }
        private void PickByActor(out vtkActor pickedActor, int x, int y, bool showLabel)
        {
            double[] pickedPoint = GetPickPoint(out pickedActor, x, y);
            if (pickedPoint == null)
            {
                if (_probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }
            //
            vtkCellLocator cellLocator;
            vtkCell cell;
            int globalCellId = GetGlobalCellIdClosestTo3DPoint(ref pickedPoint, out cell, out cellLocator);
            //
            string actorName = GetActorName(pickedActor);
            vtkMaxActor actor = GetHighlightActorFromActorName(actorName);
            //
            if (actor != null)
            {
                _mouseSelectionActorCurrent = actor;
                //
                AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
                _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);
                //
                if (actor.ElementEdges != null)
                {
                    AddActorEdges(_mouseSelectionActorCurrent, false, vtkRendererLayer.Selection);
                    _selectedActors.Remove(_mouseSelectionActorCurrent);
                }
                //
                if (showLabel)
                {
                    // Probe widget
                    _renderer.SetWorldPoint(pickedPoint[0], pickedPoint[1], pickedPoint[2], 1.0);
                    _renderer.WorldToDisplay();
                    //
                    double w = x + 20d;
                    double h = y + 10d;
                    //
                    _probeWidget.SetPosition(w, h);
                    _probeWidget.SetText(Controller_GetAnnotationText(actorName));
                    //
                    if (_probeWidget.GetVisibility() == 0) _probeWidget.VisibilityOn();
                }
            }
        }
        private void PickByGeometry(out vtkActor pickedActor, int x, int y)
        {
            double[] pickedPoint = GetPickPoint(out pickedActor, x, y);
            //
            if (pickedPoint == null)
            {
                if (_probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }
            //
            vtkCellLocator cellLocator;
            vtkCell cell;
            int globalCellId = GetGlobalCellIdClosestTo3DPoint(ref pickedPoint, out cell, out cellLocator);
            int[] globalCellEdgeNodeIds = GetEdgeNodeIds(pickedPoint, globalCellId, cell, cellLocator);
            int[] globalCellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);
            //double distance = GetSelectionPrecision();
            //
            string noEdgePartName;
            vtkMaxActorData actorData = Controller_GetGeometryActorData(pickedPoint,
                                                                        globalCellId,
                                                                        globalCellEdgeNodeIds,
                                                                        globalCellFaceNodeIds,
                                                                        out noEdgePartName);
            //
            if (actorData != null)
            {
                if (actorData.Geometry.Nodes.Coor.Length == 1)
                {
                    // Nodal actor data
                    _mouseSelectionActorCurrent = new vtkMaxActor(actorData, false, true);
                }
                else
                {
                    // Edge or surface actor data
                    actorData.CanHaveElementEdges = true;
                    _mouseSelectionActorCurrent = new vtkMaxActor(actorData);
                }
            }
            else if (noEdgePartName != null)
            {
                _mouseSelectionActorCurrent = GetHighlightActorFromActorName(noEdgePartName);
            }
            //
            AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
            _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);
        }
        private void PickByGeometryVertex(out vtkActor pickedActor, int x, int y)
        {
            double[] pickedPoint = GetPickPoint(out pickedActor, x, y);
            //
            if (pickedPoint == null)
            {
                if (_probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }
            //
            vtkCellLocator cellLocator;
            vtkCell cell;
            int globalCellId = GetGlobalCellIdClosestTo3DPoint(ref pickedPoint, out cell, out cellLocator);
            int[] globalCellEdgeNodeIds = GetEdgeNodeIds(pickedPoint, globalCellId, cell, cellLocator);
            int[] globalCellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);
            //double distance = GetSelectionPrecision();
            //
            vtkMaxActorData actorData = Controller_GetGeometryVertexActorData(pickedPoint,
                                                                              globalCellId,
                                                                              globalCellEdgeNodeIds,
                                                                              globalCellFaceNodeIds);
            if (actorData != null)
            {
                _mouseSelectionActorCurrent = new vtkMaxActor(actorData, false, true);
                //
                AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
                _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);
            }
        }
        private void PickByGeometryEdgeAngle(out vtkActor pickedActor, int x, int y)
        {
            double[] pickedPoint = GetPickPoint(out pickedActor, x, y);
            if (pickedPoint == null)
            {
                if (_probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }
            //
            vtkCell cell;
            vtkCellLocator cellLocator;
            int globalPointId = GetNodeIdOnCellFaceClosestToPoint(pickedPoint);
            int globalCellId = GetGlobalCellIdClosestTo3DPoint(ref pickedPoint, out cell, out cellLocator);
            int[] globalCellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);
            // Works on deformed mesh
            vtkMaxActorData cellFaceData = Controller_GetCellFaceActorData(globalCellId, globalCellFaceNodeIds);
            //
            int[] nodeIds;
            GetClosestEdgeCell(pickedPoint, cellFaceData, out nodeIds, out _, out _, out _);
            //
            vtkMaxActorData edgeData = Controller_GetEdgeActorData(globalCellId, nodeIds);
            //
            if (edgeData != null)
            {
                vtkMaxActor actor = new vtkMaxActor(edgeData);
                _mouseSelectionActorCurrent = actor;
                //
                AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
                _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);
            }
        }
        //
        private void PickByArea(int x1, int y1, int x2, int y2, bool highlight, out string[] pickedActorNames)
        {
            _areaPicker.AreaPick(x1, y1, x2, y2, _renderer);
            vtkPlanes planes = _areaPicker.GetFrustum();
            //
            List<string> selectedActorNames = new List<string>();
            HashSet<int> selectedPointGlobalIds = new HashSet<int>();
            HashSet<int> selectedCellGlobalIds = new HashSet<int>();
            vtkActor pickedActor;
            vtkProp3DCollection pickedActors = _areaPicker.GetProp3Ds();
            //
            pickedActors.InitTraversal();
            pickedActor = (vtkActor)pickedActors.GetNextProp3D();
            //
            vtkCellLocator locator;
            vtkExtractSelectedFrustum extractor;
            //
            while (pickedActor != null)
            {
                string pickedActorName = GetActorName(pickedActor);
                vtkMaxActor maxActor;
                //
                if (pickedActorName == null ||
                    !(_actors.TryGetValue(pickedActorName, out maxActor) && maxActor.FrustumCellLocator != null))
                {
                    // Actor has no locator
                }
                else
                {
                    // Actors               
                    selectedActorNames.Add(pickedActorName);
                    //
                    locator = maxActor.FrustumCellLocator;
                    // Points               
                    extractor = vtkExtractSelectedFrustum.New(); // must be inside the loop
                    extractor.SetFieldType((int)vtkSelectionField.POINT);
                    extractor.SetContainingCells(0);
                    extractor.SetInput(0, locator.GetDataSet());
                    extractor.SetFrustum(planes);
                    extractor.Update();
                    //
                    vtkUnstructuredGrid selected = vtkUnstructuredGrid.New();
                    selected.ShallowCopy(extractor.GetOutput());
                    //
                    vtkDataArray globalPointIds = selected.GetPointData().GetGlobalIds();
                    if (globalPointIds == null)  // skip actor
                    {
                        pickedActor = (vtkActor)pickedActors.GetNextProp3D();
                        continue;
                    }
                    else
                    {
                        if (highlight)
                        {
                            for (int i = 0; i < globalPointIds.GetNumberOfTuples(); i++)
                            {
                                selectedPointGlobalIds.Add((int)globalPointIds.GetTuple1(i));
                            }
                        }
                    }
                    // Cells                
                    extractor.SetFieldType((int)vtkSelectionField.CELL);
                    extractor.Update();
                    //
                    selected = vtkUnstructuredGrid.New();
                    selected.ShallowCopy(extractor.GetOutput());
                    //
                    vtkDataArray globalCellIds = selected.GetCellData().GetGlobalIds();
                    if (globalCellIds == null)  // skip actor
                    {
                        pickedActor = (vtkActor)pickedActors.GetNextProp3D();
                        continue;
                    }
                    else
                    {
                        if (highlight)
                        {
                            for (int i = 0; i < globalCellIds.GetNumberOfTuples(); i++)
                            {
                                selectedCellGlobalIds.Add((int)globalCellIds.GetTuple1(i));
                            }
                        }
                    }
                }
                pickedActor = (vtkActor)pickedActors.GetNextProp3D();
            }
            //
            pickedActorNames = selectedActorNames.ToArray();
            //
            // Graphics
            bool completelyInside = x1 > x2;
            if (highlight)
            {
                int[] pointIds = new int[selectedPointGlobalIds.Count];
                selectedPointGlobalIds.CopyTo(pointIds);
                //
                vtkMaxActorData data = null;
                if (_selectBy == vtkSelectBy.Node)
                {
                    if (pointIds.Length <= 0) return;
                    //
                    data = Controller_GetNodeActorData(pointIds);
                    //
                    _mouseSelectionCurrentIds = pointIds;
                    _mouseSelectionActorCurrent = new vtkMaxActor(data, true, true);
                }
                else if (_selectBy == vtkSelectBy.Element)
                {
                    if (selectedCellGlobalIds.Count <= 0) return;
                    //
                    int[] cellIds = new int[selectedCellGlobalIds.Count];
                    selectedCellGlobalIds.CopyTo(cellIds);
                    //
                    if (_selectItem == vtkSelectItem.Element && _selectBy == vtkSelectBy.Node)
                        data = Controller_GetCellActorData(cellIds, null);
                    else data = Controller_GetCellActorData(cellIds, pointIds);
                    //
                    _mouseSelectionCurrentIds = data.Geometry.Cells.Ids;
                    _mouseSelectionActorCurrent = new vtkMaxActor(data, true, false);
                }
                else if (_selectBy == vtkSelectBy.Part || _selectBy == vtkSelectBy.GeometryPart)
                {
                    _mouseSelectionActorCurrent = GetAppendedActor(pickedActorNames, GetHighlightActorFromActorName);
                    //
                    if (_mouseSelectionActorCurrent == null) return;
                }
                else if (_selectBy == vtkSelectBy.Geometry)
                {
                    int[] cellIds = new int[selectedCellGlobalIds.Count];
                    selectedCellGlobalIds.CopyTo(cellIds);
                    //
                    string[] noEdgePartNames;
                    vtkMaxActorData actorData = Controller_GetSurfaceEdgesActorDataFromNodeAndElementIds(pointIds, cellIds,
                                                                                                         completelyInside,
                                                                                                         out noEdgePartNames);
                    actorData.CanHaveElementEdges = true;
                    // Append actors
                    vtkMaxActor actor = new vtkMaxActor(actorData);
                    if (noEdgePartNames != null && noEdgePartNames.Length > 0)
                    {
                        vtkMaxActor noEdgeActor = GetAppendedActor(noEdgePartNames, GetHighlightActorFromActorName);
                        actor = GetAppendedActor(actor, noEdgeActor);
                    }
                    _mouseSelectionActorCurrent = actor;
                }
                else throw new NotSupportedException();
                //
                AddActorGeometry(_mouseSelectionActorCurrent, vtkRendererLayer.Selection);
                _mouseSelectionActorCurrent.Geometry.SetProperty(Globals.CurrentMouseSelectionProperty);
                //
                if ((_selectBy == vtkSelectBy.Element || _selectBy == vtkSelectBy.Part)
                    && _mouseSelectionActorCurrent.ElementEdges != null)
                {
                    AddActorEdges(_mouseSelectionActorCurrent, false, vtkRendererLayer.Selection);
                    _selectedActors.Remove(_mouseSelectionActorCurrent);
                }
            }
        }
        private void PickAreaHardware(int x1, int y1, int x2, int y2)
        {

            vtkHardwareSelector selector = vtkHardwareSelector.New();
            selector.SetFieldAssociation((int)vtkFieldAssociations.FIELD_ASSOCIATION_CELLS);
            selector.SetRenderer(_renderer);
            selector.SetArea((uint)x1, (uint)y1, (uint)x2, (uint)y2);

            //static_cast<unsigned int>(this.Renderer.GetPickX1()),
            //static_cast<unsigned int>(this.Renderer.GetPickY1()),
            //static_cast<unsigned int>(this.Renderer.GetPickX2()),
            //static_cast<unsigned int>(this.Renderer.GetPickY2()));

            // Make the actual pick and pass the result to the convenience function
            // defined earlier
            _renderer.Render();

            vtkSelection result = selector.Select();
            uint numNodes = result.GetNumberOfNodes();
            vtkSelectionNode node;

            vtkDataSetAttributes fd1;
            vtkIdTypeArray fd1_array;

            for (uint i = 0; i < numNodes; i++)
            {
                node = result.GetNode(i);

                //vtkIdTypeArray selIds = (vtkIdTypeArray)node.GetSelectionList();

                fd1 = node.GetSelectionData();

                if (fd1.GetNumberOfArrays() != 1) return;
                if (fd1.GetArray(0).GetDataType() != 12) return; // 12 ... VTK_ID_TYPE

                fd1_array = (vtkIdTypeArray)fd1.GetArray(0);

                long fd1_N = fd1_array.GetNumberOfTuples();

                List<long> fd1_list = new List<long>();
                for (int j = 0; j < fd1_N; j++) fd1_list.Add(fd1_array.GetValue(j));
            }
        }
        private double[] GetPickPoint(out vtkActor pickedActor, int x, int y)
        {
            pickedActor = null;
            if (_style.IsPositionOverWidget(x, y)) return null;
            //
            double[] p = null;
            int cellId = -1;
            // Make all actors visible
            foreach (var entry in _actors)
            {
                //if (entry.Value.Pickable)
                {
                    if (_selectableActorsFilter != null)
                    {
                        // Change the visibility of geometry only to enable reset
                        if (_selectableActorsFilter.Contains(entry.Value.Name)) entry.Value.Geometry.VisibilityOn();
                        else entry.Value.Geometry.VisibilityOff();
                    }
                    // Set opacity for all visible actors to 1
                    if (entry.Value.GeometryProperty.GetOpacity() != 1) entry.Value.GeometryProperty.SetOpacity(1);
                }
            }
            // Pick actor by hardware rendering - render the scene before Pick
            //_overlayRenderer.Render();
            //_propPicker.Pick(x, y, 0, _overlayRenderer);
            //pickedActor = _propPicker.GetActor();
            //
            _renderer.Render();
            _propPicker.Pick(x, y, 0, _renderer);
            pickedActor = _propPicker.GetActor();
            // First pick
            if (pickedActor != null)
            {
                p = GetPropPickerPickPosition(x, y);
                cellId = GetGlobalCellIdClosestTo3DPoint(ref p, out _, out _);
            }
            // If the exact pixel misses, pick geometry within a fixed screen-space tolerance.
            if (cellId == -1)
            {
                int[] windowSize = _renderWindow.GetSize();
                double windowDiagonal = Math.Sqrt(windowSize[0] * windowSize[0] + windowSize[1] * windowSize[1]);
                _cellPicker.SetTolerance(windowDiagonal > 0 ? _selectionDistance / windowDiagonal : 0);
                //
                _cellPicker.Pick(x, y, 0, _renderer);
                pickedActor = _cellPicker.GetActor();
                if (pickedActor != null)
                {
                    p = _cellPicker.GetPickPosition();
                    cellId = GetGlobalCellIdClosestTo3DPoint(ref p, out _, out _);
                }
            }
            // Reset the actor visibility
            foreach (var entry in _actors)
            {
                //if (entry.Value.Pickable)
                {
                    entry.Value.UpdateColor();
                    ApplyEdgeVisibilityAndBackfaceCullingToActor(entry.Key, entry.Value.Geometry, entry.Value.GeometryProperty,
                                                                 vtkRendererLayer.Base);
                }
            }
            //
            if (cellId == -1)
            {
                p = null;
                pickedActor = null;
            }
            //
            return p;
        }
        private double[] GetPropPickerPickPosition(int x, int y)
        {
            // vtkPropPicker.GetPickPosition() reconstructs the point from the normal scene
            // depth buffer, where non-pickable symbols can hide the selected model actor.
            // Use the depth produced by the restricted hardware pick pass instead.
            _renderer.SetDisplayPoint(x, y, _renderer.GetPickedZ());
            _renderer.DisplayToWorld();
            double[] worldPoint = _renderer.GetWorldPoint();
            double w = worldPoint[3];
            return new double[] { worldPoint[0] / w, worldPoint[1] / w, worldPoint[2] / w };
        }

        // Item selection - Point                                                                                                   
        private int GetGlobalCellIdClosestTo3DPoint(ref double[] point, out vtkCell cell, out vtkCellLocator cellLocator)
        {
            // a 3D point is saved in history and used for regenerate
            cell = null;
            cellLocator = null;
            //
            IntPtr x = Marshal.AllocHGlobal(3 * 8);
            Marshal.Copy(point, 0, x, 3);
            //
            IntPtr closestPoint = Marshal.AllocHGlobal(3 * 8);
            //
            int globalCellId = -1;
            double minDistance = double.MaxValue;
            long localCellId = -1;
            int subId = -1;
            double distance2 = -1;
            double[] pointOut = new double[3];
            vtkCellLocator locator;
            //
            foreach (var entry in _actors)
            {
                // Set filtered actors
                if (_selectableActorsFilter != null)
                {
                    if (_selectableActorsFilter.Contains(entry.Value.Name)) entry.Value.Geometry.VisibilityOn();
                    else entry.Value.Geometry.VisibilityOff();
                }
                // Find closest point on visible actors only
                if (entry.Value.Geometry.GetVisibility() != 0)
                {
                    locator = entry.Value.CellLocator;
                    if (locator != null)
                    {
                        locator.FindClosestPoint(x, closestPoint, ref localCellId, ref subId, ref distance2);
                        // Error: distance2 can be -1 if something in locator is not properly defined
                        if (distance2 >= 0 && distance2 < minDistance)
                        {
                            minDistance = distance2;
                            cellLocator = locator;
                            cell = locator.GetDataSet().GetCell(localCellId);
                            globalCellId = (int)locator.GetDataSet().GetCellData().GetGlobalIds().GetTuple1(localCellId);
                            //
                            Marshal.Copy(closestPoint, pointOut, 0, 3);
                        }
                    }
                }
                // Reset visibility
                ApplyEdgeVisibilityAndBackfaceCullingToActor(entry.Key, entry.Value.Geometry, entry.Value.GeometryProperty,
                                                             vtkRendererLayer.Base);
                if (minDistance == 0) break;
            }
            //
            Marshal.FreeHGlobal(x);
            Marshal.FreeHGlobal(closestPoint);
            //
            double maxErrorDistance2;
            if (_renderingOn)   // user mouse interaction
                maxErrorDistance2 = Math.Pow(GetSelectionPrecision(), 2);
            else                // regeneration - the distance to the closest item if items changed
                maxErrorDistance2 = double.MaxValue;
            //
            if (!_userPick) maxErrorDistance2 = double.MaxValue;    // must be in a separate if
            //
            if (minDistance > maxErrorDistance2) return -1;
            else
            {
                point = pointOut;
                return globalCellId;
            }
        }
        private int[] GetEdgeNodeIds(double[] point, int globalCellId, vtkCell cell, vtkCellLocator cellLocator)
        {
            // Cell face
            int[] globalCellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);
            vtkMaxActorData cellFaceData = Controller_GetCellFaceActorData(globalCellId, globalCellFaceNodeIds); // works on undeformed mesh
            // Closest edge cell
            int[] nodeIds;
            GetClosestEdgeCell(point, cellFaceData, out nodeIds, out _, out _, out _);
            //
            return nodeIds;
        }
        private int[] GetCellFaceNodeIds(vtkCell cell, vtkCellLocator cellLocator)
        {
            vtkDataArray globalIds = cellLocator.GetDataSet().GetPointData().GetGlobalIds();
            //
            int[] faceGlobalNodeIds = new int[cell.GetPointIds().GetNumberOfIds()];
            for (int i = 0; i < faceGlobalNodeIds.Length; i++)
            {
                faceGlobalNodeIds[i] = (int)globalIds.GetTuple1(cell.GetPointId(i));
            }
            //
            return faceGlobalNodeIds;
        }
        private int GetNodeIdOnCellFaceClosestToPoint(double[] point)
        {
            vtkCell cell;
            vtkCellLocator cellLocator;
            GetGlobalCellIdClosestTo3DPoint(ref point, out cell, out cellLocator);
            int[] globalCellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);

            double[][] coor = GetCellFaceNodeCoor(cell);

            double distance;
            double minDistance = double.MaxValue;
            int minId = -1;

            for (int i = 0; i < coor.Length; i++)
            {
                distance = Math.Pow(coor[i][0] - point[0], 2) + Math.Pow(coor[i][1] - point[1], 2) +
                           Math.Pow(coor[i][2] - point[2], 2);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    minId = i;
                }
            }

            int globalPointId = globalCellFaceNodeIds[minId];

            return globalPointId;
        }
        private double[][] GetCellFaceNodeCoor(vtkCell cell)
        {
            vtkPoints points = cell.GetPoints();
            double[][] coor = new double[points.GetNumberOfPoints()][];

            for (int i = 0; i < coor.Length; i++)
            {
                coor[i] = points.GetPoint(i);
            }
            return coor;
        }
        public void GetGeometryPickProperties(double[] point, out int elementId, out int[] edgeNodeIds, out int[] cellFaceNodeIds)
        {
            // Turn rendering off in order to 
            vtkCellLocator cellLocator;
            vtkCell cell;
            elementId = GetGlobalCellIdClosestTo3DPoint(ref point, out cell, out cellLocator);
            edgeNodeIds = GetEdgeNodeIds(point, elementId, cell, cellLocator);
            cellFaceNodeIds = GetCellFaceNodeIds(cell, cellLocator);
        }
        public double GetSelectionPrecision()
        {
            return vtkInteractorStyleControl.DisplayToWorldScale(_renderer, _selectionDistance);
        }
        public void SetSelectionDistance(int distanceInPixels)
        {
            _selectionDistance = Math.Max(1, distanceInPixels);
        }

        // Item selection - Frustum                                                                                                 
        public void GetPointAndCellIdsInsideFrustum(double[][] planeParameters, out int[] pointIds, out int[] cellIds)
        {
            vtkDataArray normals = vtkDoubleArray.New();
            normals.SetNumberOfComponents(3);
            vtkPoints points = vtkPoints.New();
            //
            for (int i = 0; i < planeParameters.Length; i++)
            {
                normals.InsertNextTuple3(planeParameters[i][3], planeParameters[i][4], planeParameters[i][5]);
                points.InsertNextPoint(planeParameters[i][0], planeParameters[i][1], planeParameters[i][2]);
            }
            //
            vtkPlanes planes = vtkPlanes.New();
            planes.SetPoints(points);
            planes.SetNormals(normals);
            //
            pointIds = new int[0];
            cellIds = new int[0];
            //
            bool visible;
            CaeMesh.BoundingBox bb;
            vtkExtractSelectedFrustum extractor;
            vtkUnstructuredGrid grid;
            vtkUnsignedLongArray globalPointIdsDataArray;
            vtkUnsignedLongArray globalCellIdsDataArray;
            HashSet<int> globalPointIds = new HashSet<int>();
            HashSet<int> globalCellIds = new HashSet<int>();
            vtkCellLocator locator;
            //
            foreach (var entry in _actors)
            {
                visible = entry.Value.Geometry.GetVisibility() != 0;
                // Override visibility on regeneration
                if (_selectableActorsFilter != null) visible = _selectableActorsFilter.Contains(entry.Value.Name);
                // Skip invisible actors
                if (visible)
                {
                    locator = entry.Value.FrustumCellLocator;
                    if (locator != null)
                    {
                        bb = new CaeMesh.BoundingBox(locator.GetDataSet().GetBounds());
                        switch (bb.TestBoundsAgainstFrustum(planeParameters))
                        {
                            case FrustumBoxResultEnum.Outside: continue;
                            case FrustumBoxResultEnum.Inside:
                                AddAllGlobalPointIds(locator.GetDataSet(), globalPointIds);
                                AddAllGlobalCellIds(locator.GetDataSet(), globalCellIds);
                                continue;
                            case FrustumBoxResultEnum.Intersecting:
                                break;
                        }
                        // Inside points                
                        extractor = vtkExtractSelectedFrustum.New(); // must be inside the loop
                        extractor.SetFieldType((int)vtkSelectionField.POINT);
                        extractor.SetContainingCells(0);
                        extractor.SetInput(0, locator.GetDataSet());
                        extractor.SetFrustum(planes);
                        extractor.Update();
                        //
                        grid = (vtkUnstructuredGrid)extractor.GetOutput();
                        globalPointIdsDataArray = (vtkUnsignedLongArray)grid.GetPointData().GetGlobalIds();
                        if (globalPointIdsDataArray != null)
                        {
                            for (int i = 0; i < globalPointIdsDataArray.GetNumberOfTuples(); i++)
                            {
                                globalPointIds.Add((int)globalPointIdsDataArray.GetValue(i));
                            }
                        }
                        // Inside and border cells      
                        extractor.SetFieldType((int)vtkSelectionField.CELL);
                        extractor.Update();
                        //
                        grid = (vtkUnstructuredGrid)extractor.GetOutput();
                        globalCellIdsDataArray = (vtkUnsignedLongArray)grid.GetCellData().GetGlobalIds();
                        if (globalCellIdsDataArray != null)
                        {
                            for (int i = 0; i < globalCellIdsDataArray.GetNumberOfTuples(); i++)
                            {
                                globalCellIds.Add((int)globalCellIdsDataArray.GetValue(i));
                            }
                        }
                    }
                }
            }
            //
            pointIds = globalPointIds.ToArray();
            cellIds = globalCellIds.ToArray();
        }
        static void AddAllGlobalPointIds(vtkDataSet dataSet, HashSet<int> ids)
        {
            var arr = (vtkUnsignedLongArray)dataSet.GetPointData().GetGlobalIds();
            if (arr == null) return;

            for (int i = 0; i < arr.GetNumberOfTuples(); i++)
                ids.Add((int)arr.GetValue(i));
        }
        static void AddAllGlobalCellIds(vtkDataSet dataSet, HashSet<int> ids)
        {
            var arr = (vtkUnsignedLongArray)dataSet.GetCellData().GetGlobalIds();
            if (arr == null) return;

            for (int i = 0; i < arr.GetNumberOfTuples(); i++)
                ids.Add((int)arr.GetValue(i));
        }
        #endregion #################################################################################################################

        // Private methods                                                                                                          
        private void InitializeControl()
        {
            // Render window
            _renderWindow.PointSmoothingOn();
            _renderWindow.LineSmoothingOn();
            //_renderWindow.PolygonSmoothingOn();
            _renderWindow.SetMultiSamples(2);
            // Camera lights are defined in camera coordinate system where:
            // camera is located at (0, 0, 1), the camera is looking at (0, 0, 0), and up is (0, 1, 0).
            _light1 = vtkLight.New();
            _light1.SetPosition(-1, 1, 1);
            _light1.SetFocalPoint(0, 0, 0);
            _light1.SetColor(1, 1, 1);
            _light1.SetIntensity(0.4);
            _light1.SetLightTypeToCameraLight();
            _renderer.AddLight(_light1);
            _overlayRenderer.AddLight(_light1);
            _selectionRenderer.AddLight(_light1);
            //
            _light2 = vtkLight.New();
            _light2.SetPosition(1, 1, 1);
            _light2.SetFocalPoint(0, 0, 0);
            _light2.SetColor(1, 1, 1);
            _light2.SetIntensity(0.4);
            _light2.SetLightTypeToCameraLight();
            _renderer.AddLight(_light2);
            _overlayRenderer.AddLight(_light2);
            _selectionRenderer.AddLight(_light2);
            //
            _light3 = vtkLight.New();
            _light3.SetPosition(0, -1, 0);
            _light3.SetFocalPoint(0, 0, 0);
            _light3.SetColor(1, 1, 1);
            _light3.SetIntensity(0.4);
            _light3.SetLightTypeToCameraLight();
            _renderer.AddLight(_light3);
            _overlayRenderer.AddLight(_light3);
            _selectionRenderer.AddLight(_light3);
            // Interactor style
            _style = vtkInteractorStyleControl.New();
            ApplyNavigationSensitivity();
            _style.SetWidgetHighlightEnabled(_widgetHighlightEnabled);
            _style.AutoAdjustCameraClippingRangeOn();
            _style.SetDefaultRenderer(_renderer);
            _style.SetCurrentRenderer(_renderer);
            _style.SetOverlayRenderer(_overlayRenderer);
            _style.SetSelectionRenderer(_selectionRenderer);
            _style.GetPickPoint = this.GetPickPoint;
            _style.SetBoxZoom = SetBoxZoom;
            _style.BoxZoomEnded = BoxZoomEnded;
            //
            _style.PointPickedOnMouseMoveEvt += Style_PointPickedOnMouseMoveEvt;
            _style.PointPickedOnLeftUpEvt += style_PointPickedOnLeftUpEvt;
            _style.ClearCurrentMouseSelection += ClearCurrentMouseSelection;
            _style.RightButtonPressEvent += style_RightButtonPressEvent;
            _style.MouseInteractionEvent += style_MouseInteractionEvent;
            _style.RotationPointChanged += _style_RotationPointChanged;
            _style.ZoomChangedEvent += style_ZoomChangedEvent;
            _style.KeyPressEvt += style_KeyPressEvt;
            _style.LeaveEvt += style_LeaveEvt;
            _style.EnterEvt += style_EnterEvt;
            _renderWindowInteractor.SetInteractorStyle(_style);
            _style.ResetRotationCenter();
            _renderWindowInteractor.ModifiedEvt += _renderWindowInteractor_ModifiedEvt;
            // Background
            _renderer.SetBackground(0.4, 0.4, 0.4);     // bottom
            _renderer.SetBackground2(0.8, 0.8, 0.8);    // top
            _renderer.SetGradientBackground(true);
            // Camera
            vtkCamera camera = _renderer.GetActiveCamera();
            camera.SetParallelProjection(1);
            // Offset lines from polygons
            vtkPolyDataMapper.SetResolveCoincidentTopologyToPolygonOffset();
            // Scale bar
            _scaleWidget = new vtkMaxScaleWidget();
            _scaleWidget.SetInteractor(_widgetRenderer, _renderWindowInteractor);
            _scaleWidget.SetWidth(400);
            _scaleWidget.SetHorizontallyRelativePosition(320, 0);
            _scaleWidget.SetBorderColor(0, 0, 0);
            _scaleWidget.SetTextProperty(CreateNewTextProperty());
            _scaleWidget.SetPadding(5);
            _scaleWidget.GetBackgroundProperty().SetColor(1, 1, 1);
            _scaleWidget.VisibilityOn();
            _scaleWidget.BackgroundVisibilityOff();
            _scaleWidget.BorderVisibilityOff();
            // Scalar bar
            InitializeScalarBar();
            // Color bar
            _colorBarWidget = new vtkMaxColorBarWidget();
            _colorBarWidget.SetInteractor(_widgetRenderer, _renderWindowInteractor);
            _colorBarWidget.SetTextProperty(CreateNewTextProperty());
            _colorBarWidget.SetPadding(15);
            _colorBarWidget.VisibilityOn();
            _colorBarWidget.BackgroundVisibilityOn();
            _colorBarWidget.BorderVisibilityOn();
            _colorBarWidget.SetBackgroundColor(1, 1, 1);
            _colorBarWidget.MouseDoubleClick += colorBarWidget_DoubleClicked;
            // Status block
            _statusBlockWidget = new vtkMaxStatusBlockWidget();
            _statusBlockWidget.SetInteractor(_widgetRenderer, _renderWindowInteractor);
            _statusBlockWidget.SetTextProperty(CreateNewTextProperty());
            _statusBlockWidget.SetPadding(5);
            _statusBlockWidget.GetBackgroundProperty().SetColor(1, 1, 1);
            _statusBlockWidget.BackgroundVisibilityOff();
            _statusBlockWidget.VisibilityOff();
            _statusBlockWidget.MouseDoubleClick += statusBlockWidget_DoubleClicked;
            // Probe widget
            _probeWidget = new vtkMaxTextWidget();
            _probeWidget.SetInteractor(_widgetRenderer, _renderWindowInteractor);
            _probeWidget.SetBorderColor(0, 0, 0);
            _probeWidget.SetTextProperty(CreateNewTextProperty());
            _probeWidget.SetPadding(5);
            _probeWidget.GetBackgroundProperty().SetColor(1, 1, 1);
            _probeWidget.VisibilityOff();
            // Arrow widgets
            _arrowWidgets = new Dictionary<string, vtkMaxTextWithArrowWidget>();
            // Coordinate system
            _coorSysWidget = new vtkMaxCoorSysWidget();
            _coorSysWidget.SetInteractor(_widgetRenderer, _renderWindowInteractor);
            _coorSysWidget.SetTextProperty(CreateNewTextProperty());
            _coorSysWidget.BackgroundVisibilityOff();
            _coorSysWidget.BorderVisibilityOff();
            _coorSysWidget.TargetClicked += coorSysWidget_TargetClicked;
            _coorSysWidget.HoverChanged += coorSysWidget_HoverChanged;
            SetCoorSysVisibility(_drawCoorSys);

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
            // Lookup table
            _lookupTable = vtkLookupTable.New();
            // Scalar bar
            _scalarBarWidget = new vtkMaxScalarBarWidget();
            _scalarBarWidget.SetInteractor(_widgetRenderer, _renderWindowInteractor);
            _scalarBarWidget.SetTextProperty(CreateNewTextProperty());
            _scalarBarWidget.CreateLookupTable(GetColorTransferFunction(), 0, 1);
            _scalarBarWidget.SetLabelFormat("E3");
            _scalarBarWidget.SetPadding(15);
            _scalarBarWidget.VisibilityOn();
            _scalarBarWidget.BackgroundVisibilityOn();
            _scalarBarWidget.BorderVisibilityOn();
            _scalarBarWidget.SetBackgroundColor(1, 1, 1);
            //
            _scalarBarWidget.MouseDoubleClick += scalarBarWidget_DoubleClicked;
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            ((Timer)sender).Enabled = false;
            AddDiskAnimation();
        }
        private void style_KeyPressEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            if (_scalarBarWidget.GetVisibility() == 1) _scalarBarWidget.OnRenderWindowModified();
        }
        private vtkTextProperty CreateNewTextProperty()
        {
            vtkTextProperty textProperty = vtkTextProperty.New();
            textProperty.SetColor(0, 0, 0);
            textProperty.SetFontFamilyToArial();
            //textProperty.SetFontFamilyAsString("Arial Nova");
            textProperty.SetFontSize(16);
            textProperty.SetLineOffset(-Math.Round(textProperty.GetFontSize() / 7.0));
            textProperty.SetLineSpacing(1.5);
            return textProperty;
        }
        private vtkActor GetActorEdgesFromGrid(vtkUnstructuredGrid uGridEdges)
        {
            if (uGridEdges.GetNumberOfCells() <= 0) return null;

            // extract visualization surface of the unstructured grid
            vtkUnstructuredGridGeometryFilter filter = vtkUnstructuredGridGeometryFilter.New();
            filter.SetInput(uGridEdges);
            filter.Update();

            // extract edges of the visualization surface
            vtkExtractEdges extractEdges = vtkExtractEdges.New();
            extractEdges.SetInput(filter.GetOutput());
            extractEdges.Update();

            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInput(extractEdges.GetOutput());

            vtkProperty prop = vtkProperty.New();
            //prop.SetRepresentationToWireframe();
            prop.SetColor(0, 0, 0);
            prop.SetLighting(false);

            vtkActor edges = vtkActor.New();
            edges.SetMapper(mapper);
            edges.SetProperty(prop);
            edges.PickableOff();

            return edges;
        }

        // Formatting
        private void ApplySelectionFormattingToActor(vtkMaxActor actor)
        {
            if (actor == null || actor.GeometryMapper == null) return;
            // Get actor cell type
            vtkCellTypes types = vtkCellTypes.New();
            actor.GeometryMapper.GetInput().GetCellTypes(types);
            int num = types.GetNumberOfTypes();
            byte cellType;
            bool onlyEdges = true;
            bool onlyNodes = true;
            for (int i = 0; i < num; i++)
            {
                cellType = types.GetCellType(i);
                if (cellType != (int)vtkCellType.VTK_LINE && cellType != (int)vtkCellType.VTK_QUADRATIC_EDGE)
                {
                    onlyEdges = false;
                    //break;
                }
                if (cellType != (int)vtkCellType.VTK_VERTEX)
                {
                    onlyNodes = false;
                    //break;
                }
            }
            //
            double k;
            double ambient;
            double diffuse;
            if (onlyNodes || onlyEdges)
            {
                k = 1;
                ambient = 1;
                diffuse = 1;
            }
            else
            {
                k = 0.7;
                ambient = 0.6;
                diffuse = 0.6;
            }
            // If the point size was already set, do not change it
            // If (actor.GeometryProperty.GetPointSize() <= 1)
            actor.GeometryProperty.SetPointSize(actor.GeometryProperty.GetPointSize() + 2);
            //
            Color highlightColor;
            if (actor.UseSecondaryHighlightColor) highlightColor = _secondaryHighlightColor;
            else highlightColor = _primaryHighlightColor;
            //
            actor.GeometryProperty.SetColor(highlightColor.R / 255d * k,
                                            highlightColor.G / 255d * k,
                                            highlightColor.B / 255d * k);
            actor.GeometryProperty.SetAmbient(ambient);    // color background - even away from light
            actor.GeometryProperty.SetDiffuse(diffuse);    // color from the lights
            if (actor.GeometryProperty.GetLineWidth() == 1)
                actor.GeometryProperty.SetLineWidth(2.2f);
            actor.GeometryProperty.SetOpacity(1);
            //property.BackfaceCullingOn();
            actor.Geometry.PickableOff();
        }
        private void ApplySelectionFormattingToCaptionActor(vtkMaxCaptionActor actor)
        {
            Color highlightColor;
            if (actor.UseSecondaryHighlightColor) highlightColor = _secondaryHighlightColor;
            else highlightColor = _primaryHighlightColor;
            //
            vtkTextProperty tp;
            if (actor.CaptionActor is vtkCaptionActor2D ca2D)
            {
                tp = ca2D.GetCaptionTextProperty();
                tp.SetColor(highlightColor.R / 255d, highlightColor.G / 255d, highlightColor.B / 255d);
                //tp.BoldOn();
                ca2D.SetCaptionTextProperty(tp);
            }
        }
        private void ApplySymbolFormattingToActor(vtkMaxActor actor)
        {
            actor.BackfaceCulling = true;
            actor.Ambient = 0.6;
            actor.Diffuse = 0.6;
        }
        //
        private vtkLookupTable PrepareActorLookupTable(double scalarRangeMin, double scalarRangeMax,
                                                       int overrideNumOfColors = -1)
        {
            double min = scalarRangeMin;
            double max = scalarRangeMax;
            bool addMinColor = false;
            bool addMaxColor = false;
            int numOfColors = _colorSpectrum.Mode == vtkColorSpectrumMode.Smooth
                ? vtkMaxColorSpectrum.SmoothNumberOfColors
                : _colorSpectrum.NumberOfColors;
            if (overrideNumOfColors > 0) numOfColors = overrideNumOfColors;
            //
            vtkColorTransferFunction ctf = GetColorTransferFunction();
            // Determine the need for min/max color
            if (_colorSpectrum.MinMaxType == vtkColorSpectrumMinMaxType.Manual)
            {
                min = Math.Min(scalarRangeMin, _colorSpectrum.MinUserValue);
                max = Math.Max(scalarRangeMax, _colorSpectrum.MaxUserValue);
                //
                if (_colorSpectrum.MinUserValue > min && max != min)
                {
                    addMinColor = true;
                    min = _colorSpectrum.MinUserValue;
                }
                if (_colorSpectrum.MaxUserValue < max && max != min)
                {
                    addMaxColor = true;
                    max = _colorSpectrum.MaxUserValue;
                }
            }
            //
            double[] color;
            double delta;
            vtkLookupTable lookupTable = vtkLookupTable.New(); // this is a fix for a _lookupTable.DeepCopy later on
            //
            if (addMinColor || addMaxColor)
            {
                int numOfAllColors = numOfColors;
                delta = (max - min) / numOfColors;
                //
                if (addMinColor)
                {
                    numOfAllColors++;
                    min -= delta;
                }
                if (addMaxColor)
                {
                    numOfAllColors++;
                    max += delta;
                }
                //
                lookupTable.SetTableRange(min, max);
                lookupTable.SetNumberOfColors(numOfAllColors);
                // Below range color
                int count = 0;
                if (addMinColor)
                {
                    lookupTable.SetTableValue(count++, _colorSpectrum.MinColor.R / 255.0,
                                                        _colorSpectrum.MinColor.G / 255.0,
                                                        _colorSpectrum.MinColor.B / 255.0, 1.0);        //R,G,B,A
                }
                // Between range color
                double rangeDelta = 1.0 / (numOfColors - 1);
                for (int i = 0; i < numOfColors; i++)
                {
                    color = ctf.GetColor(i * rangeDelta);
                    lookupTable.SetTableValue(count++, color[0], color[1], color[2], 1.0);             //R,G,B,A
                }
                // Above range color
                if (addMaxColor)
                {
                    lookupTable.SetTableValue(count++, _colorSpectrum.MaxColor.R / 255.0,
                                                        _colorSpectrum.MaxColor.G / 255.0,
                                                        _colorSpectrum.MaxColor.B / 255.0, 1.0);        //R,G,B,A
                }
            }
            else
            {
                lookupTable.SetTableRange(min, max);
                lookupTable.SetNumberOfColors(numOfColors);
                delta = 1.0 / (lookupTable.GetNumberOfColors() - 1);
                for (int i = 0; i < lookupTable.GetNumberOfColors(); i++)
                {
                    color = ctf.GetColor(i * delta);
                    lookupTable.SetTableValue(i, color[0], color[1], color[2], 1.0);                   //R,G,B,A
                }
            }
            //
            return lookupTable;
        }
        private vtkColorTransferFunction GetColorTransferFunction()
        {
            vtkColorTransferFunction ctf = vtkColorTransferFunction.New();
            List<double[]> rgbPoints = new List<double[]>();
            //
            // https://colorbrewer2.org/
            //
            if (_colorSpectrum.Type == vtkColorSpectrumType.CoolWarm)
            {
                // Cool-Warm
                //http://aplotnikov.com/2016/simple-visualization-of-unstructured-grid-quality/
                //http://www.kennethmoreland.com/color-maps/
                ctf.SetColorSpaceToDiverging();
                rgbPoints.Add(new double[] { 0, 0.230, 0.299, 0.754 });
                rgbPoints.Add(new double[] { 1, 0.706, 0.016, 0.150 });
                // For brightness = 0.2
                //rgbPoints.Add(new double[] { 0, 0.038, 0.124, 0.693 });
                //rgbPoints.Add(new double[] { 1, 0.632, 0.0, 0.0 });
            }
            else if (_colorSpectrum.Type == vtkColorSpectrumType.Rainbow)
            {
                double b = 0.0;
                ctf.SetColorSpaceToHSV();
                rgbPoints.Add(new double[] { 0.000, b, b, 1 });
                rgbPoints.Add(new double[] { 0.250, b, 1, 1 });
                rgbPoints.Add(new double[] { 0.500, b, 1, b });
                rgbPoints.Add(new double[] { 0.750, 1, 1, b });
                rgbPoints.Add(new double[] { 1.000, 1, b, b });
            }
            else if (_colorSpectrum.Type == vtkColorSpectrumType.RainbowDesaturated)
            {
                ctf.SetColorSpaceToRGB();
                rgbPoints.Add(new double[] { 0, 0.278, 0.278, 0.788 });
                rgbPoints.Add(new double[] { 0.143, 0.02, 0.02, 0.396 });
                rgbPoints.Add(new double[] { 0.286, 0, 0.871, 0.918 });
                rgbPoints.Add(new double[] { 0.429, 0, 0.604, 0.2 });
                rgbPoints.Add(new double[] { 0.571, 1, 0.875, 0 });
                rgbPoints.Add(new double[] { 0.714, 1, 0.329, 0 });
                rgbPoints.Add(new double[] { 0.857, 0.451, 0.02, 0.02 });
                rgbPoints.Add(new double[] { 1, 0.878, 0.302, 0.302 });
            }
            else if (_colorSpectrum.Type == vtkColorSpectrumType.Jet)
            {
                ctf.SetColorSpaceToRGB();
                rgbPoints.Add(new double[] { 0.000, 0.000, 0.000, 0.500 });
                rgbPoints.Add(new double[] { 0.125, 0.000, 0.000, 1.000 });
                rgbPoints.Add(new double[] { 0.375, 0.000, 1.000, 1.000 });
                rgbPoints.Add(new double[] { 0.500, 0.000, 1.000, 0.000 });
                rgbPoints.Add(new double[] { 0.625, 1.000, 1.000, 0.000 });
                rgbPoints.Add(new double[] { 0.875, 1.000, 0.000, 0.000 });
                rgbPoints.Add(new double[] { 1.000, 0.500, 0.000, 0.000 });
            }
            else if (_colorSpectrum.Type == vtkColorSpectrumType.Turbo)
            {
                ctf.SetColorSpaceToRGB();
                rgbPoints.Add(new double[] { 0.000, 0.18995, 0.07176, 0.23217 });
                rgbPoints.Add(new double[] { 0.067, 0.25107, 0.25237, 0.63374 });
                rgbPoints.Add(new double[] { 0.133, 0.27698, 0.46153, 0.93309 });
                rgbPoints.Add(new double[] { 0.200, 0.24427, 0.60937, 0.99697 });
                rgbPoints.Add(new double[] { 0.267, 0.17223, 0.71680, 0.93981 });
                rgbPoints.Add(new double[] { 0.333, 0.10342, 0.89600, 0.71500 });
                rgbPoints.Add(new double[] { 0.400, 0.26180, 0.96765, 0.52981 });
                rgbPoints.Add(new double[] { 0.467, 0.47375, 0.99755, 0.34963 });
                rgbPoints.Add(new double[] { 0.533, 0.69525, 0.97610, 0.21294 });
                rgbPoints.Add(new double[] { 0.600, 0.88331, 0.86553, 0.21719 });
                rgbPoints.Add(new double[] { 0.667, 0.98000, 0.73000, 0.22161 });
                rgbPoints.Add(new double[] { 0.733, 0.99297, 0.55214, 0.15417 });
                rgbPoints.Add(new double[] { 0.800, 0.94084, 0.35566, 0.07031 });
                rgbPoints.Add(new double[] { 0.867, 0.84662, 0.21407, 0.02487 });
                rgbPoints.Add(new double[] { 0.933, 0.68602, 0.09536, 0.00481 });
                rgbPoints.Add(new double[] { 1.000, 0.47960, 0.01583, 0.01055 });
            }
            else if (_colorSpectrum.Type == vtkColorSpectrumType.Warm)
            {
                ctf.SetColorSpaceToRGB();
                rgbPoints.Add(new double[] { 0, 0.549, 0.176, 0.016 });
                rgbPoints.Add(new double[] { 0.143, 0.8, 0.298, 0.008 });
                rgbPoints.Add(new double[] { 0.286, 0.925, 0.439, 0.078 });
                rgbPoints.Add(new double[] { 0.429, 0.996, 0.6, 0.161 });
                rgbPoints.Add(new double[] { 0.571, 0.996, 0.769, 0.31 });
                rgbPoints.Add(new double[] { 0.714, 0.996, 0.89, 0.569 });
                rgbPoints.Add(new double[] { 0.857, 1, 0.969, 0.737 });
                rgbPoints.Add(new double[] { 1, 1, 1, 0.898 });
            }
            else if (_colorSpectrum.Type == vtkColorSpectrumType.Cool)
            {
                ctf.SetColorSpaceToRGB();
                rgbPoints.Add(new double[] { 0, 0.031, 0.271, 0.58 });
                rgbPoints.Add(new double[] { 0.143, 0.129, 0.443, 0.71 });
                rgbPoints.Add(new double[] { 0.286, 0.259, 0.573, 0.776 });
                rgbPoints.Add(new double[] { 0.429, 0.42, 0.682, 0.839 });
                rgbPoints.Add(new double[] { 0.571, 0.62, 0.792, 0.882 });
                rgbPoints.Add(new double[] { 0.714, 0.776, 0.859, 0.937 });
                rgbPoints.Add(new double[] { 0.857, 0.871, 0.922, 0.969 });
                rgbPoints.Add(new double[] { 1, 0.969, 0.984, 1 });
            }
            else if (_colorSpectrum.Type == vtkColorSpectrumType.Cividis)
            {
                ctf.SetColorSpaceToRGB();
                rgbPoints.Add(new double[] { 0, 0, 0.125, 0.297 });
                rgbPoints.Add(new double[] { 0.125, 0.023, 0.211, 0.43 });
                rgbPoints.Add(new double[] { 0.25, 0.254, 0.301, 0.418 });
                rgbPoints.Add(new double[] { 0.375, 0.375, 0.391, 0.43 });
                rgbPoints.Add(new double[] { 0.5, 0.484, 0.48, 0.469 });
                rgbPoints.Add(new double[] { 0.625, 0.609, 0.578, 0.465 });
                rgbPoints.Add(new double[] { 0.75, 0.738, 0.684, 0.43 });
                rgbPoints.Add(new double[] { 0.875, 0.875, 0.793, 0.363 });
                rgbPoints.Add(new double[] { 1, 0.996, 0.91, 0.27 });
            }
            else if (_colorSpectrum.Type == vtkColorSpectrumType.Viridis)
            {
                ctf.SetColorSpaceToRGB();
                rgbPoints.Add(new double[] { 0, 0.267, 0.005, 0.329 });
                rgbPoints.Add(new double[] { 0.143, 0.275, 0.197, 0.497 });
                rgbPoints.Add(new double[] { 0.286, 0.213, 0.359, 0.552 });
                rgbPoints.Add(new double[] { 0.429, 0.153, 0.498, 0.558 });
                rgbPoints.Add(new double[] { 0.571, 0.122, 0.632, 0.531 });
                rgbPoints.Add(new double[] { 0.714, 0.29, 0.759, 0.428 });
                rgbPoints.Add(new double[] { 0.857, 0.622, 0.854, 0.226 });
                rgbPoints.Add(new double[] { 1, 0.993, 0.906, 0.144 });
            }
            else if (_colorSpectrum.Type == vtkColorSpectrumType.Plasma)
            {
                ctf.SetColorSpaceToRGB();
                rgbPoints.Add(new double[] { 0, 0.05, 0.03, 0.528 });
                rgbPoints.Add(new double[] { 0.143, 0.328, 0.007, 0.64 });
                rgbPoints.Add(new double[] { 0.286, 0.545, 0.038, 0.647 });
                rgbPoints.Add(new double[] { 0.429, 0.725, 0.197, 0.538 });
                rgbPoints.Add(new double[] { 0.571, 0.859, 0.359, 0.408 });
                rgbPoints.Add(new double[] { 0.714, 0.956, 0.534, 0.285 });
                rgbPoints.Add(new double[] { 0.857, 0.995, 0.738, 0.167 });
                rgbPoints.Add(new double[] { 1, 0.94, 0.975, 0.131 });
            }
            else if (_colorSpectrum.Type == vtkColorSpectrumType.BlackBody)
            {
                ctf.SetColorSpaceToLab();
                rgbPoints.Add(new double[] { 0, 0, 0, 0 });
                rgbPoints.Add(new double[] { 0.143, 0.257, 0.089, 0.069 });
                rgbPoints.Add(new double[] { 0.286, 0.502, 0.123, 0.107 });
                rgbPoints.Add(new double[] { 0.429, 0.735, 0.198, 0.124 });
                rgbPoints.Add(new double[] { 0.571, 0.877, 0.395, 0.038 });
                rgbPoints.Add(new double[] { 0.714, 0.911, 0.632, 0.1 });
                rgbPoints.Add(new double[] { 0.857, 0.907, 0.855, 0.189 });
                rgbPoints.Add(new double[] { 1, 1, 1, 1 });
            }
            else if (_colorSpectrum.Type == vtkColorSpectrumType.Inferno)
            {
                ctf.SetColorSpaceToLab();
                rgbPoints.Add(new double[] { 0, 0.001, 0, 0.014 });
                rgbPoints.Add(new double[] { 0.143, 0.159, 0.044, 0.329 });
                rgbPoints.Add(new double[] { 0.286, 0.397, 0.083, 0.433 });
                rgbPoints.Add(new double[] { 0.429, 0.623, 0.165, 0.388 });
                rgbPoints.Add(new double[] { 0.571, 0.831, 0.283, 0.259 });
                rgbPoints.Add(new double[] { 0.714, 0.962, 0.49, 0.084 });
                rgbPoints.Add(new double[] { 0.857, 0.982, 0.756, 0.153 });
                rgbPoints.Add(new double[] { 1, 0.988, 0.998, 0.645 });
            }
            else if (_colorSpectrum.Type == vtkColorSpectrumType.Kindlmann)
            {
                ctf.SetColorSpaceToLab();
                rgbPoints.Add(new double[] { 0, 0, 0, 0 });
                rgbPoints.Add(new double[] { 0.143, 0.14, 0.022, 0.46 });
                rgbPoints.Add(new double[] { 0.286, 0.028, 0.243, 0.587 });
                rgbPoints.Add(new double[] { 0.429, 0.021, 0.449, 0.382 });
                rgbPoints.Add(new double[] { 0.571, 0.031, 0.625, 0.083 });
                rgbPoints.Add(new double[] { 0.714, 0.44, 0.767, 0.037 });
                rgbPoints.Add(new double[] { 0.857, 0.98, 0.815, 0.572 });
                rgbPoints.Add(new double[] { 1, 1, 1, 1 });
            }
            else if (_colorSpectrum.Type == vtkColorSpectrumType.Grayscale)
            {
                ctf.SetColorSpaceToLab();
                //rgbPoints.Add(new double[] { 0, 0, 0, 0 });
                rgbPoints.Add(new double[] { 0, 0.2, 0.2, 0.2 });
                rgbPoints.Add(new double[] { 1, 1.0, 1.0, 1.0 });
            }
            //
            double n = 0;
            double k = 1;
            double x = _colorSpectrum.ColorBrightness;
            if (_colorSpectrum.ReverseColors)
            {
                n = 1;
                k = -1;
            }

            //
            foreach (var rgbPoint in rgbPoints)
                ctf.AddRGBPoint(k * rgbPoint[0] + n, rgbPoint[1] + x * (1 - rgbPoint[1]),
                                                     rgbPoint[2] + x * (1 - rgbPoint[2]),
                                                     rgbPoint[3] + x * (1 - rgbPoint[3]));
            //
            return ctf;
        }
        //
        private void ApplyEdgesVisibilityAndBackfaceCulling()
        {
            // Visibility functions must use vtkMaxActor.Visible property to determine visibility
            // Base layer
            foreach (var entry in _actors)
            {
                ApplyEdgeVisibilityAndBackfaceCullingToActor(entry.Key, entry.Value.Geometry, entry.Value.GeometryProperty,
                                                             vtkRendererLayer.Base);
                if (entry.Value.ElementEdges != null) ApplyEdgeVisibilityAndBackfaceCullingToActorEdges(entry.Value, entry.Key);
                if (entry.Value.ModelEdges != null) ApplyEdgeVisibilityAndBackfaceCullingToModelEdges(entry.Value, entry.Key);
            }
            // Selection
            foreach (var selectedActor in _selectedActors)
            {
                ApplyEdgeVisibilityAndBackfaceCullingToActor(null, selectedActor.Geometry, selectedActor.GeometryProperty,
                                                             vtkRendererLayer.Selection);
                if (selectedActor.ElementEdges != null) ApplyEdgeVisibilityAndBackfaceCullingToActorEdges(selectedActor, null);
            }
            // Overlay
            foreach (var entry in _overlayActors)
            {
                if (entry.Value.Geometry != null)   // null is for Caption actor
                    ApplyEdgeVisibilityAndBackfaceCullingToActor(null, entry.Value.Geometry, entry.Value.Geometry.GetProperty(),
                                                                 vtkRendererLayer.Overlay);
            }
            //
            RenderScene();
        }
        private void ApplyEdgeVisibilityAndBackfaceCullingToActor(string actorName, vtkActor actor, vtkProperty actorProperty,
                                                                  vtkRendererLayer layer)
        {
            // Visibility functions must use vtkMaxActor.Visible property to determine visibility
            //
            // Skip formatting for the overlay layer
            if (layer == vtkRendererLayer.Overlay) return;
            //
            vtkMaxActor maxActor = null;
            if (layer == vtkRendererLayer.Base)
            {
                if (actorName == null) actorName = GetActorName(actor);
                maxActor = _actors[actorName];
            }
            else if (layer == vtkRendererLayer.Selection)
            {
                maxActor = GetSelectedMaxActor(actor);
            }
            //
            if (maxActor != null && !maxActor.UseEdgeVisibility)
            {
                if (maxActor.VtkMaxActorVisible) actor.VisibilityOn();
                else actor.VisibilityOff();
                return;
            }
            //
            if (_edgesVisibility == vtkEdgesVisibility.ElementEdges) actorProperty.SetInterpolationToFlat();
            else actorProperty.SetInterpolationToPhong();
            //
            if (_edgesVisibility == vtkEdgesVisibility.Wireframe && layer == vtkRendererLayer.Base)
            {
                actor.VisibilityOff();
            }
            else
            {
                actorProperty.SetRepresentationToSurface();
                //
                if (maxActor != null)
                {
                    if (maxActor.VtkMaxActorVisible) actor.VisibilityOn();
                    else actor.VisibilityOff();
                    //
                    if (maxActor.BackfaceCulling) actorProperty.BackfaceCullingOn();
                    else actorProperty.BackfaceCullingOff();
                    //
                    if (maxActor.ActorRepresentation == vtkMaxActorRepresentation.SolidAsShell && _sectionViewData.Active)
                        actorProperty.BackfaceCullingOff();
                }
            }
        }
        private void ApplyEdgeVisibilityAndBackfaceCullingToActorEdges(vtkMaxActor actor, string actorName)
        {
            // Visibility functions must use vtkMaxActor.Visible property to determine visibility
            //
            // Turn off edges for hidden actors
            if (actorName != null && _actors.ContainsKey(actorName) && !_actors[actorName].VtkMaxActorVisible)
            {
                actor.ElementEdges.VisibilityOff();
            }
            else
            {
                // For visible actors
                if (_edgesVisibility == vtkEdgesVisibility.ElementEdges)
                {
                    actor.ElementEdges.VisibilityOn();
                }
                else
                {
                    actor.ElementEdges.VisibilityOff();
                }
            }
            actor.ElementEdgesProperty.BackfaceCullingOn();
        }
        private void ApplyEdgeVisibilityAndBackfaceCullingToModelEdges(vtkMaxActor actor, string actorName)
        {
            // visibility functions must use vtkMaxActor.Visible property to determine visibility

            // turn off edges for hidden actors
            if (actorName != null && _actors.ContainsKey(actorName) && !_actors[actorName].VtkMaxActorVisible)
            {
                actor.ModelEdges.VisibilityOff();
            }
            else
            {
                if (_edgesVisibility == vtkEdgesVisibility.ModelEdges || _edgesVisibility == vtkEdgesVisibility.Wireframe)
                {
                    actor.ModelEdges.VisibilityOn();
                }
                //else if (_edgesVisibility == vtkEdgesVisibility.ElementEdges && !_actors[actorName].BackfaceCulling)
                //{
                //    modelEdges.VisibilityOn();
                //}
                else
                {
                    actor.ModelEdges.VisibilityOff();
                }
                actor.ModelEdgesProperty.BackfaceCullingOn();
            }
        }
        private void ApplyVisibilityToActorNodes()
        {
            // Base layer
            foreach (var entry in _actors)
            {
                if (entry.Value.Nodes == null) continue;
                // Visibility functions must use vtkMaxActor.Visible property to determine visibility
                //
                // Turn off nodes for hidden actors
                if (entry.Key != null && _actors.ContainsKey(entry.Key) && !_actors[entry.Key].VtkMaxActorVisible)
                {
                    entry.Value.Nodes.VisibilityOff();
                }
                else
                {
                    // For visible actors
                    entry.Value.Nodes.VisibilityOn();
                }
            }
        }

        // Public methods                                                                                                          
        #region Views  #############################################################################################################
        public void GetViewParameters(out double[] position, out double[] focalPoint, out double[] viewUp, out double parallelScale)
        {
            vtkCamera camera = _renderer.GetActiveCamera();
            position = camera.GetPosition();
            focalPoint = camera.GetFocalPoint();
            viewUp = camera.GetViewUp();
            parallelScale = camera.GetParallelScale();
        }
        public void SetViewParameters(bool animate, double[] position, double[] focalPoint, double[] viewUp, double parallelScale)
        {
            if (_animating) return;
            _animating = animate;
            //
            if (_drawCameraPath) ClearOverlay();
            //
            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCamera cameraStart = vtkCamera.New();
            cameraStart.DeepCopy(camera);
            //
            camera.SetPosition(position[0], position[1], position[2]);
            camera.SetFocalPoint(focalPoint[0], focalPoint[1], focalPoint[2]);
            camera.SetViewUp(viewUp[0], viewUp[1], viewUp[2]);
            //
            ResetCamera();
            camera.SetParallelScale(parallelScale);
            //
            if (animate) AnimateCamera(cameraStart, camera, camera);
            else RenderScene();
        }
        public void SetFrontBackView(bool animate, bool front)
        {
            if (_animating) return;
            _animating = animate;

            int delta = 1;
            if (!front) delta = -1;

            if (_drawCameraPath) ClearOverlay();

            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCamera cameraStart = vtkCamera.New();
            cameraStart.DeepCopy(camera);

            double[] fPos = camera.GetFocalPoint();
            camera.SetPosition(fPos[0], fPos[1], fPos[2] + delta);
            camera.SetViewUp(0, 1, 0);

            ResetCamera();

            if (animate) AnimateCamera(cameraStart, camera, camera);
            else RenderScene();
        }
        public void SetTopBottomView(bool animate, bool top)
        {
            if (_animating) return;
            _animating = animate;

            int delta = 1;
            if (!top) delta = -1;

            if (_drawCameraPath) ClearOverlay();

            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCamera cameraStart = vtkCamera.New();
            cameraStart.DeepCopy(camera);

            double[] fPos = camera.GetFocalPoint();
            camera.SetPosition(fPos[0], fPos[1] + delta, fPos[2]);
            camera.SetViewUp(0, 0, -delta);

            ResetCamera();

            if (animate) AnimateCamera(cameraStart, camera, camera);
            else RenderScene();
        }
        public void SetLeftRightView(bool animate, bool left)
        {
            if (_animating) return;
            _animating = animate;
            //
            int delta = -1;
            if (!left) delta = 1;
            //
            if (_drawCameraPath) ClearOverlay();
            //
            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCamera cameraStart = vtkCamera.New();
            cameraStart.DeepCopy(camera);
            //
            double[] fPos = camera.GetFocalPoint();
            camera.SetPosition(fPos[0] + delta, fPos[1], fPos[2]);
            camera.SetViewUp(0, 1, 0);
            //
            ResetCamera();
            //
            if (animate) AnimateCamera(cameraStart, camera, camera);
            else RenderScene();
        }
        public void SetAxisView(bool animate, double[] axis)
        {
            if (_animating) return;
            _animating = animate;
            //
            if (_drawCameraPath) ClearOverlay();
            //
            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCamera cameraStart = vtkCamera.New();
            cameraStart.DeepCopy(camera);
            //
            double[] viewUp = GetClosestAxisViewUp(camera, axis);
            //
            double[] fPos = camera.GetFocalPoint();
            camera.SetPosition(fPos[0] + axis[0], fPos[1] + axis[1], fPos[2] + axis[2]);
            camera.SetViewUp(viewUp[0], viewUp[1], viewUp[2]);
            //
            ResetCamera();
            //
            if (animate) AnimateCamera(cameraStart, camera, camera);
            else RenderScene();
        }
        private double[] GetClosestAxisViewUp(vtkCamera camera, double[] axis)
        {
            Vec3D normal = new Vec3D(camera.GetViewPlaneNormal());
            normal.Normalize();
            Vec3D right = Vec3D.CrossProduct(new Vec3D(camera.GetViewUp()), normal);
            right.Normalize();
            Vec3D up = Vec3D.CrossProduct(normal, right);
            // Of the four global axes perpendicular to the new view direction take the one
            // that rotates the current view the least
            Vec3D axisVector = new Vec3D(axis);
            double[] closestViewUp = null;
            double maxScore = -double.MaxValue;
            //
            for (int axisId = 0; axisId < 3; axisId++)
            {
                for (int signId = 0; signId < 2; signId++)
                {
                    double[] viewUp = new double[3];
                    viewUp[axisId] = signId == 0 ? 1 : -1;
                    Vec3D viewUpVector = new Vec3D(viewUp);
                    if (Math.Abs(Vec3D.DotProduct(viewUpVector, axisVector)) > 0.5) continue;
                    //
                    Vec3D viewRightVector = Vec3D.CrossProduct(viewUpVector, axisVector);
                    double score = Vec3D.DotProduct(viewRightVector, right) + Vec3D.DotProduct(viewUpVector, up);
                    if (closestViewUp == null || score > maxScore)
                    {
                        maxScore = score;
                        closestViewUp = viewUp;
                    }
                }
            }
            return closestViewUp;
        }
        //
        public void SetNormalView(bool animate)
        {
            if (_animating) return;
            _animating = animate;
            //
            if (_drawCameraPath) ClearOverlay();
            //
            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCamera cameraStart = vtkCamera.New();
            cameraStart.DeepCopy(camera);
            //
            double[] direction = camera.GetViewPlaneNormal();
            double[] abs = new double[] { Math.Abs(direction[0]), Math.Abs(direction[1]), Math.Abs(direction[2]) };
            int[] ids = new int[] { 0, 1, 2 };
            Array.Sort(abs, ids);
            int maxId = ids[2];
            //
            double[] delta = new double[3];
            // Reverse direction
            delta[maxId] = Math.Abs(direction[maxId]) == 1 ? -Math.Sign(direction[maxId]) : Math.Sign(direction[maxId]);
            //
            double[] fPoint = camera.GetFocalPoint();
            camera.SetPosition(fPoint[0] + delta[0], fPoint[1] + delta[1], fPoint[2] + delta[2]);
            //
            //SetVerticalView(false, false);
            //
            ResetCamera();
            //
            if (animate) AnimateCamera(cameraStart, camera, camera);
            else RenderScene();
        }
        public void SetVerticalView(bool animate, bool updateView)
        {
            if (updateView)
            {
                if (_animating) return;
                _animating = animate;
            }
            //
            if (_drawCameraPath) ClearOverlay();
            //
            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCamera cameraStart = vtkCamera.New();
            cameraStart.DeepCopy(camera);
            //
            double[] closestUp = GetClosestUpView();
            camera.SetViewUp(closestUp[0], closestUp[1], closestUp[2]);
            //
            ResetCamera();
            //
            if (updateView)
            {
                if (animate) AnimateCamera(cameraStart, camera, camera);
                else RenderScene();
            }
        }
        public double[] GetClosestUpView()
        {
            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCamera cameraStart = vtkCamera.New();
            cameraStart.DeepCopy(camera);
            //
            double[] up = camera.GetViewUp();
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
        public void SetIsometricView(bool animate, bool positive)
        {
            if (_animating) return;
            _animating = animate;

            int delta = 1;
            if (!positive) delta = -1;

            if (_drawCameraPath) ClearOverlay();

            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCamera cameraStart = vtkCamera.New();
            cameraStart.DeepCopy(camera);

            SetVerticalView(false, false);

            double[] fPos = camera.GetFocalPoint();
            int q = GetClosestIsoDirectionQuadrant(camera.GetViewPlaneNormal());

            if (q == 1) camera.SetPosition(fPos[0] + delta, fPos[1] + delta, fPos[2] + delta);
            else if (q == 2) camera.SetPosition(fPos[0] - delta, fPos[1] + delta, fPos[2] + delta);
            else if (q == 3) camera.SetPosition(fPos[0] - delta, fPos[1] - delta, fPos[2] + delta);
            else if (q == 4) camera.SetPosition(fPos[0] + delta, fPos[1] - delta, fPos[2] + delta);
            else if (q == 5) camera.SetPosition(fPos[0] + delta, fPos[1] + delta, fPos[2] - delta);
            else if (q == 6) camera.SetPosition(fPos[0] - delta, fPos[1] + delta, fPos[2] - delta);
            else if (q == 7) camera.SetPosition(fPos[0] - delta, fPos[1] - delta, fPos[2] - delta);
            else if (q == 8) camera.SetPosition(fPos[0] + delta, fPos[1] - delta, fPos[2] - delta);

            camera.OrthogonalizeViewUp(); // not needed ?
            double[] up = camera.GetViewUp();

            int maxID = -1;
            double max = -double.MaxValue;
            for (int i = 0; i < up.Length; i++)
            {
                if (Math.Abs(up[i]) > max)
                {
                    max = Math.Abs(up[i]);
                    maxID = i;
                }
            }

            if (maxID == 0) camera.SetViewUp(Math.Sign(up[0]), 0, 0);
            else if (maxID == 1) camera.SetViewUp(0, Math.Sign(up[1]), 0);
            else camera.SetViewUp(0, 0, Math.Sign(up[2]));

            ResetCamera();

            if (animate) AnimateCamera(cameraStart, camera, camera);
            else RenderScene();
        }
        public void SetZoomFactor(double factor)
        {
            vtkCamera camera = _renderer.GetActiveCamera();
            camera.SetParallelScale(factor);
        }
        public void AnimateZoomToFactor(double factor, int timeMs)
        {
            if (_animating) return;
            _animating = true;
            //
            if (_drawCameraPath) ClearOverlay();
            //
            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCamera cameraStart = vtkCamera.New();
            cameraStart.DeepCopy(camera);
            //
            SetZoomFactor(factor);
            //
            AnimateCamera(cameraStart, camera, camera, timeMs);
        }
        public void SetZoomToFit(bool animate)
        {
            if (_animating) return;
            _animating = animate;
            //
            if (_drawCameraPath) ClearOverlay();
            //
            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCamera cameraStart = vtkCamera.New();
            cameraStart.DeepCopy(camera);
            //
            ResetCamera();
            //_renderWindowInteractor.Modified(); // this updates the vtkMax annotation objects
            if (animate) AnimateCamera(cameraStart, camera, camera);
            else RenderScene();
        }
        public void SetBoxZoom(int[] point1, int[] point2, bool animate)
        {
            vtkCamera camera = _renderer.GetActiveCamera();
            vtkCamera cameraStart = vtkCamera.New();
            cameraStart.DeepCopy(camera);
            //
            int[] windowSize = _renderWindowInteractor.GetRenderWindow().GetSize();
            // Center
            int[] selectionCenter = new int[] { (point2[0] + point1[0]) / 2, (point2[1] + point1[1]) / 2 };
            int[] windowCenter = new int[] { windowSize[0] / 2, windowSize[1] / 2 };
            double[] selectionCenter3D = 
                vtkInteractorStyleControl.DisplayToWorld(_renderer, new double[] { selectionCenter[0], selectionCenter[1] });
            double[] windowCenter3D =
                vtkInteractorStyleControl.DisplayToWorld(_renderer, new double[] { windowCenter[0], windowCenter[1] });
            // Zoom
            int[] selectionSize = new int[] { Math.Abs(point2[0] - point1[0]), Math.Abs(point2[1] - point1[1]) };
            if (selectionSize[0] == 0 || selectionSize[1] == 1) return;
            //
            double widthZoomFactor = (double)windowSize[0] / (double)selectionSize[0];
            double heightZoomFactor = (double)windowSize[1] / (double)selectionSize[1];
            double zoomFactor = Math.Min(widthZoomFactor, heightZoomFactor);
            // Camera
            camera.SetParallelScale(camera.GetParallelScale() / zoomFactor);
            double[] motionVector = new double[3];
            motionVector[0] = selectionCenter3D[0] - windowCenter3D[0];
            motionVector[1] = selectionCenter3D[1] - windowCenter3D[1];
            motionVector[2] = selectionCenter3D[2] - windowCenter3D[2];
            //
            double[] cPos = camera.GetPosition();
            double[] fPos = camera.GetFocalPoint();
            //
            camera.SetPosition(cPos[0] + motionVector[0], cPos[1] + motionVector[1], cPos[2] + motionVector[2]);
            camera.SetFocalPoint(fPos[0] + motionVector[0], fPos[1] + motionVector[1], fPos[2] + motionVector[2]);
            //
            if (animate) AnimateCamera(cameraStart, camera, camera);
            else RenderScene();
        }
        public void BoxZoomEnded()
        {
            Form_BoxZoomEnded?.Invoke();
        }
        //
        private void _style_RotationPointChanged(double[] rotationPoint)
        {
            RotationPointChanged?.Invoke(rotationPoint);
        }
        public void SetRotationPoint(double[] rotationPoint)
        {
            _style.SetRotationPoint(rotationPoint);
        }
        //
        private void ResetCamera()
        {
            vtkCamera cam = _renderer.GetActiveCamera();
            //
            _renderer.ResetCamera();
            double[] position1 = cam.GetPosition();
            double[] focalPoint1 = cam.GetFocalPoint();
            double scale1 = cam.GetParallelScale();
            //
            _overlayRenderer.ResetCamera();
            //int n = _overlayRenderer.GetNumberOfPropsRendered();
            //double[] position2 = cam.GetPosition();
            double[] focalPoint2 = cam.GetFocalPoint();
            double scale2 = cam.GetParallelScale();
            // Aspect
            double aspect = _renderWindow.GetSize()[0] / (double)_renderWindow.GetSize()[1];
            // Y bounds
            double yMax = Math.Max(focalPoint1[1] + scale1, focalPoint2[1] + scale2);
            double yMin = Math.Min(focalPoint1[1] - scale1, focalPoint2[1] - scale2);
            // X bounds
            double xMax = Math.Max(focalPoint1[0] + scale1 * aspect, focalPoint2[0] + scale2 * aspect);
            double xMin = Math.Min(focalPoint1[0] - scale1 * aspect, focalPoint2[0] - scale2 * aspect);
            // New center from bounds
            double centerX = (xMin + xMax) / 2.0;
            double centerY = (yMin + yMax) / 2.0;
            // Compute new parallel scale
            double scaleY = (yMax - yMin) / 2.0;
            double scaleX = (xMax - xMin) / (2.0 * aspect);
            double newScale = Math.Max(scaleY, scaleX);
            // Preserve view direction
            double[] direction = new double[3];
            direction[0] = position1[0] - focalPoint1[0];
            direction[1] = position1[1] - focalPoint1[1];
            direction[2] = position1[2] - focalPoint1[2];
            // Set new focal point
            cam.SetFocalPoint(centerX, centerY, focalPoint1[2]);
            // Set new position (keep same view direction)
            cam.SetPosition(centerX + direction[0], centerY + direction[1], focalPoint1[2] + direction[2]);
            cam.SetParallelScale(newScale);
            //
            _style.ResetClippingRange();
            //
            SetAllCaptionPositions();
        }
        private double[] ComputeVisiblePropBounds()
        {
            double[] b1 = _renderer.ComputeVisiblePropBounds();
            double[] b2 = _overlayRenderer.ComputeVisiblePropBounds();
            //
            return new double[] { Math.Min(b1[0], b2[0]), Math.Max(b1[1], b2[1]),
                                  Math.Min(b1[2], b2[2]), Math.Max(b1[3], b2[3]),
                                  Math.Min(b1[4], b2[4]), Math.Max(b1[5], b2[5])};
        }
        //
        private int GetClosestIsoDirectionQuadrant(double[] camera)
        {
            double[] quadrant = new double[3];
            double[] angles = new double[4];
            // I
            quadrant[0] = 1;
            quadrant[1] = 1;
            quadrant[2] = 1;
            angles[0] = GetAngle(camera, quadrant);

            // II
            quadrant[0] = -1;
            quadrant[1] = 1;
            quadrant[2] = 1;
            angles[1] = GetAngle(camera, quadrant);

            // III
            quadrant[0] = -1;
            quadrant[1] = -1;
            quadrant[2] = 1;
            angles[2] = GetAngle(camera, quadrant);

            // IV
            quadrant[0] = 1;
            quadrant[1] = -1;
            quadrant[2] = 1;
            angles[3] = GetAngle(camera, quadrant);

            // find min angle
            int minId = -1;
            double min = double.MaxValue;
            for (int i = 0; i < angles.Length; i++)
            {
                if (angles[i] < min)
                {
                    min = angles[i];
                    minId = i + 1;
                }
            }

            if (minId == 1)
            {
                if (camera[0] >= 0 && camera[1] >= 0 && camera[2] >= 0) return 1;
                else return 7;
            }
            else if (minId == 2)
            {
                if (camera[0] <= 0 && camera[1] >= 0 && camera[2] >= 0) return 2;
                else return 8;
            }
            else if (minId == 3)
            {
                if (camera[0] <= 0 && camera[1] <= 0 && camera[2] >= 0) return 3;
                else return 5;
            }
            else //if (minId == 4)
            {
                if (camera[0] >= 0 && camera[1] <= 0 && camera[2] >= 0) return 4;
                else return 6;
            }
        }
        public double[] GetViewPlaneNormal()
        {
            vtkCamera camera = _renderer.GetActiveCamera();
            //double[] fPos = camera.GetFocalPoint();
            double[] vpn = camera.GetViewPlaneNormal();
            return vpn;
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
        public void AnimateCamera_(vtkCamera cameraStart, vtkCamera cameraEnd, vtkCamera camera)
        {
            vtkCameraInterpolator interpolator = vtkCameraInterpolator.New();
            interpolator.SetInterpolationTypeToSpline();

            interpolator.AddCamera(0, cameraStart);
            interpolator.AddCamera(1, cameraEnd);

            //double zEnd = camera.GetParallelScale();

            //interpolator.InterpolateCamera(0.5, camera);

            //double zStart = cameraStart.GetParallelScale();
            //double zMid = camera.GetParallelScale();


            double[] v1 = cameraStart.GetDirectionOfProjection();
            double[] v2 = cameraEnd.GetDirectionOfProjection();

            double angle = v1[0] * v2[0] + v1[1] * v2[1] + v1[2] * v2[2];

            if (angle < -0.99)
            {
                cameraStart.OrthogonalizeViewUp();
                cameraEnd.OrthogonalizeViewUp();
                double[] up1 = cameraStart.GetViewUp();
                double[] up2 = cameraEnd.GetViewUp();

                double[] up3 = new double[3];
                up3[0] = (up1[0] + up2[0]) / 2;
                up3[1] = (up1[1] + up2[1]) / 2;
                up3[2] = (up1[2] + up2[2]) / 2;

                if (up3[0] == 0 && up3[1] == 0 && up3[2] == 0)
                {
                    up3[0] = v1[0];
                    up3[1] = v1[1];
                    up3[2] = v1[2];
                }

                double[] v3 = new double[3];
                v3[0] = v1[1] * up1[2] - v1[2] * up1[1];
                v3[1] = -(v1[0] * up1[2] - v1[2] * up1[0]);
                v3[2] = v1[0] * up1[1] - v1[1] * up1[0];

                double[] focal = cameraStart.GetFocalPoint();
                double distance = Math.Max(cameraStart.GetDistance(), cameraEnd.GetDistance());
                cameraStart.SetPosition(focal[0] + v3[0] * distance, focal[1] + v3[1] * distance, focal[2] + v3[2] * distance);
                cameraStart.SetViewUp(up3[0], up3[1], up3[2]);
                cameraStart.OrthogonalizeViewUp();

                interpolator.AddCamera(0.5, cameraStart);
            }

            DateTime start = DateTime.Now;
            int delta = 200; //ms
            double currentDelta = 0;
            while (currentDelta < delta)
            {
                currentDelta = (DateTime.Now - start).TotalMilliseconds;
                interpolator.InterpolateCamera(currentDelta / delta, camera);
                camera.OrthogonalizeViewUp();
                _style.AdjustCameraDistanceAndClipping();
                System.Threading.Thread.Sleep(1);
                RenderScene();
                Application.DoEvents();
            }

            interpolator.InterpolateCamera(1, camera);
            RenderScene();
        }
        public void AnimateCameraQuat(vtkCamera cameraStart, vtkCamera cameraEnd, vtkCamera camera)
        {
            vtkMatrix4x4 mCamStart = cameraStart.GetViewTransformMatrix();
            double[][] rStart = new double[3][];
            for (int i = 0; i < 3; i++)
            {
                rStart[i] = new double[3];
                for (int j = 0; j < 3; j++)
                {
                    rStart[i][j] = mCamStart.GetElement(i, j);
                }
            }
            double[] tStart = new double[3];
            for (int i = 0; i < 3; i++)
            {
                tStart[i] = mCamStart.GetElement(i, 3);
            }
            double[] fpStart = cameraStart.GetFocalPoint();
            double zStart = cameraStart.GetParallelScale();
            double[] q1 = QuaternionHelper.QuaternionFromMatrix3x3(rStart);

            vtkMatrix4x4 mCamEnd = cameraEnd.GetViewTransformMatrix();
            double[][] rEnd = new double[3][];
            for (int i = 0; i < 3; i++)
            {
                rEnd[i] = new double[3];
                for (int j = 0; j < 3; j++)
                {
                    rEnd[i][j] = mCamEnd.GetElement(i, j);
                }
            }
            double[] tEnd = new double[3];
            for (int i = 0; i < 3; i++)
            {
                tEnd[i] = mCamEnd.GetElement(i, 3);
            }
            double[] fpEnd = cameraEnd.GetFocalPoint();
            double zEnd = cameraEnd.GetParallelScale();
            double[] q2 = QuaternionHelper.QuaternionFromMatrix3x3(rEnd);

            double[] q;
            double[][] rOut;
            double[][] rOutT;
            double[] transform;
            double[] fp;
            double[] pos = new double[3];
            double[] up = new double[3];
            double z;
            double t;

            DateTime start = DateTime.Now;
            int delta = 500; //ms
            double currentDelta;

            do
            {
                //http://math.stackexchange.com/questions/82602/how-to-find-camera-position-and-rotation-from-a-4x4-matrix
                //
                //     |         |     
                // M = |   R   T |     
                //     |         |     
                //     | 0 0 0 1 |     
                //                     
                // camera position C:  
                // C = -R(transpose)*T 
                //                     

                currentDelta = (DateTime.Now - start).TotalMilliseconds;
                t = currentDelta / delta;
                if (t > 1) t = 1;

                q = QuaternionHelper.QuaternionSlerp(q1, q2, t);
                transform = QuaternionHelper.VectorLerp(tStart, tEnd, t);
                fp = QuaternionHelper.VectorLerp(fpStart, fpEnd, t);
                z = zStart + (t) * (zEnd - zStart);
                rOut = QuaternionHelper.Matrix3x3FromQuaternion(q);
                rOutT = QuaternionHelper.TransponseMatrix3x3(rOut);

                pos[0] = -(rOutT[0][0] * transform[0] + rOutT[0][1] * transform[1] + rOutT[0][2] * transform[2]);
                pos[1] = -(rOutT[1][0] * transform[0] + rOutT[1][1] * transform[1] + rOutT[1][2] * transform[2]);
                pos[2] = -(rOutT[2][0] * transform[0] + rOutT[2][1] * transform[1] + rOutT[2][2] * transform[2]);

                up[0] = rOutT[0][1];
                up[1] = rOutT[1][1];
                up[2] = rOutT[2][1];

                camera.SetPosition(pos[0], pos[1], pos[2]);
                camera.SetFocalPoint(fp[0], fp[1], fp[2]);
                camera.SetViewUp(up[0], up[1], up[2]);
                camera.SetParallelScale(z);
                camera.OrthogonalizeViewUp();
                _style.AdjustCameraDistanceAndClipping();
                System.Threading.Thread.Sleep(5);
                RenderScene();
                Application.DoEvents();
            } while (currentDelta < delta);

            _animating = false;
        }
        public void AnimateCamera(vtkCamera cameraStart, vtkCamera cameraEnd, vtkCamera camera, int timeMs = 500)
        {
            // The idea is to use quaternions to interpolate the camera orientation while maintaining the
            // focal point (end camera position) in the center of the screen. Then use projections to pan
            // the cmara in 2D display coordinates from the start to end positions
            ((vtkInteractorStyleControl)_renderWindowInteractor.GetInteractorStyle()).Animating = true;
            IntPtr pq1 = new IntPtr();
            IntPtr pq2 = new IntPtr();
            IntPtr pq = new IntPtr();
            try
            {
                // Get camera end data                                                              
                vtkMatrix4x4 mCamEnd = cameraEnd.GetViewTransformMatrix();
                double[][] rEnd = new double[3][];
                for (int i = 0; i < 3; i++)
                {
                    rEnd[i] = new double[3];
                    for (int j = 0; j < 3; j++)
                    {
                        rEnd[i][j] = mCamEnd.GetElement(i, j);
                        if (double.IsNaN(rEnd[i][j])) throw new NotSupportedException();
                    }
                }
                double[] tEnd = new double[3];
                for (int i = 0; i < 3; i++)
                {
                    tEnd[i] = mCamEnd.GetElement(i, 3);
                }
                double[] fpEnd = cameraEnd.GetFocalPoint();
                double zEnd = cameraEnd.GetParallelScale();
                double[] q2 = QuaternionHelper.QuaternionFromMatrix3x3(rEnd);
                // Get display (XY) projection of the final focal point (model center) for both cameras
                double[] fpEndXY = vtkInteractorStyleControl.WorldToDisplay(_renderer, fpEnd);
                _renderer.SetActiveCamera(cameraStart);
                double[] fpStartXY = vtkInteractorStyleControl.WorldToDisplay(_renderer, fpEnd);
                // Project both XY points into 3D
                double[] fpEndworld;
                double[] fpStartWorld;
                fpEndworld = vtkInteractorStyleControl.DisplayToWorld(_renderer, fpEndXY);
                fpStartWorld = vtkInteractorStyleControl.DisplayToWorld(_renderer, fpStartXY);
                // Assign the end camera back to the renderer
                _renderer.SetActiveCamera(camera);
                // Compute the delta of the 3D projected points
                double[] fpDelta = new double[] { fpStartWorld[0] - fpEndworld[0],
                                                  fpStartWorld[1] - fpEndworld[1],
                                                  fpStartWorld[2] - fpEndworld[2]};
                // Pan the start camera to look at the model center 
                double[] cPos = cameraStart.GetPosition();
                double[] fPos = cameraStart.GetFocalPoint();
                cameraStart.SetPosition(cPos[0] + fpDelta[0], cPos[1] + fpDelta[1], cPos[2] + fpDelta[2]);
                cameraStart.SetFocalPoint(fPos[0] + fpDelta[0], fPos[1] + fpDelta[1], fPos[2] + fpDelta[2]);
                // Get camera start data                                                            
                vtkMatrix4x4 mCamStart = cameraStart.GetViewTransformMatrix();
                double[][] rStart = new double[3][];
                for (int i = 0; i < 3; i++)
                {
                    rStart[i] = new double[3];
                    for (int j = 0; j < 3; j++)
                    {
                        rStart[i][j] = mCamStart.GetElement(i, j);
                        if (double.IsNaN(rStart[i][j])) throw new NotSupportedException();
                    }
                }
                double[] tStart = new double[3];
                for (int i = 0; i < 3; i++)
                {
                    tStart[i] = mCamStart.GetElement(i, 3);
                }
                double[] fpStart = cameraStart.GetFocalPoint();
                double zStart = cameraStart.GetParallelScale();
                double[] q1 = QuaternionHelper.QuaternionFromMatrix3x3(rStart);
                // A quaternion and its negation are the same rotation but only the one in the
                // same hemisphere as the start is interpolated along the shortest path
                if (q1[0] * q2[0] + q1[1] * q2[1] + q1[2] * q2[2] + q1[3] * q2[3] < 0)
                {
                    for (int i = 0; i < q2.Length; i++) q2[i] = -q2[i];
                }
                pq1 = QuaternionHelper.IntPtrFromQuaternion(q1);
                pq2 = QuaternionHelper.IntPtrFromQuaternion(q2);
                //
                double[] q;
                double[][] rOut;
                double[][] rOutT;
                double[] transform;
                double[] fp;
                double[] pos = new double[3];
                double[] up = new double[3];
                double[] fpXY;
                double z;
                double t;
                //
                vtkQuaternionInterpolator qi = vtkQuaternionInterpolator.New();
                qi.SetInterpolationTypeToLinear();
                qi.AddQuaternion(0, pq1);
                qi.AddQuaternion(1, pq2);
                pq = Marshal.AllocHGlobal(Marshal.SizeOf(q1[0]) * q1.Length);
                //
                DateTime start = DateTime.Now;
                int delta = timeMs;
                double currentDelta = 0;
                //
                _positions = new List<double[]>();
                _focalPoints = new List<double[]>();
                // Do interpolation
                do
                {
                    //
                    //http://math.stackexchange.com/questions/82602/how-to-find-camera-position-and-rotation-from-a-4x4-matrix
                    //
                    //     |         |     
                    // M = |   R   T |     
                    //     |         |     
                    //     | 0 0 0 1 |     
                    //                     
                    // camera position C:  
                    // C = -R(transpose)*T 
                    //                     
                    currentDelta = (DateTime.Now - start).TotalMilliseconds;
                    t = currentDelta / delta;
                    if (t > 1) t = 1;
                    //
                    if (Math.Abs(q1[0] - q2[0]) + Math.Abs(q1[1] - q2[1]) + Math.Abs(q1[2] - q2[2]) + Math.Abs(q1[3] - q2[3]) < 0.00001)
                        q = q1;
                    else
                    {
                        qi.InterpolateQuaternion(t, pq);
                        q = QuaternionHelper.QuaternionFromIntPtr(pq);
                    }
                    //
                    transform = QuaternionHelper.VectorLerp(tStart, tEnd, t);
                    //fp = QuaternionHelper.VectorLerp(fpStart, fpEnd, t);
                    fp = fpEnd;
                    z = zStart + (t) * (zEnd - zStart);
                    rOut = QuaternionHelper.Matrix3x3FromQuaternion(q);
                    rOutT = QuaternionHelper.TransponseMatrix3x3(rOut);
                    //
                    pos[0] = -(rOutT[0][0] * transform[0] + rOutT[0][1] * transform[1] + rOutT[0][2] * transform[2]);
                    pos[1] = -(rOutT[1][0] * transform[0] + rOutT[1][1] * transform[1] + rOutT[1][2] * transform[2]);
                    pos[2] = -(rOutT[2][0] * transform[0] + rOutT[2][1] * transform[1] + rOutT[2][2] * transform[2]);
                    //
                    up[0] = rOutT[0][1];
                    up[1] = rOutT[1][1];
                    up[2] = rOutT[2][1];
                    // Update camera to get correct projections
                    camera.SetPosition(pos[0], pos[1], pos[2]);
                    camera.SetFocalPoint(fp[0], fp[1], fp[2]);
                    camera.SetParallelScale(z);
                    // Pan the camera in the 2D display coordinates                                 
                    fpXY = QuaternionHelper.VectorLerp(fpStartXY, fpEndXY, t);
                    fpEndworld = vtkInteractorStyleControl.DisplayToWorld(_renderer, fpEndXY);
                    fpStartWorld = vtkInteractorStyleControl.DisplayToWorld(_renderer, fpXY);
                    // Compute the delta of the 3D projected points
                    fpDelta = new double[] { fpEndworld[0] - fpStartWorld[0],
                                             fpEndworld[1] - fpStartWorld[1],
                                             fpEndworld[2] - fpStartWorld[2]};
                    // Pan the camera to look at the model center 
                    camera.SetPosition(pos[0] + fpDelta[0], pos[1] + fpDelta[1], pos[2] + fpDelta[2]);
                    camera.SetFocalPoint(fp[0] + fpDelta[0], fp[1] + fpDelta[1], fp[2] + fpDelta[2]);
                    //
                    camera.SetViewUp(up[0], up[1], up[2]);
                    camera.OrthogonalizeViewUp();
                    _style.AdjustCameraDistanceAndClipping();
                    //System.Threading.Thread.Sleep(5);
                    _renderWindowInteractor.Modified(); // this updates the vtkMax annotation objects
                    RenderScene();
                    //
                    if (_drawCameraPath)
                    {
                        _positions.Add(pos.ToArray());
                        _focalPoints.Add(fp.ToArray());
                    }
                    //
                    Application.DoEvents();
                }
                while (currentDelta < delta);
            }
            finally
            {
                // Free the unmanaged memory.
                Marshal.FreeHGlobal(pq1);
                Marshal.FreeHGlobal(pq2);
                Marshal.FreeHGlobal(pq);
                _animating = false;
                //
                ((vtkInteractorStyleControl)_renderWindowInteractor.GetInteractorStyle()).Animating = false;
                //
                if (_drawCameraPath) DrawCameraPath();
            }
        }
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
        public void AdjustCameraDistanceAndClipping()
        {
            _style.AdjustCameraDistanceAndClipping();
        }
        public void AdjustCameraDistanceAndClippingRedraw()
        {
            _style.AdjustCameraDistanceAndClipping();
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
            _style.AdjustCameraDistanceAndClipping();
            //
            RenderScene();
        }
        //
        public void Rotate(double azimuthAngle, double elevationAngle, double rollAngle)
        {
            _style.Rotate(azimuthAngle, elevationAngle, rollAngle);
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
        public vtkMaxActor AddPoints(vtkMaxActorData data)
        {
            vtkMaxActor actor = new vtkMaxActor(data, false, true);
            AddActorGeometry(actor, data.Layer);
            //
            if (!AreAnyActorsVisible()) AdjustCameraDistanceAndClippingRedraw();
            else AdjustCameraDistanceAndClipping();
            //
            return actor;
        }
        public vtkMaxActor AddCells(vtkMaxActorData data)
        {
            // Create actor
            vtkMaxActor actor = new vtkMaxActor(data);
            //
            AddActor(actor);
            //
            if (!AreAnyActorsVisible()) AdjustCameraDistanceAndClippingRedraw();
            else AdjustCameraDistanceAndClipping();
            //
            return actor;
        }
        public void AddCells(vtkMaxActorData[] data)
        {
            // Create actor
            vtkMaxActor[] actors = new vtkMaxActor[data.Length];
            //Parallel.For(0, data.Length, i =>
            for (int i = 0; i < data.Length; i++)
            {
                actors[i] = new vtkMaxActor(data[i]);
            }
            //);
            //
            foreach (var actor in actors) AddActor(actor);
            //
            if (!AreAnyActorsVisible()) AdjustCameraDistanceAndClippingRedraw();
            else AdjustCameraDistanceAndClipping();
        }
        //
        public void AddSphereActor(vtkMaxActorData data, double symbolSize)
        {
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;
            // Points
            vtkPoints pointData = vtkPoints.New();
            for (int i = 0; i < data.Geometry.Nodes.Coor.GetLength(0); i++)
            {
                pointData.InsertNextPoint(data.Geometry.Nodes.Coor[i][0],
                                          data.Geometry.Nodes.Coor[i][1],
                                          data.Geometry.Nodes.Coor[i][2]);
            }
            // Poly data
            vtkPolyData polyData = vtkPolyData.New();
            polyData.SetPoints(pointData);
            // Calculate the distance to the camera of each point.
            vtkDistanceToCamera distanceToCamera = vtkDistanceToCamera.New();
            distanceToCamera.SetInput(polyData);
            distanceToCamera.SetScreenSize(symbolSize);
            distanceToCamera.SetRenderer(_renderer);
            // Source object
            vtkSphereSource sphereSource = vtkSphereSource.New();
            sphereSource.SetRadius(0.2);
            sphereSource.SetPhiResolution(13);
            sphereSource.SetThetaResolution(13);
            sphereSource.Update();
            // Triangulation helps with silhouette edges and 3mf export
            vtkTriangleFilter triangleFilter = vtkTriangleFilter.New();
            triangleFilter.SetInput(sphereSource.GetOutput());
            triangleFilter.Update();
            // Compute normals
            vtkPolyDataNormals normals = vtkPolyDataNormals.New();
            normals.SetInput(triangleFilter.GetOutput());
            normals.Update();
            // Set normals
            triangleFilter.GetOutput().GetPointData().SetNormals(normals.GetOutput().GetPointData().GetNormals());
            // Glyph
            vtkGlyph3D glyph = vtkGlyph3D.New();
            glyph.SetSourceConnection(triangleFilter.GetOutputPort());
            glyph.SetInputConnection(distanceToCamera.GetOutputPort());
            glyph.SetVectorModeToUseNormal();
            // Scale
            glyph.ScalingOn();
            glyph.SetScaleModeToScaleByScalar();
            glyph.SetInputArrayToProcess(0, 0, 0, "vtkDataObject::FIELD_ASSOCIATION_POINTS", "DistanceToCamera");
            glyph.SetScaleFactor(1.0);
            glyph.OrientOn();
            glyph.Update();
            // Mapper
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(0, glyph.GetOutputPort());
            mapper.ScalarVisibilityOff();
            // Actor
            data.Name += Globals.NameSeparator + "sphere";
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            // Add
            ApplySymbolFormattingToActor(actor);
            AddActorGeometry(actor, data.Layer);
            //
            if (_drawSymbolEdges) AddSymbolEdges(data, glyph.GetOutputPort());
        }
        public void AddCoordinateAxis(vtkMaxActorData data, double symbolSize)
        {
            double relativeSize = 1;
            //
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;
            // Points
            vtkPoints pointData = vtkPoints.New();
            pointData.InsertNextPoint(data.Geometry.Nodes.Coor[0][0], data.Geometry.Nodes.Coor[0][1],
                                      data.Geometry.Nodes.Coor[0][2]);
            // Normals
            vtkDoubleArray pointNormalsArray = vtkDoubleArray.New();
            pointNormalsArray.SetNumberOfComponents(3);
            pointNormalsArray.InsertNextTuple3(data.Geometry.Nodes.Normals[0][0], data.Geometry.Nodes.Normals[0][1],
                                               data.Geometry.Nodes.Normals[0][2]);
            // Poly data
            vtkPolyData polyData = vtkPolyData.New();
            polyData.SetPoints(pointData);
            polyData.GetPointData().SetNormals(pointNormalsArray);
            // Calculate the distance to the camera of each point.
            vtkDistanceToCamera distanceToCamera = vtkDistanceToCamera.New();
            distanceToCamera.SetInput(polyData);
            distanceToCamera.SetScreenSize(symbolSize * relativeSize);
            distanceToCamera.SetRenderer(_renderer);
            // Line
            vtkLineSource line = vtkLineSource.New();
            line.SetPoint1(0, 0, 0);
            line.SetPoint2(1 * relativeSize, 0, 0);
            // Cone
            vtkConeSource cone = vtkConeSource.New();
            cone.SetCenter(0.9 * relativeSize, 0, 0);
            cone.SetHeight(0.2 * relativeSize);
            cone.SetRadius(0.08 * relativeSize);
            cone.SetResolution(31);
            // Append
            vtkAppendPolyData append = vtkAppendPolyData.New();
            append.AddInput(line.GetOutput());
            append.AddInput(cone.GetOutput());
            // Triangulation helps with silhouette edges and 3mf export
            vtkTriangleFilter triangleFilter = vtkTriangleFilter.New();
            triangleFilter.SetInput(append.GetOutput());
            triangleFilter.Update();
            // Compute normals
            vtkPolyDataNormals appendNormals = vtkPolyDataNormals.New();
            appendNormals.SetInput(triangleFilter.GetOutput());
            appendNormals.Update();
            // Transform
            vtkTransform transform = vtkTransform.New();
            // Transform filter
            vtkTransformFilter transformFilter = vtkTransformFilter.New();
            transformFilter.SetInput(triangleFilter.GetOutput());
            transformFilter.SetTransform(transform);
            // Glyph
            vtkGlyph3D glyph = vtkGlyph3D.New();
            glyph.SetSourceConnection(transformFilter.GetOutputPort());
            glyph.SetInputConnection(distanceToCamera.GetOutputPort());
            glyph.SetVectorModeToUseNormal();
            // Scale
            glyph.ScalingOn();
            glyph.SetScaleModeToScaleByScalar();
            glyph.SetInputArrayToProcess(0, 0, 0, "vtkDataObject::FIELD_ASSOCIATION_POINTS", "DistanceToCamera");
            glyph.SetScaleFactor(1.0);
            glyph.OrientOn();
            glyph.Update();
            // Mapper
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(0, glyph.GetOutputPort());
            mapper.ScalarVisibilityOff();
            // Actor
            data.Name += Globals.NameSeparator + "axis";
            //
            data.LineWidth = 2;
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            actor.Color = data.Color;
            actor.GeometryProperty.SetRepresentationToSurface();
            // Add
            ApplySymbolFormattingToActor(actor);
            AddActorGeometry(actor, data.Layer);
            //
            if (_drawSymbolEdges) AddSymbolEdges(data, glyph.GetOutputPort());
        }
        public void AddCaptionActor(string name, string caption, Color color, double[] position,
                                    double[] offsetVector, double fontScaleFactor, vtkRendererLayer layer)
        {
            // Create an actor for the text
            vtkCaptionActor2D captionActor = vtkCaptionActor2D.New();
            // Text
            vtkTextProperty tp = CreateNewTextProperty();
            captionActor.SetCaption(caption);
            tp.SetFontSize((int)(tp.GetFontSize() * fontScaleFactor));
            //tp.SetJustificationToLeft();
            //tp.SetVerticalJustificationToCentered();
            captionActor.SetCaptionTextProperty(tp);
            captionActor.GetTextActor().SetTextScaleModeToNone();
            //
            captionActor.GetPositionCoordinate().SetValue(position[0], position[1], position[2]);
            captionActor.GetPositionCoordinate().SetCoordinateSystemToWorld();
            //
            captionActor.LeaderOff();
            captionActor.BorderOff();
            //
            captionActor.UseBoundsOff();
            //captionActor.SetWidth(0.01);
            // Actor
            vtkMaxCaptionActor actor = new vtkMaxCaptionActor(name, color, captionActor);
            actor.Position = position;
            if (offsetVector != null && offsetVector.Length == 3) actor.OffsetVector = offsetVector;
            // Add
            AddActorCaption(actor, layer);
            //
            SetAllCaptionPositions();
        }
        public void AddLabelActor(string name, double[][] positions, int[] labels, Color color,
                                  double fontScaleFactor, vtkRendererLayer layer)
        {
            if (positions == null || labels == null || positions.Length == 0 || positions.Length != labels.Length)
                throw new ArgumentException("The label positions and values must have the same non-zero length.");
            // Points and labels
            vtkPoints points = vtkPoints.New();
            vtkIntArray labelData = vtkIntArray.New();
            labelData.SetName("Labels");
            labelData.SetNumberOfComponents(1);
            for (int i = 0; i < positions.Length; i++)
            {
                points.InsertNextPoint(positions[i][0], positions[i][1], positions[i][2]);
                labelData.InsertNextValue(labels[i]);
            }
            // Poly data
            vtkPolyData polyData = vtkPolyData.New();
            polyData.SetPoints(points);
            polyData.GetPointData().SetScalars(labelData);
            // Mapper
            vtkTextProperty textProperty = CreateNewTextProperty();
            textProperty.SetColor(color.R / 255d, color.G / 255d, color.B / 255d);
            textProperty.SetFontSize((int)(textProperty.GetFontSize() * fontScaleFactor));
            vtkLabeledDataMapper mapper = vtkLabeledDataMapper.New();
            mapper.SetInput(polyData);
            mapper.SetLabelModeToLabelScalars();
            mapper.SetLabelFormat("%d");
            mapper.SetLabelTextProperty(textProperty);
            mapper.CoordinateSystemWorld();
            // Actor
            vtkActor2D labelActor = vtkActor2D.New();
            labelActor.SetMapper(mapper);
            labelActor.PickableOff();
            labelActor.UseBoundsOff();
            vtkMaxCaptionActor actor = new vtkMaxCaptionActor(name, color, labelActor);
            actor.Position = positions[0];
            // Add all labels as one overlay actor.
            AddActorCaption(actor, layer);
        }
        public void AddLabelActor(string name, double[][] positions, string[] labels, Color color,
                                  double fontScaleFactor, vtkRendererLayer layer)
        {
            if (positions == null || labels == null || positions.Length == 0 || positions.Length != labels.Length)
                throw new ArgumentException("The label positions and values must have the same non-zero length.");
            // Points and labels
            vtkPoints points = vtkPoints.New();
            vtkStringArray labelData = vtkStringArray.New();
            labelData.SetName("Labels");
            labelData.SetNumberOfComponents(1);
            for (int i = 0; i < positions.Length; i++)
            {
                points.InsertNextPoint(positions[i][0], positions[i][1], positions[i][2]);
                labelData.InsertNextValue(labels[i]);
            }
            // Poly data
            vtkPolyData polyData = vtkPolyData.New();
            polyData.SetPoints(points);
            polyData.GetPointData().AddArray(labelData);
            // Mapper
            vtkTextProperty textProperty = CreateNewTextProperty();
            textProperty.SetColor(color.R / 255d, color.G / 255d, color.B / 255d);
            textProperty.SetFontSize((int)(textProperty.GetFontSize() * fontScaleFactor));
            vtkLabeledDataMapper mapper = vtkLabeledDataMapper.New();
            mapper.SetInput(polyData);
            mapper.SetLabelModeToLabelFieldData();
            mapper.SetFieldDataName("Labels");
            mapper.SetLabelFormat("%s");
            mapper.SetLabelTextProperty(textProperty);
            mapper.CoordinateSystemWorld();
            // Actor
            vtkActor2D labelActor = vtkActor2D.New();
            labelActor.SetMapper(mapper);
            labelActor.PickableOff();
            labelActor.UseBoundsOff();
            vtkMaxCaptionActor actor = new vtkMaxCaptionActor(name, color, labelActor);
            actor.Position = positions[0];
            // Add all labels as one overlay actor.
            AddActorCaption(actor, layer);
        }
        private vtkActor GetActorTmp(double x, double y, double z)
        {
            vtkPoints points = vtkPoints.New();
            points.InsertNextPoint(x, y, z);
            //vtkCellArray lineIndices = vtkCellArray.New();
            //lineIndices.InsertNextCell(1);
            //lineIndices.InsertCellPoint(0);
            //lineIndices.InsertCellPoint(0);
            vtkPolyData pointsPolyData = vtkPolyData.New();
            pointsPolyData.SetPoints(points);
            //pointsPolyData.SetLines(lineIndices);
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInput(pointsPolyData);
            //mapper.Update();
            vtkActor actorTmp = vtkActor.New();
            actorTmp.SetMapper(mapper);
            actorTmp.UseBoundsOff();
            return actorTmp;
        }
        private void SetAllCaptionPositions()
        {
            double size = vtkInteractorStyleControl.DisplayToWorldScale(_renderer, 1);
            //
            foreach (var entry in _actors)
            {
                if (entry.Value is vtkMaxCaptionActor ca) SetCaptionOffsetPositions(ca, size);
            }
            foreach (var entry in _overlayActors)
            {
                if (entry.Value is vtkMaxCaptionActor ca) SetCaptionOffsetPositions(ca, size);
            }
            foreach (var entry in _selectedActors)
            {
                if (entry is vtkMaxCaptionActor ca) SetCaptionOffsetPositions(ca, size);
            }
        }
        private void SetCaptionOffsetPositions(vtkMaxCaptionActor vtkMaxCaptionActor, double size)
        {
            double[] position = vtkMaxCaptionActor.Position;
            double[] offset = vtkMaxCaptionActor.OffsetVector;
            //
            if (offset != null && offset.Length == 3)
            {
                double[] pos = new double[3];   // make a copy
                pos[0] = position[0] + size * offset[0];
                pos[1] = position[1] + size * offset[1];
                pos[2] = position[2] + size * offset[2];
                //
                vtkMaxCaptionActor.CaptionActor.GetPositionCoordinate().SetValue(pos[0], pos[1], pos[2]);
            }
        }
        public void AddOrientedDisplacementConstraintActor(vtkMaxActorData data, double symbolSize)
        {
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;
            //
            double[][] points = data.Geometry.Nodes.Coor;
            double[][] normals = data.Geometry.Nodes.Normals;
            // Points
            vtkPoints pointData = vtkPoints.New();
            for (int i = 0; i < points.GetLength(0); i++)
            {
                if (points[i] != null && points[i].Length == 3)
                    pointData.InsertNextPoint(points[i][0], points[i][1], points[i][2]);
            }
            // Normals
            vtkDoubleArray pointNormalsArray = vtkDoubleArray.New();
            pointNormalsArray.SetNumberOfComponents(3);
            for (int i = 0; i < normals.GetLength(0); i++)
            {
                pointNormalsArray.InsertNextTuple3(normals[i][0], normals[i][1], normals[i][2]);
            }
            // Poly data
            vtkPolyData polyData = vtkPolyData.New();
            polyData.SetPoints(pointData);
            polyData.GetPointData().SetNormals(pointNormalsArray);
            // Calculate the distance to the camera of each point.
            vtkDistanceToCamera distanceToCamera = vtkDistanceToCamera.New();
            distanceToCamera.SetInput(polyData);
            distanceToCamera.SetScreenSize(symbolSize);
            distanceToCamera.SetRenderer(_renderer);
            // Cone
            //vtkTubeFilter cone = CreateConeSource(-0.5, 0, 0, 21);
            vtkConeSource cone = vtkConeSource.New();
            cone.SetCenter(-0.5, 0, 0);
            cone.SetResolution(31);
            // Triangulation helps with silhouette edges and 3mf export
            vtkTriangleFilter triangleFilter = vtkTriangleFilter.New();
            triangleFilter.SetInput(cone.GetOutput());
            triangleFilter.Update();
            // Compute normals
            vtkPolyDataNormals coneNormals = vtkPolyDataNormals.New();
            coneNormals.SetInput(triangleFilter.GetOutput());
            coneNormals.Update();
            // Set normals
            //cone.GetOutput().GetPointData().SetNormals(coneNormals.GetOutput().GetPointData().GetNormals());
            // Glyph
            vtkGlyph3D glyph = vtkGlyph3D.New();
            glyph.SetSourceConnection(triangleFilter.GetOutputPort());
            glyph.SetInputConnection(distanceToCamera.GetOutputPort());
            glyph.SetVectorModeToUseNormal();
            // Scale
            glyph.ScalingOn();
            glyph.SetScaleModeToScaleByScalar();
            glyph.SetInputArrayToProcess(0, 0, 0, "vtkDataObject::FIELD_ASSOCIATION_POINTS", "DistanceToCamera");
            glyph.SetScaleFactor(0.3);
            glyph.OrientOn();
            glyph.Update();
            // Mapper
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(glyph.GetOutputPort());
            mapper.ScalarVisibilityOff();
            // Actor
            data.Name += Globals.NameSeparator + "cones";
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            actor.GeometryProperty.SetRepresentationToSurface();
            // Add
            ApplySymbolFormattingToActor(actor);
            AddActorGeometry(actor, data.Layer);
            //
            if (_drawSymbolEdges) AddSymbolEdges(data, glyph.GetOutputPort());
        }
        public void AddOrientedConeActors(vtkMaxActorData data, double symbolSize)
        {
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;
            //
            double[][] points = data.Geometry.Nodes.Coor;
            double[][] normals = data.Geometry.Nodes.Normals;
            // Points
            vtkPoints pointData = vtkPoints.New();
            for (int i = 0; i < points.GetLength(0); i++)
            {
                if (points[i] != null && points[i].Length == 3)
                    pointData.InsertNextPoint(points[i][0], points[i][1], points[i][2]);
            }
            // Normals
            vtkDoubleArray pointNormalsArray = vtkDoubleArray.New();
            pointNormalsArray.SetNumberOfComponents(3);
            for (int i = 0; i < normals.GetLength(0); i++)
            {
                pointNormalsArray.InsertNextTuple3(normals[i][0], normals[i][1], normals[i][2]);
            }
            // Poly data
            vtkPolyData polyData = vtkPolyData.New();
            polyData.SetPoints(pointData);
            polyData.GetPointData().SetNormals(pointNormalsArray);
            // Calculate the distance to the camera of each point.
            vtkDistanceToCamera distanceToCamera = vtkDistanceToCamera.New();
            distanceToCamera.SetInput(polyData);
            distanceToCamera.SetScreenSize(symbolSize);
            distanceToCamera.SetRenderer(_renderer);
            // Cone
            //vtkTubeFilter cone = CreateConeSource(-0.5, 0, 0, 21);
            vtkConeSource cone = vtkConeSource.New();
            cone.SetCenter(-0.5, 0, 0);
            cone.SetRadius(0.35);
            cone.CappingOn();
            cone.SetResolution(11);
            // Triangulation helps with silhouette edges and 3mf export
            vtkTriangleFilter triangleFilter = vtkTriangleFilter.New();
            triangleFilter.SetInput(cone.GetOutput());
            triangleFilter.Update();
            // Glyph
            vtkGlyph3D glyph = vtkGlyph3D.New();
            glyph.SetSourceConnection(triangleFilter.GetOutputPort());
            glyph.SetInputConnection(distanceToCamera.GetOutputPort());
            glyph.SetVectorModeToUseNormal();
            // Scale
            glyph.ScalingOn();
            glyph.SetScaleModeToScaleByScalar();
            glyph.SetInputArrayToProcess(0, 0, 0, "vtkDataObject::FIELD_ASSOCIATION_POINTS", "DistanceToCamera");
            glyph.SetScaleFactor(0.3);
            glyph.OrientOn();
            glyph.Update();
            // Mapper
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(glyph.GetOutputPort());
            mapper.ScalarVisibilityOff();
            // Actor
            data.Name += Globals.NameSeparator + "cones";
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            actor.GeometryProperty.SetRepresentationToSurface();
            // Add
            ApplySymbolFormattingToActor(actor);
            AddActorGeometry(actor, data.Layer);
            //
            if (_drawSymbolEdges) AddSymbolEdges(data, glyph.GetOutputPort());
        }
        public void AddOrientedRotationalConstraintActor(vtkMaxActorData data, double symbolSize)
        {
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;
            //
            double[][] points = data.Geometry.Nodes.Coor;
            double[][] normals = data.Geometry.Nodes.Normals;
            // Points
            vtkPoints pointData = vtkPoints.New();
            for (int i = 0; i < points.GetLength(0); i++)
            {
                pointData.InsertNextPoint(points[i][0], points[i][1], points[i][2]);
            }
            // Normals
            vtkDoubleArray pointNormalsArray = vtkDoubleArray.New();
            pointNormalsArray.SetNumberOfComponents(3);
            for (int i = 0; i < normals.GetLength(0); i++)
            {
                pointNormalsArray.InsertNextTuple3(normals[i][0], normals[i][1], normals[i][2]);
            }
            // Poly data
            vtkPolyData polyData = vtkPolyData.New();
            polyData.SetPoints(pointData);
            polyData.GetPointData().SetNormals(pointNormalsArray);
            // Calculate the distance to the camera of each point.
            vtkDistanceToCamera distanceToCamera = vtkDistanceToCamera.New();
            distanceToCamera.SetInput(polyData);
            distanceToCamera.SetScreenSize(symbolSize);
            distanceToCamera.SetRenderer(_renderer);
            // Cube
            vtkCubeSource cubeSource =  vtkCubeSource.New();
            cubeSource.SetCenter(-1.55, 0.0, 0.0);
            cubeSource.SetXLength(0.4);
            cubeSource.SetYLength(0.9);
            cubeSource.SetZLength(0.9);
            // Line
            vtkLineSource line = vtkLineSource.New();
            line.SetPoint1(0, 0, 0);
            line.SetPoint2(-1.3, 0, 0);
            // Append
            vtkAppendPolyData append = vtkAppendPolyData.New();
            append.AddInput(cubeSource.GetOutput());
            append.AddInput(line.GetOutput());
            // Triangulation helps with silhouette edges and 3mf export
            vtkTriangleFilter triangleFilter = vtkTriangleFilter.New();
            triangleFilter.SetInput(append.GetOutput());
            triangleFilter.Update();
            // Glyph
            vtkGlyph3D glyph = vtkGlyph3D.New();
            glyph.SetSourceConnection(triangleFilter.GetOutputPort());
            glyph.SetInputConnection(distanceToCamera.GetOutputPort());
            glyph.SetVectorModeToUseNormal();
            // Scale
            glyph.ScalingOn();
            glyph.SetScaleModeToScaleByScalar();
            glyph.SetInputArrayToProcess(0, 0, 0, "vtkDataObject::FIELD_ASSOCIATION_POINTS", "DistanceToCamera");
            glyph.SetScaleFactor(0.3);
            glyph.OrientOn();
            glyph.Update();
            // Mapper
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(glyph.GetOutputPort());
            mapper.ScalarVisibilityOff();
            // Actor
            data.Name += Globals.NameSeparator + "cylinder";
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            actor.GeometryProperty.SetInterpolationToFlat();
            // Add
            ApplySymbolFormattingToActor(actor);
            AddActorGeometry(actor, data.Layer);
            //
            if (_drawSymbolEdges) AddSymbolEdges(data, glyph.GetOutputPort());
        }
        public void AddOrientedArrowsActor(vtkMaxActorData data, double symbolSize, bool invert, double relativeSize = 1)
        {
            data.Geometry.RemoveZeroLengthNormals();
            //
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;
            // Points
            vtkPoints pointData = vtkPoints.New();
            for (int i = 0; i < data.Geometry.Nodes.Coor.GetLength(0); i++)
            {
                pointData.InsertNextPoint(data.Geometry.Nodes.Coor[i][0], data.Geometry.Nodes.Coor[i][1],
                                          data.Geometry.Nodes.Coor[i][2]);
            }
            // Normals
            vtkDoubleArray pointNormalsArray = vtkDoubleArray.New();
            pointNormalsArray.SetNumberOfComponents(3); //3d normals (ie x,y,z)
            for (int i = 0; i < data.Geometry.Nodes.Normals.GetLength(0); i++)
            {
                pointNormalsArray.InsertNextTuple3(data.Geometry.Nodes.Normals[i][0], data.Geometry.Nodes.Normals[i][1],
                                                   data.Geometry.Nodes.Normals[i][2]);
            }
            // Poly data
            vtkPolyData polyData = vtkPolyData.New();
            polyData.SetPoints(pointData);
            polyData.GetPointData().SetNormals(pointNormalsArray);
            // Calculate the distance to the camera of each point.
            vtkDistanceToCamera distanceToCamera = vtkDistanceToCamera.New();
            distanceToCamera.SetInput(polyData);
            distanceToCamera.SetScreenSize(symbolSize * relativeSize);
            distanceToCamera.SetRenderer(_renderer);
            // Arrow
            vtkArrowSource arrow = vtkArrowSource.New();
            arrow.SetTipResolution(_arrowTipResolution);
            arrow.SetTipLength(0.3 / relativeSize);
            arrow.SetTipRadius(0.1 / relativeSize);
            arrow.SetShaftResolution(_arrowShaftResolution);
            arrow.SetShaftRadius(0.03 / relativeSize);
            // Triangulation helps with silhouette edges and 3mf export
            vtkTriangleFilter triangleFilter = vtkTriangleFilter.New();
            triangleFilter.SetInput(arrow.GetOutput());
            triangleFilter.Update();
            // Compute normals
            vtkPolyDataNormals normals = vtkPolyDataNormals.New();
            normals.SetInput(triangleFilter.GetOutput());
            normals.Update();
            // Set normals
            //triangleFilter.GetOutput().GetPointData().SetNormals(normals.GetOutput().GetPointData().GetNormals());
            // Transform
            vtkTransform transform = vtkTransform.New();
            transform.Identity();
            if (invert) transform.Translate(-1.05, 0, 0);
            else transform.Translate(0.05, 0, 0);
            // Transform filter
            vtkTransformFilter transformFilter = vtkTransformFilter.New();
            transformFilter.SetInput(triangleFilter.GetOutput());
            transformFilter.SetTransform(transform);
            // Glyph
            vtkGlyph3D glyph = vtkGlyph3D.New();
            glyph.SetSourceConnection(transformFilter.GetOutputPort());
            glyph.SetInputConnection(distanceToCamera.GetOutputPort());
            glyph.SetVectorModeToUseNormal();
            // Scale
            glyph.ScalingOn();
            glyph.SetScaleModeToScaleByScalar();
            glyph.SetInputArrayToProcess(0, 0, 0, "vtkDataObject::FIELD_ASSOCIATION_POINTS", "DistanceToCamera");
            glyph.SetScaleFactor(1.0);
            glyph.OrientOn();
            glyph.Update();
            // Mapper
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(0, glyph.GetOutputPort());
            mapper.ScalarVisibilityOff();
            // Actor
            data.Name += Globals.NameSeparator + "arrow";
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            actor.Color = data.Color;// Color.Blue;
            actor.GeometryProperty.SetRepresentationToSurface();
            // Add
            ApplySymbolFormattingToActor(actor);
            AddActorGeometry(actor, data.Layer);
            //
            if (_drawSymbolEdges) AddSymbolEdges(data, glyph.GetOutputPort());
        }
        public void AddOrientedDoubleArrowsActor(vtkMaxActorData data, double symbolSize)
        {
            data.Geometry.RemoveZeroLengthNormals();
            //
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;
            // Double arrow for moment loads
            // Points
            vtkPoints pointData = vtkPoints.New();
            for (int i = 0; i < data.Geometry.Nodes.Coor.GetLength(0); i++)
            {
                pointData.InsertNextPoint(data.Geometry.Nodes.Coor[i][0], data.Geometry.Nodes.Coor[i][1],
                                          data.Geometry.Nodes.Coor[i][2]);
            }
            // Normals
            vtkDoubleArray pointNormalsArray = vtkDoubleArray.New();
            pointNormalsArray.SetNumberOfComponents(3); //3d normals (ie x,y,z)
            for (int i = 0; i < data.Geometry.Nodes.Normals.GetLength(0); i++)
            {
                pointNormalsArray.InsertNextTuple3(data.Geometry.Nodes.Normals[i][0], data.Geometry.Nodes.Normals[i][1],
                                                   data.Geometry.Nodes.Normals[i][2]);
            }
            // Poly data
            vtkPolyData polyData = vtkPolyData.New();
            polyData.SetPoints(pointData);
            polyData.GetPointData().SetNormals(pointNormalsArray);
            // Calculate the distance to the camera of each point.
            vtkDistanceToCamera distanceToCamera = vtkDistanceToCamera.New();
            distanceToCamera.SetInput(polyData);
            distanceToCamera.SetScreenSize(symbolSize);
            distanceToCamera.SetRenderer(_renderer);
            // Arrow
            vtkArrowSource arrow = vtkArrowSource.New();
            arrow.SetTipResolution(_arrowTipResolution);
            arrow.SetTipLength(0.3);
            arrow.SetTipRadius(0.1);
            arrow.SetShaftResolution(_arrowShaftResolution);
            arrow.SetShaftRadius(0.03);            
            // Cone
            vtkConeSource cone = vtkConeSource.New();
            cone.SetResolution(21);
            cone.SetHeight(0.3);
            cone.SetRadius(0.1);
            cone.SetCenter(0.65, 0, 0);
            // Append
            vtkAppendPolyData append = vtkAppendPolyData.New();
            append.AddInput(arrow.GetOutput());
            append.AddInput(cone.GetOutput());
            // Triangulation helps with silhouette edges and 3mf export
            vtkTriangleFilter triangleFilter = vtkTriangleFilter.New();
            triangleFilter.SetInput(append.GetOutput());
            triangleFilter.Update();
            // Compute normals
            vtkPolyDataNormals normals = vtkPolyDataNormals.New();
            normals.SetInput(triangleFilter.GetOutput());
            normals.Update();
            // Set normals
            //appendFilter.GetOutput().GetPointData().SetNormals(normals.GetOutput().GetPointData().GetNormals());
            // Transform
            vtkTransform transform = vtkTransform.New();
            transform.Identity();
            transform.Translate(0.05, 0, 0);
            // Transform filter
            vtkTransformFilter transformFilter = vtkTransformFilter.New();
            transformFilter.SetInput(triangleFilter.GetOutput());
            transformFilter.SetTransform(transform);
            // Glyph
            vtkGlyph3D glyph = vtkGlyph3D.New();
            glyph.SetSourceConnection(transformFilter.GetOutputPort());
            glyph.SetInputConnection(distanceToCamera.GetOutputPort());
            glyph.SetVectorModeToUseNormal();
            // Scale
            glyph.ScalingOn();
            glyph.SetScaleModeToScaleByScalar();
            glyph.SetInputArrayToProcess(0, 0, 0, "vtkDataObject::FIELD_ASSOCIATION_POINTS", "DistanceToCamera");
            glyph.SetScaleFactor(1.0);
            glyph.OrientOn();
            glyph.Update();
            // Mapper
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(glyph.GetOutputPort());
            mapper.ScalarVisibilityOff();
            // Actor
            data.Name += Globals.NameSeparator + "doubleArrow";
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            // Add
            ApplySymbolFormattingToActor(actor);
            AddActorGeometry(actor, data.Layer);
            //
            if (_drawSymbolEdges) AddSymbolEdges(data, glyph.GetOutputPort());
        }
        public void AddOrientedSpringActor(vtkMaxActorData data, double symbolSize, bool invert)
        {
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;
            // Points
            vtkPoints pointData = vtkPoints.New();
            for (int i = 0; i < data.Geometry.Nodes.Coor.GetLength(0); i++)
            {
                pointData.InsertNextPoint(data.Geometry.Nodes.Coor[i][0], data.Geometry.Nodes.Coor[i][1],
                                          data.Geometry.Nodes.Coor[i][2]);
            }
            // Normals
            vtkDoubleArray pointNormalsArray = vtkDoubleArray.New();
            pointNormalsArray.SetNumberOfComponents(3); //3d normals (ie x,y,z)
            for (int i = 0; i < data.Geometry.Nodes.Normals.GetLength(0); i++)
            {
                pointNormalsArray.InsertNextTuple3(data.Geometry.Nodes.Normals[i][0], data.Geometry.Nodes.Normals[i][1],
                                                   data.Geometry.Nodes.Normals[i][2]);
            }
            // Poly data
            vtkPolyData polyData = vtkPolyData.New();
            polyData.SetPoints(pointData);
            polyData.GetPointData().SetNormals(pointNormalsArray);
            // Calculate the distance to the camera of each point.
            vtkDistanceToCamera distanceToCamera = vtkDistanceToCamera.New();
            distanceToCamera.SetInput(polyData);
            distanceToCamera.SetScreenSize(symbolSize);
            distanceToCamera.SetRenderer(_renderer);
            // Source for the glyph filter
            vtkPolyData lineData;
            vtkPolyData spring = CreateSpringSource(0.2, 1, 8, 10, out lineData);
            // Transform
            vtkTransform transform = vtkTransform.New();
            transform.Identity();
            //if (invert) transform.Translate(-1.05, 0, 0);
            //else transform.Translate(0.05, 0, 0);
            transform.Translate(-1.05, 0, 0);
            // Transform filter
            vtkTransformFilter transformFilter = vtkTransformFilter.New();
            transformFilter.SetInput(spring);
            transformFilter.SetTransform(transform);
            transformFilter.Update();
            // Glyph
            vtkGlyph3D glyph = vtkGlyph3D.New();
            glyph.SetSourceConnection(transformFilter.GetOutputPort());
            glyph.SetInputConnection(distanceToCamera.GetOutputPort());
            glyph.SetVectorModeToUseNormal();
            // Scale
            glyph.ScalingOn();
            glyph.SetScaleModeToScaleByScalar();
            glyph.SetInputArrayToProcess(0, 0, 0, "vtkDataObject::FIELD_ASSOCIATION_POINTS", "DistanceToCamera");
            glyph.SetScaleFactor(1.0);
            glyph.OrientOn();
            glyph.Update();
            // Mapper
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(0, glyph.GetOutputPort());
            mapper.ScalarVisibilityOff();
            // Actor
            data.Name += Globals.NameSeparator + "spring";
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            actor.GeometryProperty.SetRepresentationToSurface();
            // Add
            ApplySymbolFormattingToActor(actor);
            AddActorGeometry(actor, data.Layer);
            //
            if (_drawSymbolEdges) AddSymbolEdges(data, glyph.GetOutputPort());
        }
        private vtkPolyData CreateSpringSource(double diameter, double height, int numOfTurns, int radialResolution,
                                                      out vtkPolyData lineData)
        {
            // Points
            double r = diameter / 2;
            double delta;
            double phi = 0;
            double phiDelta = Math.PI / radialResolution;
            double linPart = diameter;
            double hDelta = (height - 2 * linPart) / (numOfTurns * radialResolution);
            int linResolution = 3;
            int numOfPoints = numOfTurns * radialResolution + 1 + 2 * 2 * linResolution;
            int count = 0;
            //
            double[][] points = new double[numOfPoints][];
            // Start
            delta = linPart / linResolution;
            for (int i = 0; i < linResolution; i++) points[count++] = new double[] { delta * i, 0, 0 };
            delta = r / linResolution;
            for (int i = 0; i < linResolution; i++) points[count++] = new double[] { linPart, delta * i, 0 };
            // Helix
            for (int i = 0; i <= numOfTurns * radialResolution; i++)
            {
                points[count++] = new double[] { linPart + hDelta * i, r * Math.Cos(phi), r * Math.Sin(phi) };
                phi += phiDelta;
            }
            // End
            delta = r / linResolution;
            for (int i = 0; i < linResolution; i++) points[count++] = new double[] { height - linPart, r - delta * (i + 1), 0, 0 };
            delta = linPart / linResolution;
            for (int i = 0; i < linResolution; i++) points[count++] = new double[] { height - linPart + delta * (i + 1), 0, 0 };
            //
            points = SmoothLinePoints(points, 3);
            //
            vtkPoints vtkPoints = vtkPoints.New();
            foreach (var point in points) vtkPoints.InsertNextPoint(point[0], point[1], point[2]);
            // Polyline
            vtkPolyLine polyLine = vtkPolyLine.New();
            polyLine.GetPointIds().SetNumberOfIds(numOfPoints);
            for (int i = 0; i < numOfPoints; i++) polyLine.GetPointIds().SetId(i, i);
            // Create a cell array to store the lines in and add the lines to it
            vtkCellArray cells = vtkCellArray.New();
            cells.InsertNextCell(polyLine);
            // Poly data
            lineData = vtkPolyData.New();
            lineData.SetPoints(vtkPoints);
            lineData.SetLines(cells);
            // Create tube filter
            vtkTubeFilter tubeFilter = vtkTubeFilter.New();
            tubeFilter.SetInput(lineData);
            tubeFilter.SetRadius(0.03);
            tubeFilter.SetNumberOfSides(10);
            tubeFilter.CappingOn();
            tubeFilter.UseDefaultNormalOff();
            tubeFilter.Update();
            // Triangulation helps with silhouette edges and 3mf export
            vtkTriangleFilter triangleFilter = vtkTriangleFilter.New();
            triangleFilter.SetInput(tubeFilter.GetOutput());
            triangleFilter.Update();
            //
            return triangleFilter.GetOutput();
        }
        private double[][] SmoothLinePoints(double[][] points, int numOfPoints)
        {
            int half = numOfPoints / 2;
            int evenNumOfPoints = half * 2 + 1;
            double[][] smooth = new double[points.Length][];
            double[] newPoint;
            //
            for (int i = 0; i < points.Length; i++)
            {
                if (i < half || i >= points.Length - half)
                {
                    smooth[i] = points[i];
                }
                else
                {
                    newPoint = new double[3];
                    for (int j = -half; j <= half; j++)
                    {
                        newPoint[0] += points[i + j][0];
                        newPoint[1] += points[i + j][1];
                        newPoint[2] += points[i + j][2];
                    }
                    newPoint[0] /= evenNumOfPoints;
                    newPoint[1] /= evenNumOfPoints;
                    newPoint[2] /= evenNumOfPoints;
                    //
                    smooth[i] = newPoint;
                }
            }
            //
            return smooth;
        }
        public void AddOrientedThermoActor(vtkMaxActorData data, double symbolSize, bool invert)
        {
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;
            // Points
            vtkPoints pointData = vtkPoints.New();
            for (int i = 0; i < data.Geometry.Nodes.Coor.GetLength(0); i++)
            {
                pointData.InsertNextPoint(data.Geometry.Nodes.Coor[i][0], data.Geometry.Nodes.Coor[i][1],
                                          data.Geometry.Nodes.Coor[i][2]);
            }
            // Normals
            vtkDoubleArray pointNormalsArray = vtkDoubleArray.New();
            pointNormalsArray.SetNumberOfComponents(3); //3d normals (ie x,y,z)
            for (int i = 0; i < data.Geometry.Nodes.Normals.GetLength(0); i++)
            {
                pointNormalsArray.InsertNextTuple3(data.Geometry.Nodes.Normals[i][0], data.Geometry.Nodes.Normals[i][1],
                                                   data.Geometry.Nodes.Normals[i][2]);
            }
            // Poly data
            vtkPolyData polyData = vtkPolyData.New();
            polyData.SetPoints(pointData);
            polyData.GetPointData().SetNormals(pointNormalsArray);
            // Calculate the distance to the camera of each point.
            vtkDistanceToCamera distanceToCamera = vtkDistanceToCamera.New();
            distanceToCamera.SetInput(polyData);
            distanceToCamera.SetScreenSize(symbolSize);
            distanceToCamera.SetRenderer(_renderer);
            // Sphere
            double rs = 0.2;
            double rc = 0.1;
            vtkSphereSource sphereSource = vtkSphereSource.New();
            sphereSource.SetRadius(rs);
            sphereSource.SetPhiResolution(11);
            sphereSource.SetThetaResolution(11);
            sphereSource.SetCenter(rs, 0, 0);
            // Cylinder
            vtkCylinderSource cylinderSource = vtkCylinderSource.New();
            cylinderSource.SetRadius(rc);
            cylinderSource.SetHeight(1 - rs);
            cylinderSource.SetResolution(7);
            cylinderSource.SetCenter(0, 0.5 + rs, 0);
            //Transform
            vtkTransform transform = vtkTransform.New();
            transform.Identity();
            transform.RotateZ(-90);
            // Transform filter
            vtkTransformFilter transformFilter = vtkTransformFilter.New();
            transformFilter.SetInput(cylinderSource.GetOutput());
            transformFilter.SetTransform(transform);
            // Append
            vtkAppendPolyData append = vtkAppendPolyData.New();
            append.AddInput(sphereSource.GetOutput());
            append.AddInput(transformFilter.GetOutput());
            // Triangulation helps with silhouette edges and 3mf export
            vtkTriangleFilter triangleFilter = vtkTriangleFilter.New();
            triangleFilter.SetInput(append.GetOutput());
            triangleFilter.Update();
            // Compute normals
            vtkPolyDataNormals normals = vtkPolyDataNormals.New();
            normals.SetInput(triangleFilter.GetOutput());
            normals.Update();
            // Set normals
            triangleFilter.GetOutput().GetPointData().SetNormals(normals.GetOutput().GetPointData().GetNormals());
            // Transform
            transform = vtkTransform.New();
            transform.Identity();
            if (invert) transform.Translate(-1.05, 0, 0);
            else transform.Translate(0.05, 0, 0);
            // Transform filter
            transformFilter = vtkTransformFilter.New();
            transformFilter.SetInput(triangleFilter.GetOutput());
            transformFilter.SetTransform(transform);
            // Glyph
            vtkGlyph3D glyph = vtkGlyph3D.New();
            glyph.SetSourceConnection(transformFilter.GetOutputPort());
            glyph.SetInputConnection(distanceToCamera.GetOutputPort());
            glyph.SetVectorModeToUseNormal();
            // Scale
            glyph.ScalingOn();
            glyph.SetScaleModeToScaleByScalar();
            glyph.SetInputArrayToProcess(0, 0, 0, "vtkDataObject::FIELD_ASSOCIATION_POINTS", "DistanceToCamera");
            glyph.SetScaleFactor(0.5);
            glyph.OrientOn();
            glyph.Update();
            // Mapper
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(0, glyph.GetOutputPort());
            mapper.ScalarVisibilityOff();
            // Actor
            data.Name += Globals.NameSeparator + "thermo";
            vtkMaxActor actor = new vtkMaxActor(data, mapper);            
            // Add
            ApplySymbolFormattingToActor(actor);
            AddActorGeometry(actor, data.Layer);
            //
            if (_drawSymbolEdges) AddSymbolEdges(data, glyph.GetOutputPort());
        }
        public void AddOrientedFluxActor(vtkMaxActorData data, double symbolSize, bool center, bool invert)
        {
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;
            // Points
            vtkPoints pointData = vtkPoints.New();
            for (int i = 0; i < data.Geometry.Nodes.Coor.GetLength(0); i++)
            {
                pointData.InsertNextPoint(data.Geometry.Nodes.Coor[i][0], data.Geometry.Nodes.Coor[i][1],
                                          data.Geometry.Nodes.Coor[i][2]);
            }
            // Normals
            vtkDoubleArray pointNormalsArray = vtkDoubleArray.New();
            pointNormalsArray.SetNumberOfComponents(3); //3d normals (ie x,y,z)
            for (int i = 0; i < data.Geometry.Nodes.Normals.GetLength(0); i++)
            {
                pointNormalsArray.InsertNextTuple3(data.Geometry.Nodes.Normals[i][0], data.Geometry.Nodes.Normals[i][1],
                                                   data.Geometry.Nodes.Normals[i][2]);
            }
            // Poly data
            vtkPolyData polyData = vtkPolyData.New();
            polyData.SetPoints(pointData);
            polyData.GetPointData().SetNormals(pointNormalsArray);
            // Calculate the distance to the camera of each point.
            vtkDistanceToCamera distanceToCamera = vtkDistanceToCamera.New();
            distanceToCamera.SetInput(polyData);
            distanceToCamera.SetScreenSize(symbolSize);
            distanceToCamera.SetRenderer(_renderer);
            // Sphere
            double rs = 0.2;
            vtkSphereSource sphereSource = vtkSphereSource.New();
            sphereSource.SetRadius(rs);
            sphereSource.SetPhiResolution(9);
            sphereSource.SetThetaResolution(11);
            if (center) rs = 0;
            sphereSource.SetCenter(rs, 0, 0);
            // Triangulation helps with silhouette edges and 3mf export
            vtkTriangleFilter triangleFilter = vtkTriangleFilter.New();
            triangleFilter.SetInput(sphereSource.GetOutput());
            triangleFilter.Update();
            // Compute normals
            vtkPolyDataNormals normals = vtkPolyDataNormals.New();
            normals.SetInput(triangleFilter.GetOutput());
            normals.Update();
            // Set normals
            //triangleFilter.GetOutput().GetPointData().SetNormals(normals.GetOutput().GetPointData().GetNormals());
            // Transform
            vtkTransform transform = vtkTransform.New();
            transform.Identity();
            if (invert) transform.Translate(-1.05, 0, 0);
            else transform.Translate(0.05, 0, 0);
            // Transform filter
            vtkTransformFilter transformFilter = vtkTransformFilter.New();
            transformFilter.SetInput(triangleFilter.GetOutput());
            transformFilter.SetTransform(transform);
            // Glyph
            vtkGlyph3D glyph = vtkGlyph3D.New();
            glyph.SetSourceConnection(transformFilter.GetOutputPort());
            glyph.SetInputConnection(distanceToCamera.GetOutputPort());
            glyph.SetVectorModeToUseNormal();
            // Scale
            glyph.ScalingOn();
            glyph.SetScaleModeToScaleByScalar();
            glyph.SetInputArrayToProcess(0, 0, 0, "vtkDataObject::FIELD_ASSOCIATION_POINTS", "DistanceToCamera");
            glyph.SetScaleFactor(0.5);
            glyph.OrientOn();
            glyph.Update();
            // Mapper
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(0, glyph.GetOutputPort());
            mapper.ScalarVisibilityOff();
            // Actor
            data.Name += Globals.NameSeparator + "flux";
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            // Add
            ApplySymbolFormattingToActor(actor);
            AddActorGeometry(actor, data.Layer);
            //
            if (_drawSymbolEdges) AddSymbolEdges(data, glyph.GetOutputPort());
        }
        private void AddSymbolEdges(vtkMaxActorData data, vtkAlgorithmOutput output)
        {
            // Compute the silhouette                                                        
            vtkPolyDataSilhouette silhouette = vtkPolyDataSilhouette.New();
            silhouette.SetInputConnection(0, output);
            silhouette.SetCamera(_renderer.GetActiveCamera());
            silhouette.SetEnableFeatureAngle(1);
            silhouette.SetFeatureAngle(60);
            silhouette.Update();
            // Mapper
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(silhouette.GetOutputPort());
            // Actor
            data.Name += Globals.NameSeparator + "silhouette";
            bool pickable = data.Pickable;
            data.Pickable = false;
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            data.Pickable = pickable;
            // Backface color is used for annotateWithColor
            if (data.BackfaceColor != Color.Empty) actor.Color = data.BackfaceColor;
            //
            actor.GeometryProperty.SetLineWidth(1.5f);
            //
            double k = 0.2;
            actor.Color = Color.FromArgb((int)(actor.Color.R + (255 - actor.Color.R) * k),
                                         (int)(actor.Color.G + (255 - actor.Color.G) * k),
                                         (int)(actor.Color.B + (255 - actor.Color.B) * k));
            // Add
            AddActorGeometry(actor, data.Layer);
        }
        //
        public void AddActor(vtkMaxActor actor)
        {
            AddActor(actor, actor.Layer);
        }
        private void AddActor(vtkMaxActor actor, vtkRendererLayer layer)
        {
            // Add actor
            AddActorGeometry(actor, layer);
            // Add actorElementEdges
            if (actor.ElementEdges != null) AddActorEdges(actor, false, layer);
            // Add modelEdges
            if (actor.ModelEdges != null) AddActorEdges(actor, true, layer);
            // Add nodes
            if (actor.Nodes != null) AddActorNodes(actor, layer);
        }
        private void AddActorGeometry(vtkMaxActor actor, vtkRendererLayer layer)
        {
            // Add actor
            if (layer == vtkRendererLayer.Base)
            {
                if (actor.Name == null) actor.Name = (_actors.Count + 1).ToString();
                _actors.Add(actor.Name, actor);
                _renderer.AddActor(actor.Geometry);
                //
                if (actor.Geometry.GetPickable() == 1)
                {
                    if (actor.CellLocator != null) _cellPicker.AddLocator(actor.CellLocator);
                    _propPicker.AddPickList(actor.Geometry);
                }
            }
            else if (layer == vtkRendererLayer.Overlay)
            {
                if (actor.Name == null) actor.Name = (_overlayActors.Count + 1).ToString();
                _overlayActors.Add(actor.Name, actor);
                _overlayRenderer.AddActor(actor.Geometry);
                //
                if (actor.Geometry.GetPickable() == 1)
                {
                    if (actor.CellLocator != null) _cellPicker.AddLocator(actor.CellLocator);
                    _propPicker.AddPickList(actor.Geometry);
                }
                else actor.Geometry.PickableOff();
            }
            else if (layer == vtkRendererLayer.Selection)
            {
                if (actor.Name == null) actor.Name = (_selectedActors.Count + 1).ToString();
                _selectedActors.Add(actor);
                if (actor.DrawOnGeometry) _renderer.AddActor(actor.Geometry);
                else _selectionRenderer.AddActor(actor.Geometry);
                //
                actor.Geometry.PickableOff();
                ApplySelectionFormattingToActor(actor);
            }
            //
            ApplyEdgeVisibilityAndBackfaceCullingToActor(actor.Name, actor.Geometry, actor.GeometryProperty, layer);            
            //
            UpdateTmpActor(layer);
        }
        private void AddActorCaption(vtkMaxCaptionActor actor, vtkRendererLayer layer)
        {
            // Add actor
            actor.CaptionActor.UseBoundsOff();
            //
            if (layer == vtkRendererLayer.Base)
            {
                if (actor.Name == null) actor.Name = (_actors.Count + 1).ToString();
                _actors.Add(actor.Name, actor);
                _renderer.AddActor2D(actor.CaptionActor);
            }
            else if (layer == vtkRendererLayer.Overlay)
            {
                if (actor.Name == null) actor.Name = (_overlayActors.Count + 1).ToString();
                _overlayActors.Add(actor.Name, actor);
                _overlayRenderer.AddActor2D(actor.CaptionActor);
            }
            else if (layer == vtkRendererLayer.Selection)
            {
                if (actor.Name == null) actor.Name = (_selectedActors.Count + 1).ToString();
                _selectedActors.Add(actor);
                // Caption 2d actors are added to overlay renderer since no text is rendered in selectionRenderer !!!
                _selectionRenderer.AddActor2D(actor.CaptionActor);
                ApplySelectionFormattingToCaptionActor(actor);
            }
            //
            AddTmpActor(layer, actor.Position);
        }
        private void AddActorEdges(vtkMaxActor actor, bool isModelEdge, vtkRendererLayer layer)
        {
            if (layer == vtkRendererLayer.Base)
            {
                if (actor.Name == null) actor.Name = (_actors.Count + 1).ToString();
                //
                if (_actors.ContainsKey(actor.Name))
                {
                    if (isModelEdge && _actors[actor.Name].ModelEdges != actor.ModelEdges)
                        throw new Exception("Animation changes exception.");
                    if (!isModelEdge && _actors[actor.Name].ElementEdges != actor.ElementEdges)
                        throw new Exception("Animation changes exception.");
                }
                else
                {
                    _actors.Add(actor.Name, actor);
                }
                //
                if (isModelEdge) _renderer.AddActor(actor.ModelEdges);
                else _renderer.AddActor(actor.ElementEdges);
            }
            else if (layer == vtkRendererLayer.Overlay)
            {
                _overlayActors.Add(actor.Name, actor);
                //
                if (isModelEdge) _overlayRenderer.AddActor(actor.ModelEdges);
                else _overlayRenderer.AddActor(actor.ElementEdges);
            }
            else if (layer == vtkRendererLayer.Selection)
            {                
                // Wireframe selection
                if (!_selectedActors.Contains(actor)) _selectedActors.Add(actor);
                //
                vtkActor edgeActor;
                if (isModelEdge) edgeActor = actor.ModelEdges;
                else edgeActor = actor.ElementEdges;
                //
                if (actor.DrawOnGeometry) _renderer.AddActor(edgeActor);
                else _selectionRenderer.AddActor(edgeActor);
            }
            //if (isModelEdge) ApplyEdgesFormattingToActor(actor.ModelEdges);
            //else ApplyEdgesFormattingToActor(actor.ElementEdges);
            if (isModelEdge) ApplyEdgeVisibilityAndBackfaceCullingToModelEdges(actor, actor.Name);
            else ApplyEdgeVisibilityAndBackfaceCullingToActorEdges(actor, actor.Name);
            //
            UpdateTmpActor(layer);
        }
        private void AddActorNodes(vtkMaxActor actor, vtkRendererLayer layer)
        {
            if (layer == vtkRendererLayer.Base)
            {
                if (actor.Name == null) actor.Name = (_actors.Count + 1).ToString();
                //
                if (_actors.ContainsKey(actor.Name))
                {
                    if (_actors[actor.Name].Nodes != actor.Nodes) throw new Exception("Animation changes exception.");
                }
                else
                {
                    _actors.Add(actor.Name, actor);
                }
                //
                _renderer.AddActor(actor.Nodes);
            }
            //
            UpdateTmpActor(layer);
        }
        private void AddTmpActor(vtkRendererLayer layer, double[] position)
        {
            if (layer == vtkRendererLayer.Base) _captionActorAddedToBase = true;
            else if (layer == vtkRendererLayer.Overlay) _captionActorAddedToOverlay = true;
            else if (layer == vtkRendererLayer.Selection) _captionActorAddedToSelection = true;
            //
            ((vtkPolyData)_captionTmpActor.GetMapper().GetInput()).GetPoints().SetPoint(0, position[0], position[1], position[2]);
            _captionTmpActor.GetMapper().Modified();
            //
            UpdateTmpActor(layer);
        }
        private void UpdateTmpActor(vtkRendererLayer layer)
        {
            //double[] p = ((vtkPolyData)_captionTmpActor.GetMapper().GetInput()).GetPoints().GetPoint(0);
            //
            if (_captionActorAddedToBase || _captionActorAddedToOverlay || _captionActorAddedToSelection)
            {
                if (layer == vtkRendererLayer.Base)
                {
                    if (_captionActorAddedToBase)
                    {
                        _renderer.RemoveActor(_captionTmpActor);
                        _renderer.AddActor(_captionTmpActor);   // move tmpActor to last place
                    }
                }
                else if (layer == vtkRendererLayer.Overlay)
                {
                    if (_captionActorAddedToOverlay)
                    {
                        _overlayRenderer.RemoveActor(_captionTmpActor);
                        _overlayRenderer.AddActor(_captionTmpActor);    // move tmpActor to last place
                    }
                }
                else if (layer == vtkRendererLayer.Selection)
                {
                    if (_captionActorAddedToSelection)
                    {
                        _selectionRenderer.RemoveActor(_captionTmpActor);
                        _selectionRenderer.AddActor(_captionTmpActor);  // move tmpActor to last place
                    }
                }
            }
        }

        #endregion  ################################################################################################################

        #region Highlight geometry  ################################################################################################
        public void HighlightActor(string actorName)
        {
            string sectionViewActorName = GetSectionViewActorName(actorName);
            HighlightAllActorsByName(actorName);
            if (_sectionViewData.Active) HighlightAllActorsByName(sectionViewActorName);
        }
        private void HighlightAllActorsByName(string actorName)
        {
            vtkMaxActor highlightActor = GetHighlightActorFromActorName(actorName);
            //
            if (highlightActor != null)
            {
                highlightActor.GeometryProperty.SetOpacity(1);
                AddActorGeometry(highlightActor, vtkRendererLayer.Selection);
                RenderScene();
            }
        }
        private vtkMaxActor GetHighlightActorFromActorName(string actorName)
        {
            vtkMaxActor highlightActor = GetCopyOfModelEdgesActor(actorName);
            if (highlightActor != null && highlightActor.Geometry.GetMapper().GetInput().GetNumberOfPoints() == 0)
            {
                // Silhouette
                vtkMaxActor actor = _actors[actorName];
                vtkPolyDataSilhouette silhouette = vtkPolyDataSilhouette.New();
                silhouette.SetInput(actor.GeometryMapper.GetInput());
                silhouette.SetCamera(_renderer.GetActiveCamera());
                //silhouette.SetDirectionToCameraOrigin();  // not working
                silhouette.SetDirectionToCameraVector();
                silhouette.SetBorderEdges(1); // free edges
                silhouette.SetEnableFeatureAngle(1);
                silhouette.SetFeatureAngle(30);
                //
                vtkDataSetMapper dsMapper = vtkDataSetMapper.New();
                dsMapper.SetInput(silhouette.GetOutput());
                //
                highlightActor = new vtkMaxActor();
                highlightActor.GeometryMapper = dsMapper;
                highlightActor.Geometry.PickableOff();
            }
            //
            return highlightActor;
        }
        private vtkMaxActor GetCopyOfEdgesActor(string actorName)
        {
            vtkMaxActor actor = null;
            if (_actors.ContainsKey(actorName))
            {
                vtkMaxActor actorToHighLight = _actors[actorName];
                //
                vtkPolyData data = vtkPolyData.New();
                data.DeepCopy(actorToHighLight.ElementEdges.GetMapper().GetInput());
                //
                vtkDataSetMapper mapper = vtkDataSetMapper.New();
                mapper.SetInput(data);
                //
                actor = new vtkMaxActor();
                actor.GeometryMapper = mapper;
                actor.Geometry.PickableOff();
            }
            return actor;
        }
        private vtkMaxActor GetCopyOfModelEdgesActor(string actorName)
        {
            vtkMaxActor actor = null;
            if (_actors.ContainsKey(actorName))
            {
                vtkMaxActor actorToHighLight = _actors[actorName];
                //
                vtkPolyData data = vtkPolyData.New();
                data.DeepCopy(actorToHighLight.ModelEdges.GetMapper().GetInput());
                //
                vtkDataSetMapper mapper = vtkDataSetMapper.New();
                mapper.SetInput(data);
                //
                actor = new vtkMaxActor();
                actor.GeometryMapper = mapper;
                actor.Geometry.PickableOff();
            }
            return actor;
        }
        private vtkMaxActor GetAppendedActor(string[] actorNames, Func<string, vtkMaxActor> GetActor)
        {
            if (actorNames == null || actorNames.Length == 0) return null;
            //
            if (actorNames.Length == 1) return GetActor(actorNames[0]);
            //
            bool added = false;
            vtkMaxActor appendedActor;
            vtkAppendPolyData appendFilter = vtkAppendPolyData.New();
            //
            foreach (var actorName in actorNames)
            {
                appendedActor = GetActor(actorName);
                if (appendedActor != null)
                {
                    appendFilter.AddInput(appendedActor.GeometryMapper.GetInput());
                    added = true;
                }
            }
            //
            if (added)
            {
                vtkDataSetMapper mapper = vtkDataSetMapper.New();
                mapper.SetInput(appendFilter.GetOutput());
                //
                appendedActor = new vtkMaxActor();
                appendedActor.GeometryMapper = mapper;
                appendedActor.Geometry.PickableOff();
                //
                return appendedActor;
            }
            else return null;
        }
        private vtkMaxActor GetAppendedActor(vtkMaxActor actor1, vtkMaxActor actor2)
        {
            vtkAppendPolyData appendFilter = vtkAppendPolyData.New();
            appendFilter.AddInput(actor1.GeometryMapper.GetInput());
            appendFilter.AddInput(actor2.GeometryMapper.GetInput());
            //
            vtkDataSetMapper mapper = vtkDataSetMapper.New();
            mapper.SetInput(appendFilter.GetOutput());
            //
            vtkMaxActor appendedActor = new vtkMaxActor();
            appendedActor.GeometryMapper = mapper;
            appendedActor.Geometry.PickableOff();
            //
            return appendedActor;
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
                    // Name splitting also takes care of section view actors since they have the same base name
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
            if (updateColorContours) UpdateScalarFormatting();
            //
            ApplyEdgesVisibilityAndBackfaceCulling();
            ApplyVisibilityToActorNodes();
            _style.AdjustCameraDistanceAndClipping();
        }
        private void HideShowActor(vtkMaxActor actor, bool visible)
        {
            // Use vtkMaxActor Visibility setting
            actor.VtkMaxActorVisible = visible;
            // Section view
            if (_sectionViewData.Active && actor.SectionViewActor != null)
                actor.SectionViewActor.VtkMaxActorVisible = visible;
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
            //
            if (update)
            {
                UpdateScalarFormatting();
                _style.AdjustCameraDistanceAndClipping();
                RenderScene();
            }
        }

        #endregion  ################################################################################################################

        #region Section view  ######################################################################################################
        public void CreateSectionView(double[] point, double[] normal, bool lightenColors, Color sectionColor)
        {
            lock (myLock)
            {
                if (!_sectionViewData.Active)
                {
                    _sectionViewData.Active = true;
                    // Set section cut plane
                    _sectionViewData.Plane = vtkPlane.New();
                    _sectionViewData.Plane.SetOrigin(point[0], point[1], point[2]);
                    _sectionViewData.Plane.SetNormal(normal[0], normal[1], normal[2]);
                    _sectionViewData.LightenColors = lightenColors;
                    _sectionViewData.SectionColor = sectionColor;
                    //
                    foreach (var entry in _actors)
                    {
                        if (entry.Value.SectionViewPossible) entry.Value.AddClippingPlane(_sectionViewData.Plane);
                    }
                    // Add new section cut actors
                    vtkMaxActor sectionViewActor;
                    foreach (var actor in _actors.Values.ToArray())     // must be copied to array
                    {
                        if (actor.SectionViewPossible)
                        {
                            sectionViewActor = GetSectionViewActor(actor);
                            //
                            if (sectionViewActor != null)
                            {
                                UpdateSectionViewActorColor(sectionViewActor);
                                //
                                AddActor(sectionViewActor, vtkRendererLayer.Base);
                                actor.SectionViewActor = sectionViewActor;
                            }
                        }
                    }
                    //
                    UpdateScalarFormatting();
                    //
                    RenderScene();
                }
            }
        }
        public void UpdateSectionView(double[] point, double[] normal, bool lightenColors, Color sectionColor)
        {
            lock (myLock)
            {
                if (_sectionViewData.Active)
                {
                    // Remove previous section cut actors
                    ClearSectionViewActors();
                    // Modify section cut plane
                    _sectionViewData.Plane.SetOrigin(point[0], point[1], point[2]);
                    _sectionViewData.Plane.SetNormal(normal[0], normal[1], normal[2]);
                    _sectionViewData.LightenColors = lightenColors;
                    _sectionViewData.SectionColor = sectionColor;
                    // Add new section cut actors
                    vtkMaxActor sectionViewActor;
                    foreach (var actor in _actors.Values.ToArray())
                    {
                        if (actor.SectionViewPossible)
                        {
                            sectionViewActor = GetSectionViewActor(actor);
                            //
                            if (sectionViewActor != null)
                            {
                                UpdateSectionViewActorColor(sectionViewActor);
                                //
                                AddActor(sectionViewActor, vtkRendererLayer.Base);
                                actor.SectionViewActor = sectionViewActor;
                            }
                            //GetSectionViewActorProperties(sectionViewActor, point);
                        }
                    }
                    //
                    UpdateScalarFormatting();
                    //
                    RenderScene();
                }
            }
        }
        public void RemoveSectionView()
        {
            lock (myLock)
            {
                if (_sectionViewData.Active)
                {
                    foreach (var entry in _actors) entry.Value.RemoveAllClippingPlanes();
                    // Remove previous section cut actors
                    ClearSectionViewActors();
                    //
                    _sectionViewData.Reset();
                    //
                    UpdateScalarFormatting();
                    //
                    RenderScene();
                }
            }
        }
        //
        private void ClearSectionViewActors()
        {
            vtkMaxActor sectionVewActor;
            foreach (var entry in _actors.ToArray())
            {
                sectionVewActor = entry.Value.SectionViewActor;
                if (sectionVewActor != null)
                {
                    _renderer.RemoveActor(sectionVewActor.Geometry);
                    if (sectionVewActor.ElementEdges != null) _renderer.RemoveActor(sectionVewActor.ElementEdges);
                    if (sectionVewActor.ModelEdges != null) _renderer.RemoveActor(sectionVewActor.ModelEdges);
                    //
                    _actors.Remove(sectionVewActor.Name);
                    entry.Value.SectionViewActor = null;
                }
            }
        }
        private vtkMaxActor GetSectionViewActor(vtkMaxActor actor)
        {
            try
            {
                if (actor.ActorRepresentation == vtkMaxActorRepresentation.Solid ||
                    actor.ActorRepresentation == vtkMaxActorRepresentation.Shell)
                {
                    return GetSectionViewActorFromSolid(actor);
                }
                else if (actor.ActorRepresentation == vtkMaxActorRepresentation.SolidAsShell)
                {
                    return GetSectionViewActorFromSolidAsShell(actor);
                }
                else
                    return null;
            }
            catch
            {
                return null;
            }
        }
        private void GetSectionViewActorProperties(vtkMaxActor actor, double[] point)
        {
            vtkCell cell;
            vtkPoints points;
            vtkDataSet dataSet = actor.GeometryMapper.GetInput();
            long numPoints = dataSet.GetNumberOfPoints();
            long numCells = dataSet.GetNumberOfCells();
            //
            Dictionary<int, FeNode> nodes = new Dictionary<int, FeNode>();
            for (int i = 0; i < numPoints; i++)
            {
                nodes.Add(i, new FeNode(i, dataSet.GetPoint(i)));
            }
            int[] nodeIds;
            Dictionary<int, FeElement> elements = new Dictionary<int, FeElement>();
            for (int i = 0; i < numCells; i++)
            {
                cell = dataSet.GetCell(i);
                if (cell.GetCellType() == (int)vtkCellType.VTK_TRIANGLE)
                {
                    vtkTriangle triangle = (vtkTriangle)cell;
                    nodeIds = new int[] { (int)triangle.GetPointId(0), (int)triangle.GetPointId(1), (int)triangle.GetPointId(2) };
                    elements.Add(i, new LinearTriangleElement(i, nodeIds));
                }
            }
            FeMesh mesh = new FeMesh(nodes, elements, MeshRepresentation.Geometry, ImportOptions.DetectEdges);
            //
            double sumA = -1;
            double sumL = -1;
            double[] faceCenter = new double[3];
            if (mesh.Parts.Count == 1)
            {
                VisualizationData vis = mesh.Parts.First().Value.Visualization;
                if (vis.FaceCount == 1)
                {
                    sumA = vis.FaceAreas[0];
                    sumL = 0;
                    for (int i = 0; i < vis.FaceEdgeIds[0].Length; i++) sumL += vis.EdgeLengths[i];
                }
                //
                double A;
                double[] elementFaceCenter;
                foreach (var entry in elements)
                {
                    mesh.GetElementFaceCenterAndNormal(entry.Value, FeFaceName.S2, out elementFaceCenter, out _, out _);
                    A = entry.Value.GetArea(FeFaceName.S2, nodes);
                    //
                    faceCenter[0] += elementFaceCenter[0] * A;
                    faceCenter[1] += elementFaceCenter[1] * A;
                    faceCenter[2] += elementFaceCenter[2] * A;
                }
                faceCenter[0] /= sumA;
                faceCenter[1] /= sumA;
                faceCenter[2] /= sumA;
            }
            //
            string data = string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}{8}", point[0], point[1], point[2],
                                        sumA, sumL, faceCenter[0], faceCenter[1], faceCenter[2],
                                        Environment.NewLine);
            File.AppendAllText(@"C:\Temp\CrossSectionData.txt", data);
            
        }
        private vtkMaxActor GetSectionViewActorFromSolid(vtkMaxActor actor)
        {
            vtkMaxActor sectionViewActor = new vtkMaxActor(actor);
            sectionViewActor.Name = GetSectionViewActorName(actor.Name);
            sectionViewActor.BackfaceCulling = false;
            // Get the unstructured grid
            vtkUnstructuredGrid grid = vtkUnstructuredGrid.New();
            grid.DeepCopy(actor.FrustumCellLocator.GetDataSet());
            // Create a cutter
            vtkCutter cutter = vtkCutter.New();
            cutter.SetCutFunction(_sectionViewData.Plane);
            cutter.SetInput(grid);
            cutter.GenerateCutScalarsOff();
            cutter.Update();
            // GEOMETRY  ##########################################################################
            // Create poly data for the geometry actor
            vtkGeometryFilter geometryFilter = vtkGeometryFilter.New();
            geometryFilter.SetInput(cutter.GetOutput());
            geometryFilter.Update();
            vtkPolyData polyData = geometryFilter.GetOutput();
            // Create the mapper for the geometry actor
            vtkPolyDataMapper polyMapper = vtkPolyDataMapper.New();
            polyMapper.SetInput(polyData);
            // Create actor
            sectionViewActor.Geometry = vtkActor.New();
            sectionViewActor.GeometryMapper = polyMapper;
            sectionViewActor.GeometryProperty.DeepCopy(actor.GeometryProperty);
            sectionViewActor.Geometry.PickableOff();
            sectionViewActor.Geometry.SetVisibility(actor.Geometry.GetVisibility());
            // EDGES  #############################################################################
            // The best function would be:                                                          
            //  - vtkExtractEdges                                                                   
            // but vtkExtractEdges produces internal edges for quadratic cells                      
            // The solution:                                                                        
            //  - extract the faces of all cells and cut the faces to get cell edges in the plane   
            //  - to make it faster first find the cells on the plane                               
            vtkExtractGeometry extract = vtkExtractGeometry.New();
            extract.SetImplicitFunction(_sectionViewData.Plane);
            extract.ExtractBoundaryCellsOn();
            extract.ExtractOnlyBoundaryCellsOn();
            extract.SetInput(grid);
            extract.Update();
            //
            vtkCell cell;
            vtkCell cellFace;
            vtkUnstructuredGrid cellGrid = vtkUnstructuredGrid.New();
            cellGrid.SetPoints(extract.GetOutput().GetPoints());
            //
            for (int i = 0; i < extract.GetOutput().GetNumberOfCells(); i++)
            {
                cell = extract.GetOutput().GetCell(i);
                for (int j = 0; j < cell.GetNumberOfFaces(); j++)
                {
                    cellFace = cell.GetFace(j);
                    cellGrid.InsertNextCell(cellFace.GetCellType(), cellFace.GetPointIds());
                }
            }
            cellGrid.Update();
            // Sometimes when cutting with a parallel plane, no section is found                                
            // The solution:                                                                                    
            //  - move the cutting plane to find some edges                                                     
            //  - project the edges back to the plane to prevent them from hiding behind the section cut surface
            vtkPlane cutPlane = vtkPlane.New();
            double[] origin = _sectionViewData.Plane.GetOrigin();
            double[] normal = _sectionViewData.Plane.GetNormal();
            cutPlane.SetOrigin(origin[0], origin[1], origin[2]);
            cutPlane.SetNormal(normal[0], normal[1], normal[2]);
            //
            cutter = vtkCutter.New();
            cutter.SetCutFunction(cutPlane);
            cutter.SetInput(cellGrid);
            cutter.GenerateCutScalarsOff();
            cutter.Update();
            //
            int count = 0;
            int step = 0;
            double delta = 0.01;
            double[] b = GetBoundingBoxSize();
            double[] v = new double[] { b[0] * delta * normal[0], b[1] * delta * normal[1], b[2] * delta * normal[2] };
            //
            if (cellGrid.GetNumberOfCells() > 0 && cutter.GetOutput().GetNumberOfCells() == 0)
            {
                // Try to move the cut plane forward and backward   
                while (count++ <= 4)
                {
                    if (cutter.GetOutput().GetNumberOfCells() == 0)
                    {
                        step = (int)Math.Pow(-1, count) * (count + 1) / 2;
                        cutPlane.SetOrigin(origin[0] + step * v[0], origin[1] + step * v[1], origin[2] + step * v[2]);
                        cutter.Update();
                    }
                    else
                    {
                        // The section was found - move the points back to the plane
                        vtkPoints points = cutter.GetOutput().GetPoints();
                        double[] coor;
                        for (int i = 0; i < points.GetNumberOfPoints(); i++)
                        {
                            coor = points.GetPoint(i);
                            coor[0] -= step * v[0];
                            coor[1] -= step * v[1];
                            coor[2] -= step * v[2];
                            points.SetPoint(i, coor[0], coor[1], coor[2]);
                        }
                        break;
                    }
                }
            }
            // Create the mapper for the element edges actor
            polyMapper = vtkPolyDataMapper.New();
            polyMapper.SetInput(cutter.GetOutput());
            // Create actor
            sectionViewActor.ElementEdges = vtkActor.New();
            sectionViewActor.ElementEdges.SetMapper(polyMapper);
            sectionViewActor.ElementEdgesProperty.DeepCopy(actor.ElementEdgesProperty);
            sectionViewActor.ElementEdges.PickableOff();
            sectionViewActor.ElementEdges.SetVisibility(actor.ElementEdges.GetVisibility());
            sectionViewActor.ElementEdges.GetMapper().GetInput().GetPointData().RemoveArray(Globals.ScalarArrayName);
            // MODEL EDGES  #######################################################################
            vtkFeatureEdges featureEdges = vtkFeatureEdges.New();
            featureEdges.SetInput(polyData);
            featureEdges.BoundaryEdgesOn();
            featureEdges.FeatureEdgesOff();
            featureEdges.ManifoldEdgesOff();
            featureEdges.NonManifoldEdgesOff();
            featureEdges.SetColoring(0);
            featureEdges.Update();
            // Create the mapper for the model edges actor
            polyMapper = vtkPolyDataMapper.New();
            polyMapper.SetInput(featureEdges.GetOutput());
            // Create actor
            sectionViewActor.ModelEdges = vtkActor.New();
            sectionViewActor.ModelEdges.SetMapper(polyMapper);
            sectionViewActor.ModelEdgesProperty.DeepCopy(actor.ModelEdgesProperty);
            sectionViewActor.ModelEdges.PickableOff();
            sectionViewActor.ModelEdges.SetVisibility(actor.ModelEdges.GetVisibility());
            sectionViewActor.ModelEdges.GetMapper().GetInput().GetPointData().RemoveArray(Globals.ScalarArrayName);
            //
            return sectionViewActor;
        }
        private vtkMaxActor GetSectionViewActorFromSolidAsShell(vtkMaxActor actor)
        {
            vtkMaxActor sectionViewActor = new vtkMaxActor(actor);
            sectionViewActor.Name = GetSectionViewActorName(actor.Name);
            sectionViewActor.BackfaceCulling = false;            

            // Get the unstructured grid
            vtkUnstructuredGrid grid = vtkUnstructuredGrid.New();
            grid.DeepCopy(actor.FrustumCellLocator.GetDataSet());

            // Create a cutter
            vtkCutter cutter = vtkCutter.New();
            cutter.SetCutFunction(_sectionViewData.Plane);
            //cutter.SetInput(grid);
            cutter.SetInput(actor.GeometryMapper.GetInput());
            cutter.Update();

            vtkStripper cutStrips = vtkStripper.New();
            cutStrips.SetInput(cutter.GetOutput());
            cutStrips.Update();

            vtkPolyData polyData;
            vtkPolyDataMapper polyMapper;
            ///////////////////////////////////////////////////////////////////////////////////////
            polyData = vtkPolyData.New();
            polyMapper = vtkPolyDataMapper.New();
            polyMapper.SetInput(polyData);

            sectionViewActor.Geometry = vtkActor.New();
            sectionViewActor.GeometryMapper = polyMapper;

            polyData = cutStrips.GetOutput();
            polyMapper = vtkPolyDataMapper.New();
            polyMapper.SetInput(polyData);

            sectionViewActor.ElementEdges = vtkActor.New();
            sectionViewActor.ElementEdges.SetMapper(polyMapper);
            sectionViewActor.ElementEdgesProperty.DeepCopy(actor.ElementEdgesProperty);
            sectionViewActor.ElementEdges.PickableOff();
            sectionViewActor.ElementEdges.SetVisibility(actor.ElementEdges.GetVisibility());

            sectionViewActor.ModelEdges = vtkActor.New();
            sectionViewActor.ModelEdges.SetMapper(polyMapper);
            sectionViewActor.ModelEdgesProperty.DeepCopy(actor.ModelEdgesProperty);
            sectionViewActor.ModelEdges.PickableOff();
            sectionViewActor.ModelEdges.SetVisibility(actor.ModelEdges.GetVisibility());

            return sectionViewActor;
            ///////////////////////////////////////////////////////////////////////////////////////

            if (cutter.GetOutput().GetNumberOfPoints() == 0) return null;

            //System.Diagnostics.Debug.WriteLine("Cutter num. of points: " + cutter.GetOutput().GetNumberOfPoints());
            //System.Diagnostics.Debug.WriteLine("Cutter num. of cells: " + cutter.GetOutput().GetNumberOfCells());
            
            // GEOMETRY                                                                             
            //System.Diagnostics.Debug.WriteLine("CutStrips num. of points: " + cutStrips.GetOutput().GetNumberOfPoints());
            //System.Diagnostics.Debug.WriteLine("CutStrips num. of cells: " + cutStrips.GetOutput().GetNumberOfCells());

           
            // Normal
            double[] n = _sectionViewData.Plane.GetNormal();
            // Axis = n X z(0,0,1)
            double[] axis = new double[] { n[1], -n[0], 0 };
            // Angle
            double l2 = Math.Pow(n[0], 2) + Math.Pow(n[1], 2) + Math.Pow(n[2], 2);
            double phi = Math.Acos(n[2] / Math.Sqrt(l2));
            vtkTransform transform = vtkTransform.New();
            transform.RotateWXYZ(phi, axis[0], axis[1], axis[2]);

            n[0] = Math.Abs(n[0]);
            n[1] = Math.Abs(n[1]);
            n[2] = Math.Abs(n[2]);

            if (cutStrips.GetOutput().GetNumberOfCells() > 1
                && cutStrips.GetOutput().GetCell(0) is vtkPolyLine
                && n[2] == Math.Max(Math.Max(n[0], n[1]), n[2])
                //&& false
                )
            {                
                polyData = cutStrips.GetOutput();

                vtkPolyLine line0 = vtkPolyLine.New();
                vtkPolyLine line1 = vtkPolyLine.New();
                line0.DeepCopy(polyData.GetCell(0));
                line1.DeepCopy(polyData.GetCell(1));


                // Define a polygonal hole with a clockwise polygon
                vtkPolygon polygon0 = vtkPolygon.New();
                vtkPolygon polygon1 = vtkPolygon.New();
                double[] coor;
                long id;

                for (int i = 0; i < line0.GetNumberOfPoints(); i++)
                //for (int i = (int)line0.GetNumberOfPoints() - 1; i >= 0 ; i--)
                {
                    id = line0.GetPointId(i);
                    polygon0.GetPointIds().InsertNextId(id);
                    coor = line0.GetPoints().GetPoint(i);
                    polygon0.GetPoints().InsertNextPoint(coor[0], coor[1], coor[2]);
                }

                for (int i = 0; i < line1.GetNumberOfPoints(); i++)
                //for (int i = (int)line1.GetNumberOfPoints() - 1; i >= 0 ; i--)
                {
                    id = line1.GetPointId(i);
                    polygon1.GetPointIds().InsertNextId(id);
                    coor = line1.GetPoints().GetPoint(i);
                    polygon1.GetPoints().InsertNextPoint(coor[0], coor[1], coor[2]);
                }

                double[] bounds0 = polygon0.GetBounds();
                double[] bounds1 = polygon1.GetBounds();

                vtkDelaunay3D test = vtkDelaunay3D.New();

                // Create a cell array to store the polygon in
                vtkCellArray polygonArray = vtkCellArray.New();
                //polygonArray.InsertNextCell(polygon0);
                //polygonArray.InsertNextCell(polygon1);

                vtkPolyData allPoints = vtkPolyData.New();
                allPoints.SetPoints(cutStrips.GetOutput().GetPoints());

                // Create a poly data to store the boundary. The points must be the
                // same as the points we will triangulate.
                vtkPolyData boundary = vtkPolyData.New();       // this is the boundary
                boundary.SetPoints(allPoints.GetPoints());
                boundary.SetPolys(polygonArray);

                // Triangulate the grid points
                vtkDelaunay2D delaunay = vtkDelaunay2D.New();
                delaunay.SetTransform(transform);
                delaunay.SetInput(0, allPoints);
                delaunay.SetInput(1, boundary);
                delaunay.Update();

                polyData = delaunay.GetOutput();

                //polyData = boundary;

                //if (polyData.GetNumberOfPoints() == 0)
                //    return null;

                polyMapper = vtkPolyDataMapper.New();
                polyMapper.SetInput(polyData);
                //polyMapper.SetInputConnection(delaunay.GetOutputPort());
            }
            else
            {
                polyData = vtkPolyData.New();
                polyData.SetPoints(cutStrips.GetOutput().GetPoints());
                polyData.SetPolys(cutStrips.GetOutput().GetLines());

                vtkTriangleFilter cutTriangles = vtkTriangleFilter.New();
                cutTriangles.SetInput(polyData);
                cutTriangles.Update();

                polyData = cutTriangles.GetOutput();

                // Create the mapper for the geometry actor
                polyMapper = vtkPolyDataMapper.New();
                polyMapper.SetInput(polyData);
                //polyMapper.SetInputConnection(cutTriangles.GetOutputPort());
            }

            if (polyData.GetNumberOfPoints() == 0) return null;

            // Create Actor
            sectionViewActor.Geometry = vtkActor.New();
            sectionViewActor.GeometryMapper = polyMapper;
            sectionViewActor.GeometryProperty.DeepCopy(actor.GeometryProperty);
            sectionViewActor.Geometry.PickableOff();
            sectionViewActor.Geometry.SetVisibility(actor.Geometry.GetVisibility());
            ////////////////////////////////////////////////////////////////////////////////////////
            //vtkClipClosedSurface clipClosed = vtkClipClosedSurface.New();                         
            ////////////////////////////////////////////////////////////////////////////////////////

            // EDGES                                                                                

            //System.Diagnostics.Debug.WriteLine("Num. of points: " + polyData.GetNumberOfPoints());
            //System.Diagnostics.Debug.WriteLine("Num. of cells: " + polyData.GetNumberOfCells());

            vtkExtractEdges extractEdges = vtkExtractEdges.New();
            extractEdges.SetInput(polyData);
            extractEdges.Update();

            // Create the mapper for the geometry actor
            polyMapper = vtkPolyDataMapper.New();
            polyMapper.SetInput(extractEdges.GetOutput());

            sectionViewActor.ElementEdges = vtkActor.New();
            sectionViewActor.ElementEdges.SetMapper(polyMapper);
            sectionViewActor.ElementEdgesProperty.DeepCopy(actor.ElementEdgesProperty);
            sectionViewActor.ElementEdges.PickableOff();
            sectionViewActor.ElementEdges.SetVisibility(actor.ElementEdges.GetVisibility());


            // MODEL EDGES                                                                          

            vtkFeatureEdges featureEdges = vtkFeatureEdges.New();
            featureEdges.SetInput(polyData);
            featureEdges.BoundaryEdgesOn();
            featureEdges.FeatureEdgesOff();
            featureEdges.ManifoldEdgesOff();
            featureEdges.NonManifoldEdgesOff();
            featureEdges.SetColoring(0);
            featureEdges.Update();

            // Create the mapper for the geometry actor
            polyMapper = vtkPolyDataMapper.New();
            //polyMapper.SetInput(featureEdges.GetOutput());
            polyMapper.SetInput(cutStrips.GetOutput());

            sectionViewActor.ModelEdges = vtkActor.New();
            sectionViewActor.ModelEdges.SetMapper(polyMapper);
            sectionViewActor.ModelEdgesProperty.DeepCopy(actor.ModelEdgesProperty);
            sectionViewActor.ModelEdges.PickableOff();
            sectionViewActor.ModelEdges.SetVisibility(actor.ModelEdges.GetVisibility());

            return sectionViewActor;
        }
        //
        private string GetSectionViewActorName(string actorName)
        {
            string name = actorName + Globals.NameSeparator + Globals.SectionViewSuffix + Globals.NameSeparator;
            name = _actors.GetNextNumberedKey(name);
            return name;
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
                    offsetStart = actor.Geometry.GetPosition();
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
                //t = 0.5 - 0.5 * Math.Cos(t * Math.PI);
                t = 0.5 + 0.5 * Math.Atan(t * 6 - 3) / Math.Atan(3);
                //
                foreach (var partOffset in partOffsetsAnimation)
                {
                    position.Coor = partOffset.Value;
                    position *= (t - tPrev);
                    foreach (vtkMaxActor partActor in partActors[partOffset.Key])
                    {
                        Vec3D actorPosition = new Vec3D(partActor.Geometry.GetPosition());
                        actorPosition += position;
                        SetExplodedViewActorPosition(partActor, actorPosition.Coor);
                    }
                }
                tPrev = t;
                //
                AdjustCameraDistanceAndClipping();
                //
                RenderScene();
                //
                Application.DoEvents();
            }
            while (t < 1);
        }
        public void RemovePreviewedExplodedView(string[] partNames)
        {
            double[] offset = new double[] { 0, 0, 0 };
            HashSet<string> partNameSet = new HashSet<string>(partNames);
            //
            foreach (var entry in _actors)
            {
                if (partNameSet.Contains(entry.Key) ||
                    (entry.Value.ParentName != null && partNameSet.Contains(entry.Value.ParentName)))
                    SetExplodedViewActorPosition(entry.Value, offset);
            }
            //
            RenderScene();
        }
        private static void SetExplodedViewActorPosition(vtkMaxActor actor, double[] position)
        {
            actor.Geometry?.SetPosition(position[0], position[1], position[2]);
            actor.ModelEdges?.SetPosition(position[0], position[1], position[2]);
            actor.ElementEdges?.SetPosition(position[0], position[1], position[2]);
            actor.Nodes?.SetPosition(position[0], position[1], position[2]);
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
            if (_actors.TryGetValue(partName, out actor))
            {
                vtkDataSet dataset = actor.GeometryMapper.GetInputAsDataSet();
                vtkUnsignedLongArray globalIds = vtkUnsignedLongArray.SafeDownCast(dataset.GetPointData().GetGlobalIds());
                if (globalIds != null)
                {
                    long numPoints = dataset.GetNumberOfPoints();
                    for (long i = 0; i < numPoints; i++)
                    {
                        if (globalIds.GetValue(i) == nodeId)
                        {
                            return (int)i;
                        }
                    }
                }
            }
            return -1;
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
            vtkMaxActor actor;
            double[] centerPointCoor;
            //
            if (_actors.TryGetValue(_followerViewPartNames[0], out actor))
            {
                vtkDataSet dataset = actor.GeometryMapper.GetInputAsDataSet();
                centerPointCoor = dataset.GetPoint(_followerViewPointIds[0]);
            }
            else return;
            //
            if (_followerViewPointCoor[0] == null) _followerViewPointCoor[0] = centerPointCoor;
            else
            {
                Vec3D delta = new Vec3D(_followerViewPointCoor[0]) - new Vec3D(centerPointCoor);
                //
                vtkCamera camera = _renderer.GetActiveCamera();
                Vec3D position = new Vec3D(camera.GetPosition());
                Vec3D focalPoint = new Vec3D(camera.GetFocalPoint());
                // Move only in the view plane
                Vec3D normal = new Vec3D(camera.GetViewPlaneNormal());
                double t = Vec3D.DotProduct(normal, delta);
                Vec3D projection = t * normal;
                delta -= projection;
                //
                position -= delta;
                focalPoint -= delta;
                //
                camera.SetPosition(position.X, position.Y, position.Z);
                camera.SetFocalPoint(focalPoint.X, focalPoint.Y, focalPoint.Z);
                _renderer.SetActiveCamera(camera);
                _style.AdjustCameraDistanceAndClipping();
                //
                _followerViewPointCoor[0] = centerPointCoor;
            }
        }
        public void UpdateFollowerViewPlane()
        {
            Vec3D p1;
            Vec3D p2;
            Vec3D p3;
            vtkMaxActor actor;
            if (_actors.TryGetValue(_followerViewPartNames[0], out actor))
            {
                vtkDataSet dataset = actor.GeometryMapper.GetInputAsDataSet();
                p1 = new Vec3D(dataset.GetPoint(_followerViewPointIds[0]));
            }
            else return;
            if (_actors.TryGetValue(_followerViewPartNames[1], out actor))
            {
                vtkDataSet dataset = actor.GeometryMapper.GetInputAsDataSet();
                p2 = new Vec3D(dataset.GetPoint(_followerViewPointIds[1]));
            }
            else return;
            if (_actors.TryGetValue(_followerViewPartNames[2], out actor))
            {
                vtkDataSet dataset = actor.GeometryMapper.GetInputAsDataSet();
                p3 = new Vec3D(dataset.GetPoint(_followerViewPointIds[2]));
            }
            else return;
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
            vtkCamera camera = _renderer.GetActiveCamera();
            Vec3D pos = new Vec3D(camera.GetPosition());
            Vec3D foc = new Vec3D(camera.GetFocalPoint());
            Vec3D up = new Vec3D(camera.GetViewUp());
            //
            pos = MultiplyMatrixVector(RInv, pos) + tInv;
            foc = MultiplyMatrixVector(RInv, foc) + tInv;
            up = MultiplyMatrixVector((RInv), up);
            //
            camera.SetPosition(pos.X, pos.Y, pos.Z);
            camera.SetFocalPoint(foc.X, foc.Y, foc.Z);
            camera.SetViewUp(up.X, up.Y, up.Z);
            //
            _renderer.SetActiveCamera(camera);
            _style.AdjustCameraDistanceAndClipping();
            //
            _followerViewPointCoor[0] = p1.Coor;
            _followerViewPointCoor[1] = p2.Coor;
            _followerViewPointCoor[2] = p3.Coor;
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
            vtkTransform transform = vtkTransform.New();
            transform.Translate(symmetryPoint[0], symmetryPoint[1], symmetryPoint[2]);
            if (symmetryPlane == 0) transform.Scale(-1.0, 1.0, 1.0);
            else if (symmetryPlane == 1) transform.Scale(1.0, -1.0, 1.0);
            else if (symmetryPlane == 2) transform.Scale(1.0, 1.0, -1.0);
            else throw new NotSupportedException();
            transform.Translate(-symmetryPoint[0], -symmetryPoint[1], -symmetryPoint[2]);
            //
            _transforms.Add(transform, 2);  // must be 2 for 1 loop
        }
        public void AddLinearPattern(double[] displacement, int numOfItems)
        {
            vtkTransform transform = vtkTransform.New();
            transform.Translate(displacement[0], displacement[1], displacement[2]);
            _transforms.Add(transform, numOfItems);
        }
        public void AddCircularPattern(double[] axisPoint, double[] axisNormal, double angle, int numOfItems)
        {
            vtkTransform transform = vtkTransform.New();
            transform.Translate(axisPoint[0], axisPoint[1], axisPoint[2]);
            transform.RotateWXYZ(angle, axisNormal[0], axisNormal[1], axisNormal[2]);
            transform.Translate(-axisPoint[0], -axisPoint[1], -axisPoint[2]);
            //
            _transforms.Add(transform, numOfItems);
        }
        public void ApplyTransforms()
        {
            foreach (var transform in _transforms) TransformAllMaxActors(transform.Key, transform.Value);
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
        }
        private void RemoveActorWithCopies(vtkMaxActor actor)
        {
            // Transformed copies
            foreach (var copy in actor.Copies) RemoveActorWithCopies(copy);
            //
            actor.Copies.Clear();
            //
            _actors.Remove(actor.Name);
            //
            _renderer.RemoveActor(actor.Geometry);
            _renderer.RemoveActor(actor.ElementEdges);
            _renderer.RemoveActor(actor.ModelEdges);
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
            ApplyEdgesVisibilityAndBackfaceCulling();
            ApplyVisibilityToActorNodes();
            //_style.AdjustCameraDistanceAndClipping();
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
            ApplyEdgesVisibilityAndBackfaceCulling();
            ApplyVisibilityToActorNodes();
            //_style.AdjustCameraDistanceAndClipping();
        }
        //
        public void TransformAllMaxActors(vtkTransform transform, int numOfItems)
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
                    //
                    baseActor.Copies.Add(transformedActor);
                    //
                    TransformMaxActor(transformedActor, transform);
                    //
                    AddActor(transformedActor, vtkRendererLayer.Base);
                    //
                    baseActor = transformedActor;
                }
            }
        }
        private void TransformMaxActor(vtkMaxActor vtkMaxActor, vtkTransform transform)
        {
            TransformActor(vtkMaxActor.Geometry, transform);
            if (vtkMaxActor.ElementEdges != null) TransformActor(vtkMaxActor.ElementEdges, transform);
            if (vtkMaxActor.ModelEdges != null) TransformActor(vtkMaxActor.ModelEdges, transform);
            // Transform locator to enable section view
            if (vtkMaxActor.FrustumCellLocator != null)
            {
                vtkTransformFilter transformFilter = vtkTransformFilter.New();
                transformFilter.SetInput(vtkMaxActor.FrustumCellLocator.GetDataSet());
                transformFilter.SetTransform(transform);
                transformFilter.Update();
                //
                vtkMaxActor.FrustumCellLocator.SetDataSet(transformFilter.GetOutput());
            }
            // Reconstruct the selected directional result component after geometry transformation.
            vtkMaxActor.ApplyResultFieldTransformation(GetResultFieldTransformation(transform));
        }
        private static double[] GetResultFieldTransformation(vtkTransform transform)
        {
            vtkMatrix4x4 matrix = transform.GetMatrix();
            double[] transformation = new double[9];
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                    transformation[3 * row + column] = matrix.GetElement(row, column);
            }
            return transformation;
        }
        private void TransformActor(vtkActor actor, vtkTransform transform)
        {
            vtkPolyData polyData = (vtkPolyData)actor.GetMapper().GetInput();
            //
            vtkTransformPolyDataFilter transformFilter = vtkTransformPolyDataFilter.New();
            transformFilter.SetInput(polyData);
            transformFilter.SetTransform(transform);
            transformFilter.Update();
            //
            double[] scale = transform.GetScale();
            if (scale[0] < 0 || scale[1] < 0 || scale[2] < 0)
            {
                vtkReverseSense reverseSense = vtkReverseSense.New();
                reverseSense.SetInputConnection(transformFilter.GetOutputPort());
                reverseSense.ReverseNormalsOff();
                reverseSense.ReverseCellsOn();
                reverseSense.Update();
                //
                actor.GetMapper().SetInputConnection(reverseSense.GetOutputPort());
            }
            else
            {
                actor.GetMapper().SetInputConnection(transformFilter.GetOutputPort());
            }
            //
            actor.PickableOff();
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
            _scalarBarWidget.SetTopLeftPosition(20, 20);
            _statusBlockWidget.SetTopLeftPosition(Width - _statusBlockWidget.GetWidth() - 20, 20);
        }
        public void SetScalarBarColorSpectrum(vtkMaxColorSpectrum colorSpectrum)
        {
            _colorSpectrum.DeepCopy(colorSpectrum);
            //
            bool smoothColors = _colorSpectrum.Mode == vtkColorSpectrumMode.Smooth;
            _scalarBarWidget.SetSmoothColors(smoothColors);
            _scalarBarWidget.SetNumberOfColors(_colorSpectrum.NumberOfColors);  // colors or legend intervals
            _scalarBarWidget.MinColorVisible = colorSpectrum.MinColorVisible;
            _scalarBarWidget.MaxColorVisible = colorSpectrum.MaxColorVisible;
            _scalarBarWidget.MinColor = colorSpectrum.MinColor;
            _scalarBarWidget.MaxColor = colorSpectrum.MaxColor;
        }
        // Below-min / above-max limit color bands only exist when the manual limit clips the data
        public bool ScalarBarAddMinColor { get { return _scalarBarWidget.AddMinColor; } }
        public bool ScalarBarAddMaxColor { get { return _scalarBarWidget.AddMaxColor; } }
        public void SetScalarBarVisibility(bool visibility)
        {
            _keepScalarBarVisible = visibility;
            if (!visibility) _scalarBarWidget.VisibilityOff();
        }
        public bool TryGetScalarBarValueEditData(bool editMinimum, out Rectangle rectangle, out string valueText,
                                                 out double minimum, out double maximum)
        {
            return _scalarBarWidget.TryGetValueEditData(editMinimum, out rectangle, out valueText,
                                                        out minimum, out maximum);
        }
        public void SetScalarBarNumberFormat(string numberFormat)
        {
            _scalarBarWidget.SetLabelFormat(numberFormat);
        }
        public void SetScalarBarText(string fieldName, string componentName, string unitAbbreviation, string complexComponent,
                                     string minMaxType)
        {
            _scalarBarWidget.SetText(fieldName, componentName, unitAbbreviation, complexComponent, minMaxType);
            //
            UpdateScalarFormatting();
        }
        public void DrawScalarBarBackground(bool drawBackground)
        {
            _scalarBarWidget.SetBackgroundColor(1, 1, 1);
            if (drawBackground) _scalarBarWidget.BackgroundVisibilityOn();
            else _scalarBarWidget.BackgroundVisibilityOff();
        }
        public void DrawScalarBarBorder(bool drawBorder)
        {
            if (drawBorder) _scalarBarWidget.BorderVisibilityOn();
            else _scalarBarWidget.BorderVisibilityOff();
        }
        private string GetUnitAbbreviation()
        {
            if (_scalarBarWidget.GetVisibility() == 0) return "";
            //
            string unitAbbreviation = _scalarBarWidget.UnitAbbreviation;
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
            if (_renderer != null)
            {
                _renderer.SetGradientBackground(gradient);
                _renderer.SetBackground2(topColor.R / 255.0, topColor.G / 255.0, topColor.B / 255.0);
                _renderer.SetBackground(bottomColor.R / 255.0, bottomColor.G / 255.0, bottomColor.B / 255.0);
                if (redraw) RenderScene();
            }
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
            if (redraw) RenderScene();
        }
        public void SetSmoothing(bool pointSmoothing, bool lineSmoothing, bool redraw)
        {
            if (_renderWindow != null)
            {
                if (pointSmoothing != (_renderWindow.GetPointSmoothing() == 1) ||
                    lineSmoothing != (_renderWindow.GetLineSmoothing() == 1))
                {
                    List<vtkMaxBorderWidget> widgets = new List<vtkMaxBorderWidget>();
                    if (_coorSysWidget != null) widgets.Add(_coorSysWidget);
                    if (_scaleWidget != null) widgets.Add(_scaleWidget);
                    if (_scalarBarWidget != null) widgets.Add(_scalarBarWidget);
                    if (_colorBarWidget != null) widgets.Add(_colorBarWidget);
                    if (_statusBlockWidget != null) widgets.Add(_statusBlockWidget);
                    if (_probeWidget != null) widgets.Add(_probeWidget);
                    if (_arrowWidgets != null) widgets.AddRange(_arrowWidgets.Values);

                    // Widgets keep a reference to the interactor. Detach them before disposing it so
                    // they never call through an invalid native VTK pointer after the window is rebuilt.
                    foreach (vtkMaxBorderWidget widget in widgets) widget.RemoveInteractor();
                    _renderWindowInteractor.ModifiedEvt -= _renderWindowInteractor_ModifiedEvt;
                    _renderWindowInteractor.Dispose();
                    _renderWindow.Dispose();

                    _renderWindow = vtkRenderWindow.New();
                    _renderWindow.SetParentId(this.Handle);
                    _renderWindow.SetNumberOfLayers(4);

                    _renderWindow.AddRenderer(_renderer);
                    _renderWindow.AddRenderer(_overlayRenderer);
                    _renderWindow.AddRenderer(_selectionRenderer);
                    _renderWindow.AddRenderer(_widgetRenderer);

                    SyncRenderWindowSize();

                    _renderWindowInteractor = vtkRenderWindowInteractor.New();
                    _renderWindowInteractor.SetInteractorStyle(_style);
                    _style.ResetRotationCenter();

                    _renderWindow.SetInteractor(_renderWindowInteractor);
                    _renderWindowInteractor.ModifiedEvt += _renderWindowInteractor_ModifiedEvt;

                    foreach (vtkMaxBorderWidget widget in widgets)
                        widget.SetInteractor(_widgetRenderer, _renderWindowInteractor);

                    if (pointSmoothing) _renderWindow.PointSmoothingOn();
                    else _renderWindow.PointSmoothingOff();

                    if (lineSmoothing) _renderWindow.LineSmoothingOn();
                    else _renderWindow.LineSmoothingOff();
                }
            }
            
            if (redraw) RenderScene();
        }
        // Highlight
        public void SetHighlightColor(Color primaryHighlightColor, Color secondaryHighlightColor)
        {
            _primaryHighlightColor = primaryHighlightColor;
            _secondaryHighlightColor = secondaryHighlightColor;
            //
            Color highlightColor;
            foreach (var actor in _selectedActors)
            {
                if (actor.UseSecondaryHighlightColor) highlightColor = secondaryHighlightColor;
                else highlightColor = primaryHighlightColor;
                //
                ApplySelectionFormattingToActor(actor);
            }
            RenderScene();
        }
        public void SetMouseHighlightColor(Color mouseHighlightColor)
        {
            Globals.CurrentMouseHighlightColor = mouseHighlightColor;
            Globals.Initialize();
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
            vtkCellArray lines = vtkCellArray.New();
            lines.Allocate(12L * numArrows, 1000);
            vtkCellArray vertices = vtkCellArray.New();
            vertices.Allocate(2L * numNodalPoints, 1000);
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
                AddLineArrow(coordinates, values, lines, arrowId, scalarValue,
                             origin[0], origin[1], origin[2],
                             origin[0] + shaftLength * ux,
                             origin[1] + shaftLength * uy,
                             origin[2] + shaftLength * uz,
                             headLength, headHalfWidth);
                arrowId++;
            }
            //
            // Draw a scalar-colored point at every retained nodal position, including zero vectors.
            int nodalPointId = numArrowPoints;
            for (int i = 0; i < data.Geometry.Nodes.Values.Length; i++)
            {
                float scalarValue = data.Geometry.Nodes.Values[i];
                if (float.IsNaN(scalarValue) || float.IsInfinity(scalarValue)) continue;
                double[] origin = data.Geometry.Nodes.Coor[i];
                int coordinateId = 3 * nodalPointId;
                coordinates[coordinateId] = (float)origin[0];
                coordinates[coordinateId + 1] = (float)origin[1];
                coordinates[coordinateId + 2] = (float)origin[2];
                values[nodalPointId] = scalarValue;
                vertices.InsertNextCell(1);
                vertices.InsertCellPoint(nodalPointId++);
            }
            //
            vtkFloatArray vtkCoordinates = vtkFloatArray.New();
            vtkCoordinates.SetNumberOfComponents(3);
            IntPtr coordinatePointer = vtkCoordinates.WriteVoidPointer(0, coordinates.Length);
            Marshal.Copy(coordinates, 0, coordinatePointer, coordinates.Length);
            vtkCoordinates.DataChanged();
            vtkPoints points = vtkPoints.New();
            points.SetData(vtkCoordinates);
            //
            vtkFloatArray scalars = vtkFloatArray.New();
            scalars.SetName(Globals.ScalarArrayName);
            scalars.SetNumberOfComponents(1);
            IntPtr scalarPointer = scalars.WriteVoidPointer(0, values.Length);
            Marshal.Copy(values, 0, scalarPointer, values.Length);
            scalars.DataChanged();
            //
            vtkPolyData polyData = vtkPolyData.New();
            polyData.SetPoints(points);
            polyData.SetLines(lines);
            polyData.SetVerts(vertices);
            polyData.GetPointData().SetScalars(scalars);
            //
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInput(polyData);
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            // Keep lookup-table colors unchanged on render paths that use the diffuse
            // material color even when lighting is disabled.
            actor.GeometryProperty.SetColor(1, 1, 1);
            actor.GeometryProperty.SetLighting(false);
            if (data.Geometry.ExtremeNodes != null)
            {
                NodesExchangeData extremes = data.Geometry.ExtremeNodes;
                actor.MinNode = new vtkMaxExtreemeNode(extremes.Ids[0], extremes.Coor[0], extremes.Values[0]);
                actor.MaxNode = new vtkMaxExtreemeNode(extremes.Ids[1], extremes.Coor[1], extremes.Values[1]);
            }
            AddActorGeometry(actor, data.Layer);
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
            vtkCellArray lines = vtkCellArray.New();
            lines.Allocate(24L * numGlyphs, 1000);
            vtkCellArray vertices = vtkCellArray.New();
            vertices.Allocate(2L * numNodalPoints, 1000);
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
                    AddLineArrow(coordinates, values, lines, arrowId++, scalarValue,
                                 origin[0], origin[1], origin[2],
                                 origin[0] - halfShaftLength * ux,
                                 origin[1] - halfShaftLength * uy,
                                 origin[2] - halfShaftLength * uz,
                                 headLength, headHalfWidth);
                    AddLineArrow(coordinates, values, lines, arrowId++, scalarValue,
                                 origin[0], origin[1], origin[2],
                                 origin[0] + halfShaftLength * ux,
                                 origin[1] + halfShaftLength * uy,
                                 origin[2] + halfShaftLength * uz,
                                 headLength, headHalfWidth);
                }
                else
                {
                    double tailDistance = headLength + halfShaftLength;
                    AddLineArrow(coordinates, values, lines, arrowId++, scalarValue,
                                 origin[0] - tailDistance * ux,
                                 origin[1] - tailDistance * uy,
                                 origin[2] - tailDistance * uz,
                                 origin[0] - headLength * ux,
                                 origin[1] - headLength * uy,
                                 origin[2] - headLength * uz,
                                 headLength, headHalfWidth);
                    AddLineArrow(coordinates, values, lines, arrowId++, scalarValue,
                                 origin[0] + tailDistance * ux,
                                 origin[1] + tailDistance * uy,
                                 origin[2] + tailDistance * uz,
                                 origin[0] + headLength * ux,
                                 origin[1] + headLength * uy,
                                 origin[2] + headLength * uz,
                                 headLength, headHalfWidth);
                }
            }
            // Draw a scalar-colored point at every retained nodal position, including zero values.
            int nodalPointId = numArrowPoints;
            for (int i = 0; i < data.Geometry.Nodes.Values.Length; i++)
            {
                float scalarValue = data.Geometry.Nodes.Values[i];
                if (float.IsNaN(scalarValue) || float.IsInfinity(scalarValue)) continue;
                double[] origin = data.Geometry.Nodes.Coor[i];
                int coordinateId = 3 * nodalPointId;
                coordinates[coordinateId] = (float)origin[0];
                coordinates[coordinateId + 1] = (float)origin[1];
                coordinates[coordinateId + 2] = (float)origin[2];
                values[nodalPointId] = scalarValue;
                vertices.InsertNextCell(1);
                vertices.InsertCellPoint(nodalPointId++);
            }
            //
            vtkFloatArray vtkCoordinates = vtkFloatArray.New();
            vtkCoordinates.SetNumberOfComponents(3);
            IntPtr coordinatePointer = vtkCoordinates.WriteVoidPointer(0, coordinates.Length);
            Marshal.Copy(coordinates, 0, coordinatePointer, coordinates.Length);
            vtkCoordinates.DataChanged();
            vtkPoints points = vtkPoints.New();
            points.SetData(vtkCoordinates);
            //
            vtkFloatArray scalars = vtkFloatArray.New();
            scalars.SetName(Globals.ScalarArrayName);
            scalars.SetNumberOfComponents(1);
            IntPtr scalarPointer = scalars.WriteVoidPointer(0, values.Length);
            Marshal.Copy(values, 0, scalarPointer, values.Length);
            scalars.DataChanged();
            //
            vtkPolyData polyData = vtkPolyData.New();
            polyData.SetPoints(points);
            polyData.SetLines(lines);
            polyData.SetVerts(vertices);
            polyData.GetPointData().SetScalars(scalars);
            //
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInput(polyData);
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            actor.GeometryProperty.SetColor(1, 1, 1);
            actor.GeometryProperty.SetLighting(false);
            if (data.Geometry.ExtremeNodes != null)
            {
                NodesExchangeData extremes = data.Geometry.ExtremeNodes;
                actor.MinNode = new vtkMaxExtreemeNode(extremes.Ids[0], extremes.Coor[0], extremes.Values[0]);
                actor.MaxNode = new vtkMaxExtreemeNode(extremes.Ids[1], extremes.Coor[1], extremes.Values[1]);
            }
            AddActorGeometry(actor, data.Layer);
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
            vtkPolyData polyData = CreateAnimatedGlyphPolyData(glyphFrames[0],
                                                               numArrowsPerNode * numSourceNodes, numSourceNodes);
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInput(polyData);
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            actor.GeometryProperty.SetColor(1, 1, 1);
            actor.GeometryProperty.SetLighting(false);
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
        private static vtkPolyData CreateAnimatedGlyphPolyData(NodesExchangeData nodes, int numArrows, int numNodalPoints)
        {
            float[] coordinates = new float[3 * nodes.Coor.Length];
            int coordinateId = 0;
            foreach (double[] coor in nodes.Coor)
            {
                coordinates[coordinateId++] = (float)coor[0];
                coordinates[coordinateId++] = (float)coor[1];
                coordinates[coordinateId++] = (float)coor[2];
            }
            vtkFloatArray vtkCoordinates = vtkFloatArray.New();
            vtkCoordinates.SetNumberOfComponents(3);
            IntPtr coordinatePointer = vtkCoordinates.WriteVoidPointer(0, coordinates.Length);
            Marshal.Copy(coordinates, 0, coordinatePointer, coordinates.Length);
            vtkCoordinates.DataChanged();
            vtkPoints points = vtkPoints.New();
            points.SetData(vtkCoordinates);
            //
            vtkFloatArray scalars = vtkFloatArray.New();
            scalars.SetName(Globals.ScalarArrayName);
            scalars.SetNumberOfComponents(1);
            IntPtr scalarPointer = scalars.WriteVoidPointer(0, nodes.Values.Length);
            Marshal.Copy(nodes.Values, 0, scalarPointer, nodes.Values.Length);
            scalars.DataChanged();
            //
            vtkCellArray lines = vtkCellArray.New();
            lines.Allocate(12L * numArrows, 1000);
            for (int arrowId = 0; arrowId < numArrows; arrowId++)
            {
                int pointId = _numPointsPerLineArrow * arrowId;
                AddLine(lines, pointId, pointId + 1);
                AddLine(lines, pointId + 2, pointId + 3);
                AddLine(lines, pointId + 3, pointId + 4);
                AddLine(lines, pointId + 4, pointId + 2);
            }
            vtkCellArray vertices = vtkCellArray.New();
            vertices.Allocate(2L * numNodalPoints, 1000);
            int firstNodalPointId = _numPointsPerLineArrow * numArrows;
            for (int i = 0; i < numNodalPoints; i++)
            {
                vertices.InsertNextCell(1);
                vertices.InsertCellPoint(firstNodalPointId + i);
            }
            //
            vtkPolyData polyData = vtkPolyData.New();
            polyData.SetPoints(points);
            polyData.SetLines(lines);
            polyData.SetVerts(vertices);
            polyData.GetPointData().SetScalars(scalars);
            return polyData;
        }
        private void AddEmptyScalarFieldActor(vtkMaxActorData data)
        {
            // A field with no non-zero glyphs still has a valid scalar range. Register an
            // actor without renderable cells so the scalar bar and extreme-value widgets
            // remain available while zero-length arrows are omitted.
            vtkPolyData polyData = vtkPolyData.New();
            if (data.Geometry.ExtremeNodes != null)
            {
                NodesExchangeData extremes = data.Geometry.ExtremeNodes;
                float[] coordinates = new float[6];
                float[] values = new float[2];
                for (int i = 0; i < 2; i++)
                {
                    coordinates[3 * i] = (float)extremes.Coor[i][0];
                    coordinates[3 * i + 1] = (float)extremes.Coor[i][1];
                    coordinates[3 * i + 2] = (float)extremes.Coor[i][2];
                    values[i] = extremes.Values[i];
                }
                //
                vtkFloatArray vtkCoordinates = vtkFloatArray.New();
                vtkCoordinates.SetNumberOfComponents(3);
                IntPtr coordinatePointer = vtkCoordinates.WriteVoidPointer(0, coordinates.Length);
                Marshal.Copy(coordinates, 0, coordinatePointer, coordinates.Length);
                vtkCoordinates.DataChanged();
                vtkPoints points = vtkPoints.New();
                points.SetData(vtkCoordinates);
                polyData.SetPoints(points);
                //
                vtkFloatArray scalars = vtkFloatArray.New();
                scalars.SetName(Globals.ScalarArrayName);
                scalars.SetNumberOfComponents(1);
                IntPtr scalarPointer = scalars.WriteVoidPointer(0, values.Length);
                Marshal.Copy(values, 0, scalarPointer, values.Length);
                scalars.DataChanged();
                polyData.GetPointData().SetScalars(scalars);
            }
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInput(polyData);
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            if (data.Geometry.ExtremeNodes != null)
            {
                NodesExchangeData extremes = data.Geometry.ExtremeNodes;
                actor.MinNode = new vtkMaxExtreemeNode(extremes.Ids[0], extremes.Coor[0], extremes.Values[0]);
                actor.MaxNode = new vtkMaxExtreemeNode(extremes.Ids[1], extremes.Coor[1], extremes.Values[1]);
            }
            AddActorGeometry(actor, data.Layer);
        }
        private static void AddLineArrow(float[] coordinates, float[] values, vtkCellArray lines, int arrowId,
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
            //
            AddLine(lines, pointId, pointId + 1);
            AddLine(lines, pointId + 2, pointId + 3);
            AddLine(lines, pointId + 3, pointId + 4);
            AddLine(lines, pointId + 4, pointId + 2);
        }
        private static void AddLine(vtkCellArray lines, int pointId1, int pointId2)
        {
            lines.InsertNextCell(2);
            lines.InsertCellPoint(pointId1);
            lines.InsertCellPoint(pointId2);
        }
        //
        public void AddScalarFieldOnCells(vtkMaxActorData data, bool update)
        {
            // Create actor
            vtkMaxActor actor = new vtkMaxActor(data);
            // Add actor
            AddActorGeometry(actor, vtkRendererLayer.Base);
            // Add actorEdges
            if (data.CanHaveElementEdges && actor.ElementEdges != null) AddActorEdges(actor, false, vtkRendererLayer.Base);
            // Add modelEdges
            if (actor.ModelEdges != null) AddActorEdges(actor, true, data.Layer);
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
            double animationMemorySizeKb = actor.InitializeAnimation(data);
            actor.RecomputeMemorySize();
            _animationFrameData.MemMb += (actor.MemorySize + animationMemorySizeKb) / 1024.0;

            AddActorGeometry(actor, data.Layer);
            if (data.CanHaveElementEdges && actor.ElementEdges != null)
                AddActorEdges(actor, false, data.Layer);
            if (actor.ModelEdges != null) AddActorEdges(actor, true, data.Layer);
            _animationActors.Add(actor.Name, actor);
            _currentAnimationFrameNumber = 0;
            int[] animationNodeIds = registerNodeIds
                ? data.CellLocator?.Nodes?.Ids ?? data.Geometry.Nodes.Ids
                : null;
            if (animationNodeIds != null)
            {
                foreach (int nodeId in animationNodeIds) _animationNodeActors[nodeId] = actor;
            }

            AdjustCameraDistanceAndClippingRedraw();
            return true;
        }
        //
        private void UpdateScalarFormatting()
        {
            vtkMaxActor actor;
            vtkMapper mapper;
            vtkPointData pointData;
            // Legend
            _scalarBarWidget.VisibilityOff();
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
                mapper = actor.GeometryMapper;
                pointData = mapper.GetInput().GetPointData();
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
                if (actor.ColorContours)
                {
                    if (pointData.GetAttribute(0) == null || pointData.GetAttribute(0).GetName() != Globals.ScalarArrayName)
                        pointData.SetActiveScalars(Globals.ScalarArrayName);
                    if (mapper.GetInterpolateScalarsBeforeMapping() != 1)
                        mapper.SetInterpolateScalarsBeforeMapping(1); // discrete colors - smooth colors
                }
                else
                {
                    if (pointData.GetAttribute(0) != null && pointData.GetAttribute(0).GetName() != "none")
                        pointData.SetActiveScalars("none");
                    if (mapper.GetInterpolateScalarsBeforeMapping() != 0)
                        mapper.SetInterpolateScalarsBeforeMapping(0); // discrete colors must be turned off
                    actor.UpdateColor();
                }
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
                    _scalarBarWidget.VisibilityOn();
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
                    _scalarBarWidget.CreateLookupTable(GetColorTransferFunction(), animationRange[0], animationRange[1]);
                    _lookupTable = PrepareActorLookupTable(animationRange[0], animationRange[1]);
                }
                else // min max from current frame
                {
                    _scalarBarWidget.CreateLookupTable(GetColorTransferFunction(), minNode.Value, maxNode.Value);
                    _lookupTable = PrepareActorLookupTable(minNode.Value, maxNode.Value);
                }
            }
            else  // Manual and min max from current frame
            {
                _scalarBarWidget.CreateLookupTable(GetColorTransferFunction(), minNode.Value, maxNode.Value, 
                                                   _colorSpectrum.MinUserValue, _colorSpectrum.MaxUserValue);
                _lookupTable = PrepareActorLookupTable(minNode.Value, maxNode.Value);
            }
            // Edit actors mapper
            double[] actorRange = _lookupTable.GetTableRange();
            vtkLookupTable lookup = vtkLookupTable.New();
            lookup.DeepCopy(_lookupTable);
            // Set NAN color as white
            lookup.SetNanColor(1, 1, 1, 1);
            //
            foreach (var entry in _actors)
            {
                actor = entry.Value;
                mapper = actor.GeometryMapper;
                pointData = mapper.GetInput().GetPointData();
                if (actor.ColorContours && actor.VtkMaxActorVisible && pointData.GetScalars() != null)
                {
                    mapper.SetScalarRange(actorRange[0], actorRange[1]);
                    mapper.SetLookupTable(lookup);
                }
            }
            // Scalar bar
            _scalarBarWidget.VisibilityOn();
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
                                           float[] frustumCellLocatorValues, ResultFieldExchangeData resultField,
                                           ResultFieldExchangeData frustumResultField, bool update)
        {
            if (System.Diagnostics.Debugger.IsAttached && false)
            {
                Test(actorName, values, extremeNodes, frustumCellLocatorValues);
                return;
            }
            // Add scalars
            if (values != null)
            {
                vtkFloatArray scalars = vtkFloatArray.New();
                scalars.SetName(Globals.ScalarArrayName);
                scalars.SetNumberOfValues(values.Length);
                for (int i = 0; i < values.Length; i++) scalars.SetValue(i, values[i]);
                //
                vtkFloatArray frustumScalars = vtkFloatArray.New();
                frustumScalars.SetName(Globals.ScalarArrayName);
                frustumScalars.SetNumberOfValues(frustumCellLocatorValues.Length);
                for (int i = 0; i < frustumCellLocatorValues.Length; i++) frustumScalars.SetValue(i, frustumCellLocatorValues[i]);
                //
                UpdateActorScalarField(actorName, scalars, frustumScalars, extremeNodes,
                                       resultField, frustumResultField);
            }
            else
            {
                RemoveActorScalarField(actorName);
            }
            //
            if (update) UpdateScalarsAndRedraw();
        }
        private void UpdateActorScalarField(string actorName, vtkFloatArray scalars, vtkFloatArray frustumScalars,
                                            NodesExchangeData extremeNodes, ResultFieldExchangeData resultField,
                                            ResultFieldExchangeData frustumResultField)
        {
            vtkMaxActor actor = _actors[actorName];
            // Transformed copies
            foreach (var copy in actor.Copies)
                UpdateActorScalarField(copy.Name, scalars, frustumScalars, extremeNodes,
                                       resultField, frustumResultField);
            // Set scalars
            actor.GeometryMapper.GetInput().GetPointData().SetScalars(scalars);
            actor.MinNode = new vtkMaxExtreemeNode(extremeNodes.Ids[0], extremeNodes.Coor[0], extremeNodes.Values[0]);
            actor.MaxNode = new vtkMaxExtreemeNode(extremeNodes.Ids[1], extremeNodes.Coor[1], extremeNodes.Values[1]);
            // Set frustum scalars
            actor.FrustumCellLocator.GetDataSet().GetPointData().SetScalars(frustumScalars);
            // Refresh the complete directional field and reconstruct this actor's selected component.
            actor.UpdateResultFields(resultField, frustumResultField);
        }

        private void RemoveActorScalarField(string actorName)
        {
            if (_actors.TryGetValue(actorName, out vtkMaxActor actor))
            {
                // Transformed copies
                foreach (var copy in actor.Copies) RemoveActorScalarField(copy.Name);
                // Remove scalars
                //actor.GeometryMapper.GetInput().GetPointData().GetScalars().RemoveLastTuple();
                actor.GeometryMapper.GetInput().GetPointData().RemoveArray(Globals.ScalarArrayName);
                actor.MinNode = null;
                actor.MaxNode = null;
                actor.UpdateResultFields(null, null);
                // Remove frustum scalars
                actor.FrustumCellLocator.GetDataSet().GetPointData().RemoveArray(Globals.ScalarArrayName);
                //
                vtkMapper mapper = actor.GeometryMapper;
                if (mapper.GetInterpolateScalarsBeforeMapping() != 0) mapper.SetInterpolateScalarsBeforeMapping(0);
            }
        }
        private void Test(string actorName, float[] values, CaeGlobals.NodesExchangeData extremeNodes,
                                            float[] frustumCellLocatorValues)
        {
            // Add scalars
            if (values != null)
            {
                vtkFloatArray scalars = vtkFloatArray.New();
                scalars.SetName(Globals.ScalarArrayName);
                scalars.SetNumberOfValues(values.Length);
                for (int i = 0; i < values.Length; i++)
                {
                    scalars.SetValue(i, values[i]);
                }

                vtkFloatArray frustumScalars = vtkFloatArray.New();
                frustumScalars.SetName(Globals.ScalarArrayName);
                frustumScalars.SetNumberOfValues(frustumCellLocatorValues.Length);
                for (int i = 0; i < frustumCellLocatorValues.Length; i++)
                {
                    frustumScalars.SetValue(i, frustumCellLocatorValues[i]);
                }
                
                if (_actors.ContainsKey(actorName)) // added for testing
                {
                    // Set scalars
                    _actors[actorName].GeometryPointData.SetScalars(scalars);
                    //
                    _actors[actorName].MinNode = new vtkMaxExtreemeNode(extremeNodes.Ids[0], extremeNodes.Coor[0], extremeNodes.Values[0]);
                    _actors[actorName].MaxNode = new vtkMaxExtreemeNode(extremeNodes.Ids[1], extremeNodes.Coor[1], extremeNodes.Values[1]);
                    //Set frustum scalars
                    _actors[actorName].FrustumPointData.SetScalars(frustumScalars);
                    //up to here 185000++
                }
                
                //UpdateScalarFormatting();
                if (true)
                {
                    vtkMaxActor actor;
                    //vtkMapper mapper;
                    vtkPointData pointData;

                    vtkMaxExtreemeNode minNode = null;
                    vtkMaxExtreemeNode maxNode = null;

                    // Find min and max value on actors
                    foreach (var entry in _actors)
                    {
                        actor = entry.Value;
                        if (entry.Value == null) throw new ArgumentNullException("_actors.Actor", "The actor does not exist.");
                        //mapper = actor.GeometryMapper;
                        pointData = actor.GeometryPointData;

                        // if the part does not have scalar data
                        if (actor.MinNode == null || actor.MaxNode == null) continue;

                        if (actor.ColorContours)
                        {
                            if (pointData.GetAttribute(0) == null || pointData.GetAttribute(0).GetName() != Globals.ScalarArrayName)
                                pointData.SetActiveScalars(Globals.ScalarArrayName);
                            //if (mapper.GetInterpolateScalarsBeforeMapping() != 1)
                            //    mapper.SetInterpolateScalarsBeforeMapping(1); // discrete colors
                        }
                        else
                        {
                            if (pointData.GetAttribute(0) != null && pointData.GetAttribute(0).GetName() != "none")
                                pointData.SetActiveScalars("none");
                            //if (mapper.GetInterpolateScalarsBeforeMapping() != 0)
                            //    mapper.SetInterpolateScalarsBeforeMapping(0); // discrete colors must be turned off
                            actor.GeometryProperty.SetColor(actor.Color.R / 255d, actor.Color.G / 255d, actor.Color.B / 255d);
                        }

                        if (!actor.VtkMaxActorVisible || !actor.ColorContours) continue;

                        if (minNode == null || maxNode == null)     // the first time through
                        {
                            minNode = actor.MinNode;
                            maxNode = actor.MaxNode;
                        }
                        else
                        {
                            if (actor.MinNode != null && actor.MinNode.Value < minNode.Value) minNode = actor.MinNode;
                            if (actor.MaxNode != null && actor.MaxNode.Value > maxNode.Value) maxNode = actor.MaxNode;
                        }
                    }

                    // Working to here





                    //if (minNode == null || maxNode == null) return;

                    //// Scalar bar and min/max values of actor scalar range
                    //if (_colorSpectrum.MinMaxType == vtkColorSpectrumMinMaxType.Automatic)
                    //{
                    //    if (_animationFrameData != null && _animationFrameData.UseAllFrameData)   // animation from all frames
                    //    {
                    //        double[] animationRange = _animationFrameData.AllFramesScalarRange;
                    //        _scalarBarWidget.CreateLookupTable(GetColorTransferFunction(), animationRange[0], animationRange[1]);
                    //        PrepareActorLookupTable(animationRange[0], animationRange[1]);
                    //    }
                    //    else // min max from current frame
                    //    {
                    //        _scalarBarWidget.CreateLookupTable(GetColorTransferFunction(), minNode.Value, maxNode.Value);
                    //        PrepareActorLookupTable(minNode.Value, maxNode.Value);
                    //    }
                    //}
                    //else  // Manual and min max from current frame
                    //{
                    //    _scalarBarWidget.CreateLookupTable(GetColorTransferFunction(), minNode.Value, maxNode.Value, _colorSpectrum.MinUserValue, _colorSpectrum.MaxUserValue);
                    //    PrepareActorLookupTable(minNode.Value, maxNode.Value);
                    //}
                }

                RenderScene();

                countError++;
                if (countError % 1000 == 0) Debug.WriteLine("Count: " + countError);
            }
        }
        public void UpdateActorsColorContoursVisibility(string[] actorNames, bool colorContours)
        {
            foreach (var name in actorNames) UpdateActorColorContoursVisibility(name, colorContours);
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
            // Section view
            if (actor.SectionViewActor != null) actor.SectionViewActor.ColorContours = colorContours;
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
                double[] incremental = GetResultFieldTransformation(transformEntry.Key);
                double[][] existing = transformations.ToArray();
                foreach (double[] existingTransformation in existing)
                {
                    double[] current = existingTransformation;
                    for (int i = 1; i < transformEntry.Value; i++)
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
            _currentAnimationFrameNumber = frameNumber;
            //
            if (applyTransforms) ApplyTransforms();
            //
            if (applySectionView) CreateSectionView(sectionViewData.Plane.GetOrigin(), sectionViewData.Plane.GetNormal(),
                                                   sectionViewData.LightenColors, sectionViewData.SectionColor);
            //
            UpdateScalarFormatting();
            //
            bool initializeArrowAnchors = _animationArrowAnchorRequests.Count > 0;
            Controller_AnimationFrameChanged?.Invoke(frameNumber, initializeArrowAnchors);
            UpdateAnimatedArrowWidgetAnchors();
            //
            UpdateFollowerView();
            _style.AdjustCameraDistanceAndClipping(false);
            _style.RefreshWidgetHighlights();
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
            vtkWindowToImageFilter windowToImage = vtkWindowToImageFilter.New();
            windowToImage.SetInput(_renderWindow);
            windowToImage.SetInputBufferTypeToRGB();
            //
            vtkAVIWriter avw = vtkAVIWriter.New();
            avw.SetInputConnection(windowToImage.GetOutputPort());
            avw.SetFileName(fileName);
            avw.SetQuality(2);
            avw.SetRate(fps);
            //
            if (encoderOptions) avw.PromptCompressionOptionsOn();
            //avw.SetCompressorFourCC("MSVC");  //DIVX, XVID, and H264
            //
            vtkObject.GlobalWarningDisplayOff();    // if the video compression window is closed an error occurs
            avw.Start();
            vtkObject.GlobalWarningDisplayOn();
            //
            for (int i = firstLastFrame[0]; i <= firstLastFrame[1]; i += step)
            {
                SetAnimationFrame(i, scalarRangeFromAllFrames);
                windowToImage.Modified();
                avw.Write();
            }
            if (swing)
            {
                for (int i = firstLastFrame[1]; i >= firstLastFrame[0]; i -= step)
                {
                    SetAnimationFrame(i, scalarRangeFromAllFrames);
                    windowToImage.Modified();
                    avw.Write();
                }
            }
            avw.End();
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

            vtkWindowToImageFilter windowToImage = vtkWindowToImageFilter.New();
            windowToImage.SetInput(_renderWindow);
            windowToImage.SetInputBufferTypeToRGB();

            vtkPNGWriter pngWriter = vtkPNGWriter.New();
            pngWriter.SetInputConnection(windowToImage.GetOutputPort());

            count = 0;
            for (int i = firstLastFrame[0]; i <= firstLastFrame[1]; i += step)
            {
                SetAnimationFrame(i, scalarRangeFromAllFrames);
                windowToImage.Modified();
                pngWriter.SetFileName(fileNames[count++]);
                pngWriter.Write();
            }
            if (swing)
            {
                for (int i = firstLastFrame[1]; i >= firstLastFrame[0]; i -= step)
                {
                    SetAnimationFrame(i, scalarRangeFromAllFrames);
                    windowToImage.Modified();
                    pngWriter.SetFileName(fileNames[count++]);
                    pngWriter.Write();
                }
            }
        }
        public void RenderToPNG(string fileName)
        {
            vtkWindowToImageFilter windowToImage = vtkWindowToImageFilter.New();
            windowToImage.SetInput(_renderWindow);
            windowToImage.SetInputBufferTypeToRGB();
            //
            vtkPNGWriter pngWriter = vtkPNGWriter.New();
            pngWriter.SetInputConnection(windowToImage.GetOutputPort());
            //
            pngWriter.SetFileName(fileName);
            pngWriter.Write();
        }

        #endregion #################################################################################################################

        #region Clear ##############################################################################################################
        public void Clear()
        {
            if (_sectionViewData.Active) RemoveSectionView();
            _transforms.Clear();
            //
            foreach (var entry in _actors)
            {
                if (entry.Value is vtkMaxCaptionActor ca) _renderer.RemoveActor2D(ca.CaptionActor);
                else
                {
                    _renderer.RemoveActor(entry.Value.Geometry);
                    _renderer.RemoveActor(entry.Value.ElementEdges);
                    _renderer.RemoveActor(entry.Value.ModelEdges);
                    _renderer.RemoveActor(entry.Value.Nodes);
                }
            }
            //
            _renderer.RemoveActor(_captionTmpActor);
            _captionActorAddedToBase = false;
            //_renderer.RemoveAllViewProps();  this removes all other actors for min/max values ...
            _actors.Clear();            
            _cellPicker.RemoveAllLocators();
            _propPicker.InitializePickList();
            _animationActors.Clear();
            _animationNodeActors.Clear();
            _currentAnimationFrameNumber = -1;
            _animationArrowAnchors.Clear();
            _animationArrowAnchorRequests.Clear();
            _animationFrameData = new vtkMaxAnimationFrameData();
            _keepScalarBarVisible = false;
            //
            if (_scalarBarWidget != null) _scalarBarWidget.VisibilityOff();
            if (_colorBarWidget != null) HideColorBar();
            if (_statusBlockWidget != null) _statusBlockWidget.VisibilityOff();
            if (_style != null) _style.ResetRotationCenter();
            // Widgets
            HideAllArrowWidgets();
            //
            ClearSelection();
            ClearOverlay();
            RemoveSectionView();
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
                    // Remove from renderer
                    _renderer.RemoveActor(actor.Geometry);
                    _renderer.RemoveActor(actor.ElementEdges);
                    _renderer.RemoveActor(actor.ModelEdges);
                    _renderer.RemoveActor(entry.Value.Nodes);
                    // Remove cell locator and picker
                    if (actor.Geometry.GetPickable() == 1)
                    {
                        if (actor.CellLocator != null) _cellPicker.RemoveLocator(actor.CellLocator);
                        _propPicker.DeletePickList(actor.Geometry);
                    }
                }
            }
            // Remove actors
            foreach (string name in actorsToRemove) _actors.Remove(name);
            //
            ClearSelection();
            ClearOverlay();
        }
        public void ClearSelection(bool clearAllMouseSelection = true)
        {
            if (clearAllMouseSelection)
            {
                if (_mouseSelectionAllIds.Count > 0) _mouseSelectionAllIds.Clear();
                _mouseSelectionCurrentIds = null;
            }
            //
            ClearCurrentMouseSelection(false);
            // Actors
            foreach (var entry in _actors)
            {
                entry.Value.UpdateColor();
            }
            // Actors and edges - leave mouse
            foreach (vtkMaxActor actor in _selectedActors)
            {
                if (actor is vtkMaxCaptionActor ca) _selectionRenderer.RemoveActor2D(ca.CaptionActor);
                else
                {
                    _renderer.RemoveActor(actor.Geometry);  // some selections are drawn on the base layer
                    _renderer.RemoveActor(actor.ElementEdges);
                    //
                    _selectionRenderer.RemoveActor(actor.Geometry);
                    _selectionRenderer.RemoveActor(actor.ElementEdges);
                }
            }
            //
            _selectionRenderer.RemoveActor(_captionTmpActor);
            _captionActorAddedToSelection = false;
            //
            _selectedActors.Clear();
            //
            RenderScene();
        }
        public void ClearCurrentMouseSelection()
        {
            ClearCurrentMouseSelection(true);
        }
        public void ClearCurrentMouseSelection(bool render)
        {
            if (_mouseSelectionActorCurrent != null)
            {
                _selectionRenderer.RemoveActor(_mouseSelectionActorCurrent.Geometry);
                _selectionRenderer.RemoveActor(_mouseSelectionActorCurrent.ElementEdges);
                _selectedActors.Remove(_mouseSelectionActorCurrent);
                _mouseSelectionActorCurrent = null;
            }
            //
            _mouseSelectionCurrentIds = null;
            //
            if (_probeWidget != null && _probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
            if (render) RenderScene();
        }
        public void ClearOverlay()
        {
            _maxSymbolSize = 0;
            foreach (var entry in _overlayActors)
            {
                if (entry.Value is vtkMaxCaptionActor ca) _overlayRenderer.RemoveActor2D(ca.CaptionActor);
                else
                {
                    ReleaseActorGraphicsResources(entry.Value);
                    _overlayRenderer.RemoveActor(entry.Value.Geometry);
                }
            }
            //
            _overlayRenderer.RemoveActor(_captionTmpActor);
            _captionActorAddedToOverlay = false;
            //
            _overlayActors.Clear();
            _measurementArrowInputs.Clear();
            _measurementArrowSymbolSizes.Clear();
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

            if (arrowCoordinates != null && arrowNormals != null)
            {
                vtkMaxActor arrowActor = AddMeasurementArrowActor(name, arrowCoordinates, arrowNormals,
                                                                  symbolSize, lineData.Color);
                arrowActor.VtkMaxActorVisible = visible;
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
                   _measurementArrowInputs.TryGetValue(name, out vtkPolyData arrowInput) &&
                   arrowCoordinates.Length == arrowInput.GetNumberOfPoints() &&
                   _measurementArrowSymbolSizes.TryGetValue(name, out double currentSymbolSize) &&
                   currentSymbolSize == symbolSize;
        }
        private bool MeasurementActorHasPointCount(string actorName, double[][] coordinates)
        {
            if (coordinates == null || !_overlayActors.TryGetValue(actorName, out vtkMaxActor actor)) return false;
            vtkPointSet pointSet = actor.GeometryMapper.GetInput() as vtkPointSet;
            return pointSet != null && pointSet.GetNumberOfPoints() == coordinates.Length;
        }
        public void UpdateMeasurementAnnotation(string name, double[][] nodeCoordinates, double[][] lineCoordinates,
                                                double[][] arrowCoordinates, double[][] arrowNormals, bool visible)
        {
            UpdateMeasurementActor(GetMeasurementActorName(name, "nodes"), nodeCoordinates, visible);
            UpdateMeasurementActor(GetMeasurementActorName(name, "lines"), lineCoordinates, visible);

            string arrowActorName = GetMeasurementActorName(name, "arrows");
            if (_measurementArrowInputs.TryGetValue(name, out vtkPolyData arrowInput) &&
                arrowCoordinates != null && arrowNormals != null &&
                arrowCoordinates.Length == arrowInput.GetNumberOfPoints())
            {
                vtkPoints points = arrowInput.GetPoints();
                vtkDataArray normals = arrowInput.GetPointData().GetNormals();
                for (int i = 0; i < arrowCoordinates.Length; i++)
                {
                    points.SetPoint(i, arrowCoordinates[i][0], arrowCoordinates[i][1], arrowCoordinates[i][2]);
                    normals.SetTuple3(i, arrowNormals[i][0], arrowNormals[i][1], arrowNormals[i][2]);
                }
                points.Modified();
                arrowInput.Modified();
                if (_overlayActors.TryGetValue(arrowActorName, out vtkMaxActor arrowActor))
                    arrowActor.VtkMaxActorVisible = visible;
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
                    ReleaseActorGraphicsResources(actor);
                    _overlayRenderer.RemoveActor(actor.Geometry);
                    _overlayActors.Remove(actorName);
                    removed = true;
                }
            }
            _measurementArrowInputs.Remove(name);
            _measurementArrowSymbolSizes.Remove(name);
            return removed;
        }
        private void UpdateMeasurementActor(string actorName, double[][] coordinates, bool visible)
        {
            if (coordinates == null || !_overlayActors.TryGetValue(actorName, out vtkMaxActor actor)) return;
            vtkPointSet pointSet = actor.GeometryMapper.GetInput() as vtkPointSet;
            if (pointSet == null || pointSet.GetNumberOfPoints() != coordinates.Length) return;

            vtkPoints points = pointSet.GetPoints();
            for (int i = 0; i < coordinates.Length; i++)
                points.SetPoint(i, coordinates[i][0], coordinates[i][1], coordinates[i][2]);
            points.Modified();
            pointSet.Modified();
            actor.GeometryMapper.Modified();
            actor.VtkMaxActorVisible = visible;
        }
        private vtkMaxActor AddMeasurementArrowActor(string name, double[][] coordinates, double[][] normals,
                                                     double symbolSize, Color color)
        {
            if (symbolSize > _maxSymbolSize) _maxSymbolSize = symbolSize;

            vtkPoints points = vtkPoints.New();
            vtkDoubleArray normalArray = vtkDoubleArray.New();
            normalArray.SetNumberOfComponents(3);
            for (int i = 0; i < coordinates.Length; i++)
            {
                points.InsertNextPoint(coordinates[i][0], coordinates[i][1], coordinates[i][2]);
                normalArray.InsertNextTuple3(normals[i][0], normals[i][1], normals[i][2]);
            }

            vtkPolyData input = vtkPolyData.New();
            input.SetPoints(points);
            input.GetPointData().SetNormals(normalArray);

            vtkDistanceToCamera distanceToCamera = vtkDistanceToCamera.New();
            distanceToCamera.SetInput(input);
            distanceToCamera.SetScreenSize(symbolSize);
            distanceToCamera.SetRenderer(_renderer);

            vtkConeSource cone = vtkConeSource.New();
            cone.SetCenter(-0.5, 0, 0);
            cone.SetRadius(0.35);
            cone.CappingOn();
            cone.SetResolution(11);

            vtkTriangleFilter triangleFilter = vtkTriangleFilter.New();
            triangleFilter.SetInput(cone.GetOutput());
            triangleFilter.Update();

            vtkGlyph3D glyph = vtkGlyph3D.New();
            glyph.SetSourceConnection(triangleFilter.GetOutputPort());
            glyph.SetInputConnection(distanceToCamera.GetOutputPort());
            glyph.SetVectorModeToUseNormal();
            glyph.ScalingOn();
            glyph.SetScaleModeToScaleByScalar();
            glyph.SetInputArrayToProcess(0, 0, 0, "vtkDataObject::FIELD_ASSOCIATION_POINTS", "DistanceToCamera");
            glyph.SetScaleFactor(0.3);
            glyph.OrientOn();
            glyph.Update();

            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(glyph.GetOutputPort());
            mapper.ScalarVisibilityOff();

            vtkMaxActorData data = new vtkMaxActorData();
            data.Name = GetMeasurementActorName(name, "arrows");
            data.Color = color;
            data.Layer = vtkRendererLayer.Overlay;
            data.Pickable = false;
            vtkMaxActor actor = new vtkMaxActor(data, mapper);
            actor.GeometryProperty.SetRepresentationToSurface();
            ApplySymbolFormattingToActor(actor);
            AddActorGeometry(actor, vtkRendererLayer.Overlay);

            _measurementArrowInputs[name] = input;
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
                    if (actor is vtkMaxCaptionActor captionActor)
                        _overlayRenderer.RemoveActor2D(captionActor.CaptionActor);
                    else
                    {
                        ReleaseActorGraphicsResources(actor);
                        _overlayRenderer.RemoveActor(actor.Geometry);
                    }
                    //
                    _overlayActors.Remove(actorName);
                    removed = true;
                }
            }
            if (removed) RenderScene();
        }
        private void ReleaseActorGraphicsResources(vtkMaxActor actor)
        {
            if (actor == null || _renderWindow == null) return;
            //
            if (actor.Geometry != null) actor.Geometry.ReleaseGraphicsResources(_renderWindow);
            if (actor.ElementEdges != null) actor.ElementEdges.ReleaseGraphicsResources(_renderWindow);
            if (actor.ModelEdges != null) actor.ModelEdges.ReleaseGraphicsResources(_renderWindow);
            if (actor.Nodes != null) actor.Nodes.ReleaseGraphicsResources(_renderWindow);
        }
        #endregion #################################################################################################################

        #region Widgets ############################################################################################################
        public void SetWidgetInteractionEnabled(bool enabled)
        {
            _widgetInteractionEnabled = enabled;
            //
            if (_style != null) _style.SetWidgetLocalHighlightEnabled(enabled);
            RenderScene();
        }
        public void SetWidgetHighlightLocked(bool locked)
        {
            _widgetHighlightLocked = locked;
            if (!locked && _style != null) _style.ClearWidgetHighlights();
        }
        public void SetWidgetHighlightEnabled(bool enabled)
        {
            _widgetHighlightEnabled = enabled;
            if (_style != null) _style.SetWidgetHighlightEnabled(enabled);
        }
        public void ResetScaleBarPosition()
        {
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
                arrowWidget.SetInteractor(_widgetRenderer, _renderWindowInteractor);
                arrowWidget.SetBorderColor(0, 0, 0);
                arrowWidget.SetTextProperty(CreateNewTextProperty());
                arrowWidget.SetPadding(5);
                arrowWidget.GetBackgroundProperty().SetColor(1, 1, 1);
                // Set number format before the widget contents are updated
                arrowWidget.SetNumberFormat(numberFormat);
                // Event
                arrowWidget.MouseDoubleClick += arrowWidget_DoubleClicked;
                // Add
                _arrowWidgets.Add(name, arrowWidget);
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
            int[] viewportSize = _selectionRenderer.GetSize();
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
            foreach (var entry in _arrowWidgets) entry.Value.RemoveInteractor();
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
                    widget.RemoveInteractor();
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

        public void SwitchLights()
        {
            _renderer.RemoveAllLights();
            //
            _renderer.AddLight(_light1);
            _light1.SetSwitch((_light1.GetSwitch() + 1) % 2);
            //
            _renderer.AddLight(_light2);
            _light2.SetSwitch((_light2.GetSwitch() + 1) % 2);
            //
            vtkLight light3 = vtkLight.New();
            light3.SetPosition(0, -1, 0);
            light3.SetFocalPoint(0, 0, 0);
            light3.SetColor(1, 1, 1);
            light3.SetIntensity(0.5);
            light3.SetSpecularColor(0, 0, 0);
            light3.SetLightTypeToCameraLight();
            _renderer.AddLight(light3);
            //
            RenderScene();
        }

        public void InterpolateMeshData(PartExchangeData source, ref PartExchangeData input)
        {
            vtkMaxActorData vtkSourceData = new vtkMaxActorData();
            vtkSourceData.Geometry = source;
            vtkMaxActor vtkSourceActor = new vtkMaxActor(vtkSourceData);
            //
            vtkMaxActorData vtkInputData = new vtkMaxActorData();
            vtkInputData.Geometry = input;
            vtkMaxActor vtkInputActor = new vtkMaxActor(vtkInputData);
            //
            vtkProbeFilter probeFilter = new vtkProbeFilter();
            probeFilter.SetSource(vtkSourceActor.Geometry.GetMapper().GetInput());
            probeFilter.SetInput(vtkInputActor.Geometry.GetMapper().GetInput());
            probeFilter.Update();
            //
            vtkDataArray scalars = probeFilter.GetOutput().GetPointData().GetScalars(Globals.ScalarArrayName);
            input.Nodes.Values = new float[scalars.GetNumberOfTuples()];
            for (int i = 0; i < scalars.GetNumberOfTuples(); i++)
            {
                input.Nodes.Values[i] = (float)scalars.GetTuple1(i);
            }
        }

        public bool ContainsActor(string actorName)
        {
            return _actors.ContainsKey(actorName);
        }
        private string GetActorName(vtkActor actor)
        {
            if (actor == null) return null;
            //
            foreach (var entry in _actors)
            {
                if (entry.Value.Geometry == actor || entry.Value.ElementEdges == actor ||entry.Value.ModelEdges == actor)
                    return entry.Key;
            }
            //
            return null;
        }
        private vtkMaxActor GetSelectedMaxActor(vtkActor actor)
        {
            if (actor == null) return null;
            //
            foreach (var selectionActor in _selectedActors)
            {
                if (selectionActor.Geometry == actor || selectionActor.ElementEdges == actor || selectionActor.ModelEdges == actor)
                    return selectionActor;
            }
            //
            return null;
        }
        private vtkMaxActor GetMaxActor(vtkActor actor)
        {
            if (actor == null) return null;
            //
            foreach (var entry in _actors)
            {
                if (entry.Value.Geometry == actor || entry.Value.ElementEdges == actor || entry.Value.ModelEdges == actor)
                    return entry.Value;
            }
            //
            return null;
        }
        public void UpdateActor(string oldName, string newName, Color newColor)
        {            
            if (!_actors.ContainsKey(oldName)) return;
            //
            vtkMaxActor actor = _actors[oldName];
            actor.Name = newName;
            //
            UpdateActorColor(actor, newColor);
            //
            _actors.Remove(oldName);
            _actors.Add(newName, actor);
            //
            ApplyEdgesVisibilityAndBackfaceCulling();
            //
            RenderScene();
        }
        public void UpdateActorColor(vtkMaxActor actor, Color newColor)
        {
            actor.Color = newColor;
            // Transformed copies
            foreach (var copy in actor.Copies) UpdateActorColor(copy, newColor);
            // Section view
            if (_sectionViewData.Active && actor.SectionViewActor != null)
            {
                actor.SectionViewActor.Color = newColor;                // set to default
                UpdateSectionViewActorColor(actor.SectionViewActor);    // update
            }
        }
        private void UpdateSectionViewActorColor(vtkMaxActor sectionViewActor)
        {
            if (sectionViewActor != null)
            {
                if (_sectionViewData.LightenColors)
                    sectionViewActor.Color = sectionViewActor.Color.Lighten();
                else if (_sectionViewData.SectionColor != null &&
                         _sectionViewData.SectionColor != Color.Empty)
                {
                    sectionViewActor.Color = _sectionViewData.SectionColor;
                }
            }
        }
        public double[] GetBoundingBox()
        {
            // xMin, xMax, yMin, yMax, zMin, zMax
            return ComputeVisiblePropBounds();
        }
        public double[] GetBoundingBoxSize()
        {
            // xMin, xMax, yMin, yMax, zMin, zMax
            double[] b = ComputeVisiblePropBounds();
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
                this.Invalidate();
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
            vtkMaxActor actor = _actors[partName];
            actor.CropWithCylinder(r, fileName);
        }
        public void CropPartWithCube(string partName, double x, double y, double z, double a,string fileName)
        {
            vtkMaxActor actor = _actors[partName];
            actor.CropWithCube(x, y, z, a, fileName);
        }
        public void SmoothPart(string partName, double a, string fileName)
        {
            vtkMaxActor actor = _actors[partName];
            actor.Smooth(a, fileName);
        }
        public vtkMaxActorData[] GetVtkMaxActorData()
        {
            vtkMaxActorData maxActorData;
            List<vtkMaxActorData> maxActorDatas = new List<vtkMaxActorData>();
            //
            foreach (var entry in _actors)
            {
                maxActorData = GetVtkMaxActorData(entry.Value);
                if (maxActorData != null) maxActorDatas.Add(maxActorData);
            }
            foreach (var entry in _overlayActors)
            {
                maxActorData = GetVtkMaxActorData(entry.Value);
                if (maxActorData != null) maxActorDatas.Add(maxActorData);
            }
            //
            return maxActorDatas.ToArray();
        }
        private vtkMaxActorData GetVtkMaxActorData(vtkMaxActor actor)
        {

            long n;
            long numPts;
            long numCells;
            vtkPolyData polyData;
            vtkPoints pts;
            vtkCellArray cellArray;
            vtkIdList idList;
            //
            vtkActor geometryActor = actor.Geometry;
            vtkActor elementEdgesActor = actor.ElementEdges;
            vtkActor modelEdgesActor = actor.ModelEdges;
            //
            bool actorsSwaped = false;
            if (actor.ActorRepresentation == vtkMaxActorRepresentation.Wire)    // constraints, undeformed model
            {
                modelEdgesActor = geometryActor;
                geometryActor = null;
                elementEdgesActor = null;
                actorsSwaped = true;
            }
            //
            vtkMaxActorData maxActorData = new vtkMaxActorData();
            maxActorData.Name = actor.Name;
            if (actorsSwaped) maxActorData.ColorTable = new Color[] { actor.Color, actor.Color, actor.Color };
            else maxActorData.ColorTable = new Color[] { actor.Color, Color.Black, Color.Black };
            maxActorData.ColorContours = actor.ColorContours;
            maxActorData.Geometry = new PartExchangeData();
            maxActorData.ElementEdges = new PartExchangeData();
            maxActorData.ModelEdges = new PartExchangeData();
            // Geometry                                                             
            if (geometryActor != null && geometryActor.GetVisibility() == 1 && geometryActor.GetMapper() != null)
            {
                polyData = (vtkPolyData)geometryActor.GetMapper().GetInput();
                pts = polyData.GetPoints();
                cellArray = polyData.GetPolys();
                numPts = pts.GetNumberOfPoints();
                numCells = cellArray.GetNumberOfCells();
                //
                PartExchangeData geometryData = maxActorData.Geometry;
                geometryData.Nodes.Coor = new double[numPts][];
                geometryData.Cells.CellNodeIds = new int[numCells][];
                // Coordinates
                for (int i = 0; i < numPts; i++) geometryData.Nodes.Coor[i] = pts.GetPoint(i);
                // Cells
                int cellCount = 0;
                int[] cell;
                cellArray.InitTraversal();
                idList = vtkIdList.New();
                while (cellArray.GetNextCell(idList) != 0)
                {
                    n = idList.GetNumberOfIds();
                    cell = new int[n];
                    for (int i = 0; i < n; i++) cell[i] = (int)idList.GetId(i);
                    geometryData.Cells.CellNodeIds[cellCount++] = cell;
                }
                // Values
                if (maxActorData.ColorContours)
                {
                    vtkDataArray scalars = polyData.GetPointData().GetScalars(Globals.ScalarArrayName);
                    geometryData.Nodes.Values = new float[scalars.GetNumberOfTuples()];
                    for (int i = 0; i < scalars.GetNumberOfTuples(); i++)
                    {
                        geometryData.Nodes.Values[i] = (float)scalars.GetTuple1(i);
                    }
                }
            }
            else maxActorData.Geometry = null;
            // Element edges
            if (elementEdgesActor != null && elementEdgesActor.GetVisibility() == 1)
            {
                maxActorData.ElementEdges = new PartExchangeData();
                // Add geometry
                polyData = (vtkPolyData)elementEdgesActor.GetMapper().GetInput();
                pts = polyData.GetPoints();
                numPts = pts.GetNumberOfPoints();
                numCells = polyData.GetNumberOfCells();
                //
                PartExchangeData elementEdgesData = maxActorData.ElementEdges;
                elementEdgesData.Nodes.Coor = new double[numPts][];
                elementEdgesData.Cells.CellNodeIds = new int[numCells][];
                // Coordinates
                for (int i = 0; i < numPts; i++) elementEdgesData.Nodes.Coor[i] = pts.GetPoint(i);
                // Cells
                List<int[]> edgeCellIds = new List<int[]>();
                for (int i = 0; i < numCells; i++)
                {
                    idList = polyData.GetCell(i).GetPointIds();
                    n = idList.GetNumberOfIds();
                    if (n == 2)
                    {
                        edgeCellIds.Add(new int[] { (int)idList.GetId(0), (int)idList.GetId(1) });
                    }
                    else if (n == 3)
                    {
                        edgeCellIds.Add(new int[] { (int)idList.GetId(0), (int)idList.GetId(1) });
                        edgeCellIds.Add(new int[] { (int)idList.GetId(1), (int)idList.GetId(2) });
                    }
                }
                elementEdgesData.Cells.CellNodeIds = edgeCellIds.ToArray();
            }
            else maxActorData.ElementEdges = null;
            // Model edges                                                          
            if (modelEdgesActor != null && modelEdgesActor.GetVisibility() == 1)
            {
                maxActorData.ModelEdges = new PartExchangeData();
                // Add geometry
                polyData = (vtkPolyData)modelEdgesActor.GetMapper().GetInput();
                pts = polyData.GetPoints();
                numPts = pts.GetNumberOfPoints();
                numCells = polyData.GetNumberOfCells();
                //
                PartExchangeData modelEdgesData = maxActorData.ModelEdges;
                modelEdgesData.Nodes.Coor = new double[numPts][];
                modelEdgesData.Cells.CellNodeIds = new int[numCells][];
                // Coordinates
                for (int i = 0; i < numPts; i++) modelEdgesData.Nodes.Coor[i] = pts.GetPoint(i);
                // Cells
                List<int[]> edgeCellIds = new List<int[]>();
                for (int i = 0; i < numCells; i++)
                {
                    idList = polyData.GetCell(i).GetPointIds();
                    n = idList.GetNumberOfIds();
                    if (n == 2)
                    {
                        edgeCellIds.Add(new int[] { (int)idList.GetId(0), (int)idList.GetId(1) });
                    }
                    else if (n == 3)
                    {
                        edgeCellIds.Add(new int[] { (int)idList.GetId(0), (int)idList.GetId(1) });
                        edgeCellIds.Add(new int[] { (int)idList.GetId(1), (int)idList.GetId(2) });
                    }
                }
                modelEdgesData.Cells.CellNodeIds = edgeCellIds.ToArray();
            }
            else maxActorData.ModelEdges = null;
            //
            return maxActorData;
        }
        public vtkMaxLookupTable GetLookupTable()
        {
            vtkMaxLookupTable maxLookupTable = new vtkMaxLookupTable();
            if (_lookupTable != null)
            {
                double min = _lookupTable.GetTableRange()[0];
                double max = _lookupTable.GetTableRange()[1];
                maxLookupTable = new vtkMaxLookupTable(PrepareActorLookupTable(min, max),
                                                       _colorSpectrum.Mode == vtkColorSpectrumMode.Smooth);
            }
            return maxLookupTable;
        }
        // Test
        public void Export(string fileName)
        {
            foreach (var entry in _actors)
            {
                if (entry.Key == "Solid_part-1")
                {
                    vtkPolyDataWriter writer = new vtkPolyDataWriter();
                    writer.SetInput(entry.Value.GeometryMapper.GetInput());
                    fileName = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + ".vtk");
                    writer.SetFileName(fileName);
                    writer.Write();
                }

            }

            return;

            _renderWindow.RemoveRenderer(_selectionRenderer);
            _renderWindow.RemoveRenderer(_overlayRenderer);
            _renderWindow.RemoveRenderer(_widgetRenderer);
            vtkOBJExporter exporter = vtkOBJExporter.New();
            //vtkX3DExporter exporter = vtkX3DExporter.New();
            //vtkVRMLExporter exporter = vtkVRMLExporter.New();
            

            exporter.SetRenderWindow(_renderWindow);
            exporter.DebugOn();

            //exporter.SetFileName(Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + ".vrml"));
            exporter.SetFilePrefix(Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + ".out"));
            //string outName = exporter.GetFilePrefix();
            
            exporter.Update();
            exporter.Write();

            _renderWindow.AddRenderer(_selectionRenderer);
            _renderWindow.AddRenderer(_overlayRenderer);
            _renderWindow.AddRenderer(_widgetRenderer);
        }
        public void AddTetrahedronFaces(double[][] nodes, int[][] tetrahedrons, Color color, int faceId, vtkRendererLayer layer)
        {
            // Create the points
            vtkPoints points = vtkPoints.New();
            points.SetNumberOfPoints(nodes.GetLength(0));

            for (int i = 0; i < nodes.GetLength(0); i++)
            {
                points.SetPoint(i, nodes[i][0], nodes[i][1], nodes[i][2]);
            }

            // Node Ids from face ids
            int[] nodeIds;
            switch (faceId)
            {
                case 1:
                    nodeIds = new int[] { 0, 2, 1 };
                    break;
                case 2:
                    nodeIds = new int[] { 0, 1, 3 };
                    break;
                case 3:
                    nodeIds = new int[] { 1, 2, 3 };
                    break;
                case 4:
                    nodeIds = new int[] { 2, 0, 3 };
                    break;
                default:
                    throw new NotImplementedException();
            }

            // Elements to cells
            vtkCellArray cells = vtkCellArray.New();
            for (int i = 0; i < tetrahedrons.GetLength(0); i++)
            {
                cells.InsertNextCell(3);
                cells.InsertCellPoint(tetrahedrons[i][nodeIds[0]]);
                cells.InsertCellPoint(tetrahedrons[i][nodeIds[1]]);
                cells.InsertCellPoint(tetrahedrons[i][nodeIds[2]]);
            }

            // Unstructured grid
            vtkUnstructuredGrid uGrid = vtkUnstructuredGrid.New();
            uGrid.SetPoints(points);
            uGrid.SetCells((int)vtkCellType.VTK_TRIANGLE, cells);
            uGrid.Update();

            // Edges
            vtkExtractEdges extractEdges = vtkExtractEdges.New();
            extractEdges.SetInput(uGrid);
            extractEdges.Update();

            // Visualize mesh
            vtkDataSetMapper mapperGrid = vtkDataSetMapper.New();
            mapperGrid.SetInput(uGrid);
            vtkMaxActor actorGrid = new vtkMaxActor();
            actorGrid.GeometryMapper = mapperGrid;
            actorGrid.GeometryProperty.SetColor(color.R / 255d, color.G / 255d, color.B / 255d);
            actorGrid.GeometryProperty.SetAmbient(0.5);
            actorGrid.GeometryProperty.BackfaceCullingOn();

            AddActorGeometry(actorGrid, layer);

            RenderScene();
        }
        
        public void AddTest()
        {
            double[,] p = new double[,] {
            { 0.0, 0.0, 0.0 },
            { 1.0, 0.0, 0.0 },
            { 1.0, 1.0, 0.0 },
            { 0.0, 1.0, 0.0 },
            { 0.0, 0.0, 1.0 },
            { 1.0, 0.0, 1.0 },
            { 1.0, 1.0, 1.0 },
            { 0.0, 1.0, 1.0 },
            { 0.0, 1.0, 2.0 }
            };

            // Create the points
            vtkPoints points = vtkPoints.New();
            points.SetNumberOfPoints(p.GetLength(0));

            for (int i = 0; i < p.GetLength(0); i++)
            {
                points.SetPoint(i, p[i, 0], p[i, 1], p[i, 2]);
            }

            // Elements to cells
            //vtkCellArray cells = vtkCellArray.New();
            //for (int i = 0; i < tetrahedrons.GetLength(0); i++)
            //{
            //    cells.InsertNextCell(4);
            //    cells.InsertCellPoint(tetrahedrons[i][0]);
            //    cells.InsertCellPoint(tetrahedrons[i][1]);
            //    cells.InsertCellPoint(tetrahedrons[i][2]);
            //    cells.InsertCellPoint(tetrahedrons[i][3]);
            //}

            
            

            // Create a hexahedron from the points

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            // Unstructured grid
            vtkUnstructuredGrid uGrid = vtkUnstructuredGrid.New();
            uGrid.SetPoints(points);

            vtkIdList list = vtkIdList.New();
            for (int i = 0; i < 1; i++)
            {
                list.SetNumberOfIds(8);
                for (int j = 0; j < 8; j++)
                {
                    list.SetId(j, j);
                }
                uGrid.InsertNextCell((int)vtkCellType.VTK_HEXAHEDRON, list);

                list.SetNumberOfIds(4);
                list.SetId(0, 4);
                list.SetId(1, 6);
                list.SetId(2, 7);
                list.SetId(3, 8);
                uGrid.InsertNextCell((int)vtkCellType.VTK_TETRA, list);
            }
            watch.Stop();
            //uGrid.InsertNextCell(hex.GetCellType(), hex.GetPointIds());

            uGrid.Update();

            // Visualize mesh
            vtkDataSetMapper mapperGrid = vtkDataSetMapper.New();
            mapperGrid.SetInput(uGrid);
            vtkMaxActor actorGrid = new vtkMaxActor();
            actorGrid.GeometryMapper = mapperGrid;
            actorGrid.GeometryProperty.SetColor(1, 0.5, 0.5);

            // Visualize edges
            // Extract edges
            vtkExtractEdges extractEdges = vtkExtractEdges.New();
            extractEdges.SetInput(uGrid);
            extractEdges.Update();

            vtkPolyDataMapper mapperEdges = vtkPolyDataMapper.New();
            mapperEdges.SetInputConnection(extractEdges.GetOutputPort());
            vtkMaxActor actorEdges = new vtkMaxActor();
            actorEdges.GeometryMapper = mapperEdges;
            actorEdges.GeometryProperty.SetColor(0, 0, 0);
            actorEdges.GeometryProperty.SetLineWidth(1);
            actorEdges.Geometry.PickableOff();

            AddActorGeometry(actorGrid, vtkRendererLayer.Base);

            RenderScene();
        }
        private void Hexahedron()
        {
            // Setup the coordinates of eight points 
            // (faces must be in counter clockwise order as viewed from the outside)
            double[,] p = new double[,] {
            { 0.0, 0.0, 2.0 },
            { 1.0, 0.0, 2.0 },
            { 1.0, 1.0, 2.0 },
            { 0.0, 1.0, 2.0 },
            { 0.0, 0.0, 3.0 },
            { 1.0, 0.0, 3.0 },
            { 1.0, 1.0, 3.0 },
            { 0.0, 1.0, 3.0 }
            };

            // Create the points
            vtkPoints points = vtkPoints.New();
            for (int i = 0; i < 8; i++)
                points.InsertNextPoint(p[i, 0], p[i, 1], p[i, 2]);

            // Create a hexahedron from the points
            vtkHexahedron hex = vtkHexahedron.New();
            for (int i = 0; i < 8; i++)
                hex.GetPointIds().SetId(i, i);

            // Add the hexahedron to a cell array
            vtkCellArray hexes = vtkCellArray.New();
            hexes.InsertNextCell(hex);

            // Add the points and hexahedron to an unstructured grid
            vtkUnstructuredGrid uGrid = vtkUnstructuredGrid.New();
            uGrid.SetPoints(points);
            uGrid.InsertNextCell(hex.GetCellType(), hex.GetPointIds());

            // Edges
            vtkExtractEdges extractEdges = vtkExtractEdges.New();
            extractEdges.SetInput(uGrid);
            extractEdges.Update();

            // Visualize
            vtkDataSetMapper mapper = vtkDataSetMapper.New();
            mapper.SetInput(uGrid);
            vtkActor actor = vtkActor.New();
            actor.SetMapper(mapper);
            actor.GetProperty().BackfaceCullingOn();

            // Visualize edges
            vtkPolyDataMapper mapperEdges = vtkPolyDataMapper.New();
            mapperEdges.SetInputConnection(extractEdges.GetOutputPort());
            vtkPolyDataMapper.SetResolveCoincidentTopologyToPolygonOffset();
            vtkActor actorEdges = vtkActor.New();
            actorEdges.SetMapper(mapperEdges);
            actorEdges.GetProperty().SetColor(0, 0, 0);
            actorEdges.GetProperty().SetLineWidth(1);

            _renderer.AddActor(actor);
            _renderer.AddActor(actorEdges);

            // fit all in


            //double[] bounds = ComputeVisiblePropBounds();
            IntPtr test = System.Runtime.InteropServices.Marshal.AllocHGlobal(6 * 8);
            _renderer.ComputeVisiblePropBounds(test);
            _renderer.ResetCamera(test);

            RenderScene();
        }
        public void AddSphere()
        {
            // source object
            vtkSphereSource SphereSource = vtkSphereSource.New();
            SphereSource.SetRadius(100);
            //SphereSource.SetEndTheta(180);
            //SphereSource.SetEndPhi(180);

            SphereSource.SetPhiResolution(50);
            SphereSource.SetThetaResolution(50);

            SphereSource.Update();

            // mapper
            vtkPolyDataMapper sphereMapper = vtkPolyDataMapper.New();
            sphereMapper.SetInputConnection(SphereSource.GetOutputPort());

            // actor
            vtkActor sphereActor = vtkActor.New();
            sphereActor.GetProperty().SetColor(1, 0, 0);
            sphereActor.SetMapper(sphereMapper);
            //sphereActor.GetProperty().SetInterpolationToFlat();
            //sphereActor.GetProperty().SetInterpolationToGouraud();
            sphereActor.GetProperty().SetInterpolationToPhong();
            sphereActor.GetProperty().SetAmbient(0.5);
            sphereActor.GetProperty().SetDiffuse(0.5);
            sphereActor.GetProperty().SetSpecular(0.6);
            sphereActor.GetProperty().SetSpecularColor(1, 1, 1);
            sphereActor.GetProperty().SetSpecularPower(100);

            // add actor to the renderer
            _renderer.AddActor(sphereActor);
            //_selectedActors.Add(sphereActor);

            // ensure all actors are visible (in this example not necessarily needed,
            // but in case more than one actor needs to be shown it might be a good idea)
            _renderer.ResetCamera();

            RenderScene();
        }
        private void Actor2D()
        {
            // Create the geometry of a point (the coordinate)
            vtkPoints points = vtkPoints.New();
            //points.InsertNextPoint(rotationCenter[0], rotationCenter[1], rotationCenter[2]);
            points.InsertNextPoint(100, 100, 0);
            points.InsertNextPoint(200, 200, 0);
            points.InsertNextPoint(200, 10, 200);

            vtkPolyData pointsPolyData = vtkPolyData.New();
            pointsPolyData.SetPoints(points);

            vtkVertexGlyphFilter glyphFilter = vtkVertexGlyphFilter.New();
            glyphFilter.SetInputConnection(pointsPolyData.GetProducerPort());
            glyphFilter.Update();

            // Visualize
            vtkPolyDataMapper2D mapper = vtkPolyDataMapper2D.New();
            mapper.SetInputConnection(glyphFilter.GetOutputPort());
            mapper.Update();

            vtkActor2D actor = vtkActor2D.New();
            actor.SetMapper(mapper);
            actor.GetProperty().SetColor(1, 0, 0);
            actor.GetProperty().SetPointSize(5);

            _renderer.AddViewProp(actor);
            _renderer.ResetCamera();
        }
        private void AddGlyphDemo()
        {
            vtkPoints points = vtkPoints.New();
            points.InsertNextPoint(77.5, 65, 0);
            points.InsertNextPoint(78.5, 65, 0);
            points.InsertNextPoint(79.5, 65, 0);

            vtkPolyData polyData = vtkPolyData.New();
            polyData.SetPoints(points);

            // Create anything you want here, we will use a polygon for the demo.
            vtkRegularPolygonSource polygonSource = vtkRegularPolygonSource.New(); //default is 6 sides

            vtkGlyph2D glyph2D = vtkGlyph2D.New();
            glyph2D.SetSourceConnection(polygonSource.GetOutputPort());
            glyph2D.SetInput(polyData);
            glyph2D.Update();

            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(glyph2D.GetOutputPort());
            mapper.Update();

            // Create a subclass of vtkActor: a vtkFollower that remains facing the camera
            vtkFollower follower = vtkFollower.New();
            follower.SetMapper(mapper);
            follower.GetProperty().SetColor(1, 0, 0); // red 
            
            vtkActor actor = vtkActor.New();
            actor.SetMapper(mapper);

            _renderer.AddActor(actor);
            _renderer.AddActor(follower);

            follower.SetCamera(_renderer.GetActiveCamera());

            RenderScene();
        }
        private void AddCaptionWidget()
        {
            // Create the widget and its representation
            vtkCaptionRepresentation captionRepresentation = vtkCaptionRepresentation.New();
            captionRepresentation.GetCaptionActor2D().SetCaption("Test caption");
            captionRepresentation.GetCaptionActor2D().GetTextActor().GetTextProperty().SetFontSize(16);

            double[] pos = new double[] { 66, 66, 20 };
            IntPtr posPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(4 * 8);
            Marshal.Copy(pos, 0, posPtr, 2);
            captionRepresentation.SetAnchorPosition(posPtr);
            Marshal.FreeHGlobal(posPtr);

            //vtkPointHandleRepresentation3D anchorRepresentation = captionRepresentation.GetAnchorRepresentation();
            vtkPropCollection coll = null;
            captionRepresentation.GetVolumes(coll);

            vtkCaptionWidget captionWidget = vtkCaptionWidget.New();
            captionWidget.SetInteractor(_renderWindowInteractor);
            captionWidget.SetRepresentation(captionRepresentation);
            captionWidget.SetCurrentRenderer(_selectionRenderer);
            captionWidget.SetDefaultRenderer(_selectionRenderer);

            // Add the actors to the scene

            captionWidget.On();
        }
        private void AddDiskAnimation()
        {
            //vtkCamera camera = vtkCamera.New();
            //camera.Elevation(-45);
            //_renderer.SetActiveCamera(camera);

            //# Creating disk mesh
            vtkDiskSource disk = vtkDiskSource.New();
            disk.SetInnerRadius(0.1);
            disk.SetOuterRadius(2.0);
            disk.SetRadialResolution(800);
            disk.SetCircumferentialResolution(800);
            disk.Update();
            vtkPolyData polyData = disk.GetOutput();

            Console.WriteLine(polyData.GetNumberOfPoints() + " nodes");
            Console.WriteLine(polyData.GetNumberOfCells() + " elements");

            //# Setup actor and mapper
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInput(polyData);
            vtkActor actor = vtkActor.New();
            actor.SetMapper(mapper);

            _renderer.AddActor(actor);

            int n = 36;
            double[] nodePos;
            polyData = disk.GetOutput();
            vtkPoints[] vtkPointsList = new vtkPoints[n];

            Console.WriteLine("Calculate...");
            for (int i = 0; i < n; i++)
            {
                vtkPointsList[i] = vtkPoints.New();
                vtkPointsList[i].DeepCopy(polyData.GetPoints());
                for (int j = 0; j < polyData.GetNumberOfPoints(); j++)
                {
                    nodePos = vtkPointsList[i].GetPoint(j);
                    vtkPointsList[i].SetPoint(j, nodePos[0], nodePos[1], 0.3 * Math.Sin(nodePos[0] * 2 + 360 / (n - 1) * i * Math.PI / 180));
                }
                polyData.SetPoints(vtkPointsList[i]);
                _renderWindow.Render();
                Application.DoEvents();
            }

            Console.WriteLine("Done. Animate.");
            System.Threading.Thread.Sleep(2000);

            //# First animation without color
            DateTime start_time = DateTime.Now;

            //for (int j = 0; j < 10; j++)
            //{
            //    for (int i = 0; i < n; i++)
            //    {
            //        polyData.SetPoints(vtkPointsList[i]);
            //        _renderWindow.Render();
            //        //Application.DoEvents();
            //        //RenderScene();
            //    }
            //}

            double fps = 10 * n / (DateTime.Now - start_time).TotalSeconds;
            Console.WriteLine(fps + " FPS");
            MessageBoxes.ShowInfo(fps + " FPS");

            Console.WriteLine("Done. Animate actor array.");

            _renderer.RemoveActor(actor);
            vtkActor[] actors = new vtkActor[n];
            for (int i = 0; i < n; i++)
            {
                disk = vtkDiskSource.New();
                disk.SetInnerRadius(0.1);
                disk.SetOuterRadius(2.0);
                disk.SetRadialResolution(800);
                disk.SetCircumferentialResolution(800);
                disk.Update();
                polyData = disk.GetOutput();
                polyData.SetPoints(vtkPointsList[i]);
                mapper = vtkPolyDataMapper.New();
                mapper.SetInput(polyData);
                actors[i] = vtkActor.New();
                actors[i].SetMapper(mapper);
                _renderer.AddActor(actors[i]);
                _renderWindow.Render();
                Application.DoEvents();
                actors[i].VisibilityOff();
            }

            start_time = DateTime.Now;
            for (int j = 0; j < 10; j++)
            {
                for (int i = 0; i < n; i++)
                {
                    //for (int k = 0; k < n; k++)
                    //{
                    //   actors[k].VisibilityOff();
                    //}
                    actors[(i + n - 1) % n].VisibilityOff();
                    actors[i].VisibilityOn();
                    _renderWindow.Render();
                }
            }

            fps = 10 * n / (DateTime.Now - start_time).TotalSeconds;
            Console.WriteLine(fps + " FPS");
            MessageBoxes.ShowInfo(fps + " FPS");
        }
        


       
        

       
    }
}
