// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using Kitware.VTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;

namespace vtkControl
{
    public delegate double[] GetPickPointDelegate(out vtkActor pickedActor, int x, int y);
    class vtkInteractorStyleControl : vtkInteractorStyleTrackballCamera
    {
        // Enum
        private enum EventIds
        {
            NoEvent = 0,
            AnyEvent,
            DeleteEvent,
            StartEvent,
            EndEvent,
            RenderEvent,
            ProgressEvent,
            PickEvent,
            StartPickEvent,
            EndPickEvent,
            AbortCheckEvent,
            ExitEvent,
            LeftButtonPressEvent,
            LeftButtonReleaseEvent,
            MiddleButtonPressEvent,
            MiddleButtonReleaseEvent,
            RightButtonPressEvent,
            RightButtonReleaseEvent,
            EnterEvent,
            LeaveEvent,
            KeyPressEvent,
            KeyReleaseEvent,
            CharEvent,
            ExposeEvent,
            ConfigureEvent,
            TimerEvent,
            MouseMoveEvent,
            MouseWheelForwardEvent,
            MouseWheelBackwardEvent,
            ResetCameraEvent,
            ResetCameraClippingRangeEvent,
            ModifiedEvent,
            WindowLevelEvent,
            StartWindowLevelEvent,
            EndWindowLevelEvent,
            ResetWindowLevelEvent,
            SetOutputEvent,
            ErrorEvent,
            WarningEvent,
            StartInteractionEvent, //mainly used
            InteractionEvent,
            EndInteractionEvent,
            EnableEvent,
            DisableEvent,
            CreateTimerEvent,
            DestroyTimerEvent,
            PlacePointEvent,
            PlaceWidgetEvent,
            CursorChangedEvent,
            ExecuteInformationEvent,
            RenderWindowMessageEvent,
            WrongTagEvent,
            StartAnimationCueEvent, // used
            AnimationCueTickEvent,
            EndAnimationCueEvent,
            VolumeMapperRenderProgressEvent,
            VolumeMapperComputeGradientsEndEvent,
            VolumeMapperComputeGradientsProgressEvent,
            VolumeMapperComputeGradientsStartEvent,
            WidgetModifiedEvent,
            WidgetValueChangedEvent,
            WidgetActivateEvent,
            UserEvent = 1000
        }
        // Variables                                                                                                                
        public const int VTKIS_START = 0;
        public const int VTKIS_NONE = 0;
        public const int VTKIS_ROTATE = 1;
        public const int VTKIS_PAN = 2;
        public const int VTKIS_SPIN = 3;
        public const int VTKIS_DOLLY = 4;
        public const int VTKIS_ZOOM = 5;
        public const int VTKIS_USCALE = 6;
        public const int VTKIS_TIMER = 7;
        public const int VTKIS_FORWARDFLY = 8;
        public const int VTKIS_REVERSEFLY = 9;
        public const int VTKIS_SELECTION = 10;
        public const int VTKIS_ANIM_OFF = 0;
        public const int VTKIS_ANIM_ON = 1;
        //
        private double _motionFactor;
        private double _rotateSensitivity;
        private double _zoomSensitivity;
        private bool _invertZoom;
        // Smooth zoom
        private const int SmoothZoomIntervalMilliseconds = 15;
        private const double SmoothZoomApproachRatio = 0.4;
        private const double SmoothZoomMinLogFactor = 0.005;
        private const double SmoothZoomMaxFrameMilliseconds = 30;
        private bool _smoothZoom;
        private double _smoothZoomRemainingFactor;
        private int _smoothZoomX;
        private int _smoothZoomY;
        private bool _smoothZoomStepping;
        private double _smoothZoomFrameMilliseconds;
        private System.Windows.Threading.DispatcherTimer _smoothZoomTimer;
        //
        private double[] _rotationCenterWorld;
        private double[] _rotationCenterDisplay;
        private int[] _clickPos;
        private double[] _clickPosWorld;
        private vtkProp _centerAnnotationActor;
        private bool _selectionBoxEnabled;
        private bool _boxZoomEnabled;
        private bool _animating;
        private System.Windows.Threading.DispatcherTimer _selectionTimer;
        private int _x;
        private int _y;
        // Double click
        protected int _lastX;
        protected int _lastY;
        protected int _numOfClicks;
        protected DateTime _lastClickTime;
        private TimeSpan _doubleClickTimeSpan = new TimeSpan(0, 0, 0, 0, SystemInformation.DoubleClickTime);
        private int _doubleClickDisp = 5;
        //
        protected bool _leftMouseButtonPressed;
        protected bool _boxSelection;
        protected bool _selectionCanceled;
        protected vtkRenderer _selectionRenderer;
        protected vtkRenderer _overlayRenderer;
        protected vtkPolyDataMapper2D _selectionBackgroundMapper;
        protected vtkPolyData _selectionBackgroundPolyData;
        protected vtkPoints _selectionBackgroundPoints;
        protected vtkActor2D _selectionBackgroundActor;
        protected vtkActor2D _selectionBorderActor;
        //
        protected List<vtkMaxBorderWidget> _widgets;
        protected List<vtkMaxBorderWidget> _reversedWidgets;
        protected vtkMaxBorderWidget _capturedWidget;
        protected bool _capturedWidgetPromoted;
        protected bool _widgetHighlightEnabled;
        protected bool _widgetLocalHighlightEnabled;


        // Properties                                                                                                               
        public new const string MRFullTypeName = "Kitware.VTK.vtkInteractorStyleControl";
        public bool SelectionBoxEnabled
        {
            get { return _selectionBoxEnabled; }
            set { _selectionBoxEnabled = value; }
        }
        public bool BoxZoomEnabled
        {
            get { return _boxZoomEnabled; }
            set
            {
                _boxZoomEnabled = value;
                if (!_boxZoomEnabled) BoxZoomEnded?.Invoke();
            }
        }
        public bool Animating
        {
            get { return _animating; }
            set
            {
                _animating = value;
                if (_animating) FinishSmoothZoom();
            }
        }
        public double RotateSensitivity { get { return _rotateSensitivity; } set { _rotateSensitivity = value; } }
        public double ZoomSensitivity { get { return _zoomSensitivity; } set { _zoomSensitivity = value; } }
        public bool InvertZoom { get { return _invertZoom; } set { _invertZoom = value; } }
        public bool SmoothZoom
        {
            get { return _smoothZoom; }
            set
            {
                _smoothZoom = value;
                if (!_smoothZoom) FinishSmoothZoom();
            }
        }


        // Getters
        public bool IsBoxSelectionActive { get { return _boxSelection; } }
        public bool NavigationAllowed { get { return !_animating && this.GetState() == VTKIS_NONE; } }
        public double[] GetRotationPoint()
        {
            return _rotationCenterWorld;
        }


        // Callbacks
        public GetPickPointDelegate GetPickPoint;
        public Action<int[], int[], bool> SetBoxZoom;
        public Action BoxZoomEnded;


        // Events                                                                                                                   
        public event Action<int, int, bool, int, int> PointPickedOnMouseMoveEvt;
        public event Action<MouseEventArgs, bool, int, int> PointPickedOnLeftUpEvt;
        public event Action<double[]> RotationPointChanged;
        public event Action ClearCurrentMouseSelection;
        public event Action<object, MouseEventArgs> RightButtonPressEvent;
        public event Action MouseInteractionEvent;
        public event Action ZoomChangedEvent;


