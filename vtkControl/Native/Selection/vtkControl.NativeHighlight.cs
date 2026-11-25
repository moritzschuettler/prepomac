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
using System.Windows.Forms;
using CaeGlobals;
using CaeMesh;
using vtkControl.Native;

namespace vtkControl
{
    // The mouse selection through the native view: the hover with the native pick, the native id resolution and the
    // highlights of the native selection layer, and the selection by clicking in the native view when the widgets
    // area, whose style raises the pick events, is off
    public partial class vtkControl
    {
        // Variables
        private int[] _nativeLeftButtonDown;


        // Event handlers
        // A click selects and the mouse moving over the parts highlights what a click would select; a drag rotates
        // the view and selects nothing
        private void NativeWindow_SelectionEvent(object sender, VtkNativeEventArgs e)
        {
            if (_nativeMirror == null || (_nativeAreas & VtkNativeArea.Selection) == 0) return;
            int x = (int)e.Values[0];
            int y = (int)e.Values[1];
            if (e.Event == VtkNativeEvent.LeftButtonDown) _nativeLeftButtonDown = new int[] { x, y };
            else if (e.Event == VtkNativeEvent.LeftButtonUp)
            {
                bool click = _nativeLeftButtonDown != null && Math.Abs(x - _nativeLeftButtonDown[0]) <= 2 &&
                             Math.Abs(y - _nativeLeftButtonDown[1]) <= 2;
                _nativeLeftButtonDown = null;
                if (!click) return;
                ClearNativeMouseSelection();
                PickWithNativeMirror(x, y, false, x, y);
                _nativeMirror.Render();
            }
            else if (e.Event == VtkNativeEvent.MouseMove && _nativeLeftButtonDown == null)
            {
                HoverWithNativeMirror(x, y, false, x, y);
            }
        }


