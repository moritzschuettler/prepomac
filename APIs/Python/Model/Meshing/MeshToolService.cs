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
using System;
using System.IO;

namespace APIs.Python.Model
{
    internal class MeshToolService
    {
        // Variables
        private readonly ModelResolver _modelResolver;
        private readonly SelectionService _selectionService;
        private readonly IPythonMeshGateway _meshGateway;


        // Constructors
        public MeshToolService(ModelResolver modelResolver, SelectionService selectionService,
                               IPythonMeshGateway meshGateway)
        {
            _modelResolver = modelResolver;
            _selectionService = selectionService;
            _meshGateway = meshGateway;
        }


        // Methods
        public void CreateBoundaryLayer(SelectionAPI selection, object thickness)
        {
            if (selection == null) throw new PythonAPIException("The argument 'selection' must not be None.");
            //
            SelectionCompilation compilation = _selectionService.CompileForView(selection, "femodel", "geometry");
            // The selection travels with the command, so a regeneration on changed geometry reselects it
            GeometrySelection geometrySelection =
                new GeometrySelection("Selection", compilation.Ids, compilation.Selection);
            EnsureCommandsAvailable();
            _meshGateway.CreateBoundaryLayer(geometrySelection,
                                             PythonValueConverter.ToDouble(thickness, "thickness"));
        }
        public void RemeshElements(SelectionAPI selection, object maxSize, object minSize, object hausdorff)
        {
            if (selection == null) throw new PythonAPIException("The argument 'selection' must not be None.");
            //
            EnsureCommandsAvailable();
            MeshingParameters meshingParameters = _meshGateway.GetDefaultMeshingParameters("Remeshing");
            if (meshingParameters == null)
                throw new PythonAPIException("The default meshing parameters are not available.");
            if (maxSize != null) meshingParameters.MaxH = PythonValueConverter.ToDouble(maxSize, "max_size");
            if (minSize != null) meshingParameters.MinH = PythonValueConverter.ToDouble(minSize, "min_size");
            if (hausdorff != null)
                meshingParameters.FactorHausdorff = PythonValueConverter.ToDouble(hausdorff, "hausdorff");
            //
            SelectionCompilation compilation = _selectionService.CompileForView(selection, "femodel", "geometry");
            RemeshingParameters remeshingParameters =
                new RemeshingParameters(null, RegionTypeEnum.Selection, meshingParameters);
            remeshingParameters.CreationData = compilation.Selection;
            remeshingParameters.CreationIds = compilation.Ids;
            //
            _meshGateway.RemeshElements(remeshingParameters);
        }
        public void ThickenShellMesh(object partNames, object thickness, int numberOfLayers, object offset,
                                     bool keepModelEdges)
        {
            string[] names = PythonStringListConverter.ToStringArrayOrSingle(partNames, "part_names");
            if (names == null || names.Length == 0)
                throw new PythonAPIException("The argument 'part_names' must not be empty.");
            //
            FeMesh mesh = _modelResolver.GetModel().Mesh;
            foreach (string name in names)
            {
                if (mesh == null || !mesh.Parts.ContainsKey(name))
                    throw new PythonAPIException("The model part '" + name + "' does not exist.");
            }
            if (numberOfLayers < 1)
                throw new PythonAPIException("The argument 'number_of_layers' must be larger than zero.");
            //
            EnsureCommandsAvailable();
            _meshGateway.ThickenShellMesh(names, PythonValueConverter.ToDouble(thickness, "thickness"),
                                          numberOfLayers,
                                          offset == null ? 0 : PythonValueConverter.ToDouble(offset, "offset"),
                                          keepModelEdges);
        }
        public void SplitPartMeshUsingSurface(SelectionAPI basePartSelection, SelectionAPI splitterSurfaceSelection,
                                              object offset, object maxSize, object minSize, object hausdorff,
                                              bool exact)
        {
            if (basePartSelection == null)
                throw new PythonAPIException("The argument 'base_part_selection' must not be None.");
            if (splitterSurfaceSelection == null)
                throw new PythonAPIException("The argument 'splitter_surface_selection' must not be None.");
            // This is a one time operation, not a tree item: the IDs record what was operated on and stay
            // valid on replay, because the preceding history reproduces the same mesh
            SelectionCompilation baseCompilation =
                _selectionService.CompileForView(basePartSelection, "femodel", "geometry");
            SelectionCompilation splitterCompilation =
                _selectionService.CompileForView(splitterSurfaceSelection, "femodel", "surfaces");
            //
            SplitPartMeshData splitPartMeshData =
                new SplitPartMeshData(RegionTypeEnum.Selection, null, RegionTypeEnum.Selection, null);
            splitPartMeshData.BasePartCreationData = baseCompilation.Selection;
            splitPartMeshData.BasePartCreationIds = baseCompilation.Ids;
            splitPartMeshData.SplitterSurfaceCreationData = splitterCompilation.Selection;
            splitPartMeshData.SplitterSurfaceCreationIds = splitterCompilation.Ids;
            if (offset != null) splitPartMeshData.Offset = PythonValueConverter.ToDouble(offset, "offset");
            if (maxSize != null) splitPartMeshData.MaxH = PythonValueConverter.ToDouble(maxSize, "max_size");
            if (minSize != null) splitPartMeshData.MinH = PythonValueConverter.ToDouble(minSize, "min_size");
            if (hausdorff != null)
                splitPartMeshData.Hausdorff = PythonValueConverter.ToDouble(hausdorff, "hausdorff");
            splitPartMeshData.Exact = exact;
            //
            EnsureCommandsAvailable();
            _meshGateway.SplitPartMeshUsingSurface(splitPartMeshData);
        }
        public void UpdateNodalCoordinatesFromFile(string fileName)
        {
            if (fileName == null || fileName.Trim().Length == 0)
                throw new PythonAPIException("The argument 'file_name' must not be empty.");
            //
            string fullFileName;
            try
            {
                fullFileName = Path.GetFullPath(fileName);
            }
            catch (Exception ex)
            {
                throw new PythonAPIException("The argument 'file_name' is invalid: " + ex.Message);
            }
            if (!File.Exists(fullFileName))
                throw new PythonAPIException("The file '" + fullFileName + "' does not exist.");
            //
            EnsureCommandsAvailable();
            _meshGateway.UpdateNodalCoordinatesFromFile(fullFileName);
        }
        //
        private void EnsureCommandsAvailable()
        {
            if (_meshGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
    }
}
