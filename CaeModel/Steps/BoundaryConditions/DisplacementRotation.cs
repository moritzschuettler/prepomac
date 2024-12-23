using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.Runtime.Serialization;
using System.Drawing;

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
    public class DisplacementRotation : BoundaryCondition, ISerializable
    {
        // Variables                                                                                                                
        private EquationContainer _u1;         //ISerializable
        private EquationContainer _u2;         //ISerializable
        private EquationContainer _u3;         //ISerializable
        private EquationContainer _ur1;        //ISerializable
        private EquationContainer _ur2;        //ISerializable
        private EquationContainer _ur3;        //ISerializable


        // Properties                                                                                                               
        public EquationContainer U1 { get { return _u1; } set { SetU1(value); } }
        public EquationContainer U2 { get { return _u2; } set { SetU2(value); } }
        public EquationContainer U3 { get { return _u3; } set { SetU3(value); } }
        public EquationContainer UR1 { get { return GetUR(_ur1); } set { SetUR1(value); } }
        public EquationContainer UR2 { get { return GetUR(_ur2); } set { SetUR2(value); } }
        public EquationContainer UR3 { get { return GetUR(_ur3); } set { SetUR3(value); } }


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
        }
        public DisplacementRotation(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_u1":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueU1)
                            U1 = new EquationContainer(typeof(StringLengthDOFConverter), valueU1);
                        else
                            SetU1((EquationContainer)entry.Value, false);
                        break;
                    case "_u2":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueU2)
                            U2 = new EquationContainer(typeof(StringLengthDOFConverter), valueU2);
                        else
                            SetU2((EquationContainer)entry.Value, false);
                        break;
                    case "_u3":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueU3)
                            U3 = new EquationContainer(typeof(StringLengthDOFConverter), valueU3);
                        else
                            SetU3((EquationContainer)entry.Value, false);
                        break;
                    case "_ur1":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueUR1)
                            UR1 = new EquationContainer(typeof(StringAngleDOFConverter), valueUR1);
                        else
                            SetUR1((EquationContainer)entry.Value, false);
                        break;
                    case "_ur2":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueUR2)
                            UR2 = new EquationContainer(typeof(StringAngleDOFConverter), valueUR2);
                        else
                            SetUR2((EquationContainer)entry.Value, false);
                        break;
                    case "_ur3":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueUR3)
                            UR3 = new EquationContainer(typeof(StringAngleDOFConverter), valueUR3);
                        else
                            SetUR3((EquationContainer)entry.Value, false);
                        break;
                    default:
                        break;
                }
            }
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
        }
    }
}
