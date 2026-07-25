// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using CaeGlobals;
using CaeMesh;

namespace PrePoMax
{
    [Serializable]
    public enum MeasurementAnnotationType
    {
        Distance,
        Angle,
        Circle
    }

    [Serializable]
    public class MeasurementAnnotation : AnnotationBase
    {
        // Variables
        private MeasurementAnnotationType _measurementType;
        private int[] _nodeIds;


        // Properties
        public MeasurementAnnotationType MeasurementType { get { return _measurementType; } }
        public int[] NodeIds { get { return _nodeIds; } }
        public bool DrawEndArrows { get { return false; } }


        // Constructors
        public MeasurementAnnotation(string name, MeasurementAnnotationType measurementType, int[] nodeIds)
            : base(name)
        {
            if (nodeIds == null) throw new ArgumentNullException("nodeIds");
            if (measurementType == MeasurementAnnotationType.Distance && nodeIds.Length != 2)
                throw new ArgumentException("A distance measurement requires two nodes.");
            if ((measurementType == MeasurementAnnotationType.Angle ||
                 measurementType == MeasurementAnnotationType.Circle) && nodeIds.Length != 3)
                throw new ArgumentException("An angle or circle measurement requires three nodes.");

            _measurementType = measurementType;
            _nodeIds = nodeIds.ToArray();
            _partIds = Controller.DisplayedMesh.GetPartIdsFromNodeIds(_nodeIds);
        }


        // Methods
        public override void GetAnnotationData(out string text, out double[] coor)
        {
            GetAnnotationData(out text, out coor, out _, out _);
        }
        public void GetAnnotationData(out string text, out double[] anchorPoint,
                                      out double[][] nodeCoordinates, out double[][] lineCoordinates)
        {
            double[][] valueCoordinates;
            if (Controller.CurrentView == ViewGeometryModelResults.Results)
            {
                nodeCoordinates = GetNodeCoordinates(Controller.GetScale(), true);
                valueCoordinates = GetNodeCoordinates(1, false);
            }
            else
            {
                valueCoordinates = GetNodeCoordinates(0, false);
                nodeCoordinates = GetNodeCoordinates(0, true);
            }

            if (_measurementType == MeasurementAnnotationType.Distance)
                GetDistanceData(valueCoordinates, nodeCoordinates, out text, out anchorPoint, out lineCoordinates);
            else if (_measurementType == MeasurementAnnotationType.Angle)
                GetAngleData(valueCoordinates, nodeCoordinates, out text, out anchorPoint, out lineCoordinates);
            else if (_measurementType == MeasurementAnnotationType.Circle)
            {
                double[][] drawCoordinates = nodeCoordinates;
                GetCircleData(valueCoordinates, drawCoordinates, out text, out anchorPoint,
                              out nodeCoordinates, out lineCoordinates);
            }
            else
                throw new NotSupportedException();

            if (IsTextOverridden) text = OverriddenText;
        }
        public override bool IsAnimationTextChanged()
        {
            return !IsTextOverridden && Controller.CurrentView == ViewGeometryModelResults.Results;
        }
        public override bool IsAnimationAnchorChanged()
        {
            // A measurement anchor is derived from multiple material nodes. It cannot be represented by
            // the single-cell interpolation used by ordinary material-point annotation anchors.
            return Controller.CurrentView == ViewGeometryModelResults.Results;
        }
        public override bool CanUseAnimationArrowAnchor()
        {
            return false;
        }

