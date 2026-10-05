// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewPeriodicFaceSplit
    {
        // Variables
        private PeriodicFaceSplit _periodicFaceSplit;
        private DynamicCustomTypeDescriptor _dctd;


        // Properties
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Parts")]
        [DescriptionAttribute("Select the CAD parts whose periodic faces will be split. Hold the Shift key to select " +
                              "more than one part. A sub-part is split together with its compound.")]
        [Id(1, 1)]
        public string RegionSelection
        {
            get
            {
                int count = _periodicFaceSplit.CreationIds == null ? 0 : _periodicFaceSplit.CreationIds.Length;
                if (count <= 0) return "Empty";
                else if (count == 1) return "1 part";
                else return count + " parts";
            }
            set { }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Number of resulting faces")]
        [DescriptionAttribute("Enter the number of faces every periodic face - a face that closes on itself, such as " +
                              "the side of a cylinder with a single seam - will be split into.")]
        [Id(1, 2)]
        public int NumOfFaces
        {
            get { return _periodicFaceSplit.NumOfFaces; }
            set { _periodicFaceSplit.NumOfFaces = value; }
        }


        // Constructors
        public ViewPeriodicFaceSplit(PeriodicFaceSplit periodicFaceSplit)
        {
            _periodicFaceSplit = periodicFaceSplit;             // 1 command
            _dctd = ProviderInstaller.Install(this);            // 2 command
        }


        // Methods
        public PeriodicFaceSplit GetBase()
        {
            return _periodicFaceSplit;
        }
    }
}
