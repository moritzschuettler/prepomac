// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using CaeMesh;

namespace PrePoMax.Python.Journal
{
    // Mesh setup items. The Gmsh based ones share the algorithm and recombination values of their base
    // type.
    public static class PythonMeshSetupEmitter
    {
        // Variables                                                                                                                
        public const string SetupItems = "pmx.model.mesh.setup_items";


        // Methods                                                                                                                  
        public static void Emit(PythonJournalWriter writer, string oldName, MeshSetupItem item)
        {
            string selection = PythonSelectionEmitter.Emit(writer, item.CreationData);
            List<string> arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(item.Name));
            arguments.Add(selection);
            //
            bool replace = oldName != null;
            string method = (replace ? "replace_" : "add_") + AddTypeArguments(arguments, item, replace);
            //
            writer.Statement(writer.Call(SetupItems, method, arguments.ToArray()));
            PythonModelItemEmitter.EmitState(writer, SetupItems, item);
            writer.CountEmittedCommand();
        }
        // The most derived type first, because the Gmsh items form a chain. A replace keeps every value it is not
        // given, so the values an add leaves out at their default are written out on a replace
        private static string AddTypeArguments(List<string> arguments, MeshSetupItem item, bool replace)
        {
            if (item is FeMeshRefinement refinement)
            {
                arguments.Add("element_size=" + PythonValueFormatter.Number(refinement.MeshSize));
                return "local_mesh_size";
            }
            if (item is LocalMeshSize localMeshSize)
            {
                if (localMeshSize.Type == LocalMeshSizeTypeEnum.NumberOfElements)
                    arguments.Add("number_of_elements=" +
                                  PythonValueFormatter.Format(localMeshSize.NumOfElements));
                else arguments.Add("element_size=" + PythonValueFormatter.Number(localMeshSize.MeshSize));
                return "local_mesh_size";
            }
            if (item is ThickenShellMesh thicken)
            {
                arguments.Add(PythonValueFormatter.Number(thicken.Thickness));
                arguments.Add("number_of_layers=" + PythonValueFormatter.Format(thicken.NumberOfLayers));
                arguments.Add("offset=" + PythonValueFormatter.Number(thicken.Offset));
                arguments.Add("keep_model_edges=" + PythonValueFormatter.Format(thicken.KeepModelEdges));
                return "thicken_shell_mesh";
            }
            if (item is ShellQuadWild quadWild)
            {
                // A preview runs no QuadWild, so the settings of the quadrangulation, which the dialog then hides
                // and the API refuses, are left out
                bool quadrangulated = !quadWild.Preview;
                arguments.Add("preview=" + PythonValueFormatter.Format(quadWild.Preview));
                if (quadrangulated)
                {
                    arguments.Add("smooth=" + PythonValueFormatter.Format(quadWild.Smooth));
                    arguments.Add("alpha=" + PythonValueFormatter.Number(quadWild.Alpha));
                }
                arguments.Add("triangulation_mesher=" +
                              PythonValueFormatter.Text(quadWild.TriangulationMesher.ToString()));
                if (quadrangulated)
                {
                    arguments.Add("flow_profile=" + PythonValueFormatter.Text(quadWild.FlowProfile.ToString()));
                    arguments.Add("boundary_layer=" +
                                  PythonValueFormatter.Text(quadWild.BoundaryLayer.ToString()));
                }
                arguments.Add("keep_files_on_failure=" +
                              PythonValueFormatter.Format(quadWild.KeepFilesOnFailure));
                arguments.Add("debug_visualization=" +
                              PythonValueFormatter.Format(quadWild.DebugVisualization));
                if (replace || quadWild.OldAlgorithm)
                    arguments.Add("old_algorithm=" + PythonValueFormatter.Format(quadWild.OldAlgorithm));
                if (replace || quadWild.PinEdgeNodes)
                    arguments.Add("pin_edge_nodes=" + PythonValueFormatter.Format(quadWild.PinEdgeNodes));
                if (replace || quadWild.EdgeRotations)
                    arguments.Add("edge_rotations=" + PythonValueFormatter.Format(quadWild.EdgeRotations));
                if (replace || quadWild.SingularityPosition > 0)
                    arguments.Add("singularity_position=" +
                                  PythonValueFormatter.Number(quadWild.SingularityPosition));
                if (replace || quadWild.CountSearch > 0)
                    arguments.Add("number_of_search_rounds=" + PythonValueFormatter.Format(quadWild.CountSearch));
                if (quadrangulated && (replace || quadWild.TransfiniteThreeSided != TransfiniteThreeSidedEnum.Mapped))
                    arguments.Add("transfinite_three_sided=" +
                                  PythonValueFormatter.Text(quadWild.TransfiniteThreeSided.ToString()));
                if (quadrangulated && (replace || !quadWild.TransfiniteFourSided))
                    arguments.Add("transfinite_four_sided=" +
                                  PythonValueFormatter.Format(quadWild.TransfiniteFourSided));
                bool structured = quadWild.IsTransfiniteThreeSided || quadWild.TransfiniteFourSided;
                if (quadrangulated && structured && (replace || quadWild.TransfiniteAngleDeg != 135))
                    arguments.Add("transfinite_angle_deg=" + PythonValueFormatter.Number(quadWild.TransfiniteAngleDeg));
                return "shell_quad_wild";
            }
            if (item is SweepMesh sweep)
            {
                // Only an item made before the dialog stopped offering the setting keeps its imprint edges
                if (sweep.KeepImprintEdges)
                    throw new NotSupportedException("a sweep mesh that keeps its imprint edges was made by an " +
                                                    "older version of PrePoMax; the dialog no longer offers the " +
                                                    "setting and the API no longer takes it");
                arguments.Add("number_of_global_smooth_steps=" +
                              PythonValueFormatter.Format(sweep.NumberOfGlobalSmoothSteps));
                // The 2D algorithm of a sweep may be QuadWild, and the sweep builds its volume mesh itself
                arguments.Add("algorithm_2d=" + PythonValueFormatter.Text(sweep.SweepAlgorithmMesh2D.ToString()));
                AddRecombinationValues(arguments, sweep);
                arguments.Add("optimize_first_order_shell=" +
                              PythonValueFormatter.Text(sweep.OptimizeFirstOrderShell.ToString()));
                arguments.Add("elements_per_layer=" + PythonValueFormatter.List(sweep.NumOfElementsPerLayer));
                // An item read from an older file sweeps every cell, a new one uses the symmetry, so the value
                // is always written
                arguments.Add("symmetry=" + PythonValueFormatter.Text(sweep.Symmetry.ToString()));
                if (sweep.SourceMesher == SweepSourceMesherEnum.QuadWild)
                {
                    arguments.Add("quad_wild_flow_profile=" +
                                  PythonValueFormatter.Text(sweep.QuadWildFlowProfile.ToString()));
                    arguments.Add("quad_wild_alpha=" + PythonValueFormatter.Number(sweep.QuadWildAlpha));
                    arguments.Add("quad_wild_smooth=" + PythonValueFormatter.Format(sweep.QuadWildSmooth));
                }
                return "sweep_mesh";
            }
            if (item is TransfiniteMesh transfinite)
            {
                arguments.Add("allow_pyramid_elements=" +
                              PythonValueFormatter.Format(transfinite.AllowPyramidElements));
                arguments.Add("algorithm_2d=" +
                              PythonValueFormatter.Text(transfinite.AlgorithmMesh2D.ToString()));
                arguments.Add("algorithm_3d=" +
                              PythonValueFormatter.Text(transfinite.AlgorithmMesh3D.ToString()));
                arguments.Add("recombine=" +
                              PythonValueFormatter.Format(transfinite.AlgorithmRecombine !=
                                                          GmshAlgorithmRecombineEnum.None));
                arguments.Add("transfinite_angle_deg=" +
                              PythonValueFormatter.Number(transfinite.TransfiniteAngleDeg));
                arguments.Add("transfinite_three_sided=" +
                              PythonValueFormatter.Text(transfinite.TransfiniteThreeSided.ToString()));
                return "transfinite_mesh";
            }
            if (item is ShellGmsh shellGmsh)
            {
                AddSurfaceGmshValues(arguments, shellGmsh);
                arguments.Add("optimize_first_order_shell=" +
                              PythonValueFormatter.Text(shellGmsh.OptimizeFirstOrderShell.ToString()));
                return "shell_gmsh";
            }
            if (item is NetgenMesh netgenMesh)
            {
                arguments.Add("number_of_optimize_steps_2d=" + PythonValueFormatter.Format(netgenMesh.OptimizeSteps2D));
                arguments.Add("number_of_optimize_steps_3d=" + PythonValueFormatter.Format(netgenMesh.OptimizeSteps3D));
                arguments.Add("quad_dominated=" + PythonValueFormatter.Format(netgenMesh.QuadDominated));
                arguments.Add("transfinite_three_sided=" + PythonValueFormatter.Text(netgenMesh.TransfiniteThreeSided.ToString()));
                arguments.Add("transfinite_four_sided=" + PythonValueFormatter.Format(netgenMesh.TransfiniteFourSided));
                if (netgenMesh.IsTransfiniteThreeSided || netgenMesh.TransfiniteFourSided)
                    arguments.Add("transfinite_angle_deg=" + PythonValueFormatter.Number(netgenMesh.TransfiniteAngleDeg));
                return "netgen_mesh";
            }
            if (item is TetrahedralGmsh tetrahedralGmsh)
            {
                AddSolidGmshValues(arguments, tetrahedralGmsh);
                arguments.Add("optimize_first_order_solid=" +
                              PythonValueFormatter.Text(tetrahedralGmsh.OptimizeFirstOrderSolid.ToString()));
                arguments.Add("optimize_high_order=" +
                              PythonValueFormatter.Text(tetrahedralGmsh.OptimizeHighOrder.ToString()));
                return "tetrahedral_gmsh";
            }
            if (item is ShellMmg shellMmg)
            {
                AddShellMmgValues(arguments, shellMmg);
                return "shell_mmg";
            }
            if (item is MeshingParameters parameters)
            {
                AddMeshingParameterValues(arguments, parameters);
                return "meshing_parameters";
            }
            //
            throw new NotSupportedException("the mesh setup item type " + item.GetType().Name +
                                            " has no API form");
        }
        // The values every Gmsh item takes, without the two dimensional algorithm's angle
        private static void AddSolidGmshValues(List<string> arguments, GmshSetupItem item)
        {
            arguments.Add("algorithm_2d=" + PythonValueFormatter.Text(item.AlgorithmMesh2D.ToString()));
            arguments.Add("algorithm_3d=" + PythonValueFormatter.Text(item.AlgorithmMesh3D.ToString()));
            AddRecombinationValues(arguments, item);
        }
        // A surface item has no three dimensional algorithm of its own
        private static void AddSurfaceGmshValues(List<string> arguments, GmshSetupItem item)
        {
            arguments.Add("algorithm_2d=" + PythonValueFormatter.Text(item.AlgorithmMesh2D.ToString()));
            AddRecombinationValues(arguments, item);
        }
        // The minimum quality is written only while the item recombines and the angle only while it structures a
        // face, as the dialog shows them and the API takes them only then
        private static void AddRecombinationValues(List<string> arguments, GmshSetupItem item)
        {
            if (item.AlgorithmRecombine != GmshAlgorithmRecombineEnum.None)
                arguments.Add("recombine_min_quality=" + PythonValueFormatter.Number(item.RecombineMinQuality));
            arguments.Add("algorithm_recombine=" +
                          PythonValueFormatter.Text(item.AlgorithmRecombine.ToString()));
            arguments.Add("transfinite_three_sided=" +
                          PythonValueFormatter.Text(item.TransfiniteThreeSided.ToString()));
            arguments.Add("transfinite_four_sided=" +
                          PythonValueFormatter.Format(item.TransfiniteFourSided));
            if (item.IsTransfiniteThreeSided || item.TransfiniteFourSided)
                arguments.Add("transfinite_angle_deg=" + PythonValueFormatter.Number(item.TransfiniteAngleDeg));
        }
        // Gmsh, Mmg and the local mesh size limits use the sizes in either mode, and the dialog sizes the Netgen
        // mesh by the factors only with the relative switch on in its advanced view, the only state in which the
        // API takes them
        private static void AddMeshingParameterValues(List<string> arguments, MeshingParameters parameters)
        {
            bool relativeSizeUsed = parameters.RelativeSize && parameters.AdvancedView;
            arguments.Add("max_size=" + PythonValueFormatter.Number(parameters.MaxH));
            arguments.Add("min_size=" + PythonValueFormatter.Number(parameters.MinH));
            arguments.Add("grading=" + PythonValueFormatter.Number(parameters.Grading));
            arguments.Add("elements_per_edge=" + PythonValueFormatter.Number(parameters.ElementsPerEdge));
            arguments.Add("elements_per_curve=" + PythonValueFormatter.Number(parameters.ElementsPerCurve));
            arguments.Add("second_order=" + PythonValueFormatter.Format(parameters.SecondOrder));
            if (parameters.SecondOrder)
                arguments.Add("midside_nodes_on_geometry=" +
                              PythonValueFormatter.Format(parameters.MidsideNodesOnGeometry));
            arguments.Add("relative_size=" + PythonValueFormatter.Format(relativeSizeUsed));
            if (relativeSizeUsed)
            {
                arguments.Add("factor_max=" + PythonValueFormatter.Number(parameters.FactorMax));
                arguments.Add("factor_min=" + PythonValueFormatter.Number(parameters.FactorMin));
            }
            arguments.Add("split_compound_mesh=" + PythonValueFormatter.Format(parameters.SplitCompoundMesh));
            arguments.Add("merge_compound_parts=" +
                          PythonValueFormatter.Format(parameters.MergeCompoundParts));
            arguments.Add("merge_cells=" + PythonValueFormatter.Format(parameters.MergeCells));
        }
        private static void AddShellMmgValues(List<string> arguments, ShellMmg shellMmg)
        {
            arguments.Add("relative_size=" + PythonValueFormatter.Format(shellMmg.RelativeSize));
            if (shellMmg.RelativeSize)
                arguments.Add("relative_hausdorff=" + PythonValueFormatter.Number(shellMmg.RelativeHausdorff));
            else arguments.Add("hausdorff=" + PythonValueFormatter.Number(shellMmg.Hausdorff));
            arguments.Add("keep_model_edges=" + PythonValueFormatter.Format(shellMmg.KeepModelEdges));
        }
    }
}
