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
            SurfaceInteraction surfaceInteraction = new SurfaceInteraction(name);
            ApplyProperties(surfaceInteraction, properties);
            //
            return surfaceInteraction;
        }
        public void Add(SurfaceInteraction surfaceInteraction)
        {
            EnsureCommandsAvailable();
            _modelGateway.AddSurfaceInteraction(surfaceInteraction);
        }
        public SurfaceInteraction Replace(string oldName, string name, object properties)
        {
            SurfaceInteraction surfaceInteraction = ResolveSurfaceInteraction(oldName).DeepClone();
            if (name != null)
            {
                ValidateName(name, oldName);
                surfaceInteraction.Name = name;
            }
            if (properties != null) ApplyProperties(surfaceInteraction, properties);
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceSurfaceInteraction(oldName, surfaceInteraction);
            //
            return surfaceInteraction;
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
            PythonNameComparer.ValidateFormat(name, "name", allowedExistingName);
            if (allowedExistingName != null && PythonNameComparer.AreSame(name, allowedExistingName)) return;
            //
            foreach (string existingName in GetNames())
            {
                if (PythonNameComparer.AreSame(name, existingName))
                    throw new PythonAPIException("The surface interaction '" + name + "' already exists.");
            }
        }
        //
        private static void ApplyProperties(SurfaceInteraction surfaceInteraction, object properties)
        {
            List<SurfaceInteractionPropertyAPI> converted =
                PythonObjectListConverter.ToList<SurfaceInteractionPropertyAPI>(properties, "properties",
                                                                                 "a surface interaction property " +
                                                                                 "created by pmx.create or read " +
                                                                                 "from a surface interaction, or " +
                                                                                 "a list of them");
            if (converted.Count == 0)
                throw new PythonAPIException("The argument 'properties' must contain at least one surface " +
                                             "interaction property.");
            //
            // The dialog adds a property type only once
            surfaceInteraction.Properties.Clear();
            HashSet<System.Type> propertyTypes = new HashSet<System.Type>();
            foreach (SurfaceInteractionPropertyAPI property in converted)
            {
                SurfaceInteractionProperty propertyCopy = property.GetPropertyCopy();
                if (!propertyTypes.Add(propertyCopy.GetType()))
                    throw new PythonAPIException("The argument 'properties' contains the " +
                                                 SurfaceInteractionPropertyAPI.GetPropertyType(propertyCopy) +
                                                 " property more than once; a surface interaction holds one " +
                                                 "property of each type.");
                surfaceInteraction.AddProperty(propertyCopy);
            }
        }
        private void EnsureCommandsAvailable()
        {
            if (_modelGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
    }
}
