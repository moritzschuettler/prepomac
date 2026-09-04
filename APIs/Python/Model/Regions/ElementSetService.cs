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
    internal class ElementSetService : RegionService
    {
        // Constructors
        public ElementSetService(ModelResolver modelResolver, SelectionService selectionService,
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
            return mesh.ElementSets.Keys.ToArray();
        }
        public FeElementSet GetElementSet(string name)
        {
            FeMesh mesh = GetMeshOrNull();
            if (mesh == null || name == null) return null;
            //
            FeElementSet elementSet;
            if (!mesh.ElementSets.TryGetValue(name, out elementSet)) return null;
            //
            return elementSet;
        }
        public FeElementSet ResolveElementSet(string name)
        {
            FeElementSet elementSet = GetElementSet(name);
            if (elementSet == null)
                throw new PythonAPIException("The element set '" + name + "' does not exist.");
            //
            return elementSet;
        }
        public void Add(string name, SelectionAPI selection)
        {
            ValidateNewName(name, null);
            FeElementSet elementSet = Build(name, selection);
            //
            EnsureCommandsAvailable();
            ModelGateway.AddElementSet(elementSet);
        }
        public void Replace(string oldName, string name, SelectionAPI selection)
        {
            ResolveElementSet(oldName);
            ValidateNewName(name, oldName);
            FeElementSet elementSet = Build(name, selection);
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceElementSet(oldName, elementSet);
        }
        public void Duplicate(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.DuplicateElementSets(names);
        }
        public void Remove(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.RemoveElementSets(names);
        }
        public void Rename(NamedClass item, string newName)
        {
            ValidateNewName(newName, item.Name);
            //
            EnsureCommandsAvailable();
            ModelGateway.RenameModelItem(item, newName, null);
        }
        public void ConvertToMeshParts(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.ConvertElementSetsToMeshParts(names);
        }
        //
        private FeElementSet Build(string name, SelectionAPI selection)
        {
            // The controller recomputes the labels from the creation data when the set is added
            SelectionCompilation compilation = CompileModelSelection(selection, "elements");
            FeElementSet elementSet = new FeElementSet(name, compilation.Ids);
            elementSet.CreationData = compilation.Selection;
            elementSet.CreationIds = compilation.Ids;
            //
            return elementSet;
        }
    }
}
