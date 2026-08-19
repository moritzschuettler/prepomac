// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeGlobals;

namespace CaeResults
{
    [Serializable]
    public enum AffineFitPointSourceEnum
    {
        Nodes,
        CellCentroids
    }

    [Serializable]
    public enum AffineFitWeightingEnum
    {
        Equal,
        Area,
        Perimeter
    }

    [Serializable]
    public enum AffineFitStrainMeasureEnum
    {
        Linear,
        Biot,
        Logarithmic
    }

    [Serializable]
    public class ResultHistoryOutputFromAffineFit : ResultHistoryOutput
    {
        // Variables
        private double _centerX;
        private double _centerY;
        private double _radius;
        private double _taperStartFraction;
        private AffineFitPointSourceEnum _pointSource;
        private AffineFitWeightingEnum _weighting;
        private double _cellAreaToleranceFraction;
        private double _areaFilterRelativeMedian;
        private AffineFitStrainMeasureEnum _strainMeasure;
        private double _loadingDirectionX;
        private double _loadingDirectionY;
        private int _stepId;
        private int _stepIncrementId;
        private int _radiusSweepStepCount;
        //
        private static readonly string[] _parentNames = new string[] { FOFieldNames.Disp };


        // Properties
        public double CenterX { get { return _centerX; } set { _centerX = value; } }
        public double CenterY { get { return _centerY; } set { _centerY = value; } }
        public double Radius { get { return _radius; } set { _radius = value; } }
        public double TaperStartFraction { get { return _taperStartFraction; } set { _taperStartFraction = value; } }
        public AffineFitPointSourceEnum PointSource { get { return _pointSource; } set { _pointSource = value; } }
        public AffineFitWeightingEnum Weighting { get { return _weighting; } set { _weighting = value; } }
        public double CellAreaToleranceFraction { get { return _cellAreaToleranceFraction; } set { _cellAreaToleranceFraction = value; } }
        public double AreaFilterRelativeMedian { get { return _areaFilterRelativeMedian; } set { _areaFilterRelativeMedian = value; } }
        public AffineFitStrainMeasureEnum StrainMeasure { get { return _strainMeasure; } set { _strainMeasure = value; } }
        public double LoadingDirectionX { get { return _loadingDirectionX; } set { _loadingDirectionX = value; } }
        public double LoadingDirectionY { get { return _loadingDirectionY; } set { _loadingDirectionY = value; } }
        public int StepId { get { return _stepId; } set { _stepId = value; } }
        public int StepIncrementId { get { return _stepIncrementId; } set { _stepIncrementId = value; } }
        public int RadiusSweepStepCount { get { return _radiusSweepStepCount; } set { _radiusSweepStepCount = value; } }
        //
        public static string AllSteps = "All";
        public static string FieldName = "AFFINE FIT";


        // Constructors
        public ResultHistoryOutputFromAffineFit(string name)
            : base(name, "", RegionTypeEnum.None)
        {
            _centerX = 0;
            _centerY = 0;
            _radius = 1;
            _taperStartFraction = 1;
            _pointSource = AffineFitPointSourceEnum.CellCentroids;
            _weighting = AffineFitWeightingEnum.Area;
            _cellAreaToleranceFraction = 0.2;
            _areaFilterRelativeMedian = 1;
            _strainMeasure = AffineFitStrainMeasureEnum.Linear;
            _loadingDirectionX = 0;
            _loadingDirectionY = 0;
            _stepId = -1;
            _stepIncrementId = -1;
            _radiusSweepStepCount = 1;
        }


        // Methods
        public override string[] GetParentNames()
        {
            return _parentNames;
        }
    }
}
