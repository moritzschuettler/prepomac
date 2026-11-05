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
using System.Runtime.CompilerServices;
using CaeGlobals;
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
        // The parts only mode of the pilot checks: the native view keeps the base layer only, as ActiViz then draws
        private void MirrorPartsOnly()
        {
            MirrorGeometry("PartsOnly", () =>
            {
                _nativeMirror.Clear(VtkNativeClear.Overlay);
                _nativeMirror.Clear(VtkNativeClear.Selection);
            });
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
                MirrorActorColors(data, actor.Name);
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
        // The result values, extreme nodes, cell values and color table vtkMaxActor takes from its data
        private void MirrorActorColors(vtkMaxActorData data, string name)
        {
            PartExchangeData geometry = data.Geometry;
            if (geometry.Nodes.Values != null && data.Layer == vtkRendererLayer.Base)
            {
                VtkNativeExtremeNodes? extremes = null;
                if (geometry.ExtremeNodes != null && geometry.ExtremeNodes.Ids != null)
                    extremes = VtkNativeExtremeNodes.FromNodesExchangeData(geometry.ExtremeNodes);
                _nativeMirror.SetActorScalars(name, geometry.Nodes.Values, extremes);
            }
            if ((geometry.Cells.Values != null && geometry.Cells.Ids != null) || data.ColorTable != null)
            {
                float[] cellValues = geometry.Cells.Ids != null ? geometry.Cells.Values : null;
                _nativeMirror.SetActorCellColors(name, VtkNativePartStyle.ToNativeLayer(data.Layer), cellValues,
                                                 data.ColorTable);
            }
        }
        // UpdateScalarFormatting: the native view colors its result actors from its own extreme nodes
        private void MirrorScalarFormatting()
        {
            MirrorGeometry("UpdateScalarFormatting", () =>
            {
                bool useAllFrames = _animationFrameData != null && _animationFrameData.UseAllFrameData;
                double[] range = useAllFrames ? _animationFrameData.AllFramesScalarRange : new double[] { 0, 0 };
                _nativeMirror.UpdateScalarFormatting(VtkNativeColorSpectrum.FromColorSpectrum(_colorSpectrum),
                                                     _keepScalarBarVisible, useAllFrames, range[0], range[1]);
            });
        }
        private void MirrorActorScalarField(string actorName, float[] values, NodesExchangeData extremeNodes)
        {
            MirrorGeometry("UpdateActorScalarField " + actorName, () =>
            {
                VtkNativeExtremeNodes? extremes = null;
                if (values != null && extremeNodes != null && extremeNodes.Ids != null)
                    extremes = VtkNativeExtremeNodes.FromNodesExchangeData(extremeNodes);
                _nativeMirror.SetActorScalars(actorName, values, extremes);
            });
        }
        // CreateSectionView and UpdateSectionView: every mirrored part that may be cut gets its volume grid, the
        // color of its section faces (formatted here, as Lighten is a .NET extension) and its hatch index
        private void MirrorSectionView(double[] point, double[] normal)
        {
            MirrorGeometry("SectionView", () =>
            {
                Dictionary<string, int> hatchIndices = new Dictionary<string, int>();
                foreach (vtkMaxActor actor in _actors.Values.ToArray())
                {
                    if (!actor.SectionViewPossible || !actor.VolumeGridAvailable ||
                        !_nativeMirroredActors.Contains(actor)) continue;
                    PartExchangeData data = GetVolumeGridData(actor.VolumeGridSourceName ?? actor.Name);
                    if (data == null || data.Cells == null || data.Cells.CellNodeIds == null) continue;
                    //
                    System.Drawing.Color color = actor.Color;
                    if (_sectionViewData.LightenColors) color = color.Lighten();
                    else if (_sectionViewData.SectionColor != System.Drawing.Color.Empty) color = _sectionViewData.SectionColor;
                    string hatchGroup = Controller_GetSectionViewHatchGroup?.Invoke(actor.Name) ?? actor.Name;
                    int hatchIndex;
                    if (!hatchIndices.TryGetValue(hatchGroup, out hatchIndex))
                    {
                        hatchIndex = hatchIndices.Count;
                        hatchIndices.Add(hatchGroup, hatchIndex);
                    }
                    _nativeMirror.SetSectionViewData(actor.Name, VtkNativeMeshData.FromPartExchangeData(data),
                                                     data.Nodes.Values, color, hatchIndex, actor.HasVolumeCells);
                    if (actor.SectionViewActor != null) _nativeMirroredActors.Add(actor.SectionViewActor);
                }
                VtkNativeSectionView sectionView = new VtkNativeSectionView();
                sectionView.PointX = point[0];
                sectionView.PointY = point[1];
                sectionView.PointZ = point[2];
                sectionView.NormalX = normal[0];
                sectionView.NormalY = normal[1];
                sectionView.NormalZ = normal[2];
                sectionView.ElementWise = _sectionViewData.ElementWise ? 1 : 0;
                sectionView.Hatch = _sectionViewData.Hatch ? 1 : 0;
                sectionView.HatchSpacing = _sectionViewData.HatchSpacing;
                _nativeMirror.CreateSectionView(sectionView);
            });
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
                if (actor.SectionViewHatch != null) props.Add(actor.SectionViewHatch);
            }
            return props;
        }
    }
}
