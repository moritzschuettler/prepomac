using CaeGlobals;
using CaeMesh;
using CaeResults;
using DynamicTypeDescriptor;
using PrePoMax.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace PrePoMax
{
    [Serializable]
    public class ViewExport3mfFileProperties : ViewExportFileProperties
    {
        // Variables                                                                                                                


        // Properties                                                                                                               
        [Category("Data")]
        [OrderedDisplayName(1, 10, "Edge thickness")]
        [Description("Enter the edge thickness ratio.")]
        [Id(2, 1)]
        public double EdgeThicknessRatio
        {
            get { return (_properties as Export3mfFileProperties).EdgeThicknessRatio; }
            set { (_properties as Export3mfFileProperties).EdgeThicknessRatio = value; }
        }


        // Constructors                                                                                                             
        public ViewExport3mfFileProperties(Export3mfFileProperties properties)
            : base(properties)
        {
        }


        // Methods                                                                                                                  


    }
}

