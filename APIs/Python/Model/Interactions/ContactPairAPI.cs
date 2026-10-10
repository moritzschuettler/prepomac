// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;

namespace APIs.Python.Model
{
    public class ContactPairAPI : BaseAPI
    {
        // Variables
        private readonly string _contactPairName;
        private readonly ContactPairService _service;


        // Properties
        [PythonAPI("The contact pair name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("The name of the surface interaction the contact pair uses.", Returns = "str")]
        public string surface_interaction_name { get { return Resolve().SurfaceInteractionName; } }
        [PythonAPI("The contact method: NodeToSurface, SurfaceToSurface, Mortar or Massless.", Returns = "str")]
        public string method { get { return Resolve().Method.ToString(); } }
        [PythonAPI("True when the small sliding formulation is used. Only the NodeToSurface method writes " +
                   "it, so it reads back False for any other method.", Returns = "bool")]
        public bool small_sliding
        {
            get
            {
                ContactPair contactPair = Resolve();
                return contactPair.Method == ContactPairMethod.NodeToSurface && contactPair.SmallSliding;
            }
        }
        [PythonAPI("True when the slave nodes are adjusted onto the master surface.", Returns = "bool")]
        public bool adjust { get { return Resolve().Adjust; } }
        [PythonAPI("The adjustment size. It reads back as None while adjust is False, since the size is then " +
                   "not used, and for the Default size the dialog offers; the argument adjustment_size takes " +
                   "float('nan') to set it back to Default.", Returns = "float or None")]
        public object adjustment_size
        {
            get
            {
                ContactPair contactPair = Resolve();
                if (!contactPair.Adjust) return null;
                //
                double value = contactPair.AdjustmentSize;
                return double.IsNaN(value) ? null : (object)value;
            }
        }
        [PythonAPI("True when the contact pair is written to the solver input file.", Returns = "bool")]
        public bool active { get { return Resolve().Active; } }
        [PythonAPI("True when the contact pair symbols are drawn.", Returns = "bool")]
        public bool visible { get { return Resolve().Visible; } }
        [PythonAPI("The region type of the master side.", Returns = "str")]
        public string master_region_type { get { return Resolve().MasterRegionType.ToString(); } }
        [PythonAPI("The region name of the master side.", Returns = "str")]
        public string master_region_name { get { return Resolve().MasterRegionName; } }
        [PythonAPI("The region type of the slave side.", Returns = "str")]
        public string slave_region_type { get { return Resolve().SlaveRegionType.ToString(); } }
        [PythonAPI("The region name of the slave side.", Returns = "str")]
        public string slave_region_name { get { return Resolve().SlaveRegionName; } }
        [PythonAPI("The master surface color as a list [R, G, B, A] of channel values, which the master_color " +
                   "argument takes back.", Returns = "list[int]")]
        public object master_color { get { return PythonColorConverter.ToChannels(Resolve().MasterColor); } }
        [PythonAPI("The slave surface color as a list [R, G, B, A] of channel values, which the slave_color " +
                   "argument takes back.", Returns = "list[int]")]
        public object slave_color { get { return PythonColorConverter.ToChannels(Resolve().SlaveColor); } }


        // Constructors
        internal ContactPairAPI(string contactPairName, ContactPairService service)
        {
            _contactPairName = contactPairName;
            _service = service;
        }


        // Methods
        public override string ToString()
        {
            ContactPair contactPair = Resolve();
            return "ContactPair(name=" + StringListAPI.ToDisplayString(contactPair.Name) + ", master=" +
                   StringListAPI.ToDisplayString(contactPair.MasterRegionName) + ", slave=" +
                   StringListAPI.ToDisplayString(contactPair.SlaveRegionName) + ")";
        }
        //
        private ContactPair Resolve()
        {
            return _service.ResolveContactPair(_contactPairName);
        }
    }
}
