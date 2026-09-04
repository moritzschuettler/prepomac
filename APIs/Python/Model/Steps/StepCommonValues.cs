// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace APIs.Python.Model
{
    // The solution values every step type shares. A null value keeps what the step already carries, so
    // the same holder serves both the add and the replace methods.
    internal class StepCommonValues
    {
        // Variables
        private readonly object _nlgeom;
        private readonly object _maxIncrements;
        private readonly object _incrementationType;
        private readonly object _solverType;
        private readonly object _outputFrequency;
        private readonly object _perturbation;
        private readonly object _runAnalysis;


        // Properties
        public object Nlgeom { get { return _nlgeom; } }
        public object MaxIncrements { get { return _maxIncrements; } }
        public object IncrementationType { get { return _incrementationType; } }
        public object SolverType { get { return _solverType; } }
        public object OutputFrequency { get { return _outputFrequency; } }
        public object Perturbation { get { return _perturbation; } }
        public object RunAnalysis { get { return _runAnalysis; } }


        // Constructors
        public StepCommonValues(object nlgeom, object maxIncrements, object incrementationType, object solverType,
                                object outputFrequency, object perturbation, object runAnalysis)
        {
            _nlgeom = nlgeom;
            _maxIncrements = maxIncrements;
            _incrementationType = incrementationType;
            _solverType = solverType;
            _outputFrequency = outputFrequency;
            _perturbation = perturbation;
            _runAnalysis = runAnalysis;
        }
    }
}
