// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;

namespace APIs.Python.Model
{
    // Applies the given values to a step. A null value keeps what the step already carries, so a new
    // step keeps the application default and a replaced step keeps its stored value.
    internal static class StepFactory
    {
        // Methods
        public static void ApplyCommonValues(Step step, StepCommonValues values)
        {
            if (values == null) return;
            //
            if (values.Nlgeom != null) step.Nlgeom = PythonValueConverter.ToBool(values.Nlgeom, "nlgeom");
            if (values.MaxIncrements != null)
                step.MaxIncrements = PythonValueConverter.ToInt(values.MaxIncrements, "max_increments");
            if (values.IncrementationType != null)
                step.IncrementationType =
                    PythonValueConverter.ToEnum<IncrementationTypeEnum>(
                        AsString(values.IncrementationType, "incrementation_type"), "incrementation_type");
            if (values.SolverType != null)
                step.SolverType =
                    PythonValueConverter.ToEnum<SolverTypeEnum>(AsString(values.SolverType, "solver_type"),
                                                                "solver_type");
            if (values.OutputFrequency != null)
                step.OutputFrequency = PythonValueConverter.ToInt(values.OutputFrequency, "output_frequency");
            if (values.RunAnalysis != null)
                step.RunAnalysis = PythonValueConverter.ToBool(values.RunAnalysis, "run_analysis");
            if (values.Perturbation != null)
            {
                bool perturbation = PythonValueConverter.ToBool(values.Perturbation, "perturbation");
                if (perturbation && !step.IsPerturbationSupported())
                    throw new PythonAPIException("The step type " + step.GetType().Name +
                                                 " does not support a perturbation step.");
                step.Perturbation = perturbation;
            }
        }
        public static void ApplyStaticValues(StaticStep step, object timePeriod, object initialTimeIncrement,
                                             object minTimeIncrement, object maxTimeIncrement, object direct)
        {
            if (timePeriod != null) step.TimePeriod = PythonValueConverter.ToDouble(timePeriod, "time_period");
            if (initialTimeIncrement != null)
                step.InitialTimeIncrement =
                    PythonValueConverter.ToDouble(initialTimeIncrement, "initial_time_increment");
            if (minTimeIncrement != null)
                step.MinTimeIncrement = PythonValueConverter.ToDouble(minTimeIncrement, "min_time_increment");
            if (maxTimeIncrement != null)
                step.MaxTimeIncrement = PythonValueConverter.ToDouble(maxTimeIncrement, "max_time_increment");
            if (direct != null) step.Direct = PythonValueConverter.ToBool(direct, "direct");
        }
        public static void ApplyFrequencyValues(FrequencyStep step, object numOfFrequencies, object lowerFrequency,
                                                object upperFrequency, object storage,
                                                object cyclicSymmetryModeMin, object cyclicSymmetryModeMax)
        {
            if (numOfFrequencies != null)
                step.NumOfFrequencies = PythonValueConverter.ToInt(numOfFrequencies, "num_of_frequencies");
            SetFrequencyRange(lowerFrequency, upperFrequency, step.UpperFrequency,
                              value => step.LowerFrequency = value, value => step.UpperFrequency = value);
            if (storage != null) step.Storage = PythonValueConverter.ToBool(storage, "storage");
            if (cyclicSymmetryModeMin != null)
                step.CyclicSymmetryModeMin =
                    PythonValueConverter.ToInt(cyclicSymmetryModeMin, "cyclic_symmetry_mode_min");
            if (cyclicSymmetryModeMax != null)
                step.CyclicSymmetryModeMax =
                    PythonValueConverter.ToInt(cyclicSymmetryModeMax, "cyclic_symmetry_mode_max");
        }
        public static void ApplyComplexFrequencyValues(ComplexFrequencyStep step, object numOfFrequencies)
        {
            if (numOfFrequencies != null)
                step.NumOfFrequencies = PythonValueConverter.ToInt(numOfFrequencies, "num_of_frequencies");
        }
        public static void ApplyBuckleValues(BuckleStep step, object numOfBucklingFactors, object accuracy)
        {
            if (numOfBucklingFactors != null)
                step.NumOfBucklingFactors =
                    PythonValueConverter.ToInt(numOfBucklingFactors, "num_of_buckling_factors");
            if (accuracy != null) step.Accuracy = PythonValueConverter.ToDouble(accuracy, "accuracy");
        }
        public static void ApplyModalDynamicsValues(ModalDynamicsStep step, object steadyState,
                                                    object relativeError, object modalDamping)
        {
            if (steadyState != null) step.SteadyState = PythonValueConverter.ToBool(steadyState, "steady_state");
            if (relativeError != null)
                step.RelativeError = PythonValueConverter.ToDouble(relativeError, "relative_error");
            if (modalDamping != null) step.ModalDamping = ToModalDamping(modalDamping);
        }
        public static void ApplySteadyStateDynamicsValues(SteadyStateDynamicsStep step, object harmonic,
                                                          object lowerFrequency, object upperFrequency,
                                                          object numDataPoints, object bias,
                                                          object numFourierTerms, object timeLower,
                                                          object timeUpper, object modalDamping)
        {
            if (harmonic != null) step.Harmonic = PythonValueConverter.ToBool(harmonic, "harmonic");
            SetFrequencyRange(lowerFrequency, upperFrequency, step.UpperFrequency,
                              value => step.LowerFrequency = value, value => step.UpperFrequency = value);
            if (numDataPoints != null)
                step.NumDataPoints = PythonValueConverter.ToInt(numDataPoints, "num_data_points");
            if (bias != null) step.Bias = PythonValueConverter.ToDouble(bias, "bias");
            if (numFourierTerms != null)
                step.NumFourierTerms = PythonValueConverter.ToInt(numFourierTerms, "num_fourier_terms");
            if (timeLower != null) step.TimeLower = PythonValueConverter.ToDouble(timeLower, "time_lower");
            if (timeUpper != null) step.TimeUpper = PythonValueConverter.ToDouble(timeUpper, "time_upper");
            if (modalDamping != null) step.ModalDamping = ToModalDamping(modalDamping);
        }
        public static void ApplyDynamicValues(DynamicStep step, object alpha, object solutionProcedure,
                                              object relativeToAbsolute, object damping)
        {
            if (alpha != null) step.Alpha = PythonValueConverter.ToDouble(alpha, "alpha");
            if (solutionProcedure != null)
                step.SolutionProcedure =
                    PythonValueConverter.ToEnum<SolutionProcedureEnum>(
                        AsString(solutionProcedure, "solution_procedure"), "solution_procedure");
            if (relativeToAbsolute != null)
                step.RelativeToAbsolute = PythonValueConverter.ToBool(relativeToAbsolute, "relative_to_absolute");
            if (damping != null)
            {
                Damping converted = damping as Damping;
                if (converted == null)
                    throw new PythonAPIException("The argument 'damping' must be a damping part created by " +
                                                 "pmx.create.damping.");
                step.Damping = converted;
            }
        }
        public static void ApplyHeatTransferValues(HeatTransferStep step, object steadyState, object deltmx)
        {
            if (steadyState != null) step.SteadyState = PythonValueConverter.ToBool(steadyState, "steady_state");
            if (deltmx != null) step.Deltmx = PythonValueConverter.ToDouble(deltmx, "deltmx");
        }
        //
        // The model refuses a lower frequency at or above the stored upper one and the other way round, so a call
        // that moves both limits past each other sets the upper one first
        private static void SetFrequencyRange(object lowerFrequency, object upperFrequency, double storedUpper,
                                              System.Action<double> setLower, System.Action<double> setUpper)
        {
            double lower = lowerFrequency == null ? double.NaN :
                           PythonValueConverter.ToDouble(lowerFrequency, "lower_frequency");
            double upper = upperFrequency == null ? double.NaN :
                           PythonValueConverter.ToDouble(upperFrequency, "upper_frequency");
            bool upperFirst = upperFrequency != null && lowerFrequency != null && lower >= storedUpper;
            //
            if (upperFirst) setUpper(upper);
            if (lowerFrequency != null) setLower(lower);
            if (upperFrequency != null && !upperFirst) setUpper(upper);
        }
        private static ModalDamping ToModalDamping(object value)
        {
            ModalDamping converted = value as ModalDamping;
            if (converted == null)
                throw new PythonAPIException("The argument 'modal_damping' must be a modal damping part created " +
                                             "by pmx.create.modal_damping.");
            //
            return converted;
        }
        private static string AsString(object value, string argumentName)
        {
            string text = value as string;
            if (text == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must be a string.");
            //
            return text;
        }
    }
}
