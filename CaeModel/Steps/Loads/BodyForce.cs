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
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using CaeMesh;
using CaeGlobals;
using CaeResults;

namespace CaeModel
{
    [PmxType(3285)]
    public class BodyForce : Load, IDistribution, IPreviewable
    {
        // Variables
        [PmxKey(0)] private string _regionName;
        [PmxKey(1)] private RegionTypeEnum _regionType;
        [PmxKey(2)] private EquationContainer _f1;
        [PmxKey(3)] private EquationContainer _f2;
        [PmxKey(4)] private EquationContainer _f3;
        [PmxKey(5)] private EquationContainer _magnitude;
        [PmxKey(6)] private string _distributionName;


        // Properties
        public override string RegionName { get { return _regionName; } set { _regionName = value; } }
        public override RegionTypeEnum RegionType { get { return _regionType; } set { _regionType = value; } }
        public EquationContainer F1 { get { UpdateEquations(); return _f1; } set { SetF1(value); } }
        public EquationContainer F2 { get { UpdateEquations(); return _f2; } set { SetF2(value); } }
        public EquationContainer F3 { get { UpdateEquations(); return _f3; } set { SetF3(value); } }
        public EquationContainer Magnitude { get { UpdateEquations(); return _magnitude; } set { SetMagnitude(value); } }
        public string DistributionName { get { return _distributionName; } set { _distributionName = value; } }


        // Constructor
        public BodyForce(string name, string regionName, RegionTypeEnum regionType, double f1, double f2, double f3,
                         bool twoD, bool complex, double phaseDeg)
            : base(name, twoD, complex, phaseDeg)
        {
            _regionName = regionName;
            _regionType = regionType;
            //
            double mag = Math.Sqrt(f1 * f1 + f2 * f2 + f3 * f3);
            F1 = new EquationContainer(typeof(StringForcePerVolumeConverter), f1);
            F2 = new EquationContainer(typeof(StringForcePerVolumeConverter), f2);
            F3 = new EquationContainer(typeof(StringForcePerVolumeConverter), f3);
            Magnitude = new EquationContainer(typeof(StringForcePerVolumeConverter), mag);
            //
            _distributionName = Distribution.DefaultDistributionName;
        }


