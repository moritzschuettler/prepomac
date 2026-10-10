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
using CaeJob;
using CaeModel;

namespace PrePoMax.Python.Journal
{
    // Each step type has its own API member, and they all share the solution values of the base type.
    // The shared values are emitted in full rather than compared against a new step, because the
    // application seeds a new step from its settings and those differ between machines.
    public static class PythonStepEmitter
    {
        // Methods                                                                                                                  
        public static void Add(PythonJournalWriter writer, Step step, bool copyBoundaryConditionsAndLoads)
        {
            List<string> arguments = new List<string> { PythonValueFormatter.Text(step.Name) };
            string method = "add_" + AddTypeArguments(arguments, step);
            AddCommonValues(arguments, step);
            arguments.Add("copy_bcs_and_loads=" + PythonValueFormatter.Format(copyBoundaryConditionsAndLoads));
            //
            Write(writer, method, arguments, step);
        }
        public static void Replace(PythonJournalWriter writer, string oldName, Step step)
        {
            List<string> arguments = new List<string> { PythonValueFormatter.Text(oldName),
                                                        PythonValueFormatter.Text(step.Name) };
            string method = "replace_" + AddTypeArguments(arguments, step);
            AddCommonValues(arguments, step);
            //
            Write(writer, method, arguments, step);
        }
        private static void Write(PythonJournalWriter writer, string method, List<string> arguments, Step step)
        {
            writer.Statement(writer.Call("pmx.model.steps", method, arguments.ToArray()));
            PythonModelItemEmitter.EmitState(writer, "pmx.model.steps", step);
            writer.CountEmittedCommand();
        }
        // The most derived type first, because the step types form a chain: a coupled temperature
        // displacement step is an uncoupled one, which is a heat transfer step, which is a static step
        private static string AddTypeArguments(List<string> arguments, Step step)
        {
            if (step is CoupledTempDispStep coupled)
            {
                AddHeatTransferValues(arguments, coupled);
                AddStaticValues(arguments, coupled);
                return "coupled_temp_disp";
            }
            if (step is UncoupledTempDispStep uncoupled)
            {
                AddHeatTransferValues(arguments, uncoupled);
                AddStaticValues(arguments, uncoupled);
                return "uncoupled_temp_disp";
            }
            if (step is HeatTransferStep heatTransfer)
            {
                AddHeatTransferValues(arguments, heatTransfer);
                AddStaticValues(arguments, heatTransfer);
                return "heat_transfer";
            }
            if (step is DynamicStep dynamic)
            {
                arguments.Add("alpha=" + PythonValueFormatter.Number(dynamic.Alpha));
                arguments.Add("solution_procedure=" +
                              PythonValueFormatter.Text(dynamic.SolutionProcedure.ToString()));
                arguments.Add("relative_to_absolute=" + PythonValueFormatter.Format(dynamic.RelativeToAbsolute));
                arguments.Add("damping=" + GetDamping(dynamic.Damping));
                AddStaticValues(arguments, dynamic);
                return "dynamic";
            }
            if (step is ModalDynamicsStep modalDynamics)
            {
                arguments.Add("steady_state=" + PythonValueFormatter.Format(modalDynamics.SteadyState));
                if (modalDynamics.SteadyState)
                    arguments.Add("relative_error=" + PythonValueFormatter.Number(modalDynamics.RelativeError));
                arguments.Add("modal_damping=" + GetModalDamping(modalDynamics.ModalDamping));
                if (!modalDynamics.SteadyState)
                    arguments.Add("time_period=" + PythonValueFormatter.Number(modalDynamics.TimePeriod));
                arguments.Add("initial_time_increment=" +
                              PythonValueFormatter.Number(modalDynamics.InitialTimeIncrement));
                return "modal_dynamics";
            }
            if (step is SlipWearStep slipWear)
            {
                AddStaticValues(arguments, slipWear);
                return "slip_wear";
            }
            if (step is StaticStep staticStep)
            {
                AddStaticValues(arguments, staticStep);
                return "static";
            }
            if (step is FrequencyStep frequency)
            {
                arguments.Add("number_of_frequencies=" + frequency.NumOfFrequencies);
                arguments.Add("lower_frequency=" + GetFrequencyLimit(frequency.LowerFrequency));
                arguments.Add("upper_frequency=" + GetFrequencyLimit(frequency.UpperFrequency));
                arguments.Add("storage=" + PythonValueFormatter.Format(frequency.Storage));
                arguments.Add("cyclic_symmetry_mode_min=" + GetUnsetCount(frequency.CyclicSymmetryModeMin));
                arguments.Add("cyclic_symmetry_mode_max=" + GetUnsetCount(frequency.CyclicSymmetryModeMax));
                return "frequency";
            }
            if (step is ComplexFrequencyStep complexFrequency)
            {
                arguments.Add("number_of_frequencies=" + complexFrequency.NumOfFrequencies);
                return "complex_frequency";
            }
            if (step is BuckleStep buckle)
            {
                arguments.Add("number_of_buckling_factors=" + buckle.NumOfBucklingFactors);
                arguments.Add("accuracy=" + PythonValueFormatter.Number(buckle.Accuracy));
                return "buckle";
            }
            if (step is SteadyStateDynamicsStep steadyState)
            {
                arguments.Add("harmonic=" + PythonValueFormatter.Format(steadyState.Harmonic));
                arguments.Add("lower_frequency=" + PythonValueFormatter.Number(steadyState.LowerFrequency));
                arguments.Add("upper_frequency=" + PythonValueFormatter.Number(steadyState.UpperFrequency));
                arguments.Add("number_of_data_points=" + PythonValueFormatter.Format(steadyState.NumDataPoints));
                arguments.Add("bias=" + PythonValueFormatter.Number(steadyState.Bias));
                // Only nonharmonic loading uses the Fourier terms and the time range, which the dialog shows only
                // then: the API refuses them otherwise, and a replace keeps them
                if (!steadyState.Harmonic)
                {
                    arguments.Add("number_of_fourier_terms=" + steadyState.NumFourierTerms);
                    arguments.Add("time_lower=" + PythonValueFormatter.Number(steadyState.TimeLower));
                    arguments.Add("time_upper=" + PythonValueFormatter.Number(steadyState.TimeUpper));
                }
                arguments.Add("modal_damping=" + GetModalDamping(steadyState.ModalDamping));
                return "steady_state_dynamics";
            }
            if (step is BoundaryDisplacementStep) return "boundary_displacement";
            //
            throw new NotSupportedException("the step type " + step.GetType().Name + " has no API form");
        }
        // Only the values the incrementation type uses, which are the values the dialog shows: the API refuses the
        // others, and a replace keeps them
        private static void AddStaticValues(List<string> arguments, StaticStep step)
        {
            if (step.IncrementationType == IncrementationTypeEnum.Default) return;
            //
            arguments.Add("time_period=" + PythonValueFormatter.Number(step.TimePeriod));
            arguments.Add("initial_time_increment=" + PythonValueFormatter.Number(step.InitialTimeIncrement));
            if (step.IncrementationType != IncrementationTypeEnum.Automatic) return;
            //
            arguments.Add("min_time_increment=" + PythonValueFormatter.Number(step.MinTimeIncrement));
            arguments.Add("max_time_increment=" + PythonValueFormatter.Number(step.MaxTimeIncrement));
        }
        private static void AddHeatTransferValues(List<string> arguments, HeatTransferStep step)
        {
            arguments.Add("steady_state=" + PythonValueFormatter.Format(step.SteadyState));
            // An unlimited temperature change is the default, and a replace must be able to restore it
            arguments.Add("deltmx=" + (double.IsPositiveInfinity(step.Deltmx) ? "float('inf')" :
                                       PythonValueFormatter.Number(step.Deltmx)));
        }
        // A limit that is not set is NaN, which None would keep on a replace instead of clearing
        private static string GetFrequencyLimit(double limit)
        {
            return double.IsNaN(limit) ? "float('nan')" : PythonValueFormatter.Number(limit);
        }
        // A count that is not set is the smallest integer, which the API takes as float('nan'), as it does a limit
        private static string GetUnsetCount(int count)
        {
            return count == int.MinValue ? "float('nan')" : PythonValueFormatter.Format(count);
        }
        // Only the shared values the step type uses: the API refuses the others, and a replace keeps them. A boundary
        // displacement step is not written to the solver input, so it uses none. A heat transfer step does not
        // show Nlgeom, only a step of the add_static arguments shows the incrementation and writes the maximum
        // number of increments unless the type is Default, a modal dynamics step always has the Automatic type,
        // and a complex frequency step writes no solver
        private static void AddCommonValues(List<string> arguments, Step step)
        {
            if (step is BoundaryDisplacementStep) return;
            //
            if (step.GetType() != typeof(HeatTransferStep))
                arguments.Add("nlgeom=" + PythonValueFormatter.Format(step.Nlgeom));
            if (step is StaticStep && step.IncrementationType != IncrementationTypeEnum.Default)
                arguments.Add("max_increments=" + step.MaxIncrements);
            if (step is StaticStep && !(step is ModalDynamicsStep))
                arguments.Add("incrementation_type=" + PythonValueFormatter.Text(step.IncrementationType.ToString()));
            if (!(step is ComplexFrequencyStep))
                arguments.Add("solver_type=" + PythonValueFormatter.Text(step.SolverType.ToString()));
            arguments.Add("output_frequency=" + GetUnsetCount(step.OutputFrequency));
            arguments.Add("perturbation=" + PythonValueFormatter.Format(step.Perturbation));
            arguments.Add("run_analysis=" + PythonValueFormatter.Format(step.RunAnalysis));
        }
        // Only the values the damping type uses, which are the values the dialog shows: the maker refuses the others
        private static string GetDamping(Damping damping)
        {
            if (damping == null) return "None";
            //
            string dampingType = PythonValueFormatter.Text(damping.DampingType.ToString());
            if (damping.DampingType != DampingTypeEnum.Rayleigh)
                return PythonJournalWriter.Join("pmx.create.damping", dampingType);
            //
            return PythonJournalWriter.Join("pmx.create.damping", dampingType,
                                            PythonValueFormatter.Number(damping.Alpha),
                                            PythonValueFormatter.Number(damping.Beta));
        }
        private static string GetModalDamping(ModalDamping damping)
        {
            if (damping == null) return "None";
            //
            List<string> arguments = new List<string> { PythonValueFormatter.Text(damping.DampingType.ToString()) };
            if (damping.DampingType == ModalDampingTypeEnum.Rayleigh)
            {
                arguments.Add(PythonValueFormatter.Number(damping.Alpha));
                arguments.Add(PythonValueFormatter.Number(damping.Beta));
            }
            else if (damping.DampingType == ModalDampingTypeEnum.Constant)
                arguments.Add("viscous_damping_ratio=" + PythonValueFormatter.Number(damping.ViscousDampingRatio));
            else if (damping.DampingType == ModalDampingTypeEnum.Direct && damping.DampingRatiosAndRanges != null &&
                     damping.DampingRatiosAndRanges.Count > 0)
            {
                List<string> ratios = new List<string>();
                foreach (DampingRatioAndRange ratio in damping.DampingRatiosAndRanges)
                    ratios.Add(PythonJournalWriter.Join("pmx.create.damping_ratio",
                                                        PythonValueFormatter.Number(ratio.DampingRatio),
                                                        PythonValueFormatter.Format(ratio.LowestMode),
                                                        PythonValueFormatter.Format(ratio.HighestMode)));
                arguments.Add("damping_ratios=[" + string.Join(", ", ratios) + "]");
            }
            //
            return PythonJournalWriter.Join("pmx.create.modal_damping", arguments.ToArray());
        }
    }
}
