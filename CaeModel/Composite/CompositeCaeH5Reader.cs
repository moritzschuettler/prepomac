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
using System.Globalization;
using System.IO;
using System.Linq;
using CaeGlobals;
using CaeMesh;
using PureHDF;
using PureHDF.VOL.Native;

namespace CaeModel
{
    // Reader for the "Composite CAE" HDF5 specification (format_version 1.x) written by composite
    // manufacturing tools such as 9T Labs or ANSYS ACP:
    //
    //  /composite_cae/components/componentN   TITLE, layer_id, orientation, type='ply'
    //      -> material        soft link to /composite_cae/material_data/fabrics/fN
    //      -> element_set     soft link to /composite_cae/meshes/mN/element_sets/esN
    //      -> data_map        soft link to /composite_cae/data_maps/data_mapN
    //      -> component_group soft link to /composite_cae/component_groups/groupN
    //  /composite_cae/meshes/mN               nodes, element_nodes, element_types, element_sets
    //
    public static class CompositeCaeH5Reader
    {
        private const string RootPath = "/composite_cae";


        // Methods
        public static CompositeCaeData Read(string fileName)
        {
            if (!File.Exists(fileName)) throw new CaeException("The file does not exist: " + fileName);
            //
            CompositeCaeData data = new CompositeCaeData();
            data.FileName = fileName;
            //
            NativeFile file;
            try
            {
                file = H5File.OpenRead(fileName);
            }
            catch (Exception ex)
            {
                throw new CaeException("The file is not a readable HDF5 file: " + fileName +
                                       Environment.NewLine + ex.Message);
            }
            //
            using (file)
            {
                ReadData(file, data);
            }
            //
            if (data.Plies.Count == 0) throw new CaeException("No plies were found in the file: " + fileName);
            //
            return data;
        }
        private static void ReadData(IH5Group file, CompositeCaeData data)
        {
            if (!file.LinkExists(RootPath))
                throw new CaeException("The file does not contain a '" + RootPath + "' group and is thus not a " +
                                       "Composite CAE HDF5 file: " + data.FileName);
            //
            IH5Group root = file.Group(RootPath);
            data.FormatVersion = GetStringAttribute(root, "format_version");
            data.Vendor = GetStringAttribute(root, "vendor");
            data.LengthUnit = GetStringAttribute(root, "length_unit");
            data.AngleUnit = GetStringAttribute(root, "angles");
            data.MassUnit = GetStringAttribute(root, "mass_unit");
            //
            if (data.FormatVersion != null && !data.FormatVersion.StartsWith("1."))
                data.Warnings.Add("Unsupported Composite CAE format version '" + data.FormatVersion +
                                  "'. The file is read as version 1.x.");
            //
            ReadMaterials(root, data);
            ReadPlies(root, data);
        }


        // Materials
        private static void ReadMaterials(IH5Group root, CompositeCaeData data)
        {
            if (!root.LinkExists("material_data")) return;
            //
            IH5Group materialData = root.Group("material_data");
            HashSet<string> usedNames = new HashSet<string>();
            //
            foreach (string collectionName in new string[] { "fabrics", "stackups", "sublaminates" })
            {
                if (!materialData.LinkExists(collectionName)) continue;
                //
                IH5Group collection = materialData.Group(collectionName);
                foreach (string name in GetChildNames(collection))
                {
                    IH5Group materialGroup = collection.Group(name);
                    CompositeCaeMaterial material = new CompositeCaeMaterial();
                    material.RawName = GetStringAttribute(materialGroup, "TITLE");
                    if (string.IsNullOrEmpty(material.RawName)) material.RawName = name;
                    material.Name = GetUniqueSafeName(material.RawName, "Material", usedNames);
                    //
                    if (materialGroup.LinkExists("mechanical"))
                    {
                        IH5Group mechanical = materialGroup.Group("mechanical");
                        material.Architecture = GetStringAttribute(mechanical, "architecture");
                        material.E1 = GetDoubleAttribute(mechanical, "E1", 0);
                        material.E2 = GetDoubleAttribute(mechanical, "E2", 0);
                        material.E3 = GetDoubleAttribute(mechanical, "E3", 0);
                        material.G12 = GetDoubleAttribute(mechanical, "G12", 0);
                        // The specification names the 3-1 shear modulus G31
                        material.G13 = GetDoubleAttribute(mechanical, "G31", GetDoubleAttribute(mechanical, "G13", 0));
                        material.G23 = GetDoubleAttribute(mechanical, "G23", 0);
                        material.Nu12 = GetDoubleAttribute(mechanical, "v12", 0);
                        material.Nu13 = GetDoubleAttribute(mechanical, "v13", 0);
                        material.Nu23 = GetDoubleAttribute(mechanical, "v23", 0);
                        material.CuredThickness = GetDoubleAttribute(mechanical, "cured_thickness", 0);
                        material.WeightPerArea = GetDoubleAttribute(mechanical, "weight_per_area", 0);
                        //
                        material.HasMechanicalData = material.E1 > 0 && material.E2 > 0 && material.E3 > 0 &&
                                                     material.G12 > 0 && material.G13 > 0 && material.G23 > 0;
                        if (!material.HasMechanicalData)
                            data.Warnings.Add("The material '" + material.RawName + "' has an incomplete set of " +
                                              "mechanical properties and is imported by name only.");
                    }
                    //
                    data.Materials.Add(material);
                }
            }
        }


