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


namespace CaeModel
{
    [Serializable]
    public class ComplexFrequencyStep : Step, ISerializable
    {
        // Variables                                                                                                                
        private int _numOfFrequencies;          //ISerializable


        // Properties                                                                                                               
        public int NumOfFrequencies
        {
            get { return _numOfFrequencies; }
            set 
            {
                if (value <= 0) throw new Exception("The number of frequencies must be larger than 0.");
                _numOfFrequencies = value;
            }
        }


        // Constructors                                                                                                             
        public ComplexFrequencyStep(string name)
            :base(name)
        {
            _numOfFrequencies = 10;
            //
            AddFieldOutput(new NodalFieldOutput("NF-Output-1", NodalFieldVariable.U | NodalFieldVariable.PU));
            AddFieldOutput(new ElementFieldOutput("EF-Output-1", ElementFieldVariable.E | ElementFieldVariable.S));
        }
        //ISerializable
        public ComplexFrequencyStep(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_numOfFrequencies":
                        _numOfFrequencies = (int)entry.Value; break;
                }
            }
        }


        // Methods                                                                                                                  
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
        }
    }
}
