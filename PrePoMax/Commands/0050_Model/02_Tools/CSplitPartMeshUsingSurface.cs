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
    class CSplitPartMeshUsingSurface : PreprocessCommand
    {
        // Variables                                                                                                                
        private SplitPartMeshData _splitPartMeshData;


        // Constructor                                                                                                              
        public CSplitPartMeshUsingSurface(SplitPartMeshData splitPartMeshData)
            : base("Split part mesh using surface")
        {
            _splitPartMeshData = splitPartMeshData;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            return receiver.SplitPartMeshUsingSurface(_splitPartMeshData);
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + "By selection";
        }
    }
}

