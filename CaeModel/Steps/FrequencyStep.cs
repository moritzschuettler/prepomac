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
using System.Runtime.Serialization;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public class FrequencyStep : Step, ISerializable
    {
        // Variables                                                                                                                
        private int _numOfFrequencies;          //ISerializable
        private double _lowerFrequency;         //ISerializable
        private double _upperFrequency;         //ISerializable
        private bool _storage;                  //ISerializable
        private int _cyclicSymmetryModeMin;     //ISerializable
        private int _cyclicSymmetryModeMax;     //ISerializable


        // Properties                                                                                                               
        public int NumOfFrequencies
        {
            get { return _numOfFrequencies; }
            set 
            {
                if (value <= 0) throw new CaeException("The number of frequencies must be larger than 0.");
                _numOfFrequencies = value;
            }
        }
        public double LowerFrequency
        {
            get { return _lowerFrequency; }
            set
            {
                if (!double.IsNaN(value))
                {
                    if (value < 0) throw new CaeException("The lower frequency value must be larger or equal to 0.");
                    if (!double.IsNaN(_upperFrequency) && value >= _upperFrequency && _upperFrequency != 0)
                        throw new CaeException("The lower frequency value must be smaller than the upper frequency value.");
                }
                _lowerFrequency = value;
            }
        }
        public double UpperFrequency
        {
            get { return _upperFrequency; }
            set
            {
                if (!double.IsNaN(value))
                {
                    if (value < 0) throw new CaeException("The upper frequency value must be larger or equal to 0.");
                    if (!double.IsNaN(_lowerFrequency) && value <= _lowerFrequency && _lowerFrequency != 0)
                        throw new CaeException("The upper frequency value must be larger than the lower frequency value.");
                }
                _upperFrequency = value;
            }
        }
        public bool Storage { get { return _storage; } set { _storage = value; } }
        public int CyclicSymmetryModeMin
        {
            get { return _cyclicSymmetryModeMin; }
            set
            {
                if (value != int.MinValue && value < 0)
                    throw new CaeException("The minimum cyclic symmetry mode must be larger than or equal to 0.");
                if (value != int.MinValue && _cyclicSymmetryModeMax != int.MinValue &&
                    value > _cyclicSymmetryModeMax) _cyclicSymmetryModeMax = value;
                _cyclicSymmetryModeMin = value;
            }
        }
        public int CyclicSymmetryModeMax
        {
            get { return _cyclicSymmetryModeMax; }
            set
            {
                if (value != int.MinValue && value < 0)
                    throw new CaeException("The maximum cyclic symmetry mode must be larger than or equal to 0.");
                if (value != int.MinValue && _cyclicSymmetryModeMin != int.MinValue &&
                    value < _cyclicSymmetryModeMin) _cyclicSymmetryModeMin = value;
                _cyclicSymmetryModeMax = value;
            }
        }


        // Constructors                                                                                                             
        public FrequencyStep(string name)
            :base(name)
        {
            _numOfFrequencies = 10;
            _lowerFrequency = double.NaN;
            _upperFrequency = double.NaN;
            _storage = false;
            _cyclicSymmetryModeMin = int.MinValue;
            _cyclicSymmetryModeMax = int.MinValue;
            //
            AddFieldOutput(new NodalFieldOutput("NF-Output-1", NodalFieldVariable.U));
            AddFieldOutput(new ElementFieldOutput("EF-Output-1", ElementFieldVariable.E | ElementFieldVariable.S));
        }
        //ISerializable
        public FrequencyStep(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _cyclicSymmetryModeMin = int.MinValue;
            _cyclicSymmetryModeMax = int.MinValue;
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_numOfFrequencies":
                        _numOfFrequencies = (int)entry.Value; break;
                    case "_lowerFrequency":
                        _lowerFrequency = (double)entry.Value; break;
                    case "_upperFrequency":
                        _upperFrequency = (double)entry.Value; break;
                    case "_storage":
                        _storage = (bool)entry.Value; break;
                    case "_cyclicSymmetryModeMin":
                        _cyclicSymmetryModeMin = (int)entry.Value; break;
                    case "_cyclicSymmetryModeMax":
                        _cyclicSymmetryModeMax = (int)entry.Value; break;
                }
            }
        }


        // Methods                                                                                                                  
        public void CheckCyclicSymmetryModes(int numberOfSectors, bool containsAxisNodes)
        {
            int highestMode = numberOfSectors / 2;
            int minimumMode = _cyclicSymmetryModeMin == int.MinValue ? 0 : _cyclicSymmetryModeMin;
            int maximumMode = _cyclicSymmetryModeMax == int.MinValue ? highestMode : _cyclicSymmetryModeMax;
            //
            if (minimumMode > highestMode || maximumMode > highestMode)
                throw new CaeException("The cyclic symmetry nodal diameter must not exceed " + highestMode +
                                       " for " + numberOfSectors + " sectors.");
            if (containsAxisNodes &&
                ((minimumMode == 0 && maximumMode != 0) ||
                 (minimumMode == 1 && maximumMode != 1)))
                throw new CaeException("The cyclic symmetry faces share nodes on the symmetry axis. Nodal diameter " +
                                       "0 must be computed as 0 to 0 and nodal diameter 1 as 1 to 1 in separate " +
                                       "frequency steps. Nodal diameters 2 and higher may be computed together.");
        }
        public override bool IsPerturbationSupported()
        {
            return true;
        }
        public override bool IsBoundaryConditionSupported(BoundaryCondition boundaryCondition)
        {
            if (boundaryCondition is FixedBC || 
                boundaryCondition is DisplacementRotation)
                return true;
            else if (boundaryCondition is SubmodelBC ||
                     boundaryCondition is TemperatureBC)
                return false;
            else throw new NotSupportedException();
        }
        public override bool IsLoadTypeSupported(Type loadType)
        {
            if (loadType == typeof(CLoad) ||
                loadType == typeof(MomentLoad) ||
                loadType == typeof(DLoad) ||
                loadType == typeof(HydrostaticPressure) ||
                loadType == typeof(ImportedPressure) ||
                loadType == typeof(STLoad) ||
                loadType == typeof(ImportedSTLoad) ||
                loadType == typeof(ShellEdgeLoad) ||
                loadType == typeof(GravityLoad) ||
                loadType == typeof(CentrifLoad) ||
                loadType == typeof(PreTensionLoad) ||
                loadType == typeof(CFlux) ||
                loadType == typeof(DFlux) ||
                loadType == typeof(BodyFlux) ||
                loadType == typeof(FilmHeatTransfer) ||
                loadType == typeof(RadiationHeatTransfer))
            {
                return false;
            }
            else throw new NotSupportedException();
        }
        public override bool IsDefinedFieldSupported(DefinedField definedField)
        {
            if (definedField is DefinedTemperature) return false;
            else throw new NotSupportedException();
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_numOfFrequencies", _numOfFrequencies, typeof(int));
            info.AddValue("_lowerFrequency", _lowerFrequency, typeof(double));
            info.AddValue("_upperFrequency", _upperFrequency, typeof(double));
            info.AddValue("_storage", _storage, typeof(bool));
            info.AddValue("_cyclicSymmetryModeMin", _cyclicSymmetryModeMin, typeof(int));
            info.AddValue("_cyclicSymmetryModeMax", _cyclicSymmetryModeMax, typeof(int));
        }
    }
}
