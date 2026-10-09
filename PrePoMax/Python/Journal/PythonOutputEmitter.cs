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
    // The field outputs, history outputs and defined fields of a step. A field output applies to the
    // whole model and so takes no region, while a history output and a defined field do.
    public static class PythonOutputEmitter
    {
        // Methods                                                                                                                  
        public static void FieldOutput(PythonJournalWriter writer, string stepName, string oldName,
                                       FieldOutput output)
        {
            string collection = PythonStepItemEmitter.GetCollection(stepName, "field_outputs");
            List<string> arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(output.Name));
            //
            string verb = oldName == null ? "add" : "replace";
            string method;
            if (output is NodalFieldOutput nodal)
            {
                method = verb + "_nodal";
                arguments.Add(PythonValueFormatter.Flags(nodal.Variables));
            }
            else if (output is ElementFieldOutput element)
            {
                method = verb + "_element";
                arguments.Add(PythonValueFormatter.Flags(element.Variables));
                arguments.Add("output=" + PythonValueFormatter.Text(element.Output.ToString()));
            }
            else if (output is ContactFieldOutput contact)
            {
                method = verb + "_contact";
                arguments.Add(PythonValueFormatter.Flags(contact.Variables));
            }
            else throw new NotSupportedException("the field output type " + output.GetType().Name +
                                                 " has no API form");
            //
            arguments.Add("last_iterations=" + PythonValueFormatter.Format(output.LastIterations));
            arguments.Add("contact_elements=" + PythonValueFormatter.Format(output.ContactElements));
            arguments.Add("global_output=" + PythonValueFormatter.Format(output.Global));
            //
            writer.Statement(writer.Call(collection, method, arguments.ToArray()));
            PythonModelItemEmitter.EmitState(writer, collection, output);
            writer.CountEmittedCommand();
        }
        public static void HistoryOutput(PythonJournalWriter writer, string stepName, string oldName,
                                         HistoryOutput output)
        {
            string collection = PythonStepItemEmitter.GetCollection(stepName, "history_outputs");
            List<string> arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(output.Name));
            //
            string verb = oldName == null ? "add" : "replace";
            string method;
            if (output is ContactHistoryOutput contact)
            {
                // A contact history output names a contact pair rather than a region of the mesh
                method = verb + "_contact";
                arguments.Add(PythonValueFormatter.Text(output.RegionName));
                arguments.Add(PythonValueFormatter.Flags(contact.Variables));
            }
            else
            {
                string region = PythonRegionEmitter.Emit(writer, output.RegionName, output.RegionType,
                                                         output.CreationData);
                arguments.Add(region);
                if (output is NodalHistoryOutput nodal)
                {
                    method = verb + "_nodal";
                    arguments.Add(PythonValueFormatter.Flags(nodal.Variables));
                }
                else if (output is ElementHistoryOutput element)
                {
                    method = verb + "_element";
                    arguments.Add(PythonValueFormatter.Flags(element.Variables));
                }
                else if (output is SectionHistoryOutput section)
                {
                    method = verb + "_section";
                    arguments.Add(PythonValueFormatter.Flags(section.Variables));
                }
                else throw new NotSupportedException("the history output type " + output.GetType().Name +
                                                     " has no API form");
            }
            arguments.Add("totals_type=" + PythonValueFormatter.Text(output.TotalsType.ToString()));
            arguments.Add("global_output=" + PythonValueFormatter.Format(output.Global));
            //
            writer.Statement(writer.Call(collection, method, arguments.ToArray()));
            PythonModelItemEmitter.EmitState(writer, collection, output);
            writer.CountEmittedCommand();
        }
        public static void DefinedField(PythonJournalWriter writer, string stepName, string oldName,
                                        DefinedField field)
        {
            string collection = PythonStepItemEmitter.GetCollection(stepName, "defined_fields");
            DefinedTemperature temperature = field as DefinedTemperature;
            if (temperature == null)
                throw new NotSupportedException("the defined field type " + field.GetType().Name +
                                                " has no API form");
            //
            List<string> arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(field.Name));
            //
            string verb = oldName == null ? "add" : "replace";
            string method;
            if (temperature.Type == DefinedTemperatureTypeEnum.FromFile)
            {
                method = verb + "_temperature_from_file";
                arguments.Add(writer.Path(temperature.FileName));
                arguments.Add("step_id=" + PythonValueFormatter.Format(temperature.StepNumber));
            }
            else
            {
                method = verb + "_temperature";
                arguments.Add(PythonRegionEmitter.Emit(writer, field.RegionName, field.RegionType,
                                                       field.CreationData));
                arguments.Add(PythonValueFormatter.Equation(temperature.Temperature));
                if (!string.IsNullOrEmpty(temperature.DistributionName))
                    arguments.Add("distribution_name=" +
                                  PythonValueFormatter.Text(temperature.DistributionName));
                // A temperature read from a file is written without an amplitude, so only this form takes one
                PythonStepItemEmitter.AddAmplitude(arguments, field.AmplitudeName);
            }
            DefinedTemperature defaultField = new DefinedTemperature("Default", null,
                                                                     CaeGlobals.RegionTypeEnum.Selection, 0);
            PythonStepItemEmitter.AddColor(arguments, field.Color, defaultField.Color, oldName != null);
            //
            writer.Statement(writer.Call(collection, method, arguments.ToArray()));
            PythonModelItemEmitter.EmitState(writer, collection, field);
            writer.CountEmittedCommand();
        }
    }
}
