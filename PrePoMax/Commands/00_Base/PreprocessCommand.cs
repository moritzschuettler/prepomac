using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using CaeGlobals;
using System.Runtime.Serialization;

namespace PrePoMax.Commands
{
    [Serializable]
    public abstract class PreprocessCommand : Command
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public PreprocessCommand(string name)
            : base(name)
        {
        }


        // Methods                                                                                                                  
        
    }
}
