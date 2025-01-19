using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeMesh;
using CaeGlobals;
using CaeJob;
using PrePoMax.Forms;

namespace PrePoMax.Commands
{
    [Serializable]
    class CAddDeafaultJob : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _defaultName;

        // Constructor                                                                                                              
        public CAddDeafaultJob()
            : base("Add default analysis")
        {
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            _defaultName = receiver.AddDefaultJob();
            return true;
        }

        public override string GetCommandString()
        {
            string data = _defaultName != null ? _defaultName : "";
            return base.GetCommandString() + data;
        }
    }
}
