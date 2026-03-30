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

namespace CaeModel
{
    [Serializable]
    public class CompressionOnly : Constraint, IMultiRegion, ISerializable
    {
        // Variables                                                                                                                
        private EquationContainer _clearance;                       //ISerializable
        private EquationContainer _springStiffness;                 //ISerializable
        private EquationContainer _tensileForceAtNegativeInfinity;  //ISerializable
        private EquationContainer _offset;                          //ISerializable
        private bool _nonLinear;                                    //ISerializable


        // Properties                                                                                                               
        public string RegionName { get { return MasterRegionName; } set { MasterRegionName = value; } }
        public RegionTypeEnum RegionType { get { return MasterRegionType; } set { MasterRegionType = value; } }
        //
        public int[] CreationIds { get { return MasterCreationIds; } set { MasterCreationIds = value; } }
        public Selection CreationData { get { return MasterCreationData; } set { MasterCreationData = value; } }
        //
        public EquationContainer Clearance { get { return _clearance; } set { SetClearance(value); } }
        public EquationContainer SpringStiffness { get { return _springStiffness; } set { SetSpringStiffness(value); } }
        public EquationContainer TensileForceAtNegativeInfinity
        {
            get { return _tensileForceAtNegativeInfinity; }
            set { SetTensileForceAtNegativeInfinity(value); }
        }
        public EquationContainer Offset { get { return _offset; } set { SetOffset(value); } }
        public bool NonLinear { get { return _nonLinear; } set { _nonLinear = value; } }


        // Constructors                                                                                                             
        public CompressionOnly(string name, string regionName, RegionTypeEnum regionType, bool twoD)
            : base(name, regionName, regionType, "", RegionTypeEnum.None, twoD)
        {
            Clearance = new EquationContainer(typeof(StringLengthConverter), 0);
            SpringStiffness = new EquationContainer(typeof(StringForcePerLengthDefaultConverter), double.NaN);
            TensileForceAtNegativeInfinity = new EquationContainer(typeof(StringForceDefaultConverter), double.NaN);
            Offset = new EquationContainer(typeof(StringLengthConverter), 0);
            _nonLinear = false;
        }
        public CompressionOnly(SerializationInfo info, StreamingContext context)
           : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_clearance":
                        SetClearance((EquationContainer)entry.Value, false);
                        break;
                    case "_springStiffness":
                        SetSpringStiffness((EquationContainer)entry.Value, false);
                        break;
                    case "_tensileForceAtNegInfinity":
                        SetTensileForceAtNegativeInfinity((EquationContainer)entry.Value, false);
                        break;
                    case "_offset":
                        SetOffset((EquationContainer)entry.Value, false);
                        break;
                    case "_nonLinear":
                        _nonLinear = (bool)entry.Value;
                        break;
                    default:
                        break;
                }
            }
            // Compatibility for version v1.5.3
            if (_offset == null) Offset = new EquationContainer(typeof(StringLengthConverter), 0);
            // Compatibility for version v2.2.11
            if (_clearance == null) Clearance = new EquationContainer(typeof(StringLengthConverter), 0);
        }


        // Methods                                                                                                                  
        private void SetClearance(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _clearance, value, null, checkEquation);
        }
        private void SetSpringStiffness(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _springStiffness, value, null, checkEquation);
        }
        private void SetTensileForceAtNegativeInfinity(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _tensileForceAtNegativeInfinity, value, null, checkEquation);
        }
        private void SetOffset(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _offset, value, null, checkEquation);
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            base.CheckEquations();
            //
            _clearance.CheckEquation();
            _springStiffness.CheckEquation();
            _tensileForceAtNegativeInfinity.CheckEquation();
            _offset.CheckEquation();
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_clearance", _clearance, typeof(EquationContainer));
            info.AddValue("_springStiffness", _springStiffness, typeof(EquationContainer));
            info.AddValue("_tensileForceAtNegInfinity", _tensileForceAtNegativeInfinity, typeof(EquationContainer));
            info.AddValue("_offset", _offset, typeof(EquationContainer));
            info.AddValue("_nonLinear", _nonLinear, typeof(bool));
        }
    }
}