        // Plies
        private static void ReadPlies(IH5Group root, CompositeCaeData data)
        {
            if (!root.LinkExists("components"))
                throw new CaeException("The file does not contain a '" + RootPath + "/components' group.");
            //
            IH5Group components = root.Group("components");
            IH5Group meshes = root.LinkExists("meshes") ? root.Group("meshes") : null;
            if (meshes == null) throw new CaeException("The file does not contain a '" + RootPath + "/meshes' group.");
            //
            Dictionary<int, string> meshNameBySuffix = new Dictionary<int, string>();
            List<string> meshNames = GetChildNames(meshes);
            foreach (string meshName in meshNames)
            {
                int suffix = GetNameSuffix(meshName);
                if (suffix >= 0 && !meshNameBySuffix.ContainsKey(suffix)) meshNameBySuffix.Add(suffix, meshName);
            }
            //
            Dictionary<string, CompositeCaeSurface> surfaceCache = new Dictionary<string, CompositeCaeSurface>();
            HashSet<string> usedNames = new HashSet<string>();
            List<string> componentNames = GetChildNames(components);
            componentNames.Sort(CompareBySuffix);
            //
            foreach (string componentName in componentNames)
            {
                IH5Group component = components.Group(componentName);
                string type = GetStringAttribute(component, "type");
                if (type != null && type != "ply") continue;
                //
                CompositeCaePly ply = new CompositeCaePly();
                ply.RawName = GetStringAttribute(component, "TITLE");
                if (string.IsNullOrEmpty(ply.RawName)) ply.RawName = componentName;
                ply.Name = GetUniqueSafeName(ply.RawName, "Ply", usedNames);
                ply.LayerId = (int)GetDoubleAttribute(component, "layer_id", 0);
                ply.Orientation = GetDoubleAttribute(component, "orientation", 0);
                //
                if (component.LinkExists("component_group"))
                    ply.GroupName = GetStringAttribute(component.Group("component_group"), "TITLE");
                //
                int[] elementSelection = null;
                if (component.LinkExists("element_set"))
                    elementSelection = ReadIntArray(component.Dataset("element_set"));
                //
                ply.MaterialName = GetPlyMaterialName(component, data);
                ply.Thickness = GetPlyThickness(component, elementSelection, data, ply.MaterialName);
                //
                string meshName = ResolveMeshName(componentName, elementSelection, meshes, meshNames, meshNameBySuffix, data);
                if (meshName == null)
                {
                    data.Warnings.Add("The ply surface mesh of component '" + componentName + "' could not be resolved. " +
                                      "The ply is skipped.");
                    continue;
                }
                //
                string cacheKey = meshName + "|" + (elementSelection == null ? "all" : elementSelection.Length.ToString());
                CompositeCaeSurface surface;
                if (!surfaceCache.TryGetValue(cacheKey, out surface))
                {
                    surface = ReadSurface(meshes.Group(meshName), elementSelection);
                    surfaceCache.Add(cacheKey, surface);
                }
                if (surface == null || surface.TriangleCount == 0)
                {
                    data.Warnings.Add("The ply surface mesh of component '" + componentName + "' is empty. " +
                                      "The ply is skipped.");
                    continue;
                }
                ply.Surface = surface;
                //
                data.Plies.Add(ply);
            }
            // Bottom to top
            data.Plies = data.Plies.OrderBy(x => x.LayerId).ToList();
        }
        private static string GetPlyMaterialName(IH5Group component, CompositeCaeData data)
        {
            if (!component.LinkExists("material")) return null;
            //
            string rawName = GetStringAttribute(component.Group("material"), "TITLE");
            if (string.IsNullOrEmpty(rawName)) return null;
            //
            foreach (CompositeCaeMaterial material in data.Materials)
            {
                if (material.RawName == rawName) return material.Name;
            }
            // The material is not part of material_data
            CompositeCaeMaterial newMaterial = new CompositeCaeMaterial();
            newMaterial.RawName = rawName;
            newMaterial.Name = NamedClass.GetErrorFreeName(rawName, "Material", null);
            data.Materials.Add(newMaterial);
            //
            return newMaterial.Name;
        }
        private static double GetPlyThickness(IH5Group component, int[] elementSelection, CompositeCaeData data,
                                              string materialName)
        {
            if (component.LinkExists("data_map"))
            {
                IH5Group dataMap = component.Group("data_map");
                if (dataMap.LinkExists("thicknesses"))
                {
                    double[] thicknesses = ReadDoubleArray(dataMap.Dataset("thicknesses"));
                    if (thicknesses != null && thicknesses.Length > 0)
                    {
                        // A ply has a single thickness value; use the average of the per element values
                        double sum = 0;
                        int count = 0;
                        foreach (double thickness in thicknesses)
                        {
                            if (thickness > 0) { sum += thickness; count++; }
                        }
                        if (count > 0) return sum / count;
                    }
                }
            }
            // Fall back to the cured thickness of the material
            CompositeCaeMaterial material = materialName == null ? null : data.GetMaterial(materialName);
            if (material != null && material.CuredThickness > 0) return material.CuredThickness;
            //
            return 1.0;
        }
        // The Composite CAE specification links a component to its mesh with a soft link. PureHDF resolves
        // soft links transparently and does not expose the link target, so the mesh is resolved from the
        // shared numeric suffix of the component and the mesh group and the result is validated.
        private static string ResolveMeshName(string componentName, int[] elementSelection, IH5Group meshes,
                                              List<string> meshNames, Dictionary<int, string> meshNameBySuffix,
                                              CompositeCaeData data)
        {
            // A single mesh shared by all components
            if (meshNames.Count == 1) return meshNames[0];
            //
            int selectionCount = elementSelection == null ? -1 : elementSelection.Length;
            int suffix = GetNameSuffix(componentName);
            string meshName;
            if (suffix >= 0 && meshNameBySuffix.TryGetValue(suffix, out meshName) &&
                IsMeshCompatible(meshes.Group(meshName), selectionCount))
            {
                return meshName;
            }
            // Fall back to the only mesh with a compatible element count
            string candidate = null;
            foreach (string name in meshNames)
            {
                if (!IsMeshCompatible(meshes.Group(name), selectionCount)) continue;
                if (candidate != null) return null;     // ambiguous
                candidate = name;
            }
            if (candidate != null)
            {
                data.Warnings.Add("The ply surface mesh of component '" + componentName + "' was resolved to '" +
                                  candidate + "' by its element count.");
            }
            return candidate;
        }
        private static bool IsMeshCompatible(IH5Group mesh, int selectionCount)
        {
            if (!mesh.LinkExists("element_nodes")) return false;
            if (selectionCount < 0) return true;
            //
            if (mesh.LinkExists("element_sets"))
            {
                foreach (string name in GetChildNames(mesh.Group("element_sets")))
                {
                    if (mesh.Group("element_sets").Dataset(name).Space.Dimensions[0] == (ulong)selectionCount) return true;
                }
                return false;
            }
            return mesh.Dataset("element_nodes").Space.Dimensions[0] == (ulong)selectionCount;
        }
        private static CompositeCaeSurface ReadSurface(IH5Group mesh, int[] elementSelection)
        {
            double[] flatNodes = ReadDoubleArray(mesh.Dataset("nodes"));
            int[][] connectivity = ReadJaggedIntArray(mesh.Dataset("element_nodes"));
            if (flatNodes == null || connectivity == null) return null;
            //
            int nodeCount = flatNodes.Length / 3;
            // Element indices to read; the element set holds indices into the element arrays of the mesh
            List<int> elementIndices = new List<int>();
            if (elementSelection == null)
            {
                for (int i = 0; i < connectivity.Length; i++) elementIndices.Add(i);
            }
            else
            {
                foreach (int index in elementSelection)
                {
                    if (index >= 0 && index < connectivity.Length) elementIndices.Add(index);
                }
            }
            // Compact the nodes so that the bounding box of the ply surface is tight
            Dictionary<int, int> nodeMap = new Dictionary<int, int>();
            List<double[]> nodes = new List<double[]>();
            List<int[]> elements = new List<int[]>();
            List<int[]> triangles = new List<int[]>();
            BoundingBox box = new BoundingBox();
            //
            foreach (int elementIndex in elementIndices)
            {
                int[] sourceIds = connectivity[elementIndex];
                if (sourceIds == null || sourceIds.Length < 3) continue;
                //
                int[] localIds = new int[sourceIds.Length];
                bool valid = true;
                for (int i = 0; i < sourceIds.Length; i++)
                {
                    int sourceId = sourceIds[i];
                    if (sourceId < 0 || sourceId >= nodeCount) { valid = false; break; }
                    //
                    int localId;
                    if (!nodeMap.TryGetValue(sourceId, out localId))
                    {
                        localId = nodes.Count;
                        double[] coor = new double[] { flatNodes[3 * sourceId], flatNodes[3 * sourceId + 1],
                                                       flatNodes[3 * sourceId + 2] };
                        nodes.Add(coor);
                        nodeMap.Add(sourceId, localId);
                        box.IncludeCoor(coor);
                    }
                    localIds[i] = localId;
                }
                if (!valid) continue;
                //
                elements.Add(localIds);
                // Fan triangulation of the corner nodes
                int cornerCount = GetCornerCount(localIds.Length);
                for (int i = 1; i < cornerCount - 1; i++)
                {
                    triangles.Add(new int[] { localIds[0], localIds[i], localIds[i + 1] });
                }
            }
            //
            CompositeCaeSurface surface = new CompositeCaeSurface();
            surface.Nodes = nodes.ToArray();
            surface.Elements = elements.ToArray();
            surface.Triangles = triangles.ToArray();
            surface.Box = box;
            //
            return surface;
        }
        // Second order surface elements list the corner nodes first
        private static int GetCornerCount(int nodeCount)
        {
            if (nodeCount == 6) return 3;
            if (nodeCount == 8 || nodeCount == 9) return 4;
            return nodeCount;
        }


