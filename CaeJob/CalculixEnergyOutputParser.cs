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
    internal class CalculixEnergyOutputParser
    {
        // Variables                                                                                                                
        private int _revision;
        private int _currentStep;
        private double _latestTotalTime;
        private bool _hasLatestTotalTime;
        //
        private bool _hasCurrentRecord;
        private int _recordStep;
        private double _recordTotalTime;
        private double _initialEnergy;
        private double _externalWork;
        private double _dampingWork;
        private double _netWork;
        private double _internalEnergy;
        private double _kineticEnergy;
        private double _contactEnergy;
        private double _frictionEnergy;
        private double _totalEnergy;
        private double _energyIncrease;
        private double _absoluteBalance;
        private double _relativeBalance;
        private double _artificialMassScalingEnergy;
        private double _massScalingRelativeBalance;
        //
        private List<int> _steps;
        private List<double> _totalTimes;
        private List<double> _initialEnergies;
        private List<double> _externalWorks;
        private List<double> _dampingWorks;
        private List<double> _netWorks;
        private List<double> _internalEnergies;
        private List<double> _kineticEnergies;
        private List<double> _contactEnergies;
        private List<double> _frictionEnergies;
        private List<double> _totalEnergies;
        private List<double> _energyIncreases;
        private List<double> _absoluteBalances;
        private List<double> _relativeBalances;
        private List<double> _artificialMassScalingEnergies;
        private List<double> _massScalingRelativeBalances;


        // Constructors                                                                                                            
        public CalculixEnergyOutputParser()
        {
            _steps = new List<int>();
            _totalTimes = new List<double>();
            _initialEnergies = new List<double>();
            _externalWorks = new List<double>();
            _dampingWorks = new List<double>();
            _netWorks = new List<double>();
            _internalEnergies = new List<double>();
            _kineticEnergies = new List<double>();
            _contactEnergies = new List<double>();
            _frictionEnergies = new List<double>();
            _totalEnergies = new List<double>();
            _energyIncreases = new List<double>();
            _absoluteBalances = new List<double>();
            _relativeBalances = new List<double>();
            _artificialMassScalingEnergies = new List<double>();
            _massScalingRelativeBalances = new List<double>();
            Reset();
        }


        // Methods                                                                                                                 
        public void Reset()
        {
            _currentStep = 0;
            _latestTotalTime = double.NaN;
            _hasLatestTotalTime = false;
            ResetCurrentRecord();
            //
            _steps.Clear();
            _totalTimes.Clear();
            _initialEnergies.Clear();
            _externalWorks.Clear();
            _dampingWorks.Clear();
            _netWorks.Clear();
            _internalEnergies.Clear();
            _kineticEnergies.Clear();
            _contactEnergies.Clear();
            _frictionEnergies.Clear();
            _totalEnergies.Clear();
            _energyIncreases.Clear();
            _absoluteBalances.Clear();
            _relativeBalances.Clear();
            _artificialMassScalingEnergies.Clear();
            _massScalingRelativeBalances.Clear();
            _revision++;
        }

        public void AppendLine(string data)
        {
            if (string.IsNullOrWhiteSpace(data)) return;
            string line = data.Trim();
            //
            int step;
            if (TryParseStep(line, out step))
            {
                CommitCurrentRecord();
                _currentStep = step;
                _revision++;
                return;
            }
            //
            double value;
            if (TryParseValue(line, "actual total time", out value))
            {
                CommitCurrentRecord();
                _latestTotalTime = value;
                _hasLatestTotalTime = true;
                _revision++;
                return;
            }
            if (TryParseValue(line, "initial energy (at start of step)", out value))
            {
                CommitCurrentRecord();
                StartCurrentRecord(value);
                _revision++;
                return;
            }
            if (!_hasCurrentRecord) return;
            //
            if (TryParseValue(line, "external work", out value)) _externalWork = value;
            else if (TryParseValue(line, "work performed by the damping forces", out value)) _dampingWork = value;
            else if (TryParseValue(line, "netto work", out value)) _netWork = value;
            else if (TryParseValue(line, "internal energy", out value)) _internalEnergy = value;
            else if (TryParseValue(line, "kinetic energy", out value)) _kineticEnergy = value;
            else if (TryParseValue(line, "elastic contact energy", out value)) _contactEnergy = value;
            else if (TryParseValue(line, "energy lost due to friction", out value)) _frictionEnergy = value;
            else if (TryParseValue(line, "total energy", out value)) _totalEnergy = value;
            else if (TryParseValue(line, "energy increase", out value)) _energyIncrease = value;
            else if (TryParseValue(line, "energy balance (absolute)", out value)) _absoluteBalance = value;
            else if (TryParseValue(line, "energy balance (relative)", out value)) _relativeBalance = value;
            else if (TryParseValue(line, "artificial energy due to selective mass scaling", out value))
                _artificialMassScalingEnergy = value;
            else if (TryParseValue(line, "energy balance with mass scaling(relative)", out value))
                _massScalingRelativeBalance = value;
            else if (line.Equals("Job finished", StringComparison.OrdinalIgnoreCase))
            {
                CommitCurrentRecord();
                return;
            }
            else return;
            //
            _revision++;
        }

        public void Complete()
        {
            CommitCurrentRecord();
        }

        public CalculixEnergyData GetData()
        {
            bool includeCurrentRecord = CurrentRecordIsUsable();
            int count = _steps.Count + (includeCurrentRecord ? 1 : 0);
            int[] steps = new int[count];
            double[] totalTimes = new double[count];
            double[] initialEnergies = new double[count];
            double[] externalWorks = new double[count];
            double[] dampingWorks = new double[count];
            double[] netWorks = new double[count];
            double[] internalEnergies = new double[count];
            double[] kineticEnergies = new double[count];
            double[] contactEnergies = new double[count];
            double[] frictionEnergies = new double[count];
            double[] totalEnergies = new double[count];
            double[] energyIncreases = new double[count];
            double[] absoluteBalances = new double[count];
            double[] relativeBalances = new double[count];
            double[] artificialMassScalingEnergies = new double[count];
            double[] massScalingRelativeBalances = new double[count];
            //
            _steps.CopyTo(steps);
            _totalTimes.CopyTo(totalTimes);
            _initialEnergies.CopyTo(initialEnergies);
            _externalWorks.CopyTo(externalWorks);
            _dampingWorks.CopyTo(dampingWorks);
            _netWorks.CopyTo(netWorks);
            _internalEnergies.CopyTo(internalEnergies);
            _kineticEnergies.CopyTo(kineticEnergies);
            _contactEnergies.CopyTo(contactEnergies);
            _frictionEnergies.CopyTo(frictionEnergies);
            _totalEnergies.CopyTo(totalEnergies);
            _energyIncreases.CopyTo(energyIncreases);
            _absoluteBalances.CopyTo(absoluteBalances);
            _relativeBalances.CopyTo(relativeBalances);
            _artificialMassScalingEnergies.CopyTo(artificialMassScalingEnergies);
            _massScalingRelativeBalances.CopyTo(massScalingRelativeBalances);
            //
            if (includeCurrentRecord)
            {
                int index = count - 1;
                steps[index] = _recordStep;
                totalTimes[index] = _recordTotalTime;
                initialEnergies[index] = _initialEnergy;
                externalWorks[index] = _externalWork;
                dampingWorks[index] = _dampingWork;
                netWorks[index] = _netWork;
                internalEnergies[index] = _internalEnergy;
                kineticEnergies[index] = _kineticEnergy;
                contactEnergies[index] = _contactEnergy;
                frictionEnergies[index] = _frictionEnergy;
                totalEnergies[index] = _totalEnergy;
                energyIncreases[index] = _energyIncrease;
                absoluteBalances[index] = _absoluteBalance;
                relativeBalances[index] = _relativeBalance;
                artificialMassScalingEnergies[index] = _artificialMassScalingEnergy;
                massScalingRelativeBalances[index] = _massScalingRelativeBalance;
            }
            //
            return new CalculixEnergyData(_revision, steps, totalTimes, initialEnergies, externalWorks, dampingWorks,
                                          netWorks, internalEnergies, kineticEnergies, contactEnergies,
                                          frictionEnergies, totalEnergies, energyIncreases, absoluteBalances,
                                          relativeBalances, artificialMassScalingEnergies,
                                          massScalingRelativeBalances);
        }

        private void StartCurrentRecord(double initialEnergy)
        {
            ResetCurrentRecord();
            _hasCurrentRecord = true;
            _recordStep = _currentStep;
            _recordTotalTime = _hasLatestTotalTime ? _latestTotalTime : double.NaN;
            _initialEnergy = initialEnergy;
        }

        private void ResetCurrentRecord()
        {
            _hasCurrentRecord = false;
            _recordStep = 0;
            _recordTotalTime = double.NaN;
            _initialEnergy = double.NaN;
            _externalWork = double.NaN;
            _dampingWork = double.NaN;
            _netWork = double.NaN;
            _internalEnergy = double.NaN;
            _kineticEnergy = double.NaN;
            _contactEnergy = double.NaN;
            _frictionEnergy = double.NaN;
            _totalEnergy = double.NaN;
            _energyIncrease = double.NaN;
            _absoluteBalance = double.NaN;
            _relativeBalance = double.NaN;
            _artificialMassScalingEnergy = double.NaN;
            _massScalingRelativeBalance = double.NaN;
        }

        private void CommitCurrentRecord()
        {
            if (CurrentRecordIsUsable())
            {
                _steps.Add(_recordStep);
                _totalTimes.Add(_recordTotalTime);
                _initialEnergies.Add(_initialEnergy);
                _externalWorks.Add(_externalWork);
                _dampingWorks.Add(_dampingWork);
                _netWorks.Add(_netWork);
                _internalEnergies.Add(_internalEnergy);
                _kineticEnergies.Add(_kineticEnergy);
                _contactEnergies.Add(_contactEnergy);
                _frictionEnergies.Add(_frictionEnergy);
                _totalEnergies.Add(_totalEnergy);
                _energyIncreases.Add(_energyIncrease);
                _absoluteBalances.Add(_absoluteBalance);
                _relativeBalances.Add(_relativeBalance);
                _artificialMassScalingEnergies.Add(_artificialMassScalingEnergy);
                _massScalingRelativeBalances.Add(_massScalingRelativeBalance);
                _revision++;
            }
            ResetCurrentRecord();
        }

        private bool CurrentRecordIsUsable()
        {
            return _hasCurrentRecord && IsFinite(_recordTotalTime) && IsFinite(_internalEnergy) &&
                   IsFinite(_kineticEnergy) && IsFinite(_totalEnergy);
        }

        private static bool TryParseStep(string line, out int step)
        {
            step = 0;
            if (!line.StartsWith("STEP", StringComparison.OrdinalIgnoreCase)) return false;
            string[] fields = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            return fields.Length == 2 && int.TryParse(fields[1], NumberStyles.Integer,
                                                       CultureInfo.InvariantCulture, out step);
        }

        private static bool TryParseValue(string line, string name, out double value)
        {
            value = double.NaN;
            if (!line.StartsWith(name, StringComparison.OrdinalIgnoreCase)) return false;
            int equalsIndex = line.IndexOf('=');
            if (equalsIndex < 0) return false;
            //
            string valueText = line.Substring(equalsIndex + 1).Trim();
            int separatorIndex = valueText.IndexOfAny(new char[] { ' ', '\t', '%' });
            if (separatorIndex >= 0) valueText = valueText.Substring(0, separatorIndex);
            valueText = valueText.Replace('D', 'E').Replace('d', 'E');
            return double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
