using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.Runtime.Serialization;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Data;

namespace CaeModel
{
    [Serializable]
    public abstract class FieldOutput : NamedClass, ISerializable
    {
        // Variables                                                                                                                
        private bool _lastIterations;       //ISerializable
        private bool _contactElements;      //ISerializable
        private bool _global;               //ISerializable


        // Properties                                                                                                               
        public bool LastIterations { get { return _lastIterations; } set { _lastIterations = value; } }
        public bool ContactElements { get { return _contactElements; } set { _contactElements = value; } }
        public bool Global { get { return _global; } set { _global = value; } }


        // Constructors                                                                                                             
        public FieldOutput(string name)
            : base(name)
        {
            _global = true;
            _lastIterations = false;
            _contactElements = false;
        }
        public FieldOutput(SerializationInfo info, StreamingContext context)
           : base(info, context)
        {
            // Compatibility v2.1.0
            _global = true;
            //
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_lastIterations":
                    case "FieldOutput+_lastIterations":     // Compatibility v2.1.0
                        _lastIterations = (bool)entry.Value; break;
                    case "_contactElements":
                    case "FieldOutput+_contactElements":    // Compatibility v2.1.0
                        _contactElements = (bool)entry.Value; break;
                    case "_global":
                        _global = (bool)entry.Value; break;
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
            info.AddValue("_lastIterations", _lastIterations, typeof(bool));
            info.AddValue("_contactElements", _contactElements, typeof(bool));
            info.AddValue("_global", _global, typeof(bool));
        }
    }
}
