// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace PrePoMax.Forms
{
    public class ViewChangeElementOrder : UserControls.IMissingPropertyValues
    {
        // Variables                                                                                                                
        private const string Linear = "Linear";
        private const string Parabolic = "Parabolic";
        private DynamicCustomTypeDescriptor _dctd;
        private string _regionType;
        private string _elementOrder;
        private bool _projectToGeometry;
        private int[] _creationIds;
        private Selection _creationData;


        // Properties                                                                                                               
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [DescriptionAttribute("Select the parts for the change of the element order.")]
        [Id(1, 1)]
        public string RegionType { get { return _regionType; } set { _regionType = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Element order")]
        [DescriptionAttribute("Select the element order of the selected parts. Linear elements are converted to " +
                              "parabolic elements by adding the midside nodes at the middle of the straight element edges. " +
                              "Parabolic elements are converted to linear elements by removing the midside nodes.")]
        [Id(1, 2)]
        public string ElementOrder
        {
            get { return _elementOrder; }
            set
            {
                _elementOrder = value;
                _dctd.GetProperty(nameof(ProjectToGeometry)).SetIsBrowsable(SecondOrder);
            }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Project to geometry")]
        [DescriptionAttribute("Select yes to move the new midside nodes on the model edges and faces onto the CAD " +
                              "geometry the part was meshed from. A part without CAD geometry keeps its midside nodes " +
                              "on the straight element edges.")]
        [Id(2, 2)]
        public bool ProjectToGeometry { get { return _projectToGeometry; } set { _projectToGeometry = value; } }
        //
        [Browsable(false)]
        public bool SecondOrder { get { return _elementOrder == Parabolic; } }
        [Browsable(false)]
        public int[] CreationIds { get { return _creationIds; } set { _creationIds = value; } }
        [Browsable(false)]
        public Selection CreationData { get { return _creationData; } set { _creationData = value; } }


        // Constructor                                                                                                              
        public ViewChangeElementOrder(bool secondOrder)
        {
            _regionType = "Selection";
            _elementOrder = secondOrder ? Parabolic : Linear;
            _projectToGeometry = true;
            _creationIds = null;
            _creationData = null;
            //
            _dctd = ProviderInstaller.Install(this);
            _dctd.PopulateProperty(nameof(RegionType), new string[] { _regionType });
            _dctd.PopulateProperty(nameof(ElementOrder), new string[] { Linear, Parabolic });
            _dctd.RenameBooleanPropertyToYesNo(nameof(ProjectToGeometry));
            _dctd.GetProperty(nameof(ProjectToGeometry)).SetIsBrowsable(secondOrder);
        }


        // Methods                                                                                                                  
        public bool IsPropertyValueMissing(string propertyName)
        {
            if (propertyName != nameof(RegionType)) return false;
            return _creationIds == null || _creationIds.Length == 0;
        }
    }
}
