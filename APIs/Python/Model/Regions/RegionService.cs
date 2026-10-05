// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using CaeModel;
using System.Collections.Generic;

namespace APIs.Python.Model
{
    // The behaviour shared by the region services. A region name is unique across all the mesh
    // entities, and every region is built from a selection in the model view.
    internal abstract class RegionService
    {
        // Variables
        private readonly ModelResolver _modelResolver;
        private readonly SelectionService _selectionService;
        private readonly IPythonModelGateway _modelGateway;


        // Properties
        protected ModelResolver ModelResolver { get { return _modelResolver; } }
        protected IPythonModelGateway ModelGateway { get { return _modelGateway; } }


        // Constructors
        protected RegionService(ModelResolver modelResolver, SelectionService selectionService,
                                IPythonModelGateway modelGateway)
        {
            _modelResolver = modelResolver;
            _selectionService = selectionService;
            _modelGateway = modelGateway;
        }


        // Methods
        protected FeMesh GetMeshOrNull()
        {
            return _modelResolver.GetModel().Mesh;
        }
        protected bool GetIsTwoDimensional()
        {
            ModelSpaceEnum modelSpace = _modelResolver.GetModel().Properties.ModelSpace;
            return modelSpace != ModelSpaceEnum.ThreeD && modelSpace != ModelSpaceEnum.Undefined;
        }
        protected SelectionCompilation CompileModelSelection(SelectionAPI selection, string target)
        {
            if (selection == null) throw new PythonAPIException("The argument 'selection' must not be None.");
            //
            return _selectionService.CompileForView(selection, "femodel", target);
        }
        // Only the characters a plain model item name takes, unless the region type accepts more
        protected virtual HashSet<char> GetAdditionalNameCharacters()
        {
            return null;
        }
        protected void ValidateNewName(string name, string allowedExistingName)
        {
            PythonNameComparer.ValidateFormat(name, "name", allowedExistingName, GetAdditionalNameCharacters());
            if (allowedExistingName != null && PythonNameComparer.AreSame(name, allowedExistingName)) return;
            // A region shares the name space with the other mesh entities
            foreach (string existingName in _modelResolver.GetAllMeshEntityNames())
            {
                if (PythonNameComparer.AreSame(name, existingName))
                    throw new PythonAPIException("The name '" + name + "' is already used by another model item.");
            }
        }
        protected void EnsureCommandsAvailable()
        {
            if (_modelGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
    }
}
