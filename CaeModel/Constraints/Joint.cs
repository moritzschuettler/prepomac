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

namespace CaeModel
{
    [Serializable]
    public class Joint : Constraint, ISerializable
    {
        // Variables
        private JointTypeEnum _jointType;                   //ISerializable
        private JointCenterDefinitionEnum _centerDefinition;//ISerializable
        private int[] _centerCreationIds;                   //ISerializable
        private Selection _centerCreationData;              //ISerializable
        private EquationContainer _x;                       //ISerializable
        private EquationContainer _y;                       //ISerializable
        private EquationContainer _z;                       //ISerializable
        private JointDefinitionEnum _axisDefinition;        //ISerializable
        private EquationContainer _n1;                      //ISerializable
        private EquationContainer _n2;                      //ISerializable
        private EquationContainer _n3;                      //ISerializable
        private JointFormulationEnum _formulation;          //ISerializable
        private JointStiffnessDefinitionEnum _stiffnessDefinition;  //ISerializable
        private EquationContainer _stiffness;               //ISerializable


        // Properties
        public JointTypeEnum JointType { get { return _jointType; } set { _jointType = value; } }
        public JointCenterDefinitionEnum CenterDefinition
        {
            get { return _centerDefinition; }
            set { _centerDefinition = value; }
        }
        public int[] CenterCreationIds { get { return _centerCreationIds; } set { _centerCreationIds = value; } }
        public Selection CenterCreationData { get { return _centerCreationData; } set { _centerCreationData = value; } }
        public EquationContainer X { get { return _x; } set { SetX(value); } }
        public EquationContainer Y { get { return _y; } set { SetY(value); } }
        public EquationContainer Z { get { return _z; } set { SetZ(value); } }
        public JointDefinitionEnum AxisDefinition { get { return _axisDefinition; } set { _axisDefinition = value; } }
        public EquationContainer N1 { get { return _n1; } set { SetN1(value); } }
        public EquationContainer N2 { get { return _n2; } set { SetN2(value); } }
        public EquationContainer N3 { get { return _n3; } set { SetN3(value); } }
        public bool IsCenterFromSelection
        {
            get
            {
                return _centerDefinition == JointCenterDefinitionEnum.OnPoint ||
                       _centerDefinition == JointCenterDefinitionEnum.BetweenTwoPoints ||
                       _centerDefinition == JointCenterDefinitionEnum.CircleCenter;
            }
        }
        public int NumberOfCenterNodes
        {
            get
            {
                if (_centerDefinition == JointCenterDefinitionEnum.OnPoint) return 1;
                else if (_centerDefinition == JointCenterDefinitionEnum.BetweenTwoPoints) return 2;
                else if (_centerDefinition == JointCenterDefinitionEnum.CircleCenter) return 3;
                else return -1;
            }
        }
        public bool HasAxis { get { return _jointType != JointTypeEnum.Ball && _jointType != JointTypeEnum.Weld; } }
        public bool HasUserDefinedAxis { get { return HasAxis && _axisDefinition == JointDefinitionEnum.UserDefined; } }
        public JointFormulationEnum Formulation { get { return _formulation; } set { _formulation = value; } }
        public JointStiffnessDefinitionEnum StiffnessDefinition
        {
            get { return _stiffnessDefinition; }
            set { _stiffnessDefinition = value; }
        }
        public EquationContainer Stiffness { get { return _stiffness; } set { SetStiffness(value); } }
        public bool HasSprings { get { return _formulation == JointFormulationEnum.Springs; } }
        public bool HasUserDefinedStiffness
        {
            get { return HasSprings && _stiffnessDefinition == JointStiffnessDefinitionEnum.UserDefined; }
        }


