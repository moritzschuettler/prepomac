// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CaeMesh
{
    /// <summary>
    /// The element counts of a set of edges, settled in one solve: every edge has the count the mesh size asks for, its
    /// target, and the rows tie the counts together. lp_solve finds the integer counts that keep every hard row and come
    /// closest to the targets, a count above its target costing more than one below, for the coarser mesh, and the
    /// deviation measured relative to the target. A structured or a preferred row may be given up at a price; with the
    /// hard rows relaxed as well, at a far higher price, the counts that come back name the rows that cannot be kept.
    /// </summary>
    public class IntervalProblem
    {
        // Variables
        private const double _upWeight = 1.5;
        private const double _downWeight = 1.0;
        private const double _hardPenalty = 1E4;
        private const double _structuredPenalty = 1E2;
        private const double _preferredPenalty = 5;
        private readonly Func<int, int> _getTarget;
        private readonly Dictionary<int, int> _edgeIdTarget;
        private readonly Dictionary<int, int> _edgeIdFixedCount;
        private readonly Dictionary<int, double> _edgeIdWeight;
        private readonly Dictionary<int, double> _edgeIdDownWeight;
        private readonly Dictionary<int, int> _edgeIdMinimum;
        private readonly Dictionary<int, int> _edgeIdMaximum;
        private readonly List<IntervalRow> _rows;


        // Properties
        public IEnumerable<int> EdgeIds { get { return _edgeIdTarget.Keys; } }
        public List<IntervalRow> Rows { get { return _rows; } }
        public Dictionary<int, int> EdgeIdTarget { get { return _edgeIdTarget; } }
        public Dictionary<int, int> EdgeIdFixedCount { get { return _edgeIdFixedCount; } }


        // Constructors
        public IntervalProblem(Func<int, int> getTarget)
        {
            _getTarget = getTarget;
            _edgeIdTarget = new Dictionary<int, int>();
            _edgeIdFixedCount = new Dictionary<int, int>();
            _edgeIdWeight = new Dictionary<int, double>();
            _edgeIdDownWeight = new Dictionary<int, double>();
            _edgeIdMinimum = new Dictionary<int, int>();
            _edgeIdMaximum = new Dictionary<int, int>();
            _rows = new List<IntervalRow>();
        }


        // Methods
        public void AddEdge(int edgeId)
        {
            if (!_edgeIdTarget.ContainsKey(edgeId)) _edgeIdTarget.Add(edgeId, Math.Max(1, _getTarget(edgeId)));
        }
        public void FixEdge(int edgeId, int numOfElements)
        {
            AddEdge(edgeId);
            _edgeIdFixedCount[edgeId] = Math.Max(1, numOfElements);
        }
        /// <summary>
        /// How much more a deviation from the target of the edge costs than that of another edge.
        /// </summary>
        public void SetWeight(int edgeId, double weight)
        {
            AddEdge(edgeId);
            _edgeIdWeight[edgeId] = weight;
        }
        /// <summary>
        /// How much more a count below the target of the edge costs than one below the target of another edge, on top
        /// of its weight.
        /// </summary>
        public void SetDownWeight(int edgeId, double weight)
        {
            AddEdge(edgeId);
            _edgeIdDownWeight[edgeId] = weight;
        }
        /// <summary>
        /// The fewest elements the edge may carry, unless its count is fixed.
        /// </summary>
        public void SetMinimum(int edgeId, int minimum)
        {
            AddEdge(edgeId);
            _edgeIdMinimum[edgeId] = Math.Max(1, minimum);
        }
        /// <summary>
        /// The most elements the edge may carry, unless its count is fixed.
        /// </summary>
        public void SetMaximum(int edgeId, int maximum)
        {
            AddEdge(edgeId);
            _edgeIdMaximum[edgeId] = Math.Max(1, maximum);
        }
        private double GetWeight(int edgeId)
        {
            double weight;
            return _edgeIdWeight.TryGetValue(edgeId, out weight) ? weight : 1;
        }
        private double GetDownWeight(int edgeId)
        {
            double weight;
            return _edgeIdDownWeight.TryGetValue(edgeId, out weight) ? weight : 1;
        }
        public void AddEqualSums(string name, IntervalRowTierEnum tier, int[] sideA, int[] sideB, int surfaceId = 0)
        {
            AddRow(new IntervalRow(name, tier, IntervalRowKindEnum.EqualSums, new int[][] { sideA, sideB }, surfaceId));
        }
        public void AddEvenSum(string name, IntervalRowTierEnum tier, int[] side)
        {
            AddRow(new IntervalRow(name, tier, IntervalRowKindEnum.EvenSum, new int[][] { side }));
        }
        public void AddAtLeastSum(string name, IntervalRowTierEnum tier, int[] side, int bound, int surfaceId = 0)
        {
            AddRow(new IntervalRow(name, tier, IntervalRowKindEnum.AtLeastSum, new int[][] { side }, surfaceId, bound));
        }
        public void AddThreeSided(string name, IntervalRowTierEnum tier, int[][] sides, int surfaceId = 0)
        {
            AddRow(new IntervalRow(name, tier, IntervalRowKindEnum.ThreeSided, sides, surfaceId));
        }
        private void AddRow(IntervalRow row)
        {
            foreach (var side in row.Sides)
            {
                foreach (var edgeId in side) AddEdge(edgeId);
            }
            _rows.Add(row);
        }
        public List<IntervalRow> GetViolatedRows(Func<int, int> getCount)
        {
            return _rows.Where(row => !row.IsSatisfied(getCount)).ToList();
        }
        /// <summary>
        /// The deviation of the counts from the targets the solve minimizes, without the price of the rows given up.
        /// </summary>
        public double GetDeviation(Func<int, int> getCount)
        {
            double deviation = 0;
            foreach (var entry in _edgeIdTarget)
            {
                int difference = getCount(entry.Key) - entry.Value;
                deviation += GetWeight(entry.Key) * (difference > 0 ? _upWeight * difference :
                                                     -GetDownWeight(entry.Key) * _downWeight * difference) / entry.Value;
            }
            return deviation;
        }
        public IntervalSolution Solve(bool relaxHardRows, int timeoutSeconds, string lpFileName = null)
        {
            Stopwatch watch = Stopwatch.StartNew();
            // Columns: the count of every edge with its deviation above and below the target, then the auxiliary
            // integers and the slacks of the rows
            List<double> lower = new List<double>();
            List<double> upper = new List<double>();
            List<bool> integer = new List<bool>();
            List<double> cost = new List<double>();
            List<string> columnNames = new List<string>();
            Func<string, double, double, bool, double, int> addColumn = (name, low, up, isInteger, price) =>
            {
                columnNames.Add(name);
                lower.Add(low);
                upper.Add(up);
                integer.Add(isInteger);
                cost.Add(price);
                return lower.Count;
            };
            List<Tuple<Dictionary<int, double>, int, double>> lpRows = new List<Tuple<Dictionary<int, double>, int, double>>();
            List<string> rowNames = new List<string>();
            //
            int fixedCount;
            int[] edgeIds = _edgeIdTarget.Keys.ToArray();
            Dictionary<int, int> edgeIdColumn = new Dictionary<int, int>();
            foreach (var edgeId in edgeIds)
            {
                int target = _edgeIdTarget[edgeId];
                bool isFixed = _edgeIdFixedCount.TryGetValue(edgeId, out fixedCount);
                int minimum;
                if (!_edgeIdMinimum.TryGetValue(edgeId, out minimum)) minimum = 1;
                int maximum;
                double upperBound = _edgeIdMaximum.TryGetValue(edgeId, out maximum) ? Math.Max(minimum, maximum) : LpSolve.Infinity;
                int column = addColumn("n" + edgeId, isFixed ? fixedCount : minimum, isFixed ? fixedCount : upperBound, true, 0);
                double weight = GetWeight(edgeId);
                int upColumn = addColumn("up" + edgeId, 0, LpSolve.Infinity, false, weight * _upWeight / target);
                int downColumn = addColumn("down" + edgeId, 0, LpSolve.Infinity, false,
                                           weight * GetDownWeight(edgeId) * _downWeight / target);
                edgeIdColumn.Add(edgeId, column);
                rowNames.Add("target" + edgeId);
                lpRows.Add(new Tuple<Dictionary<int, double>, int, double>(
                    new Dictionary<int, double>() { { column, 1 }, { upColumn, -1 }, { downColumn, 1 } }, LpSolve.EQ, target));
            }
            //
            Action<Dictionary<int, double>, int[], double> addSide = (coefficients, side, sign) =>
            {
                foreach (var edgeId in side)
                {
                    int column = edgeIdColumn[edgeId];
                    coefficients.TryGetValue(column, out double value);
                    coefficients[column] = value + sign;
                }
            };
            for (int r = 0; r < _rows.Count; r++)
            {
                IntervalRow row = _rows[r];
                double penalty;
                if (row.Tier == IntervalRowTierEnum.Hard) penalty = relaxHardRows ? _hardPenalty : -1;
                else if (row.Tier == IntervalRowTierEnum.Structured) penalty = _structuredPenalty;
                else penalty = _preferredPenalty;
                //
                List<Dictionary<int, double>> rowCoefficients = new List<Dictionary<int, double>>();
                int rowType = LpSolve.EQ;
                if (row.Kind == IntervalRowKindEnum.AtLeastSum)
                {
                    Dictionary<int, double> coefficients = new Dictionary<int, double>();
                    addSide(coefficients, row.Sides[0], 1);
                    rowCoefficients.Add(coefficients);
                    rowType = LpSolve.GE;
                }
                else if (row.Kind == IntervalRowKindEnum.EqualSums)
                {
                    Dictionary<int, double> coefficients = new Dictionary<int, double>();
                    addSide(coefficients, row.Sides[0], 1);
                    addSide(coefficients, row.Sides[1], -1);
                    rowCoefficients.Add(coefficients);
                }
                else if (row.Kind == IntervalRowKindEnum.EvenSum)
                {
                    Dictionary<int, double> coefficients = new Dictionary<int, double>();
                    addSide(coefficients, row.Sides[0], 1);
                    coefficients.Add(addColumn("k" + r, 0, LpSolve.Infinity, true, 0), -2);
                    rowCoefficients.Add(coefficients);
                }
                else
                {
                    // a = q + r, b = r + p, c = p + q
                    int[] blockColumns = Enumerable.Range(0, 3).Select(i => addColumn("b" + r + "_" + i, 1, LpSolve.Infinity, true, 0))
                                                .ToArray();
                    for (int i = 0; i < 3; i++)
                    {
                        Dictionary<int, double> coefficients = new Dictionary<int, double>();
                        addSide(coefficients, row.Sides[i], 1);
                        coefficients.Add(blockColumns[(i + 1) % 3], -1);
                        coefficients.Add(blockColumns[(i + 2) % 3], -1);
                        rowCoefficients.Add(coefficients);
                    }
                }
                for (int i = 0; i < rowCoefficients.Count; i++)
                {
                    Dictionary<int, double> coefficients = rowCoefficients[i];
                    if (penalty > 0)
                    {
                        coefficients.Add(addColumn("plus" + r + "_" + i, 0, LpSolve.Infinity, false, penalty), 1);
                        coefficients.Add(addColumn("minus" + r + "_" + i, 0, LpSolve.Infinity, false, penalty), -1);
                    }
                    lpRows.Add(new Tuple<Dictionary<int, double>, int, double>(coefficients, rowType,
                                                                               rowType == LpSolve.GE ? row.Bound : 0));
                    rowNames.Add("row" + r + (rowCoefficients.Count > 1 ? "_" + i : ""));
                }
            }
            //
            IntPtr lp;
            try
            {
                lp = LpSolve.make_lp(0, lower.Count);
            }
            catch (Exception ex) when (ex is DllNotFoundException || ex is BadImageFormatException ||
                                       ex is EntryPointNotFoundException)
            {
                throw new CaeException("lp_solve, which settles the numbers of elements on the edges, could not be loaded " +
                                       "from lpsolve55.dll next to GmshCaller.exe: " + ex.Message);
            }
            if (lp == IntPtr.Zero) throw new CaeException("lp_solve could not create a model.");
            try
            {
                LpSolve.set_verbose(lp, LpSolve.Critical);
                LpSolve.set_add_rowmode(lp, 1);
                int[] costColumns = Enumerable.Range(1, cost.Count).Where(column => cost[column - 1] != 0).ToArray();
                LpSolve.set_obj_fnex(lp, costColumns.Length, costColumns.Select(column => cost[column - 1]).ToArray(),
                                     costColumns);
                foreach (var lpRow in lpRows)
                {
                    int[] columns = lpRow.Item1.Where(entry => entry.Value != 0).Select(entry => entry.Key).ToArray();
                    double[] values = columns.Select(column => lpRow.Item1[column]).ToArray();
                    LpSolve.add_constraintex(lp, columns.Length, values, columns, lpRow.Item2, lpRow.Item3);
                }
                LpSolve.set_add_rowmode(lp, 0);
                for (int i = 0; i < rowNames.Count; i++) LpSolve.set_row_name(lp, i + 1, rowNames[i]);
                for (int column = 1; column <= lower.Count; column++)
                {
                    LpSolve.set_col_name(lp, column, columnNames[column - 1]);
                    LpSolve.set_bounds(lp, column, lower[column - 1], upper[column - 1]);
                    if (integer[column - 1]) LpSolve.set_int(lp, column, 1);
                }
                LpSolve.set_minim(lp);
                LpSolve.set_timeout(lp, timeoutSeconds);
                if (lpFileName != null)
                {
                    LpSolve.write_lp(lp, lpFileName);
                    File.WriteAllLines(lpFileName + ".rows.txt", _rows.Select((row, index) => "row" + index + ": " +
                                                                                             row.Describe(id => _edgeIdTarget[id])));
                }
                //
                int status = LpSolve.solve(lp);
                if (status != LpSolve.Optimal && status != LpSolve.SubOptimal)
                    return new IntervalSolution(status, null, null, 0, watch.Elapsed.TotalSeconds);
                //
                double[] solution = new double[lower.Count];
                LpSolve.get_variables(lp, solution);
                Dictionary<int, int> counts = edgeIds.ToDictionary(edgeId => edgeId,
                                                                   edgeId => (int)Math.Round(solution[edgeIdColumn[edgeId] - 1]));
                return new IntervalSolution(status, counts, GetViolatedRows(edgeId => counts[edgeId]),
                                            LpSolve.get_objective(lp), watch.Elapsed.TotalSeconds);
            }
            finally
            {
                LpSolve.delete_lp(lp);
            }
        }
    }
}
