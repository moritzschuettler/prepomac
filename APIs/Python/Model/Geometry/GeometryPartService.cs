// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using System.Collections.Generic;
using System.Drawing;

namespace APIs.Python.Model
{
    internal class GeometryPartService : PartService
    {
        // Properties
        protected override string ViewName { get { return "geometry"; } }


        // Constructors
        public GeometryPartService(ModelResolver modelResolver, SelectionService selectionService,
                                   IPythonMeshGateway meshGateway, IPythonModelGateway modelGateway)
            : base(modelResolver, selectionService, meshGateway, modelGateway)
        {
        }


        // Methods
        public string GetRecognizedMeshType(string partName)
        {
            ResolvePart(partName);
            return ModelGateway.GetRecognizedMeshType(partName);
        }
        public void CreateCompound(string[] partNames)
        {
            EnsureCommandsAvailable();
            MeshGateway.CreateAndImportCompoundPart(partNames);
        }
        public void RegenerateCompound(string[] partNames)
        {
            EnsureCommandsAvailable();
            MeshGateway.RegenerateCompoundParts(partNames);
        }
        public void SwapGeometries(string partName1, string partName2)
        {
            ResolvePart(partName1);
            ResolvePart(partName2);
            EnsureCommandsAvailable();
            MeshGateway.SwapPartGeometries(partName1, partName2);
        }
        public void ConvertShellToSolid(string[] partNames)
        {
            EnsureCommandsAvailable();
            MeshGateway.ConvertShellToSolid(partNames);
        }
        public void ConvertSolidToShell(string[] partNames)
        {
            EnsureCommandsAvailable();
            MeshGateway.ConvertSolidToShell(partNames);
        }
        public void FindEdgesByAngle(string[] partNames, object edgeAngle)
        {
            double angle = PythonValueConverter.ToDouble(edgeAngle, "edge_angle");
            EnsureCommandsAvailable();
            MeshGateway.FindEdgesByAngleForGeometryParts(partNames, angle);
        }
        public Dictionary<string, int> FlipFaceOrientations(SelectionAPI selection)
        {
            GeometrySelection geometrySelection = BuildGeometrySelection(selection, "selection");
            EnsureCommandsAvailable();
            return MeshGateway.FlipFaceOrientations(geometrySelection);
        }
        public Dictionary<string, int> Defeature(SelectionAPI selection)
        {
            GeometrySelection geometrySelection = BuildGeometrySelection(selection, "selection");
            EnsureCommandsAvailable();
            return MeshGateway.Defeature(geometrySelection);
        }
        public string[] ExtractSurface(SelectionAPI selection, object offset)
        {
            GeometrySelection geometrySelection = BuildGeometrySelection(selection, "selection");
            double offsetValue = offset == null ? 0 : PythonValueConverter.ToDouble(offset, "offset");
            EnsureCommandsAvailable();
            return MeshGateway.ExtractSurface(geometrySelection, offsetValue);
        }
        public Dictionary<string, int> SplitFace(SelectionAPI surfaceSelection, SelectionAPI verticesSelection)
        {
            GeometrySelection surfaces = BuildGeometrySelection(surfaceSelection, "surface_selection");
            GeometrySelection vertices = BuildGeometrySelection(verticesSelection, "vertices_selection");
            EnsureCommandsAvailable();
            return MeshGateway.SplitFaceUsingTwoPoints(surfaces, vertices);
        }
        public void FlipStlNormals(string[] partNames)
        {
            EnsureCommandsAvailable();
            MeshGateway.FlipStlPartSurfacesNormal(partNames);
        }
        public Dictionary<string, int> DeleteStlFaces(SelectionAPI selection)
        {
            GeometrySelection geometrySelection = BuildGeometrySelection(selection, "selection");
            EnsureCommandsAvailable();
            return MeshGateway.DeleteStlPartFaces(geometrySelection);
        }
        //
        protected override PartProperties GetPartProperties(string partName)
        {
            return MeshGateway.GetGeometryPartProperties(partName);
        }
        protected override FeMesh GetMeshOrNull()
        {
            return ModelResolver.GetModel().Geometry;
        }
        protected override void ReplacePartProperties(string oldPartName, PartProperties properties)
        {
            MeshGateway.ReplaceGeometryPartProperties(oldPartName, properties);
        }
        protected override void SetColorForParts(string[] partNames, Color color)
        {
            MeshGateway.SetColorForGeometryParts(partNames, color);
        }
        protected override void ResetColorForParts(string[] partNames)
        {
            MeshGateway.ResetColorForGeometryParts(partNames);
        }
        protected override void SetTransparencyForParts(string[] partNames, byte alpha)
        {
            MeshGateway.SetTransparencyForGeometryParts(partNames, alpha);
        }
        protected override void HideParts(string[] partNames)
        {
            MeshGateway.HideGeometryParts(partNames);
        }
        protected override void ShowParts(string[] partNames)
        {
            MeshGateway.ShowGeometryParts(partNames);
        }
        protected override void RemoveParts(string[] partNames)
        {
            MeshGateway.RemoveGeometryParts(partNames);
        }
        protected override void ScaleParts(string[] partNames, double[] scaleCenter, double[] scaleFactors, bool copy)
        {
            MeshGateway.ScaleGeometryParts(partNames, scaleCenter, scaleFactors, copy);
        }
    }
}
