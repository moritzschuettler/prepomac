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
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.Runtime.Serialization;
using System.Drawing;
using CaeResults;

namespace CaeModel
{
    [Serializable]
    public enum DOFType
    {
        Free,
        Zero,
        Fixed,
        Prescribed
    }

    [Serializable]
    public class DisplacementRotation : BoundaryCondition, IDistribution, IPreviewable, ISerializable
    {
        // Variables                                                                                                                
        private EquationContainer _u1;         //ISerializable
        private EquationContainer _u2;         //ISerializable
        private EquationContainer _u3;         //ISerializable
        private EquationContainer _ur1;        //ISerializable
        private EquationContainer _ur2;        //ISerializable
        private EquationContainer _ur3;        //ISerializable
        private string _distributionName;      //ISerializable


        // Properties                                                                                                               
        public EquationContainer U1 { get { return _u1; } set { SetU1(value); } }
        public EquationContainer U2 { get { return _u2; } set { SetU2(value); } }
        public EquationContainer U3 { get { return _u3; } set { SetU3(value); } }
        public EquationContainer UR1 { get { return GetUR(_ur1); } set { SetUR1(value); } }
        public EquationContainer UR2 { get { return GetUR(_ur2); } set { SetUR2(value); } }
        public EquationContainer UR3 { get { return GetUR(_ur3); } set { SetUR3(value); } }
        public string DistributionName { get { return _distributionName; } set { _distributionName = value; } }


