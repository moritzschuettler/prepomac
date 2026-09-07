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
    // type, and the extrude and revolve items add the layer sizing on top of them.
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
            string method = (oldName == null ? "add_" : "replace_") + AddTypeArguments(arguments, item);
            //
            writer.Statement(writer.Call(SetupItems, method, arguments.ToArray()));
            PythonModelItemEmitter.EmitState(writer, SetupItems, item);
            writer.CountEmittedCommand();
        }
        // The most derived type first, because the Gmsh items form a chain
        private static string AddTypeArguments(List<string> arguments, MeshSetupItem item)
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
                arguments.Add("preview=" + PythonValueFormatter.Format(quadWild.Preview));
                arguments.Add("remesh=" + PythonValueFormatter.Format(quadWild.Remesh));
                arguments.Add("smooth=" + PythonValueFormatter.Format(quadWild.Smooth));
                arguments.Add("alpha=" + PythonValueFormatter.Number(quadWild.Alpha));
                arguments.Add("triangulation_mesher=" +
                              PythonValueFormatter.Text(quadWild.TriangulationMesher.ToString()));
                arguments.Add("scale_factor=" + PythonValueFormatter.Number(quadWild.ScaleFactor));
                arguments.Add("flow_profile=" + PythonValueFormatter.Text(quadWild.FlowProfile.ToString()));
                return "shell_quad_wild";
            }
            if (item is ExtrudeMesh extrude)
            {
                arguments.Add("direction=" + PythonValueFormatter.List(extrude.Direction));
                arguments.Add("extrude_center=" + PythonValueFormatter.List(extrude.ExtrudeCenter));
                AddSurfaceGmshValues(arguments, extrude);
                AddLayerValues(arguments, extrude);
                return "extrude_mesh";
            }
            if (item is RevolveMesh revolve)
            {
                arguments.Add("axis_center=" + PythonValueFormatter.List(revolve.AxisCenter));
                arguments.Add("axis_direction=" + PythonValueFormatter.List(revolve.AxisDirection));
                arguments.Add("angle=" + PythonValueFormatter.Number(revolve.AngleDeg));
                arguments.Add("middle_r=" + PythonValueFormatter.Number(revolve.MiddleR));
                arguments.Add("create_full_360_model=" +
                              PythonValueFormatter.Format(revolve.CreateFull360Model));
                AddSurfaceGmshValues(arguments, revolve);
                AddLayerValues(arguments, revolve);
                return "revolve_mesh";
            }
            if (item is SweepMesh sweep)
            {
                arguments.Add("number_of_global_smooth_steps=" +
                              PythonValueFormatter.Format(sweep.NumberOfGlobalSmoothSteps));
                AddSolidGmshValues(arguments, sweep);
                arguments.Add("optimize_first_order_shell=" +
                              PythonValueFormatter.Text(sweep.OptimizeFirstOrderShell.ToString()));
                arguments.Add("elements_per_layer=" +
                              PythonValueFormatter.List(sweep.NumOfElementsPerLayer));
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
                              PythonValueFormatter.Text(transfinite.AlgorithmRecombine.ToString()));
                arguments.Add("transfinite_angle=" +
                              PythonValueFormatter.Number(transfinite.TransfiniteAngleDeg));
                return "transfinite_mesh";
            }
            if (item is ShellGmsh shellGmsh)
            {
                AddSurfaceGmshValues(arguments, shellGmsh);
                arguments.Add("optimize_first_order_shell=" +
                              PythonValueFormatter.Text(shellGmsh.OptimizeFirstOrderShell.ToString()));
                return "shell_gmsh";
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
            arguments.Add("recombine_min_quality=" + PythonValueFormatter.Number(item.RecombineMinQuality));
            arguments.Add("algorithm_recombine=" +
                          PythonValueFormatter.Text(item.AlgorithmRecombine.ToString()));
            arguments.Add("transfinite_three_sided=" +
                          PythonValueFormatter.Format(item.TransfiniteThreeSided));
            arguments.Add("transfinite_four_sided=" +
                          PythonValueFormatter.Format(item.TransfiniteFourSided));
            arguments.Add("transfinite_angle=" + PythonValueFormatter.Number(item.TransfiniteAngleDeg));
        }
        // A surface item has no three dimensional algorithm of its own
        private static void AddSurfaceGmshValues(List<string> arguments, GmshSetupItem item)
        {
            arguments.Add("algorithm_2d=" + PythonValueFormatter.Text(item.AlgorithmMesh2D.ToString()));
            arguments.Add("recombine_min_quality=" + PythonValueFormatter.Number(item.RecombineMinQuality));
            arguments.Add("algorithm_recombine=" +
                          PythonValueFormatter.Text(item.AlgorithmRecombine.ToString()));
            arguments.Add("transfinite_three_sided=" +
                          PythonValueFormatter.Format(item.TransfiniteThreeSided));
            arguments.Add("transfinite_four_sided=" +
                          PythonValueFormatter.Format(item.TransfiniteFourSided));
            arguments.Add("transfinite_angle=" + PythonValueFormatter.Number(item.TransfiniteAngleDeg));
        }
        // How an extruded or revolved mesh is divided along its path
        private static void AddLayerValues(List<string> arguments, GmshSetupItem item)
        {
            arguments.Add("element_size_type=" + PythonValueFormatter.Text(item.ElementSizeType.ToString()));
            arguments.Add("element_scale_factor=" + PythonValueFormatter.Number(item.ElementScaleFactor));
            arguments.Add("number_of_elements=" + PythonValueFormatter.Format(item.NumberOfElements));
            arguments.Add("layer_sizes=" + PythonValueFormatter.List(item.LayerSizes));
            arguments.Add("elements_per_layer=" + PythonValueFormatter.List(item.NumOfElementsPerLayer));
        }
        private static void AddMeshingParameterValues(List<string> arguments, MeshingParameters parameters)
        {
            arguments.Add("max_size=" + PythonValueFormatter.Number(parameters.MaxH));
            arguments.Add("min_size=" + PythonValueFormatter.Number(parameters.MinH));
            arguments.Add("fineness=" + PythonValueFormatter.Number(parameters.Fineness));
            arguments.Add("grading=" + PythonValueFormatter.Number(parameters.Grading));
            arguments.Add("elements_per_edge=" + PythonValueFormatter.Number(parameters.ElementsPerEdge));
            arguments.Add("elements_per_curve=" + PythonValueFormatter.Number(parameters.ElementsPerCurve));
            arguments.Add("second_order=" + PythonValueFormatter.Format(parameters.SecondOrder));
            arguments.Add("quad_dominated=" + PythonValueFormatter.Format(parameters.QuadDominated));
            arguments.Add("midside_nodes_on_geometry=" +
                          PythonValueFormatter.Format(parameters.MidsideNodesOnGeometry));
            arguments.Add("use_mmg=" + PythonValueFormatter.Format(parameters.UseMmg));
            arguments.Add("keep_model_edges=" + PythonValueFormatter.Format(parameters.KeepModelEdges));
            arguments.Add("relative_size=" + PythonValueFormatter.Format(parameters.RelativeSize));
            arguments.Add("factor_max=" + PythonValueFormatter.Number(parameters.FactorMax));
            arguments.Add("factor_min=" + PythonValueFormatter.Number(parameters.FactorMin));
            arguments.Add("factor_hausdorff=" + PythonValueFormatter.Number(parameters.FactorHausdorff));
            arguments.Add("hausdorff=" + PythonValueFormatter.Number(parameters.Hausdorff));
            arguments.Add("optimize_steps_2d=" + PythonValueFormatter.Format(parameters.OptimizeSteps2D));
            arguments.Add("optimize_steps_3d=" + PythonValueFormatter.Format(parameters.OptimizeSteps3D));
            arguments.Add("split_compound_mesh=" + PythonValueFormatter.Format(parameters.SplitCompoundMesh));
            arguments.Add("merge_compound_parts=" +
                          PythonValueFormatter.Format(parameters.MergeCompoundParts));
        }
    }
}
