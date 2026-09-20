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
    public class ViewSplitByExtendedFace : ViewGeometryFeature
    {
        // Variables
        private SplitByExtendedFace _splitByExtendedFace;


        // Properties
        [CategoryAttribute("Splitter")]
        [OrderedDisplayName(0, 10, "Face")]
        [DescriptionAttribute("Select the face whose surface is continued past its boundary into the splitting " +
                              "tool. The face may lie on a part that is not being split.")]
        [Id(1, 2)]
        public string SplitterSelection
        {
            get { return SelectionToString(_splitByExtendedFace.SplitterIds, "face"); }
            set { }
        }
        //
        [CategoryAttribute("Splitter")]
        [OrderedDisplayName(1, 10, "Surface type")]
        [DescriptionAttribute("The surface the picked face lies on. Only a plane, a cylinder, a cone, a sphere " +
                              "and a torus continue past the face boundary; a spline patch does not.")]
        [ReadOnlyAttribute(true)]
        [Id(2, 2)]
        public string SurfaceType
        {
            get
            {
                if (_splitByExtendedFace.SplitterIds == null || _splitByExtendedFace.SplitterIds.Length == 0)
                    return "Empty";
                else if (SplitByExtendedFace.CanBeExtended(_splitByExtendedFace.FaceType))
                    return _splitByExtendedFace.FaceType.ToString();
                else
                    return _splitByExtendedFace.FaceType.ToString() + " - cannot be extended";
            }
            set { }
        }


        // Constructors
        public ViewSplitByExtendedFace(SplitByExtendedFace splitByExtendedFace)
        {
            _splitByExtendedFace = splitByExtendedFace;                     // 1 command
            _dctd = ProviderInstaller.Install(this);                        // 2 command
            //
            UpdateVisibility();
        }


        // Methods
        public override GeometryFeature GetBase()
        {
            return _splitByExtendedFace;
        }
    }
}
