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
using CaeGlobals;
using CaeMesh;
using vtkControl.Native;

namespace vtkControl
{
    // The id resolution of the Selection region through the native view: the helpers the Controller resolves the
    // picks with (GetGeometryPickProperties, GetPointAndCellIdsInsideFrustum, GetClosestCellsOnActors,
    // GetSelectionPrecision) take their cells, distances and frustum ids from it when the selection area is on
    public partial class vtkControl
    {
        // Variables
        private bool _nativeIdResolution = true;
        private Dictionary<string, Tuple<VtkNativeView, vtkMaxActor, int, PartExchangeData>> _nativeVolumeGrids;


        // Properties
        // On by default; the checks switch it off to resolve the picks of the ActiViz path the old way
        public bool NativeIdResolution { get { return _nativeIdResolution; } set { _nativeIdResolution = value; } }
        private bool UseNativeIdResolution
        {
            get
            {
                return _nativeIdResolution && _nativeMirror != null && (_nativeAreas & VtkNativeArea.Selection) != 0;
            }
        }


        // Methods
        private string[] GetNativeSelectableParts()
        {
            if (_selectableActorsFilter == null) return null;
            string[] selectableParts = new string[_selectableActorsFilter.Count];
            _selectableActorsFilter.CopyTo(selectableParts);
            return selectableParts;
        }
        // GetGlobalCellIdClosestTo3DPoint over the visible parts: a pick by the user is limited to the selection
        // precision, a regeneration takes the closest cell
        private int NativeGetClosestCell(ref double[] point, out int[] cellFaceNodeIds, out double[][] cellFaceNodeCoor)
        {
            double maxDistance = _renderingOn && _userPick ? _nativeMirror.GetSelectionPrecision(_selectionDistance) : -1;
            return _nativeMirror.GetClosestCell(ref point, maxDistance, GetNativeSelectableParts(), out cellFaceNodeIds,
                                                out cellFaceNodeCoor);
        }
        private void NativeGetGeometryPickProperties(double[] point, out int elementId, out int[] edgeNodeIds,
                                                     out int[] cellFaceNodeIds)
        {
            elementId = NativeGetClosestCell(ref point, out cellFaceNodeIds, out _);
            if (cellFaceNodeIds == null)
            {
                edgeNodeIds = null;
                return;
            }
            edgeNodeIds = GetEdgeNodeIds(point, elementId, cellFaceNodeIds);
        }
        private int[] GetEdgeNodeIds(double[] point, int globalCellId, int[] globalCellFaceNodeIds)
        {
            vtkMaxActorData cellFaceData = Controller_GetCellFaceActorData(globalCellId, globalCellFaceNodeIds);
            int[] nodeIds;
            GetClosestEdgeCell(point, cellFaceData, out nodeIds, out _, out _, out _);
            return nodeIds;
        }
        // GetNodeIdOnCellFaceClosestToPoint: the node of the closest surface cell nearest to the point
        private int NativeGetNodeIdOnCellFaceClosestToPoint(double[] point)
        {
            int[] nodeIds;
            double[][] coor;
            NativeGetClosestCell(ref point, out nodeIds, out coor);
            double distance;
            double minDistance = double.MaxValue;
            int minId = -1;
            for (int i = 0; i < coor.Length; i++)
            {
                distance = (coor[i][0] - point[0]) * (coor[i][0] - point[0]) +
                           (coor[i][1] - point[1]) * (coor[i][1] - point[1]) +
                           (coor[i][2] - point[2]) * (coor[i][2] - point[2]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    minId = i;
                }
            }
            return nodeIds[minId];
        }
        // GetPointAndCellIdsInsideFrustum with the bounds and the frustum extraction of the native view
        private void NativeGetPointAndCellIdsInsideFrustum(double[][] planeParameters, out int[] pointIds,
                                                           out int[] cellIds)
        {
            HashSet<int> globalPointIds = new HashSet<int>();
            HashSet<int> globalCellIds = new HashSet<int>();
            foreach (var entry in _actors)
            {
                vtkMaxActor actor = entry.Value;
                bool visible = actor.Geometry.GetVisibility() != 0 ||
                               (actor.GeometryHiddenBySectionView && actor.VtkMaxActorVisible);
                if (_selectableActorsFilter != null) visible = _selectableActorsFilter.Contains(actor.Name);
                if (!visible || !actor.VolumeGridAvailable || !_nativeMirrorNames.Contains(entry.Key)) continue;
                //
                BoundingBox bb = new BoundingBox(_nativeMirror.GetPartBounds(entry.Key));
                FrustumBoxResultEnum boxResult = bb.TestBoundsAgainstFrustum(planeParameters);
                if (boxResult == FrustumBoxResultEnum.Outside) continue;
                if (boxResult == FrustumBoxResultEnum.Inside && TryAddWholePart(actor, globalPointIds, globalCellIds))
                    continue;
                PartExchangeData grid = SendNativeVolumeGrid(entry.Key, actor);
                if (grid == null) continue;
                if (boxResult == FrustumBoxResultEnum.Inside)
                {
                    globalPointIds.UnionWith(grid.Nodes.Ids);
                    globalCellIds.UnionWith(grid.Cells.Ids);
                    continue;
                }
                int[] insidePointIds;
                int[] insideCellIds;
                _nativeMirror.GetIdsInsideFrustum(entry.Key, planeParameters, grid.Nodes.Ids.Length,
                                                  grid.Cells.Ids.Length, out insidePointIds, out insideCellIds);
                globalPointIds.UnionWith(insidePointIds);
                globalCellIds.UnionWith(insideCellIds);
            }
            pointIds = new int[globalPointIds.Count];
            globalPointIds.CopyTo(pointIds);
            cellIds = new int[globalCellIds.Count];
            globalCellIds.CopyTo(cellIds);
        }
        // TryGetVolumeGrid for the native view: the grid the actor's provider returns is sent once per native view,
        // actor and animation frame, as vtkMaxActor keeps its grid until the actor is replaced (new scalars change
        // no id). A transformed copy is not ported: its grid is the source part's, untransformed
        private PartExchangeData SendNativeVolumeGrid(string name, vtkMaxActor actor)
        {
            if (actor.VolumeGridProvider == null) return null;
            string sourceName = actor.VolumeGridSourceName ?? name;
            int frameNumber = _animationActors.ContainsKey(sourceName) ? _currentAnimationFrameNumber : -1;
            if (_nativeVolumeGrids == null)
                _nativeVolumeGrids = new Dictionary<string, Tuple<VtkNativeView, vtkMaxActor, int, PartExchangeData>>();
            //
            Tuple<VtkNativeView, vtkMaxActor, int, PartExchangeData> sent;
            if (_nativeVolumeGrids.TryGetValue(name, out sent) && sent.Item1 == _nativeMirror && sent.Item2 == actor &&
                sent.Item3 == frameNumber) return sent.Item4;
            //
            PartExchangeData data = actor.VolumeGridProvider(sourceName);
            if (data == null || data.Nodes == null || data.Nodes.Ids == null || data.Cells == null ||
                data.Cells.CellNodeIds == null || data.Cells.Ids == null) return null;
            _nativeMirror.SetVolumeGrid(name, VtkNativeMeshData.FromPartExchangeData(data));
            _nativeVolumeGrids[name] = Tuple.Create(_nativeMirror, actor, frameNumber, data);
            return data;
        }
    }
}
