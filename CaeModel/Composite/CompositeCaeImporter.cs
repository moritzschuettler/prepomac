// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
// Contributors:
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CaeGlobals;
using CaeMesh;

namespace CaeModel
{
    public class CompositeCaeImportResult
    {
        public List<Material> Materials = new List<Material>();
        public List<FeElementSet> ElementSets = new List<FeElementSet>();
        public CompositeLayup Layup;
        public string[] AddedPartNames;
        public string MeshSourcePlyName;
        public List<string> Log = new List<string>();

        public bool MeshWasCreated { get { return AddedPartNames != null && AddedPartNames.Length > 0; } }

        public string GetSummary()
        {
            return string.Join(Environment.NewLine, Log);
        }
    }

    // Imports a composite layup from a Composite CAE HDF5 file: the plies of the file are mapped onto the
    // elements of the structural mesh by core sampling and each mapped ply becomes a layer of a new layup.
    // The created materials, element sets and the layup are returned but not registered in the model so
    // that the caller can add them together with their tree nodes.
    public static class CompositeCaeImporter
    {
        public static CompositeCaeImportResult Import(FeModel model, CompositeCaeData data, string layupName,
                                                      string regionName, double drillDepth, double tolerance)
        {
            if (model == null) throw new CaeException("The model is not defined.");
            if (data == null || data.Plies.Count == 0) throw new CaeException("The file contains no plies.");
            //
            CompositeCaeImportResult result = new CompositeCaeImportResult();
            //
            foreach (string warning in data.Warnings) result.Log.Add("Warning: " + warning);
            //
            CreateMeshIfMissing(model, data, result);
            //
            FeMesh mesh = model.Mesh;
            if (mesh == null || mesh.Elements.Count == 0)
                throw new CaeException("The model contains no mesh and no mesh could be created from the file.");
            //
            CreateMaterials(model, data, result);
            //
            int[] targetElementIds = CompositeCaeMapper.GetTargetElementIds(mesh, regionName);
            if (targetElementIds.Length == 0)
                throw new CaeException("No elements were found in the region: " +
                                       (string.IsNullOrEmpty(regionName) ? "<all shell elements>" : regionName));
            //
            Dictionary<int, List<int>> plyElementIds =
                CompositeCaeMapper.MapPliesToElements(mesh, targetElementIds, data, drillDepth, tolerance,
                                                      message => result.Log.Add(message));
            //
            CreateLayup(model, data, layupName, plyElementIds, result);
            //
            result.Log.Add("Plies in file: " + data.Plies.Count + " (" + data.GetLayerCount() + " layers, " +
                           data.GetTriangleCount() + " triangles).");
            result.Log.Add("Plies mapped: " + plyElementIds.Count + " of " + data.Plies.Count + ".");
            result.Log.Add("Elements sampled: " + targetElementIds.Length + ".");
            //
            return result;
        }
        // Uses the finite element mesh stored in the file when the model has none of its own
        private static void CreateMeshIfMissing(FeModel model, CompositeCaeData data, CompositeCaeImportResult result)
        {
            if (model.Mesh != null && model.Mesh.Elements.Count > 0) return;
            //
            int layerId;
            int plyCount;
            FeMesh mesh = CompositeCaeMeshBuilder.BuildMesh(data, out layerId, out plyCount);
            if (mesh == null)
            {
                result.Log.Add("Warning: the model has no mesh and no mesh could be created from the file.");
                return;
            }
            //
            HashSet<string> reservedPartNames = model.GetReservedPartNames();
            model.ImportMesh(mesh, reservedPartNames, true, false);
            //
            result.MeshSourcePlyName = "Layer " + layerId;
            result.AddedPartNames = model.Mesh.Parts.Keys.Where(x => !reservedPartNames.Contains(x)).ToArray();
            result.Log.Add("The model had no mesh; the surfaces of the " + plyCount + " plies of layer " + layerId +
                           " (" + mesh.Elements.Count + " elements) were imported from the file as the structural mesh.");
        }
        private static void CreateMaterials(FeModel model, CompositeCaeData data, CompositeCaeImportResult result)
        {
            int created = 0;
            int reused = 0;
            int nameOnly = 0;
            //
            foreach (CompositeCaeMaterial source in data.Materials)
            {
                if (model.Materials.ContainsKey(source.Name)) { reused++; continue; }
                if (!source.HasMechanicalData) { nameOnly++; continue; }
                //
                Material material = new Material(source.Name);
                material.Description = "Imported from " + Path.GetFileName(data.FileName);
                material.CompositeMaterialType = GetCompositeMaterialType(source.Architecture);
                material.AddProperty(new ElasticEngineeringConstants(new double[][] { new double[] {
                    source.E1, source.E2, source.E3,
                    source.Nu12, source.Nu13, source.Nu23,
                    source.G12, source.G13, source.G23, 0 } }));
                //
                result.Materials.Add(material);
                created++;
            }
            //
            result.Log.Add("Materials: " + created + " created, " + reused + " reused, " + nameOnly +
                           " referenced by name only.");
            if (nameOnly > 0)
                result.Log.Add("The materials referenced by name only have to be defined in the model.");
        }
        private static CompositeMaterialType GetCompositeMaterialType(string architecture)
        {
            if (architecture == null) return CompositeMaterialType.None;
            //
            switch (architecture.Trim().ToLowerInvariant())
            {
                case "uni":
                case "ud":
                case "unidirectional": return CompositeMaterialType.Unidirectional;
                case "woven":
                case "biax":
                case "biaxial": return CompositeMaterialType.Biaxial;
                case "triax":
                case "triaxial": return CompositeMaterialType.Triaxial;
                case "ncf": return CompositeMaterialType.NCF;
                default: return CompositeMaterialType.None;
            }
        }
        private static void CreateLayup(FeModel model, CompositeCaeData data, string layupName,
                                        Dictionary<int, List<int>> plyElementIds, CompositeCaeImportResult result)
        {
            if (plyElementIds.Count == 0)
                throw new CaeException("None of the " + data.Plies.Count + " plies of the file could be mapped onto " +
                                       "the mesh. Check the position of the mesh and increase the drill depth.");
            //
            layupName = NamedClass.GetErrorFreeName(layupName, "Layup", null);
            layupName = model.CompositeLayups.Keys.ToArray().GetNextNumberedKey(layupName);
            //
            HashSet<string> reservedElementSetNames = model.Mesh.GetReservedElementSetNames();
            //
            CompositeLayup layup = new CompositeLayup(layupName, plyElementIds.Count,
                                                      CompositeElementType.ConventionalShell);
            //
            int plyCount = 0;
            foreach (int plyIndex in plyElementIds.Keys.OrderBy(x => x))
            {
                CompositeCaePly source = data.Plies[plyIndex];
                List<int> elementIds = plyElementIds[plyIndex];
                elementIds.Sort();
                //
                string elementSetName = NamedClass.GetErrorFreeName(layupName + "_" + source.Name, "Set", null);
                elementSetName = reservedElementSetNames.GetNextNumberedKey(elementSetName);
                reservedElementSetNames.Add(elementSetName);
                //
                FeElementSet elementSet = new FeElementSet(elementSetName, elementIds.ToArray());
                result.ElementSets.Add(elementSet);
                //
                Ply ply = layup.Plies[plyCount++];
                ply.Name = source.Name;
                ply.Region = elementSetName;
                ply.Material = source.MaterialName == null ? "" : source.MaterialName;
                ply.Thickness = source.Thickness > 0 ? source.Thickness : 1.0;
                ply.RotationAngle = source.Orientation;
            }
            //
            result.Layup = layup;
            result.Log.Add("Composite layup '" + layupName + "' created with " + plyCount + " plies.");
        }
    }
}
