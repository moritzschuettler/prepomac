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
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python.Model
{
    internal class SurfaceInteractionService
    {
        // Variables
        private readonly ModelResolver _modelResolver;
        private readonly IPythonModelGateway _modelGateway;


        // Constructors
        public SurfaceInteractionService(ModelResolver modelResolver, IPythonModelGateway modelGateway)
        {
            _modelResolver = modelResolver;
            _modelGateway = modelGateway;
        }


        // Methods
        public string[] GetNames()
        {
            return _modelResolver.GetModel().SurfaceInteractions.Keys.ToArray();
        }
        public SurfaceInteraction GetSurfaceInteraction(string name)
        {
            if (name == null) return null;
            //
            SurfaceInteraction surfaceInteraction;
            if (!_modelResolver.GetModel().SurfaceInteractions.TryGetValue(name, out surfaceInteraction))
                return null;
            //
            return surfaceInteraction;
        }
        public SurfaceInteraction ResolveSurfaceInteraction(string name)
        {
            SurfaceInteraction surfaceInteraction = GetSurfaceInteraction(name);
            if (surfaceInteraction == null)
                throw new PythonAPIException("The surface interaction '" + name + "' does not exist.");
            //
            return surfaceInteraction;
        }
        public SurfaceInteraction Build(string name, object properties)
        {
            List<SurfaceInteractionProperty> converted =
                PythonObjectListConverter.ToList<SurfaceInteractionProperty>(properties, "properties",
                                                                              "a surface interaction property " +
                                                                              "created by pmx.create, or a list " +
                                                                              "of them");
            if (converted.Count == 0)
                throw new PythonAPIException("The argument 'properties' must contain at least one surface " +
                                             "interaction property.");
            //
            SurfaceInteraction surfaceInteraction = new SurfaceInteraction(name);
            foreach (SurfaceInteractionProperty property in converted) surfaceInteraction.AddProperty(property);
            //
            return surfaceInteraction;
        }
        public void Add(SurfaceInteraction surfaceInteraction)
        {
            EnsureCommandsAvailable();
            _modelGateway.AddSurfaceInteraction(surfaceInteraction);
        }
        public void Replace(string oldName, SurfaceInteraction surfaceInteraction)
        {
            ResolveSurfaceInteraction(oldName);
            ValidateName(surfaceInteraction.Name, oldName);
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceSurfaceInteraction(oldName, surfaceInteraction);
        }
        public void Duplicate(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.DuplicateSurfaceInteractions(names);
        }
        public void Remove(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.RemoveSurfaceInteractions(names);
        }
        public void Rename(NamedClass item, string newName)
        {
            ValidateName(newName, item.Name);
            //
            EnsureCommandsAvailable();
            _modelGateway.RenameModelItem(item, newName, null);
        }
        public void ValidateName(string name, string allowedExistingName)
        {
            if (name == null || name.Trim().Length == 0)
                throw new PythonAPIException("The argument 'name' must not be empty.");
            if (allowedExistingName != null && PythonNameComparer.AreSame(name, allowedExistingName)) return;
            //
            foreach (string existingName in GetNames())
            {
                if (PythonNameComparer.AreSame(name, existingName))
                    throw new PythonAPIException("The surface interaction '" + name + "' already exists.");
            }
        }
        //
        private void EnsureCommandsAvailable()
        {
            if (_modelGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
    }
}
