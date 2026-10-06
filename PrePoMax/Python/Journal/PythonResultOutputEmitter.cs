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
using CaeGlobals;
using CaeResults;

namespace PrePoMax.Python.Journal
{
    // The derived field and history outputs of a result. Both kinds carry up to two filters, which are
    // detached objects the pmx.results factories make, so they are emitted the way pmx.create parts are.
    public static class PythonResultOutputEmitter
    {
        // Variables                                                                                                                
        public const string FieldOutputs = "pmx.results.field_outputs";
        public const string HistoryOutputs = "pmx.results.history_outputs";


        // Methods                                                                                                                  
        public static void FieldOutput(PythonJournalWriter writer, string oldName, ResultFieldOutput output)
        {
            List<string> arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(output.Name));
            //
            string verb = oldName == null ? "add" : "replace";
            string method;
            if (output is ResultFieldOutputLimit limit)
            {
                method = verb + "_limit";
                arguments.Add(PythonValueFormatter.Text(limit.FieldName));
                arguments.Add(PythonValueFormatter.Text(limit.ComponentName));
                arguments.Add(PythonValueFormatter.Text(limit.LimitPlotBasedOn.ToString()));
                // The per item limits are part of the definition, so a journal that leaves them out
                // would write a limit plot that shows something else
                arguments.Add(ItemLimits(limit.ItemNameLimit));
            }
            else if (output is ResultFieldOutputEnvelope envelope)
            {
                method = verb + "_envelope";
                arguments.Add(PythonValueFormatter.Text(envelope.FieldName));
                arguments.Add(PythonValueFormatter.Text(envelope.ComponentName));
            }
            else if (output is ResultFieldOutputEquation equation)
            {
                method = verb + "_equation";
                arguments.Add(PythonValueFormatter.Text(equation.Equation));
                arguments.Add("unit=" + PythonValueFormatter.Text(equation.Unit));
            }
            else if (output is ResultFieldOutputCoordinateSystemTransform transform)
            {
                method = verb + "_coordinate_system_transform";
                arguments.Add(PythonValueFormatter.Text(transform.FieldName));
                arguments.Add(PythonValueFormatter.Text(transform.CoordinateSystemName));
            }
            else throw new NotSupportedException("the result field output type " + output.GetType().Name +
                                                 " has no API form");
            //
            AddFilter(arguments, "filter1", GetFieldFilter(output.Filter1));
            AddFilter(arguments, "filter2", GetFieldFilter(output.Filter2));
            //
            writer.Statement(writer.Call(FieldOutputs, method, arguments.ToArray()));
            writer.CountEmittedCommand();
        }
        public static void HistoryOutput(PythonJournalWriter writer, string oldName,
                                         ResultHistoryOutput output)
        {
            List<string> arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(output.Name));
            //
            string verb = oldName == null ? "add" : "replace";
            string method;
            if (output is ResultHistoryOutputFromField fromField)
            {
                method = verb + "_from_field";
                arguments.Add(PythonValueFormatter.Text(fromField.FieldName));
                arguments.Add(PythonValueFormatter.List(fromField.ComponentNames));
                arguments.Add(PythonRegionEmitter.Emit(writer, output.RegionName, output.RegionType,
                                                       output.CreationData));
                // A result replace re-specifies the whole definition, so an option left at its default is
                // left out on a replace as well
                if (fromField.ComplexResultType != ComplexResultTypeEnum.Real)
                    arguments.Add("complex_result_type=" +
                                  PythonValueFormatter.Text(fromField.ComplexResultType.ToString()));
                if (fromField.ComplexAngleDeg != 0)
                    arguments.Add("complex_angle_deg=" + PythonValueFormatter.Number(fromField.ComplexAngleDeg));
                if (fromField.StepId != -1)
                    arguments.Add("step_id=" + PythonValueFormatter.Format(fromField.StepId));
                if (fromField.StepIncrementId != -1)
                    arguments.Add("increment_id=" + PythonValueFormatter.Format(fromField.StepIncrementId));
                if (fromField.Harmonic) arguments.Add("harmonic=" + PythonValueFormatter.Format(true));
                if (fromField.OutputNodeCoordinates != OutputNodeCoordinatesEnum.Off)
                    arguments.Add("output_node_coordinates=" +
                                  PythonValueFormatter.Text(fromField.OutputNodeCoordinates.ToString()));
            }
            else if (output is ResultHistoryOutputFromEquation fromEquation)
            {
                method = verb + "_from_equation";
                arguments.Add(PythonValueFormatter.Text(fromEquation.Equation));
                arguments.Add("unit=" + PythonValueFormatter.Text(fromEquation.Unit));
            }
            else if (output is ResultHistoryOutputFromIntegration fromIntegration)
            {
                method = verb + "_from_integration";
                arguments.Add(PythonValueFormatter.Text(fromIntegration.IntegralType.ToString()));
                arguments.Add("field_name=" + PythonValueFormatter.Text(fromIntegration.FieldName));
                arguments.Add("component_names=" +
                              PythonValueFormatter.List(fromIntegration.ComponentNames));
                arguments.Add("region=" + PythonRegionEmitter.Emit(writer, output.RegionName,
                                                                   output.RegionType, output.CreationData));
                arguments.Add("integral_result=" +
                              PythonValueFormatter.Text(fromIntegration.IntegralResult.ToString()));
                arguments.Add("deformation_variable_name=" +
                              PythonValueFormatter.Text(fromIntegration.DeformationVariableName));
                arguments.Add("unit=" + PythonValueFormatter.Text(fromIntegration.Unit));
                arguments.Add("cross_section=" + PythonValueFormatter.Text(fromIntegration.CrossSection.ToString()));
                arguments.Add("thickness=" + PythonValueFormatter.Number(fromIntegration.Thickness));
            }
            else if (output is ResultHistoryOutputFromMergedHistories merged)
            {
                method = verb + "_from_merged_histories";
                arguments.Add(Source(merged.Source1));
                arguments.Add(Source(merged.Source2));
                if (merged.Source3 != null) arguments.Add("source3=" + Source(merged.Source3));
                if (merged.Source4 != null) arguments.Add("source4=" + Source(merged.Source4));
            }
            else if (output is ResultHistoryOutputFromExtractedEntry extracted)
            {
                method = verb + "_from_extracted_entry";
                arguments.Add(Source(extracted.Source));
                arguments.Add(PythonValueFormatter.Text(extracted.EntryName));
            }
            else if (output is ResultHistoryOutputXY xy)
            {
                method = verb + "_xy";
                arguments.Add(Source(xy.XSource));
                arguments.Add(Source(xy.YSource));
            }
            else if (output is ResultHistoryOutputFromTable fromTable)
            {
                method = verb + "_from_table";
                arguments.Add(Table(fromTable.DataPoints));
                arguments.Add("y_column_names=" + PythonValueFormatter.List(fromTable.YColumnNames));
                arguments.Add("x_axis_unit=" + PythonValueFormatter.Text(fromTable.XAxisUnit));
                arguments.Add("y_axis_unit=" + PythonValueFormatter.Text(fromTable.YAxisUnit));
            }
            else if (output is ResultHistoryOutputFromElementSize elementSize)
            {
                // The From Element Size output was replaced in v2.6.0 by an integration over the geometry,
                // so an old definition journals as the output that took its place
                method = verb + "_from_integration";
                arguments.Add(PythonValueFormatter.Text(elementSize.ElementSizeType == ElementSizeTypeEnum.Volume
                                                        ? "Volume" : "Surface"));
                arguments.Add("region=" + PythonRegionEmitter.Emit(writer, output.RegionName, output.RegionType,
                                                                   output.CreationData));
                arguments.Add("integral_result=" + PythonValueFormatter.Text("Geometry"));
                arguments.Add("deformation_variable_name=" +
                              PythonValueFormatter.Text(elementSize.DeformationVariableName));
            }
            else throw new NotSupportedException("the result history output type " + output.GetType().Name +
                                                 " has no API form");
            //
            AddFilter(arguments, "filter1", GetHistoryFilter(output.Filter1));
            AddFilter(arguments, "filter2", GetHistoryFilter(output.Filter2));
            //
            writer.Statement(writer.Call(HistoryOutputs, method, arguments.ToArray()));
            writer.CountEmittedCommand();
        }
        private static void AddFilter(List<string> arguments, string name, string maker)
        {
            if (maker == null) return;
            //
            arguments.Add(name + "=" + maker);
        }
        private static string GetFieldFilter(FieldResultFilter filter)
        {
            if (filter == null || filter.Type == FieldResultFilterTypeEnum.None) return null;
            //
            string method;
            if (filter.Type == FieldResultFilterTypeEnum.Median) method = "create_median_filter";
            else if (filter.Type == FieldResultFilterTypeEnum.Average) method = "create_average_filter";
            else if (filter.Type == FieldResultFilterTypeEnum.Smooth) method = "create_smooth_filter";
            else throw new NotSupportedException("the field filter " + filter.Type + " has no factory");
            //
            return PythonJournalWriter.Join(FieldOutputs + "." + method,
                                            PythonValueFormatter.Text(filter.SourceType.ToString()),
                                            "layers=" + filter.NumberOfNeighbouringLayers,
                                            "iterations=" + filter.NumberOfIterations);
        }
        private static string GetHistoryFilter(HistoryResultFilter filter)
        {
            if (filter == null || filter.Type == HistoryResultFilterTypeEnum.None) return null;
            //
            string method;
            string argument;
            if (filter.Type == HistoryResultFilterTypeEnum.Minimum)
            {
                method = "create_minimum_filter";
                argument = "return_value=";
            }
            else if (filter.Type == HistoryResultFilterTypeEnum.Maximum)
            {
                method = "create_maximum_filter";
                argument = "return_value=";
            }
            else if (filter.Type == HistoryResultFilterTypeEnum.Sum)
            {
                method = "create_sum_filter";
                argument = "operate_on=";
            }
            else if (filter.Type == HistoryResultFilterTypeEnum.Average)
            {
                method = "create_average_filter";
                argument = "operate_on=";
            }
            else throw new NotSupportedException("the history filter " + filter.Type + " has no factory");
            //
            string option = filter.Option == null ? null : argument + PythonValueFormatter.Text(filter.Option);
            return PythonJournalWriter.Join(HistoryOutputs + "." + method, option);
        }
        // A source names a history set, one of its fields and one of that field's components
        private static string Source(HistoryResultData source)
        {
            if (source == null) return "None";
            //
            return "[" + PythonValueFormatter.Text(source.SetName) + ", " +
                   PythonValueFormatter.Text(source.FieldName) + ", " +
                   PythonValueFormatter.Text(source.ComponentName) + "]";
        }
        private static string ItemLimits(OrderedDictionary<string, double> itemLimits)
        {
            if (itemLimits == null) throw new NotSupportedException("the limit plot carries no item limits");
            //
            List<string> entries = new List<string>();
            foreach (var entry in itemLimits)
                entries.Add(PythonValueFormatter.Text(entry.Key) + ": " + PythonValueFormatter.Number(entry.Value));
            //
            return "{" + string.Join(", ", entries) + "}";
        }
        private static string Table(double[][] table)
        {
            if (table == null) return "None";
            //
            List<string> rows = new List<string>();
            foreach (double[] row in table) rows.Add(PythonValueFormatter.List(row));
            //
            return "[" + string.Join(", ", rows) + "]";
        }
    }
}
