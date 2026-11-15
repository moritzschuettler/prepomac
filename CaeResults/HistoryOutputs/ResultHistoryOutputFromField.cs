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
using System.ComponentModel;
using CaeGlobals;

namespace CaeResults
{
    [PmxType(5050)] 
    public enum OutputNodeCoordinatesEnum
    {
        Off,
        Undeformed,
        Deformed
    }

    [PmxType(5051)]
    public class ResultHistoryOutputFromField : ResultHistoryOutput
    {
        // Variables                                                                                                                
        public readonly static string AllSteps = "All steps";
        public readonly static string AllIncrements = "All increments";
        //
        [PmxKey(0)] private string _fieldName;
        [PmxKey(1)] private string[] _componentNames;
        [PmxKey(2)] private ComplexResultTypeEnum _complexResultType;
        [PmxKey(3)] private double _complexAngleDeg;    // must be double
        [PmxKey(4)] private int _stepId;
        [PmxKey(5)] private int _stepIncrementId;
        [PmxKey(6)] private bool _harmonic;
        [PmxKey(7)] private OutputNodeCoordinatesEnum _outputNodeCoordinates;


        // Properties                                                                                                               
        public string FieldName { get { return _fieldName; } set { _fieldName = value; } }
        public string[] ComponentNames { get { return _componentNames; } set { _componentNames = value; } }
        public ComplexResultTypeEnum ComplexResultType { get { return _complexResultType; } set { _complexResultType = value; } }
        public double ComplexAngleDeg { get { return _complexAngleDeg; } set { _complexAngleDeg = value; } }
        public int StepId { get { return _stepId; } set { _stepId = value; } }
        public int StepIncrementId { get { return _stepIncrementId; } set { _stepIncrementId = value; } }
        public bool Harmonic { get { return _harmonic; } set { _harmonic = value; } }
        public OutputNodeCoordinatesEnum OutputNodeCoordinates
        {
            get { return _outputNodeCoordinates; }
            set { _outputNodeCoordinates = value; }
        }

        

        // Constructors                                                                                                             
        public ResultHistoryOutputFromField(string name, string regionName, RegionTypeEnum regionType,
                                            string filedName, string[] componentNames)
            : base(name, regionName, regionType)
        {
            _fieldName = filedName;
            _componentNames = componentNames;
            _complexResultType = ComplexResultTypeEnum.Real;
            _stepId = -1;
            _stepIncrementId = -1;
            _harmonic = false;
            _outputNodeCoordinates = OutputNodeCoordinatesEnum.Off;
        }


        // Methods                                                                                                                  
        public override string[] GetParentNames()
        {
            return new string[] { _fieldName };
        }
    }
}