        // Constructors                                                                                                             
        public vtkInteractorStyleControl()
        {
            _motionFactor = 10.0;
            _rotateSensitivity = 1;
            _zoomSensitivity = 1;
            _invertZoom = false;
            _smoothZoom = true;
            _smoothZoomRemainingFactor = 1;
            _smoothZoomStepping = false;
            _smoothZoomFrameMilliseconds = 0;
            _smoothZoomTimer = new System.Windows.Threading.DispatcherTimer();
            _smoothZoomTimer.Interval = new TimeSpan(0, 0, 0, 0, SmoothZoomIntervalMilliseconds);
            _smoothZoomTimer.Tick += _smoothZoomTimer_Tick;
            _rotationCenterWorld = null;
            _animating = false;
            _selectionTimer = new System.Windows.Threading.DispatcherTimer();
            _selectionTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            _selectionTimer.Tick += _selectionTimer_Tick;
            //
            _lastX = int.MinValue;
            _lastY = int.MinValue;
            _lastClickTime = DateTime.Now;
            //
            _selectionBoxEnabled = true;
            _boxZoomEnabled = false;
            _widgets = new List<vtkMaxBorderWidget>();
            _reversedWidgets = new List<vtkMaxBorderWidget>();
            _capturedWidget = null;
            _capturedWidgetPromoted = false;
            _widgetHighlightEnabled = true;
            _widgetLocalHighlightEnabled = true;
            //
            this.LeftButtonPressEvt += vtkInteractorStyleControl_LeftButtonPressEvt;
            this.LeftButtonReleaseEvt += vtkInteractorStyleControl_LeftButtonReleaseEvt;
            this.MiddleButtonPressEvt += vtkInteractorStyleControl_MiddleButtonPressEvt;
            this.MiddleButtonReleaseEvt += vtkInteractorStyleControl_MiddleButtonReleaseEvt;
            this.RightButtonPressEvt += vtkInteractorStyleControl_RightButtonPressEvt;
            this.RightButtonReleaseEvt += vtkInteractorStyleControl_RightButtonReleaseEvt;
            this.MouseMoveEvt += vtkInteractorStyleControl_MouseMoveEvt;
            this.MouseWheelForwardEvt += vtkInteractorStyleControl_MouseWheelForwardEvt;
            this.MouseWheelBackwardEvt += vtkInteractorStyleControl_MouseWheelBackwardEvt;
            this.KeyPressEvt += VtkInteractorStyleControl_KeyPressEvt;
            this.KeyReleaseEvt += VtkInteractorStyleControl_KeyReleaseEvt;
        }
        new public static vtkInteractorStyleControl New()
        {
            return new vtkInteractorStyleControl();
        }


