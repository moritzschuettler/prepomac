using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace CaeModel
{
    [Serializable]
    [Flags]
    public enum NodalFieldVariable
    {
        // Must start at 1 for the UI to work
        RF = 1,
        U = 2,
        PU = 4,
        V = 8,
        // Thermal
        NT = 16,
        PNT = 32,
        RFL = 64,
    }

    [Serializable]
    public class NodalFieldOutput : FieldOutput, ISerializable
    {
        // Variables                                                                                                                
        private NodalFieldVariable _variables;          //ISerializable


        // Properties                                                                                                               
        public NodalFieldVariable Variables { get { return _variables; } set { _variables = value; } }


        // Constructors                                                                                                             
        public NodalFieldOutput(string name, NodalFieldVariable variables)
            : base(name) 
        {
            _variables |= variables;
        }
        public NodalFieldOutput(SerializationInfo info, StreamingContext context)
           : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_variables":
                    case "NodalFieldOutput+_variables":     // Compatibility v2.1.0
                        _variables = (NodalFieldVariable)entry.Value; break;
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
            info.AddValue("_variables", _variables, typeof(NodalFieldVariable));
        }
    }
}
