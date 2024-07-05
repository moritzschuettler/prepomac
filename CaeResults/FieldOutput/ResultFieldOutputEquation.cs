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
    public class ResultFieldOutputEquation : ResultFieldOutput
    {
        // Variables                                                                                                                
        public static string EquationSeparator = ".";
        public static string ComponentName = "VALUE";
        private string _equation;
        private string _unit;
        private string[] _parentNames;


        // Properties                                                                                                               
        public string Equation { get { return _equation; } set { _equation = value; } }
        public string Unit { get { return _unit; } set { _unit = value; } }


        // Constructors                                                                                                             
        public ResultFieldOutputEquation(string name, string equation)
            : base(name)
        {
            _equation = equation;
            _unit = "/";
            _parentNames = null;
        }


        // Methods                                                                                                                  
        public void SetParentNames(string[] parentNames)
        {
            _parentNames = parentNames;
        }
        public override string[] GetParentNames()
        {
            return _parentNames;
        }
        public override string[] GetComponentNames()
        {
            return new string[] { ComponentName };
        }
    }
}
