// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Collections.Generic;
using System.Globalization;
using CaeModel;

namespace PrePoMax.Python.Journal
{
    // A material orientation takes its region the way a section does: from the selection it was created with whenever
    // it has one, because the controller turned that selection into an internal element set at add time.
    public static class PythonMaterialOrientationEmitter
    {
        // Variables
        private const string _collection = "pmx.model.material_orientations";


        // Methods
        public static void Add(PythonJournalWriter writer, MaterialOrientation orientation)
        {
            Emit(writer, null, orientation);
        }
        public static void Replace(PythonJournalWriter writer, string oldName, MaterialOrientation orientation)
        {
            Emit(writer, oldName, orientation);
        }
        private static void Emit(PythonJournalWriter writer, string oldName, MaterialOrientation orientation)
        {
            bool replace = oldName != null;
            string region = PythonRegionEmitter.EmitKept(writer, orientation.RegionName, orientation.RegionType,
                                                         orientation.CreationData, replace);
            List<string> arguments = new List<string>();
            if (replace) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(orientation.Name));
            PythonJournalWriter.AddPositional(arguments, "region", region);
            // A replace keeps the coordinate system it is not given, so the global one is written out there
            string coordinateSystemName = writer.KeptReference("pmx.model.coordinate_systems",
                                                               orientation.CoordinateSystemName, replace);
            if (coordinateSystemName != null &&
                (replace || coordinateSystemName != CaeMesh.CoordinateSystem.DefaultCoordinateSystemName))
                arguments.Add("coordinate_system_name=" + PythonValueFormatter.Text(coordinateSystemName));
            if (replace || orientation.ReferenceAxis != CaeMesh.MaterialOrientationAxisEnum.X)
                arguments.Add("reference_axis=" + PythonValueFormatter.Text(orientation.ReferenceAxis.ToString()));
            arguments.Add("rotation_angle_deg=" + PythonValueFormatter.Equation(orientation.RotationAngle));
            if (replace || orientation.SymbolDensity != MaterialOrientation.DefaultSymbolDensity)
                arguments.Add("symbol_density=" + orientation.SymbolDensity.ToString(CultureInfo.InvariantCulture));
            //
            writer.Statement(writer.Call(_collection, replace ? "replace" : "add", arguments.ToArray()));
            PythonModelItemEmitter.EmitState(writer, _collection, orientation);
            writer.CountEmittedCommand();
        }
    }
}
