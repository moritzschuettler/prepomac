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

namespace CaeModel
{
    [Serializable]
    public class InitialAngularVelocity : InitialCondition, IContainsEquations, IPreviewable
    {

        // Variables                                                                                                                
        private EquationContainer _x;
        private EquationContainer _y;
        private EquationContainer _z;
        private EquationContainer _n1;
        private EquationContainer _n2;
        private EquationContainer _n3;
        private EquationContainer _rotationalSpeed;


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
        // IContainsEquations
        public void CheckEquations()
        {
            _x.CheckEquation();
            _y.CheckEquation();
            _z.CheckEquation();
            _n1.CheckEquation();
            _n2.CheckEquation();
            _n3.CheckEquation();
            _rotationalSpeed.CheckEquation();
        }
        public virtual bool TryCheckEquations()
        {
            try
            {
                CheckEquations();
                return true;
            }
            catch (Exception ex) { return false; }
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
            Vec3D point = new Vec3D(_x.Value, _y.Value, _z.Value);
            Vec3D normal = new Vec3D(_n1.Value, _n2.Value, _n3.Value);
            normal.Normalize();
            //
            float[] values1 = new float[allData.Nodes.Coor.Length];
            float[] values2 = new float[allData.Nodes.Coor.Length];
            float[] values3 = new float[allData.Nodes.Coor.Length];
            float[] valuesAll = new float[allData.Nodes.Coor.Length];
            //
            double t;
            double omega = _rotationalSpeed.Value;
            Vec3D node;
            Vec3D pointToNode;
            Vec3D axisPoint;
            Vec3D r;
            Vec3D v;
            for (int i = 0; i < allData.Nodes.Coor.Length; i++)
            {
                if (nodeIds.Contains(allData.Nodes.Ids[i]))
                {
                    node = new Vec3D(allData.Nodes.Coor[i]);
                    pointToNode = node - point;
                    t = Vec3D.DotProduct(pointToNode, normal);
                    axisPoint = normal * t;
                    r = normal - axisPoint;
                    v = Vec3D.CrossProduct(normal, r) * omega;
                    //
                    values1[i] = (float)v.X;
                    values2[i] = (float)v.Y;
                    values3[i] = (float)v.Z;
                    valuesAll[i] = (float)v.Len;
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
    }
}