        // Methods
        private void WireNativeWindowSelection(VtkNativeView view, bool wire)
        {
            if (view == null) return;
            view.NativeEvent -= NativeWindow_SelectionEvent;
            if (wire) view.NativeEvent += NativeWindow_SelectionEvent;
            _nativeLeftButtonDown = null;
        }
        private void ClearNativeMouseSelection()
        {
            if (_nativeMirror == null || (_nativeAreas & VtkNativeArea.Selection) == 0) return;
            _nativeMirror.ClearHighlights(true);
        }
        // The mouse moving over the parts, or the box of a box selection being dragged: what a click would select is
        // highlighted. The probe label of a query is the probe widget of this control, whose native twin draws it
        public void HoverWithNativeMirror(int x1, int y1, bool boxSelection, int x2, int y2)
        {
            if (_nativeMirror == null || (_nativeAreas & VtkNativeArea.Selection) == 0) return;
            bool userPick = _userPick;
            try
            {
                _userPick = true;
                if (_selectBy == vtkSelectBy.Off || _selectBy == vtkSelectBy.Widget) return;
                else if (_selectBy == vtkSelectBy.Default)
                {
                    if (boxSelection)
                    {
                        string[] pickedActorNames = NativeMirrorPickArea(x1, y1, x2, y2, out _);
                        MouseEventArgs e = new MouseEventArgs(MouseButtons.Left, 1, x1, y1, 0);
                        Controller_ActorsPicked?.Invoke(e, ModifierKeys, pickedActorNames);
                    }
                }
                else if (boxSelection)
                {
                    ClearNativeMouseSelection();
                    NativePickByAreaHighlight(x1, y1, x2, y2);
                }
                else
                {
                    ClearNativeMouseSelection();
                    NativeHoverPoint(x1, y1);
                }
            }
            catch { }
            finally
            {
                _userPick = UserPick;
                _nativeMirror.Render();
            }
        }
        // The hover of a point: the node, element, edge, surface, part or geometry entity under it
        private void NativeHoverPoint(int x, int y)
        {
            double[] pickedPoint;
            string partName = NativeMirrorPickPoint(x, y, out pickedPoint, out _);
            if (partName == null)
            {
                if (_probeWidget != null && _probeWidget.GetVisibility() == 1) _probeWidget.VisibilityOff();
                return;
            }
            // The cell of the picked part under the point
            int[] cellFaceNodeIds;
            double[][] cellFaceNodeCoor;
            int cellId = _nativeMirror.GetClosestCell(ref pickedPoint, -1, new string[] { partName }, out cellFaceNodeIds,
                                                      out cellFaceNodeCoor);
            if (cellId == -1) return;
            //
            switch (_selectBy)
            {
                case vtkSelectBy.Id:
                    break;
                case vtkSelectBy.Node:
                    int nodeId = NativeGetNodeIdOnCellFaceClosestToPoint(pickedPoint);
                    AddNativeMouseHighlight(Controller_GetNodeActorData(new int[] { nodeId }), true, false);
                    if (_querySelectionInProgress) ShowNativeProbe(x, y, nodeId.ToString());
                    break;
                case vtkSelectBy.Element:
                    vtkMaxActorData cellData = Controller_GetCellActorData(new int[] { cellId }, null);
                    AddNativeMouseHighlight(cellData, false, cellData != null && cellData.CanHaveElementEdges);
                    if (_querySelectionInProgress) ShowNativeProbe(x, y, cellId.ToString());
                    break;
                case vtkSelectBy.Edge:
                case vtkSelectBy.GeometryEdge:
                case vtkSelectBy.GeometryEdgeAngle:
                    int[] edgeNodeIds = GetEdgeNodeIds(pickedPoint, cellId, cellFaceNodeIds);
                    vtkMaxActorData edgeData = Controller_GetEdgeActorData(cellId, edgeNodeIds);
                    AddNativeMouseHighlight(edgeData, false, false);
                    if (_querySelectionInProgress && _selectBy == vtkSelectBy.GeometryEdge && edgeData != null)
                        ShowNativeProbe(x, y, edgeData.Name);
                    break;
                case vtkSelectBy.Surface:
                case vtkSelectBy.GeometrySurface:
                case vtkSelectBy.GeometrySurfaceAngle:
                    string noEdgePartName;
                    vtkMaxActorData surfaceData = Controller_GetSurfaceEdgesActorDataFromElementId(cellId, cellFaceNodeIds,
                                                                                                   out noEdgePartName);
                    if (surfaceData != null) AddNativeMouseHighlight(surfaceData, false, false);
                    else if (noEdgePartName != null) AddNativePartMouseHighlight(noEdgePartName);
                    if (_querySelectionInProgress && _selectBy == vtkSelectBy.GeometrySurface && surfaceData != null)
                        ShowNativeProbe(x, y, surfaceData.Name);
                    break;
                case vtkSelectBy.EdgeAngle:
                    NativeHoverEdgeAngle(pickedPoint, cellId, cellFaceNodeIds);
                    break;
                case vtkSelectBy.SurfaceAngle:
                    vtkMaxActorData faceData = Controller_GetCellFaceActorData(cellId, cellFaceNodeIds);
                    AddNativeMouseHighlight(faceData, false, faceData != null && faceData.CanHaveElementEdges);
                    break;
                case vtkSelectBy.Part:
                case vtkSelectBy.GeometryPart:
                    AddNativePartMouseHighlight(partName);
                    if (_querySelectionInProgress && _selectBy == vtkSelectBy.Part) ShowNativeProbe(x, y, partName);
                    break;
                case vtkSelectBy.Geometry:
                    NativeHoverGeometry(pickedPoint, cellId, cellFaceNodeIds);
                    break;
                case vtkSelectBy.GeometryVertex:
                    int[] vertexEdgeNodeIds = GetEdgeNodeIds(pickedPoint, cellId, cellFaceNodeIds);
                    vtkMaxActorData vertexData = Controller_GetGeometryVertexActorData(pickedPoint, cellId, vertexEdgeNodeIds,
                                                                                       cellFaceNodeIds);
                    AddNativeMouseHighlight(vertexData, true, false);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
        // The probe label of the hovered node, element, edge, surface or part during a query
        private void ShowNativeProbe(int x, int y, string itemName)
        {
            if (_probeWidget == null) return;
            _probeWidget.SetPosition(x + 20d, y + 10d);
            _probeWidget.SetText(Controller_GetAnnotationText(itemName));
            if (_probeWidget.GetVisibility() == 0) _probeWidget.VisibilityOn();
        }
        // The edge angle selection: the closest edge of the cell face as a line of its own
        private void NativeHoverEdgeAngle(double[] pickedPoint, int cellId, int[] cellFaceNodeIds)
        {
            vtkMaxActorData cellFaceData = Controller_GetCellFaceActorData(cellId, cellFaceNodeIds);
            double[][] nodeCoor;
            int[] edgeCell;
            int edgeCellType;
            GetClosestEdgeCell(pickedPoint, cellFaceData, out _, out nodeCoor, out edgeCell, out edgeCellType);
            if (nodeCoor == null) return;
            //
            double[] coordinates = new double[3 * nodeCoor.Length];
            for (int i = 0; i < nodeCoor.Length; i++) Array.Copy(nodeCoor[i], 0, coordinates, 3 * i, 3);
            int[] cells = new int[edgeCell.Length + 1];
            cells[0] = edgeCell.Length;
            Array.Copy(edgeCell, 0, cells, 1, edgeCell.Length);
            VtkNativeHighlightStyle style = GetNativeMouseSelectionStyle();
            _nativeMirror.AddHighlight(new VtkNativeMeshData(coordinates, cells, new byte[] { (byte)edgeCellType }, null, null),
                                       style);
        }
        // The geometry selection: the vertex, edge or surface under the point, or the part without edges, and the
        // point the entity is marked with
        private void NativeHoverGeometry(double[] pickedPoint, int cellId, int[] cellFaceNodeIds)
        {
            int[] edgeNodeIds = GetEdgeNodeIds(pickedPoint, cellId, cellFaceNodeIds);
            string noEdgePartName;
            vtkMaxActorData actorData = Controller_GetGeometryActorData(pickedPoint, cellId, edgeNodeIds, cellFaceNodeIds,
                                                                        out noEdgePartName);
            if (actorData != null && actorData.Geometry.Nodes.Coor != null && actorData.Geometry.Nodes.Coor.Length > 0)
                AddNativeMouseHighlight(actorData, actorData.Geometry.Nodes.Coor.Length == 1, false);
            else if (noEdgePartName != null) AddNativePartMouseHighlight(noEdgePartName);
            //
            if (actorData != null && actorData.ExtraPointCoor != null && actorData.ExtraPointCoor.Length > 0)
            {
                double[] coordinates = new double[3 * actorData.ExtraPointCoor.Length];
                for (int i = 0; i < actorData.ExtraPointCoor.Length; i++)
                    Array.Copy(actorData.ExtraPointCoor[i], 0, coordinates, 3 * i, 3);
                VtkNativeHighlightStyle style = GetNativeMouseSelectionStyle();
                style.Points = 1;
                _nativeMirror.AddHighlight(new VtkNativeMeshData(coordinates, new int[0], new byte[0], null, null), style);
            }
        }
        // The highlight of a box selection: the nodes and elements of the picked parts inside the frustum
        private void NativePickByAreaHighlight(int x1, int y1, int x2, int y2)
        {
            double[][] planes;
            string[] pickedActorNames = NativeMirrorPickArea(x1, y1, x2, y2, out planes);
            HashSet<int> selectedPointGlobalIds = new HashSet<int>();
            HashSet<int> selectedCellGlobalIds = new HashSet<int>();
            foreach (string name in pickedActorNames)
            {
                vtkMaxActor actor = _actors[name];
                if (new BoundingBox(_nativeMirror.GetPartBounds(name)).TestBoundsAgainstFrustum(planes) ==
                    FrustumBoxResultEnum.Inside && TryAddWholePart(actor, selectedPointGlobalIds, selectedCellGlobalIds))
                    continue;
                PartExchangeData grid = SendNativeVolumeGrid(name, actor);
                if (grid == null) continue;
                int[] insidePointIds;
                int[] insideCellIds;
                _nativeMirror.GetIdsInsideFrustum(name, planes, grid.Nodes.Ids.Length, grid.Cells.Ids.Length,
                                                  out insidePointIds, out insideCellIds);
                selectedPointGlobalIds.UnionWith(insidePointIds);
                selectedCellGlobalIds.UnionWith(insideCellIds);
            }
            int[] pointIds = new int[selectedPointGlobalIds.Count];
            selectedPointGlobalIds.CopyTo(pointIds);
            int[] cellIds = new int[selectedCellGlobalIds.Count];
            selectedCellGlobalIds.CopyTo(cellIds);
            bool completelyInside = x1 > x2;
            //
            if (_selectBy == vtkSelectBy.Node)
            {
                if (pointIds.Length > 0) AddNativeMouseHighlight(Controller_GetNodeActorData(pointIds), true, false);
            }
            else if (_selectBy == vtkSelectBy.Element)
            {
                if (cellIds.Length == 0) return;
                vtkMaxActorData data = Controller_GetCellActorData(cellIds, pointIds);
                AddNativeMouseHighlight(data, false, data.CanHaveElementEdges);
            }
            else if (_selectBy == vtkSelectBy.Part || _selectBy == vtkSelectBy.GeometryPart)
            {
                foreach (string name in pickedActorNames) AddNativePartMouseHighlight(name);
            }
            else if (_selectBy == vtkSelectBy.Geometry)
            {
                string[] noEdgePartNames;
                vtkMaxActorData actorData = Controller_GetSurfaceEdgesActorDataFromNodeAndElementIds(pointIds, cellIds,
                                                                                                     completelyInside,
                                                                                                     out noEdgePartNames);
                AddNativeMouseHighlight(actorData, false, false);
                if (noEdgePartNames != null) foreach (string name in noEdgePartNames) AddNativePartMouseHighlight(name);
            }
            else throw new NotSupportedException();
        }
        // A highlight of the mouse selection in the color of Globals.CurrentMouseHighlightColor: the nodes of the data
        // as points, or its cells with the element edges when they can have them
        private void AddNativeMouseHighlight(vtkMaxActorData data, bool points, bool elementEdges)
        {
            if (data == null || data.Geometry == null) return;
            VtkNativeMeshData mesh = VtkNativeMeshData.FromPartExchangeData(data.Geometry);
            if (mesh.PointCount == 0) return;
            VtkNativeHighlightStyle style = GetNativeMouseSelectionStyle();
            style.Points = points || mesh.CellCount == 0 ? 1 : 0;
            style.ElementEdges = elementEdges && _edgesVisibility == vtkEdgesVisibility.ElementEdges ? 1 : 0;
            _nativeMirror.AddHighlight(mesh, style);
        }
        private VtkNativeHighlightStyle GetNativeMouseSelectionStyle()
        {
            return VtkNativeHighlightStyle.CreateMouseSelectionStyle(Globals.CurrentMouseHighlightColor);
        }
        private void AddNativePartMouseHighlight(string partName)
        {
            if (!_nativeMirrorNames.Contains(partName)) return;
            _nativeMirror.AddPartHighlight(partName, GetNativeMouseSelectionStyle());
        }
    }
}
