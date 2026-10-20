// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using Octree;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CaeResults
{
    [Serializable]
    public static class DatFileReader
    {
        private static readonly string[] _spaceSplitter = new string[] { " " };
        private static readonly string[] _commaSplitter = new string[] { "," };
        private static readonly string[] _underscoreSplitter = new string[] { "_" };
        private static readonly string[] _parenthesesSplitter = new string[] { "(", ")" };
        private static readonly string[] _componentsSplitter = new string[] { " ", "," };
        private static readonly string[] _dataSplitter = new string[] { " ", "for set", "and time" };
        private static readonly string[] _signSplitter = new string[] { "-", "+" };
        private static readonly string[] _atSplitter = new string[] { "@@@" };
        private static readonly string _stepKey = "S T E P";
        private static readonly string _incrementKey = "INCREMENT";
        private static readonly string _stepNumberKey = "STEP_";
        private static readonly string _participationFactorsForFrequencyKey =
            "P A R T I C I P A T I O N   F A C T O R S   F O R   F R E Q U E N C Y";
        private const string _eigenvalueOutputKey = "E I G E N V A L U E   O U T P U T";
        private const string _participationFactorsKey = "P A R T I C I P A T I O N   F A C T O R S";
        
        private const string _effectiveModalMassKey = "E F F E C T I V E   M O D A L   M A S S";
        private const string _totalEffectiveMassKey = "T O T A L   E F F E C T I V E   M A S S";
        private const string _eigenvalueNumberKey = "E I G E N V A L U E    N U M B E R";
        private const string _bucklingFactorKey = "B U C K L I N G   F A C T O R   O U T P U T";
        // Complex frequency
        private const string _participationFactorsForModeKey = "P A R T I C I P A T I O N   F A C T O R S   F O R   M O D E";
        private const string _modalAssuranceCriteriumKey = "Modal Assurance Criterium";
        private const string _eigenmodeTurningDirectionKey = "E I G E N M O D E   T U R N I N G   D I R E C T I O N";
        // CalculiX writes these totals of an axisymmetric model for its 2 degree segment, while the loads and the forces
        // of the results file are given for the full revolution. The statistics of a contact print cover the segment, but
        // those of a section print cover 1 degree for CAX4 and CAX8R elements and 2 degrees for the other types. The
        // modes are normalized to the mass of the segment, so the participation factors scale with its square root and
        // the values of a mode with one over it, as do those of a buckling mode, normalized to the stiffness of the
        // segment. Over the revolution the moments and the radial and the out of plane
        // directions of the participation factors and the effective masses cancel, so they are left for the segment
        private const double _axisymmetricSegmentsPerRevolution = 180;
        private static readonly HashSet<string> _axisymmetricSegmentFieldNames = new HashSet<string>()
        {
            HOFieldNames.Forces,
            HOFieldNames.TotalForce,
            HOFieldNames.ContactPrintEnergy,
            HOFieldNames.ContactSpringEnergy,
            HOFieldNames.Volume,
            HOFieldNames.TotalVolume,
            HOFieldNames.InternalEnergy,
            HOFieldNames.TotalInternalEnergy,
            HOFieldNames.KineticEnergy,
            HOFieldNames.TotalKineticEnergy,
            HOFieldNames.HeatGeneration,
            HOFieldNames.TotalHeatGeneration,
            HOFieldNames.BodyHeating,
            HOFieldNames.TotalBodyHeating
        };
        private static readonly Dictionary<string, double> _axisymmetricSectionPrintSegmentsPerRevolution =
            new Dictionary<string, double>()
        {
            { FeElementTypeLinearTria.CAX3.ToString(), 180 },
            { FeElementTypeParabolicTria.CAX6.ToString(), 180 },
            { FeElementTypeLinearQuad.CAX4.ToString(), 360 },
            { FeElementTypeLinearQuad.CAX4R.ToString(), 180 },
            { FeElementTypeParabolicQuad.CAX8.ToString(), 180 },
            { FeElementTypeParabolicQuad.CAX8R.ToString(), 360 }
        };
        private static readonly HashSet<string> _axisymmetricSegmentStatisticsFieldNames = new HashSet<string>()
        {
            HOFieldNames.TotalSurfaceForce,
            HOFieldNames.SurfaceArea,
            HOFieldNames.SurfaceLoads
        };
        private static readonly HashSet<string> _axisymmetricMomentComponentNames = new HashSet<string>()
        {
            HOComponentNames.TORQUE,
            HOComponentNames.BENDING_MOMENT
        };
        private static readonly HashSet<string> _axisymmetricModeFieldNames = new HashSet<string>()
        {
            HOFieldNames.Displacements,
            HOFieldNames.Forces,
            HOFieldNames.TotalForce,
            HOFieldNames.Stresses,
            HOFieldNames.Strains,
            HOFieldNames.MechanicalStrains,
            HOFieldNames.RelativeContactDisplacement,
            HOFieldNames.ContactStress,
            HOFieldNames.TotalSurfaceForce,
            HOFieldNames.MomentAboutOrigin,
            HOFieldNames.MomentAboutCG,
            HOFieldNames.SurfaceLoads
        };
        private static readonly HashSet<string> _axisymmetricModeEnergyFieldNames = new HashSet<string>()
        {
            HOFieldNames.InternalEnergyDensity,
            HOFieldNames.InternalEnergy,
            HOFieldNames.TotalInternalEnergy,
            HOFieldNames.KineticEnergy,
            HOFieldNames.TotalKineticEnergy,
            HOFieldNames.ContactPrintEnergy,
            HOFieldNames.ContactSpringEnergy
        };
        // CalculiX prints a value for every contact element of a slave face. The energies of the face are their sum,
        // while its displacements and stresses are their average
        private static readonly HashSet<string> _contactFaceSumFieldNames = new HashSet<string>()
        {
            HOFieldNames.ContactPrintEnergy,
            HOFieldNames.ContactSpringEnergy
        };
        //
        private static readonly Dictionary<string, string> _compMapRP = new Dictionary<string, string>()
        {
            { "Id", "Id" },
            //
            { FOComponentNames.U1, "UR1" },
            { FOComponentNames.U2, "UR2" },
            { FOComponentNames.U3, "UR3" },
            //
            { "RF1", "RM1" },
            { "RF2", "RM2" },
            { "RF3", "RM3" },
            //
            { FOComponentNames.V1, "VR1" },
            { FOComponentNames.V2, "VR2" },
            { FOComponentNames.V3, "VR3" },
            //
            { "T", "T" }
        };
        private static readonly Dictionary<string, string> nameKeyMap = new Dictionary<string, string>()
        {
            // Nodal                                                                
            { HOFieldNames.Displacements, HOFieldNames.Displacements.Replace('_', ' ')},
            { HOFieldNames.Velocities, HOFieldNames.Velocities.Replace('_', ' ')},
            { HOFieldNames.Forces, HOFieldNames.Forces.Replace('_', ' ')},
            { HOFieldNames.TotalForce, HOFieldNames.TotalForce.Replace('_', ' ')},
            { HOFieldNames.Stresses, HOFieldNames.Stresses.Replace('_', ' ')},
            { HOFieldNames.Strains, HOFieldNames.Strains.Replace('_', ' ')},
            { HOFieldNames.MechanicalStrains, HOFieldNames.MechanicalStrains.Replace('_', ' ')},
            { HOFieldNames.EquivalentPlasticStrains, HOFieldNames.EquivalentPlasticStrains.Replace('_', ' ')},
            { HOFieldNames.InternalEnergyDensity, HOFieldNames.InternalEnergyDensity.Replace('_', ' ')},
            // Thermal
            { HOFieldNames.Temperatures, HOFieldNames.Temperatures.Replace('_', ' ')},
            { HOFieldNames.HeatGeneration, HOFieldNames.HeatGeneration.Replace('_', ' ')},
            { HOFieldNames.TotalHeatGeneration, HOFieldNames.TotalHeatGeneration.Replace('_', ' ')},
            // Frequency                                                            
            { _stepKey, _stepKey},
            { _incrementKey, _incrementKey},
            { _eigenvalueOutputKey, _eigenvalueOutputKey},
            { _participationFactorsKey, _participationFactorsKey},
            { _effectiveModalMassKey, _effectiveModalMassKey},
            { _totalEffectiveMassKey, _totalEffectiveMassKey},
            { _eigenvalueNumberKey, _eigenvalueNumberKey},
            { _bucklingFactorKey, _bucklingFactorKey},
            // Steady state                                                         
            { _participationFactorsForFrequencyKey, _participationFactorsForFrequencyKey},
            // Complex frequency                                                    
            { _participationFactorsForModeKey, _participationFactorsForModeKey},
            { _modalAssuranceCriteriumKey, _modalAssuranceCriteriumKey},
            { _eigenmodeTurningDirectionKey, _eigenmodeTurningDirectionKey},
            // Contact                                                              
            { HOFieldNames.RelativeContactDisplacement, HOFieldNames.RelativeContactDisplacement.Replace('_', ' ')},
            { HOFieldNames.ContactStress, HOFieldNames.ContactStress.Replace('_', ' ')},
            { HOFieldNames.ContactPrintEnergy, HOFieldNames.ContactPrintEnergy.Replace('_', ' ')},
            { HOFieldNames.ContactSpringEnergy, HOFieldNames.ContactSpringEnergy.Replace('_', ' ')},
            { HOFieldNames.TotalNumberOfContactElements, HOFieldNames.TotalNumberOfContactElements.Replace('_', ' ')},
            // Set                                                                  
            { HOFieldNames.StatisticsForSlaveSet, HOFieldNames.StatisticsForSlaveSet.Replace('_', ' ')},
            { HOFieldNames.StatisticsForSurfaceSet, HOFieldNames.StatisticsForSurfaceSet.Replace('_', ' ')},
            //
            { HOFieldNames.TotalSurfaceForce, HOFieldNames.TotalSurfaceForce.Replace('_', ' ')},
            { HOFieldNames.MomentAboutOrigin, HOFieldNames.MomentAboutOrigin.Replace('_', ' ')},
            { HOFieldNames.CenterOfGravityCG, HOFieldNames.CenterOfGravityCG.Replace('_', ' ')},
            { HOFieldNames.MeanSurfaceNormal, HOFieldNames.MeanSurfaceNormal.Replace('_', ' ')},
            { HOFieldNames.MomentAboutCG, HOFieldNames.MomentAboutCG.Replace('_', ' ')},
            { HOFieldNames.SurfaceArea, HOFieldNames.SurfaceArea.Replace('_', ' ')},
            { HOFieldNames.SurfaceLoads, HOFieldNames.SurfaceLoads.Replace('_', ' ')},
            // Element                                                              
            { HOFieldNames.Volume, HOFieldNames.Volume.Replace('_', ' ')},
            { HOFieldNames.TotalVolume, HOFieldNames.TotalVolume.Replace('_', ' ')},
            { HOFieldNames.InternalEnergy, HOFieldNames.InternalEnergy.Replace('_', ' ')},
            { HOFieldNames.TotalInternalEnergy, HOFieldNames.TotalInternalEnergy.Replace('_', ' ')},
            { HOFieldNames.KineticEnergy, HOFieldNames.KineticEnergy.Replace('_', ' ')},
            { HOFieldNames.TotalKineticEnergy, HOFieldNames.TotalKineticEnergy.Replace('_', ' ')},
            // Thermal                                                              
            { HOFieldNames.HeatFlux, HOFieldNames.HeatFlux.Replace('_', ' ')},
            { HOFieldNames.BodyHeating, HOFieldNames.BodyHeating.Replace('_', ' ')},
            { HOFieldNames.TotalBodyHeating, HOFieldNames.TotalBodyHeating.Replace('_', ' ')},
        };

        // Methods                                                                                                                  
        static public HistoryResults Read(string fileName, IEnumerable<string> fullNames, bool axisymmetric,
                                          FeMesh axisymmetricModelMesh, out string[] errors,
                                          out string[] unscaledSectionMessages)
        {
            errors = null;
            unscaledSectionMessages = null;
            List<string> errorsList = new List<string>();
            //
            if (fileName != null && File.Exists(fileName))
            {
                string[] lines = Tools.ReadAllLines(fileName, true);
                //
                Dictionary<string, string> hashSuffixFullNames = CalculixNameShortener.GetHashSuffixFullNames(fullNames);
                Dictionary<string, string> repairedSetNames = new Dictionary<string, string>();
                //
                bool steadyStateDynamics;
                IEnumerable<string[]> dataSetLinesList = SplitToDataSetLinesList(lines, repairedSetNames, out steadyStateDynamics);
                Repair(dataSetLinesList);
                //
                int len;
                int stepId = -1;
                int nodalDiameter = -1;
                int incrementId = -1;
                int eigenvalueNumber = -1;
                string tmp;
                string stepKey = null;
                DatDataSet newDataSet;
                DatDataSet existingDataSet;
                Dictionary<double, double> timeFrequency = null;
                Dictionary<double, double> timeBucklingFactor = null;
                CaeGlobals.OrderedDictionary<string, DatDataSet> dataSets = new CaeGlobals.OrderedDictionary<string, DatDataSet>("DataSets");
                //
                HistoryResults allHistoryResults = new HistoryResults("HistoryOutput");
                HistoryResults frequencyHistoryOutput = new HistoryResults("HistoryOutput");
                HistoryResults bucklingHistoryOutput = new HistoryResults("HistoryOutput");
                foreach (string[] dataSetLines in dataSetLinesList)
                {
                    try
                    {
                        // Frequency
                        if (dataSetLines[0] == _eigenvalueOutputKey ||
                            dataSetLines[0] == _participationFactorsKey ||
                            dataSetLines[0] == _effectiveModalMassKey ||
                            dataSetLines[0] == _totalEffectiveMassKey ||
                            // Steady state
                            dataSetLines[0].ToUpper().StartsWith(_participationFactorsForFrequencyKey) ||
                            // Complex frequency
                            dataSetLines[0].ToUpper().StartsWith(_participationFactorsForModeKey) ||
                            dataSetLines[0] == _modalAssuranceCriteriumKey)
                        {
                            if (dataSetLines[0] == _eigenvalueOutputKey)
                            {
                                nodalDiameter = GetCyclicNodalDiameter(dataSetLines);
                                stepKey = GetFrequencyHistorySetName(stepId, nodalDiameter);
                                timeFrequency = null;
                            }
                            else if (stepKey == null) stepKey = GetFrequencyHistorySetName(stepId, nodalDiameter);
                            frequencyHistoryOutput.AppendSets(GetFrequencyDatDataSets(dataSetLines, stepId, nodalDiameter,
                                                                                     axisymmetric));
                        }
                        else if (dataSetLines[0] == _eigenmodeTurningDirectionKey)
                        {
                            if (stepKey == null) stepKey = GetFrequencyHistorySetName(stepId, nodalDiameter);
                            frequencyHistoryOutput.AppendSets(GetComplexFrequencyTurningDirection(dataSetLines, stepId,
                                                                                                  nodalDiameter));
                        }
                        // Buckling
                        else if (dataSetLines[0] == _bucklingFactorKey)
                        {
                            if (stepKey == null) stepKey = _stepNumberKey + stepId;
                            bucklingHistoryOutput.AppendSets(GetBucklingDatDataSets(dataSetLines, stepId));
                        }
                        // Save the step id
                        else if (dataSetLines[0].StartsWith(_stepKey))
                        {
                            len = _stepKey.Length;
                            tmp = dataSetLines[0].Substring(len, dataSetLines[0].Length - len);
                            stepId = int.Parse(tmp);
                            nodalDiameter = -1;
                            stepKey = null;
                            timeFrequency = null;
                            timeBucklingFactor = null;
                            //
                            eigenvalueNumber = -1;  // reset the eigenvalue number
                        }
                        // Save the increment id
                        else if (dataSetLines[0].StartsWith(_incrementKey))
                        {
                            len = _incrementKey.Length;
                            tmp = dataSetLines[0].Substring(len, dataSetLines[0].Length - len);
                            incrementId = int.Parse(tmp);
                        }
                        else
                        {
                            // Save the eigenvalue number
                            if (dataSetLines.Length == 1 && dataSetLines[0].StartsWith(_eigenvalueNumberKey))
                            {
                                len = _eigenvalueNumberKey.Length;
                                tmp = dataSetLines[0].Substring(len, dataSetLines[0].Length - len);
                                eigenvalueNumber = int.Parse(tmp);
                            }
                            else
                            {
                                // Read the dataset
                                newDataSet = GetDatDataSet(dataSetLines, repairedSetNames, hashSuffixFullNames);
                                if (stepId != -1) newDataSet.StepId = stepId;
                                if (incrementId != -1) newDataSet.IncrementId = incrementId;
                                if (newDataSet.SetName == _participationFactorsForFrequencyKey) continue;
                                // Steady state dynamics
                                if (steadyStateDynamics && dataSets.TryGetValue(newDataSet.GetHashKey(), out existingDataSet))
                                {
                                    existingDataSet.FieldName += HOFieldNames.ComplexRealSuffix;
                                    dataSets.Replace(newDataSet.GetHashKey(), existingDataSet.GetHashKey(), existingDataSet);
                                    //
                                    newDataSet.FieldName += HOFieldNames.ComplexImaginarySuffix;
                                }
                                // Inside an eigenvalue number block CalculiX prints the total time, the same for every
                                // mode, so the time is changed to the frequency or the buckling factor of the block, as
                                // the results file gives it
                                if (eigenvalueNumber != -1 && stepKey != null)
                                {
                                    double modeTime;
                                    if (timeFrequency == null)
                                        timeFrequency = GetTimeFrequencyPairs(frequencyHistoryOutput, stepKey);
                                    if (timeBucklingFactor == null)
                                        timeBucklingFactor = GetTimeBucklingFactorPairs(bucklingHistoryOutput, stepKey);
                                    //
                                    if (timeFrequency != null && timeFrequency.TryGetValue(eigenvalueNumber, out modeTime))
                                    {
                                        newDataSet.Mode = true;
                                        newDataSet.Time = modeTime;
                                    }
                                    else if (timeBucklingFactor != null &&
                                             timeBucklingFactor.TryGetValue(eigenvalueNumber, out modeTime))
                                    {
                                        newDataSet.Mode = true;
                                        newDataSet.Time = modeTime;
                                    }
                                }
                                // Add
                                if (newDataSet.FieldName != HOFieldNames.Error)
                                {
                                    if (!dataSets.ContainsKey(newDataSet.GetHashKey()))
                                        dataSets.Add(newDataSet.GetHashKey(), newDataSet);
                                    else
                                        errorsList.Add("Data sets already contains the item key " + newDataSet.GetHashKey() +
                                                       " the data was not be added to the history.");
                                }
                                else
                                {
                                    errorsList.Add("Data failed to be parsed." + Environment.NewLine +
                                                   "First data line: " + dataSetLines[0]);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errorsList.Add(ex.Message);
                    }
                }
                //
                errors = errorsList.ToArray();
                //
                AddTotalEffectiveModalMassToMassRatio(frequencyHistoryOutput);
                MergeCyclicFrequencyHistory(frequencyHistoryOutput);
                allHistoryResults.AppendSets(frequencyHistoryOutput);
                allHistoryResults.AppendSets(bucklingHistoryOutput);
                //
                List<string> unscaledSectionMessagesList = new List<string>();
                HistoryResults historyOutput = GetHistoryOutput(dataSets.Values, hashSuffixFullNames, axisymmetric,
                                                                axisymmetricModelMesh, unscaledSectionMessagesList);
                unscaledSectionMessages = unscaledSectionMessagesList.ToArray();
                AddComplexFields(historyOutput);
                AddStressComponents(historyOutput);
                AddStrainComponents(historyOutput);
                //
                allHistoryResults.AppendSets(historyOutput);
                return allHistoryResults;
            }
            //
            return null;
        }
        static private IEnumerable<string[]> SplitToDataSetLinesList(string[] lines, Dictionary<string, string> repairedSetNames,
                                                                     out bool steadyStateDynamics)
        {
            // displacements (vx, vy, vz) for set DISP and time  0.1000000E+00
            //         5   5.080202E+00  0.000000E+00  0.000000E+00
            // forces (fx, fy, fz) for set FORCE and time  0.1000000E+00
            //         69 -2.678823E-05  6.029691E+01  1.673592E+01
            //         47  7.091853E-05  1.246206E+01  3.973111E+00
            //         48 -1.268813E-04 -5.285929E+01 -1.336375E+01
            // stresses (elem, integ.pnt.,sxx,syy,szz,sxy,sxz,syz) for set ELEMENTSET-1 and time  0.1000000E+01
            //         2030   1 -1.824212E-11 -1.000000E-01 -1.062898E-10 -6.614340E-11 -5.298272E-10  3.899325E-10
            //         2030   2 -2.853204E-09 -1.000000E-01 -3.195191E-11  1.074865E-09 -1.638492E-10 -3.539078E-10
            Dictionary<string, HashSet<string>> existingNames = new Dictionary<string, HashSet<string>>();
            List<string> dataSet;
            List<string[]> dataSets = new List<string[]>();
            //
            string theName;
            steadyStateDynamics = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length == 0) continue;
                //
                theName = null;
                foreach (var entry in nameKeyMap)
                {
                    if (lines[i].ToUpper().StartsWith(entry.Value.ToUpper()))
                    {
                        theName = entry.Key;
                        break;
                    }
                }
                // Frequency
                if (theName == _eigenvalueOutputKey ||
                    theName == _participationFactorsKey ||
                    theName == _effectiveModalMassKey ||
                    theName == _totalEffectiveMassKey ||
                    // Steady state
                    theName == _participationFactorsForFrequencyKey ||
                    // Complex
                    theName == _participationFactorsForModeKey ||
                    theName == _modalAssuranceCriteriumKey)
                {
                    if (!steadyStateDynamics && lines[i].ToUpper().StartsWith(_participationFactorsForFrequencyKey))
                        steadyStateDynamics = true;
                    //
                    int emptyLinesCounter = 0;
                    dataSet = new List<string> { lines[i] };
                    i++;
                    //
                    while (i < lines.Length)
                    {
                        if (lines[i].Length == 0) emptyLinesCounter++;
                        else dataSet.Add(lines[i]);
                        if (emptyLinesCounter == 3) break;  // must be here to prevent i++
                        i++;
                    }
                    //
                    dataSets.Add(dataSet.ToArray());
                }
                // Complex frequency
                else if (theName == _eigenmodeTurningDirectionKey)
                {
                    int emptyLinesCounter = 0;
                    dataSet = new List<string> { lines[i] };
                    i++;
                    //
                    while (i < lines.Length && emptyLinesCounter <= 4)
                    {
                        bool nextDataSet = nameKeyMap.Values.Any(
                            key => lines[i].StartsWith(key, StringComparison.OrdinalIgnoreCase));
                        if (nextDataSet)
                        {
                            i--;
                            break;
                        }
                        if (lines[i].Length == 0) emptyLinesCounter++;
                        else dataSet.Add(lines[i]);
                        i++;
                    }
                    //
                    dataSets.Add(dataSet.ToArray());
                }
                // Buckling
                else if (theName == _bucklingFactorKey)
                {
                    int emptyLinesCounter = 0;
                    dataSet = new List<string> { lines[i] };
                    i++;
                    //
                    while (i < lines.Length)
                    {
                        if (lines[i].Length == 0) emptyLinesCounter++;
                        else dataSet.Add(lines[i]);
                        if (emptyLinesCounter == 3) break;
                        //
                        i++;
                    }
                    //
                    dataSets.Add(dataSet.ToArray());
                }
                // Contact statistics
                else if (theName == HOFieldNames.StatisticsForSlaveSet)
                {
                    dataSet = new List<string>();
                    for (int j = 0; j <= 8 && i < lines.Length; j++)
                    {
                        if (lines[i].Length != 0) dataSet.Add(lines[i]);
                        i+=2;
                    }
                    // At the end reduce the i for 1 since it will be increased next time in the loop
                    i--;
                    //
                    IEnumerable<string[]> repairedSets = RepairContactStatistics(dataSet.ToArray(), ref existingNames, repairedSetNames);
                    //
                    if (repairedSets != null) dataSets.AddRange(repairedSets);
                }
                // Section statistics
                else if (theName == HOFieldNames.StatisticsForSurfaceSet)
                {
                    dataSet = new List<string>();
                    for (int j = 0; j <= 8 && i < lines.Length; j++)
                    {
                        if (lines[i].Length != 0) dataSet.Add(lines[i]);
                        i += 2;
                    }
                    // At the end reduce the i for 1 since it will be increased next time in the loop
                    i--;
                    IEnumerable<string[]> repairedSets = RepairSectionStatistics(dataSet.ToArray(), ref existingNames, repairedSetNames);
                    //
                    if (repairedSets != null) dataSets.AddRange(repairedSets);
                }
                else if (theName != null)
                {
                    dataSet = new List<string> { lines[i] };
                    i++;
                    //
                    if (theName == _stepKey || theName == _incrementKey || theName == _eigenvalueNumberKey) { }
                    else
                    {
                        while (i < lines.Length && lines[i].Length == 0) i++;    // skip empty lines
                        //
                        while (i < lines.Length)
                        {
                            if (lines[i].Length == 0 || lines[i].Contains("time")) break;    // last line is empty
                            else dataSet.Add(lines[i]);
                            //
                            i++;
                        }
                        //
                    }
                    dataSets.Add(dataSet.ToArray());
                }
            }
            //
            return dataSets;
        }
        static private List<string[]> RepairContactStatistics(string[] lines, ref Dictionary<string, HashSet<string>> existingNames,
                                                              Dictionary<string, string> repairedSetNames)
        {            
            //statistics for slave set INTERNAL_SELECTION - 2_CONTACTPAIR - 1_SLAVE, master set INTERNAL_SELECTION - 1_CONTACTPAIR - 1_MASTER and time  0.5000000E+00
            //total surface force (fx, fy, fz) and moment about the origin (mx, my, mz)
            //3.821561E+02  1.814803E+02 - 5.224860E+03  4.075417E+05  3.873634E+03  2.996193E+04
            //center of gravity and mean normal
            //- 6.862848E+00 - 8.165902E+01  1.040585E+02  0.000000E+00  0.000000E+00  1.000000E+00
            //moment about the center of gravity(mx, my, mz)
            //- 2.306952E+02 - 3.552590E+01  9.056343E-01
            //area, normal force(+ = tension) and shear force(size)
            //7.729843E+03 - 5.224860E+03  4.230584E+02
            //
            string[] dataSet;
            List<string[]> repairedDataSets = new List<string[]>();
            //
            if (lines.Length != 9) return repairedDataSets;
            //
            string[] tmp = lines[0].Split(new string[] { "slave set", "master set", "and time"}, 
                                          StringSplitOptions.RemoveEmptyEntries);
            string slaveName = tmp[1].Trim(new char[] { ' ', ','});
            string masterName = tmp[2].Trim();
            string time = tmp[3].Trim();
            //
            string name = slaveName + _atSplitter[0] + masterName;
            //
            HashSet<string> existingNamesAtTime;
            if (existingNames.TryGetValue(time, out existingNamesAtTime))
            {
                if (existingNamesAtTime.Contains(name)) return null;
            }
            else
            {
                existingNames.Add(time, new HashSet<string>() { name });
            }
            // Total surface force
            tmp = lines[2].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
            dataSet = new string[2];
            dataSet[0] = "Total surface force (FX, FY, FZ) for set " + name + " and time " + time;
            dataSet[1] = string.Format("{0} {1} {2}", tmp[0], tmp[1], tmp[2]);
            repairedDataSets.Add(dataSet);
            // Moment about origin
            dataSet = new string[2];
            dataSet[0] = "Moment about origin (MX, MY, MZ) for set " + name + " and time " + time;
            dataSet[1] = string.Format("{0} {1} {2}", tmp[3], tmp[4], tmp[5]);
            repairedDataSets.Add(dataSet);
            // Center of gravity CG
            tmp = lines[4].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
            dataSet = new string[2];
            dataSet[0] = "Center of gravity CG (X, Y, Z) for set " + name + " and time " + time;
            dataSet[1] = string.Format("{0} {1} {2}", tmp[0], tmp[1], tmp[2]);
            repairedDataSets.Add(dataSet);
            // Mean normal
            dataSet = new string[2];
            dataSet[0] = "Mean surface normal (NX, NY, NZ) for set " + name + " and time " + time;
            dataSet[1] = string.Format("{0} {1} {2}", tmp[3], tmp[4], tmp[5]);
            repairedDataSets.Add(dataSet);
            // Moment about CG
            tmp = lines[6].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
            dataSet = new string[2];
            dataSet[0] = "Moment about CG (MX, MY, MZ) for set " + name + " and time " + time;
            dataSet[1] = string.Format("{0} {1} {2}", tmp[0], tmp[1], tmp[2]);
            repairedDataSets.Add(dataSet);
            // Area
            tmp = lines[8].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
            dataSet = new string[2];
            dataSet[0] = "Surface area (A) for set " + name + " and time " + time;
            dataSet[1] = tmp[0];
            repairedDataSets.Add(dataSet);
            // Surface load
            dataSet = new string[2];
            dataSet[0] = string.Format("Surface loads ({0}, {1}) for set {2}  and time {3}",
                                        HOComponentNames.NORMAL_FORCE,
                                        HOComponentNames.SHEAR_FORCE,
                                        name, time);
            dataSet[1] = string.Format("{0} {1}", tmp[1], tmp[2]);
            repairedDataSets.Add(dataSet);
            //
            return repairedDataSets;
        }
        static private List<string[]> RepairSectionStatistics(string[] lines, ref Dictionary<string, HashSet<string>> existingNames,
                                                              Dictionary<string, string> repairedSetNames)
        {
            // statistics for surface set SURFACE-1 and time  0.1000000E+00
            //   total surface force(fx, fy, fz) and moment about the origin(mx, my, mz)
            //   - 2.667946E+02 - 1.644923E-01  5.003642E+00  4.311920E+01  7.424729E+01  2.009665E+02
            //   center of gravity and mean normal
            //   - 4.802744E+00  2.625216E-04  4.805098E-05  4.819484E-01  3.554706E-17 - 6.629240E-19
            //   moment about the center of gravity(mx, my, mz)
            //    4.311787E+01  5.022890E+01  2.001065E+02
            //   area, normal force(+ = tension), shear force(size), torque and bending moment(size)
            //    3.872828E+02 - 1.285813E+02  2.048863E+02  2.078059E+01  2.089530E+02
            //
            string[] dataSet;
            List<string[]> repairedDataSets = new List<string[]>();
            //
            if (lines.Length != 9) return repairedDataSets;
            //
            string[] tmp = lines[0].Split(new string[] { "surface set", "and time" },
                                          StringSplitOptions.RemoveEmptyEntries);
            string name = tmp[1].Trim();
            string time = tmp[2].Trim();
            //
            HashSet<string> existingNamesAtTime;
            if (existingNames.TryGetValue(time, out existingNamesAtTime))
            {
                if (existingNamesAtTime.Contains(name)) return null;
            }
            else
            {
                existingNames.Add(time, new HashSet<string>() { name });
            }
            // Total surface force
            tmp = lines[2].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
            dataSet = new string[2];
            dataSet[0] = "Total surface force (FX, FY, FZ) for set " + name + " and time " + time;
            dataSet[1] = string.Format("{0} {1} {2}", tmp[0], tmp[1], tmp[2]);
            repairedDataSets.Add(dataSet);
            // Moment about origin
            dataSet = new string[2];
            dataSet[0] = "Moment about origin (MX, MY, MZ) for set " + name + " and time " + time;
            dataSet[1] = string.Format("{0} {1} {2}", tmp[3], tmp[4], tmp[5]);
            repairedDataSets.Add(dataSet);
            // Center of gravity CG
            tmp = lines[4].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
            dataSet = new string[2];
            dataSet[0] = "Center of gravity CG (X, Y, Z) for set " + name + " and time " + time;
            dataSet[1] = string.Format("{0} {1} {2}", tmp[0], tmp[1], tmp[2]);
            repairedDataSets.Add(dataSet);
            // Mean normal
            dataSet = new string[2];
            dataSet[0] = "Mean surface normal (NX, NY, NZ) for set " + name + " and time " + time;
            dataSet[1] = string.Format("{0} {1} {2}", tmp[3], tmp[4], tmp[5]);
            repairedDataSets.Add(dataSet);
            // Moment about CG
            tmp = lines[6].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
            dataSet = new string[2];
            dataSet[0] = "Moment about CG (MX, MY, MZ) for set " + name + " and time " + time;
            dataSet[1] = string.Format("{0} {1} {2}", tmp[0], tmp[1], tmp[2]);
            repairedDataSets.Add(dataSet);
            // Area
            tmp = lines[8].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
            dataSet = new string[2];
            dataSet[0] = "Surface area (A) for set " + name + " and time " + time;
            dataSet[1] = tmp[0];
            repairedDataSets.Add(dataSet);
            // Surface load
            dataSet = new string[2];
            dataSet[0] = string.Format("Surface loads ({0}, {1}, {2}, {3}) for set {4}  and time {5}",
                                        HOComponentNames.NORMAL_FORCE,
                                        HOComponentNames.SHEAR_FORCE,
                                        HOComponentNames.TORQUE,
                                        HOComponentNames.BENDING_MOMENT,
                                        name, time);
            dataSet[1] = string.Format("{0} {1} {2} {3}", tmp[1], tmp[2], tmp[3], tmp[4]);
            repairedDataSets.Add(dataSet);
            //
            return repairedDataSets;
        }
        static private void Repair(IEnumerable<string[]> dataSetLinesList)
        {
            foreach (var lines in dataSetLinesList)
            {
                if (lines.Length > 0)
                {
                    foreach (var entry in nameKeyMap)
                    {
                        if (lines[0].ToUpper().StartsWith(entry.Value.ToUpper()))
                        {
                            if (entry.Key == HOFieldNames.Displacements)
                            {
                                //displacements (vx,vy,vz) for set NODESET-1 and time  0.1000000E+01
                                //       310 -2.462709E-03 -6.331758E-04 -4.384750E-05
                                lines[0] = lines[0].Replace("(vx,vy,vz)", "(Id,U1,U2,U3)");
                            }
                            else if (entry.Key == HOFieldNames.Velocities)
                            {
                                //velocities (vx,vy,vz) for set INTERNAL_SELECTION-1_NH_OUTPUT-1 and time  0.8102399E-05
                                //       40  1.162032E-17 -7.948453E-02 -8.204195E-18
                                lines[0] = lines[0].Replace("(vx,vy,vz)", "(Id,V1,V2,V3)");
                            }
                            else if (entry.Key == HOFieldNames.Forces)
                            {
                                //forces (fx,fy,fz) for set NODESET-1 and time  0.1000000E+01
                                //       310 -2.582430E-13 -1.013333E-01  6.199805E-14
                                lines[0] = lines[0].Replace("(fx,fy,fz)", "(Id,RF1,RF2,RF3)");
                            }
                            else if (entry.Key == HOFieldNames.TotalForce)
                            {
                                //total force (fx,fy,fz) for set NODESET-1 and time  0.1000000E+01
                                //       -5.868470E-13 -1.000000E+00 -1.028019E-13
                                lines[0] = lines[0].Replace("(fx,fy,fz)", "(RF1,RF2,RF3)");
                            }
                            else if (entry.Key == HOFieldNames.Stresses)
                            {
                                //stresses (elem, integ.pnt.,sxx,syy,szz,sxy,sxz,syz) for set SOLID_PART-1 and time  0.1000000E+01
                                //      1655   1  1.186531E-02 -3.997792E-02 -3.119545E-03  1.104426E-02  2.740127E-03 -9.467634E-03
                                lines[0] = lines[0].Replace("(elem, integ.pnt.,sxx,syy,szz,sxy,sxz,syz)",
                                                            "(Id,Int.Pnt.,S11,S22,S33,S12,S13,S23)");
                                RepairShellStressLines(lines);
                            }
                            else if (entry.Key == HOFieldNames.Strains)
                            {
                                //strains (elem, integ.pnt.,exx,eyy,ezz,exy,exz,eyz) forset SOLID_PART-1 and time  0.1000000E+01
                                //      1655   1  1.180693E-07 -2.028650E-07  2.530589E-08  6.836925E-08  1.696269E-08 -5.860917E-08
                                lines[0] = lines[0].Replace("(elem, integ.pnt.,exx,eyy,ezz,exy,exz,eyz)",
                                                            "(Id,Int.Pnt.,E11,E22,E33,E12,E13,E23)");
                                lines[0] = lines[0].Replace(" forset ", " for set ");
                            }
                            else if (entry.Key == HOFieldNames.MechanicalStrains)
                            {
                                // mechanical strains (elem, integ.pnt.,exx,eyy,ezz,exy,exz,eyz) forset ELEMENTSET-1 and time  0.1000000E+01
                                //      2030   1  1.428572E-07 -4.761905E-07  1.428571E-07 -1.951987E-14  2.100533E-15  1.625910E-15
                                lines[0] = lines[0].Replace("(elem, integ.pnt.,exx,eyy,ezz,exy,exz,eyz)",
                                                            "(Id,Int.Pnt.,E11,E22,E33,E12,E13,E23)");
                                lines[0] = lines[0].Replace(" forset ", " for set ");
                            }
                            else if (entry.Key == HOFieldNames.EquivalentPlasticStrains)
                            {
                                // equivalent plastic strain (elem, integ.pnt.,pe)for set ELEMENTSET-1 and time  0.6250000E-01
                                //      1682   1  0.000000E+00
                                lines[0] = lines[0].Replace("(elem, integ.pnt.,pe)",
                                                            "(Id,Int.Pnt.,PEEQ)");
                                lines[0] = lines[0].Replace(")for set ", ") for set ");
                            }
                            // Thermal                                                                                              
                            else if (entry.Key == HOFieldNames.Temperatures)
                            {
                                // temperatures for set INTERNAL_SELECTION-1_NH_OUTPUT-1 and time  0.1000000E+01
                                //      3450  1.000000E+02
                                lines[0] = lines[0].Replace("temperatures for set", "temperatures (Id,T) for set");
                            }
                            else if (entry.Key == HOFieldNames.HeatGeneration)
                            {
                                // heat generation for set INTERNAL_SELECTION-4_NH_OUTPUT-1 and time  0.1000000E+01
                                //       793  6.764684E+02
                                lines[0] = lines[0].Replace("heat generation for set", "heat generation (Id,RFL) for set");
                            }
                            else if (entry.Key == HOFieldNames.TotalHeatGeneration)
                            {
                                // total heat generation for set INTERNAL_SELECTION-4_NH_OUTPUT-1 and time  0.5750000E+01
                                //        9.890290E+02
                                lines[0] = lines[0].Replace("total heat generation for set", "total heat generation (RFL) for set");
                            }
                            //                                                                                                      
                            // Contact                                                                                              
                            //                                                                                                      
                            else if (entry.Key == HOFieldNames.RelativeContactDisplacement)
                            {
                                // relative contact displacement (slave element+face,normal,tang1,tang2) for all contact elements and time 0.1000000E+01
                                //     84102          4 -1.111983E-07 -2.300226E-07  1.142343E-07
                                lines[0] = lines[0].Replace("(slave element+face,normal,tang1,tang2)",
                                                            "(Id,Int.Pnt.,NORMAL,TANG1,TANG2)");
                                lines[0] = lines[0].Replace("for all contact elements", "for set ALL_CONTACT_ELEMENTS");
                            }
                            else if (entry.Key == HOFieldNames.ContactStress)
                            {
                                // contact stress (slave element+face,press,tang1,tang2) for all contact elements and time 0.5000000E+00
                                //     97837          4  9.511105E-01 -2.082501E-01 -2.605988E-02
                                lines[0] = lines[0].Replace("(slave element+face,press,tang1,tang2)",
                                                            "(Id,Int.Pnt.,PRESS,TANG1,TANG2)");
                                lines[0] = lines[0].Replace("for all contact elements", "for set ALL_CONTACT_ELEMENTS");
                            }
                            else if (entry.Key == HOFieldNames.ContactPrintEnergy || entry.Key == HOFieldNames.ContactSpringEnergy)
                            {
                                // contact print energy (slave element+face,energy)for all contact elements and time 0.5000000E+00
                                //     98823          4  6.898953E-06

                                // contact spring energy (slave element+face,energy) for all contact elements and time 0.1000000E+01
                                //       345          3  7.965814E-03
                                lines[0] = lines[0].Replace("(slave element+face,energy) for", "(Id,Int.Pnt.,ENERGY) for");
                                lines[0] = lines[0].Replace("(slave element+face,energy)for", "(Id,Int.Pnt.,ENERGY) for");
                                lines[0] = lines[0].Replace("for all contact elements", "for set ALL_CONTACT_ELEMENTS");
                            }
                            else if (entry.Key == HOFieldNames.TotalNumberOfContactElements)
                            {
                                // total number of contact elements for time  0.5000000E+00
                                // 560
                                lines[0] = lines[0].Replace("elements for time", "elements (NUM) for set ALL_CONTACT_ELEMENTS and time");
                            }
                            //                                                                                                      
                            // Element                                                                                              
                            //                                                                                                      
                            else if (entry.Key == HOFieldNames.InternalEnergyDensity)
                            {
                                // internal energy density (elem, integ.pnt.,eneset ELEMENTSET-1 and time  0.6250000E-01
                                //      3068   1  4.313000E-01
                                lines[0] = lines[0].Replace("(elem, integ.pnt.,eneset ",
                                                            "(Id,Int.Pnt.,ENER) for set ");
                                // internal energy density (elem, integ.pnt.,energy) for set INTERNAL_SELECTION-1_EH_OUTPUT-1 and time  0.6000000E+04
                                lines[0] = lines[0].Replace("(elem, integ.pnt.,energy",
                                                            "(Id,Int.Pnt.,ENER");
                            }
                            else if (entry.Key == HOFieldNames.InternalEnergy)
                            {
                                //internal energy (element, energy) for set SOLID_PART-1 and time  0.1000000E+01
                                //      1655  9.342906E-09
                                lines[0] = lines[0].Replace("(element, energy)", "(Id,ELSE)");
                            }
                            else if (entry.Key == HOFieldNames.TotalInternalEnergy)
                            {
                                //total internal energy for set SOLID_PART-1 and time  0.1000000E+01
                                //        3.249095E-04
                                lines[0] = lines[0].Replace("total internal energy for set",
                                                            "total internal energy (SE) for set");
                            }
                            else if (entry.Key == HOFieldNames.KineticEnergy)
                            {
                                //kinetic energy (element, energy) for set SOLID_PART-1 and time  0.1000000E+01
                                //      1655  9.342906E-09
                                lines[0] = lines[0].Replace("(element, energy)", "(Id,ELKE)");
                                lines[0] = lines[0].Replace("(element,energy)", "(Id,ELKE)");
                                lines[0] = lines[0].Replace("(elem, energy)", "(Id,ELKE)");
                                lines[0] = lines[0].Replace("(elem,energy)", "(Id,ELKE)");
                            }
                            else if (entry.Key == HOFieldNames.TotalKineticEnergy)
                            {
                                //total kinetic energy for set SOLID_PART-1 and time  0.1000000E+01
                                //        3.249095E-04
                                lines[0] = lines[0].Replace("total kinetic energy for set",
                                                            "total kinetic energy (KE) for set");
                            }
                            else if (entry.Key == HOFieldNames.Volume)
                            {
                                //volume (element, volume) for set SOLID_PART-1 and time  0.1000000E+01
                                //      1655  1.538557E+00
                                lines[0] = lines[0].Replace("(element, volume)", "(Id,EVOL)");
                            }
                            else if (entry.Key == HOFieldNames.TotalVolume)
                            {
                                //total volume for set SOLID_PART-1 and time  0.1000000E+01
                                //        2.322033E+03
                                lines[0] = lines[0].Replace("total volume for set", "total volume (VOL) for set");
                            }
                            // Thermal                                                                                              
                            else if (entry.Key == HOFieldNames.HeatFlux)
                            {
                                // heat flux (elem, integ.pnt.,qx,qy,qz) for set INTERNAL_SELECTION-3_EH_OUTPUT-1 and time  0.1000000E+01
                                //       477   1 -6.733427E+00  1.076158E+01 -1.030648E+01
                                lines[0] = lines[0].Replace("(elem, integ.pnt.,qx,qy,qz)", "(Id,Int.Pnt.,Q1,Q2,Q3)");
                            }
                            else if (entry.Key == HOFieldNames.BodyHeating)
                            {
                                // body heating (element, volume) for set INTERNAL_SELECTION-3_EH_OUTPUT-1 and time  0.1000000E+01
                                //       477  0.000000E+00
                                lines[0] = lines[0].Replace("(element, volume)", "(Id,EBHE)");
                            }
                            else if (entry.Key == HOFieldNames.TotalBodyHeating)
                            {
                                // total body heating for set INTERNAL_SELECTION-3_EH_OUTPUT-1 and time  0.1000000E+01
                                //        0.126000E+00
                                lines[0] = lines[0].Replace("total body heating for set", "total body heating (BHE) for set");
                            }
                        }
                        //stresses (elem, integ.pnt.,sxx,syy,szz,sxy,sxz,syz) for set INTERNAL_SELECTION-1_EH_OUTPUT-1 and time  0.2000000E+01
                        //
                        //19   1 -8.604228E-16  1.101896E-14  5.000000E-01  2.171349E-16  2.768470E-16 -1.250697E-15 _shell_0000000019   

                    }

                }
            }
        }
        static private void RepairShellStressLines(string[] lines)
        {
            int start;
            for (int i = 1; i < lines.Length; i++)
            {
                start = lines[i].IndexOf("_sh");
                if (start > 0) lines[i] = lines[i].Substring(0, start);
            }
        }
        static private string GetFullSetName(string setName, Dictionary<string, string> hashSuffixFullNames)
        {
            if (hashSuffixFullNames.Count == 0) return setName;
            //
            string fullName;
            string[] names = setName.Split(_atSplitter, StringSplitOptions.None);
            for (int i = 0; i < names.Length; i++)
            {
                fullName = CalculixNameShortener.GetFullName(names[i], hashSuffixFullNames);
                if (fullName != names[i]) names[i] = fullName.ToUpper();
            }
            return string.Join(_atSplitter[0], names);
        }
        static private string RepairSetName(string setName, Dictionary<string, string> repairedSetNames)
        {
            string[] tmp;
            string newName = setName;
            if (setName.Contains(_atSplitter[0]))    // must be first
            {
                if (!repairedSetNames.TryGetValue(setName, out newName))
                {
                    tmp = setName.Split(_atSplitter, StringSplitOptions.None);
                    if (tmp.Length == 2)
                    {
                        string slaveName = RepairSetName(tmp[0], repairedSetNames);
                        string masterName = RepairSetName(tmp[1], repairedSetNames);
                        //
                        if (slaveName.EndsWith(Globals.SlaveNameSuffix.ToUpper()))
                            slaveName = slaveName.Replace(Globals.SlaveNameSuffix.ToUpper(), "");
                        if (masterName.EndsWith(Globals.MasterNameSuffix.ToUpper()))
                            masterName = masterName.Replace(Globals.MasterNameSuffix.ToUpper(), "");
                        //
                        newName = slaveName == masterName ? slaveName : slaveName + "_" + masterName;
                        // Check if an existing set was already renamed to an existing new name
                        if (repairedSetNames.Values.Contains(newName))
                        {
                            newName = NamedClass.GetNameWithoutLastValue(newName);
                            newName = new HashSet<string>(repairedSetNames.Values).GetNextNumberedKey(newName);
                        }
                        repairedSetNames.Add(setName, newName);
                    }
                }
            }
            else if (setName.StartsWith(Globals.InternalSelectionName.ToUpper()))
            {
                if (!repairedSetNames.TryGetValue(setName, out newName))
                {
                    int count = Globals.InternalSelectionName.Split(_underscoreSplitter, StringSplitOptions.None).Length;
                    //
                    tmp = setName.Split(_underscoreSplitter, StringSplitOptions.None);
                    newName = "";
                    for (int i = count; i < tmp.Length; i++)
                    {
                        newName += tmp[i];
                        if (i < tmp.Length - 1) newName += "_";
                    }
                    // Check if an existing set was already renamed to an existing new name
                    if (repairedSetNames.Values.Contains(newName))
                    {
                        newName = NamedClass.GetNameWithoutLastValue(newName);
                        newName = new HashSet<string>(repairedSetNames.Values).GetNextNumberedKey(newName);
                    }
                    repairedSetNames.Add(setName, newName);
                }
            }
            else if (setName.StartsWith(Globals.InternalName.ToUpper()))
            {
                if (!repairedSetNames.TryGetValue(setName, out newName))
                {
                    int count = Globals.InternalName.Split(_underscoreSplitter, StringSplitOptions.None).Length;
                    //
                    tmp = setName.Split(_underscoreSplitter, StringSplitOptions.None);
                    newName = "";
                    for (int i = count; i < tmp.Length; i++)
                    {
                        newName += tmp[i];
                        if (i < tmp.Length - 1) newName += "_";
                    }
                    // Check if an existing set was already renamed to an existing new name
                    if (repairedSetNames.Values.Contains(newName))
                    {
                        newName = NamedClass.GetNameWithoutLastValue(newName);
                        newName = new HashSet<string>(repairedSetNames.Values).GetNextNumberedKey(newName);
                    }
                    repairedSetNames.Add(setName, newName);
                }
            }
            return newName;
        }
        static private string GetFrequencyHistorySetName(int stepId, int nodalDiameter)
        {
            string setName = _stepNumberKey + stepId;
            if (nodalDiameter >= 0) setName += "_ND_" + nodalDiameter;
            return setName;
        }
        static private int GetCyclicNodalDiameter(string[] dataSetLines)
        {
            if (dataSetLines[0] != _eigenvalueOutputKey) return -1;
            string[] tmp;
            for (int i = 1; i < dataSetLines.Length; i++)
            {
                tmp = dataSetLines[i].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
                if (tmp.Length == 6 &&
                    int.TryParse(tmp[0], out int nodalDiameter))
                    return nodalDiameter;
            }
            return -1;
        }
        static private HistoryResults GetFrequencyDatDataSets(string[] dataSetLines, int stepId, int nodalDiameter,
                                                              bool axisymmetric)
        {
            int len;
            int firstDataRowId = 1;
            double factor = 1;
            double componentFactor;
            string mode;
            string timeFrequency;
            string entryName = null;
            string totalEntryName = HOFieldNames.TotalEffectiveModalMass;
            string historyResultFieldName = HOFieldNames.Frequency;
            string[] tmp;
            string[] componentNames = null;
            bool cyclicEigenvalueOutput = false;
            double time = 0;
            List<double[]> valuesList = null;
            double[] rowValues = null;
            IEnumerable<DatDataSet> dataSets = new List<DatDataSet>();
            //
            if (dataSetLines[0] == _eigenvalueOutputKey)
            {
                firstDataRowId = 4;
                //
                historyResultFieldName = HOFieldNames.Frequency;
                //
                tmp = dataSetLines[firstDataRowId].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
                if (tmp.Length == 5)
                {
                    entryName = HOFieldNames.EigenvalueOutput;
                    componentNames = new string[] { HOComponentNames.EIGENVALUE, HOComponentNames.OMEGA,
                                                    HOComponentNames.FREQUENCY, HOComponentNames.FREQUENCY_IM};
                }
                // Cyclic frequency
                else if (tmp.Length == 6)
                {
                    entryName = HOFieldNames.EigenvalueOutput;
                    componentNames = new string[] { HOComponentNames.NODAL_DIAMETER, HOComponentNames.EIGENVALUE,
                                                    HOComponentNames.OMEGA, HOComponentNames.FREQUENCY,
                                                    HOComponentNames.FREQUENCY_IM };
                    cyclicEigenvalueOutput = true;
                }
                // Complex frequency                            
                else if (tmp.Length == 4)
                {
                    entryName = HOFieldNames.EigenvalueOutput;
                    componentNames = new string[] { HOComponentNames.OMEGA, HOComponentNames.FREQUENCY,
                                                    HOComponentNames.FREQUENCY_IM};
                }
            }
            else if (dataSetLines[0] == _participationFactorsKey)
            {
                firstDataRowId = 2;
                //
                historyResultFieldName = HOFieldNames.Factor;
                //
                entryName = HOFieldNames.ParticipationFactors;
                componentNames = new string[] { HOComponentNames.XCOMPONENT, HOComponentNames.YCOMPONENT,
                                                HOComponentNames.ZCOMPONENT, HOComponentNames.XROTATION,
                                                HOComponentNames.YROTATION, HOComponentNames.ZROTATION };
                if (axisymmetric) factor = Math.Sqrt(_axisymmetricSegmentsPerRevolution);
            }
            else if (dataSetLines[0] == _effectiveModalMassKey)
            {
                firstDataRowId = 2;
                //
                historyResultFieldName = HOFieldNames.Factor;
                //
                if (axisymmetric) factor = _axisymmetricSegmentsPerRevolution;
                entryName = HOFieldNames.EffectiveModalMass;
                componentNames = new string[] { HOComponentNames.XCOMPONENT, HOComponentNames.YCOMPONENT,
                                                HOComponentNames.ZCOMPONENT, HOComponentNames.XROTATION,
                                                HOComponentNames.YROTATION, HOComponentNames.ZROTATION };
            }
            else if (dataSetLines[0] == _totalEffectiveMassKey)
            {
                firstDataRowId = 2;
                //
                historyResultFieldName = HOFieldNames.Factor;
                //
                if (axisymmetric) factor = _axisymmetricSegmentsPerRevolution;
                entryName = HOFieldNames.TotalEffectiveMass;
                componentNames = new string[] { HOComponentNames.XCOMPONENT, HOComponentNames.YCOMPONENT,
                                                HOComponentNames.ZCOMPONENT, HOComponentNames.XROTATION,
                                                HOComponentNames.YROTATION, HOComponentNames.ZROTATION };
            }
            // Steady state                                     
            else if (dataSetLines[0].ToUpper().StartsWith(_participationFactorsForFrequencyKey))
            {
                firstDataRowId = 3;
                //
                historyResultFieldName = HOFieldNames.Factor;
                //
                len = _participationFactorsForFrequencyKey.Length;
                timeFrequency = dataSetLines[0].Substring(len, dataSetLines[0].Length - len).Trim();
                tmp = timeFrequency.Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
                entryName = HOFieldNames.ParticipationFactorsForFrequency + "_" + tmp[0];
                componentNames = new string[] { HOComponentNames.FREQUENCY, HOComponentNames.PAR_FACTOR,
                                                HOComponentNames.PAR_FACTOR_IM };
            }
            // Complex frequency                                
            else if (dataSetLines[0].ToUpper().StartsWith(_participationFactorsForModeKey))
            {
                firstDataRowId = 3;
                //
                historyResultFieldName = HOFieldNames.Factor;
                //
                len = _participationFactorsForModeKey.Length;
                mode = dataSetLines[0].Substring(len, dataSetLines[0].Length - len).Trim();
                entryName = HOFieldNames.ParticipationFactorsForMode + "_" + mode;
                //
                componentNames = new string[] { HOComponentNames.FREQUENCY, HOComponentNames.PAR_FACTOR,
                                                HOComponentNames.PAR_FACTOR_IM };
            }
            else if (dataSetLines[0] == _modalAssuranceCriteriumKey)
            {
                firstDataRowId = 1;
                //
                historyResultFieldName = HOFieldNames.Factor;
                //
                tmp = dataSetLines[firstDataRowId].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
                entryName = HOFieldNames.ModalAssuranceCriterium;
                componentNames = new string[tmp.Length];
                for (int i = 0; i < componentNames.Length; i++)
                {
                    componentNames[i] = HOComponentNames.MODE + "_" + (i + 1);
                }
            }
            //
            HistoryResults historyResults = new HistoryResults("HistoryResults");
            HistoryResultSet historyResultSet = new HistoryResultSet(GetFrequencyHistorySetName(stepId, nodalDiameter));
            HistoryResultField historyResultField = new HistoryResultField(historyResultFieldName);
            HistoryResultComponent historyResultComponent = new HistoryResultComponent(entryName);
            HistoryResultComponent totalHistoryResultComponent = new HistoryResultComponent(totalEntryName);
            HistoryResultEntries historyResultEntries = null;
            HistoryResultEntries totalHistoryResultEntries = null;
            valuesList = new List<double[]>();
            //
            for (int i = firstDataRowId; i < dataSetLines.Length; i++)
            {
                tmp = dataSetLines[i].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
                if (tmp.Length >= 2)
                {
                    rowValues = new double[tmp.Length];
                    for (int j = 0; j < rowValues.Length; j++) rowValues[j] = ParseStringToDouble(tmp[j]);
                    if (cyclicEigenvalueOutput && rowValues.Length == 6)
                    {
                        double rowNodalDiameter = rowValues[0];
                        rowValues[0] = rowValues[1];
                        rowValues[1] = rowNodalDiameter;
                    }
                    valuesList.Add(rowValues);
                }
            }
            double[][] values = valuesList.ToArray();   // [row][col]
            if (values != null && values.Length > 0)
            {
                int delta = values[0].Length - componentNames.Length;
                for (int col = delta; col < values[0].Length; col++)  // first value in a row is time
                {
                    componentFactor = componentNames[col - delta] == HOComponentNames.YCOMPONENT ? factor : 1;
                    historyResultEntries = new HistoryResultEntries(componentNames[col - delta], false);
                    totalHistoryResultEntries = new HistoryResultEntries(componentNames[col - delta], false);
                    //
                    for (int row = 0; row < values.Length; row++)
                    {
                        if (delta == 1) time = values[row][0];
                        else time = row + 1;
                        //
                        if (double.IsNaN(time)) totalHistoryResultEntries.Add(1, values[row][col] * componentFactor);
                        else historyResultEntries.Add(time, values[row][col] * componentFactor);
                    }
                    historyResultComponent.Entries.Add(historyResultEntries.Name, historyResultEntries);
                    if (totalHistoryResultEntries.Time.Count > 0)
                        totalHistoryResultComponent.Entries.Add(totalHistoryResultEntries.Name, totalHistoryResultEntries);
                }
                historyResultField.Components.Add(historyResultComponent.Name, historyResultComponent);
                if (totalHistoryResultComponent.Entries.Count > 0)
                    historyResultField.Components.Add(totalHistoryResultComponent.Name, totalHistoryResultComponent);
                historyResultSet.Fields.Add(historyResultField.Name, historyResultField);
                historyResults.Sets.Add(historyResultSet.Name, historyResultSet);
            }
            //
            return historyResults;
        }
        static private HistoryResults GetComplexFrequencyTurningDirection(string[] dataSetLines, int stepId, int nodalDiameter)
        {
            string[] tmp;
            HistoryResults historyResults = new HistoryResults("HistoryResults");
            HistoryResultSet historyResultSet = new HistoryResultSet(GetFrequencyHistorySetName(stepId, nodalDiameter));
            HistoryResultField historyResultField = new HistoryResultField(HOFieldNames.Rotation);
            HistoryResultComponent hrcAxisDirection = new HistoryResultComponent(HOFieldNames.AxisDirection);
            HistoryResultComponent hrcTurningDirection = new HistoryResultComponent(HOFieldNames.TurningDirection);
            HistoryResultEntries historyResultEntries;
            // Axis
            tmp = dataSetLines[1].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
            if (tmp.Length == 6)
            {
                historyResultEntries = new HistoryResultEntries(HOComponentNames.X, false);
                historyResultEntries.Add(1, ParseStringToDouble(tmp[3]));
                hrcAxisDirection.Entries.Add(historyResultEntries.Name, historyResultEntries);
                //
                historyResultEntries = new HistoryResultEntries(HOComponentNames.Y, false);
                historyResultEntries.Add(1, ParseStringToDouble(tmp[4]));
                hrcAxisDirection.Entries.Add(historyResultEntries.Name, historyResultEntries);
                //
                historyResultEntries = new HistoryResultEntries(HOComponentNames.Z, false);
                historyResultEntries.Add(1, ParseStringToDouble(tmp[5]));
                hrcAxisDirection.Entries.Add(historyResultEntries.Name, historyResultEntries);
            }
            //
            historyResultField.Components.Add(hrcAxisDirection.Name, hrcAxisDirection);
            // Direction
            historyResultEntries = new HistoryResultEntries(HOComponentNames.DIRECTION, false);
            int modeNumber;
            string directionText;
            double direction;
            for (int i = 3; i < dataSetLines.Length; i++)
            {
                tmp = dataSetLines[i].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
                if (tmp.Length == 2)
                {
                    int.TryParse(tmp[0], out modeNumber);
                    directionText = tmp[1];
                }
                else if (tmp.Length == 3)
                {
                    int.TryParse(tmp[1], out modeNumber);
                    directionText = tmp[2];
                }
                else continue;
                //
                if (directionText.Trim().ToUpper() == "F") direction = 1;
                else direction = -1;
                historyResultEntries.Add(modeNumber, direction);
            }
            hrcTurningDirection.Entries.Add(historyResultEntries.Name, historyResultEntries);
            historyResultField.Components.Add(hrcTurningDirection.Name, hrcTurningDirection);
            //
            historyResultSet.Fields.Add(historyResultField.Name, historyResultField);
            historyResults.Sets.Add(historyResultSet.Name, historyResultSet);
            //
            return historyResults;
        }
        static private HistoryResults GetBucklingDatDataSets(string[] dataSetLines, int stepId)
        {
            int firstDataRowId = 1;
            string[] componentNames = null;
            double time = 0;
            List<double[]> valuesList = null;
            double[] rowValues = null;
            IEnumerable<DatDataSet> dataSets = new List<DatDataSet>();
            //
            if (dataSetLines[0] == _bucklingFactorKey)
            {
                firstDataRowId = 3;
                componentNames = new string[] { HOComponentNames.BUCKLINGFACTOR};
            }
            //
            string[] tmp;
            HistoryResults historyResults = new HistoryResults("HistoryResults");
            HistoryResultSet historyResultSet = new HistoryResultSet(_stepNumberKey + stepId);
            HistoryResultField historyResultField = new HistoryResultField(HOFieldNames.Buckling);
            HistoryResultComponent historyResultComponent = new HistoryResultComponent(HOComponentNames.BUCKLINGFACTOR);
            HistoryResultEntries historyResultEntries = null;
            valuesList = new List<double[]>();
            //
            for (int i = firstDataRowId; i < dataSetLines.Length; i++)
            {
                tmp = dataSetLines[i].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
                if (tmp.Length >= 2)
                {
                    rowValues = new double[tmp.Length];
                    for (int j = 0; j < rowValues.Length; j++) rowValues[j] = ParseStringToDouble(tmp[j]);
                    valuesList.Add(rowValues);
                }
            }
            double[][] values = valuesList.ToArray();   // [row][col]
            if (values != null && values.Length > 0)
            {
                int delta = values[0].Length - componentNames.Length;
                for (int col = delta; col < values[0].Length; col++)  // first value in a row is time
                {
                    historyResultEntries = new HistoryResultEntries(componentNames[col - delta], false);
                    //
                    for (int row = 0; row < values.Length; row++)
                    {
                        if (delta == 1) time = values[row][0];
                        else time = row + 1;
                        //
                        historyResultEntries.Add(time, values[row][col]);
                    }
                    historyResultComponent.Entries.Add(historyResultEntries.Name, historyResultEntries);
                }
                historyResultField.Components.Add(historyResultComponent.Name, historyResultComponent);
                //
                historyResultSet.Fields.Add(historyResultField.Name, historyResultField);
                historyResults.Sets.Add(historyResultSet.Name, historyResultSet);
            }
            //
            return historyResults;
        }
        static private Dictionary<double, double> GetTimeFrequencyPairs(HistoryResults frequencyHistoryOutput, string stepKey)
        {
            HistoryResultSet hrs;
            HistoryResultField hrf;
            HistoryResultComponent hrc;
            HistoryResultEntries hre;
            Dictionary<double, double> timeFrequency = null;
            //
            if (frequencyHistoryOutput.Sets.TryGetValue(stepKey, out hrs) &&
                hrs.Fields.TryGetValue(HOFieldNames.Frequency, out hrf) &&
                hrf.Components.TryGetValue(HOFieldNames.EigenvalueOutput, out hrc) &&
                hrc.Entries.TryGetValue(HOComponentNames.FREQUENCY, out hre))
            {
                timeFrequency = new Dictionary<double, double>();
                for (int i = 0; i < hre.Time.Count; i++) timeFrequency.Add(hre.Time[i], hre.Values[i]);
            }
            //
            return timeFrequency;
        }
        static private Dictionary<double, double> GetTimeBucklingFactorPairs(HistoryResults bucklingHistoryOutput, string stepKey)
        {
            HistoryResultSet hrs;
            HistoryResultField hrf;
            HistoryResultComponent hrc;
            HistoryResultEntries hre;
            Dictionary<double, double> timeFrequency = null;
            //
            if (bucklingHistoryOutput.Sets.TryGetValue(stepKey, out hrs) &&
                hrs.Fields.TryGetValue(HOFieldNames.Buckling, out hrf) &&
                hrf.Components.TryGetValue(HOComponentNames.BUCKLINGFACTOR, out hrc) &&
                hrc.Entries.TryGetValue(HOComponentNames.BUCKLINGFACTOR, out hre))
            {
                timeFrequency = new Dictionary<double, double>();
                for (int i = 0; i < hre.Time.Count; i++) timeFrequency.Add(hre.Time[i], hre.Values[i]);
            }
            //
            return timeFrequency;
        }
        static private DatDataSet GetDatDataSet(string[] dataSetLines, Dictionary<string, string> repairedSetNames,
                                                Dictionary<string, string> hashSuffixFullNames)
        {
            try
            {
                // displacements (vx, vy, vz) for set DISP and time  0.1000000E+00
                IEnumerable<string> componentNames = new List<string>();
                DatDataSet dataSet = new DatDataSet();
                //
                string firstLine = dataSetLines[0];
                if (firstLine.ToUpper().StartsWith(_participationFactorsForFrequencyKey))
                {
                    dataSet.SetName = _participationFactorsForFrequencyKey;
                    return dataSet;
                }
                //
                foreach (var entry in nameKeyMap)
                {
                    if (firstLine.ToLower().StartsWith(entry.Value.ToLower()))
                    {
                        dataSet.FieldName = entry.Key;
                        break;
                    }
                }
                //
                string[] tmp = firstLine.Split(_parenthesesSplitter, StringSplitOptions.RemoveEmptyEntries);
                //
                string[] tmp2 = tmp[1].Split(_componentsSplitter, StringSplitOptions.RemoveEmptyEntries);
                componentNames = tmp2.ToList();
                //
                tmp2 = tmp[2].Split(_dataSplitter, StringSplitOptions.RemoveEmptyEntries);
                dataSet.BaseSetName = GetFullSetName(tmp2[0], hashSuffixFullNames);
                dataSet.SetName = RepairSetName(GetFullSetName(tmp2[0].Trim(), hashSuffixFullNames), repairedSetNames);
                dataSet.Time = double.Parse(tmp2[1]);
                //
                int length;
                double[] values;
                List<bool> locals = new List<bool>();
                List<double[]> allValues = new List<double[]>();
                for (int i = 1; i < dataSetLines.Length; i++)
                {
                    tmp = dataSetLines[i].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
                    // Local result
                    if (tmp[tmp.Length - 1].ToUpper() == "L")
                    {
                        locals.Add(true);
                        length = tmp.Length - 1;
                    }
                    else
                    {
                        locals.Add(false);
                        length = tmp.Length;
                    }
                    //
                    values = new double[length];
                    //
                    for (int j = 0; j < length; j++) values[j] = ParseStringToDouble(tmp[j]);
                    // Fix for when no history output exists - Find all occurrences!!!
                    if (dataSet.FieldName == HOFieldNames.RelativeContactDisplacement)
                    {
                        if (values.Length != 5 || double.IsNaN(values[0]) || double.IsNaN(values[1]))
                        {
                            locals.Add(false);
                            // Set the element id to -1
                            // Set the faceName to 1 equals to FeFaceName.S1
                            // Set the values to the smallest possible value
                            allValues.Add(new double[] { -1, 1, double.Epsilon, double.Epsilon, double.Epsilon });
                        }
                    }
                    //
                    allValues.Add(values);
                }
                // Fix for when no history output exists - Find all occurrences!!!
                if (dataSet.FieldName == HOFieldNames.RelativeContactDisplacement && allValues.Count == 0)
                {
                    locals.Add(false);
                    // Set the element id to -1
                    // Set the faceName to 1 equals to FeFaceName.S1
                    // Set the values to the smallest possible value
                    allValues.Add(new double[] { -1, 1, double.Epsilon, double.Epsilon, double.Epsilon });
                }
                dataSet.Locals = locals.ToArray();
                dataSet.Values = allValues.ToArray();
                dataSet.ComponentNames = componentNames.ToArray();
                //
                return dataSet;
            }
            catch
            {
                return new DatDataSet() { FieldName = HOFieldNames.Error };
            }
        }
        static private double ParseStringToDouble(string valueAsString)
        {
            double value;
            int count;
            string tmp3;
            string[] tmp2;
            if (!double.TryParse(valueAsString, out value))
            {
                // Try to repair values as:
                // 1.090361-282  8.468565-284 -8.171595-285
                tmp3 = valueAsString;
                tmp2 = tmp3.Split(_signSplitter, StringSplitOptions.RemoveEmptyEntries);
                if (tmp2.Length == 2)
                {
                    count = tmp3.Length - (tmp2[1].Length + 1);
                    valueAsString = tmp3.Substring(0, count) + "E" + tmp3.Substring(count, tmp2[1].Length + 1);
                    //
                    if (!double.TryParse(valueAsString, out value)) value = double.NaN;
                }
                else value = double.NaN;
            }
            return value;
        }
        static private HistoryResults GetHistoryOutput(IEnumerable<DatDataSet> dataSets,
                                                       Dictionary<string, string> hashSuffixFullNames, bool axisymmetric,
                                                       FeMesh axisymmetricModelMesh, List<string> unscaledSectionMessages)
        {
            Dictionary<string, double> sectionPrintFactors = new Dictionary<string, double>();
            HistoryResults historyOutput = new HistoryResults("HistoryOutput");
            HistoryResultSet set;
            HistoryResultField field;
            HistoryResultComponent component = null;
            HistoryResultEntries entries;
            //
            DatDataSet repairedDataSet;
            int offset;
            string valueId;
            string id;
            double time;
            double factor;
            double componentFactor;
            double[] values;
            //
            foreach (var dataSet in dataSets)
            {
                repairedDataSet = RepairReferencePointDataSet(dataSet, hashSuffixFullNames);
                //
                factor = 1;
                if (axisymmetric)
                    factor = GetAxisymmetricSegmentsPerRevolution(repairedDataSet, axisymmetricModelMesh,
                                                                  sectionPrintFactors, unscaledSectionMessages) *
                             GetAxisymmetricModeFactor(repairedDataSet);
                time = repairedDataSet.Time;
                // Get or create a set
                if (!historyOutput.Sets.TryGetValue(repairedDataSet.SetName, out set))
                {
                    set = new HistoryResultSet(repairedDataSet.SetName);
                    set.BaseSetName = repairedDataSet.BaseSetName;
                    historyOutput.Sets.Add(set.Name, set);
                }
                // Get or create a field
                if (!set.Fields.TryGetValue(repairedDataSet.FieldName, out field))
                {
                    field = new HistoryResultField(repairedDataSet.FieldName);
                    set.Fields.Add(field.Name, field);
                }
                // For each value line in data set: id x y z
                for (int i = 0; i < repairedDataSet.Values.Length; i++)
                {
                    values = repairedDataSet.Values[i];
                    //
                    if (repairedDataSet.ComponentNames.Length > 0 && repairedDataSet.ComponentNames[0] == "Id")
                    {
                        // The first column is id column
                        if (repairedDataSet.ComponentNames[1] == "Int.Pnt.")
                        {
                            // The second column is In.Pnt. column
                            valueId = values[0].ToString() + "_" + values[1].ToString();
                            offset = 2;
                        }
                        else
                        {
                            valueId = values[0].ToString();
                            offset = 1;
                        }
                    }
                    // There is no id
                    else
                    {
                        valueId = null;
                        offset = 0;
                    }                    
                    //                                                                                  
                    // For each component
                    for (int j = 0; j < values.Length - offset; j++)
                    {
                        //19   1 - 8.604228E-16  1.101896E-14  5.000000E-01  2.171349E-16  2.768470E-16 - 1.250697E-15 _shell_0000000019

                        // Get or create a component
                        if (!field.Components.TryGetValue(repairedDataSet.ComponentNames[j + offset], out component))
                        {
                            component = new HistoryResultComponent(repairedDataSet.ComponentNames[j + offset]);
                            field.Components.Add(component.Name, component);
                        }
                        componentFactor = factor;
                        if (axisymmetric && HOFieldNames.GetNoSuffixName(field.Name) == HOFieldNames.SurfaceLoads &&
                            _axisymmetricMomentComponentNames.Contains(component.Name))
                            componentFactor = GetAxisymmetricModeFactor(repairedDataSet);
                        // For the case of total forces
                        if (valueId == null) id = component.Name;
                        else id = valueId;
                        // Get or create historyValues as component entries
                        if (!component.Entries.TryGetValue(id, out entries))
                        {
                            entries = new HistoryResultEntries(id, repairedDataSet.Locals[i]);
                            component.Entries.Add(entries.Name, entries);
                        }
                        // Sum - If the same Id exists for the same time: sum them together
                        if ((field.Name == HOFieldNames.RelativeContactDisplacement ||
                             field.Name == HOFieldNames.ContactStress ||
                             field.Name == HOFieldNames.ContactPrintEnergy ||
                             field.Name == HOFieldNames.ContactSpringEnergy) && entries.Time.Contains(time))
                        {
                            entries.SumValue(values[j + offset] * componentFactor);
                        }
                        // Skip repeating
                        else if (field.Name == HOFieldNames.TotalNumberOfContactElements && entries.Time.Contains(time))
                        { }
                        // Add
                        else
                        {
                            entries.Add(time, values[j + offset] * componentFactor);
                        }
                    }
                }
                //
            }
            // Average the summed values
            foreach (var setEntry in historyOutput.Sets)
            {
                foreach (var fieldEntry in setEntry.Value.Fields)
                {
                    if (_contactFaceSumFieldNames.Contains(fieldEntry.Key)) continue;
                    foreach (var componentEntry in fieldEntry.Value.Components)
                    {
                        foreach (var entry in componentEntry.Value.Entries)
                        {
                            entry.Value.ComputeAverage();
                        }
                    }
                }
            }
            //
            return historyOutput;
        }
        static private double GetAxisymmetricModeFactor(DatDataSet dataSet)
        {
            if (!dataSet.Mode) return 1;
            if (_axisymmetricModeFieldNames.Contains(dataSet.FieldName))
                return 1 / Math.Sqrt(_axisymmetricSegmentsPerRevolution);
            if (_axisymmetricModeEnergyFieldNames.Contains(dataSet.FieldName)) return 1 / _axisymmetricSegmentsPerRevolution;
            return 1;
        }
        static private double GetAxisymmetricSegmentsPerRevolution(DatDataSet dataSet, FeMesh axisymmetricModelMesh,
                                                                   Dictionary<string, double> sectionPrintFactors,
                                                                   List<string> unscaledSectionMessages)
        {
            string fieldName = dataSet.FieldName;
            if (fieldName.EndsWith(HOFieldNames.ComplexRealSuffix))
                fieldName = fieldName.Substring(0, fieldName.Length - HOFieldNames.ComplexRealSuffix.Length);
            else if (fieldName.EndsWith(HOFieldNames.ComplexImaginarySuffix))
                fieldName = fieldName.Substring(0, fieldName.Length - HOFieldNames.ComplexImaginarySuffix.Length);
            //
            if (_axisymmetricSegmentFieldNames.Contains(fieldName)) return _axisymmetricSegmentsPerRevolution;
            if (!_axisymmetricSegmentStatisticsFieldNames.Contains(fieldName) || dataSet.BaseSetName == null) return 1;
            // The contact statistics are named by both of their surfaces
            if (dataSet.BaseSetName.Contains(_atSplitter[0])) return _axisymmetricSegmentsPerRevolution;
            //
            double factor;
            if (!sectionPrintFactors.TryGetValue(dataSet.BaseSetName, out factor))
            {
                string message;
                factor = GetAxisymmetricSectionPrintSegmentsPerRevolution(axisymmetricModelMesh, dataSet.BaseSetName,
                                                                          dataSet.SetName, out message);
                if (message != null) unscaledSectionMessages.Add(message);
                sectionPrintFactors.Add(dataSet.BaseSetName, factor);
            }
            return factor;
        }
        static private double GetAxisymmetricSectionPrintSegmentsPerRevolution(FeMesh mesh, string surfaceName,
                                                                               string historySetName, out string message)
        {
            message = null;
            FeSurface surface = null;
            if (mesh != null)
            {
                foreach (var entry in mesh.Surfaces)
                {
                    if (entry.Key.ToUpper() == surfaceName.ToUpper())
                    {
                        surface = entry.Value;
                        break;
                    }
                }
            }
            if (surface != null && !surface.Name.StartsWith(Globals.InternalName)) surfaceName = surface.Name;
            else surfaceName = historySetName;
            string messageStart = "The section print history " + surfaceName + " of the axisymmetric model is left for " +
                                  "the part of the revolution CalculiX writes it for, since ";
            if (mesh == null)
            {
                message = messageStart + "the angle depends on the element types of the model the result was computed " +
                          "from, which is not open.";
                return 1;
            }
            if (surface == null || surface.ElementFaces == null)
            {
                message = messageStart + "the surface was not found in the model.";
                return 1;
            }
            //
            string typeName;
            FeElement element;
            FeElementSet elementSet;
            HashSet<string> typeNames = new HashSet<string>();
            foreach (var entry in surface.ElementFaces)
            {
                if (!mesh.ElementSets.TryGetValue(entry.Value, out elementSet)) continue;
                foreach (var elementId in elementSet.Labels)
                {
                    if (!mesh.Elements.TryGetValue(elementId, out element)) continue;
                    typeName = GetAxisymmetricElementTypeName(mesh.GetPartFromId(element.PartId) as MeshPart, element);
                    if (typeName != null) typeNames.Add(typeName);
                    else typeNames.Add(element.GetType().Name);
                }
            }
            //
            double factor;
            HashSet<double> factors = new HashSet<double>();
            List<string> typeDescriptions = new List<string>();
            foreach (var name in typeNames.OrderBy(n => n))
            {
                if (_axisymmetricSectionPrintSegmentsPerRevolution.TryGetValue(name, out factor))
                {
                    factors.Add(factor);
                    typeDescriptions.Add(name + " for " + (360 / factor) + (factor == 360 ? " degree" : " degrees"));
                }
                else
                {
                    factors.Add(double.NaN);
                    typeDescriptions.Add(name + " for an unknown angle");
                }
            }
            if (factors.Count == 1 && !double.IsNaN(factors.First())) return factors.First();
            //
            if (typeNames.Count == 0) message = messageStart + "the surface has no elements.";
            else message = messageStart + "CalculiX writes its element types for different or unknown angles: " +
                           string.Join(", ", typeDescriptions) + ".";
            return 1;
        }
        static private string GetAxisymmetricElementTypeName(MeshPart part, FeElement element)
        {
            if (part == null) return null;
            if (element is LinearTriangleElement) return part.LinearTriaType.ToString();
            if (element is ParabolicTriangleElement) return part.ParabolicTriaType.ToString();
            if (element is LinearQuadrilateralElement) return part.LinearQuadType.ToString();
            if (element is ParabolicQuadrilateralElement) return part.ParabolicQuadType.ToString();
            return null;
        }
        static private DatDataSet RepairReferencePointDataSet(DatDataSet dataSet, Dictionary<string, string> hashSuffixFullNames)
        {
            string setName = dataSet.SetName;
            string[] tmp;
            // Ref node
            tmp = setName.ToUpper().Split(new string[] { FeReferencePoint.RefName.ToUpper() },
                                          StringSplitOptions.RemoveEmptyEntries);
            if (tmp.Length == 2) dataSet.SetName = GetFullSetName(tmp[0], hashSuffixFullNames);
            // Rot node
            tmp = setName.ToUpper().Split(new string[] { FeReferencePoint.RotName.ToUpper() },
                                          StringSplitOptions.RemoveEmptyEntries);
            if (tmp.Length == 2)
            {
                dataSet.SetName = GetFullSetName(tmp[0], hashSuffixFullNames);
                for (int i = 0; i < dataSet.ComponentNames.Length; i++)
                {
                    dataSet.ComponentNames[i] = _compMapRP[dataSet.ComponentNames[i]];
                }
            }
            //
            return dataSet;
        }
        //
        static private void AddComplexFields(HistoryResults historyOutput)
        {
            HistoryResultField fieldR;
            HistoryResultField fieldI;
            HistoryResultField fieldMag;
            HistoryResultField fieldPha;
            HistoryResultComponent compR;
            HistoryResultComponent compI;
            HistoryResultComponent compMag;
            HistoryResultComponent compPha;
            HistoryResultEntries entryR;
            HistoryResultEntries entryI;
            HistoryResultEntries entryMag;
            HistoryResultEntries entryPha;
            double[] real;
            double[] imaginary;
            double[] mag;
            double[] pha;
            HashSet<string> complexFieldNames = new HashSet<string>();
            foreach (var setsEntry in historyOutput.Sets)
            {
                complexFieldNames.Clear();
                foreach (var fieldEntry in setsEntry.Value.Fields)
                {
                    if (HOFieldNames.HasComplexSuffix(fieldEntry.Key))
                        complexFieldNames.Add(HOFieldNames.GetNoSuffixName(fieldEntry.Key));
                }
                //
                foreach (var complexFieldName in complexFieldNames)
                {
                    if (setsEntry.Value.Fields.TryGetValue(complexFieldName + HOFieldNames.ComplexRealSuffix, out fieldR) &&
                        setsEntry.Value.Fields.TryGetValue(complexFieldName + HOFieldNames.ComplexImaginarySuffix, out fieldI))
                    {
                        fieldMag = new HistoryResultField(complexFieldName + HOFieldNames.ComplexMagnitudeSuffix);
                        fieldPha = new HistoryResultField(complexFieldName + HOFieldNames.ComplexPhaseSuffix);
                        //
                        foreach (var componentName in fieldR.Components.Keys)
                        {
                            compMag = new HistoryResultComponent(componentName);
                            compPha = new HistoryResultComponent(componentName);
                            compPha.Unit = StringAngleDegConverter.GetUnitAbbreviation();
                            //
                            if (fieldR.Components.TryGetValue(componentName, out compR) &&
                                fieldI.Components.TryGetValue(componentName, out compI))
                            {
                                foreach (var entryName in compR.Entries.Keys)
                                {
                                    entryMag = new HistoryResultEntries(entryName, false);
                                    entryPha = new HistoryResultEntries(entryName, false);
                                    //
                                    if (compR.Entries.TryGetValue(entryName, out entryR) &&
                                        compI.Entries.TryGetValue(entryName, out entryI))
                                    {
                                        real = entryR.Values.ToArray();
                                        imaginary = entryI.Values.ToArray();
                                        mag = new double[Math.Min(real.Length, imaginary.Length)];
                                        pha = new double[mag.Length];
                                        for (int i = 0; i < mag.Length; i++)
                                        {
                                            mag[i] = Tools.GetComplexMagnitude(real[i], imaginary[i]);
                                            pha[i] = Tools.GetComplexPhaseDeg(real[i], imaginary[i]);
                                        }
                                        //
                                        entryMag.Time = entryR.Time;
                                        entryMag.Values = mag.ToList();
                                        compMag.Entries.Add(entryMag.Name, entryMag);
                                        //
                                        entryPha.Time = entryR.Time;
                                        entryPha.Values = pha.ToList();
                                        compPha.Entries.Add(entryPha.Name, entryPha);
                                    }
                                }
                            }
                            fieldMag.Components.Add(compMag.Name, compMag);
                            fieldPha.Components.Add(compPha.Name, compPha);
                        }
                        //
                        setsEntry.Value.Fields.Add(fieldMag.Name, fieldMag);
                        setsEntry.Value.Fields.Add(fieldPha.Name, fieldPha);
                    }
                }
            }
        }
        static private void AddStressComponents(HistoryResults historyOutput)
        {
            HistoryResultComponent vonMisesCom;
            HistoryResultComponent trescaCom;
            HistoryResultComponent sgnMaxAbsPrinCom;
            HistoryResultComponent prinMaxCom;
            HistoryResultComponent prinMidCom;
            HistoryResultComponent prinMinCom;
            //
            foreach (var setsEntry in historyOutput.Sets)
            {
                foreach (var fieldEntry in setsEntry.Value.Fields)
                {
                    string noSuffixName = HOFieldNames.GetNoSuffixName(fieldEntry.Key);
                    //
                    if (noSuffixName == HOFieldNames.Stresses &&
                        (noSuffixName == fieldEntry.Key || HOFieldNames.HasRealComplexSuffix(fieldEntry.Key)))
                    {
                        vonMisesCom = new HistoryResultComponent(HOComponentNames.Mises);
                        trescaCom = new HistoryResultComponent(HOComponentNames.Tresca);
                        sgnMaxAbsPrinCom = new HistoryResultComponent(HOComponentNames.SgnMaxAbsPri);
                        prinMaxCom = new HistoryResultComponent(HOComponentNames.PrincipalMax);
                        prinMidCom = new HistoryResultComponent(HOComponentNames.PrincipalMid);
                        prinMinCom = new HistoryResultComponent(HOComponentNames.PrincipalMin);
                        //
                        string[] entryNames = fieldEntry.Value.Components[HOComponentNames.S11].Entries.Keys.ToArray();
                        double[][] values = new double[6][];
                        double[] vmArray;
                        double[] trescaArray;
                        double[] sgnMaxAbsPrinArray;
                        double[] prinMaxArray;
                        double[] prinMidArray;
                        double[] prinMinArray;
                        HistoryResultEntries hrEntries;
                        //
                        double s11;
                        double s22;
                        double s33;
                        double s12;
                        double s23;
                        double s31;
                        double I1;
                        double I2;
                        double I3;
                        double sp1, sp2, sp3;                        
                        //
                        foreach (var entryName in entryNames)
                        {
                            values[0] = fieldEntry.Value.Components[HOComponentNames.S11].Entries[entryName].Values.ToArray();
                            values[1] = fieldEntry.Value.Components[HOComponentNames.S22].Entries[entryName].Values.ToArray();
                            values[2] = fieldEntry.Value.Components[HOComponentNames.S33].Entries[entryName].Values.ToArray();
                            values[3] = fieldEntry.Value.Components[HOComponentNames.S12].Entries[entryName].Values.ToArray();
                            values[4] = fieldEntry.Value.Components[HOComponentNames.S23].Entries[entryName].Values.ToArray();
                            values[5] = fieldEntry.Value.Components[HOComponentNames.S13].Entries[entryName].Values.ToArray();
                            //
                            vmArray = new double[values[0].Length];
                            trescaArray = new double[values[0].Length];
                            sgnMaxAbsPrinArray = new double[values[0].Length];
                            prinMaxArray = new double[values[0].Length];
                            prinMidArray = new double[values[0].Length];
                            prinMinArray = new double[values[0].Length];
                            //
                            for (int i = 0; i < vmArray.Length; i++)
                            {
                                vmArray[i] = Math.Sqrt(0.5 * (
                                                               Math.Pow(values[0][i] - values[1][i], 2)
                                                             + Math.Pow(values[1][i] - values[2][i], 2)
                                                             + Math.Pow(values[2][i] - values[0][i], 2)
                                                             + 6 * (
                                                                      Math.Pow(values[3][i], 2)
                                                                    + Math.Pow(values[4][i], 2)
                                                                    + Math.Pow(values[5][i], 2)
                                                                   )
                                                            )
                                                     );
                                //
                                s11 = values[0][i];
                                s22 = values[1][i];
                                s33 = values[2][i];
                                s12 = values[3][i];
                                s23 = values[4][i];
                                s31 = values[5][i];
                                //
                                I1 = s11 + s22 + s33;
                                I2 = s11 * s22 + s22 * s33 + s33 * s11 -
                                     Math.Pow(s12, 2.0) - Math.Pow(s23, 2.0) - Math.Pow(s31, 2.0);
                                I3 = s11 * s22 * s33 - s11 * Math.Pow(s23, 2.0) -
                                     s22 * Math.Pow(s31, 2.0) - s33 * Math.Pow(s12, 2.0) + 2.0 * s12 * s23 * s31;
                                //
                                sp1 = sp2 = sp3 = 0;
                                Tools.SolveQubicEquationDepressedCubic(1.0, -I1, I2, -I3, ref sp1, ref sp2, ref sp3);
                                Tools.Sort3_descending(ref sp1, ref sp2, ref sp3);
                                //
                                if (double.IsNaN(sp1)) sp1 = 0;
                                if (double.IsNaN(sp2)) sp2 = 0;
                                if (double.IsNaN(sp3)) sp3 = 0;
                                //
                                sgnMaxAbsPrinArray[i] = Math.Abs(sp1) > Math.Abs(sp3) ? (float)sp1 : (float)sp3;
                                prinMaxArray[i] = sp1;
                                prinMidArray[i] = sp2;
                                prinMinArray[i] = sp3;
                                //
                                trescaArray[i] = sp1 - sp3;
                            }
                            //
                            hrEntries = new HistoryResultEntries(entryName, false);
                            hrEntries.Time = fieldEntry.Value.Components[HOComponentNames.S11].Entries[entryName].Time;
                            hrEntries.Values = vmArray.ToList();
                            vonMisesCom.Entries.Add(entryName, hrEntries);
                            //
                            hrEntries = new HistoryResultEntries(entryName, false);
                            hrEntries.Time = fieldEntry.Value.Components[HOComponentNames.S11].Entries[entryName].Time;
                            hrEntries.Values = trescaArray.ToList();
                            trescaCom.Entries.Add(entryName, hrEntries);
                            //
                            hrEntries = new HistoryResultEntries(entryName, false);
                            hrEntries.Time = fieldEntry.Value.Components[HOComponentNames.S11].Entries[entryName].Time;
                            hrEntries.Values = sgnMaxAbsPrinArray.ToList();
                            sgnMaxAbsPrinCom.Entries.Add(entryName, hrEntries);
                            //
                            hrEntries = new HistoryResultEntries(entryName, false);
                            hrEntries.Time = fieldEntry.Value.Components[HOComponentNames.S11].Entries[entryName].Time;
                            hrEntries.Values = prinMaxArray.ToList();
                            prinMaxCom.Entries.Add(entryName, hrEntries);
                            //
                            hrEntries = new HistoryResultEntries(entryName, false);
                            hrEntries.Time = fieldEntry.Value.Components[HOComponentNames.S11].Entries[entryName].Time;
                            hrEntries.Values = prinMidArray.ToList();
                            prinMidCom.Entries.Add(entryName, hrEntries);
                            //
                            hrEntries = new HistoryResultEntries(entryName, false);
                            hrEntries.Time = fieldEntry.Value.Components[HOComponentNames.S11].Entries[entryName].Time;
                            hrEntries.Values = prinMinArray.ToList();
                            prinMinCom.Entries.Add(entryName, hrEntries);
                        }
                        //
                        fieldEntry.Value.Components.Insert(0, vonMisesCom.Name, vonMisesCom);
                        fieldEntry.Value.Components.Insert(1, trescaCom.Name, trescaCom);
                        fieldEntry.Value.Components.Add(sgnMaxAbsPrinCom.Name, sgnMaxAbsPrinCom);
                        fieldEntry.Value.Components.Add(prinMaxCom.Name, prinMaxCom);
                        fieldEntry.Value.Components.Add(prinMidCom.Name, prinMidCom);
                        fieldEntry.Value.Components.Add(prinMinCom.Name, prinMinCom);
                    }
                }
            }
        }
        static private void AddStrainComponents(HistoryResults historyOutput)
        {
            foreach (var setsEntry in historyOutput.Sets)
            {
                foreach (var fieldEntry in setsEntry.Value.Fields)
                {
                    string noSuffixName = HOFieldNames.GetNoSuffixName(fieldEntry.Key);
                    //
                    if ((noSuffixName == HOFieldNames.Strains || noSuffixName == HOFieldNames.MechanicalStrains) &&
                        (noSuffixName == fieldEntry.Key || HOFieldNames.HasRealComplexSuffix(fieldEntry.Key)))
                    {
                        HistoryResultComponent vonMisesCom = new HistoryResultComponent(HOComponentNames.Mises);
                        HistoryResultComponent trescaCom = new HistoryResultComponent(HOComponentNames.Tresca);
                        HistoryResultComponent sgnMaxAbsPrinCom = new HistoryResultComponent(HOComponentNames.SgnMaxAbsPri);
                        HistoryResultComponent prinMaxCom = new HistoryResultComponent(HOComponentNames.PrincipalMax);
                        HistoryResultComponent prinMidCom = new HistoryResultComponent(HOComponentNames.PrincipalMid);
                        HistoryResultComponent prinMinCom = new HistoryResultComponent(HOComponentNames.PrincipalMin);
                        //
                        string[] entryNames = fieldEntry.Value.Components[HOComponentNames.E11].Entries.Keys.ToArray();
                        double[][] values = new double[6][];
                        double[] vmArray;
                        double[] trescaArray;
                        double[] sgnMaxAbsPrinArray;
                        double[] prinMaxArray;
                        double[] prinMidArray;
                        double[] prinMinArray;
                        HistoryResultEntries hrEntries;
                        //
                        double e11;
                        double e22;
                        double e33;
                        double e12;
                        double e23;
                        double e31;
                        double I1;
                        double I2;
                        double I3;
                        double ep1, ep2, ep3;
                        //
                        foreach (var entryName in entryNames)
                        {
                            values[0] = fieldEntry.Value.Components[HOComponentNames.E11].Entries[entryName].Values.ToArray();
                            values[1] = fieldEntry.Value.Components[HOComponentNames.E22].Entries[entryName].Values.ToArray();
                            values[2] = fieldEntry.Value.Components[HOComponentNames.E33].Entries[entryName].Values.ToArray();
                            values[3] = fieldEntry.Value.Components[HOComponentNames.E12].Entries[entryName].Values.ToArray();
                            values[4] = fieldEntry.Value.Components[HOComponentNames.E23].Entries[entryName].Values.ToArray();
                            values[5] = fieldEntry.Value.Components[HOComponentNames.E13].Entries[entryName].Values.ToArray();
                            //
                            vmArray = new double[values[0].Length];
                            trescaArray = new double[values[0].Length];
                            sgnMaxAbsPrinArray = new double[values[0].Length];
                            prinMaxArray = new double[values[0].Length];
                            prinMidArray = new double[values[0].Length];
                            prinMinArray = new double[values[0].Length];
                            //
                            for (int i = 0; i < values[0].Length; i++)
                            {
                                vmArray[i] = Math.Sqrt(0.5 * (
                                                              Math.Pow(values[0][i] - values[1][i], 2)
                                                            + Math.Pow(values[1][i] - values[2][i], 2)
                                                            + Math.Pow(values[2][i] - values[0][i], 2)
                                                            + 6 * (
                                                                     Math.Pow(values[3][i], 2)
                                                                   + Math.Pow(values[4][i], 2)
                                                                   + Math.Pow(values[5][i], 2)
                                                                  )
                                                           )
                                                    );
                                //
                                e11 = values[0][i];
                                e22 = values[1][i];
                                e33 = values[2][i];
                                e12 = values[3][i];
                                e23 = values[4][i];
                                e31 = values[5][i];
                                //
                                I1 = e11 + e22 + e33;
                                I2 = e11 * e22 + e22 * e33 + e33 * e11 -
                                     Math.Pow(e12, 2.0) - Math.Pow(e23, 2.0) - Math.Pow(e31, 2.0);
                                I3 = e11 * e22 * e33 - e11 * Math.Pow(e23, 2.0) -
                                     e22 * Math.Pow(e31, 2.0) - e33 * Math.Pow(e12, 2.0) + 2.0 * e12 * e23 * e31;
                                //
                                ep1 = ep2 = ep3 = 0;
                                Tools.SolveQubicEquationDepressedCubic(1.0, -I1, I2, -I3, ref ep1, ref ep2, ref ep3);
                                Tools.Sort3_descending(ref ep1, ref ep2, ref ep3);
                                //
                                if (double.IsNaN(ep1)) ep1 = 0;
                                if (double.IsNaN(ep2)) ep2 = 0;
                                if (double.IsNaN(ep3)) ep3 = 0;
                                //
                                sgnMaxAbsPrinArray[i] = Math.Abs(ep1) > Math.Abs(ep3) ? (float)ep1 : (float)ep3;
                                prinMaxArray[i] = ep1;
                                prinMidArray[i] = ep2;
                                prinMinArray[i] = ep3;
                                //
                                trescaArray[i] = ep1 - ep3;
                            }
                            //
                            hrEntries = new HistoryResultEntries(entryName, false);
                            hrEntries.Time = fieldEntry.Value.Components[HOComponentNames.E11].Entries[entryName].Time;
                            hrEntries.Values = vmArray.ToList();
                            vonMisesCom.Entries.Add(entryName, hrEntries);
                            //
                            hrEntries = new HistoryResultEntries(entryName, false);
                            hrEntries.Time = fieldEntry.Value.Components[HOComponentNames.E11].Entries[entryName].Time;
                            hrEntries.Values = trescaArray.ToList();
                            trescaCom.Entries.Add(entryName, hrEntries);
                            //
                            hrEntries = new HistoryResultEntries(entryName, false);
                            hrEntries.Time = fieldEntry.Value.Components[HOComponentNames.E11].Entries[entryName].Time;
                            hrEntries.Values = sgnMaxAbsPrinArray.ToList();
                            sgnMaxAbsPrinCom.Entries.Add(entryName, hrEntries);
                            //
                            hrEntries = new HistoryResultEntries(entryName, false);
                            hrEntries.Time = fieldEntry.Value.Components[HOComponentNames.E11].Entries[entryName].Time;
                            hrEntries.Values = prinMaxArray.ToList();
                            prinMaxCom.Entries.Add(entryName, hrEntries);
                            //
                            hrEntries = new HistoryResultEntries(entryName, false);
                            hrEntries.Time = fieldEntry.Value.Components[HOComponentNames.E11].Entries[entryName].Time;
                            hrEntries.Values = prinMidArray.ToList();
                            prinMidCom.Entries.Add(entryName, hrEntries);
                            //
                            hrEntries = new HistoryResultEntries(entryName, false);
                            hrEntries.Time = fieldEntry.Value.Components[HOComponentNames.E11].Entries[entryName].Time;
                            hrEntries.Values = prinMinArray.ToList();
                            prinMinCom.Entries.Add(entryName, hrEntries);
                        }
                        //
                        fieldEntry.Value.Components.Insert(0, vonMisesCom.Name, vonMisesCom);
                        fieldEntry.Value.Components.Insert(1, trescaCom.Name, trescaCom);
                        fieldEntry.Value.Components.Add(sgnMaxAbsPrinCom.Name, sgnMaxAbsPrinCom);
                        fieldEntry.Value.Components.Add(prinMaxCom.Name, prinMaxCom);
                        fieldEntry.Value.Components.Add(prinMidCom.Name, prinMidCom);
                        fieldEntry.Value.Components.Add(prinMinCom.Name, prinMinCom);
                    }
                }
            }
        }
        static private void AddTotalEffectiveModalMassToMassRatio(HistoryResults historyOutput)
        {
            double[] effectiveModelMassTimes;
            double[] effectiveModelMassValues;
            double value;
            double totalModalMassValue;
            double totalMassValue;
            HistoryResultEntries entry;
            HistoryResultComponent effectiveModalMass;
            HistoryResultComponent totalModalMass;
            HistoryResultComponent totalMass;
            HistoryResultComponent relativeEffectiveModalMass;
            HistoryResultComponent relativeTotalMass;
            string[] entryNames = new string[] { HOComponentNames.XCOMPONENT,
                                                 HOComponentNames.YCOMPONENT,
                                                 HOComponentNames.ZCOMPONENT,
                                                 HOComponentNames.XROTATION,
                                                 HOComponentNames.YROTATION,
                                                 HOComponentNames.ZROTATION };
            //
            foreach (var setsEntry in historyOutput.Sets)
            {
                foreach (var fieldEntry in setsEntry.Value.Fields)
                {
                    if (fieldEntry.Key == HOFieldNames.Factor)
                    {
                        if (fieldEntry.Value.Components.TryGetValue(HOFieldNames.EffectiveModalMass, out effectiveModalMass) &&
                            fieldEntry.Value.Components.TryGetValue(HOFieldNames.TotalEffectiveModalMass, out totalModalMass) &&
                            fieldEntry.Value.Components.TryGetValue(HOFieldNames.TotalEffectiveMass, out totalMass))
                        {
                            relativeEffectiveModalMass = new HistoryResultComponent(HOFieldNames.RelativeEffectiveModalMass);
                            relativeTotalMass = new HistoryResultComponent(HOFieldNames.RelativeTotalEffectiveModalMass);
                            foreach (var entryName in entryNames)
                            {
                                effectiveModelMassValues = effectiveModalMass.Entries[entryName].Values.ToArray();
                                effectiveModelMassTimes = effectiveModalMass.Entries[entryName].Time.ToArray();
                                totalModalMassValue = totalModalMass.Entries[entryName].Values.ToArray()[0];
                                totalMassValue = totalMass.Entries[entryName].Values.ToArray()[0];
                                // Relative effective modal mass
                                entry = new HistoryResultEntries(entryName, false);
                                for (int i = 0; i < effectiveModelMassValues.Length; i++)
                                {
                                    if (totalMassValue != 0) value = effectiveModelMassValues[i] / totalMassValue;
                                    else value = 0;
                                    value = Tools.RoundToSignificantDigits(value, 6);
                                    entry.Add(effectiveModelMassTimes[i],value);
                                }
                                relativeEffectiveModalMass.Entries.Add(entry.Name, entry);
                                // Ratio
                                if (totalMassValue != 0) value = totalModalMassValue / totalMassValue;
                                else value = 0;
                                value = Tools.RoundToSignificantDigits(value, 6);
                                //
                                entry = new HistoryResultEntries(entryName, false);
                                entry.Add(1, value);
                                //
                                relativeTotalMass.Entries.Add(entry.Name, entry);
                            }
                            //
                            fieldEntry.Value.Components.Add(relativeEffectiveModalMass.Name, relativeEffectiveModalMass);
                            fieldEntry.Value.Components.Add(relativeTotalMass.Name, relativeTotalMass);
                        }
                    }
                }
            }
        }
        static private void MergeCyclicFrequencyHistory(HistoryResults historyOutput)
        {
            const string nodalDiameterSeparator = "_ND_";
            Dictionary<string, List<double[]>> spectrumRows = new Dictionary<string, List<double[]>>();
            Dictionary<string, HistoryResultComponent> axisDirections =
                new Dictionary<string, HistoryResultComponent>();
            Dictionary<string, int> firstSetIndices = new Dictionary<string, int>();
            HashSet<string> sourceSetNames = new HashSet<string>();
            int setIndex = 0;
            //
            foreach (var setEntry in historyOutput.Sets)
            {
                int separatorIndex = setEntry.Key.LastIndexOf(nodalDiameterSeparator, StringComparison.Ordinal);
                if (separatorIndex < 0)
                {
                    setIndex++;
                    continue;
                }
                //
                string baseSetName = setEntry.Key.Substring(0, separatorIndex);
                string nodalDiameterText = setEntry.Key.Substring(separatorIndex + nodalDiameterSeparator.Length);
                if (!int.TryParse(nodalDiameterText, out int setNodalDiameter) ||
                    !setEntry.Value.Fields.TryGetValue(HOFieldNames.Frequency, out HistoryResultField frequencyField) ||
                    !frequencyField.Components.TryGetValue(HOFieldNames.EigenvalueOutput,
                                                           out HistoryResultComponent eigenvalueOutput) ||
                    !eigenvalueOutput.Entries.TryGetValue(HOComponentNames.EIGENVALUE,
                                                          out HistoryResultEntries eigenvalueEntries) ||
                    !eigenvalueOutput.Entries.TryGetValue(HOComponentNames.OMEGA,
                                                          out HistoryResultEntries omegaEntries) ||
                    !eigenvalueOutput.Entries.TryGetValue(HOComponentNames.FREQUENCY,
                                                          out HistoryResultEntries frequencyEntries) ||
                    !eigenvalueOutput.Entries.TryGetValue(HOComponentNames.FREQUENCY_IM,
                                                          out HistoryResultEntries frequencyImEntries))
                {
                    setIndex++;
                    continue;
                }
                //
                HistoryResultEntries nodalDiameterEntries = null;
                eigenvalueOutput.Entries.TryGetValue(HOComponentNames.NODAL_DIAMETER, out nodalDiameterEntries);
                //
                Dictionary<double, double> turningDirections = new Dictionary<double, double>();
                if (setEntry.Value.Fields.TryGetValue(HOFieldNames.Rotation, out HistoryResultField rotationField))
                {
                    if (!axisDirections.ContainsKey(baseSetName) &&
                        rotationField.Components.TryGetValue(HOFieldNames.AxisDirection,
                                                             out HistoryResultComponent axisDirection))
                        axisDirections.Add(baseSetName, axisDirection);
                    //
                    if (rotationField.Components.TryGetValue(HOFieldNames.TurningDirection,
                                                             out HistoryResultComponent turningDirection) &&
                        turningDirection.Entries.TryGetValue(HOComponentNames.DIRECTION,
                                                             out HistoryResultEntries directionEntries))
                    {
                        int directionCount = Math.Min(directionEntries.Time.Count, directionEntries.Values.Count);
                        for (int i = 0; i < directionCount; i++)
                            turningDirections[directionEntries.Time[i]] = directionEntries.Values[i];
                    }
                }
                //
                if (!spectrumRows.TryGetValue(baseSetName, out List<double[]> rows))
                {
                    rows = new List<double[]>();
                    spectrumRows.Add(baseSetName, rows);
                    firstSetIndices.Add(baseSetName, setIndex);
                }
                //
                int rowCount = new int[] { eigenvalueEntries.Values.Count,
                                           omegaEntries.Values.Count,
                                           frequencyEntries.Values.Count,
                                           frequencyImEntries.Values.Count,
                                           frequencyEntries.Time.Count }.Min();
                for (int i = 0; i < rowCount; i++)
                {
                    double mode = frequencyEntries.Time[i];
                    double nodalDiameter = setNodalDiameter;
                    if (nodalDiameterEntries != null && nodalDiameterEntries.Values.Count > i)
                        nodalDiameter = nodalDiameterEntries.Values[i];
                    //
                    if (!turningDirections.TryGetValue(mode, out double direction)) direction = double.NaN;
                    rows.Add(new double[] { nodalDiameter,
                                            mode,
                                            eigenvalueEntries.Values[i],
                                            omegaEntries.Values[i],
                                            frequencyEntries.Values[i],
                                            frequencyImEntries.Values[i],
                                            direction });
                }
                sourceSetNames.Add(setEntry.Key);
                setIndex++;
            }
            //
            if (spectrumRows.Count == 0) return;
            //
            foreach (string sourceSetName in sourceSetNames)
            {
                HistoryResultSet sourceSet = historyOutput.Sets[sourceSetName];
                sourceSet.Fields.Remove(HOFieldNames.Frequency);
                sourceSet.Fields.Remove(HOFieldNames.Rotation);
            }
            foreach (string sourceSetName in sourceSetNames)
            {
                if (historyOutput.Sets[sourceSetName].Fields.Count == 0)
                    historyOutput.Sets.Remove(sourceSetName);
            }
            //
            foreach (var spectrumEntry in spectrumRows.OrderBy(entry => firstSetIndices[entry.Key]))
            {
                List<double[]> rows = spectrumEntry.Value;
                HistoryResultSet spectrumSet = new HistoryResultSet(spectrumEntry.Key);
                HistoryResultField frequencyField = new HistoryResultField(HOFieldNames.Frequency);
                HistoryResultComponent eigenvalueOutput =
                    new HistoryResultComponent(HOFieldNames.EigenvalueOutput);
                HistoryResultEntries modeEntries = new HistoryResultEntries(HOComponentNames.MODE, false);
                HistoryResultEntries nodalDiameterEntries =
                    new HistoryResultEntries(HOComponentNames.NODAL_DIAMETER, false);
                HistoryResultEntries eigenvalueEntries =
                    new HistoryResultEntries(HOComponentNames.EIGENVALUE, false);
                HistoryResultEntries omegaEntries = new HistoryResultEntries(HOComponentNames.OMEGA, false);
                HistoryResultEntries frequencyEntries =
                    new HistoryResultEntries(HOComponentNames.FREQUENCY, false);
                HistoryResultEntries frequencyImEntries =
                    new HistoryResultEntries(HOComponentNames.FREQUENCY_IM, false);
                HistoryResultEntries directionEntries =
                    new HistoryResultEntries(HOComponentNames.DIRECTION, false);
                bool hasTurningDirection = false;
                //
                for (int i = 0; i < rows.Count; i++)
                {
                    double resultId = i + 1;
                    modeEntries.Add(resultId, rows[i][1]);
                    nodalDiameterEntries.Add(resultId, rows[i][0]);
                    eigenvalueEntries.Add(resultId, rows[i][2]);
                    omegaEntries.Add(resultId, rows[i][3]);
                    frequencyEntries.Add(resultId, rows[i][4]);
                    frequencyImEntries.Add(resultId, rows[i][5]);
                    directionEntries.Add(resultId, rows[i][6]);
                    if (!double.IsNaN(rows[i][6])) hasTurningDirection = true;
                }
                //
                eigenvalueOutput.Entries.Add(modeEntries.Name, modeEntries);
                eigenvalueOutput.Entries.Add(nodalDiameterEntries.Name, nodalDiameterEntries);
                eigenvalueOutput.Entries.Add(eigenvalueEntries.Name, eigenvalueEntries);
                eigenvalueOutput.Entries.Add(omegaEntries.Name, omegaEntries);
                eigenvalueOutput.Entries.Add(frequencyEntries.Name, frequencyEntries);
                eigenvalueOutput.Entries.Add(frequencyImEntries.Name, frequencyImEntries);
                if (hasTurningDirection) eigenvalueOutput.Entries.Add(directionEntries.Name, directionEntries);
                frequencyField.Components.Add(eigenvalueOutput.Name, eigenvalueOutput);
                spectrumSet.Fields.Add(frequencyField.Name, frequencyField);
                //
                if (axisDirections.TryGetValue(spectrumEntry.Key, out HistoryResultComponent axisDirection))
                {
                    HistoryResultField rotationField = new HistoryResultField(HOFieldNames.Rotation);
                    rotationField.Components.Add(axisDirection.Name, axisDirection);
                    spectrumSet.Fields.Add(rotationField.Name, rotationField);
                }
                //
                if (historyOutput.Sets.TryGetValue(spectrumSet.Name, out HistoryResultSet existingSet))
                {
                    existingSet.AppendFields(spectrumSet);
                }
                else
                {
                    int insertIndex = 0;
                    string sourcePrefix = spectrumSet.Name + nodalDiameterSeparator;
                    foreach (var setEntry in historyOutput.Sets)
                    {
                        if (setEntry.Key.StartsWith(sourcePrefix, StringComparison.Ordinal)) break;
                        insertIndex++;
                    }
                    historyOutput.Sets.Insert(insertIndex, spectrumSet.Name, spectrumSet);
                }
            }
        }



    }
}
