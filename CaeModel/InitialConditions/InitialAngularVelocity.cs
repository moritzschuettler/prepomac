using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using System.ComponentModel;
using CaeGlobals;
using CaeResults;
using System.Data;
using System.Runtime.Serialization;
using QuantumConcepts.Formats.StereoLithography;
using System.Xml.Linq;

namespace CaeModel
{
    [Serializable]
    public class InitialAngularVelocity : InitialCondition, IPreviewable, ISerializable
    {

        // Variables                                                                                                                
        private EquationContainer _x;                   //ISerializable
        private EquationContainer _y;                   //ISerializable
        private EquationContainer _z;                   //ISerializable
        private EquationContainer _n1;                  //ISerializable
        private EquationContainer _n2;                  //ISerializable
        private EquationContainer _n3;                  //ISerializable
        private EquationContainer _rotationalSpeed;     //ISerializable


        // Properties                                                                                                               
        public EquationContainer X { get { return _x; } set { SetX(value); } }
        public EquationContainer Y { get { return _y; } set { SetY(value); } }
        public EquationContainer Z { get { return _z; } set { SetZ(value); } }
        public EquationContainer N1 { get { return _n1; } set { SetN1(value); } }
        public EquationContainer N2 { get { return _n2; } set { SetN2(value); } }
        public EquationContainer N3 { get { return _n3; } set { SetN3(value); } }
        public double RotationalSpeed2 { get { return Math.Pow(_rotationalSpeed.Value, 2); } }
        public EquationContainer RotationalSpeed { get { return _rotationalSpeed; } set { SetRotationalSpeed(value); } }


