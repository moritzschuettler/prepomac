// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using System;
using System.Runtime.Serialization;

using PrePoMax.Python.Journal;
using CaeGlobals;

namespace PrePoMax.Commands
{
    [PmxType(10078)]
    class CCreateBoundaryLayer : PreprocessCommand
    {
        // Variables
        [PmxKey(0)] private GeometrySelection _geometrySelection;   //ISerializable
        [PmxKey(1)] private double _thickness;                      //ISerializable


        // Constructor
        public CCreateBoundaryLayer(GeometrySelection geometrySelection, double thickness)
            : base("Create boundary layer")
        {
            _geometrySelection = geometrySelection;
            _thickness = thickness;
        }
        public CCreateBoundaryLayer(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            int[] geometryIds = null;
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_geometryIds":            // Compatibility for version v2.6.2 and older
                        geometryIds = (int[])entry.Value; break;
                    case "_geometrySelection":
                        _geometrySelection = (GeometrySelection)entry.Value; break;
                    case "_thickness":
                        _thickness = (double)entry.Value; break;
                }
            }
            // An older command stored the resolved ids only; it keeps replaying from them
            if (_geometrySelection == null)
                _geometrySelection = new GeometrySelection("Selection", geometryIds, null);
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.CreateBoundaryLayer(_geometrySelection, _thickness);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            string selection = PythonSelectionEmitter.Emit(writer, _geometrySelection.CreationData);
            writer.Statement(writer.Call("pmx.model.mesh", "generate_boundary_layer", selection,
                                         PythonValueFormatter.Number(_thickness)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + "Thickness: " + _thickness.ToString();
        }
    }
}
