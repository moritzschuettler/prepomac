// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using CaeModel;

namespace PrePoMax.Python.Journal
{
    // Every section type has its own API member, and each takes the region the section applies to. The
    // region comes from the selection the section was created with whenever it has one, because the
    // controller turned that selection into an internal element or node set at add time.
    public static class PythonSectionEmitter
    {
        // Methods                                                                                                                  
        public static void Add(PythonJournalWriter writer, Section section)
        {
            Emit(writer, "add", null, section);
        }
        public static void Replace(PythonJournalWriter writer, string oldName, Section section)
        {
            Emit(writer, "replace", oldName, section);
        }
        private static void Emit(PythonJournalWriter writer, string verb, string oldName, Section section)
        {
            bool replace = oldName != null;
            string region = PythonRegionEmitter.EmitKept(writer, section.RegionName, section.RegionType,
                                                         section.CreationData, replace);
            string materialName = writer.KeptReference("pmx.model.materials", section.MaterialName, replace);
            string material = materialName == null ? null : PythonValueFormatter.Text(materialName);
            List<string> arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(section.Name));
            //
            string method;
            if (section is SolidSection solidSection)
            {
                method = verb + "_solid";
                PythonJournalWriter.AddPositional(arguments, "material_name", material);
                PythonJournalWriter.AddPositional(arguments, "region", region);
                // Only a 2D solid section writes its thickness, and add_solid refuses one in a 3D model
                if (solidSection.TwoD)
                    arguments.Add("thickness=" + PythonValueFormatter.Equation(solidSection.Thickness));
            }
            else if (section is ShellSection shellSection)
            {
                method = verb + "_shell";
                PythonJournalWriter.AddPositional(arguments, "material_name", material);
                PythonJournalWriter.AddPositional(arguments, "region", region);
                arguments.Add("thickness=" + PythonValueFormatter.Equation(shellSection.Thickness));
                arguments.Add("offset=" + PythonValueFormatter.Equation(shellSection.Offset));
            }
            else if (section is MembraneSection membraneSection)
            {
                method = verb + "_membrane";
                PythonJournalWriter.AddPositional(arguments, "material_name", material);
                PythonJournalWriter.AddPositional(arguments, "region", region);
                arguments.Add("thickness=" + PythonValueFormatter.Equation(membraneSection.Thickness));
                arguments.Add("offset=" + PythonValueFormatter.Equation(membraneSection.Offset));
            }
            else if (section is PointMassSection pointMassSection)
            {
                method = verb + "_point_mass";
                PythonJournalWriter.AddPositional(arguments, "region", region);
                arguments.Add("mass=" + PythonValueFormatter.Equation(pointMassSection.Mass));
            }
            else if (section is DistributedMassSection distributedMassSection)
            {
                method = verb + "_distributed_mass";
                PythonJournalWriter.AddPositional(arguments, "region", region);
                arguments.Add("mass=" + PythonValueFormatter.Equation(distributedMassSection.Mass));
            }
            else throw new NotSupportedException("the section type " + section.GetType().Name +
                                                 " has no API form");
            //
            writer.Statement(writer.Call("pmx.model.sections", method, arguments.ToArray()));
            PythonModelItemEmitter.EmitState(writer, "pmx.model.sections", section);
            writer.CountEmittedCommand();
        }
    }
}