        // Constructors                                                                                                             
        public InitialAngularVelocity(string name, string regionName, RegionTypeEnum regionType, bool twoD)
            : this(name, regionName, regionType, new double[] { 0, 0, 0 }, new double[] { 0, 0, 0 }, 0, twoD)
        {
        }
        public InitialAngularVelocity(string name, string regionName, RegionTypeEnum regionType, double[] point, double[] normal,
                                      double rotationalSpeed, bool twoD)
            : base(name, regionName, regionType, twoD)
        {
            X = new EquationContainer(typeof(StringLengthConverter), point[0]);
            Y = new EquationContainer(typeof(StringLengthConverter), point[1]);
            Z = new EquationContainer(typeof(StringLengthConverter), point[2]);
            //
            N1 = new EquationContainer(typeof(StringLengthConverter), normal[0]);
            N2 = new EquationContainer(typeof(StringLengthConverter), normal[1]);
            N3 = new EquationContainer(typeof(StringLengthConverter), normal[2]);
            //
            RotationalSpeed = new EquationContainer(typeof(StringRotationalSpeedConverter), rotationalSpeed);
        }
        public InitialAngularVelocity(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_x":
                        SetX((EquationContainer)entry.Value, false); break;
                    case "_y":
                        SetY((EquationContainer)entry.Value, false); break;
                    case "_z":
                        SetZ((EquationContainer)entry.Value, false); break;
                    case "_n1":
                        SetN1((EquationContainer)entry.Value, false); break;
                    case "_n2":
                        SetN2((EquationContainer)entry.Value, false); break;
                    case "_n3":
                        SetN3((EquationContainer)entry.Value, false); break;
                    case "_rotationalSpeed":
                        SetRotationalSpeed((EquationContainer)entry.Value, false); break;
                    default:
                        break;
                }
            }
        }

        // Methods                                                                                                                  
        private void SetX(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _x, value, null, checkEquation);
        }
        private void SetY(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _y, value, null, checkEquation);
        }
        private void SetZ(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _z, value, CheckTwoD, checkEquation);
        }
        private void SetN1(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _n1, value, CheckTwoD, checkEquation);
        }
        private void SetN2(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _n2, value, CheckTwoD, checkEquation);
        }
        private void SetN3(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _n3, value, null, checkEquation);
        }
        private void SetRotationalSpeed(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _rotationalSpeed, value, CheckNonNegative, checkEquation);
        }
        //
        private double CheckTwoD(double value)
        {
            if (_twoD) return 0;
            else return value;
        }
        private double CheckNonNegative(double value)
        {
            if (value < 0) throw new CaeException("The value of the rotational speed must be non-negative.");
            else return value;
        }
        public double[] GetDirection()
        {
            return new double[] { _n1.Value, _n2.Value, _n3.Value };
        }
        public double[] GetPosition()
        {
            return new double[] { _x.Value, _y.Value, _z.Value };
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            base.CheckEquations();
            //
            _x.CheckEquation();
            _y.CheckEquation();
            _z.CheckEquation();
            _n1.CheckEquation();
            _n2.CheckEquation();
            _n3.CheckEquation();
            _rotationalSpeed.CheckEquation();
        }
        // IPreviewable
        public FeResults GetPreview(FeMesh targetMesh, string resultName, UnitSystem unitSystem)
        {
            PartExchangeData allData = new PartExchangeData();
            targetMesh.GetAllNodesAndCells(out allData.Nodes.Ids, out allData.Nodes.Coor, out allData.Cells.Ids,
                                           out allData.Cells.CellNodeIds, out allData.Cells.Types);
            //
            HashSet<int> nodeIds;
            if (RegionType == RegionTypeEnum.NodeSetName)
            {
                nodeIds = new HashSet<int>(targetMesh.NodeSets[RegionName].Labels);
            }
            else if (RegionType == RegionTypeEnum.SurfaceName)
            {
                string nodeSetName = targetMesh.Surfaces[RegionName].NodeSetName;
                nodeIds = new HashSet<int>(targetMesh.NodeSets[nodeSetName].Labels);
            }
            else if (RegionType == RegionTypeEnum.ReferencePointName)
            {
                nodeIds = new HashSet<int>();
            }
            else throw new NotSupportedException();
            //
            Dictionary<int, double[]> nodeIdCoor = new Dictionary<int, double[]>();
            foreach (var nodeId in nodeIds) nodeIdCoor[nodeId] = allData.Nodes.Coor[nodeId];
            //
            Dictionary<int, double[]> nodeIdVelocity;
            GetTranslationalVelocities(nodeIdCoor, out nodeIdVelocity);
            //
            double[] velocity;
            float[] values1 = new float[allData.Nodes.Coor.Length];
            float[] values2 = new float[allData.Nodes.Coor.Length];
            float[] values3 = new float[allData.Nodes.Coor.Length];
            float[] valuesAll = new float[allData.Nodes.Coor.Length];
            //
            for (int i = 0; i < allData.Nodes.Coor.Length; i++)
            {
                if (nodeIdVelocity.TryGetValue(allData.Nodes.Ids[i], out velocity))
                {
                    values1[i] = (float)velocity[0];
                    values2[i] = (float)velocity[1];
                    values3[i] = (float)velocity[2];
                    valuesAll[i] = (float)Math.Sqrt(Math.Pow(velocity[0], 2) + Math.Pow(velocity[1], 2) + Math.Pow(velocity[2], 2));
                }
                else
                {
                    values1[i] = float.NaN;
                    values2[i] = float.NaN;
                    values3[i] = float.NaN;
                    valuesAll[i] = float.NaN;
                }
            }
            //
            Dictionary<int, int> nodeIdsLookUp = new Dictionary<int, int>();
            for (int i = 0; i < allData.Nodes.Coor.Length; i++) nodeIdsLookUp.Add(allData.Nodes.Ids[i], i);
            FeResults results = new FeResults(resultName, unitSystem);
            results.SetMesh(targetMesh, nodeIdsLookUp);
            // Add group
            FieldData fieldData = new FieldData(FOFieldNames.Velo);
            fieldData.GlobalIncrementId = 1;
            fieldData.StepType = StepTypeEnum.Static;
            fieldData.Time = 1;
            fieldData.MethodId = 1;
            fieldData.StepId = 1;
            fieldData.StepIncrementId = 1;
            // Add values
            Field field = new Field(fieldData.Name);
            field.AddComponent(FOComponentNames.All, valuesAll);
            field.AddComponent(FOComponentNames.V1, values1);
            field.AddComponent(FOComponentNames.V2, values2);
            field.AddComponent(FOComponentNames.V3, values3);
            results.AddField(fieldData, field);
            //
            return results;
        }
        public void GetTranslationalVelocities(Dictionary<int, double[]> nodeIdCoor, out Dictionary<int, double[]> nodeIdVelocity)
        {
            double t;
            double omega = _rotationalSpeed.Value;
            Vec3D node;
            Vec3D pointToNode;
            Vec3D axisPoint;
            Vec3D r;
            Vec3D v;
            //
            Vec3D point = new Vec3D(_x.Value, _y.Value, _z.Value);
            Vec3D normal = new Vec3D(_n1.Value, _n2.Value, _n3.Value);
            normal.Normalize();
            nodeIdVelocity = new Dictionary<int, double[]>();
            //
            foreach (var entry in nodeIdCoor)
            {
                node = new Vec3D(entry.Value);
                pointToNode = node - point;
                t = Vec3D.DotProduct(pointToNode, normal);
                axisPoint = point + normal * t;
                r = node - axisPoint;
                if (r.Len2 > 1E-3)
                {
                    v = Vec3D.CrossProduct(normal, r) * omega;
                    nodeIdVelocity[entry.Key] = v.Coor;
                }
                else
                {
                    v = normal * omega;
                    nodeIdVelocity[-entry.Key] = v.Coor;    // negative node id
                }
            }
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_x", _x, typeof(EquationContainer));
            info.AddValue("_y", _y, typeof(EquationContainer));
            info.AddValue("_z", _z, typeof(EquationContainer));
            info.AddValue("_n1", _n1, typeof(EquationContainer));
            info.AddValue("_n2", _n2, typeof(EquationContainer));
            info.AddValue("_n3", _n3, typeof(EquationContainer));
            info.AddValue("_rotationalSpeed", _rotationalSpeed, typeof(EquationContainer));
        }
    }
}
