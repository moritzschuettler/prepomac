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
using System.Linq;
using System.Windows.Forms;
using Kitware.VTK;
using vtkControl.Native;

namespace vtkControl
{
    // The widgets area of the native port: every widget of this control has a native twin in the native view while
    // the area is on. The twin receives the state of the widget, in the order the control set it up, and from then
    // on every call the widget takes; its events end in the handlers of the widget it ports
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
        private List<vtkProp> _nativeMirrorWidgetsHiddenProps;


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
            MirrorWidget(NativeScaleBarName, _scaleWidget, VtkNativeWidgetKind.ScaleBar);
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
            nativeWidget.MouseDoubleClick += (sender, e) =>
            {
                if (_widgetInteractionEnabled) Form_WidgetPicked(e, Keys.None, name, nativeWidget.GetRectangle());
            };
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
            foreach (string name in _nativeMirrorWidgets.Keys.ToArray()) UnmirrorWidget(name);
            _nativeMirrorWidgets = null;
            _nativeMirroredWidgets = null;
        }
        private void MirrorWidgetHighlight()
        {
            if (_nativeMirror == null || (_nativeAreas & VtkNativeArea.Widgets) == 0) return;
            _nativeMirror.SetWidgetHighlight(_widgetHighlightEnabled, _widgetInteractionEnabled);
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
            double[][] axes = new double[][] { new double[] { 1, 0, 0 }, new double[] { -1, 0, 0 },
                                               new double[] { 0, 1, 0 }, new double[] { 0, -1, 0 },
                                               new double[] { 0, 0, 1 }, new double[] { 0, 0, -1 } };
            if (target == VtkNativeTriadTarget.Origin) _nativeMirror.SetStandardView(VtkNativeOrientation.Isometric, false);
            else if (target >= VtkNativeTriadTarget.XPositiveVertical)
                _nativeMirror.SetVerticalAxisView(axes[target - VtkNativeTriadTarget.XPositiveVertical], false);
            else if (target >= VtkNativeTriadTarget.XPositive)
                _nativeMirror.SetAxisView(axes[target - VtkNativeTriadTarget.XPositive], false);
        }
        // Shows only the widgets on both paths: the props of the 3D layers of this control are hidden and the native
        // view drops its parts; the camera is kept, since the scale bar, the triad and the arrows follow it
        public void SetNativeMirrorWidgetsOnly(bool widgetsOnly)
        {
            if (widgetsOnly && _nativeMirrorWidgetsHiddenProps == null)
            {
                _nativeMirrorWidgetsHiddenProps = new List<vtkProp>();
                foreach (vtkRenderer renderer in new vtkRenderer[] { _renderer, _overlayRenderer, _selectionRenderer })
                {
                    vtkPropCollection props = renderer.GetViewProps();
                    props.InitTraversal();
                    for (vtkProp prop = props.GetNextProp(); prop != null; prop = props.GetNextProp())
                    {
                        if (prop.GetVisibility() == 0) continue;
                        prop.VisibilityOff();
                        _nativeMirrorWidgetsHiddenProps.Add(prop);
                    }
                }
                if (_nativeMirror != null)
                {
                    _nativeMirror.ClearActors();
                    if (_nativeMirrorNames != null) _nativeMirrorNames.Clear();
                }
            }
            else if (!widgetsOnly && _nativeMirrorWidgetsHiddenProps != null)
            {
                foreach (vtkProp prop in _nativeMirrorWidgetsHiddenProps) prop.VisibilityOn();
                _nativeMirrorWidgetsHiddenProps = null;
            }
            RenderScene();
        }
    }
}
