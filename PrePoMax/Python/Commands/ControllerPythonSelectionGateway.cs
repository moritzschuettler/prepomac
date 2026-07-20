// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using APIs.Python;
using CaeGlobals;
using CaeMesh;
using System;

namespace PrePoMax.Python
{
    internal class ControllerPythonSelectionGateway : IPythonSelectionGateway
    {
        // Variables
        private readonly Controller _controller;


        // Constructors
        public ControllerPythonSelectionGateway(Controller controller)
        {
            _controller = controller;
        }


        // Methods
        public int GetViewId(string view)
        {
            if (view == "geometry") return (int)ViewGeometryModelResults.Geometry;
            if (view == "femodel") return (int)ViewGeometryModelResults.Model;
            if (view == "results") return (int)ViewGeometryModelResults.Results;
            throw new ArgumentException("Unsupported selection view '" + view + "'.", nameof(view));
        }
        public FeMesh GetMesh(string view)
        {
            if (_controller.Model == null) return null;
            if (view == "geometry") return _controller.Model.Geometry;
            if (view == "femodel") return _controller.Model.Mesh;
            throw new ArgumentException("Unsupported selection view '" + view + "'.", nameof(view));
        }
        public void Highlight(Selection selection)
        {
            _controller.HighlightSelection(selection);
        }
        public void ClearHighlight()
        {
            _controller.ClearSelectionHistoryAndCallSelectionChanged();
        }
    }
}