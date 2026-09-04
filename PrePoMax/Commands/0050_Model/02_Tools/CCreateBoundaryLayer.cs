// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using System;
using System.Runtime.Serialization;

namespace PrePoMax.Commands
{
    [Serializable]
    class CCreateBoundaryLayer : PreprocessCommand, ISerializable
    {
        // Variables
        private GeometrySelection _geometrySelection;   //ISerializable
        private double _thickness;                      //ISerializable


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
        public override string GetCommandString()
        {
            return base.GetCommandString() + "Thickness: " + _thickness.ToString();
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_geometrySelection", _geometrySelection, typeof(GeometrySelection));
            info.AddValue("_thickness", _thickness, typeof(double));
        }
    }
}
