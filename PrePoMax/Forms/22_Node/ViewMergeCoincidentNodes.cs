using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.ComponentModel;
using DynamicTypeDescriptor;
using System.Drawing.Printing;

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewMergeCoincidentNodes
    {
        // Variables                                                                                                                
        private MergeCoincidentNodes _mergeCoincidentNodes;
        private DynamicCustomTypeDescriptor _dctd = null;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Tolerance")]
        [DescriptionAttribute("Tolerance for merging coincident nodes.")]
        [TypeConverter(typeof(StringLengthConverter))]
        public double Tolerance
        {
            get { return _mergeCoincidentNodes.Tolerance; }
            set { _mergeCoincidentNodes.Tolerance = value; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Nodes to keep")]
        [DescriptionAttribute("Select which coincident nodse to keep.")]
        public NodesToKeepEnum NodesToKeep
        {
            get { return _mergeCoincidentNodes.NodesToKeep; }
            set { _mergeCoincidentNodes.NodesToKeep = value; }
        }
        //
        [Browsable(false)]
        public Selection CreationData
        {
            get { return _mergeCoincidentNodes.CreationData; }
            set { _mergeCoincidentNodes.CreationData = value; }
        }
        //
        [Browsable(false)]
        public int[] GeometryIds
        {
            get { return _mergeCoincidentNodes.GeometryIds; }
            set { _mergeCoincidentNodes.GeometryIds = value; }
        }


        // Constructors                                                                                                             
        public ViewMergeCoincidentNodes()
        {
            _mergeCoincidentNodes = new MergeCoincidentNodes();
            _dctd = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
        public MergeCoincidentNodes GetBase()
        {
            return _mergeCoincidentNodes;
        }



    }
}
