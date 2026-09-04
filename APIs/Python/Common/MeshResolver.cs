// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using CaeResults;

namespace APIs.Python
{
    internal class MeshResolver : IMeshResolver
    {
        // Variables
        private readonly ResultResolver _resultResolver;
        private readonly IPythonSelectionGateway _selectionGateway;


        // Constructors
        public MeshResolver(ResultResolver resultResolver, IPythonSelectionGateway selectionGateway)
        {
            _resultResolver = resultResolver;
            _selectionGateway = selectionGateway;
        }


        // Methods
        public FeMesh GetMesh(string view, string resultName)
        {
            if (view == "results") return GetResultMesh(resultName);
            //
            if (_selectionGateway == null)
                throw new PythonAPIException("The '" + view + "' view is not available in this Python API context.");
            //
            return _selectionGateway.GetMesh(view);
        }
        private FeMesh GetResultMesh(string resultName)
        {
            string name = resultName != null ? resultName : _resultResolver.GetCurrentResultName();
            if (name == null) throw new PythonAPIException("No current result is selected.");
            //
            FeResults result = _resultResolver.ResolveResult(name);
            if (resultName == null && !_resultResolver.IsCurrentResult(name))
                throw new PythonAPIException("No current result is selected.");
            //
            return result.Mesh;
        }
    }
}
