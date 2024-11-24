using CaeMesh;
using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PrePoMax.Forms
{
    public abstract class ViewMeshSetupItem
    {
        // Variables                                                                                                                
        protected DynamicCustomTypeDescriptor _dctd;
        protected string _regionType;


        // Variables                                                                                                                
        public abstract string Name { get; set; }
        public abstract string RegionType { get; set; }
       

        // Methods
        public abstract MeshSetupItem GetBase();
        public void InitializeRegion()
        {
            _regionType = "Selection";
            _dctd.PopulateProperty(nameof(RegionType), new string[] { _regionType });
        }
        public void HideName()
        {
            _dctd.GetProperty(nameof(Name)).SetIsBrowsable(false);
        }
    }
}
