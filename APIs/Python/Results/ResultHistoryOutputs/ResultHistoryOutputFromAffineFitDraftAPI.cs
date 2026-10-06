// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;

namespace APIs.Python
{
    public class ResultHistoryOutputFromAffineFitDraftAPI : ResultHistoryOutputDraftAPI
    {
        // Variables
        private readonly double _centerX;
        private readonly double _centerY;
        private readonly double _radius;
        private double _taperStartFraction;
        private AffineFitPointSourceEnum _pointSource;
        private AffineFitWeightingEnum _weighting;
        private double _cellAreaToleranceFraction;
        private double _areaFilterRelativeMedian;
        private AffineFitStrainMeasureEnum _strainMeasure;
        private double _loadingDirectionX;
        private double _loadingDirectionY;
        private int _stepId;
        private int _incrementId;
        private int _radiusSweepStepCount;


        // Properties
        [PythonAPI("The centre of the fitted circle as [x, y].", Returns = "list[float]")]
        public double[] center { get { return new double[] { _centerX, _centerY }; } }
        [PythonAPI("The radius of the fitted circle.", Returns = "float")]
        public double radius { get { return _radius; } }
        [PythonAPI("The fraction of the radius at which the taper starts.", Returns = "float")]
        public double taper_start_fraction { get { return _taperStartFraction; } set { _taperStartFraction = value; } }
        [PythonAPI("Where the fitted points come from: CellCentroids or Nodes.", Returns = "str")]
        public string point_source { get { return _pointSource.ToString(); } }
        [PythonAPI("How the points are weighted: Area or None.", Returns = "str")]
        public string weighting { get { return _weighting.ToString(); } }
        [PythonAPI("The cell area tolerance, as a fraction.", Returns = "float")]
        public double cell_area_tolerance_fraction
        {
            get { return _cellAreaToleranceFraction; }
            set { _cellAreaToleranceFraction = value; }
        }
        [PythonAPI("The area filter, relative to the median cell area.", Returns = "float")]
        public double area_filter_relative_median
        {
            get { return _areaFilterRelativeMedian; }
            set { _areaFilterRelativeMedian = value; }
        }
        [PythonAPI("The strain measure: Linear or Logarithmic.", Returns = "str")]
        public string strain_measure { get { return _strainMeasure.ToString(); } }
        [PythonAPI("The loading direction as [x, y].", Returns = "list[float]")]
        public object loading_direction
        {
            get { return new double[] { _loadingDirectionX, _loadingDirectionY }; }
            set
            {
                double[] direction = ResultHistoryOutputService.ToPlanarVector(value, "loading_direction");
                _loadingDirectionX = direction[0];
                _loadingDirectionY = direction[1];
            }
        }
        [PythonAPI("The step the fit is evaluated in, or -1 for every step.", Returns = "int")]
        public int step_id { get { return _stepId; } set { _stepId = value; } }
        [PythonAPI("The increment the fit is evaluated in, or -1 for every increment.", Returns = "int")]
        public int increment_id { get { return _incrementId; } set { _incrementId = value; } }
        [PythonAPI("The number of radii the fit sweeps through.", Returns = "int")]
        public int radius_sweep_step_count
        {
            get { return _radiusSweepStepCount; }
            set { _radiusSweepStepCount = value; }
        }


        // Constructors
        internal ResultHistoryOutputFromAffineFitDraftAPI(string name, double centerX, double centerY,
                                                          double radius, double taperStartFraction,
                                                          AffineFitPointSourceEnum pointSource,
                                                          AffineFitWeightingEnum weighting,
                                                          double cellAreaToleranceFraction,
                                                          double areaFilterRelativeMedian,
                                                          AffineFitStrainMeasureEnum strainMeasure,
                                                          double loadingDirectionX, double loadingDirectionY,
                                                          int stepId, int incrementId,
                                                          int radiusSweepStepCount)
            : base(name, null)
        {
            _centerX = centerX;
            _centerY = centerY;
            _radius = radius;
            _taperStartFraction = taperStartFraction;
            _pointSource = pointSource;
            _weighting = weighting;
            _cellAreaToleranceFraction = cellAreaToleranceFraction;
            _areaFilterRelativeMedian = areaFilterRelativeMedian;
            _strainMeasure = strainMeasure;
            _loadingDirectionX = loadingDirectionX;
            _loadingDirectionY = loadingDirectionY;
            _stepId = stepId;
            _incrementId = incrementId;
            _radiusSweepStepCount = radiusSweepStepCount;
        }


        // Methods
        protected override ResultHistoryOutput CreateHistoryOutput()
        {
            ResultHistoryOutputFromAffineFit output = new ResultHistoryOutputFromAffineFit(name);
            output.CenterX = _centerX;
            output.CenterY = _centerY;
            output.Radius = _radius;
            output.TaperStartFraction = _taperStartFraction;
            output.PointSource = _pointSource;
            output.Weighting = _weighting;
            output.CellAreaToleranceFraction = _cellAreaToleranceFraction;
            output.AreaFilterRelativeMedian = _areaFilterRelativeMedian;
            output.StrainMeasure = _strainMeasure;
            output.LoadingDirectionX = _loadingDirectionX;
            output.LoadingDirectionY = _loadingDirectionY;
            output.StepId = _stepId;
            output.StepIncrementId = _incrementId;
            output.RadiusSweepStepCount = _radiusSweepStepCount;
            //
            return output;
        }
        // The fit works on the whole result mesh rather than on a region of it
        protected override string GetRequiredTarget()
        {
            return null;
        }
        public override string ToString()
        {
            return "HistoryOutputFromAffineFitDraft(name=" + StringListAPI.ToDisplayString(name) +
                   ", radius=" + _radius + ")";
        }
    }
}
