using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using System.Runtime.Serialization;

namespace CaeModel
{
    [Serializable]
    [Flags]
    public enum ElementHistoryVariable
    {
        // must start at 1 for the UI to work
        S = 1,
        E = 2,
        ME = 4,
        PEEQ = 8,
        // Thermal
        HFL = 16,
        // Whole element
        ENER = 32,
        ELSE = 64,
        ELKE = 128,
        EVOL = 256,
        EBHE = 512,
        //
        SDV = 1073741824,
    }

    [Serializable]
    public class ElementHistoryOutput : HistoryOutput, ISerializable
    {
        // Variables                                                                                                                
        private ElementHistoryVariable _variables;          //ISerializable


        // Properties                                                                                                               
        public ElementHistoryVariable Variables { get { return _variables; } set { _variables = value; } }


        // Constructors                                                                                                             
        public ElementHistoryOutput(string name, ElementHistoryVariable variables, string regionName, RegionTypeEnum regionType)
            : base(name, regionName, regionType)
        {
            _variables = variables;
        }
        public ElementHistoryOutput(SerializationInfo info, StreamingContext context)
           : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_variables":
                    case "ElementHistoryOutput+_variables":     // Compatibility v2.1.0
                        _variables = (ElementHistoryVariable)entry.Value; break;
                }
            }
        }


        // Methods                                                                                                                  


        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_variables", _variables, typeof(ElementHistoryVariable));
        }
    }
}
