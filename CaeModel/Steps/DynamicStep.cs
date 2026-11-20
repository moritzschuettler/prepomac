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
using DynamicTypeDescriptor;
using CaeGlobals;

namespace CaeModel
{
    [PmxType(3231)]
    public enum SolutionProcedureEnum
    {
        [StandardValue("ImplicitImplicit", DisplayName = "Implicit/Implicit")]
        ImplicitImplicit = 0,
        //
        [StandardValue("ImplicitExplicit", DisplayName = "Implicit/Explicit")]
        ImplicitExplicit = 1,
        //
        [StandardValue("ExplicitImplicit", DisplayName = "Explicit/Implicit")]
        ExplicitImplicit = 2,
        //
        [StandardValue("ExplicitExplicit", DisplayName = "Explicit/Explicit")]
        ExplicitExplicit = 3
    }

    [PmxType(3232)]
    public class DynamicStep : StaticStep
    {
        // Variables                                                                                                                
        public static readonly double AlphaDefault = -0.05;
        [PmxKey(0)] private double _alpha;                              //ISerializable
        [PmxKey(1)] private SolutionProcedureEnum _solutionProcedure;   //ISerializable
        [PmxKey(2)] private bool _relativeToAbsolute;                   //ISerializable
        [PmxKey(3)] private Damping _damping;                           //ISerializable


        // Properties                                                                                                               
        public double Alpha
        {
            get { return _alpha; }
            set
            {
                _alpha = value;
                if (_alpha > 0) _alpha = 0;
                else if (_alpha < -1.0 / 3.0) _alpha = -1.0 / 3.0;
            }
        }
        public SolutionProcedureEnum SolutionProcedure { get { return _solutionProcedure; } set { _solutionProcedure = value; } }
        public bool RelativeToAbsolute { get { return _relativeToAbsolute; } set { _relativeToAbsolute = value; } }
        public Damping Damping { get { return _damping; } set { _damping = value; } }


        // Constructors                                                                                                             
        public DynamicStep(string name)
            : this(name, true)
        {
        }
        public DynamicStep(string name, bool addFieldOutputs)
            : base(name, false)
        {
            _alpha = AlphaDefault;
            _solutionProcedure = SolutionProcedureEnum.ImplicitImplicit;
            _relativeToAbsolute = false;
            _damping = new Damping();
            _incrementationType = IncrementationTypeEnum.Automatic;
            //
            if (addFieldOutputs)
            {
                AddFieldOutput(new NodalFieldOutput("NF-Output-1", NodalFieldVariable.RF | NodalFieldVariable.U |
                                                    NodalFieldVariable.V));
                AddFieldOutput(new ElementFieldOutput("EF-Output-1", ElementFieldVariable.E | ElementFieldVariable.S |
                                                      ElementFieldVariable.ENER));
            }
        }
        //ISerializable
        public DynamicStep(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_alpha":
                        _alpha = (double)entry.Value; break;
                    case "_explicit":
                        _solutionProcedure = (SolutionProcedureEnum)entry.Value; break;
                    case "_relativeToAbsolute":
                        _relativeToAbsolute = (bool)entry.Value; break;
                    case "_damping":
                        _damping = (Damping)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        public override bool IsPerturbationSupported()
        {
            return false;
        }
        public override bool IsBoundaryConditionSupported(BoundaryCondition boundaryCondition)
        {
            if (boundaryCondition is FixedBC ||
                boundaryCondition is DisplacementRotation ||
                boundaryCondition is SubmodelBC)
                return true;
            else if (boundaryCondition is TemperatureBC)
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
                loadType == typeof(PreTensionLoad))
            {
                return true;
            }
            else if (loadType == typeof(CFlux) ||
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
            if (definedField is DefinedTemperature) return true;
            else throw new NotSupportedException();
        }
    }
}