        // Event handlers                                                                                                           
        void vtkInteractorStyleControl_LeftButtonPressEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            MouseInteractionEvent?.Invoke();
            if (_animating) return;
            FinishSmoothZoom();
            //
            vtkRenderWindowInteractor rwi = this.GetInteractor();
            //
            int x = rwi.GetEventPosition()[0];
            int y = rwi.GetEventPosition()[1];
            // Recover cleanly if the native control did not deliver a previous release.
            if (_capturedWidget != null)
            {
                _capturedWidget.LeftButtonRelease(x, y);
                _capturedWidget = null;
                _capturedWidgetPromoted = false;
            }
            //
            _numOfClicks = 1;
            // Double click
            if (DateTime.Now - _lastClickTime < _doubleClickTimeSpan &&
                Math.Abs(x - _lastX) < _doubleClickDisp &&
                Math.Abs(y - _lastY) < _doubleClickDisp) _numOfClicks = 2;
            // Set global variable
            _clickPos = new int[] { x, y };
            // World click pos
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;
            _clickPosWorld = DisplayToWorld(renderer, new double[] { x, y });
            // Widgets
            vtkMaxBorderWidget borderWidget = null;
            MouseEventArgs mea = new MouseEventArgs(MouseButtons.Left, _numOfClicks, x, y, 0);
            foreach (vtkMaxBorderWidget widget in _reversedWidgets)
            {
                if (widget.LeftButtonPress(mea))
                {
                    borderWidget = widget;
                    break;  // do not return here to set _lastX, lastY, _lastClickTime
                }
            }
            if (borderWidget != null)
            {
                _capturedWidget = borderWidget;
                _capturedWidgetPromoted = false;
                if (_numOfClicks == 2)
                {
                    PromoteWidget(borderWidget);
                    _capturedWidgetPromoted = true;
                }
                UpdateWidgetHighlights(x, y);
                rwi.Render();
            }
            //
            if (borderWidget == null)
            {
                _leftMouseButtonPressed = true;
                _boxSelection = false;
                _selectionCanceled = false;
                // Area selection
                vtkPoints backgroundPoints = _selectionBackgroundPoints;
                backgroundPoints.SetPoint(0, _clickPos[0], _clickPos[1], 0.0);
                backgroundPoints.SetPoint(1, _clickPos[0], _clickPos[1], 0.0);
                backgroundPoints.SetPoint(2, _clickPos[0], _clickPos[1], 0.0);
                backgroundPoints.SetPoint(3, _clickPos[0], _clickPos[1], 0.0);
                backgroundPoints.Modified();
            }
            //
            _lastX = x;
            _lastY = y;
            _lastClickTime = DateTime.Now;
        }
        void vtkInteractorStyleControl_LeftButtonReleaseEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            if (_animating) return;
            //
            vtkRenderWindowInteractor rwi = this.GetInteractor();
            //
            int x = rwi.GetEventPosition()[0];
            int y = rwi.GetEventPosition()[1];
            //
            _selectionBackgroundActor.VisibilityOff();
            _selectionBorderActor.VisibilityOff();
            //
            // A pressed widget exclusively owns the pointer until this release.
            bool widgetHandled = _capturedWidget != null;
            if (_capturedWidget != null)
            {
                _capturedWidget.LeftButtonRelease(x, y);
                _capturedWidget = null;
                _capturedWidgetPromoted = false;
                UpdateWidgetHighlights(x, y);
            }
            if (!widgetHandled && !_selectionCanceled)
            {
                int[] mousePos = rwi.GetEventPosition();
                //
                if (PointPickedOnLeftUpEvt != null)
                {
                    if (_clickPos == null) { }
                    else
                    {
                        if (_boxZoomEnabled)
                        {
                            SetBoxZoom(_clickPos, mousePos, true);
                        }
                        else
                        {
                            MouseEventArgs mea = new MouseEventArgs(MouseButtons.Left, _numOfClicks, mousePos[0], mousePos[1], 0);
                            PointPickedOnLeftUpEvt(mea, _boxSelection, _clickPos[0], _clickPos[1]);
                        }
                    }
                }
            }
            //
            _clickPos = null;
            _clickPosWorld = null;
            _leftMouseButtonPressed = false;
            _boxSelection = false;
            BoxZoomEnabled = false;
            //
            rwi.Render();
        }
        //
        void vtkInteractorStyleControl_MiddleButtonPressEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            MouseInteractionEvent?.Invoke();
            if (_animating) return;
            FinishSmoothZoom();
            ClearWidgetHighlightsInternal();
            // Cancel box selection
            CancelBoxSelection();
            //
            base.OnLeftButtonDown();
            //
            vtkRenderWindowInteractor rwi = this.GetInteractor();
            //
            int x = rwi.GetEventPosition()[0];
            int y = rwi.GetEventPosition()[1];
            this.FindPokedRenderer(x, y);
            //
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;
            //
            _clickPos = rwi.GetEventPosition();     // set global variable
            //
            UpdateRotationCenterDisplay();
            //
            AddAnnotationForCenter3D(rwi, renderer);
            // Widgets - middle pressed
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                widget.MiddleButtonPress(x, y);
            }
            //
            rwi.Render();
        }
        void vtkInteractorStyleControl_MiddleButtonReleaseEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            if (_animating) return;
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;
            // Remove first in order not to affect the bounds of the model
            RemoveAnnotationForCenter(renderer);
            //
            vtkRenderWindowInteractor rwi = this.GetInteractor();
            int[] currClickPos = rwi.GetEventPosition();
            //
            if (_clickPos != null && Math.Abs(_clickPos[0] - currClickPos[0]) + Math.Abs(_clickPos[0] - currClickPos[0]) < 2)
            {
                // Only click happened
                if (GetPickPoint != null)
                {
                    vtkActor actor;
                    double[] pickPoint = GetPickPoint(out actor, currClickPos[0], currClickPos[1]);
                    if (pickPoint != null) _rotationCenterWorld = pickPoint;
                    //
                    if (actor == null) // picker.GetActor() == null
                    {
                        // The click was not a hit
                        // Use the center of the bounding box
                        double[] bounds1 = renderer.ComputeVisiblePropBounds();
                        int numActors = _overlayRenderer.GetNumberOfPropsRendered();
                        if (numActors > 0)
                        {
                            double[] bounds2 = _overlayRenderer.ComputeVisiblePropBounds();
                            double[] max = new double[] { Math.Min(bounds1[0], bounds2[0]), Math.Max(bounds1[1], bounds2[1]),
                                                          Math.Min(bounds1[2], bounds2[2]), Math.Max(bounds1[3], bounds2[3]),
                                                          Math.Min(bounds1[4], bounds2[4]), Math.Max(bounds1[5], bounds2[5])};
                            _rotationCenterWorld[0] = (max[1] + max[0]) / 2;
                            _rotationCenterWorld[1] = (max[3] + max[2]) / 2;
                            _rotationCenterWorld[2] = (max[5] + max[4]) / 2;
                        }
                        else
                        {
                            _rotationCenterWorld[0] = (bounds1[1] + bounds1[0]) / 2;
                            _rotationCenterWorld[1] = (bounds1[3] + bounds1[2]) / 2;
                            _rotationCenterWorld[2] = (bounds1[5] + bounds1[4]) / 2;
                        }
                        //-   
                    }
                    else
                    {
                        // The click was a hit
                        vtkCamera camera = renderer.GetActiveCamera();
                        double[] center = camera.GetFocalPoint();
                        PanCamera(camera, center, _rotationCenterWorld);
                        AdjustCameraDistance(renderer, camera);
                    }
                    //
                    RotationPointChanged?.Invoke(_rotationCenterWorld);
                }
            }
            // Adjust the clipping range and lights
            if (this.GetAutoAdjustCameraClippingRange() == 1) ResetClippingRange();
            if (rwi.GetLightFollowCamera() == 1) renderer.UpdateLightsGeometryToFollowCamera();
            // Widgets - middle released
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                widget.MiddleButtonRelease(currClickPos[0], currClickPos[1]);
            }
            //
            rwi.Modified();
            //
            _clickPos = null;
            _clickPosWorld = null;
            //
            base.OnLeftButtonUp();  // left button is rotation by default
        }
        //
        void vtkInteractorStyleControl_RightButtonPressEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            MouseInteractionEvent?.Invoke();
            if (_animating) return;
            FinishSmoothZoom();
            //
            vtkRenderWindowInteractor rwi = this.GetInteractor();
            int x = rwi.GetEventPosition()[0];
            int y = rwi.GetEventPosition()[1];
            //
            this.FindPokedRenderer(x, y);
            //
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer != null)
            {
                int[] clickPos = rwi.GetEventPosition();
                // Widgets - right pressed
                vtkMaxBorderWidget clickedWidget = null;
                MouseEventArgs mea = new MouseEventArgs(MouseButtons.Right, 1, x, y, 0);
                //
                foreach (vtkMaxBorderWidget widget in _reversedWidgets)
                {
                    if (widget.RightButtonPress(x, y))
                    {
                        clickedWidget = widget;
                        break;
                    }
                }
                if (clickedWidget != null)
                {
                    PromoteWidget(clickedWidget);
                    UpdateWidgetHighlights(x, y);
                    rwi.Render();
                }
                //
                RightButtonPressEvent?.Invoke(clickedWidget, mea);
            }
        }
        void vtkInteractorStyleControl_RightButtonReleaseEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            if (_animating) return;
        }
        //
        void vtkInteractorStyleControl_MouseMoveEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            if (_animating) return;
            //
            _x = this.GetInteractor().GetEventPosition()[0];
            _y = this.GetInteractor().GetEventPosition()[1];
            int interactionState = this.GetState();
            bool widgetMoved = false;
            bool widgetHighlightChanged = false;
            bool widgetProximityChanged = false;
            if (interactionState == VTKIS_NONE)
            {
                // Widgets - Move
                widgetMoved = _capturedWidget != null && _capturedWidget.MouseMove(_x, _y);
                if (widgetMoved && !_capturedWidgetPromoted)
                {
                    PromoteWidget(_capturedWidget);
                    _capturedWidgetPromoted = true;
                }
                // Widgets - Proximity - must be before the hover to update the widget content first
                widgetProximityChanged = UpdateWidgetProximity(_x, _y);
                // Widgets - Hover
                widgetHighlightChanged = UpdateWidgetHighlights(_x, _y);
                if (widgetMoved || widgetHighlightChanged || widgetProximityChanged)
                {
                    this.GetInteractor().Render();
                    return;
                }
            }
            //
            switch (interactionState)
            {
                case VTKIS_NONE:
                    _selectionTimer.Stop();
                    _selectionTimer.Start();
                    //
                    if (_selectionBoxEnabled || _boxZoomEnabled)
                    {
                        if (!_boxSelection && _leftMouseButtonPressed && _clickPos != null && _numOfClicks != 2 &&
                            Math.Abs(_x - _clickPos[0]) > 1 && Math.Abs(_y - _clickPos[1]) > 1)
                        {
                            _boxSelection = true;
                        }
                        if (_boxSelection) DrawBoxSelection(_x, _y);
                    }
                    break;
                case VTKIS_ROTATE:
                    this.FindPokedRenderer(_x, _y);
                    this.Rotate();
                    this.InvokeEvent((uint)EventIds.InteractionEvent, IntPtr.Zero);
                    break;
                case VTKIS_PAN:
                    this.FindPokedRenderer(_x, _y);
                    this.Pan();
                    this.InvokeEvent((uint)EventIds.InteractionEvent, IntPtr.Zero);
                    break;
                case VTKIS_SPIN:
                    this.FindPokedRenderer(_x, _y);
                    this.Zoom();
                    this.InvokeEvent((uint)EventIds.InteractionEvent, IntPtr.Zero);
                    break;
            }
        }
        //
        void vtkInteractorStyleControl_MouseWheelForwardEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            MouseInteractionEvent?.Invoke();
            if (_animating) return;
            //
            if (this.GetState() == VTKIS_NONE)
            {
                int[] clickPos = this.GetInteractor().GetEventPosition();
                ZoomAtDisplayPointSmooth(GetMouseWheelZoomFactor(), clickPos[0], clickPos[1]);
            }            
        }
        void vtkInteractorStyleControl_MouseWheelBackwardEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            MouseInteractionEvent?.Invoke();
            if (_animating) return;
            //
            if (this.GetState() == VTKIS_NONE)
            {
                int[] clickPos = this.GetInteractor().GetEventPosition();
                ZoomAtDisplayPointSmooth(1 / GetMouseWheelZoomFactor(), clickPos[0], clickPos[1]);
            }
        }
        private double GetMouseWheelZoomFactor()
        {
            double ctrlKeyFactor = this.GetInteractor().GetControlKey();
            if (ctrlKeyFactor == 0) ctrlKeyFactor = 1;
            else ctrlKeyFactor = 0.2;
            //
            return Math.Pow(1.1, this._motionFactor * ctrlKeyFactor * 0.2 * this.GetMouseWheelMotionFactor());
        }
        //
        private void VtkInteractorStyleControl_KeyPressEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            if (_animating) return;
            FinishSmoothZoom();
            //
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;
            //
            vtkRenderWindowInteractor rwi = this.GetInteractor();
            sbyte key = rwi.GetKeyCode();
            // Arrow keys for rotation
            if (key >= 37 && key <= 40) 
            {
                CancelBoxSelection();
                //
                UpdateRotationCenterDisplay();
                rwi.Modified();
                //
                double delta = 5;
                if (key == 37) Rotate(+delta, 0);       // left
                else if (key == 39) Rotate(-delta, 0);  // right
                else if (key == 38) Rotate(0, -delta);  // up
                else if (key == 40) Rotate(0, +delta);  // down
                //
                rwi.Modified();
                rwi.Render();                
            }
            // Escape key
            else if (key == 27)
            {
                CancelBoxSelection();
            }
            // Box zoom on Z or z
            else if (key == 90 || key == 122)
            {
                BoxZoomEnabled = true;
            }
        }
        private void VtkInteractorStyleControl_KeyReleaseEvt(vtkObject sender, vtkObjectEventArgs e)
        {
            if (_animating) return;
            //
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;
            //
            vtkRenderWindowInteractor rwi = this.GetInteractor();
            sbyte key = rwi.GetKeyCode();
            // Box zoom on Z or z
            if (key == 90 || key == 122)
            {
                if (!_leftMouseButtonPressed) BoxZoomEnabled = false;
            }
        }


        // Public setters                                                                                                           
        public void SetSelectionRenderer(vtkRenderer renderer)
        {
            _selectionRenderer = renderer;
            InitializeBoxSelection();
        }
        public void SetOverlayRenderer(vtkRenderer renderer)
        {
            _overlayRenderer = renderer;
        }
        public void SetRotationPoint(double[] rotationPoint)
        {
            if (rotationPoint != null) _rotationCenterWorld = rotationPoint;
            else ResetRotationCenter();
        }


        // Methods                                                                                                                  
        void _selectionTimer_Tick(object sender, EventArgs e)
        {
            _selectionTimer.Stop();
            this.FindPokedRenderer(_x, _y);
            this.SelectOnMouseMove(_x, _y);
        }
        public bool IsPositionOverWidget(int x, int y)
        {
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                if (widget.MouseOver(x, y)) return true;
            }
            return false;
        }
        //
        public void AddVtkMaxWidget(vtkMaxBorderWidget widget)
        {
            widget.SetLocalHighlightEnabled(_widgetLocalHighlightEnabled);
            _widgets.Add(widget);
            _reversedWidgets.Insert(0, widget);
        }
        public void RemoveVtkMaxWidget(vtkMaxBorderWidget widget)
        {
            if (_capturedWidget == widget)
            {
                _capturedWidget = null;
                _capturedWidgetPromoted = false;
            }
            widget.ClearMouseHover();
            _widgets.Remove(widget);
            _reversedWidgets.Remove(widget);
        }
        public void VtkMaxWidgetVisibilityOn(vtkMaxBorderWidget widget)
        {
            if (!_widgets.Contains(widget)) return;
            //
            MoveWidgetToTop(widget);
        }
        public void ClearWidgetHighlights()
        {
            bool highlightChanged = ClearWidgetHighlightsInternal();
            if (highlightChanged && GetInteractor() != null) GetInteractor().Render();
        }
        public void ClearWidgetProximity()
        {
            bool proximityChanged = false;
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                if (widget.ClearMouseProximity()) proximityChanged = true;
            }
            if (proximityChanged && GetInteractor() != null) GetInteractor().Render();
        }
        public void RefreshWidgetHighlights()
        {
            vtkRenderWindowInteractor interactor = GetInteractor();
            if (interactor == null) return;
            //
            int[] position = interactor.GetEventPosition();
            UpdateWidgetHighlights(position[0], position[1]);
        }
        public void SetWidgetLocalHighlightEnabled(bool enabled)
        {
            _widgetLocalHighlightEnabled = enabled;
            foreach (vtkMaxBorderWidget widget in _widgets) widget.SetLocalHighlightEnabled(enabled);
            RefreshWidgetHighlights();
        }
        public void SetWidgetHighlightEnabled(bool enabled)
        {
            if (_widgetHighlightEnabled == enabled) return;
            //
            _widgetHighlightEnabled = enabled;
            if (enabled) RefreshWidgetHighlights();
            else ClearWidgetHighlights();
        }
        private bool ClearWidgetHighlightsInternal()
        {
            bool highlightChanged = false;
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                if (widget.ClearMouseHover()) highlightChanged = true;
            }
            return highlightChanged;
        }
        private void PromoteWidget(vtkMaxBorderWidget widget)
        {
            MoveWidgetToTop(widget);
            UpdateWidgetZOrder();
        }
        private void MoveWidgetToTop(vtkMaxBorderWidget widget)
        {
            _widgets.Remove(widget);
            _widgets.Add(widget);
            _reversedWidgets.Remove(widget);
            _reversedWidgets.Insert(0, widget);
        }
        private bool UpdateWidgetProximity(int x, int y)
        {
            bool proximityChanged = false;
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                if (widget.UpdateMouseProximity(x, y)) proximityChanged = true;
            }
            return proximityChanged;
        }
        private bool UpdateWidgetHighlights(int x, int y)
        {
            if (!_widgetHighlightEnabled) return ClearWidgetHighlightsInternal();
            //
            vtkMaxBorderWidget highlightedWidget = _capturedWidget;
            if (highlightedWidget == null)
            {
                foreach (vtkMaxBorderWidget widget in _reversedWidgets)
                {
                    if (widget.MouseOver(x, y))
                    {
                        highlightedWidget = widget;
                        break;
                    }
                }
            }
            //
            bool highlightChanged = false;
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                bool isHighlighted = widget == highlightedWidget;
                bool highlightEntireWidget = isHighlighted && _capturedWidget != null;
                if (widget.UpdateMouseHover(x, y, isHighlighted, highlightEntireWidget)) highlightChanged = true;
            }
            if (highlightChanged) UpdateWidgetZOrder();
            return highlightChanged;
        }
        private void UpdateWidgetZOrder()
        {
            // Each widget highlight belongs directly above its own content and below all widgets
            // that are logically in front of it.
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                widget.BringToFront();
                widget.BringHighlightToFront();
            }
        }
        public void ResetRotationCenter()
        {
            _rotationCenterWorld = null;
        }
        private void UpdateRotationCenterDisplay()
        {
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;
            //
            if (_rotationCenterWorld == null)
            {
                // For the first time use the center of the bounding box
                double[] bounds = renderer.ComputeVisiblePropBounds();
                _rotationCenterWorld = new double[3];
                _rotationCenterWorld[0] = (bounds[1] + bounds[0]) / 2;
                _rotationCenterWorld[1] = (bounds[3] + bounds[2]) / 2;
                _rotationCenterWorld[2] = (bounds[5] + bounds[4]) / 2;
            }
            // Must update since zoom can also change
            _rotationCenterDisplay = WorldToDisplay(renderer, _rotationCenterWorld);
        }
        public void StartMultiTouchGesture(int x, int y, bool showRotationCenter)
        {
            if (_animating) return;
            FinishSmoothZoom();
            //
            ClearWidgetHighlightsInternal();
            CancelBoxSelection();
            //
            this.FindPokedRenderer(x, y);
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;
            //
            UpdateRotationCenterDisplay();
            //
            if (showRotationCenter)
            {
                if (_centerAnnotationActor == null) AddAnnotationForCenter3D(this.GetInteractor(), renderer);
            }
            else RemoveAnnotationForCenter(renderer);
            //
            this.GetInteractor().Modified();
        }
        public void EndMultiTouchGesture()
        {
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;
            //
            RemoveAnnotationForCenter(renderer);
            //
            vtkRenderWindowInteractor rwi = this.GetInteractor();
            rwi.Modified();
            rwi.Render();
        }
        public static double[] WorldToDisplay(vtkRenderer renderer, double[] worldPos)
        {
            // ComputeWorldToDisplay writes three doubles, not the two that are read back out of the buffer
            IntPtr posOutPtr = Marshal.AllocHGlobal(3 * 8);
            try
            {
                ComputeWorldToDisplay(renderer, worldPos[0], worldPos[1], worldPos[2], posOutPtr);
                double[] displayPos = new double[2];
                Marshal.Copy(posOutPtr, displayPos, 0, 2);
                return displayPos;
            }
            finally
            {
                Marshal.FreeHGlobal(posOutPtr);
            }
        }
        public static double[] DisplayToWorld(vtkRenderer renderer, double[] displayPos)
        {
            // ComputeDisplayToWorld writes four doubles
            IntPtr posOutPtr = Marshal.AllocHGlobal(4 * 8);
            try
            {
                ComputeDisplayToWorld(renderer, displayPos[0], displayPos[1], 0, posOutPtr);
                double[] worldPos = new double[3];
                Marshal.Copy(posOutPtr, worldPos, 0, 3);
                return worldPos;
            }
            finally
            {
                Marshal.FreeHGlobal(posOutPtr);
            }
        }
        public static double DisplayToWorldScale(vtkRenderer renderer, int size = 1)
        {
            int delta = 1 * size;
            int x = 0;
            int y = 0;
            double[] p1 = vtkInteractorStyleControl.DisplayToWorld(renderer, new double[] { x, y });
            double[] p2 = vtkInteractorStyleControl.DisplayToWorld(renderer, new double[] { x + delta, y });
            return Math.Sqrt(Math.Pow(p1[0] - p2[0], 2)
                             + Math.Pow(p1[1] - p2[1], 2)
                             + Math.Pow(p1[2] - p2[2], 2));
        }
        public override void Pan()
        {
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;
            //
            vtkRenderWindowInteractor rwi = this.GetInteractor();
            int[] clickPos = rwi.GetEventPosition();
            //
            double[] newPickPoint;
            double[] oldPickPoint;
            //
            newPickPoint = DisplayToWorld(renderer, new double[] { clickPos[0], clickPos[1] });
            oldPickPoint = DisplayToWorld(renderer, new double[] { rwi.GetLastEventPosition()[0], rwi.GetLastEventPosition()[1] });
            //
            vtkCamera camera = renderer.GetActiveCamera();
            PanCamera(camera, newPickPoint, oldPickPoint);
            //
            if (rwi.GetLightFollowCamera() == 1) renderer.UpdateLightsGeometryToFollowCamera();
            // Widgets - Pan
            int dx;
            int dy;
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                // Compute the delta
                dx = (int)(clickPos[0] - rwi.GetLastEventPosition()[0]);
                dy = (int)(clickPos[1] - rwi.GetLastEventPosition()[1]);
                //
                widget.MousePan(dx, dy);
            }
            rwi.Render();
        }
        private void PanCamera(vtkCamera camera, double[] posStart, double[] posEnd)
        {
            double[] motionVector = new double[3];
            motionVector[0] = posEnd[0] - posStart[0];
            motionVector[1] = posEnd[1] - posStart[1];
            motionVector[2] = posEnd[2] - posStart[2];
            //
            double[] cPos = camera.GetPosition();
            double[] fPos = camera.GetFocalPoint();
            //
            //System.Diagnostics.Debug.WriteLine(string.Format("cPos1 x: {0},  y: {1},  z: {2}", cPos[0], cPos[1], cPos[2]));
            //
            camera.SetPosition(cPos[0] + motionVector[0], cPos[1] + motionVector[1], cPos[2] + motionVector[2]);
            camera.SetFocalPoint(fPos[0] + motionVector[0], fPos[1] + motionVector[1], fPos[2] + motionVector[2]);
            //
            AdjustCameraDistanceAndClipping();
        }
        public void PanByDisplayDelta(double dx, double dy, bool render = true)
        {
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;
            //
            int[] size = renderer.GetSize();
            double[] basePos = new double[] { size[0] / 2.0, size[1] / 2.0 };
            double[] newPickPoint = DisplayToWorld(renderer, new double[] { basePos[0] + dx, basePos[1] + dy });
            double[] oldPickPoint = DisplayToWorld(renderer, basePos);
            //
            vtkCamera camera = renderer.GetActiveCamera();
            PanCamera(camera, newPickPoint, oldPickPoint);
            //
            vtkRenderWindowInteractor rwi = this.GetInteractor();
            if (rwi.GetLightFollowCamera() == 1) renderer.UpdateLightsGeometryToFollowCamera();
            // Widgets - Pan
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                widget.MousePan((int)dx, (int)dy);
            }
            //
            rwi.Modified();
            if (render) rwi.Render();
        }
        // Zoom on mouse move - slow zooming
        public override void Zoom()
        {
            ClearWidgetHighlightsInternal();
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;
            //
            vtkRenderWindowInteractor rwi = this.GetInteractor();
            int[] clickPos = rwi.GetEventPosition();
            //
            vtkCamera camera = renderer.GetActiveCamera();
            double factor = rwi.GetLastEventPosition()[1] - clickPos[1];
            if (factor < 0) factor = Math.Pow(1.001, factor);
            else factor = Math.Pow(1 / 1.001, -factor);
            factor = Math.Pow(factor, _zoomSensitivity);
            //
            camera.SetParallelScale(camera.GetParallelScale() * factor);
            // Widgets - mouse wheel scrolled
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                widget.MouseWheelScrolled();
            }
            //
            if (ZoomChangedEvent != null) ZoomChangedEvent();
            //
            rwi.Modified();
            rwi.Render();
        }
        public void ZoomAtDisplayPoint(double parallelScaleFactor, int x, int y, bool render = true)
        {
            this.FindPokedRenderer(x, y);
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;
            //
            double[] displayPos = new double[] { x, y };
            double[] worldPos = DisplayToWorld(renderer, displayPos);
            //
            double factor = Math.Pow(parallelScaleFactor, _zoomSensitivity);
            if (_invertZoom) factor = 1 / factor;
            //
            vtkCamera camera = renderer.GetActiveCamera();
            camera.SetParallelScale(camera.GetParallelScale() * factor);
            //
            double[] worldPosAfter = DisplayToWorld(renderer, displayPos);
            PanCamera(camera, worldPosAfter, worldPos);
            // Box selection
            if (_boxSelection) DrawBoxSelection(_x, _y);
            // Widgets - mouse wheel scrolled
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                widget.MouseWheelScrolled();
            }
            //
            if (ZoomChangedEvent != null) ZoomChangedEvent();
            //
            this.GetInteractor().Modified();
            // A zoom may reposition projected widgets; restore the hover at the
            // unchanged pointer position after all widget geometry has been updated.
            UpdateWidgetHighlights(x, y);
            if (render) this.GetInteractor().Render();
        }
        // The mouse wheel notch is large enough that an immediate jump breaks the optical flow of the
        // model, so the notches accumulate into a target the camera then approaches over a few frames
        public void ZoomAtDisplayPointSmooth(double parallelScaleFactor, int x, int y)
        {
            _smoothZoomX = x;
            _smoothZoomY = y;
            _smoothZoomRemainingFactor *= parallelScaleFactor;
            //
            if (!_smoothZoom || _smoothZoomFrameMilliseconds > SmoothZoomMaxFrameMilliseconds)
            {
                FinishSmoothZoom();
                return;
            }
            // A mouse move selection left pending by the move that preceded the wheel would otherwise
            // pick against a camera that is still moving
            if (!_smoothZoomTimer.IsEnabled) _selectionTimer.Stop();
            //
            _smoothZoomTimer.Start();
        }
        public void FinishSmoothZoom()
        {
            _smoothZoomTimer.Stop();
            ApplyRemainingSmoothZoom();
        }
        private void _smoothZoomTimer_Tick(object sender, EventArgs e)
        {
            // Rendering pumps the message loop, which can deliver the next tick before this one returns
            if (_smoothZoomStepping) return;
            _smoothZoomStepping = true;
            //
            double logRemainingFactor = Math.Log(_smoothZoomRemainingFactor);
            // The scene has no level of detail path, so the smoothing steps out of the way as soon as
            // a single frame no longer fits the budget
            if (Math.Abs(logRemainingFactor) < SmoothZoomMinLogFactor ||
                _smoothZoomFrameMilliseconds > SmoothZoomMaxFrameMilliseconds)
            {
                _smoothZoomTimer.Stop();
                ApplyRemainingSmoothZoom();
                // The hover selection was held back for the duration and settles on the final camera
                _selectionTimer.Stop();
                _selectionTimer.Start();
            }
            else
            {
                // A constant ratio per frame is what reads as a uniform speed under a parallel projection
                double stepFactor = Math.Exp(logRemainingFactor * SmoothZoomApproachRatio);
                _smoothZoomRemainingFactor /= stepFactor;
                ZoomAtDisplayPointTimed(stepFactor, _smoothZoomX, _smoothZoomY);
            }
            //
            _smoothZoomStepping = false;
        }
        private void ApplyRemainingSmoothZoom()
        {
            if (_smoothZoomRemainingFactor == 1) return;
            //
            double parallelScaleFactor = _smoothZoomRemainingFactor;
            _smoothZoomRemainingFactor = 1;
            ZoomAtDisplayPointTimed(parallelScaleFactor, _smoothZoomX, _smoothZoomY);
        }
        private void ZoomAtDisplayPointTimed(double parallelScaleFactor, int x, int y)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ZoomAtDisplayPoint(parallelScaleFactor, x, y);
            stopwatch.Stop();
            _smoothZoomFrameMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
        }
        //
        public override void Rotate()
        {
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;
            //
            vtkRenderWindowInteractor rwi = this.GetInteractor();
            int[] clickPos = rwi.GetEventPosition();
            // Compute the angle
            int dx = clickPos[0] - rwi.GetLastEventPosition()[0];
            int dy = clickPos[1] - rwi.GetLastEventPosition()[1];
            //
            RotateByDisplayDelta(dx, dy);
        }
        public void RotateByDisplayDelta(double dx, double dy, bool render = true)
        {
            double delta_elevation = -20.0 / 500;
            double delta_azimuth = -20.0 / 500;
            //
            double rxf = dx * delta_azimuth * this._motionFactor * _rotateSensitivity;
            double ryf = dy * delta_elevation * this._motionFactor * _rotateSensitivity;
            //
            Rotate(rxf, ryf, 0, render);
        }
        public void Rotate(double azimuthAngle, double elevationAngle, double rollAngle = 0, bool render = true)
        {
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;
            //
            vtkCamera camera = renderer.GetActiveCamera();
            vtkRenderWindowInteractor rwi = this.GetInteractor();
            //
            if (azimuthAngle != 0) camera.Azimuth(azimuthAngle);
            if (elevationAngle != 0) camera.Elevation(elevationAngle);
            if (rollAngle != 0) camera.Roll(rollAngle);
            camera.OrthogonalizeViewUp();
            // Move the cameras focal point back to the display position of the first click - the opposite direction!
            double[] wpa = DisplayToWorld(renderer, _rotationCenterDisplay);
            double[] n = camera.GetViewPlaneNormal();
            // Compute a plane through rotationWorldCenter and project the rotationDisplayCenter on it
            double[] v = new double[3];                         // vector from rotationWorldCenter to DisplayToWorld point
            double[] rotCenter = _rotationCenterWorld;
            if (rotCenter == null) rotCenter = new double[3];
            v[0] = wpa[0] - rotCenter[0];
            v[1] = wpa[1] - rotCenter[1];
            v[2] = wpa[2] - rotCenter[2];
            double d = n[0] * v[0] + n[1] * v[1] + n[2] * v[2]; // distance of DisplayToWorld point from plane
            wpa[0] -= n[0] * d;
            wpa[1] -= n[1] * d;
            wpa[2] -= n[2] * d;
            //
            PanCamera(camera, wpa, rotCenter);   // calls AdjustCameraDistanceAndClipping()
            // Adjust the lights
            if (rwi.GetLightFollowCamera() == 1) renderer.UpdateLightsGeometryToFollowCamera();
            // Widgets - Rotate
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                widget.MouseRotate(azimuthAngle, elevationAngle);
            }
            rwi.Modified();
            if (render) rwi.Render();
        }
        //
        private void SelectOnMouseMove(int x, int y)
        {
            // This is used 
            if (PointPickedOnMouseMoveEvt != null && !_boxZoomEnabled)
            {
                int[] clickPos;
                vtkRenderer renderer = this.GetCurrentRenderer();
                if (_clickPos != null && _clickPosWorld != null && renderer != null)
                {
                    // Box selection
                    if (_boxSelection)
                    {
                        double[] clickPosD = WorldToDisplay(renderer, _clickPosWorld);
                        clickPos = new int[] { (int)clickPosD[0], (int)clickPosD[1] };
                    }
                    // Mouse click selection
                    else
                    {
                        clickPos = _clickPos;
                    }
                }
                // Moving the mouse without clicking selection
                else clickPos = new int[] { 0, 0 };
                //
                PointPickedOnMouseMoveEvt(x, y, _boxSelection, clickPos[0], clickPos[1]);
            }
        }
        private void DrawBoxSelection(int x, int y)
        {
            if (_clickPosWorld == null) return;
            //
            vtkRenderer renderer = this.GetCurrentRenderer();
            if (renderer == null) return;
            //
            double[] clickPos = WorldToDisplay(renderer, _clickPosWorld);
            //
            _selectionBackgroundActor.VisibilityOn();
            _selectionBorderActor.VisibilityOn();
            // Border
            if (clickPos[0] > x)
            {
                _selectionBorderActor.GetProperty().SetColor(0.3, 1.0, 0.3);
                _selectionBackgroundActor.GetProperty().SetColor(0.2, 1, 0.5);
                //_selectionBorderActor.GetProperty().SetLineStipplePattern(65280);
            }
            else
            {
                _selectionBorderActor.GetProperty().SetColor(0.3, 0.3, 1.0);
                _selectionBackgroundActor.GetProperty().SetColor(0.2, 0.5, 1);
                //_selectionBorderActor.GetProperty().SetLineStipplePattern(0xffff);
            }
            //
            vtkRenderWindowInteractor rwi = this.GetInteractor();
            vtkPoints backgroundPoints = _selectionBackgroundPoints;
            //
            int id1 = 1;
            int id2 = 2;
            int id3 = 3;
            // Switch ids 1 and 3 if II or IV quadrant
            if ((x < clickPos[0] && y > clickPos[1]) || (x > clickPos[0] && y < clickPos[1]))
            {
                id1 = 3;
                id3 = 1;
            }
            //
            backgroundPoints.SetPoint(0, clickPos[0], clickPos[1], 0.0);
            backgroundPoints.SetPoint(id1, x, clickPos[1], 0.0);
            backgroundPoints.SetPoint(id2, x, y, 0.0);
            backgroundPoints.SetPoint(id3, clickPos[0], y, 0.0);
            backgroundPoints.Modified();
            //
            rwi.Render();
        }
        private void CancelBoxSelection()
        {
            if (_boxSelection)
            {
                _selectionCanceled = true;
                _clickPos = null;
                _clickPosWorld = null;
                _boxSelection = false;
                BoxZoomEnabled = false;
                //
                _selectionBackgroundActor.VisibilityOff();
                _selectionBorderActor.VisibilityOff();
                //
                ClearCurrentMouseSelection?.Invoke();
                //
                this.GetInteractor().Render();
            }
        }
        //
        private void AddAnnotationForCenter3D(vtkRenderWindowInteractor interactor, vtkRenderer renderer)
        {
            if (_centerAnnotationActor != null) RemoveAnnotationForCenter(renderer);
            //
            int[] size = renderer.GetSize();
            double minSize = Math.Min(size[0], size[1]);
            vtkCamera camera = renderer.GetActiveCamera();
            double scale = camera.GetParallelScale();
            double dMin = 30 / minSize * scale;
            double dMax = 100 / minSize * scale;
            //
            vtkPoints points = vtkPoints.New();
            points.InsertNextPoint(_rotationCenterWorld[0] + dMin, _rotationCenterWorld[1], _rotationCenterWorld[2]);
            points.InsertNextPoint(_rotationCenterWorld[0] + dMax, _rotationCenterWorld[1], _rotationCenterWorld[2]);
            //
            points.InsertNextPoint(_rotationCenterWorld[0] - dMin, _rotationCenterWorld[1], _rotationCenterWorld[2]);
            points.InsertNextPoint(_rotationCenterWorld[0] - dMax, _rotationCenterWorld[1], _rotationCenterWorld[2]);
            //
            points.InsertNextPoint(_rotationCenterWorld[0], _rotationCenterWorld[1] + dMin, _rotationCenterWorld[2]);
            points.InsertNextPoint(_rotationCenterWorld[0], _rotationCenterWorld[1] + dMax, _rotationCenterWorld[2]);
            //
            points.InsertNextPoint(_rotationCenterWorld[0], _rotationCenterWorld[1] - dMin, _rotationCenterWorld[2]);
            points.InsertNextPoint(_rotationCenterWorld[0], _rotationCenterWorld[1] - dMax, _rotationCenterWorld[2]);
            //
            points.InsertNextPoint(_rotationCenterWorld[0], _rotationCenterWorld[1], _rotationCenterWorld[2] + dMax * 1.3);
            points.InsertNextPoint(_rotationCenterWorld[0], _rotationCenterWorld[1], _rotationCenterWorld[2] - dMax * 0.7);
            //
            vtkCellArray lineIndices = vtkCellArray.New();
            for (int i = 0; i < 5; i++)
            {
                lineIndices.InsertNextCell(2);
                lineIndices.InsertCellPoint(2 * i);
                lineIndices.InsertCellPoint(2 * i + 1);
            }
            //
            vtkPolyData pointsPolyData = vtkPolyData.New();
            pointsPolyData.SetPoints(points);
            pointsPolyData.SetLines(lineIndices);
            // Visualize
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInput(pointsPolyData);
            mapper.Update();
            //
            vtkActor actor = vtkActor.New();
            actor.SetMapper(mapper);
            actor.GetProperty().SetColor(1, 0.7, 0);
            //actor.GetProperty().SetLineWidth(1);
            renderer.AddActor(actor);
            _centerAnnotationActor = actor;
        }
        private void RemoveAnnotationForCenter(vtkRenderer renderer)
        {
            if (_centerAnnotationActor != null)
            {
                renderer.RemoveActor(_centerAnnotationActor);
                _centerAnnotationActor.Dispose();
                _centerAnnotationActor = null;
            }
        }
        public void AdjustCameraDistanceAndClipping(bool clearWidgetHighlights = true)
        {
            if (clearWidgetHighlights) ClearWidgetHighlightsInternal();
            vtkRenderer renderer = this.GetCurrentRenderer();
            vtkCamera camera = renderer.GetActiveCamera();
            //
            AdjustCameraDistance(renderer, camera);
            ResetClippingRange();
            // Widgets - Modified
            foreach (vtkMaxBorderWidget widget in _widgets)
            {
                widget.CameraModified();
            }
        }
        private void AdjustCameraDistance(vtkRenderer renderer, vtkCamera camera)
        {
            // Camera should not be to close or to far
            double[] b1 = renderer.ComputeVisiblePropBounds();
            double[] b2 = _selectionRenderer.ComputeVisiblePropBounds();
            double[] b3 = _overlayRenderer.ComputeVisiblePropBounds();
            //
            double[] b = new double[] { Math.Min(b1[0], Math.Min(b2[0], b3[0])), Math.Max(b1[1], Math.Max(b2[1], b3[1])),
                                        Math.Min(b1[2], Math.Min(b2[2], b3[2])), Math.Max(b1[3], Math.Max(b2[3], b3[3])),
                                        Math.Min(b1[4], Math.Min(b2[4], b3[4])), Math.Max(b1[5], Math.Max(b2[5], b3[5]))};
            //
            double r = 0.5 * Math.Sqrt(Math.Pow(b[0] - b[1], 2) + Math.Pow(b[2] - b[3], 2) + Math.Pow(b[4] - b[5], 2)); // radius
            r *= 1.05;
            double[] center = new double[] { (b[0] + b[1]) / 2, (b[2] + b[3]) / 2, (b[4] + b[5]) / 2 };
            //
            double[] pos = camera.GetPosition();
            //
            double[] delta = new double[3]; // vector from camera position to the geometry center
            //
            delta[0] = center[0] - pos[0];
            delta[1] = center[1] - pos[1];
            delta[2] = center[2] - pos[2];
            //
            double[] normal = camera.GetDirectionOfProjection();
            double d = normal[0] * delta[0] + normal[1] * delta[1] + normal[2] * delta[2]; // distance to geometry center
            double k = d - r;
            //
            double[] motionVector = new double[3];
            motionVector[0] = k * normal[0];
            motionVector[1] = k * normal[1];
            motionVector[2] = k * normal[2];
            //
            //System.Diagnostics.Debug.WriteLine(string.Format("pos4 x: {0},  y: {1},  z: {2}", pos[0], pos[1], pos[2]));
            //
            double[] newPos = new double[3];
            newPos[0] = pos[0] + motionVector[0];
            newPos[1] = pos[1] + motionVector[1];
            newPos[2] = pos[2] + motionVector[2];
            camera.SetPosition(newPos[0], newPos[1], newPos[2]);
            //
            //System.Diagnostics.Debug.WriteLine(string.Format("pos5 x: {0},  y: {1},  z: {2}", newPos[0], newPos[1], newPos[2]));
            //
            //double[] focalPos = camera.GetFocalPoint();
            //newPos[0] = focalPos[0] + motionVector[0];
            //newPos[1] = focalPos[1] + motionVector[1];
            //newPos[2] = focalPos[2] + motionVector[2];
            newPos[0] += normal[0];
            newPos[1] += normal[1];
            newPos[2] += normal[2];
            //
            camera.SetFocalPoint(newPos[0], newPos[1], newPos[2]);
        }
        private void AdjustCameraDistance__(vtkRenderer renderer, vtkCamera camera)
        {
            double[] b = renderer.ComputeVisiblePropBounds();
            double l = 0.01 * Math.Sqrt(Math.Pow(b[0] - b[1], 2) + Math.Pow(b[2] - b[3], 2) + Math.Pow(b[4] - b[5], 2));

            double[] pos = camera.GetPosition();
            double[] direction = camera.GetViewPlaneNormal();
            double[] point = null;
            if (direction[0] > 0 && direction[1] > 0 && direction[2] > 0) point = new double[] { b[1], b[3], b[5] };
            else if (direction[0] <= 0 && direction[1] > 0 && direction[2] > 0) point = new double[] { b[0], b[3], b[5] };
            else if (direction[0] <= 0 && direction[1] <= 0 && direction[2] > 0) point = new double[] { b[0], b[2], b[5] };
            else if (direction[0] > 0 && direction[1] <= 0 && direction[2] > 0) point = new double[] { b[1], b[2], b[5] };

            else if (direction[0] > 0 && direction[1] > 0 && direction[2] <= 0) point = new double[] { b[1], b[3], b[4] };
            else if (direction[0] <= 0 && direction[1] > 0 && direction[2] <= 0) point = new double[] { b[0], b[3], b[4] };
            else if (direction[0] <= 0 && direction[1] <= 0 && direction[2] <= 0) point = new double[] { b[0], b[2], b[4] };
            else if (direction[0] > 0 && direction[1] <= 0 && direction[2] <= 0) point = new double[] { b[1], b[2], b[4] };
            else throw new Exception();
            
            double[] relDir = new double[] { point[0] - pos[0], point[1] - pos[1], point[2] - pos[2] };

            // positive value means the camera distance
            double d = -(direction[0] * relDir[0] + direction[1] * relDir[1] + direction[2] * relDir[2]);
            d -= l;

            double[] newPos = new double[3];
            newPos[0] = pos[0] - d * direction[0];
            newPos[1] = pos[1] - d * direction[1];
            newPos[2] = pos[2] - d * direction[2];
            camera.SetPosition(newPos[0], newPos[1], newPos[2]);
        }
        private void AdjustCameraDistance_(vtkRenderer renderer, vtkCamera camera)
        {
            double[] b = renderer.ComputeVisiblePropBounds();
            double[] center = new double[] { (b[0] + b[1]) / 2, (b[2] + b[3]) / 2, (b[4] + b[5]) / 2 };
            double R = Math.Sqrt(Math.Pow(b[0] - b[1], 2) + Math.Pow(b[2] - b[3], 2) + Math.Pow(b[4] - b[5], 2)) / 2;

            double[] pos = camera.GetPosition();
            double[] direction = camera.GetViewPlaneNormal();
            double[] point = new double[] { center[0] + R * direction[0], center[1] + R * direction[1], center[2] + R * direction[2] };
            double[] relDir = new double[] { point[0] - pos[0], point[1] - pos[1], point[2] - pos[2] };

            
            // positive value means the camera will move forward
            double d = -(direction[0] * relDir[0] + direction[1] * relDir[1] + direction[2] * relDir[2]);
            d -= R * 0.01;

            camera.SetDistance(d + 10);

            double[] newPos = new double[3];
            newPos[0] = pos[0] - d * direction[0];
            newPos[1] = pos[1] - d * direction[1];
            newPos[2] = pos[2] - d * direction[2];
            camera.SetPosition(newPos[0], newPos[1], newPos[2]);

            //System.Diagnostics.Debug.WriteLine(string.Format("newPos x: {0},  y: {1},  z: {1}", newPos[0], newPos[1], newPos[2]));
        }

        public void ResetClippingRange()
        {
            double min, max, minSel, maxSel, minOver, maxOver;
            min = max = minSel = maxSel = minOver = maxOver = 0;
            //
            GetCurrentRenderer().ResetCameraClippingRange();
            GetCurrentRenderer().GetActiveCamera().GetClippingRange(ref min, ref max);
            //
            _selectionRenderer.ResetCameraClippingRange();
            _selectionRenderer.GetActiveCamera().GetClippingRange(ref minSel, ref maxSel);
            //
            _overlayRenderer.ResetCameraClippingRange();
            _overlayRenderer.GetActiveCamera().GetClippingRange(ref minOver, ref maxOver);
            // Near and far settng for the Z-buffer
            GetCurrentRenderer().GetActiveCamera().SetClippingRange(Math.Min(min, Math.Min(minSel, minOver)),
                                                                    Math.Max(max, Math.Max(maxSel, maxOver)));
            //System.Diagnostics.Debug.WriteLine("Near: " + Math.Min(min, Math.Min(minSel, minOver)) + "    " +
            //                                   "Far: " + Math.Max(max, Math.Max(maxSel, maxOver)));
        }

        private void InitializeBoxSelection()
        {
            _selectionBackgroundPoints = vtkPoints.New();
            _selectionBackgroundPoints.SetNumberOfPoints(4);
            _selectionBackgroundPoints.SetPoint(0, 0.0, 0.0, 0.0);
            _selectionBackgroundPoints.SetPoint(1, 10.5, 0.0, 0.0);
            _selectionBackgroundPoints.SetPoint(2, 10.5, 10.5, 0.0);
            _selectionBackgroundPoints.SetPoint(3, 0.0, 10.5, 0.0);

            vtkCellArray backgroundPolygon = vtkCellArray.New();
            backgroundPolygon.InsertNextCell(4);
            backgroundPolygon.InsertCellPoint(0);
            backgroundPolygon.InsertCellPoint(1);
            backgroundPolygon.InsertCellPoint(2);
            backgroundPolygon.InsertCellPoint(3);

            _selectionBackgroundPolyData = vtkPolyData.New();
            _selectionBackgroundPolyData.SetPoints(_selectionBackgroundPoints);
            _selectionBackgroundPolyData.SetPolys(backgroundPolygon);

            _selectionBackgroundMapper = vtkPolyDataMapper2D.New();
            _selectionBackgroundMapper.SetInput(_selectionBackgroundPolyData);

            _selectionBackgroundActor = vtkActor2D.New();
            _selectionBackgroundActor.SetMapper(_selectionBackgroundMapper);
            _selectionBackgroundActor.GetProperty().SetOpacity(0.2);
            _selectionBackgroundActor.VisibilityOff();
            _selectionBackgroundActor.GetProperty().SetColor(0.2, 0.5, 1);
            _selectionRenderer.AddActor(_selectionBackgroundActor);

            vtkCellArray borderPolygon = vtkCellArray.New();
            borderPolygon.InsertNextCell(5);
            borderPolygon.InsertCellPoint(0);
            borderPolygon.InsertCellPoint(1);
            borderPolygon.InsertCellPoint(2);
            borderPolygon.InsertCellPoint(3);
            borderPolygon.InsertCellPoint(0);

            vtkPolyData borderPolyData = vtkPolyData.New();
            borderPolyData.SetPoints(_selectionBackgroundPoints);
            borderPolyData.SetLines(borderPolygon);

            vtkPolyDataMapper2D borderMapper = vtkPolyDataMapper2D.New();
            borderMapper.SetInput(borderPolyData);

            _selectionBorderActor = vtkActor2D.New();
            _selectionBorderActor.SetMapper(borderMapper);
            _selectionBorderActor.GetProperty().SetColor(0.3, 0.3, 1.0);
            _selectionBorderActor.VisibilityOff();

            _selectionRenderer.AddActor(_selectionBorderActor);
        }

        private void DrawSphereOnLeft(vtkObject sender, vtkObjectEventArgs e)
        {
            vtkInteractorStyleControl interactor = (vtkInteractorStyleControl)sender;
            vtkRenderer renderer = interactor.GetInteractor().GetRenderWindow().GetRenderers().GetFirstRenderer();

            int[] clickPos = interactor.GetInteractor().GetEventPosition();

            // Pick from this location.
            vtkPropPicker picker = vtkPropPicker.New();

            //vtkRendererCollection renderers = interactor.GetInteractor().GetRenderWindow().GetRenderers();
            //interactor.GetInteractor().GetRenderWindow().RemoveRenderer(renderers.GetNextItem());

            renderer.Render(); // to redraw the first layer of geometry
            picker.Pick(clickPos[0], clickPos[1], 0, renderer);
            


            double[] pos = picker.GetPickPosition();
            Console.WriteLine("Pick position (world coordinates) is: " + pos[0].ToString() + "  "
                                                                       + pos[1].ToString() + "  "
                                                                       + pos[2].ToString());

            //Console.WriteLine("Picked actor: " + picker.GetActor().ToString());

            //Create a sphere
            vtkSphereSource sphereSource = vtkSphereSource.New();
            sphereSource.SetCenter(pos[0], pos[1], pos[2]);
            sphereSource.SetRadius(1);

            //Create a mapper and actor
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(sphereSource.GetOutputPort());

            vtkActor actor = vtkActor.New();
            actor.SetMapper(mapper);
            actor.PickableOff();

            //this.GetInteractor().GetRenderWindow().GetRenderers().GetDefaultRenderer().AddActor(actor);
            renderer.AddActor(actor);

            // Forward events
            interactor.OnLeftButtonUp();

            this.GetInteractor().Render();
        }

    }
}

