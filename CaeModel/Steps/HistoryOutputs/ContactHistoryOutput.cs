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
    public enum ContactHistoryVariable
    {
        // Must start at 1 for the UI to work
        CDIS = 1,
        CSTR = 2,
        CELS = 4,
        CNUM = 8,
        CF = 16
        //CFN = 32,
        //CFS = 64
    }

    [Serializable]
    public class ContactHistoryOutput : HistoryOutput, ISerializable
    {
        // Variables                                                                                                                
        private ContactHistoryVariable _variables;          //ISerializable


        // Properties                                                                                                               
        public ContactHistoryVariable Variables { get { return _variables; } set { _variables = value; } }


        // Constructors                                                                                                             
        public ContactHistoryOutput(string name, ContactHistoryVariable variables, string contactPairName)
            : base(name, contactPairName, RegionTypeEnum.ContactPair)
        {
            _variables = variables;
        }
        public ContactHistoryOutput(SerializationInfo info, StreamingContext context)
          : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_variables":
                    case "ContactHistoryOutput+_variables":     // Compatibility v2.1.0
                        _variables = (ContactHistoryVariable)entry.Value; break;
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
            info.AddValue("_variables", _variables, typeof(ContactHistoryVariable));
        }
    }
}
