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
        [DescriptionAttribute("When set to Yes, Create Mesh skips quad generation and imports the triangulation " +
                              "of the selected mesher.")]
        [Id(2, 3)]
        public bool Preview
        {
            get { return _shellQuadWild.Preview; }
            set { _shellQuadWild.Preview = value; UpdateVisibility(); } }
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
        [OrderedDisplayName(2, 10, "Boundary layer")]
        [DescriptionAttribute("One ring of regular quads along the boundary, inserted before the smoothing so that " +
                              "the elements at the boundary follow it. Holes puts the ring around every closed edge " +
                              "loop without corners; All edges puts it along every model edge, which helps fine " +
                              "meshes but can hurt coarse ones and shell compounds.")]
        [Id(3, 5)]
        public QuadWildBoundaryLayerEnum BoundaryLayer
        {
            get { return _shellQuadWild.BoundaryLayer; }
            set { _shellQuadWild.BoundaryLayer = value; }
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
        //
        [CategoryAttribute("Debugging")]
        [OrderedDisplayName(0, 10, "Keep files on failure")]
        [DescriptionAttribute("Keep the QuadWild MeshBatch folder when meshing fails so that its input, " +
                              "configuration, output, and log files can be inspected. The retained folder path " +
                              "is written to the output window.")]
        [Id(1, 6)]
        public bool KeepFilesOnFailure
        {
            get { return _shellQuadWild.KeepFilesOnFailure; }
            set { _shellQuadWild.KeepFilesOnFailure = value; }
        }
        //
        [CategoryAttribute("Debugging")]
        [OrderedDisplayName(1, 10, "Debug visualization")]
        [DescriptionAttribute("Add the QuadWild patches as separate parts, the irregular interior nodes (valence 3, 5, " +
                              "6 and more), the prescribed nodes, the patch corners and the seam nodes as node sets, " +
                              "the sheets of a shell compound as element sets, and the cross field and the feature " +
                              "lines QuadWild used as wire parts. With Preview, the traced triangle patches are shown " +
                              "instead of the pre-mesh. For inspection only: the patch parts are not model faces.")]
        [Id(2, 6)]
        public bool DebugVisualization
        {
            get { return _shellQuadWild.DebugVisualization; }
            set { _shellQuadWild.DebugVisualization = value; }
        }
        //
        [CategoryAttribute("Debugging")]
        [OrderedDisplayName(2, 10, "Algorithm")]
        [DescriptionAttribute("New uses all QuadWild improvements: patch corners on holes and long curved lines, " +
                              "merged strips between holes, the target size and the minimum number of elements on " +
                              "lines taken from the triangulation, alpha raised when needed, field-aligned smoothing, " +
                              "the split of quads that touch the boundary with one vertex and nodes on the smooth " +
                              "curves through the triangulation nodes. Old meshes without " +
                              "them, for comparison.")]
        [Id(3, 6)]
        public bool OldAlgorithm
        {
            get { return _shellQuadWild.OldAlgorithm; }
            set { _shellQuadWild.OldAlgorithm = value; }
        }
        //
        [CategoryAttribute("Debugging")]
        [OrderedDisplayName(3, 10, "Edge nodes")]
        [DescriptionAttribute("Free lets QuadWild place the nodes on the model edges, with at least the number of " +
                              "elements of the triangulation. Pinned keeps every node of the triangulation on the " +
                              "model edges, as a sweep source meshed after the 1D mesh would need, for comparison. " +
                              "With pinned nodes every face needs an even number of boundary elements.")]
        [Id(4, 6)]
        public bool PinEdgeNodes
        {
            get { return _shellQuadWild.PinEdgeNodes; }
            set { _shellQuadWild.PinEdgeNodes = value; }
        }
        //
        [CategoryAttribute("Debugging")]
        [OrderedDisplayName(4, 10, "Edge rotations")]
        [DescriptionAttribute("On changes the connectivity where the quads are poor (worst corner below 0.8): the " +
                              "edge shared by two quads is turned to another diagonal of the hexagon they form when " +
                              "that makes the quads around it better. Off keeps the connectivity of the quad " +
                              "generation, for comparison.")]
        [Id(5, 6)]
        public bool EdgeRotations
        {
            get { return _shellQuadWild.EdgeRotations; }
            set { _shellQuadWild.EdgeRotations = value; }
        }
        //
        [CategoryAttribute("Debugging")]
        [OrderedDisplayName(5, 10, "Singularity position")]
        [DescriptionAttribute("Weight of placing the irregular node inside every patch with 3, 5 or 6 sides where " +
                              "its separatrices fit the patch geometry, relative to the element size objective. " +
                              "0 leaves the position to the element counts; 2 to 4 are worth testing. For " +
                              "comparison.")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(6, 6)]
        public double SingularityPosition
        {
            get { return _shellQuadWild.SingularityPosition; }
            set { _shellQuadWild.SingularityPosition = value; }
        }
        //
        [CategoryAttribute("Debugging")]
        [OrderedDisplayName(6, 10, "Count search rounds")]
        [DescriptionAttribute("Rounds of the search over the element counts of the patch sides: in every round the " +
                              "three worst patches are found, every side of each is tried with one or two elements " +
                              "more or fewer, and the change that improves the worst element most is kept. Every " +
                              "trial costs a quantisation and a local smoothing, so the meshing takes longer. 0 " +
                              "keeps the counts of the quantisation.")]
        [TypeConverter(typeof(StringIntegerConverter))]
        [Id(7, 6)]
        public int CountSearch
        {
            get { return _shellQuadWild.CountSearch; }
            set { _shellQuadWild.CountSearch = value; }
        }


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
            _dctd.RenameBooleanPropertyToYesNo(nameof(Smooth));
            _dctd.RenameBooleanPropertyToYesNo(nameof(KeepFilesOnFailure));
            _dctd.RenameBooleanPropertyToYesNo(nameof(DebugVisualization));
            _dctd.RenameBooleanProperty(nameof(OldAlgorithm), "Old", "New");
            _dctd.RenameBooleanProperty(nameof(PinEdgeNodes), "Pinned", "Free");
            _dctd.RenameBooleanPropertyToOnOff(nameof(EdgeRotations));
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
            _dctd.GetProperty(nameof(FlowProfile)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(BoundaryLayer)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(Alpha)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(Smooth)).SetIsBrowsable(visible);
        }
    }
}
