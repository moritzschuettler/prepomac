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
    public enum NodalHistoryVariable
    {
        // Must start at 1 for the UI to work
        RF = 1,
        U = 2,
        V = 4,
        // Thermal
        NT = 8,
        RFL = 16,
    }

    [Serializable]
    public class NodalHistoryOutput : HistoryOutput, ISerializable
    {
        // Variables                                                                                                                
        private NodalHistoryVariable _variables;            //ISerializable


        // Properties                                                                                                               
        public NodalHistoryVariable Variables { get { return _variables; } set { _variables = value; } }


        // Constructors                                                                                                             
        public NodalHistoryOutput(string name, NodalHistoryVariable variables, string regionName, RegionTypeEnum regionType)
            : base(name, regionName, regionType)
        {
            _variables = variables;
        }
        public NodalHistoryOutput(SerializationInfo info, StreamingContext context)
           : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_variables":
                    case "NodalHistoryOutput+_variables":       // Compatibility v2.1.0
                        _variables = (NodalHistoryVariable)entry.Value; break;
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
            info.AddValue("_variables", _variables, typeof(NodalHistoryVariable));
        }
    }
}
