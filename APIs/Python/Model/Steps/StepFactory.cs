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
        // The incrementation type is applied first, so the step decides with the type the call ends with whether it
        // uses the maximum number of increments. A shared value the step type does not use may only be given as the
        // value the step carries, or as the one its dialog always gives it, so that a value read back goes back in.
        public static void ApplyCommonValues(Step step, StepCommonValues values)
        {
            if (values == null) return;
            //
            if (values.IncrementationType != null)
            {
                IncrementationTypeEnum incrementationType =
                    PythonValueConverter.ToEnum<IncrementationTypeEnum>(
                        AsString(values.IncrementationType, "incrementation_type"), "incrementation_type");
                EnsureUsedOrKept(step, incrementationType, step.IncrementationType, GetFixedIncrementationType(step),
                                 "incrementation_type");
                step.IncrementationType = incrementationType;
            }
            if (values.Nlgeom != null)
            {
                bool nlgeom = PythonValueConverter.ToBool(values.Nlgeom, "nlgeom");
                EnsureUsedOrKept(step, nlgeom, step.Nlgeom, false, "nlgeom");
                step.Nlgeom = nlgeom;
            }
            EnsureUsed(step, values.MaxIncrements, "max_increments");
            if (values.MaxIncrements != null)
                step.MaxIncrements = ToIntAtLeast(values.MaxIncrements, "max_increments", 1);
            if (values.SolverType != null)
            {
                SolverTypeEnum solverType =
                    PythonValueConverter.ToEnum<SolverTypeEnum>(AsString(values.SolverType, "solver_type"),
                                                                "solver_type");
                EnsureUsedOrKept(step, solverType, step.SolverType, null, "solver_type");
                step.SolverType = solverType;
            }
            if (values.OutputFrequency != null)
            {
                int outputFrequency = ToIntAtLeastOrUnset(values.OutputFrequency, "output_frequency", 0);
                EnsureUsedOrKept(step, outputFrequency, step.OutputFrequency, null, "output_frequency");
                step.OutputFrequency = outputFrequency;
            }
            if (values.RunAnalysis != null)
            {
                bool runAnalysis = PythonValueConverter.ToBool(values.RunAnalysis, "run_analysis");
                EnsureUsedOrKept(step, runAnalysis, step.RunAnalysis, null, "run_analysis");
                step.RunAnalysis = runAnalysis;
            }
            if (values.Perturbation != null)
            {
                bool perturbation = PythonValueConverter.ToBool(values.Perturbation, "perturbation");
                EnsureUsedOrKept(step, perturbation, step.Perturbation, null, "perturbation");
                if (perturbation && !step.IsPerturbationSupported())
                    throw new PythonAPIException("The step type " + step.GetType().Name +
                                                 " does not support a perturbation step.");
                step.Perturbation = perturbation;
            }
        }
        // The model shortens an increment longer than the time period, which it may do with a default of a new
        // step, but never with a value the caller gave, and on a replace every stored value counts as one the
        // caller gave. An increment the step does not use is not checked against the period, and the period
        // setter shortens it, as the dialog does; a period the step does not use is raised to the initial
        // increment instead, so the initial increment is kept as given. An initial increment longer than the
        // maximum one is stored as it is, but setting the maximum shortens it, so the initial increment is set
        // after the maximum and a replace sets the kept one again. The incrementation type and the steady state
        // flag are applied first, so the step decides with the values the call ends with which of the given values
        // it uses. The model keeps its Direct flag in step with the incrementation type, as the dialog does.
        public static void ApplyStaticValues(StaticStep step, object timePeriod, object initialTimeIncrement,
                                             object minTimeIncrement, object maxTimeIncrement, bool replace)
        {
            EnsureUsed(step, timePeriod, "time_period");
            EnsureUsed(step, initialTimeIncrement, "initial_time_increment");
            EnsureUsed(step, minTimeIncrement, "min_time_increment");
            EnsureUsed(step, maxTimeIncrement, "max_time_increment");
            //
            double period = timePeriod == null ? step.TimePeriod : ToPositiveDouble(timePeriod, "time_period");
            double initial = initialTimeIncrement == null ? step.InitialTimeIncrement :
                             ToPositiveDouble(initialTimeIncrement, "initial_time_increment");
            double minimum = minTimeIncrement == null ? step.MinTimeIncrement :
                             ToPositiveDouble(minTimeIncrement, "min_time_increment");
            double maximum = maxTimeIncrement == null ? step.MaxTimeIncrement :
                             ToPositiveDouble(maxTimeIncrement, "max_time_increment");
            bool periodUsed = GetUnusedValueReason(step, "time_period") == null;
            bool initialUsed = GetUnusedValueReason(step, "initial_time_increment") == null;
            bool minimumUsed = GetUnusedValueReason(step, "min_time_increment") == null;
            //
            if (periodUsed && initialUsed && (replace || initialTimeIncrement != null) && initial > period)
                throw new PythonAPIException("The initial time increment " + initial + " exceeds the time period " +
                                             period + ". Give a smaller 'initial_time_increment' or a larger " +
                                             "'time_period'.");
            if (periodUsed && minimumUsed && (replace || minTimeIncrement != null) && minimum > period)
                throw new PythonAPIException("The minimum time increment " + minimum + " exceeds the time period " +
                                             period + ". Give a smaller 'min_time_increment' or a larger " +
                                             "'time_period'.");
            //
            if (timePeriod != null) step.TimePeriod = period;
            if (!periodUsed && initialUsed && initial > step.TimePeriod) step.TimePeriod = initial;
            if (maxTimeIncrement != null) step.MaxTimeIncrement = maximum;
            if (replace || initialTimeIncrement != null) step.InitialTimeIncrement = initial;
            if (minTimeIncrement != null) step.MinTimeIncrement = minimum;
            step.Direct = step.IncrementationType == IncrementationTypeEnum.Direct;
        }
        // The value of an argument is used when the solver input carries it, which is when the dialog shows it, with
        // two exceptions that follow the dialog: a heat transfer step writes Nlgeom, which its dialog hides, and the
        // dialog of a boundary displacement step shows nothing the solver input carries, since that step is not
        // written at all. The step header carries the maximum number of increments unless the incrementation type
        // is Default, and only a step of the add_static arguments shows it. Such a step writes the time period and
        // the initial increment under Automatic and Direct, and the minimum and the maximum increment only under
        // Automatic. A modal dynamics step always has the Automatic type, writes its initial increment always, its
        // time period unless it is a steady state one, its relative error only then, and no minimum or maximum
        // increment. A complex frequency step writes no solver, and a steady state dynamics step its Fourier terms
        // and time range only for nonharmonic loading. Returns why the step does not use the value of the argument,
        // or null when it does.
        public static string GetUnusedValueReason(Step step, string argumentName)
        {
            if (step is BoundaryDisplacementStep)
                return "a boundary displacement step is not written to the solver input, which takes only its " +
                       "boundary conditions. Leave it out";
            if (argumentName == "nlgeom")
                return step.GetType() == typeof(HeatTransferStep) ?
                       "a heat transfer step does not use it, and its dialog does not show it. Leave it out" : null;
            if (argumentName == "solver_type")
                return step is ComplexFrequencyStep ? "a complex frequency step does not use it. Leave it out" : null;
            //
            StaticStep staticStep = step as StaticStep;
            if (staticStep == null)
            {
                if (argumentName == "max_increments" || argumentName == "incrementation_type")
                    return "a " + StepAPI.GetItemType(step).Replace('_', ' ') + " step does not use it. Leave it out";
                //
                SteadyStateDynamicsStep steadyStateDynamicsStep = step as SteadyStateDynamicsStep;
                if (steadyStateDynamicsStep != null && steadyStateDynamicsStep.Harmonic &&
                    (argumentName == "number_of_fourier_terms" || argumentName == "time_lower" ||
                     argumentName == "time_upper"))
                    return "only a steady state dynamics step of nonharmonic loading uses it. Give harmonic=False " +
                           "with it, or leave it out";
                return null;
            }
            //
            ModalDynamicsStep modalDynamicsStep = step as ModalDynamicsStep;
            if (argumentName == "incrementation_type")
                return modalDynamicsStep != null ? "a modal dynamics step always has the incrementation type " +
                                                   "Automatic. Leave it out" : null;
            if (argumentName == "max_increments")
            {
                if (step.IncrementationType != IncrementationTypeEnum.Default) return null;
                //
                return modalDynamicsStep != null ?
                       "the incrementation type of the step is Default. Give incrementation_type='Automatic' with " +
                       "it, or leave it out" :
                       "the incrementation type of the step is Default. Give incrementation_type='Automatic' or " +
                       "'Direct' with it, or leave it out";
            }
            //
            if (modalDynamicsStep != null)
            {
                if (argumentName == "time_period" && modalDynamicsStep.SteadyState)
                    return "a steady state modal dynamics step does not use it. Give steady_state=False with it, " +
                           "or leave it out";
                if (argumentName == "relative_error" && !modalDynamicsStep.SteadyState)
                    return "only a steady state modal dynamics step uses it. Give steady_state=True with it, or " +
                           "leave it out";
                if (argumentName == "min_time_increment" || argumentName == "max_time_increment")
                    return "a modal dynamics step does not use it. Leave it out";
                return null;
            }
            //
            if (argumentName == "time_period" || argumentName == "initial_time_increment")
                return step.IncrementationType == IncrementationTypeEnum.Default ?
                       "the incrementation type of the step is Default. Give incrementation_type='Automatic' or " +
                       "'Direct' with it, or leave it out" : null;
            if (argumentName == "min_time_increment" || argumentName == "max_time_increment")
                return step.IncrementationType == IncrementationTypeEnum.Automatic ? null :
                       "the incrementation type of the step is " + step.IncrementationType + " and only Automatic " +
                       "uses it. Give incrementation_type='Automatic' with it, or leave it out";
            //
            return null;
        }
        public static void EnsureUsed(Step step, object value, string argumentName)
        {
            if (value == null) return;
            //
            string reason = GetUnusedValueReason(step, argumentName);
            if (reason != null)
                throw new PythonAPIException("The argument '" + argumentName + "' is not used: " + reason + ".");
        }
        public static void ApplyFrequencyValues(FrequencyStep step, object numOfFrequencies, object lowerFrequency,
                                                object upperFrequency, object storage,
                                                object cyclicSymmetryModeMin, object cyclicSymmetryModeMax)
        {
            if (numOfFrequencies != null)
                step.NumOfFrequencies = ToIntAtLeast(numOfFrequencies, "number_of_frequencies", 1);
            // The model stores NaN for a limit that is not set and reads an upper limit of 0 as no upper limit. Its
            // setters refuse a limit past the stored other one, so the range is checked whole and the lower limit is
            // unset while the upper one is set
            double lower = lowerFrequency == null ? step.LowerFrequency :
                           ToFrequencyLimit(lowerFrequency, "lower_frequency");
            double upper = upperFrequency == null ? step.UpperFrequency :
                           ToFrequencyLimit(upperFrequency, "upper_frequency");
            if (lowerFrequency != null || upperFrequency != null)
            {
                if (!double.IsNaN(lower) && !double.IsNaN(upper) && upper != 0 && lower >= upper)
                    throw new PythonAPIException("The lower frequency " + lower + " must be smaller than the upper " +
                                                 "frequency " + upper + ". Give a smaller 'lower_frequency' or a " +
                                                 "larger 'upper_frequency'.");
                step.LowerFrequency = double.NaN;
                step.UpperFrequency = upper;
                step.LowerFrequency = lower;
            }
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
            if (accuracy != null)
            {
                double value = PythonValueConverter.ToFiniteDouble(accuracy, "accuracy");
                if (!(value > 0)) throw new PythonAPIException("The argument 'accuracy' must be positive.");
                step.Accuracy = value;
            }
        }
        public static void ApplyModalDynamicsValues(ModalDynamicsStep step, object steadyState,
                                                    object relativeError, object modalDamping)
        {
            if (steadyState != null) step.SteadyState = PythonValueConverter.ToBool(steadyState, "steady_state");
            EnsureUsed(step, relativeError, "relative_error");
            if (relativeError != null)
                step.RelativeError = ToDoubleBetween(relativeError, "relative_error", 0, 1);
            if (modalDamping != null) step.ModalDamping = ToModalDamping(modalDamping);
        }
        // The harmonic flag is applied first, so the step decides with the flag the call ends with whether it uses
        // the Fourier terms and the time range
        public static void ApplySteadyStateDynamicsValues(SteadyStateDynamicsStep step, object harmonic,
                                                          object lowerFrequency, object upperFrequency,
                                                          object numDataPoints, object bias,
                                                          object numFourierTerms, object timeLower,
                                                          object timeUpper, object modalDamping)
        {
            if (harmonic != null) step.Harmonic = PythonValueConverter.ToBool(harmonic, "harmonic");
            EnsureUsed(step, numFourierTerms, "number_of_fourier_terms");
            EnsureUsed(step, timeLower, "time_lower");
            EnsureUsed(step, timeUpper, "time_upper");
            //
            double lower = lowerFrequency == null ? step.LowerFrequency :
                           PythonValueConverter.ToFiniteDouble(lowerFrequency, "lower_frequency");
            double upper = upperFrequency == null ? step.UpperFrequency :
                           PythonValueConverter.ToFiniteDouble(upperFrequency, "upper_frequency");
            if ((lowerFrequency != null || upperFrequency != null) && lower > upper)
                throw new PythonAPIException("The lower frequency " + lower + " exceeds the upper frequency " + upper +
                                             ". Give a smaller 'lower_frequency' or a larger 'upper_frequency'.");
            SetFrequencyRange(lowerFrequency, upperFrequency, lower, upper, step.UpperFrequency,
                              value => step.LowerFrequency = value, value => step.UpperFrequency = value);
            if (numDataPoints != null)
                step.NumDataPoints = ToIntAtLeast(numDataPoints, "number_of_data_points", 2);
            if (bias != null) step.Bias = ToDoubleAtLeast(bias, "bias", 1);
            if (numFourierTerms != null)
                step.NumFourierTerms = ToIntAtLeast(numFourierTerms, "number_of_fourier_terms", 1);
            double lowerTime = timeLower == null ? step.TimeLower :
                               PythonValueConverter.ToFiniteDouble(timeLower, "time_lower");
            double upperTime = timeUpper == null ? step.TimeUpper :
                               PythonValueConverter.ToFiniteDouble(timeUpper, "time_upper");
            if ((timeLower != null || timeUpper != null) && lowerTime > upperTime)
                throw new PythonAPIException("The lower time " + lowerTime + " exceeds the upper time " + upperTime +
                                             ". Give a smaller 'time_lower' or a larger 'time_upper'.");
            if (timeLower != null) step.TimeLower = lowerTime;
            if (timeUpper != null) step.TimeUpper = upperTime;
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
                DampingAPI converted = damping as DampingAPI;
                if (converted == null)
                    throw new PythonAPIException("The argument 'damping' must be a damping part created by " +
                                                 "pmx.create.damping.");
                step.Damping = converted.GetDampingCopy();
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
        // A value the step does not use is accepted only as the value the step carries or the one the dialog always
        // gives the step type, which changes nothing the solver input carries
        private static void EnsureUsedOrKept(Step step, object value, object storedValue, object fixedValue,
                                             string argumentName)
        {
            if (value.Equals(storedValue) || value.Equals(fixedValue)) return;
            //
            EnsureUsed(step, value, argumentName);
        }
        private static object GetFixedIncrementationType(Step step)
        {
            if (step is ModalDynamicsStep) return IncrementationTypeEnum.Automatic;
            if (step is StaticStep) return null;
            //
            return IncrementationTypeEnum.Default;
        }
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
        // The model stores the smallest integer for a value that is not set. None keeps the value on a replace, so
        // float('nan') is how a script leaves it unset, as it is for the frequency limits, and the journal writes it
        private static int ToIntAtLeastOrUnset(object value, string argumentName, int minimum)
        {
            if (value is double && double.IsNaN((double)value)) return int.MinValue;
            //
            int converted = PythonValueConverter.ToInt(value, argumentName);
            if (converted < minimum)
                throw new PythonAPIException("The argument '" + argumentName + "' must be at least " + minimum +
                                             ", or float('nan') to leave it unset.");
            //
            return converted;
        }
        private static double ToDoubleAtLeast(object value, string argumentName, double minimum)
        {
            double converted = PythonValueConverter.ToFiniteDouble(value, argumentName);
            if (!(converted >= minimum))
                throw new PythonAPIException("The argument '" + argumentName + "' must be at least " + minimum + ".");
            //
            return converted;
        }
        private static double ToFrequencyLimit(object value, string argumentName)
        {
            double converted = PythonValueConverter.ToDouble(value, argumentName);
            if (!double.IsNaN(converted) && !(converted >= 0 && !double.IsInfinity(converted)))
                throw new PythonAPIException("The argument '" + argumentName + "' must be at least 0, or " +
                                             "float('nan') for no limit.");
            //
            return converted;
        }
        private static double ToPositiveDouble(object value, string argumentName)
        {
            double converted = PythonValueConverter.ToFiniteDouble(value, argumentName);
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
            ModalDampingAPI converted = value as ModalDampingAPI;
            if (converted == null)
                throw new PythonAPIException("The argument 'modal_damping' must be a modal damping part created " +
                                             "by pmx.create.modal_damping.");
            //
            return converted.GetModalDampingCopy();
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