        // Constructors                                                                                                             
        public DisplacementRotation(string name, string regionName, RegionTypeEnum regionType, bool twoD,
                                    bool complex, double phaseDeg)
            : base(name, regionName, regionType, twoD, complex, phaseDeg)
        {
            U1 = new EquationContainer(typeof(StringLengthDOFConverter), double.NaN);
            U2 = new EquationContainer(typeof(StringLengthDOFConverter), double.NaN);
            U3 = new EquationContainer(typeof(StringLengthDOFConverter), double.NaN);
            UR1 = new EquationContainer(typeof(StringAngleDOFConverter), double.NaN);
            UR2 = new EquationContainer(typeof(StringAngleDOFConverter), double.NaN);
            UR3 = new EquationContainer(typeof(StringAngleDOFConverter), double.NaN);
            _distributionName = Distribution.DefaultDistributionName;
        }
        public DisplacementRotation(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_u1":
                    case "<U1>k__BackingField":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueU1)
                            U1 = new EquationContainer(typeof(StringLengthDOFConverter), valueU1);
                        else
                            SetU1((EquationContainer)entry.Value, false);
                        break;
                    case "_u2":
                    case "<U2>k__BackingField":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueU2)
                            U2 = new EquationContainer(typeof(StringLengthDOFConverter), valueU2);
                        else
                            SetU2((EquationContainer)entry.Value, false);
                        break;
                    case "_u3":
                    case "<U3>k__BackingField":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueU3)
                            U3 = new EquationContainer(typeof(StringLengthDOFConverter), valueU3);
                        else
                            SetU3((EquationContainer)entry.Value, false);
                        break;
                    case "_ur1":
                    case "<UR1>k__BackingField":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueUR1)
                            UR1 = new EquationContainer(typeof(StringAngleDOFConverter), valueUR1);
                        else
                            SetUR1((EquationContainer)entry.Value, false);
                        break;
                    case "_ur2":
                    case "<UR2>k__BackingField":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueUR2)
                            UR2 = new EquationContainer(typeof(StringAngleDOFConverter), valueUR2);
                        else
                            SetUR2((EquationContainer)entry.Value, false);
                        break;
                    case "_ur3":
                    case "<UR3>k__BackingField":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueUR3)
                            UR3 = new EquationContainer(typeof(StringAngleDOFConverter), valueUR3);
                        else
                            SetUR3((EquationContainer)entry.Value, false);
                        break;
                    case "_distributionName":
                        _distributionName = (string)entry.Value; break;
                    default:
                        break;
                }
            }
            // Compatibility for version v2.6.4
            if (_distributionName == null) _distributionName = Distribution.DefaultDistributionName;
        }


        // Methods                                                                                                                  
        private EquationContainer GetUR(EquationContainer ur)
        {
            if (_coordinateSystemName != null) return new EquationContainer(typeof(StringAngleDOFConverter), double.NaN);
            else return ur;
        }
        private void SetU1(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _u1, value, null, checkEquation);
        }
        private void SetU2(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _u2, value, null, checkEquation);
        }
        private void SetU3(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _u3, value, Check2D, checkEquation);
        }
        private void SetUR1(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _ur1, value, Check2D, checkEquation);
        }
        private void SetUR2(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _ur2, value, Check2D, checkEquation);
        }
        private void SetUR3(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _ur3, value, null, checkEquation);
        }
        //
        private double Check2D(double value)
        {
            if (_twoD) return double.NaN;
            else return value;
        }
        //
        public int[] GetConstrainedDirections()
        {
            List<int> directions = new List<int>();
            if (!double.IsNaN(_u1.Value)) directions.Add(1);
            if (!double.IsNaN(_u2.Value)) directions.Add(2);
            if (!double.IsNaN(_u3.Value)) directions.Add(3);
            if (!double.IsNaN(UR1.Value)) directions.Add(4);
            if (!double.IsNaN(UR2.Value)) directions.Add(5);
            if (!double.IsNaN(UR3.Value)) directions.Add(6);
            return directions.ToArray();
        }
        public int GetConstraintHash()
        {
            int[] dir = GetConstrainedDirections();
            int hash = 0;
            for (int i = 0; i < dir.Length; i++) hash += (int)Math.Pow(2, dir[i]);
            return hash;
        }
        public double[] GetConstrainValues()
        {
            List<double> values = new List<double>();
            if (!double.IsNaN(_u1.Value)) values.Add(_u1.Value);
            if (!double.IsNaN(_u2.Value)) values.Add(_u2.Value);
            if (!double.IsNaN(_u3.Value)) values.Add(_u3.Value);
            if (!double.IsNaN(UR1.Value)) values.Add(UR1.Value);
            if (!double.IsNaN(UR2.Value)) values.Add(UR2.Value);
            if (!double.IsNaN(UR3.Value)) values.Add(UR3.Value);
            return values.ToArray();
        }
        public bool IsFreeFixedOrZero()
        {
            for (int i = 1; i <= 6; i++)
            {
                if (GetDofType(i) == DOFType.Prescribed) return false;
            }
            //
            return true;
        }
        public int[] GetFixedDirections()
        {
            List<int> directions = new List<int>();
            if (double.IsPositiveInfinity(_u1.Value)) directions.Add(1);
            if (double.IsPositiveInfinity(_u2.Value)) directions.Add(2);
            if (double.IsPositiveInfinity(_u3.Value)) directions.Add(3);
            if (double.IsPositiveInfinity(UR1.Value)) directions.Add(4);
            if (double.IsPositiveInfinity(UR2.Value)) directions.Add(5);
            if (double.IsPositiveInfinity(UR3.Value)) directions.Add(6);
            return directions.ToArray();
        }
        public bool IsProperlyDefined(out string error)
        {
            error = "";
            if (GetConstrainedDirections().Length + GetFixedDirections().Length == 0)
            {
                error = "At least one degree of freedom must be defined for the boundary condition.";
                return false;
            }
            if (GetFixedDirections().Length != 0 && GetConstrainedDirections().Length != GetFixedDirections().Length)
            {
                error = "Only fixed and unconstrained degrees of freedom can be used at the same time.";
                return false;
            }
            if (_distributionName != Distribution.DefaultDistributionName)
            {
                if (RegionType == RegionTypeEnum.ReferencePointName)
                {
                    error = "A distribution cannot be applied to a reference point.";
                    return false;
                }
                if (IsFreeFixedOrZero())
                {
                    error = "A distribution only scales prescribed nonzero degrees of freedom. " +
                            "Prescribe a nonzero value or set the distribution to " + Distribution.DefaultDistributionName + ".";
                    return false;
                }
            }
            return true;
        }
        public DOFType GetDofType(int dof)
        {
            double value;
            switch (dof)
            {
                case 1: value = _u1.Value; break;
                case 2: value = _u2.Value; break;
                case 3: value = _u3.Value; break;
                case 4: value = UR1.Value; break;
                case 5: value = UR2.Value; break;
                case 6: value = UR3.Value; break;
                default: throw new NotSupportedException();
            }
            if (double.IsNaN(value)) return DOFType.Free;
            else if (double.IsPositiveInfinity(value)) return DOFType.Fixed;
            else if (value == 0) return DOFType.Zero;
            else return DOFType.Prescribed;
        }
        public double[] GetPrescribedUDirection(CoordinateSystem coordinateSystem, double[] coor = null)
        {
            double u1 = 0;
            double u2 = 0;
            double u3 = 0;
            if (GetDofType(1) == DOFType.Prescribed) u1 = U1.Value;
            if (GetDofType(2) == DOFType.Prescribed) u2 = U2.Value;
            if (GetDofType(3) == DOFType.Prescribed) u3 = U3.Value;
            //
            if (coordinateSystem == null) return new double[] { u1, u2, u3 };
            else
            {
                Vec3D directionX = new Vec3D(GetDirectionX(coordinateSystem, coor));
                Vec3D directionY = new Vec3D(GetDirectionY(coordinateSystem, coor));
                Vec3D directionZ = new Vec3D(GetDirectionZ(coordinateSystem, coor));
                Vec3D direction = u1 * directionX + u2 * directionY + u3 * directionZ;
                return direction.Coor;
            }
        }
        public bool IsDistributionTypeSupported(DistributionTypeEnum distributionType, out string error)
        {
            error = "";
            if (distributionType == DistributionTypeEnum.Vector)
            {
                for (int dof = 4; dof <= 6; dof++)
                {
                    if (GetDofType(dof) == DOFType.Prescribed)
                    {
                        error = "A vector distribution scales only U1, U2 and U3. Use a scalar distribution for the " +
                                "boundary condition " + Name + " or do not prescribe its rotations.";
                        return false;
                    }
                }
            }
            return true;
        }
        public double[][] GetConstrainValuesForPoints(FeModel model, double[][] points)
        {
            return GetConstrainValuesAndDistancesForPoints(model, points, out _);
        }
        public double[][] GetConstrainValuesAndDistancesForPoints(FeModel model, double[][] points,
                                                                  out double[][] distances)
        {
            double[] constrainValues = GetConstrainValues();
            double[][] values = new double[points.Length][];
            distances = new double[points.Length][];
            //
            if (_distributionName == Distribution.DefaultDistributionName)
            {
                for (int i = 0; i < points.Length; i++) values[i] = constrainValues.ToArray();
            }
            else
            {
                double[][] factors;
                Distribution distribution = model.Distributions[_distributionName];
                distribution.GetMagnitudesAndDistancesForPoints(model, points, out factors, out distances);
                //
                string error;
                if (!IsDistributionTypeSupported(distribution.DistributionType, out error)) throw new CaeException(error);
                //
                int[] directions = GetConstrainedDirections();
                //
                for (int i = 0; i < points.Length; i++)
                {
                    values[i] = new double[directions.Length];
                    for (int j = 0; j < directions.Length; j++)
                    {
                        if (factors[i].Length == 1) values[i][j] = factors[i][0] * constrainValues[j];
                        else if (factors[i].Length == 3)
                        {
                            if (directions[j] <= 3) values[i][j] = factors[i][directions[j] - 1] * constrainValues[j];
                            else values[i][j] = constrainValues[j];
                        }
                        else throw new NotSupportedException();
                    }
                }
            }
            return values;
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
            FeNodeSet nodeSet;
            if (RegionType == RegionTypeEnum.NodeSetName)
            {
                nodeSet = targetMesh.NodeSets[RegionName];
            }
            else if (RegionType == RegionTypeEnum.SurfaceName)
            {
                FeSurface surface = targetMesh.Surfaces[RegionName];
                nodeSet = targetMesh.NodeSets[surface.NodeSetName];
            }
            else if (RegionType == RegionTypeEnum.ReferencePointName)
                throw new CaeException("A boundary condition applied to a reference point cannot be previewed.");
            else throw new NotSupportedException();
            //
            Dictionary<int, int> nodeIdArrayId = new Dictionary<int, int>();
            double[][] coor = new double[nodeSet.Labels.Length][];
            for (int i = 0; i < nodeSet.Labels.Length; i++)
            {
                nodeIdArrayId[nodeSet.Labels[i]] = i;
                coor[i] = targetMesh.Nodes[nodeSet.Labels[i]].Coor;
            }
            double[][] distances;
            double[][] values = GetConstrainValuesAndDistancesForPoints(model, coor, out distances);
            int[] directions = GetConstrainedDirections();
            bool hasRotations = directions.Any(direction => direction > 3);
            //
            int numOfNodes = allData.Nodes.Coor.Length;
            float[][] dofValues = new float[6][];
            for (int i = 0; i < dofValues.Length; i++) dofValues[i] = new float[numOfNodes];
            float[] displacementsAll = new float[numOfNodes];
            float[] rotationsAll = new float[numOfNodes];
            float[][] distanceValues = new float[3][];
            for (int i = 0; i < distanceValues.Length; i++) distanceValues[i] = new float[numOfNodes];
            float[] distancesAll = new float[numOfNodes];
            //
            Parallel.For(0, numOfNodes, i =>
            {
                int arrayId;
                for (int dof = 0; dof < 6; dof++) dofValues[dof][i] = float.NaN;
                if (nodeIdArrayId.TryGetValue(allData.Nodes.Ids[i], out arrayId))
                {
                    // A fixed degree of freedom does not move
                    for (int j = 0; j < directions.Length; j++)
                    {
                        double value = values[arrayId][j];
                        dofValues[directions[j] - 1][i] = double.IsInfinity(value) ? 0 : (float)value;
                    }
                    displacementsAll[i] = GetMagnitude(dofValues[0][i], dofValues[1][i], dofValues[2][i]);
                    rotationsAll[i] = GetMagnitude(dofValues[3][i], dofValues[4][i], dofValues[5][i]);
                    //
                    if (addDistances)
                    {
                        double[] distance = distances[arrayId];
                        for (int j = 0; j < 3; j++) distanceValues[j][i] = (float)distance[j];
                        distancesAll[i] = (float)Math.Sqrt(distance[0] * distance[0] + distance[1] * distance[1] +
                                                           distance[2] * distance[2]);
                    }
                }
                else
                {
                    displacementsAll[i] = float.NaN;
                    rotationsAll[i] = float.NaN;
                    for (int j = 0; j < 3; j++) distanceValues[j][i] = float.NaN;
                    distancesAll[i] = float.NaN;
                }
            });
            //
            Dictionary<int, int> nodeIdsLookUp = new Dictionary<int, int>();
            for (int i = 0; i < numOfNodes; i++) nodeIdsLookUp.Add(allData.Nodes.Ids[i], i);
            FeResults results = new FeResults(resultName, unitSystem);
            results.SetMesh(targetMesh, nodeIdsLookUp);
            // Prepare field data
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
                field.AddComponent(FOComponentNames.D1, distanceValues[0]);
                field.AddComponent(FOComponentNames.D2, distanceValues[1]);
                field.AddComponent(FOComponentNames.D3, distanceValues[2]);
                results.AddField(fieldData, field);
            }
            // Displacements
            fieldData = new FieldData(fieldData);
            fieldData.Name = FOFieldNames.PrescribedDisp;
            field = new Field(fieldData.Name);
            field.AddComponent(FOComponentNames.All, displacementsAll);
            field.AddComponent(FOComponentNames.U1, dofValues[0]);
            field.AddComponent(FOComponentNames.U2, dofValues[1]);
            field.AddComponent(FOComponentNames.U3, dofValues[2]);
            results.AddField(fieldData, field);
            // Rotations
            if (hasRotations)
            {
                fieldData = new FieldData(fieldData);
                fieldData.Name = FOFieldNames.PrescribedRot;
                field = new Field(fieldData.Name);
                field.AddComponent(FOComponentNames.All, rotationsAll);
                field.AddComponent(FOComponentNames.UR1, dofValues[3]);
                field.AddComponent(FOComponentNames.UR2, dofValues[4]);
                field.AddComponent(FOComponentNames.UR3, dofValues[5]);
                results.AddField(fieldData, field);
            }
            //
            return results;
        }
        private static float GetMagnitude(float value1, float value2, float value3)
        {
            double sum = 0;
            int count = 0;
            foreach (float value in new float[] { value1, value2, value3 })
            {
                if (float.IsNaN(value)) continue;
                sum += value * value;
                count++;
            }
            if (count == 0) return float.NaN;
            else return (float)Math.Sqrt(sum);
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            base.CheckEquations();
            //
            _u1.CheckEquation();
            _u2.CheckEquation();
            _u3.CheckEquation();
            _ur1.CheckEquation();
            _ur2.CheckEquation();
            _ur3.CheckEquation();
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_u1", _u1, typeof(EquationContainer));
            info.AddValue("_u2", _u2, typeof(EquationContainer));
            info.AddValue("_u3", _u3, typeof(EquationContainer));
            info.AddValue("_ur1", _ur1, typeof(EquationContainer));
            info.AddValue("_ur2", _ur2, typeof(EquationContainer));
            info.AddValue("_ur3", _ur3, typeof(EquationContainer));
            info.AddValue("_distributionName", _distributionName, typeof(string));
        }
    }
}
