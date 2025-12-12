using CaeGlobals;
using CaeMesh;
using CaeResults;
using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax
{
    [Serializable]
    public class Export3mfFileProperties : ExportFileProperties
    {
        // Variables                                                                                                                
        public static readonly string DefaultFileName = "Model.3mf";
        public static readonly string DefaultFilter = "3D Manufacturing Format (*.3mf)|*.3mf";
        private double _edgeThicknessRatio;


        // Properties                                                                                                               
        public double EdgeThicknessRatio { get { return _edgeThicknessRatio; } set { _edgeThicknessRatio = value; } }


        // Constructors                                                                                                             
        public Export3mfFileProperties()
            :base(DefaultFileName, DefaultFilter)
        {
            _edgeThicknessRatio = 1;
        }


        // Methods                                                                                                                  
        
    }
}
