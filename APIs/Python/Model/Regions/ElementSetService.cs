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
using System.Collections.Generic;
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
        // The characters FeElementSet adds to the ones every name may contain
        protected override HashSet<char> GetAdditionalNameCharacters()
        {
            return new HashSet<char>() { '@' };
        }
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
        // An element set made by picking parts keeps the part IDs, which stand for every element of those parts
        public int[] GetElementIds(FeElementSet elementSet)
        {
            if (elementSet.Labels == null) return new int[0];
            if (!elementSet.CreatedFromParts) return elementSet.Labels;
            //
            FeMesh mesh = GetMeshOrNull();
            HashSet<int> elementIds = new HashSet<int>();
            foreach (int partId in elementSet.Labels)
            {
                BasePart part = mesh == null ? null : mesh.GetPartFromId(partId);
                if (part != null && part.Labels != null) elementIds.UnionWith(part.Labels);
            }
            //
            return elementIds.OrderBy(id => id).ToArray();
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
            FeElementSet existing = ResolveElementSet(oldName);
            RefuseInternal(existing, "element set", "replaced");
            string newName = name == null ? existing.Name : name;
            ValidateNewName(newName, existing.Name);
            // A value that is not given is kept, so a rename alone keeps the selection the set has
            FeElementSet elementSet;
            if (selection == null)
            {
                if (existing.CreationData == null)
                    throw new PythonAPIException("The argument 'selection' must be given, because the element " +
                                                 "set '" + existing.Name + "' was not created from a selection.");
                elementSet = existing.DeepClone();
                elementSet.Name = newName;
            }
            else elementSet = Build(newName, selection);
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceElementSet(existing.Name, elementSet);
        }
        public void Duplicate(string[] names)
        {
            foreach (string name in names) RefuseInternal(GetElementSet(name), "element set", "duplicated");
            //
            EnsureCommandsAvailable();
            ModelGateway.DuplicateElementSets(names);
        }
        public void Remove(string[] names)
        {
            foreach (string name in names) RefuseInternal(GetElementSet(name), "element set", "removed");
            //
            EnsureCommandsAvailable();
            ModelGateway.RemoveElementSets(names);
        }
        public void Rename(NamedClass item, string newName)
        {
            RefuseInternal(item, "element set", "renamed");
            ValidateNewName(newName, item.Name);
            //
            EnsureCommandsAvailable();
            ModelGateway.RenameModelItem(item, newName, null);
        }
        public void ConvertToMeshParts(string[] names)
        {
            foreach (string name in names)
                RefuseInternal(GetElementSet(name), "element set", "converted to mesh parts");
            //
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
