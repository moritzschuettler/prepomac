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
            string region = PythonRegionEmitter.Emit(writer, section.RegionName, section.RegionType,
                                                     section.CreationData);
            List<string> arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(section.Name));
            //
            string method;
            if (section is SolidSection solidSection)
            {
                method = verb + "_solid";
                arguments.Add(PythonValueFormatter.Text(section.MaterialName));
                arguments.Add(region);
                arguments.Add("thickness=" + PythonValueFormatter.Equation(solidSection.Thickness));
            }
            else if (section is ShellSection shellSection)
            {
                method = verb + "_shell";
                arguments.Add(PythonValueFormatter.Text(section.MaterialName));
                arguments.Add(region);
                arguments.Add("thickness=" + PythonValueFormatter.Equation(shellSection.Thickness));
                arguments.Add("offset=" + PythonValueFormatter.Equation(shellSection.Offset));
            }
            else if (section is MembraneSection membraneSection)
            {
                method = verb + "_membrane";
                arguments.Add(PythonValueFormatter.Text(section.MaterialName));
                arguments.Add(region);
                arguments.Add("thickness=" + PythonValueFormatter.Equation(membraneSection.Thickness));
                arguments.Add("offset=" + PythonValueFormatter.Equation(membraneSection.Offset));
            }
            else if (section is PointMassSection pointMassSection)
            {
                method = verb + "_point_mass";
                arguments.Add(region);
                arguments.Add("mass=" + PythonValueFormatter.Equation(pointMassSection.Mass));
            }
            else if (section is DistributedMassSection distributedMassSection)
            {
                method = verb + "_distributed_mass";
                arguments.Add(region);
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