        // Methods
        [OnDeserialized]
        private void RestoreEquationCallbacks(StreamingContext context)
        {
            SetF1(_f1, false);
            SetF2(_f2, false);
            SetF3(_f3, false);
            SetMagnitude(_magnitude, false);
        }
        private void UpdateEquations()
        {
            try
            {
                // If error catch it silently
                if (_f1.IsEquation() || _f2.IsEquation() || _f3.IsEquation()) FEquationChanged();
                else if (_magnitude.IsEquation()) MagnitudeEquationChanged();
            }
            catch (Exception ex) { }
        }
        private void SetF1(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _f1, value, null, FEquationChanged, checkEquation);
        }
        private void SetF2(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _f2, value, null, FEquationChanged, checkEquation);
        }
        private void SetF3(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _f3, value, Check2D, FEquationChanged, checkEquation);
        }
        private void SetMagnitude(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _magnitude, value, CheckMagnitude, MagnitudeEquationChanged, checkEquation);
        }
        //
        private void FEquationChanged()
        {
            double mag = Math.Sqrt(_f1.Value * _f1.Value + _f2.Value * _f2.Value + _f3.Value * _f3.Value);
            _magnitude.SetEquationFromValue(mag, false);
        }
        private void MagnitudeEquationChanged()
        {
            double c1;
            double c2;
            double c3;
            //
            if (_magnitude.Value == 0)
            {
                c1 = 0;
                c2 = 0;
                c3 = 0;
            }
            else
            {
                double r;
                c1 = _f1.Value;
                c2 = _f2.Value;
                c3 = _f3.Value;
                double mag = Math.Sqrt(c1 * c1 + c2 * c2 + c3 * c3);
                //
                if (mag != 0)
                {
                    r = _magnitude.Value / mag;
                    c1 *= r;
                    c2 *= r;
                    c3 *= r;
                }
                else
                {
                    r = Math.Sqrt(Math.Pow(_magnitude.Value, 2) / 3);
                    c1 = r;
                    c2 = r;
                    c3 = r;
                }
            }
            //
            _f1.SetEquationFromValue(c1, false);
            _f2.SetEquationFromValue(c2, false);
            _f3.SetEquationFromValue(c3, false);
        }
        //
        private double Check2D(double value)
        {
            if (_twoD) return 0;
            else return value;
        }
        private double CheckMagnitude(double value)
        {
            if (value < 0) throw new Exception("Value of the body force magnitude must be non-negative.");
            else return value;
        }
        // A constant body force in a rectangular coordinate system has the same components in every element
        public bool IsUniform(FeModel model)
        {
            if (_distributionName != Distribution.DefaultDistributionName) return false;
            //
            CoordinateSystem coordinateSystem;
            model.Mesh.CoordinateSystems.TryGetValue(CoordinateSystemName, out coordinateSystem);
            return coordinateSystem == null || coordinateSystem.Type != CoordinateSystemTypeEnum.Cylindrical;
        }
        public double[] GetUniformForcePerVolume(FeModel model)
        {
            double[] force = new double[] { _f1.Value, _f2.Value, _f3.Value };
            CoordinateSystem coordinateSystem;
            model.Mesh.CoordinateSystems.TryGetValue(CoordinateSystemName, out coordinateSystem);
            if (coordinateSystem != null) force = coordinateSystem.GetOrientedVectorAtPoint(force, new double[3]);
            return force;
        }
        // The components in the coordinate system of the load, before they are oriented
        public void GetForcesPerVolumeAndDistancesForPoints(FeModel model, double[][] points, out double[][] distances,
                                                            out double[][] values)
        {
            distances = new double[points.Length][];
            values = new double[points.Length][];
            //
            double f1 = _f1.Value;
            double f2 = _f2.Value;
            double f3 = _f3.Value;
            if (_distributionName == Distribution.DefaultDistributionName)
            {
                for (int i = 0; i < points.Length; i++) values[i] = new double[] { f1, f2, f3 };
            }
            else
            {
                double[][] magnitudes;
                Distribution distribution = model.Distributions[_distributionName];
                distribution.GetMagnitudesAndDistancesForPoints(model, points, out magnitudes, out distances);
                //
                for (int i = 0; i < points.Length; i++)
                {
                    if (magnitudes[i].Length == 1)
                        values[i] = new double[] { magnitudes[i][0] * f1, magnitudes[i][0] * f2, magnitudes[i][0] * f3 };
                    else if (magnitudes[i].Length == 3)
                        values[i] = new double[] { magnitudes[i][0] * f1, magnitudes[i][1] * f2, magnitudes[i][2] * f3 };
                    else throw new NotSupportedException();
                }
            }
        }
        public double[][] GetOrientedForcesPerVolumeForPoints(FeModel model, double[][] points, out double[][] distances)
        {
            double[][] forces;
            GetForcesPerVolumeAndDistancesForPoints(model, points, out distances, out forces);
            //
            CoordinateSystem coordinateSystem;
            model.Mesh.CoordinateSystems.TryGetValue(CoordinateSystemName, out coordinateSystem);
            if (coordinateSystem != null)
            {
                for (int i = 0; i < forces.Length; i++)
                {
                    forces[i] = coordinateSystem.GetOrientedVectorAtPoint(forces[i], points[i]);
                    // A point on the axis of a cylindrical coordinate system has no radial or tangential direction
                    for (int j = 0; j < forces[i].Length; j++)
                    {
                        if (double.IsNaN(forces[i][j])) forces[i][j] = 0;
                    }
                }
            }
            return forces;
        }
        public void GetElementCentroidsAndForcesPerVolume(FeModel model, out int[] elementIds, out double[][] centroids,
                                                          out double[][] forces)
        {
            elementIds = model.GetElementIdsOfRegion(this);
            centroids = new double[elementIds.Length][];
            for (int i = 0; i < elementIds.Length; i++)
                centroids[i] = model.Mesh.Elements[elementIds[i]].GetCG(model.Mesh.Nodes, out _);
            //
            forces = GetOrientedForcesPerVolumeForPoints(model, centroids, out _);
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            base.CheckEquations();
            //
            _f1.CheckEquation();
            _f2.CheckEquation();
            _f3.CheckEquation();
            _magnitude.CheckEquation();
        }
        // IPreviewable
        public FeResults GetPreview(FeModel model, string resultName, UnitSystem unitSystem)
        {
            FeMesh targetMesh = model.Mesh;
            PartExchangeData allData = new PartExchangeData();
            targetMesh.GetAllNodesAndCells(out allData.Nodes.Ids, out allData.Nodes.Coor, out allData.Cells.Ids,
                                           out allData.Cells.CellNodeIds, out allData.Cells.Types);
            //
            bool addDistances = _distributionName != Distribution.DefaultDistributionName &&
                model.Distributions[_distributionName] is MappedDistribution md &&
                md.InterpolatorType == CloudInterpolatorEnum.ClosestPoint;
            //
            int[] regionNodeIds = model.Mesh.GetNodeIdsFromElementIds(model.GetElementIdsOfRegion(this));
            double[][] coor = new double[regionNodeIds.Length][];
            for (int i = 0; i < regionNodeIds.Length; i++) coor[i] = targetMesh.Nodes[regionNodeIds[i]].Coor;
            Dictionary<int, int> nodeIdArrayId = new Dictionary<int, int>();
            for (int i = 0; i < regionNodeIds.Length; i++) nodeIdArrayId.Add(regionNodeIds[i], i);
            //
            double[][] distances;
            double[][] forces = GetOrientedForcesPerVolumeForPoints(model, coor, out distances);
            //
            float[] distancesAll = new float[allData.Nodes.Coor.Length];
            float[] distances1 = new float[allData.Nodes.Coor.Length];
            float[] distances2 = new float[allData.Nodes.Coor.Length];
            float[] distances3 = new float[allData.Nodes.Coor.Length];
            float[] forcesAll = new float[allData.Nodes.Coor.Length];
            float[] forces1 = new float[allData.Nodes.Coor.Length];
            float[] forces2 = new float[allData.Nodes.Coor.Length];
            float[] forces3 = new float[allData.Nodes.Coor.Length];
            //
            int arrayId;
            double[] force;
            double[] distance;
            for (int i = 0; i < forcesAll.Length; i++)
            {
                if (nodeIdArrayId.TryGetValue(allData.Nodes.Ids[i], out arrayId))
                {
                    if (addDistances)
                    {
                        distance = distances[arrayId];
                        distances1[i] = (float)distance[0];
                        distances2[i] = (float)distance[1];
                        distances3[i] = (float)distance[2];
                        distancesAll[i] = (float)Math.Sqrt(distance[0] * distance[0] + distance[1] * distance[1] +
                                                           distance[2] * distance[2]);
                    }
                    force = forces[arrayId];
                    forces1[i] = (float)force[0];
                    forces2[i] = (float)force[1];
                    forces3[i] = (float)force[2];
                    forcesAll[i] = (float)Math.Sqrt(force[0] * force[0] + force[1] * force[1] + force[2] * force[2]);
                }
                else
                {
                    if (addDistances)
                    {
                        distances1[i] = float.NaN;
                        distances2[i] = float.NaN;
                        distances3[i] = float.NaN;
                        distancesAll[i] = float.NaN;
                    }
                    forces1[i] = float.NaN;
                    forces2[i] = float.NaN;
                    forces3[i] = float.NaN;
                    forcesAll[i] = float.NaN;
                }
            }
            //
            Dictionary<int, int> nodeIdsLookUp = new Dictionary<int, int>();
            for (int i = 0; i < allData.Nodes.Coor.Length; i++) nodeIdsLookUp.Add(allData.Nodes.Ids[i], i);
            FeResults results = new FeResults(resultName, unitSystem);
            results.SetMesh(targetMesh, nodeIdsLookUp);
            //
            Field field;
            FieldData fieldData = new FieldData(FOFieldNames.Distance);
            fieldData.GlobalIncrementId = 1;
            fieldData.StepType = StepTypeEnum.Static;
            fieldData.Time = 1;
            fieldData.MethodId = 1;
            fieldData.StepId = 1;
            fieldData.StepIncrementId = 1;
            //
            if (addDistances)
            {
                field = new Field(fieldData.Name);
                field.AddComponent(FOComponentNames.All, distancesAll);
                field.AddComponent(FOComponentNames.D1, distances1);
                field.AddComponent(FOComponentNames.D2, distances2);
                field.AddComponent(FOComponentNames.D3, distances3);
                results.AddField(fieldData, field);
            }
            //
            fieldData = new FieldData(fieldData);
            fieldData.Name = FOFieldNames.ForcePerVolume;
            //
            field = new Field(fieldData.Name);
            field.AddComponent(FOComponentNames.All, forcesAll);
            field.AddComponent(FOComponentNames.F1, forces1);
            field.AddComponent(FOComponentNames.F2, forces2);
            field.AddComponent(FOComponentNames.F3, forces3);
            fieldData.Unit = unitSystem.ForceUnitAbbreviation + "/" + unitSystem.VolumeUnitAbbreviation;
            results.AddField(fieldData, field);
            //
            return results;
        }
    }
}
