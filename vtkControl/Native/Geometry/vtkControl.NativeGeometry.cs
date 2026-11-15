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
using CaeGlobals;
using vtkControl.Native;

namespace vtkControl
{
    // The geometry area: the regions of the geometry keep the actors as vtkMaxActor and the native view draws them. A
    // native call that fails is recorded rather than thrown, so that the control keeps working and the checks report
    // the failure
    public partial class vtkControl
    {
        // Variables
        private readonly HashSet<vtkMaxActor> _nativeActors = new HashSet<vtkMaxActor>();
        private readonly List<string> _nativeMirrorErrors = new List<string>();
        private bool _nativeMirrorAllGeometry;
        private bool _nativePartsOnly;
        private int _nativeTransformIndex;


        // Methods
        // The parts only mode of the checks keeps every actor of the geometry area drawn, in every layer, instead of
        // the base layer alone
        public void SetNativeMirrorAllGeometry(bool allGeometry)
        {
            _nativeMirrorAllGeometry = allGeometry;
        }
        // The parts only mode of the checks: the native view keeps the base layer only
        private void ClearToBaseLayer()
        {
            RunNative("PartsOnly", () =>
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
        private bool IsNativeLayerDrawn(vtkRendererLayer layer)
        {
            return !_nativePartsOnly || _nativeMirrorAllGeometry || layer == vtkRendererLayer.Base;
        }
        private void RunNative(string operation, Action action)
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
        // AddCells, AddPoints and the actors added again: the actor is built natively from the data it was built
        // from, with what changed on it since
        private void AddNativeActor(vtkMaxActor actor)
        {
            vtkMaxActorData data = actor.Data;
            if (data == null || data.Geometry == null || !IsNativeGeometryOn() || !IsNativeLayerDrawn(actor.Layer)) return;
            RunNative("AddActor " + actor.Name, () =>
            {
                _nativeMirror.AddActor(actor.Name, actor.ParentName, actor.GetNativeStyle(),
                                       VtkNativeMeshData.FromPartExchangeData(data.Geometry),
                                       VtkNativeMeshData.FromPartExchangeData(data.ModelEdges),
                                       VtkNativeMeshData.FromPartExchangeData(actor.ModelEdgesToDraw));
                _nativeActors.Add(actor);
                if (actor.Layer == vtkRendererLayer.Base && actor.IsAPart) _nativeMirrorNames.Add(actor.Name);
                SetNativeActorColors(actor);
                double[] offset = actor.ExplodedOffset;
                if (offset[0] != 0 || offset[1] != 0 || offset[2] != 0)
                    _nativeMirror.SetExplodedViewOffsets(new string[] { actor.Name }, new double[][] { offset });
            });
        }
        // The symbol methods: the native view builds the symbol from the points and normals of the data, and draws
        // its silhouette edges when this control draws them
        private void AddNativeSymbol(vtkMaxActor actor, vtkMaxActorData data, VtkNativeSymbol kind, double symbolSize,
                                     int joint = 0, bool invert = false, bool center = false, double relativeSize = 1)
        {
            AddNativeSymbol(actor, data, kind, symbolSize, data.Geometry.Nodes.Coor, data.Geometry.Nodes.Normals, joint,
                            invert, center, relativeSize);
        }
        private void AddNativeSymbol(vtkMaxActor actor, vtkMaxActorData data, VtkNativeSymbol kind, double symbolSize,
                                     double[][] points, double[][] normals, int joint = 0, bool invert = false,
                                     bool center = false, double relativeSize = 1)
        {
            if (!IsNativeLayerDrawn(data.Layer)) return;
            RunNative("AddSymbol " + actor.Name, () =>
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
                _nativeActors.Add(actor);
            });
        }
        // AddVectorFieldActor, AddTensorFieldActor and the animated plots: the line arrows of pointsPerArrow points
        // each, followed by the points of the nodes, which the native view draws unlit in the colors of their values
        private void AddNativeGlyphActor(vtkMaxActor actor, vtkMaxActorData data, float[] coordinates, float[] values,
                                         int arrowCount, int pointsPerArrow, int vertexCount)
        {
            if (!IsNativeLayerDrawn(data.Layer)) return;
            RunNative("AddGlyphActor " + actor.Name, () =>
            {
                int[] lines = new int[8 * arrowCount];
                for (int i = 0; i < arrowCount; i++)
                {
                    int pointId = pointsPerArrow * i;
                    int[] arrow = { pointId, pointId + 1, pointId + 2, pointId + 3, pointId + 3, pointId + 4, pointId + 4,
                                    pointId + 2 };
                    Array.Copy(arrow, 0, lines, 8 * i, arrow.Length);
                }
                int[] vertices = Enumerable.Range(pointsPerArrow * arrowCount, vertexCount).ToArray();
                VtkNativeExtremeNodes? extremes = null;
                NodesExchangeData extremeNodes = data.Geometry.ExtremeNodes;
                if (extremeNodes != null && extremeNodes.Ids != null)
                    extremes = VtkNativeExtremeNodes.FromNodesExchangeData(extremeNodes);
                VtkNativePartStyle style = VtkNativePartStyle.FromActorData(data);
                style.Visible = actor.VtkMaxActorVisible ? 1 : 0;
                _nativeMirror.AddGlyphActor(actor.Name, actor.ParentName, style, coordinates ?? new float[0],
                                            values ?? new float[0], lines, vertices, extremes);
                _nativeActors.Add(actor);
            });
        }
        // AddCaptionActor and AddLabelActor: the text at its scaled size
        private void AddNativeCaption(vtkMaxCaptionActor actor, string caption, double[] position, double[] offset,
                                      double fontScaleFactor, vtkRendererLayer layer)
        {
            if (!IsNativeLayerDrawn(layer)) return;
            RunNative("AddCaption " + actor.Name, () =>
            {
                _nativeMirror.AddCaption(actor.Name, caption, position, offset, (int)(16 * fontScaleFactor),
                                         VtkNativePartStyle.ToNativeLayer(layer), actor.UseSecondaryHighlightColor);
                _nativeActors.Add(actor);
            });
        }
        private void AddNativeLabels(vtkMaxCaptionActor actor, double[][] positions, string[] labels, Color color,
                                     double fontScaleFactor, vtkRendererLayer layer)
        {
            if (!IsNativeLayerDrawn(layer)) return;
            RunNative("AddLabels " + actor.Name, () =>
            {
                _nativeMirror.AddLabels(actor.Name, positions, labels, color, (int)(16 * fontScaleFactor),
                                        VtkNativePartStyle.ToNativeLayer(layer));
                _nativeActors.Add(actor);
            });
        }
        // UpdateMeasurementAnnotation, UpdateOverlayPointsActor and UpdateOverlayCaptionActorPosition: the overlay
        // actors keep their cells and move their points
        private void SetNativeActorPoints(vtkMaxActor actor, double[][] coordinates, double[][] normals)
        {
            if (actor == null || !_nativeActors.Contains(actor)) return;
            RunNative("SetActorPoints " + actor.Name, () =>
                _nativeMirror.SetActorPoints(actor.Name, VtkNativePartStyle.ToNativeLayer(actor.Layer), coordinates, normals));
        }
        private void SetNativeCaptionPosition(vtkMaxActor actor, double[] position)
        {
            if (actor == null || !_nativeActors.Contains(actor)) return;
            RunNative("SetCaptionPosition " + actor.Name, () => _nativeMirror.SetCaptionPosition(actor.Name, position));
        }
        // AddCoordinateSystemSymbol and AddMaterialAxesSymbol
        private void AddNativeCoordinateSystemSymbol(double[] origin, double[][] axisDirections, string[] labels,
                                                     double symbolSize, vtkRendererLayer layer, bool materialAxes)
        {
            if (_nativePartsOnly && !_nativeMirrorAllGeometry) return;
            RunNative("AddCoordinateSystemSymbol", () =>
                _nativeMirror.AddCoordinateSystemSymbol(origin, axisDirections, labels, symbolSize,
                                                        VtkNativePartStyle.ToNativeLayer(layer), materialAxes));
        }
        // UpdateFollowerViewPint and UpdateFollowerViewPlane: the camera moves, its points by the rotation and the
        // translation and its view up by the rotation
        private void MoveNativeCamera(double[][] rotation, double[] translation)
        {
            RunNative("FollowerView", () =>
            {
                double[] camera = _nativeMirror.GetCamera();
                double[] moved = new double[10];
                for (int point = 0; point < 3; point++)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        double value = point < 2 ? translation[i] : 0;
                        for (int j = 0; j < 3; j++) value += rotation[i][j] * camera[3 * point + j];
                        moved[3 * point + i] = value;
                    }
                }
                moved[9] = camera[9];
                _nativeMirror.SetViewParameters(moved);
            });
        }
        // PreviewExplodedView and RemovePreviewedExplodedView: the positions the parts have reached
        private void SetNativeExplodedViewOffsets(IEnumerable<string> partNames)
        {
            RunNative("PreviewExplodedView", () =>
            {
                List<string> names = new List<string>();
                List<double[]> positions = new List<double[]>();
                foreach (string partName in partNames)
                {
                    vtkMaxActor actor;
                    if (!_actors.TryGetValue(partName, out actor)) continue;
                    names.Add(partName);
                    positions.Add(actor.ExplodedOffset);
                }
                _nativeMirror.SetExplodedViewOffsets(names.ToArray(), positions.ToArray());
            });
        }
        private static VtkNativeExtremeNodes? GetNativeExtremes(vtkMaxExtreemeNode minNode, vtkMaxExtreemeNode maxNode)
        {
            if (minNode == null || maxNode == null || minNode.Coor == null || maxNode.Coor == null) return null;
            NodesExchangeData extremeNodes = new NodesExchangeData();
            extremeNodes.Ids = new int[] { minNode.Id, maxNode.Id };
            extremeNodes.Coor = new double[][] { minNode.Coor, maxNode.Coor };
            extremeNodes.Values = new float[] { minNode.Value, maxNode.Value };
            return VtkNativeExtremeNodes.FromNodesExchangeData(extremeNodes);
        }
        // The result values, extreme nodes, cell values and color table of an actor
        private void SetNativeActorColors(vtkMaxActor actor)
        {
            PartExchangeData geometry = actor.Data.Geometry;
            if (actor.Layer == vtkRendererLayer.Base)
            {
                if (actor.Nodal && geometry.Nodes.Values != null)
                {
                    VtkNativeExtremeNodes? extremes = null;
                    if (geometry.ExtremeNodes != null && geometry.ExtremeNodes.Ids != null)
                        extremes = VtkNativeExtremeNodes.FromNodesExchangeData(geometry.ExtremeNodes);
                    _nativeMirror.SetActorScalars(actor.Name, geometry.Nodes.Values, extremes);
                }
                else if (actor.Values != null)
                    _nativeMirror.SetActorScalars(actor.Name, actor.Values, GetNativeExtremes(actor.MinNode, actor.MaxNode));
            }
            if ((geometry.Cells.Values != null && geometry.Cells.Ids != null) || actor.ColorTable != null)
            {
                float[] cellValues = geometry.Cells.Ids != null ? geometry.Cells.Values : null;
                _nativeMirror.SetActorCellColors(actor.NativeName, VtkNativePartStyle.ToNativeLayer(actor.Layer), cellValues,
                                                 actor.ColorTable);
            }
        }
        // UpdateScalarFormatting: the native view colors its result actors from its own extreme nodes
        private void UpdateNativeScalarFormatting()
        {
            RunNative("UpdateScalarFormatting", () =>
            {
                bool useAllFrames = _animationFrameData != null && _animationFrameData.UseAllFrameData;
                double[] range = useAllFrames ? _animationFrameData.AllFramesScalarRange : new double[] { 0, 0 };
                _nativeMirror.UpdateScalarFormatting(VtkNativeColorSpectrum.FromColorSpectrum(_colorSpectrum),
                                                     _keepScalarBarVisible, useAllFrames, range[0], range[1]);
            });
        }
        private void SetNativeActorScalarField(vtkMaxActor actor, float[] values, NodesExchangeData extremeNodes)
        {
            RunNative("UpdateActorScalarField " + actor.Name, () =>
            {
                VtkNativeExtremeNodes? extremes = null;
                if (values != null && extremeNodes != null && extremeNodes.Ids != null)
                    extremes = VtkNativeExtremeNodes.FromNodesExchangeData(extremeNodes);
                _nativeMirror.SetActorScalars(actor.Name, values, extremes);
                // The transformed copies have the values too, transformed for a directional field
                SetNativeCopyScalarFields(actor, values == null);
            });
        }
        private void SetNativeCopyScalarFields(vtkMaxActor actor, bool removed)
        {
            foreach (vtkMaxActor copy in actor.Copies)
            {
                if (!_nativeActors.Contains(copy)) continue;
                if (removed) _nativeMirror.SetActorScalars(copy.NativeName, null, null);
                else SetNativeCopyResultValues(copy);
                SetNativeCopyScalarFields(copy, removed);
            }
        }
        private void SetNativeCopyResultValues(vtkMaxActor copy)
        {
            if (copy.Values == null) return;
            _nativeMirror.SetActorScalars(copy.NativeName, copy.Values, GetNativeExtremes(copy.MinNode, copy.MaxNode));
        }
        // CreateSectionView and UpdateSectionView: every part that may be cut gets its volume grid, the color of its
        // section faces and its hatch index; the parts and surfaces the element-wise section replaces by skins are
        // read back
        private void CreateNativeSectionView(double[] point, double[] normal)
        {
            RunNative("SectionView", () =>
            {
                Dictionary<string, int> hatchIndices = new Dictionary<string, int>();
                foreach (vtkMaxActor actor in _actors.Values.ToArray())
                {
                    if (!actor.SectionViewPossible || !actor.VolumeGridAvailable || !_nativeActors.Contains(actor)) continue;
                    PartExchangeData data = GetVolumeGridData(actor.VolumeGridSourceName ?? actor.Name);
                    if (data == null || data.Cells == null || data.Cells.CellNodeIds == null) continue;
                    //
                    Color color = actor.Color;
                    if (_sectionViewData.LightenColors) color = color.Lighten();
                    else if (_sectionViewData.SectionColor != Color.Empty) color = _sectionViewData.SectionColor;
                    string hatchGroup = Controller_GetSectionViewHatchGroup?.Invoke(actor.Name) ?? actor.Name;
                    int hatchIndex;
                    if (!hatchIndices.TryGetValue(hatchGroup, out hatchIndex))
                    {
                        hatchIndex = hatchIndices.Count;
                        hatchIndices.Add(hatchGroup, hatchIndex);
                    }
                    _nativeMirror.SetSectionViewData(actor.NativeName, VtkNativeMeshData.FromPartExchangeData(data),
                                                     data.Nodes.Values, color, hatchIndex, actor.HasVolumeCells);
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
                //
                HashSet<string> hidden = new HashSet<string>(_nativeMirror.GetSectionViewHiddenActors());
                foreach (vtkMaxActor actor in _actors.Values) actor.GeometryHiddenBySectionView = hidden.Contains(actor.NativeName);
            });
        }
        // TransformAllMaxActors: a copy is named natively after the actor it starts from, the transformation and its
        // place in the row
        private void NameNativeCopy(vtkMaxActor copy, vtkMaxActor source, int item)
        {
            copy.NativeName = source.NativeName + ":t" + _nativeTransformIndex + "-" + item;
        }
        // ApplyTransforms: the copies of native actors are native, and a copy of a directional result gets the values
        // ApplyResultFieldTransformation computed for it
        private void ApplyNativeTransforms()
        {
            RunNative("ApplyTransforms", () =>
            {
                _nativeMirror.ApplyTransforms();
                bool added = true;
                while (added)
                {
                    added = false;
                    foreach (vtkMaxActor actor in _nativeActors.ToArray())
                    {
                        foreach (vtkMaxActor copy in actor.Copies)
                        {
                            if (!_nativeActors.Add(copy)) continue;
                            added = true;
                            if (copy.HasTransformedResultField) SetNativeCopyResultValues(copy);
                        }
                    }
                }
            });
        }
        // InitializeAndAddAnimatedActor: the actor and its frames, before vtkMaxActor.InitializeAnimation releases
        // the frames it took
        private void AddNativeAnimatedActor(vtkMaxActorData data, vtkMaxActor actor)
        {
            if (!IsNativeGeometryOn() || data.Geometry.NodesAnimation == null) return;
            if (!_nativeActors.Contains(actor)) AddNativeActor(actor);
            if (!_nativeActors.Contains(actor)) return;
            RunNative("AddAnimationFrames " + actor.Name, () =>
            {
                for (int i = 0; i < data.Geometry.NodesAnimation.Length; i++)
                {
                    NodesExchangeData nodes = data.Geometry.NodesAnimation[i];
                    NodesExchangeData modelEdgesNodes = data.ModelEdges != null && data.ModelEdges.NodesAnimation != null ?
                                                        data.ModelEdges.NodesAnimation[i] : null;
                    NodesExchangeData extremeNodes = data.Geometry.ExtremeNodesAnimation != null ?
                                                     data.Geometry.ExtremeNodesAnimation[i] : null;
                    VtkNativeExtremeNodes? extremes = null;
                    if (extremeNodes != null && extremeNodes.Ids != null)
                        extremes = VtkNativeExtremeNodes.FromNodesExchangeData(extremeNodes);
                    _nativeMirror.AddAnimationFrame(actor.Name, GetFloatCoordinates(nodes), nodes?.Values, extremes,
                                                    GetFloatCoordinates(modelEdgesNodes));
                }
            });
        }
        private static float[] GetFloatCoordinates(NodesExchangeData nodes)
        {
            if (nodes == null) return null;
            if (nodes.FlatCoor != null) return nodes.FlatCoor;
            if (nodes.Coor == null) return null;
            float[] coordinates = new float[3 * nodes.Coor.Length];
            for (int i = 0; i < nodes.Coor.Length; i++)
            {
                coordinates[3 * i] = (float)nodes.Coor[i][0];
                coordinates[3 * i + 1] = (float)nodes.Coor[i][1];
                coordinates[3 * i + 2] = (float)nodes.Coor[i][2];
            }
            return coordinates;
        }
        // UpdateActor: a new name of an actor
        private void RenameNativeActor(vtkMaxActor actor, string oldName, string newName)
        {
            if (actor == null || oldName == newName) return;
            bool part = _nativeMirrorNames != null && _nativeMirrorNames.Remove(oldName);
            if (part) _nativeMirrorNames.Add(newName);
            if (!part && !_nativeActors.Contains(actor)) return;
            RunNative("RenameActor " + oldName, () => _nativeMirror.RenameActor(oldName, newName));
        }
        // UpdateActor: the color and the opacity of an actor
        private void SetNativeActorColor(vtkMaxActor actor, string nativeName, Color color)
        {
            if (actor == null || !_nativeActors.Contains(actor)) return;
            RunNative("SetActorColor " + nativeName, () =>
                _nativeMirror.SetActorColor(nativeName, VtkNativePartStyle.ToNativeLayer(actor.Layer), color));
        }
        private void SetNativeActorVisible(vtkMaxActor actor, bool visible)
        {
            if (actor == null || !_nativeActors.Contains(actor)) return;
            RunNative("SetActorVisible " + actor.Name, () =>
                _nativeMirror.SetActorVisible(actor.Name, VtkNativePartStyle.ToNativeLayer(actor.Layer), visible));
        }
        private void ClearNative(VtkNativeClear what)
        {
            RunNative("Clear " + what, () => _nativeMirror.Clear(what));
            HashSet<vtkMaxActor> drawn = new HashSet<vtkMaxActor>(_actors.Values);
            drawn.UnionWith(_overlayActors.Values);
            drawn.UnionWith(_selectedActors);
            foreach (vtkMaxActor actor in _actors.Values) drawn.UnionWith(actor.Copies);
            _nativeActors.IntersectWith(drawn);
            if (_nativeMirrorNames != null) _nativeMirrorNames.RemoveWhere(name => !_actors.ContainsKey(name));
        }
    }
}
