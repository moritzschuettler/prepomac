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
using System.Globalization;

namespace CaeJob
{
    public class CalculixStatusData
    {
        // Properties                                                                                                               
        public int[] Steps { get; private set; }
        public int[] Increments { get; private set; }
        public int[] Attempts { get; private set; }
        public double[] TotalTimes { get; private set; }
        public double[] IncrementTimes { get; private set; }
        public double[] IterationCounts { get; private set; }
        public int Count { get { return TotalTimes.Length; } }


        // Constructors                                                                                                             
        internal CalculixStatusData(int[] steps, int[] increments, int[] attempts, double[] totalTimes,
                                    double[] incrementTimes, double[] iterationCounts)
        {
            Steps = steps;
            Increments = increments;
            Attempts = attempts;
            TotalTimes = totalTimes;
            IncrementTimes = incrementTimes;
            IterationCounts = iterationCounts;
        }
    }

    public class CalculixConvergenceData
    {
        // Properties                                                                                                               
        public int[] Steps { get; private set; }
        public int[] Increments { get; private set; }
        public int[] Attempts { get; private set; }
        public int[] Iterations { get; private set; }
        public double[] RecordNumbers { get; private set; }
        public double[] ResidualForces { get; private set; }
        public double[] DisplacementCorrections { get; private set; }
        public double[] ResidualFluxes { get; private set; }
        public double[] TemperatureCorrections { get; private set; }
        public int Count { get { return RecordNumbers.Length; } }


        // Constructors                                                                                                             
        internal CalculixConvergenceData(int[] steps, int[] increments, int[] attempts, int[] iterations,
                                         double[] recordNumbers, double[] residualForces,
                                         double[] displacementCorrections, double[] residualFluxes,
                                         double[] temperatureCorrections)
        {
            Steps = steps;
            Increments = increments;
            Attempts = attempts;
            Iterations = iterations;
            RecordNumbers = recordNumbers;
            ResidualForces = residualForces;
            DisplacementCorrections = displacementCorrections;
            ResidualFluxes = residualFluxes;
            TemperatureCorrections = temperatureCorrections;
        }
    }

    public static class CalculixMonitorDataParser
    {
        // Methods                                                                                                                   
        public static CalculixStatusData ParseStatus(string data)
        {
            List<int> steps = new List<int>();
            List<int> increments = new List<int>();
            List<int> attempts = new List<int>();
            List<double> totalTimes = new List<double>();
            List<double> incrementTimes = new List<double>();
            List<double> iterationCounts = new List<double>();

            foreach (string line in GetLines(data))
            {
                string[] fields = SplitFields(line);
                if (fields.Length < 7) continue;

                int step;
                int increment;
                int attempt;
                int iterations;
                double totalTime;
                double incrementTime;

                if (!TryParseInt(fields[0], out step) ||
                    !TryParseInt(fields[1], out increment) ||
                    !TryParseInt(fields[2], out attempt) ||
                    !TryParseInt(fields[3], out iterations) ||
                    !TryParseDouble(fields[4], out totalTime) ||
                    !TryParseDouble(fields[6], out incrementTime))
                {
                    continue;
                }

                steps.Add(step);
                increments.Add(increment);
                attempts.Add(attempt);
                totalTimes.Add(totalTime);
                incrementTimes.Add(incrementTime);
                iterationCounts.Add(iterations);
            }

            return new CalculixStatusData(steps.ToArray(), increments.ToArray(), attempts.ToArray(),
                                          totalTimes.ToArray(), incrementTimes.ToArray(),
                                          iterationCounts.ToArray());
        }

        public static CalculixConvergenceData ParseConvergence(string data)
        {
            List<int> steps = new List<int>();
            List<int> increments = new List<int>();
            List<int> attempts = new List<int>();
            List<int> iterations = new List<int>();
            List<double> recordNumbers = new List<double>();
            List<double> residualForces = new List<double>();
            List<double> displacementCorrections = new List<double>();
            List<double> residualFluxes = new List<double>();
            List<double> temperatureCorrections = new List<double>();

            foreach (string line in GetLines(data))
            {
                string[] fields = SplitFields(line);
                if (fields.Length < 9) continue;

                int step;
                int increment;
                int attempt;
                int iteration;
                double residualForce;
                double displacementCorrection;
                double residualFlux;
                double temperatureCorrection;

                if (!TryParseInt(fields[0], out step) ||
                    !TryParseInt(fields[1], out increment) ||
                    !TryParseInt(fields[2], out attempt) ||
                    !TryParseInt(fields[3], out iteration) ||
                    !TryParseDouble(fields[5], out residualForce) ||
                    !TryParseDouble(fields[6], out displacementCorrection) ||
                    !TryParseDouble(fields[7], out residualFlux) ||
                    !TryParseDouble(fields[8], out temperatureCorrection))
                {
                    continue;
                }

                steps.Add(step);
                increments.Add(increment);
                attempts.Add(attempt);
                iterations.Add(iteration);
                recordNumbers.Add(recordNumbers.Count + 1);
                residualForces.Add(residualForce);
                displacementCorrections.Add(displacementCorrection);
                residualFluxes.Add(residualFlux);
                temperatureCorrections.Add(temperatureCorrection);
            }

            return new CalculixConvergenceData(steps.ToArray(), increments.ToArray(), attempts.ToArray(),
                                               iterations.ToArray(), recordNumbers.ToArray(),
                                               residualForces.ToArray(), displacementCorrections.ToArray(),
                                               residualFluxes.ToArray(), temperatureCorrections.ToArray());
        }

        private static string[] GetLines(string data)
        {
            if (string.IsNullOrEmpty(data)) return new string[0];

            return data.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string[] SplitFields(string line)
        {
            return line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool TryParseInt(string value, out int result)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        private static bool TryParseDouble(string value, out double result)
        {
            value = value.Replace('D', 'E').Replace('d', 'E');
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
    }
}
