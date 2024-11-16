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
    class CDeleteStlPartFaces : PreprocessCommand
    {
        // Variables                                                                                                                
        private GeometrySelection _geometrySelection;


        // Constructor                                                                                                              
        public CDeleteStlPartFaces(GeometrySelection geometrySelection)
            : base("Delete stl part faces")
        {
            _geometrySelection = geometrySelection.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.DeleteStlPartFaces(_geometrySelection.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _geometrySelection.ToString();
        }
    }
}
