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
using System.Linq;

namespace APIs.Python.Model
{
    internal class SurfaceService : RegionService
    {
        // Constructors
        public SurfaceService(ModelResolver modelResolver, SelectionService selectionService,
                              IPythonModelGateway modelGateway)
            : base(modelResolver, selectionService, modelGateway)
        {
        }


        // Methods
        public string[] GetNames()
        {
            FeMesh mesh = GetMeshOrNull();
            if (mesh == null) return new string[0];
            //
            return mesh.Surfaces.Keys.ToArray();
        }
        public FeSurface GetSurface(string name)
        {
            FeMesh mesh = GetMeshOrNull();
            if (mesh == null || name == null) return null;
            //
            FeSurface surface;
            if (!mesh.Surfaces.TryGetValue(name, out surface)) return null;
            //
            return surface;
        }
        public FeSurface ResolveSurface(string name)
        {
            FeSurface surface = GetSurface(name);
            if (surface == null) throw new PythonAPIException("The surface '" + name + "' does not exist.");
            //
            return surface;
        }
        public void Add(string name, SelectionAPI selection, string surfaceType)
        {
            ValidateNewName(name, null);
            FeSurfaceType type = ToSurfaceType(surfaceType, FeSurfaceType.Element);
            FeSurface surface = BuildFromSelection(name, selection);
            surface.Type = type;
            //
            EnsureCommandsAvailable();
            ModelGateway.AddSurface(surface);
        }
        public void AddFromNodeSet(string name, string nodeSetName, string surfaceType)
        {
            ValidateNewName(name, null);
            FeSurfaceType type = ToSurfaceType(surfaceType, FeSurfaceType.Element);
            FeSurface surface = BuildFromNodeSet(name, nodeSetName);
            surface.Type = type;
            //
            EnsureCommandsAvailable();
            ModelGateway.AddSurface(surface);
        }
        public void Replace(string oldName, string name, SelectionAPI selection, string surfaceType)
        {
            FeSurface existing = ResolveSurface(oldName);
            RefuseInternal(existing, "surface", "replaced");
            string newName = name == null ? oldName : name;
            ValidateNewName(newName, oldName);
            FeSurfaceType type = ToSurfaceType(surfaceType, existing.Type);
            FeSurface surface;
            if (selection == null)
            {
                if (existing.CreatedFrom != FeSurfaceCreatedFrom.Selection || existing.CreationData == null)
                    throw new PythonAPIException("The argument 'selection' must be given, because the surface " +
                                                 "is not created from a selection.");
                surface = new FeSurface(existing);
                surface.Name = newName;
            }
            else surface = BuildFromSelection(newName, selection);
            surface.Type = type;
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceSurface(oldName, surface);
        }
        public void ReplaceFromNodeSet(string oldName, string name, string nodeSetName, string surfaceType)
        {
            FeSurface existing = ResolveSurface(oldName);
            RefuseInternal(existing, "surface", "replaced");
            string newName = name == null ? oldName : name;
            ValidateNewName(newName, oldName);
            FeSurfaceType type = ToSurfaceType(surfaceType, existing.Type);
            // A node set surface is fully described by its node set, so it is built rather than cloned;
            // a node set that is not given is the one the surface already uses
            string keptNodeSetName = nodeSetName != null ? nodeSetName : GetKeptNodeSetName(existing);
            FeSurface surface = BuildFromNodeSet(newName, keptNodeSetName);
            surface.Type = type;
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceSurface(oldName, surface);
        }
        public void Duplicate(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.DuplicateSurfaces(names);
        }
        public void Remove(string[] names)
        {
            foreach (string name in names) RefuseInternal(GetSurface(name), "surface", "removed");
            //
            EnsureCommandsAvailable();
            ModelGateway.RemoveSurfaces(names);
        }
        public void Rename(NamedClass item, string newName)
        {
            RefuseInternal(item, "surface", "renamed");
            ValidateNewName(newName, item.Name);
            //
            EnsureCommandsAvailable();
            ModelGateway.RenameModelItem(item, newName, null);
        }
        //
        private FeSurface BuildFromSelection(string name, SelectionAPI selection)
        {
            // The controller recomputes the face IDs from the creation data when the surface is added
            SelectionCompilation compilation = CompileModelSelection(selection, "surfaces");
            FeSurface surface = new FeSurface(name, compilation.Ids, compilation.Selection);
            surface.CreatedFrom = FeSurfaceCreatedFrom.Selection;
            //
            return surface;
        }
        private static FeSurfaceType ToSurfaceType(string surfaceType, FeSurfaceType keptType)
        {
            if (surfaceType == null) return keptType;
            //
            return PythonValueConverter.ToEnum<FeSurfaceType>(surfaceType, "surface_type");
        }
        private static string GetKeptNodeSetName(FeSurface surface)
        {
            if (surface.CreatedFrom != FeSurfaceCreatedFrom.NodeSet || surface.CreatedFromNodeSetName == null)
                throw new PythonAPIException("The argument 'node_set_name' must be given, because the surface " +
                                             "is not created from a node set.");
            //
            return surface.CreatedFromNodeSetName;
        }
        private FeSurface BuildFromNodeSet(string name, string nodeSetName)
        {
            if (nodeSetName == null || nodeSetName.Trim().Length == 0)
                throw new PythonAPIException("The argument 'node_set_name' must not be empty.");
            //
            FeMesh mesh = GetMeshOrNull();
            FeNodeSet nodeSet = null;
            if (mesh == null || !mesh.NodeSets.TryGetValue(nodeSetName, out nodeSet))
                throw new PythonAPIException("The node set '" + nodeSetName + "' does not exist.");
            // The controller follows a renamed node set by comparing names exactly, so the surface stores the
            // model's own spelling rather than the caller's
            FeSurface surface = new FeSurface(name, nodeSet.Name);
            surface.CreatedFrom = FeSurfaceCreatedFrom.NodeSet;
            //
            return surface;
        }
    }
}