        private double[][] GetNodeCoordinates(float scale, bool includeExplodedViewOffset)
        {
            double[][] coordinates = new double[_nodeIds.Length][];
            if (Controller.CurrentView == ViewGeometryModelResults.Results)
            {
                FeNode[] nodes = Controller.GetScaledNodes(scale, _nodeIds, includeExplodedViewOffset);
                for (int i = 0; i < nodes.Length; i++) coordinates[i] = nodes[i].Coor;
            }
            else
            {
                for (int i = 0; i < _nodeIds.Length; i++)
                    coordinates[i] = Controller.GetNode(_nodeIds[i], includeExplodedViewOffset).Coor.ToArray();
            }
            return coordinates;
        }
        private void GetDistanceData(double[][] valueCoordinates, double[][] drawCoordinates,
                                     out string text, out double[] anchorPoint, out double[][] lineCoordinates)
        {
            Vec3D p1 = new Vec3D(valueCoordinates[0]);
            Vec3D p2 = new Vec3D(valueCoordinates[1]);
            Vec3D distance = p2 - p1;
            string numberFormat = Controller.Settings.NumberFormat.GetNumberFormat();
            string lengthUnit = Controller.GetLengthUnit();

            text = string.Format("Distance: {0} {1}{2}", distance.Len.ToString(numberFormat), lengthUnit,
                                 Environment.NewLine);
            text += string.Format("dx: {0} {1}{2}", distance.X.ToString(numberFormat), lengthUnit,
                                  Environment.NewLine);
            text += string.Format("dy: {0} {1}{2}", distance.Y.ToString(numberFormat), lengthUnit,
                                  Environment.NewLine);
            text += string.Format("dz: {0} {1}", distance.Z.ToString(numberFormat), lengthUnit);

            Vec3D drawP1 = new Vec3D(drawCoordinates[0]);
            Vec3D drawP2 = new Vec3D(drawCoordinates[1]);
            anchorPoint = ((drawP1 + drawP2) * 0.5).Coor;
            lineCoordinates = drawCoordinates.Select(coor => coor.ToArray()).ToArray();
        }
        private void GetAngleData(double[][] valueCoordinates, double[][] drawCoordinates,
                                  out string text, out double[] anchorPoint, out double[][] lineCoordinates)
        {
            Vec3D valueP1 = new Vec3D(valueCoordinates[0]);
            Vec3D valueP2 = new Vec3D(valueCoordinates[1]);
            Vec3D valueP3 = new Vec3D(valueCoordinates[2]);
            double angle = ComputeAngle(valueP1, valueP2, valueP3, out _, out _);

            Vec3D drawP1 = new Vec3D(drawCoordinates[0]);
            Vec3D drawP2 = new Vec3D(drawCoordinates[1]);
            Vec3D drawP3 = new Vec3D(drawCoordinates[2]);
            double drawAngle = ComputeAngle(drawP1, drawP2, drawP3, out Vec3D arcStart, out Vec3D axis);

            List<double[]> linePoints =
                new List<double[]>() { drawP1.Coor, drawP2.Coor, drawP3.Coor, drawP2.Coor };
            linePoints.AddRange(ComputeCirclePoints(drawP2, axis, arcStart, drawAngle * Math.PI / 180));
            lineCoordinates = linePoints.ToArray();
            anchorPoint = lineCoordinates[lineCoordinates.Length / 2].ToArray();

            string numberFormat = Controller.Settings.NumberFormat.GetNumberFormat();
            text = string.Format("Angle: {0}{1}", angle.ToString(numberFormat),
                                 StringAngleDegConverter.GetUnitAbbreviation());
        }
        private void GetCircleData(double[][] valueCoordinates, double[][] drawCoordinates,
                                   out string text, out double[] anchorPoint, out double[][] nodeCoordinates,
                                   out double[][] lineCoordinates)
        {
            Vec3D valueP1 = new Vec3D(valueCoordinates[0]);
            Vec3D valueP2 = new Vec3D(valueCoordinates[1]);
            Vec3D valueP3 = new Vec3D(valueCoordinates[2]);
            Vec3D.GetCircle(valueP1, valueP2, valueP3, out double radius, out Vec3D center, out Vec3D axis);

            Vec3D drawP1 = new Vec3D(drawCoordinates[0]);
            Vec3D drawP2 = new Vec3D(drawCoordinates[1]);
            Vec3D drawP3 = new Vec3D(drawCoordinates[2]);
            Vec3D.GetCircle(drawP1, drawP2, drawP3, out _, out Vec3D drawCenter, out Vec3D drawAxis);

            nodeCoordinates = AddCenter(drawCoordinates, drawCenter.Coor);
            lineCoordinates = ComputeCirclePoints(drawCenter, drawAxis, drawP1, 2 * Math.PI);
            anchorPoint = lineCoordinates[2].ToArray();

            string numberFormat = Controller.Settings.NumberFormat.GetNumberFormat();
            string lengthUnit = Controller.GetLengthUnit();
            text = string.Format("Radius: {0} {1}{2}", radius.ToString(numberFormat), lengthUnit,
                                 Environment.NewLine);
            text += string.Format("X: {0} {1}{2}", center.X.ToString(numberFormat), lengthUnit,
                                  Environment.NewLine);
            text += string.Format("Y: {0} {1}{2}", center.Y.ToString(numberFormat), lengthUnit,
                                  Environment.NewLine);
            text += string.Format("Z: {0} {1}", center.Z.ToString(numberFormat), lengthUnit);
        }
        private static double[][] AddCenter(double[][] coordinates, double[] center)
        {
            double[][] coordinatesWithCenter = new double[coordinates.Length + 1][];
            for (int i = 0; i < coordinates.Length; i++)
                coordinatesWithCenter[i] = coordinates[i].ToArray();
            coordinatesWithCenter[coordinates.Length] = center.ToArray();
            return coordinatesWithCenter;
        }
        private static double ComputeAngle(Vec3D p1, Vec3D vertex, Vec3D p3, out Vec3D arcStart, out Vec3D axis)
        {
            Vec3D line1 = p1 - vertex;
            Vec3D line2 = p3 - vertex;
            if (line1.Len2 > line2.Len2)
            {
                arcStart = vertex + line2 * 0.5;
                axis = Vec3D.CrossProduct(line2, line1);
            }
            else
            {
                arcStart = vertex + line1 * 0.5;
                axis = Vec3D.CrossProduct(line1, line2);
            }
            axis.Normalize();
            line1.Normalize();
            line2.Normalize();
            double dot = Math.Max(-1, Math.Min(1, Vec3D.DotProduct(line1, line2)));
            return Math.Acos(dot) * 180 / Math.PI;
        }
        private static double[][] ComputeCirclePoints(Vec3D center, Vec3D axis, Vec3D point, double angle)
        {
            const int segments = 40;
            double[][] coordinates = new double[segments + 1][];
            double deltaAngle = angle / segments;
            Vec3D radius = point - center;
            double radiusLength = radius.Len;
            coordinates[0] = point.Coor;

            for (int i = 0; i < segments; i++)
            {
                Vec3D normal = Vec3D.CrossProduct(axis, radius);
                normal.Normalize();
                Vec3D delta = radiusLength * Math.Sin(deltaAngle) * normal -
                              (1 - Math.Cos(deltaAngle)) * radius;
                point = point + delta;
                radius = radius + delta;
                coordinates[i + 1] = point.Coor;
            }
            return coordinates;
        }
    }
}
