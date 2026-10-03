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
    public class ViewSurfaceOperation
    {
        // Variables
        private GeometrySelection _geometrySelection;
        private DynamicCustomTypeDescriptor _dctd;


        // Properties
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Surfaces")]
        [DescriptionAttribute("Select the surfaces. Hold the Shift key to select more than one surface.")]
        [Id(1, 1)]
        public string RegionSelection
        {
            get
            {
                int count = _geometrySelection.GeometryIds == null ? 0 : _geometrySelection.GeometryIds.Length;
                if (count <= 0) return "Empty";
                else if (count == 1) return "1 surface";
                else return count + " surfaces";
            }
            set { }
        }


        // Constructors
        public ViewSurfaceOperation(GeometrySelection geometrySelection, SurfaceOperationEnum operation)
        {
            _geometrySelection = geometrySelection;             // 1 command
            _dctd = ProviderInstaller.Install(this);            // 2 command
            //
            _dctd.GetProperty(nameof(RegionSelection)).SetDescription(GetRegionDescription(operation));
        }


        // Methods
        public GeometrySelection GetBase()
        {
            return _geometrySelection;
        }
        private static string GetRegionDescription(SurfaceOperationEnum operation)
        {
            string shift = " Hold the Shift key to select more than one surface.";
            if (operation == SurfaceOperationEnum.FlipNormal)
                return "Select the surfaces of CAD shell parts whose normals are flipped. While the dialog is open " +
                       "the surfaces are colored by their orientation." + shift;
            else if (operation == SurfaceOperationEnum.Defeature)
                return "Select the surfaces of CAD solid parts that are removed from the parts by defeaturing." + shift;
            else if (operation == SurfaceOperationEnum.DeleteCadFaces)
                return "Select the surfaces that are deleted from the CAD parts." + shift;
            else if (operation == SurfaceOperationEnum.DeleteStlFaces)
                return "Select the surfaces that are deleted from the STL shell parts." + shift;
            else throw new NotSupportedException();
        }
    }
}
