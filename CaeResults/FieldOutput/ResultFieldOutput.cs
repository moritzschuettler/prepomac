using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace CaeResults
{   
    [Serializable]
    public abstract class ResultFieldOutput : NamedClass
    {
        // Variables                                                                                                                


        // Properties                                                                                                               
        public abstract string[] GetParentFieldNames();     // for dependency check
        public abstract string[] GetParentComponentNames(); // for dependency check
        public abstract string[] GetComponentNames();


        // Constructors                                                                                                             
        public ResultFieldOutput(string name)
            : base(name)
        {
        }


        // Methods                                                                                                                  


    }
}
