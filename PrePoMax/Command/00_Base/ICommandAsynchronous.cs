using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeMesh;
using CaeGlobals;


namespace PrePoMax.Commands
{
    interface ICommandAsynchronous
    {
        // Methods                                                                                                                  
        bool ExecuteSynchronous(Controller receiver);
    }
}
