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
using CaeModel;

namespace PrePoMax.Python.Journal
{
    // The four boundary condition types, each with its own API member. An unconstrained degree of
    // freedom carries a NaN and is left out of an add, which leaves it free, and a fixed one carries positive
    // infinity and is named in fixed_dofs. A replace keeps every value it is not given, so there the free
    // degrees of freedom are named in free_dofs and a submodel flag that is off is written out.
    public static class PythonBoundaryConditionEmitter
    {
        // Variables
        private const string Distributions = "pmx.model.distributions";


        // Methods                                                                                                                 
        public static void Add(PythonJournalWriter writer, string stepName, BoundaryCondition condition)
        {
            Emit(writer, stepName, "add", null, condition);
        }
        public static void Replace(PythonJournalWriter writer, string stepName, string oldName,
                                   BoundaryCondition condition)
        {
            Emit(writer, stepName, "replace", oldName, condition);
        }
        private static void Emit(PythonJournalWriter writer, string stepName, string verb, string oldName,
                                 BoundaryCondition condition)
        {
            string collection = PythonStepItemEmitter.GetCollection(stepName, "bcs");
            bool replace = oldName != null;
            string region = PythonRegionEmitter.EmitKept(writer, condition.RegionName, condition.RegionType,
                                                         condition.CreationData, replace);
            List<string> arguments = new List<string>();
            if (replace) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(condition.Name));
            PythonJournalWriter.AddPositional(arguments, "region", region);
            //
            string method;
            if (condition is FixedBC)
            {
                method = verb + "_fixed";
            }
            else if (condition is DisplacementRotation displacementRotation)
            {
                method = verb + "_displacement_rotation";
                AddDegreeOfFreedom(arguments, "u1", displacementRotation.U1);
                AddDegreeOfFreedom(arguments, "u2", displacementRotation.U2);
                AddDegreeOfFreedom(arguments, "u3", displacementRotation.U3);
                AddDegreeOfFreedom(arguments, "ur1", displacementRotation.UR1);
                AddDegreeOfFreedom(arguments, "ur2", displacementRotation.UR2);
                AddDegreeOfFreedom(arguments, "ur3", displacementRotation.UR3);
                AddFixedDegreesOfFreedom(arguments, displacementRotation);
                if (replace) AddFreeDegreesOfFreedom(arguments, displacementRotation);
                string distributionName = writer.KeptReference(Distributions, displacementRotation.DistributionName,
                                                               replace);
                if (distributionName != null &&
                    (replace || distributionName != Distribution.DefaultDistributionName))
                    arguments.Add("distribution_name=" + PythonValueFormatter.Text(distributionName));
                PythonStepItemEmitter.AddAmplitude(writer, arguments, condition.AmplitudeName, replace);
                PythonStepItemEmitter.AddCoordinateSystem(writer, arguments, condition.CoordinateSystemName, replace);
                PythonStepItemEmitter.AddPhase(arguments, condition.Complex, condition.PhaseDeg);
            }
            else if (condition is SubmodelBC submodel)
            {
                method = verb + "_submodel";
                arguments.Add("step_id=" + PythonValueFormatter.Equation(submodel.StepNumber));
                AddFlag(arguments, "u1", submodel.U1, replace);
                AddFlag(arguments, "u2", submodel.U2, replace);
                AddFlag(arguments, "u3", submodel.U3, replace);
                AddFlag(arguments, "ur1", submodel.UR1, replace);
                AddFlag(arguments, "ur2", submodel.UR2, replace);
                AddFlag(arguments, "ur3", submodel.UR3, replace);
            }
            else if (condition is TemperatureBC temperature)
            {
                method = verb + "_temperature";
                arguments.Add("temperature=" + PythonValueFormatter.Equation(temperature.Temperature));
                string distributionName = writer.KeptReference(Distributions, temperature.DistributionName, replace);
                if (!string.IsNullOrEmpty(distributionName))
                    arguments.Add("distribution_name=" + PythonValueFormatter.Text(distributionName));
                PythonStepItemEmitter.AddAmplitude(writer, arguments, condition.AmplitudeName, replace);
            }
            else throw new NotSupportedException("the boundary condition type " + condition.GetType().Name +
                                                 " has no API form");
            //
            PythonStepItemEmitter.AddColor(arguments, condition.Color, new FixedBC("Default", null,
                                                                                   RegionTypeEnum.Selection,
                                                                                   false).Color, replace);
            //
            writer.Statement(writer.Call(collection, method, arguments.ToArray()));
            PythonModelItemEmitter.EmitState(writer, collection, condition);
            writer.CountEmittedCommand();
        }
        // A degree of freedom that is not constrained carries a NaN, and a fixed one positive infinity; neither
        // is a value
        private static void AddDegreeOfFreedom(List<string> arguments, string name, EquationContainer value)
        {
            if (value == null || double.IsNaN(value.Value) || double.IsPositiveInfinity(value.Value)) return;
            //
            arguments.Add(name + "=" + PythonValueFormatter.Equation(value));
        }
        private static void AddFixedDegreesOfFreedom(List<string> arguments, DisplacementRotation condition)
        {
            List<string> fixedDofs = new List<string>();
            AddIfFixed(fixedDofs, "U1", condition.U1);
            AddIfFixed(fixedDofs, "U2", condition.U2);
            AddIfFixed(fixedDofs, "U3", condition.U3);
            AddIfFixed(fixedDofs, "UR1", condition.UR1);
            AddIfFixed(fixedDofs, "UR2", condition.UR2);
            AddIfFixed(fixedDofs, "UR3", condition.UR3);
            if (fixedDofs.Count == 0) return;
            //
            arguments.Add("fixed_dofs=[" + string.Join(", ", fixedDofs.ConvertAll(PythonValueFormatter.Text)) + "]");
        }
        private static void AddIfFixed(List<string> fixedDofs, string name, EquationContainer value)
        {
            if (value != null && double.IsPositiveInfinity(value.Value)) fixedDofs.Add(name);
        }
        private static void AddFreeDegreesOfFreedom(List<string> arguments, DisplacementRotation condition)
        {
            List<string> free = new List<string>();
            AddIfFree(free, "U1", condition.U1);
            AddIfFree(free, "U2", condition.U2);
            AddIfFree(free, "U3", condition.U3);
            AddIfFree(free, "UR1", condition.UR1);
            AddIfFree(free, "UR2", condition.UR2);
            AddIfFree(free, "UR3", condition.UR3);
            if (free.Count == 0) return;
            //
            arguments.Add("free_dofs=[" + string.Join(", ", free.ConvertAll(PythonValueFormatter.Text)) + "]");
        }
        private static void AddIfFree(List<string> free, string name, EquationContainer value)
        {
            if (value == null || double.IsNaN(value.Value)) free.Add(name);
        }
        private static void AddFlag(List<string> arguments, string name, bool value, bool replace)
        {
            if (value) arguments.Add(name + "=True");
            else if (replace) arguments.Add(name + "=False");
        }
    }
}
