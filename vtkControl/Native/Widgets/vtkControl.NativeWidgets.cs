// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using vtkControl.Native;

namespace vtkControl
{
    // The widgets area of the native view: every widget of this control is drawn by a native twin in the native view
    // while the area is on. The twin receives the state the widget keeps, in the order the control set it up, and
    // from then on every call the widget takes; its events end in the handlers of this control
    public partial class vtkControl
    {
        // Variables
        public const string NativeScaleBarName = "ScaleBar";
        public const string NativeColorBarName = "ColorBar";
        public const string NativeStatusBlockName = "StatusBlock";
        public const string NativeProbeName = "Probe";
        public const string NativeTriadName = "Triad";
        //
        private Dictionary<string, VtkNativeWidget> _nativeMirrorWidgets;
        private Dictionary<string, vtkMaxBorderWidget> _nativeMirroredWidgets;
        private bool _scaleBarPositionResetPending;


        // Methods
        // The native twin of a widget: one of the names above or the name of a text with arrow widget
        public VtkNativeWidget GetNativeMirrorWidget(string name)
        {
            VtkNativeWidget widget;
            if (_nativeMirrorWidgets == null || !_nativeMirrorWidgets.TryGetValue(name, out widget)) return null;
            return widget;
        }
        public string[] GetNativeMirrorWidgetNames()
        {
            return _nativeMirrorWidgets == null ? new string[0] : _nativeMirrorWidgets.Keys.ToArray();
        }
        private void CreateNativeMirrorWidgets()
        {
            _nativeMirrorWidgets = new Dictionary<string, VtkNativeWidget>();
            _nativeMirroredWidgets = new Dictionary<string, vtkMaxBorderWidget>();
            MirrorWidgetHighlight();
            MirrorInteractionSettings();
            _nativeMirror.NativeEvent += NativeMirror_InteractionEvent;
            MirrorWidget(NativeScaleBarName, _scaleWidget, VtkNativeWidgetKind.ScaleBar);
            if (_scaleBarPositionResetPending) ResetScaleBarPosition();
            VtkNativeWidget colorBar = MirrorWidget(NativeColorBarName, _colorBarWidget, VtkNativeWidgetKind.ColorBar);
            colorBar.MouseDoubleClick += colorBarWidget_DoubleClicked;
            VtkNativeWidget statusBlock = MirrorWidget(NativeStatusBlockName, _statusBlockWidget,
                                                       VtkNativeWidgetKind.StatusBlock);
            statusBlock.MouseDoubleClick += statusBlockWidget_DoubleClicked;
            MirrorWidget(NativeProbeName, _probeWidget, VtkNativeWidgetKind.Text);
            VtkNativeWidget triad = MirrorWidget(NativeTriadName, _coorSysWidget, VtkNativeWidgetKind.Triad);
            triad.TargetClicked += NativeMirrorTriad_TargetClicked;
            foreach (var entry in _arrowWidgets) MirrorArrowWidget(entry.Key, entry.Value);
        }
        private VtkNativeWidget MirrorWidget(string name, vtkMaxBorderWidget widget, VtkNativeWidgetKind kind)
        {
            VtkNativeWidget nativeWidget = new VtkNativeWidget(_nativeMirror, kind);
            widget.SetNativeMirror(nativeWidget);
            _nativeMirrorWidgets[name] = nativeWidget;
            _nativeMirroredWidgets[name] = widget;
            return nativeWidget;
        }
        private void MirrorArrowWidget(string name, vtkMaxTextWithArrowWidget widget)
        {
            if (_nativeMirrorWidgets == null) return;
            VtkNativeWidget nativeWidget = MirrorWidget(name, widget, VtkNativeWidgetKind.TextWithArrow);
            nativeWidget.MouseDoubleClick += (sender, e) => arrowWidget_DoubleClicked(widget, e);
        }
        private void UnmirrorWidget(string name)
        {
            if (_nativeMirrorWidgets == null) return;
            VtkNativeWidget nativeWidget;
            if (_nativeMirrorWidgets.TryGetValue(name, out nativeWidget))
            {
                _nativeMirroredWidgets[name].SetNativeMirror(null);
                nativeWidget.Dispose();
                _nativeMirrorWidgets.Remove(name);
                _nativeMirroredWidgets.Remove(name);
            }
        }
        private void DisposeNativeMirrorWidgets()
        {
            if (_nativeMirrorWidgets == null) return;
            if (_nativeMirror != null) _nativeMirror.NativeEvent -= NativeMirror_InteractionEvent;
            foreach (string name in _nativeMirrorWidgets.Keys.ToArray()) UnmirrorWidget(name);
            _nativeMirrorWidgets = null;
            _nativeMirroredWidgets = null;
        }
        private void MirrorWidgetHighlight()
        {
            if (_nativeMirror == null || (_nativeAreas & VtkNativeArea.Widgets) == 0) return;
            _nativeMirror.SetWidgetHighlight(_widgetHighlightEnabled, _widgetInteractionEnabled);
        }
        // The mouse settings of the style
        private void MirrorInteractionSettings()
        {
            if (_nativeMirror == null || (_nativeAreas & VtkNativeArea.Widgets) == 0) return;
            VtkNativeInteractionSettings settings = new VtkNativeInteractionSettings();
            settings.RotateSensitivity = _rotateSensitivity;
            settings.ZoomSensitivity = _zoomSensitivity;
            settings.InvertZoom = _invertZoom ? 1 : 0;
            settings.SmoothZoom = _smoothZoom ? 1 : 0;
            settings.SelectionBoxEnabled = _selectionBoxEnabled ? 1 : 0;
            settings.SelectionDistance = _selectionDistance;
            _nativeMirror.SetInteractionSettings(settings);
        }
        // AnimateCamera: the native style takes no input while the camera of this control is animated, as
        // vtkInteractorStyleControl.Animating
        private void MirrorStyleAnimating(bool animating)
        {
            if (_nativeMirror == null || (_nativeAreas & VtkNativeArea.Widgets) == 0) return;
            _nativeMirror.Navigate(VtkNativeNavigation.SetAnimating, animating ? 1 : 0);
        }
        // In the window the box zoom of the native style ends in SetBoxZoom of this control, which animates it
        private void MirrorOwnerBoxZoom()
        {
            if (_nativeMirror == null || (_nativeAreas & VtkNativeArea.Widgets) == 0) return;
            _nativeMirror.Navigate(VtkNativeNavigation.SetOwnerBoxZoom, 1);
        }
        // StartBoxZoom: the next left drag of the native style zooms to its box
        private void MirrorStartBoxZoom()
        {
            if (_nativeMirror == null || (_nativeAreas & VtkNativeArea.Widgets) == 0) return;
            _nativeMirror.Navigate(VtkNativeNavigation.SetBoxZoom, 1);
        }
        // The touchpad and the trackpad move the native style
        private void MirrorTrackpadGesture(double dragX, double dragY, double zoom)
        {
            if (_nativeMirror == null || (_nativeAreas & VtkNativeArea.Widgets) == 0) return;
            _trackpadNavigation.Apply(new VtkNativeNavigationTarget(_nativeMirror), dragX, dragY, zoom,
                                      _trackpadCenterX, _trackpadCenterY, _trackpadShift);
        }
        private void MirrorEndTrackpadGesture()
        {
            if (_nativeMirror == null || (_nativeAreas & VtkNativeArea.Widgets) == 0) return;
            new VtkNativeNavigationTarget(_nativeMirror).EndMultiTouchGesture();
        }
        // The widget a native widget id belongs to: the scalar bar or the widget of one of the twins
        private object GetMirroredWidget(int widgetId)
        {
            if (widgetId == 0) return null;
            if (_nativeMirrorScalarBar != null && _nativeMirrorScalarBar.WidgetId == widgetId)
                return _nativeMirrorScalarBar;
            foreach (var entry in _nativeMirrorWidgets)
            {
                if (entry.Value.WidgetId == widgetId) return _nativeMirroredWidgets[entry.Key];
            }
            return null;
        }
        // The events of the native style end where the events of vtkInteractorStyleControl end
        private void NativeMirror_InteractionEvent(object sender, VtkNativeEventArgs e)
        {
            switch ((int)e.Event)
            {
                case 22:
                    if (e.Values.Length >= 6)
                        PickWithNativeMirror((int)e.Values[4], (int)e.Values[5], e.Values[3] != 0, (int)e.Values[0],
                                             (int)e.Values[1], (int)e.Values[2]);
                    break;
                case 23:
                    if (e.Values.Length >= 5)
                        HoverWithNativeMirror((int)e.Values[0], (int)e.Values[1], e.Values[2] != 0, (int)e.Values[3],
                                              (int)e.Values[4]);
                    break;
                case 24:
                    style_RightButtonPressEvent(GetMirroredWidget((int)e.Values[2]),
                                                new MouseEventArgs(MouseButtons.Right, 1, (int)e.Values[0],
                                                                   (int)e.Values[1], 0));
                    break;
                case 25:
                    RotationPointChanged?.Invoke(new double[] { e.Values[0], e.Values[1], e.Values[2] });
                    break;
                case 27:
                    Form_CommitViewportEdit?.Invoke();
                    break;
                case 28:
                    ClearNativeMouseSelection();
                    break;
                case 29:
                    BoxZoomEnded();
                    break;
                case 30:
                    if (_nativeMirror.GetInteractionState()[0] == VtkNativeView.InteractionStateRotate &&
                        _probeWidget != null && _probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                    break;
                case 32:
                    if (e.Values.Length >= 4)
                        SetBoxZoom(new int[] { (int)e.Values[0], (int)e.Values[1] },
                                   new int[] { (int)e.Values[2], (int)e.Values[3] }, true);
                    break;
                case 31:
                    if (!_widgetHighlightLocked) _nativeMirror.SetWidgetHighlight(_widgetHighlightEnabled,
                                                                                  _widgetInteractionEnabled);
                    break;
            }
        }
        // The parts only mode of the mirror draws no widget layer; shown again, each twin takes the visibility of its
        // widget
        private void ShowNativeMirrorWidgets(bool show)
        {
            if (_nativeMirrorWidgets == null) return;
            foreach (var entry in _nativeMirrorWidgets)
                entry.Value.SetVisibility(show && _nativeMirroredWidgets[entry.Key].GetVisibility() == 1);
        }
        // coorSysWidget_TargetClicked on the native view
        private void NativeMirrorTriad_TargetClicked(VtkNativeTriadTarget target)
        {
            if (_nativeMirror == null) return;
            // The views of this control animate the camera and hand each frame to the native view
            coorSysWidget_TargetClicked((vtkMaxCoorSysTarget)(int)target);
        }
        // Shows only the widgets: the native view drops its parts, which the next redraw sends again; the camera is
        // kept, since the scale bar, the triad and the arrows follow it
        public void SetNativeMirrorWidgetsOnly(bool widgetsOnly)
        {
            if (widgetsOnly && _nativeMirror != null)
            {
                _nativeMirror.ClearActors();
                if (_nativeMirrorNames != null) _nativeMirrorNames.Clear();
            }
            RenderScene();
        }
    }
}
