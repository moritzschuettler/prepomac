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
    [Serializable]
    class CRemoveOrphanElements : PreprocessCommand
    {
        // Constructor
        public CRemoveOrphanElements()
            : base("Clean orphan elements")
        {
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveOrphanElements();
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString();
        }
    }
}