        // HDF5 helpers
        private static List<string> GetChildNames(IH5Group group)
        {
            List<string> names = new List<string>();
            foreach (IH5Object child in group.Children()) names.Add(child.Name);
            return names;
        }
        private static int GetNameSuffix(string name)
        {
            int i = name.Length;
            while (i > 0 && char.IsDigit(name[i - 1])) i--;
            int value;
            if (i < name.Length && int.TryParse(name.Substring(i), NumberStyles.Integer,
                                                CultureInfo.InvariantCulture, out value)) return value;
            return -1;
        }
        private static int CompareBySuffix(string a, string b)
        {
            int sa = GetNameSuffix(a);
            int sb = GetNameSuffix(b);
            if (sa >= 0 && sb >= 0 && sa != sb) return sa.CompareTo(sb);
            return string.CompareOrdinal(a, b);
        }
        private static string GetUniqueSafeName(string rawName, string prefix, HashSet<string> usedNames)
        {
            string name = NamedClass.GetErrorFreeName(rawName, prefix, null);
            string uniqueName = name;
            int counter = 1;
            while (!usedNames.Add(uniqueName)) uniqueName = name + "-" + counter++;
            return uniqueName;
        }
        private static string GetStringAttribute(IH5Object h5Object, string name)
        {
            if (!h5Object.AttributeExists(name)) return null;
            //
            try { return h5Object.Attribute(name).Read<string>(); }
            catch { }
            try
            {
                string[] values = h5Object.Attribute(name).Read<string[]>();
                if (values != null && values.Length > 0) return values[0];
            }
            catch { }
            return null;
        }
        // The stored type has to drive the read; reading an integer as a double reinterprets the bytes
        private static double GetDoubleAttribute(IH5Object h5Object, string name, double defaultValue)
        {
            if (!h5Object.AttributeExists(name)) return defaultValue;
            //
            try
            {
                IH5Attribute attribute = h5Object.Attribute(name);
                double[] values = ToDoubleArray(attribute.Type, () => attribute.Read<double[]>(),
                                                () => attribute.Read<float[]>(), () => attribute.Read<sbyte[]>(),
                                                () => attribute.Read<short[]>(), () => attribute.Read<int[]>(),
                                                () => attribute.Read<long[]>(), () => attribute.Read<byte[]>(),
                                                () => attribute.Read<ushort[]>(), () => attribute.Read<uint[]>(),
                                                () => attribute.Read<ulong[]>());
                if (values != null && values.Length > 0) return values[0];
            }
            catch { }
            //
            return defaultValue;
        }
        private static double[] ReadDoubleArray(IH5Dataset dataset)
        {
            try
            {
                return ToDoubleArray(dataset.Type, () => dataset.Read<double[]>(), () => dataset.Read<float[]>(),
                                     () => dataset.Read<sbyte[]>(), () => dataset.Read<short[]>(),
                                     () => dataset.Read<int[]>(), () => dataset.Read<long[]>(),
                                     () => dataset.Read<byte[]>(), () => dataset.Read<ushort[]>(),
                                     () => dataset.Read<uint[]>(), () => dataset.Read<ulong[]>());
            }
            catch { }
            return null;
        }
        private static int[] ReadIntArray(IH5Dataset dataset)
        {
            double[] values = ReadDoubleArray(dataset);
            if (values == null) return null;
            //
            int[] result = new int[values.Length];
            for (int i = 0; i < values.Length; i++) result[i] = (int)values[i];
            return result;
        }
        private static double[] ToDoubleArray(IH5DataType type, Func<double[]> readDouble, Func<float[]> readFloat,
                                              Func<sbyte[]> readSByte, Func<short[]> readShort, Func<int[]> readInt,
                                              Func<long[]> readLong, Func<byte[]> readByte, Func<ushort[]> readUShort,
                                              Func<uint[]> readUInt, Func<ulong[]> readULong)
        {
            if (type.Class == H5DataTypeClass.FloatingPoint)
            {
                if (type.Size == 4) return readFloat().Select(x => (double)x).ToArray();
                return readDouble();
            }
            if (type.Class == H5DataTypeClass.FixedPoint)
            {
                bool isSigned = type.FixedPoint == null || type.FixedPoint.IsSigned;
                if (isSigned)
                {
                    switch (type.Size)
                    {
                        case 1: return readSByte().Select(x => (double)x).ToArray();
                        case 2: return readShort().Select(x => (double)x).ToArray();
                        case 8: return readLong().Select(x => (double)x).ToArray();
                        default: return readInt().Select(x => (double)x).ToArray();
                    }
                }
                switch (type.Size)
                {
                    case 1: return readByte().Select(x => (double)x).ToArray();
                    case 2: return readUShort().Select(x => (double)x).ToArray();
                    case 8: return readULong().Select(x => (double)x).ToArray();
                    default: return readUInt().Select(x => (double)x).ToArray();
                }
            }
            return null;
        }
        private static int[][] ReadJaggedIntArray(IH5Dataset dataset)
        {
            try { return dataset.Read<int[][]>(); } catch { }
            try
            {
                long[][] values = dataset.Read<long[][]>();
                int[][] result = new int[values.Length][];
                for (int i = 0; i < values.Length; i++) result[i] = values[i].Select(x => (int)x).ToArray();
                return result;
            }
            catch { }
            // A fixed size connectivity array stored as a 2D dataset
            try
            {
                ulong[] dimensions = dataset.Space.Dimensions;
                if (dimensions.Length == 2)
                {
                    int rowCount = (int)dimensions[0];
                    int columnCount = (int)dimensions[1];
                    int[] flat = dataset.Read<int[]>();
                    int[][] result = new int[rowCount][];
                    for (int i = 0; i < rowCount; i++)
                    {
                        result[i] = new int[columnCount];
                        Array.Copy(flat, i * columnCount, result[i], 0, columnCount);
                    }
                    return result;
                }
            }
            catch { }
            return null;
        }
    }
}
