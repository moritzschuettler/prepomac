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

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewShellQuadWild : ViewMeshSetupItem
    {
        // Variables
        private ShellQuadWild _shellQuadWild;


        // Properties
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Name of the mesh setup item.")]
        [Id(1, 1)]
        public override string Name { get { return _shellQuadWild.Name; } set { _shellQuadWild.Name = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [DescriptionAttribute("Select the shell parts to mesh using QuadWild.")]
        [Id(1, 2)]
        public override string RegionType { get { return _regionType; } set { _regionType = value; } }
        //
        [CategoryAttribute("Triangulation")]
        [OrderedDisplayName(0, 10, "Mesher")]
        [DescriptionAttribute("Select the mesher used to create the triangular input for QuadWild.")]
        [Id(1, 3)]
        public MesherTypeEnum TriangulationMesher
        {
            get { return _shellQuadWild.TriangulationMesher; }
            set { _shellQuadWild.TriangulationMesher = value; }
        }
        //
        [CategoryAttribute("Triangulation")]
        [OrderedDisplayName(1, 10, "Preview")]
        [DescriptionAttribute("When set to Yes, Create Mesh skips quad generation. It imports the selected mesher " +
                              "triangulation when Remesh input is No, or the QuadWild-remeshed triangulation when " +
                              "Remesh input is Yes.")]
        [Id(2, 3)]
        public bool Preview
        {
            get { return _shellQuadWild.Preview; }
            set { _shellQuadWild.Preview = value; UpdateVisibility(); } }
        //
        [CategoryAttribute("Preparation")]
        [OrderedDisplayName(0, 10, "Remesh input")]
        [DescriptionAttribute("Allow QuadWild to remesh the triangular input before patch tracing. " +
                              "This can excessively refine meshes containing very short edges. Usually keep it " +
                              "disabled and control the input using the selected triangulation mesher.")]
        [Id(1, 4)]
        public bool Remesh { get { return _shellQuadWild.Remesh; } set { _shellQuadWild.Remesh = value; } }
        //
        [CategoryAttribute("Quad generation")]
        [OrderedDisplayName(1, 10, "Quad edge multiplier")]
        [DescriptionAttribute("Multiplier applied to the average traced-triangle edge length when targeting final " +
                              "quad size. Values above 1 create larger and fewer quads. This does not control " +
                              "triangular preparation.")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(2, 5)]
        public double ScaleFactor
        {
            get { return _shellQuadWild.ScaleFactor; }
            set { _shellQuadWild.ScaleFactor = value; }
        }
        //
        [CategoryAttribute("Quad generation")]
        [OrderedDisplayName(0, 10, "Flow profile")]
        [DescriptionAttribute("Aligned uses flow.txt. No alignment uses flow_noalign.txt and relaxes singularity alignment.")]
        [Id(1, 5)]
        public QuadWildFlowProfileEnum FlowProfile
        {
            get { return _shellQuadWild.FlowProfile; }
            set { _shellQuadWild.FlowProfile = value; }
        }
        //
        [CategoryAttribute("Quad generation")]
        [OrderedDisplayName(2, 10, "Alpha")]
        [DescriptionAttribute("Controls the final quadrangulation trade-off between regularity and isometry. " +
                              "Values near 0 favor regularity; larger values allow more singularities. This does " +
                              "not control triangular preparation.")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(3, 5)]
        public double Alpha
        {
            get { return _shellQuadWild.Alpha; }
            set { _shellQuadWild.Alpha = value; }
        }
        //
        [CategoryAttribute("Quad generation")]
        [OrderedDisplayName(3, 10, "Smooth")]
        [DescriptionAttribute("Import the smoothed quadrangulation. Smoothing improves the shape of most quads, " +
                              "but can collapse a few of them onto a sharp feature line. Set to No to import the " +
                              "unsmoothed quadrangulation, which has the same topology.")]
        [Id(4, 5)]
        public bool Smooth { get { return _shellQuadWild.Smooth; } set { _shellQuadWild.Smooth = value; } }


        // Constructors
        public ViewShellQuadWild(ShellQuadWild shellQuadWild)
        {
            _shellQuadWild = shellQuadWild;
            _dctd = ProviderInstaller.Install(this);
            InitializeRegion();
            //
            _dctd.GetProperty(nameof(TriangulationMesher)).RemoveStandardValues(
                new HashSet<string>() { MesherTypeEnum.None.ToString(), MesherTypeEnum.QuadWild.ToString() });
            _dctd.RenameBooleanPropertyToYesNo(nameof(Preview));
            _dctd.RenameBooleanPropertyToYesNo(nameof(Remesh));
            _dctd.RenameBooleanPropertyToYesNo(nameof(Smooth));
            //
            UpdateVisibility();
        }


        // Methods
        public override MeshSetupItem GetBase()
        {
            return _shellQuadWild;
        }
        private void UpdateVisibility()
        {
            bool visible = !_shellQuadWild.Preview;
            _dctd.GetProperty(nameof(Remesh)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(FlowProfile)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(ScaleFactor)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(Alpha)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(Smooth)).SetIsBrowsable(visible);
        }
    }
}
