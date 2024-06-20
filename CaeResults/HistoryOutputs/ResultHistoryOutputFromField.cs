using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;

namespace CaeResults
{
    [Serializable] 
    public enum OutputNodeCoordinatesEnum
    {
        Off,
        Undeformed,
        Deformed
    }

    [Serializable]
    public class ResultHistoryOutputFromField : ResultHistoryOutput
    {
        // Variables                                                                                                                
        public readonly static string AllSteps = "All steps";
        public readonly static string AllIncrements = "All increments";
        //
        private string _fieldName;
        private string[] _componentNames;
        private ComplexResultTypeEnum _complexResultType;
        private double _complexAngleDeg;    // must be double
        private int _stepId;
        private int _stepIncrementId;
        private bool _harmonic;
        private OutputNodeCoordinatesEnum _outputNodeCoordinates;


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
        public ResultHistoryOutputFromField(string name, string filedName, string[] componentNames,
                                            string regionName, RegionTypeEnum regionType)
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
