// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace CaeJob
{
    public class CalculixEnergyData
    {
        // Properties                                                                                                               
        public int Revision { get; private set; }
        public int[] Steps { get; private set; }
        public double[] TotalTimes { get; private set; }
        public double[] InitialEnergies { get; private set; }
        public double[] ExternalWorks { get; private set; }
        public double[] DampingWorks { get; private set; }
        public double[] NetWorks { get; private set; }
        public double[] InternalEnergies { get; private set; }
        public double[] KineticEnergies { get; private set; }
        public double[] ContactEnergies { get; private set; }
        public double[] FrictionEnergies { get; private set; }
        public double[] TotalEnergies { get; private set; }
        public double[] EnergyIncreases { get; private set; }
        public double[] AbsoluteBalances { get; private set; }
        public double[] RelativeBalances { get; private set; }
        public double[] ArtificialMassScalingEnergies { get; private set; }
        public double[] MassScalingRelativeBalances { get; private set; }
        public int Count { get { return TotalTimes.Length; } }


        // Constructors                                                                                                            
        internal CalculixEnergyData(int revision, int[] steps, double[] totalTimes, double[] initialEnergies,
                                    double[] externalWorks, double[] dampingWorks, double[] netWorks,
                                    double[] internalEnergies, double[] kineticEnergies, double[] contactEnergies,
                                    double[] frictionEnergies, double[] totalEnergies, double[] energyIncreases,
                                    double[] absoluteBalances, double[] relativeBalances,
                                    double[] artificialMassScalingEnergies, double[] massScalingRelativeBalances)
        {
            Revision = revision;
            Steps = steps;
            TotalTimes = totalTimes;
            InitialEnergies = initialEnergies;
            ExternalWorks = externalWorks;
            DampingWorks = dampingWorks;
            NetWorks = netWorks;
            InternalEnergies = internalEnergies;
            KineticEnergies = kineticEnergies;
            ContactEnergies = contactEnergies;
            FrictionEnergies = frictionEnergies;
            TotalEnergies = totalEnergies;
            EnergyIncreases = energyIncreases;
            AbsoluteBalances = absoluteBalances;
            RelativeBalances = relativeBalances;
            ArtificialMassScalingEnergies = artificialMassScalingEnergies;
            MassScalingRelativeBalances = massScalingRelativeBalances;
        }
    }
}