        // Constructors
        public Joint(string name, string masterSurfaceName, RegionTypeEnum masterRegionType,
                     string slaveSurfaceName, RegionTypeEnum slaveRegionType, bool twoD)
            : this(name, JointTypeEnum.Hinge, JointCenterDefinitionEnum.MasterSurface, new double[] { 0, 0, 0 },
                   JointDefinitionEnum.MasterSurface, new double[] { 0, 0, 1 },
                   masterSurfaceName, masterRegionType, slaveSurfaceName, slaveRegionType, twoD)
        {
        }
        public Joint(string name, JointTypeEnum jointType, JointCenterDefinitionEnum centerDefinition, double[] center,
                     JointDefinitionEnum axisDefinition, double[] axisDirection,
                     string masterSurfaceName, RegionTypeEnum masterRegionType,
                     string slaveSurfaceName, RegionTypeEnum slaveRegionType, bool twoD)
            : base(name, masterSurfaceName, masterRegionType, slaveSurfaceName, slaveRegionType, twoD)
        {
            if (twoD) throw new CaeException("Joints are supported only for 3D models.");
            if (masterRegionType == RegionTypeEnum.SurfaceName && slaveRegionType == RegionTypeEnum.SurfaceName &&
                slaveSurfaceName == masterSurfaceName) throw new CaeException("Master and slave surface names must be different.");
            if (center == null || center.Length != 3)
                throw new ArgumentException("The joint centre must contain three components.", nameof(center));
            if (axisDirection == null || axisDirection.Length != 3)
                throw new ArgumentException("The joint axis direction must contain three components.", nameof(axisDirection));
            //
            _jointType = jointType;
            _centerDefinition = centerDefinition;
            X = new EquationContainer(typeof(StringLengthConverter), center[0]);
            Y = new EquationContainer(typeof(StringLengthConverter), center[1]);
            Z = new EquationContainer(typeof(StringLengthConverter), center[2]);
            //
            _axisDefinition = axisDefinition;
            N1 = new EquationContainer(typeof(StringDoubleConverter), axisDirection[0]);
            N2 = new EquationContainer(typeof(StringDoubleConverter), axisDirection[1]);
            N3 = new EquationContainer(typeof(StringDoubleConverter), axisDirection[2]);
            CheckAxisDirection();
            //
            _formulation = JointFormulationEnum.RigidBody;
            _stiffnessDefinition = JointStiffnessDefinitionEnum.FromModel;
            Stiffness = new EquationContainer(typeof(StringForcePerLengthConverter), 0);
        }
        public Joint(string name, JointTypeEnum jointType, JointCenterDefinitionEnum centerDefinition, double[] center,
                     JointDefinitionEnum axisDefinition, double[] axisDirection, JointFormulationEnum formulation,
                     JointStiffnessDefinitionEnum stiffnessDefinition, double stiffness,
                     string masterSurfaceName, RegionTypeEnum masterRegionType,
                     string slaveSurfaceName, RegionTypeEnum slaveRegionType, bool twoD)
            : this(name, jointType, centerDefinition, center, axisDefinition, axisDirection,
                   masterSurfaceName, masterRegionType, slaveSurfaceName, slaveRegionType, twoD)
        {
            _formulation = formulation;
            _stiffnessDefinition = stiffnessDefinition;
            Stiffness = new EquationContainer(typeof(StringForcePerLengthConverter), stiffness);
            CheckStiffness();
        }
        public Joint(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // Defaults make the object tolerant of fields added in later versions.
            _jointType = JointTypeEnum.Hinge;
            _centerDefinition = JointCenterDefinitionEnum.MasterSurface;
            X = new EquationContainer(typeof(StringLengthConverter), 0);
            Y = new EquationContainer(typeof(StringLengthConverter), 0);
            Z = new EquationContainer(typeof(StringLengthConverter), 0);
            _axisDefinition = JointDefinitionEnum.MasterSurface;
            N1 = new EquationContainer(typeof(StringDoubleConverter), 0);
            N2 = new EquationContainer(typeof(StringDoubleConverter), 0);
            N3 = new EquationContainer(typeof(StringDoubleConverter), 1);
            _formulation = JointFormulationEnum.RigidBody;
            _stiffnessDefinition = JointStiffnessDefinitionEnum.FromModel;
            Stiffness = new EquationContainer(typeof(StringForcePerLengthConverter), 0);
            //
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_formulation":
                        _formulation = (JointFormulationEnum)entry.Value; break;
                    case "_stiffnessDefinition":
                        _stiffnessDefinition = (JointStiffnessDefinitionEnum)entry.Value; break;
                    case "_stiffness":
                        SetStiffness((EquationContainer)entry.Value, false); break;
                    case "_jointType":
                        _jointType = (JointTypeEnum)entry.Value; break;
                    case "_centerDefinition":
                        _centerDefinition = (JointCenterDefinitionEnum)entry.Value; break;
                    case "_centerCreationIds":
                        _centerCreationIds = (int[])entry.Value; break;
                    case "_centerCreationData":
                        _centerCreationData = (Selection)entry.Value; break;
                    case "_x":
                        SetX((EquationContainer)entry.Value, false); break;
                    case "_y":
                        SetY((EquationContainer)entry.Value, false); break;
                    case "_z":
                        SetZ((EquationContainer)entry.Value, false); break;
                    case "_axisDefinition":
                        _axisDefinition = (JointDefinitionEnum)entry.Value; break;
                    case "_n1":
                        SetN1((EquationContainer)entry.Value, false); break;
                    case "_n2":
                        SetN2((EquationContainer)entry.Value, false); break;
                    case "_n3":
                        SetN3((EquationContainer)entry.Value, false); break;
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
            EquationContainer.SetAndCheck(ref _z, value, null, checkEquation);
        }
        private void SetN1(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _n1, value, null, checkEquation);
        }
        private void SetN2(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _n2, value, null, checkEquation);
        }
        private void SetN3(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _n3, value, null, checkEquation);
        }
        private void SetStiffness(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _stiffness, value, null, checkEquation);
        }
        private void CheckAxisDirection()
        {
            if (!HasUserDefinedAxis) return;
            //
            double lengthSquared = _n1.Value * _n1.Value + _n2.Value * _n2.Value + _n3.Value * _n3.Value;
            if (double.IsNaN(lengthSquared) || double.IsInfinity(lengthSquared) || lengthSquared <= 1E-24)
                throw new CaeException("The joint axis direction must be non-zero.");
        }
        private void CheckStiffness()
        {
            if (!HasUserDefinedStiffness) return;
            //
            double stiffness = _stiffness.Value;
            if (double.IsNaN(stiffness) || double.IsInfinity(stiffness) || stiffness <= 0)
                throw new CaeException("The joint spring stiffness must be positive.");
        }
        public double[] GetCenter()
        {
            return new double[] { _x.Value, _y.Value, _z.Value };
        }
        public double[] GetAxisDirection()
        {
            return new double[] { _n1.Value, _n2.Value, _n3.Value };
        }
        public override void CheckEquations()
        {
            _x.CheckEquation();
            _y.CheckEquation();
            _z.CheckEquation();
            _n1.CheckEquation();
            _n2.CheckEquation();
            _n3.CheckEquation();
            _stiffness.CheckEquation();
            //
            CheckAxisDirection();
            CheckStiffness();
        }
        //
        public void SwapMasterSlave()
        {
            if (_name.Contains(Globals.MasterSlaveSeparator))
            {
                string[] tmp = _name.Split(new string[] { Globals.MasterSlaveSeparator }, StringSplitOptions.None);
                if (tmp.Length == 2) _name = tmp[1] + Globals.MasterSlaveSeparator + tmp[0];
            }
            //
            RegionTypeEnum tmpRegionType = MasterRegionType;
            MasterRegionType = SlaveRegionType;
            SlaveRegionType = tmpRegionType;
            //
            string tmpSurfaceName = MasterRegionName;
            MasterRegionName = SlaveRegionName;
            SlaveRegionName = tmpSurfaceName;
            //
            int[] tmpCreationIds = MasterCreationIds;
            MasterCreationIds = SlaveCreationIds;
            SlaveCreationIds = tmpCreationIds;
            //
            Selection tmpCreationData = MasterCreationData;
            MasterCreationData = SlaveCreationData;
            SlaveCreationData = tmpCreationData;
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_jointType", _jointType, typeof(JointTypeEnum));
            info.AddValue("_centerDefinition", _centerDefinition, typeof(JointCenterDefinitionEnum));
            info.AddValue("_centerCreationIds", _centerCreationIds, typeof(int[]));
            info.AddValue("_centerCreationData", _centerCreationData, typeof(Selection));
            info.AddValue("_x", _x, typeof(EquationContainer));
            info.AddValue("_y", _y, typeof(EquationContainer));
            info.AddValue("_z", _z, typeof(EquationContainer));
            info.AddValue("_axisDefinition", _axisDefinition, typeof(JointDefinitionEnum));
            info.AddValue("_n1", _n1, typeof(EquationContainer));
            info.AddValue("_n2", _n2, typeof(EquationContainer));
            info.AddValue("_n3", _n3, typeof(EquationContainer));
            info.AddValue("_formulation", _formulation, typeof(JointFormulationEnum));
            info.AddValue("_stiffnessDefinition", _stiffnessDefinition, typeof(JointStiffnessDefinitionEnum));
            info.AddValue("_stiffness", _stiffness, typeof(EquationContainer));
        }
    }
}
