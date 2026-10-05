// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeModel;

namespace APIs.Python.Model
{
    // The behaviour shared by the amplitude, distribution and initial condition services
    internal abstract class FieldItemService
    {
        // Variables
        private readonly ModelResolver _modelResolver;
        private readonly SelectionService _selectionService;
        private readonly IPythonModelGateway _modelGateway;


        // Properties
        protected ModelResolver ModelResolver { get { return _modelResolver; } }
        protected SelectionService SelectionService { get { return _selectionService; } }
        protected IPythonModelGateway ModelGateway { get { return _modelGateway; } }
        protected abstract string ItemTypeName { get; }


        // Constructors
        protected FieldItemService(ModelResolver modelResolver, SelectionService selectionService,
                                   IPythonModelGateway modelGateway)
        {
            _modelResolver = modelResolver;
            _selectionService = selectionService;
            _modelGateway = modelGateway;
        }


        // Methods
        public bool GetIsTwoDimensional()
        {
            ModelSpaceEnum modelSpace = _modelResolver.GetModel().Properties.ModelSpace;
            return modelSpace != ModelSpaceEnum.ThreeD && modelSpace != ModelSpaceEnum.Undefined;
        }
        public void ValidateNewName(string name, string allowedExistingName)
        {
            PythonNameComparer.ValidateFormat(name, "name", allowedExistingName);
            if (allowedExistingName != null && PythonNameComparer.AreSame(name, allowedExistingName)) return;
            //
            foreach (string existingName in GetItemNames())
            {
                if (PythonNameComparer.AreSame(name, existingName))
                    throw new PythonAPIException("The " + ItemTypeName + " '" + name + "' already exists.");
            }
        }
        public SelectionCompilation CompileModelSelection(SelectionAPI selection, string target)
        {
            if (selection == null) throw new PythonAPIException("The argument 'selection' must not be None.");
            //
            return _selectionService.CompileForView(selection, "femodel", target);
        }
        public void EnsureCommandsAvailable()
        {
            if (_modelGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
        //
        protected abstract string[] GetItemNames();
    }
}
