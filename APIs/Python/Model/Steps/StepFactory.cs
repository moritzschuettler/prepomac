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
    // step keeps the application default and a replaced step keeps its stored value. Several step
    // setters clamp a value outside their range or throw without naming the argument, so every given
    // value is checked here first and refused with the name of the argument that carries it.
    internal static class StepFactory
    {
        // Methods
        public static void ApplyCommonValues(Step step, StepCommonValues values)
        {
            if (values == null) return;
            //
            if (values.Nlgeom != null) step.Nlgeom = PythonValueConverter.ToBool(values.Nlgeom, "nlgeom");
            if (values.MaxIncrements != null)
                step.MaxIncrements = ToIntAtLeast(values.MaxIncrements, "max_increments", 1);
            if (values.IncrementationType != null)
                step.IncrementationType =
                    PythonValueConverter.ToEnum<IncrementationTypeEnum>(
                        AsString(values.IncrementationType, "incrementation_type"), "incrementation_type");
            if (values.SolverType != null)
                step.SolverType =
                    PythonValueConverter.ToEnum<SolverTypeEnum>(AsString(values.SolverType, "solver_type"),
                                                                "solver_type");
            if (values.OutputFrequency != null)
                step.OutputFrequency = ToIntAtLeastOrUnset(values.OutputFrequency, "output_frequency", 0);
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
        // The model shortens an increment longer than the time period, and an initial increment longer than the
        // maximum one. It may do so with a default of a new step, but never with a value the caller gave, and on a
        // replace every stored value counts as one the caller gave.
        public static void ApplyStaticValues(StaticStep step, object timePeriod, object initialTimeIncrement,
                                             object minTimeIncrement, object maxTimeIncrement, object direct,
                                             bool replace)
        {
            double period = timePeriod == null ? step.TimePeriod : ToPositiveDouble(timePeriod, "time_period");
            double initial = initialTimeIncrement == null ? step.InitialTimeIncrement :
                             ToPositiveDouble(initialTimeIncrement, "initial_time_increment");
            double minimum = minTimeIncrement == null ? step.MinTimeIncrement :
                             ToPositiveDouble(minTimeIncrement, "min_time_increment");
            double maximum = maxTimeIncrement == null ? step.MaxTimeIncrement :
                             ToPositiveDouble(maxTimeIncrement, "max_time_increment");
            //
            if ((replace || initialTimeIncrement != null) && initial > period)
                throw new PythonAPIException("The initial time increment " + initial + " exceeds the time period " +
                                             period + ". Give a smaller 'initial_time_increment' or a larger " +
                                             "'time_period'.");
            if ((replace || minTimeIncrement != null) && minimum > period)
                throw new PythonAPIException("The minimum time increment " + minimum + " exceeds the time period " +
                                             period + ". Give a smaller 'min_time_increment' or a larger " +
                                             "'time_period'.");
            if ((replace || initialTimeIncrement != null) && initial > maximum)
                throw new PythonAPIException("The initial time increment " + initial + " exceeds the maximum time " +
                                             "increment " + maximum + ". Give a smaller 'initial_time_increment' or " +
                                             "a larger 'max_time_increment'.");
            //
            if (timePeriod != null) step.TimePeriod = period;
            if (maxTimeIncrement != null) step.MaxTimeIncrement = maximum;
            if (initialTimeIncrement != null) step.InitialTimeIncrement = initial;
            if (minTimeIncrement != null) step.MinTimeIncrement = minimum;
            if (direct != null) step.Direct = PythonValueConverter.ToBool(direct, "direct");
        }
        public static void ApplyFrequencyValues(FrequencyStep step, object numOfFrequencies, object lowerFrequency,
                                                object upperFrequency, object storage,
                                                object cyclicSymmetryModeMin, object cyclicSymmetryModeMax)
        {
            if (numOfFrequencies != null)
                step.NumOfFrequencies = ToIntAtLeast(numOfFrequencies, "number_of_frequencies", 1);
            // The model reads a stored upper frequency of 0 as no upper limit, but refuses to set one below a lower limit
            double lower = lowerFrequency == null ? step.LowerFrequency :
                           ToDoubleAtLeast(lowerFrequency, "lower_frequency", 0);
            double upper = upperFrequency == null ? step.UpperFrequency :
                           ToDoubleAtLeast(upperFrequency, "upper_frequency", 0);
            if ((lowerFrequency != null || upperFrequency != null) && !double.IsNaN(lower) && !double.IsNaN(upper) &&
                lower >= upper && (upper != 0 || (upperFrequency != null && lower != 0)))
                throw new PythonAPIException("The lower frequency " + lower + " must be smaller than the upper " +
                                             "frequency " + upper + ". Give a smaller 'lower_frequency' or a larger " +
                                             "'upper_frequency'.");
            SetFrequencyRange(lowerFrequency, upperFrequency, lower, upper, step.UpperFrequency,
                              value => step.LowerFrequency = value, value => step.UpperFrequency = value);
            if (storage != null) step.Storage = PythonValueConverter.ToBool(storage, "storage");
            // Each mode limit setter moves the other limit past it silently, so the pair is checked as it is stored
            int modeMin = cyclicSymmetryModeMin == null ? step.CyclicSymmetryModeMin :
                          ToIntAtLeastOrUnset(cyclicSymmetryModeMin, "cyclic_symmetry_mode_min", 0);
            int modeMax = cyclicSymmetryModeMax == null ? step.CyclicSymmetryModeMax :
                          ToIntAtLeastOrUnset(cyclicSymmetryModeMax, "cyclic_symmetry_mode_max", 0);
            if ((cyclicSymmetryModeMin != null || cyclicSymmetryModeMax != null) && modeMin != int.MinValue &&
                modeMax != int.MinValue && modeMin > modeMax)
                throw new PythonAPIException("The cyclic symmetry mode range " + modeMin + " to " + modeMax + " is " +
                                             "empty. Give a 'cyclic_symmetry_mode_min' that does not exceed " +
                                             "'cyclic_symmetry_mode_max'.");
            if (cyclicSymmetryModeMin != null) step.CyclicSymmetryModeMin = modeMin;
            if (cyclicSymmetryModeMax != null) step.CyclicSymmetryModeMax = modeMax;
        }
        public static void ApplyComplexFrequencyValues(ComplexFrequencyStep step, object numOfFrequencies)
        {
            if (numOfFrequencies != null)
                step.NumOfFrequencies = ToIntAtLeast(numOfFrequencies, "number_of_frequencies", 1);
        }
        public static void ApplyBuckleValues(BuckleStep step, object numOfBucklingFactors, object accuracy)
        {
            if (numOfBucklingFactors != null)
                step.NumOfBucklingFactors = ToIntAtLeast(numOfBucklingFactors, "number_of_buckling_factors", 1);
            if (accuracy != null) step.Accuracy = PythonValueConverter.ToDouble(accuracy, "accuracy");
        }
        public static void ApplyModalDynamicsValues(ModalDynamicsStep step, object steadyState,
                                                    object relativeError, object modalDamping)
        {
            if (steadyState != null) step.SteadyState = PythonValueConverter.ToBool(steadyState, "steady_state");
            if (relativeError != null)
                step.RelativeError = ToDoubleBetween(relativeError, "relative_error", 0, 1);
            if (modalDamping != null) step.ModalDamping = ToModalDamping(modalDamping);
        }
        public static void ApplySteadyStateDynamicsValues(SteadyStateDynamicsStep step, object harmonic,
                                                          object lowerFrequency, object upperFrequency,
                                                          object numDataPoints, object bias,
                                                          object numFourierTerms, object timeLower,
                                                          object timeUpper, object modalDamping)
        {
            if (harmonic != null) step.Harmonic = PythonValueConverter.ToBool(harmonic, "harmonic");
            double lower = lowerFrequency == null ? step.LowerFrequency :
                           PythonValueConverter.ToDouble(lowerFrequency, "lower_frequency");
            double upper = upperFrequency == null ? step.UpperFrequency :
                           PythonValueConverter.ToDouble(upperFrequency, "upper_frequency");
            if ((lowerFrequency != null || upperFrequency != null) && lower > upper)
                throw new PythonAPIException("The lower frequency " + lower + " exceeds the upper frequency " + upper +
                                             ". Give a smaller 'lower_frequency' or a larger 'upper_frequency'.");
            SetFrequencyRange(lowerFrequency, upperFrequency, lower, upper, step.UpperFrequency,
                              value => step.LowerFrequency = value, value => step.UpperFrequency = value);
            if (numDataPoints != null)
                step.NumDataPoints = ToIntAtLeastOrUnset(numDataPoints, "number_of_data_points", 2);
            if (bias != null) step.Bias = ToDoubleAtLeast(bias, "bias", 1);
            if (numFourierTerms != null)
                step.NumFourierTerms = ToIntAtLeast(numFourierTerms, "number_of_fourier_terms", 1);
            if (timeLower != null) step.TimeLower = PythonValueConverter.ToDouble(timeLower, "time_lower");
            if (timeUpper != null) step.TimeUpper = PythonValueConverter.ToDouble(timeUpper, "time_upper");
            if (modalDamping != null) step.ModalDamping = ToModalDamping(modalDamping);
        }
        public static void ApplyDynamicValues(DynamicStep step, object alpha, object solutionProcedure,
                                              object relativeToAbsolute, object damping)
        {
            if (alpha != null) step.Alpha = ToDoubleBetween(alpha, "alpha", -1.0 / 3.0, 0);
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
            if (deltmx != null)
            {
                double value = PythonValueConverter.ToDouble(deltmx, "deltmx");
                if (double.IsNaN(value) || value <= 0)
                    throw new PythonAPIException("The argument 'deltmx' must be a positive number, or float('inf') " +
                                                 "for no limit.");
                step.Deltmx = value;
            }
        }
        //
        // The model refuses a lower frequency at or above the stored upper one and the other way round, so a call
        // that moves both limits past each other sets the upper one first
        private static void SetFrequencyRange(object lowerFrequency, object upperFrequency, double lower,
                                              double upper, double storedUpper, System.Action<double> setLower,
                                              System.Action<double> setUpper)
        {
            bool upperFirst = upperFrequency != null && lowerFrequency != null && lower >= storedUpper;
            //
            if (upperFirst) setUpper(upper);
            if (lowerFrequency != null) setLower(lower);
            if (upperFrequency != null && !upperFirst) setUpper(upper);
        }
        private static int ToIntAtLeast(object value, string argumentName, int minimum)
        {
            int converted = PythonValueConverter.ToInt(value, argumentName);
            if (converted < minimum)
                throw new PythonAPIException("The argument '" + argumentName + "' must be at least " + minimum + ".");
            //
            return converted;
        }
        // The model stores the smallest integer for a value that is not set, and the journal writes it out as such
        private static int ToIntAtLeastOrUnset(object value, string argumentName, int minimum)
        {
            int converted = PythonValueConverter.ToInt(value, argumentName);
            if (converted != int.MinValue && converted < minimum)
                throw new PythonAPIException("The argument '" + argumentName + "' must be at least " + minimum + ".");
            //
            return converted;
        }
        private static double ToDoubleAtLeast(object value, string argumentName, double minimum)
        {
            double converted = PythonValueConverter.ToDouble(value, argumentName);
            if (!(converted >= minimum))
                throw new PythonAPIException("The argument '" + argumentName + "' must be at least " + minimum + ".");
            //
            return converted;
        }
        private static double ToPositiveDouble(object value, string argumentName)
        {
            double converted = PythonValueConverter.ToDouble(value, argumentName);
            if (!(converted > 0))
                throw new PythonAPIException("The argument '" + argumentName + "' must be positive.");
            //
            return converted;
        }
        private static double ToDoubleBetween(object value, string argumentName, double minimum, double maximum)
        {
            double converted = PythonValueConverter.ToDouble(value, argumentName);
            if (!(converted >= minimum && converted <= maximum))
                throw new PythonAPIException("The argument '" + argumentName + "' must be between " + minimum +
                                             " and " + maximum + ".");
            //
            return converted;
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
