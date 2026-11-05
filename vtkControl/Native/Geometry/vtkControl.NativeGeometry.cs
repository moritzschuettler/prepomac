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
using System.Runtime.CompilerServices;
using Kitware.VTK;
using vtkControl.Native;

namespace vtkControl
{
    // The geometry area of the native port: the hooks of the geometry regions send every change to the native view
    // while the area is on. A mirrored call that fails is recorded rather than thrown, so the old path keeps working
    // and the checks report the failure
    public partial class vtkControl
    {
        // Variables
        private readonly ConditionalWeakTable<vtkMaxActor, Tuple<vtkMaxActorData, bool>> _nativeActorData =
            new ConditionalWeakTable<vtkMaxActor, Tuple<vtkMaxActorData, bool>>();
        private readonly HashSet<vtkMaxActor> _nativeMirroredActors = new HashSet<vtkMaxActor>();
        private readonly List<string> _nativeMirrorErrors = new List<string>();
        private bool _nativeMirrorAllGeometry;


        // Methods
        // The parts only mode of the checks keeps every mirrored actor of the geometry area drawn, in every layer,
        // instead of the parts alone
        public void SetNativeMirrorAllGeometry(bool allGeometry)
        {
            _nativeMirrorAllGeometry = allGeometry;
        }
        public string[] GetNativeMirrorErrors()
        {
            return _nativeMirrorErrors.ToArray();
        }
        private bool IsNativeGeometryOn()
        {
            return _nativeMirror != null && (_nativeAreas & VtkNativeArea.Geometry) != 0;
        }
        private void MirrorGeometry(string operation, Action action)
        {
            if (!IsNativeGeometryOn()) return;
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _nativeMirrorErrors.Add(operation + ": " + ex.Message);
            }
        }
        // AddCells and AddPoints: the actor is built natively from the data the ActiViz actor was built from, with
        // the name and the visibility the ActiViz actor ended up with
        private void MirrorActor(vtkMaxActorData data, vtkMaxActor actor, bool nodal)
        {
            if (!IsNativeGeometryOn() || data == null || data.Geometry == null) return;
            // The parts only mode of the pilot checks keeps the native view to the base layer, as it was before
            if (_nativeMirrorHiddenProps != null && !_nativeMirrorAllGeometry && data.Layer != vtkRendererLayer.Base) return;
            _nativeActorData.Remove(actor);
            _nativeActorData.Add(actor, Tuple.Create(data, nodal));
            MirrorGeometry("AddActor " + actor.Name, () =>
            {
                VtkNativePartStyle style = VtkNativePartStyle.FromActorData(data);
                style.Ambient = actor.Ambient;
                style.Diffuse = actor.Diffuse;
                style.Visible = actor.VtkMaxActorVisible ? 1 : 0;
                style.Nodal = nodal ? 1 : 0;
                _nativeMirror.AddActor(actor.Name, actor.ParentName, style,
                                       VtkNativeMeshData.FromPartExchangeData(data.Geometry),
                                       VtkNativeMeshData.FromPartExchangeData(data.ModelEdges),
                                       VtkNativeMeshData.FromPartExchangeData(data.ModelEdgesToDraw));
                _nativeMirroredActors.Add(actor);
                if (data.Layer == vtkRendererLayer.Base && data.IsAPart) _nativeMirrorNames.Add(actor.Name);
            });
        }
        // The symbol methods: the native view builds the symbol from the points and normals of the data, and draws
        // its silhouette edges when this control draws them
        private void MirrorSymbol(vtkMaxActor actor, vtkMaxActorData data, VtkNativeSymbol kind, double symbolSize,
                                  int joint = 0, bool invert = false, bool center = false, double relativeSize = 1)
        {
            if (!IsNativeGeometryOn()) return;
            MirrorSymbol(actor, data, kind, symbolSize, data.Geometry.Nodes.Coor, data.Geometry.Nodes.Normals, joint,
                         invert, center, relativeSize);
        }
        private void MirrorSymbol(vtkMaxActor actor, vtkMaxActorData data, VtkNativeSymbol kind, double symbolSize,
                                  double[][] points, double[][] normals, int joint = 0, bool invert = false,
                                  bool center = false, double relativeSize = 1)
        {
            if (!IsNativeGeometryOn()) return;
            if (_nativeMirrorHiddenProps != null && !_nativeMirrorAllGeometry && data.Layer != vtkRendererLayer.Base) return;
            MirrorGeometry("AddSymbol " + actor.Name, () =>
            {
                List<double> pointValues = new List<double>();
                foreach (double[] point in points ?? new double[0][])
                {
                    if (point != null && point.Length == 3) pointValues.AddRange(point);
                }
                double[] normalValues = null;
                if (kind != VtkNativeSymbol.Sphere && normals != null)
                {
                    normalValues = new double[3 * normals.Length];
                    for (int i = 0; i < normals.Length; i++) Array.Copy(normals[i], 0, normalValues, 3 * i, 3);
                }
                VtkNativePartStyle style = VtkNativePartStyle.FromActorData(data);
                style.Visible = actor.VtkMaxActorVisible ? 1 : 0;
                VtkNativeSymbolParameters parameters = new VtkNativeSymbolParameters();
                parameters.Kind = (int)kind;
                parameters.Joint = joint;
                parameters.SymbolSize = symbolSize;
                parameters.RelativeSize = relativeSize;
                parameters.Invert = invert ? 1 : 0;
                parameters.Center = center ? 1 : 0;
                parameters.DrawEdges = _drawSymbolEdges ? 1 : 0;
                _nativeMirror.AddSymbolActor(actor.Name, style, pointValues.ToArray(), normalValues, parameters);
                _nativeMirroredActors.Add(actor);
                vtkMaxActor edges = FindLayerActor(actor.Name + Globals.NameSeparator + "silhouette", data.Layer);
                if (edges != null) _nativeMirroredActors.Add(edges);
            });
        }
        private vtkMaxActor FindLayerActor(string name, vtkRendererLayer layer)
        {
            vtkMaxActor actor;
            if (layer == vtkRendererLayer.Base && _actors.TryGetValue(name, out actor)) return actor;
            if (layer == vtkRendererLayer.Overlay && _overlayActors.TryGetValue(name, out actor)) return actor;
            if (layer == vtkRendererLayer.Selection)
            {
                foreach (vtkMaxActor selected in _selectedActors) if (selected.Name == name) return selected;
            }
            return null;
        }
        // AddActor of an actor the Controller kept in its selection buffer: it is built again from its data
        private void MirrorBufferedActor(vtkMaxActor actor)
        {
            Tuple<vtkMaxActorData, bool> source;
            if (!IsNativeGeometryOn() || !_nativeActorData.TryGetValue(actor, out source)) return;
            MirrorActor(source.Item1, actor, source.Item2);
        }
        // HighlightActor: the ActiViz actors the highlight added are the last selected ones
        private void MirrorHighlightActor(string actorName, int selectedCount)
        {
            if (!IsNativeGeometryOn()) return;
            MirrorGeometry("HighlightActor", () => _nativeMirror.HighlightActor(actorName));
            for (int i = selectedCount; i < _selectedActors.Count; i++) _nativeMirroredActors.Add(_selectedActors[i]);
        }
        private void MirrorActorVisible(vtkMaxActor actor, bool visible)
        {
            if (actor == null || !_nativeMirroredActors.Contains(actor)) return;
            MirrorGeometry("SetActorVisible " + actor.Name, () =>
                _nativeMirror.SetActorVisible(actor.Name, VtkNativePartStyle.ToNativeLayer(actor.Layer), visible));
        }
        private void MirrorClear(VtkNativeClear what)
        {
            MirrorGeometry("Clear " + what, () => _nativeMirror.Clear(what));
            if (!IsNativeGeometryOn()) return;
            HashSet<vtkMaxActor> drawn = new HashSet<vtkMaxActor>(_actors.Values);
            drawn.UnionWith(_overlayActors.Values);
            drawn.UnionWith(_selectedActors);
            _nativeMirroredActors.IntersectWith(drawn);
            _nativeMirrorNames.RemoveWhere(name => !_actors.ContainsKey(name));
        }
        // The props of the mirrored actors, which the parts only mode of the checks keeps drawn on the ActiViz side
        private HashSet<vtkProp> GetNativeMirroredProps()
        {
            HashSet<vtkProp> props = new HashSet<vtkProp>();
            foreach (vtkMaxActor actor in _nativeMirroredActors)
            {
                if (actor.Geometry != null) props.Add(actor.Geometry);
                if (actor.ElementEdges != null) props.Add(actor.ElementEdges);
                if (actor.ModelEdges != null) props.Add(actor.ModelEdges);
            }
            return props;
        }
    }
}
