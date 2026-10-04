// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Collections.Generic;

namespace CaeMesh
{
    /// <summary>
    /// What lp_solve returned for an interval assignment: its status, the counts when it found any, and the rows those
    /// counts leave unsatisfied - soft rows given up, or every row the counts break when the hard rows were relaxed.
    /// </summary>
    public class IntervalSolution
    {
        // Variables
        private readonly int _status;
        private readonly Dictionary<int, int> _edgeIdNumOfElements;
        private readonly List<IntervalRow> _violatedRows;
        private readonly double _objective;
        private readonly double _seconds;


        // Properties
        public int Status { get { return _status; } }
        public bool HasCounts { get { return _edgeIdNumOfElements != null; } }
        public Dictionary<int, int> EdgeIdNumOfElements { get { return _edgeIdNumOfElements; } }
        public List<IntervalRow> ViolatedRows { get { return _violatedRows; } }
        public double Objective { get { return _objective; } }
        public double Seconds { get { return _seconds; } }
        public string StatusName
        {
            get
            {
                switch (_status)
                {
                    case LpSolve.Optimal: return "optimal";
                    case LpSolve.SubOptimal: return "suboptimal";
                    case LpSolve.Infeasible: return "infeasible";
                    case LpSolve.Unbounded: return "unbounded";
                    case LpSolve.Timeout: return "timeout";
                    default: return "status " + _status;
                }
            }
        }


        // Constructors
        public IntervalSolution(int status, Dictionary<int, int> edgeIdNumOfElements, List<IntervalRow> violatedRows,
                                double objective, double seconds)
        {
            _status = status;
            _edgeIdNumOfElements = edgeIdNumOfElements;
            _violatedRows = violatedRows ?? new List<IntervalRow>();
            _objective = objective;
            _seconds = seconds;
        }
    }
}
