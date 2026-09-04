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

namespace APIs.Python.Model
{
    // The model side counterpart of ResultResolver. It never caches the model, because New and Open
    // replace the instance held by the application.
    internal class ModelResolver
    {
        // Variables
        private readonly IPythonModelGateway _modelGateway;


        // Properties
        public IPythonModelGateway ModelGateway { get { return _modelGateway; } }


        // Constructors
        public ModelResolver(IPythonModelGateway modelGateway)
        {
            _modelGateway = modelGateway;
        }


        // Methods
        public void EnsureModelAvailable()
        {
            if (_modelGateway == null)
                throw new PythonAPIException("The model is not available in this Python API context.");
        }
        public FeModel GetModel()
        {
            EnsureModelAvailable();
            //
            FeModel model = _modelGateway.GetModel();
            if (model == null) throw new PythonAPIException("No model is open.");
            //
            return model;
        }
        public FeMesh GetMesh()
        {
            FeMesh mesh = GetModel().Mesh;
            if (mesh == null) throw new PythonAPIException("The model does not contain a mesh.");
            //
            return mesh;
        }
        public FeMesh GetGeometry()
        {
            FeMesh geometry = GetModel().Geometry;
            if (geometry == null) throw new PythonAPIException("The model does not contain a geometry.");
            //
            return geometry;
        }
        public string[] GetAllMeshEntityNames()
        {
            EnsureModelAvailable();
            //
            string[] names = _modelGateway.GetAllMeshEntityNames();
            if (names == null) return new string[0];
            //
            return names;
        }
    }
}
