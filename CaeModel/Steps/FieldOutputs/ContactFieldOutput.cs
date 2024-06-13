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
    public enum ContactFieldVariable
    {
        // Must start at 1 for the UI to work
        CDIS = 1,
        CSTR = 2,
        //CELS = 4,
        PCON = 8
    }

    [Serializable]
    public class ContactFieldOutput : FieldOutput, ISerializable
    {
        // Variables                                                                                                                
        private ContactFieldVariable _variables;        //ISerializable


        // Properties                                                                                                               
        public ContactFieldVariable Variables { get { return _variables; } set { _variables = value; } }


        // Constructors                                                                                                             
        public ContactFieldOutput(string name, ContactFieldVariable variables)
            : base(name) 
        {
            _variables |= variables;
        }
        public ContactFieldOutput(SerializationInfo info, StreamingContext context)
           : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_variables":
                    case "ContactFieldOutput+_variables":     // Compatibility v2.1.0
                        _variables = (ContactFieldVariable)entry.Value; break;
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
            info.AddValue("_variables", _variables, typeof(ContactFieldVariable));
        }
    }
}
