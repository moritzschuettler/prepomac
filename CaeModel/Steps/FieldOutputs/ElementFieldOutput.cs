using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;

namespace CaeModel
{
    [Serializable]
    [Flags]
    public enum ElementFieldVariable
    {
        // Must start at 1 for the UI to work
        S = 1,
        PHS = 2,
        E = 4,
        ME = 8,
        PEEQ = 16,
        ENER = 32,
        // Thermal
        HFL = 64,
        // Error
        ERR = 128,
        HER = 256,
        ZZS = 512,
        //
        SDV = 1073741824,
    }

    [Serializable]
    public enum ElementFieldOutputOutputEnum
    {
        Default,
        [DynamicTypeDescriptor.StandardValue("TwoD", DisplayName = "2D")]
        TwoD,
        [DynamicTypeDescriptor.StandardValue("ThreeD", DisplayName = "3D")]
        ThreeD
    }

    [Serializable]
    public class ElementFieldOutput : FieldOutput, ISerializable
    {
        // Variables                                                                                                                
        private ElementFieldOutputOutputEnum _output;           //ISerializable
        private ElementFieldVariable _variables;                //ISerializable


        // Properties                                                                                                               
        public ElementFieldOutputOutputEnum Output { get { return _output; } set { _output = value; } }
        public ElementFieldVariable Variables { get { return _variables; } set { _variables = value; } }


        // Constructors                                                                                                             
        public ElementFieldOutput(string name, ElementFieldVariable variables)
            : base(name) 
        {
            _variables |= variables;
            _output = ElementFieldOutputOutputEnum.Default;
        }
        public ElementFieldOutput(SerializationInfo info, StreamingContext context)
          : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_output":
                    case "ElementFieldOutput+_output":      // Compatibility v2.1.0
                        _output = (ElementFieldOutputOutputEnum)entry.Value; break;
                    case "_variables":
                    case "_ElementFieldOutput+variables":   // Compatibility v2.1.0
                        _variables = (ElementFieldVariable)entry.Value; break;
                }
            }
        }


        // Methods                                                                                                                  
        public string GetVariablesString()
        {
            string result = _variables.ToString();
            //
            bool error = _variables.HasFlag(ElementFieldVariable.ERR) || _variables.HasFlag(ElementFieldVariable.HER) ||
                         _variables.HasFlag(ElementFieldVariable.ZZS);
            if (_variables.HasFlag(ElementFieldVariable.S) && !error) result += ", NOE";
            return result;
        }


        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_output", _output, typeof(ElementFieldOutputOutputEnum));
            info.AddValue("_variables", _variables, typeof(ElementFieldVariable));
        }
    }
}
