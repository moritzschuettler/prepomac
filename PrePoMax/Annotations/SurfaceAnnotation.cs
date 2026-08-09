// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax
{
    [Serializable]
    public class SurfaceAnnotation : AnnotationBase
    {
        // Variables                                                                                                                
        private int _geometryId;


        // Properties                                                                                                               
        public int GeometryId { get { return _geometryId; } set { _geometryId = value; } }


        // Constructors                                                                                                             
        public SurfaceAnnotation(string name, int geometryId)
            : base(name)
        {
            _geometryId = geometryId;
            _partIds = new int[] { FeMesh.GetPartIdFromGeometryId(geometryId) };
        }


        // Methods
        public override void GetAnnotationData(out string text, out double[] coor)
        {
            int[] itemTypePartIds = FeMesh.GetItemTypePartIdsFromGeometryId(_geometryId);
            FeMesh mesh = Controller.DisplayedMesh;
            int surfaceId = itemTypePartIds[0];            
            BasePart part = mesh.GetPartFromId(itemTypePartIds[2]);
            int numEdges = part.Visualization.FaceEdgeIds[surfaceId].Length;
            int numVertices = part.Visualization.GetVertexNodeIdsForSurfaceId(surfaceId).Length;
            double area;
            string areaUnit = Controller.GetAreaUnit();
            string fieldUnit = "";
            string numberFormat = Controller.Settings.NumberFormat.GetNumberFormat();
            //
            bool results = false;
            float min = float.MaxValue;
            float max = -float.MaxValue;
            float sum = 0;
            float avg = 0;
            int[] nodeIds;
            //
            GeomFaceType faceType = GeomFaceType.Unknown;
            if (part.Visualization.FaceTypes != null) faceType = part.Visualization.FaceTypes[surfaceId];
            //
            mesh.GetFaceNodes(_geometryId, out nodeIds);
            //
            if (Controller.CurrentView == ViewGeometryModelResults.Geometry ||
                Controller.CurrentView == ViewGeometryModelResults.Model)
            {
                // Area
                area = mesh.GetSurfaceArea(_geometryId);
            }
            else if (Controller.CurrentView == ViewGeometryModelResults.Results)
            {
                float value;
                FeNode[] nodes = Controller.GetScaledNodes(1, nodeIds, false);
                // Area
                Dictionary<int, double> weights;
                Dictionary<int, FeNode> nodesDic = new Dictionary<int, FeNode>();
                for (int i = 0; i < nodes.Length; i++) nodesDic.Add(nodes[i].Id, nodes[i]);
                mesh.GetFaceNodeLumpedWeights(part.Visualization, surfaceId, nodesDic, out weights, out area);
                //
                if (Controller.ViewResultsType == ViewResultsTypeEnum.ColorContours)
                {
                    results = true;
                    // Values
                    for (int i = 0; i < nodeIds.Length; i++)
                    {
                        value = Controller.GetNodalValue(nodeIds[i]);
                        if (value < min) min = value;
                        if (value > max) max = value;
                        sum += value;
                        //avg += (float)(value * weights[nodeIds[i]]);
                    }
                    avg = sum / nodes.Length;
                    // Units
                    fieldUnit = Controller.GetCurrentResultsUnitAbbreviation();
                    if (fieldUnit == "/") fieldUnit = "";
                }
            }
            else throw new NotSupportedException();
            // Coor
            coor = Controller.GetSurfaceCenter(_geometryId);
            //
            bool showSurfaceId = Controller.Settings.Annotations.ShowEdgeSurId;
            bool showSurfaceType = Controller.Settings.Annotations.ShowEdgeSurType;
            bool showSurfaceTopology = Controller.Settings.Annotations.ShowEdgeSurTopology;
            bool showSurfaceLength = Controller.Settings.Annotations.ShowEdgeSurSize;
            bool showSurfaceMax = Controller.Settings.Annotations.ShowEdgeSurMax && results;
            bool showSurfaceMin = Controller.Settings.Annotations.ShowEdgeSurMin && results;
            bool showSurfaceSum = Controller.Settings.Annotations.ShowEdgeSurSum && results;
            bool showSurfaceAvg = Controller.Settings.Annotations.ShowEdgeSurAvg && results;
            if (!showSurfaceType && !showSurfaceLength && !showSurfaceMax &&
                !showSurfaceMin && !showSurfaceSum && !showSurfaceAvg) showSurfaceId = true;
            //
            text = "";
            if (showSurfaceId)
            {
                text += string.Format("Surface id: {0} on {1}", surfaceId + 1, part.Name);
            }
            if (showSurfaceType)
            {
                if (text.Length > 0) text += Environment.NewLine;
                text += string.Format("Surface type: {0}", faceType.ToString());
            }
            if (showSurfaceLength)
            {
                if (text.Length > 0) text += Environment.NewLine;
                text += string.Format("Surface area: {0} {1}", area.ToString(numberFormat), areaUnit);
            }
            if (showSurfaceTopology)
            {
                if (text.Length > 0) text += Environment.NewLine;
                text += string.Format("Edges: {0}", numEdges);
                text += Environment.NewLine;
                text += string.Format("Vertices: {0}", numVertices);
            }
            if (showSurfaceMax)
            {
                if (text.Length > 0) text += Environment.NewLine;
                text += string.Format("Max: {0} {1}", max.ToString(numberFormat), fieldUnit);
            }
            if (showSurfaceMin)
            {
                if (text.Length > 0) text += Environment.NewLine;
                text += string.Format("Min: {0} {1}", min.ToString(numberFormat), fieldUnit);
            }
            if (showSurfaceSum)
            {
                if (text.Length > 0) text += Environment.NewLine;
                text += string.Format("Nodal sum: {0} {1}", sum.ToString(numberFormat), fieldUnit);
            }
            if (showSurfaceAvg)
            {
                if (text.Length > 0) text += Environment.NewLine;
                text += string.Format("Nodal avg: {0} {1}", avg.ToString(numberFormat), fieldUnit);
            }
            //
            if (IsTextOverridden) text = OverriddenText;
        }
        public override bool IsAnimationTextChanged()
        {
            var settings = Controller.Settings.Annotations;
            bool fieldData = Controller.ViewResultsType == ViewResultsTypeEnum.ColorContours &&
                             (settings.ShowEdgeSurMax || settings.ShowEdgeSurMin ||
                              settings.ShowEdgeSurSum || settings.ShowEdgeSurAvg);
            return !IsTextOverridden && Controller.CurrentView == ViewGeometryModelResults.Results &&
                   (settings.ShowEdgeSurSize || fieldData);
        }
    }
}
