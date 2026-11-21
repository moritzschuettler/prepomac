// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
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
    /// target, and the rows tie the counts together. HiGHS finds the integer counts that keep every hard row and come
    /// closest to the targets. A count below its target is priced by how many times larger its elements are than asked
    /// for, a count above it by how many times more elements it has, times the logarithm of that number and _upWeight:
    /// a count above its target costs more than one below, for the coarser mesh. Every further element off the target
    /// costs more than the one before, so a side lowered or raised far is lowered or raised on all its edges: with one
    /// price for all the elements those of the longest edge were the cheapest to take, and a chain of edges lowered to
    /// one element on its longest edge while its short ones kept theirs. An edge is not lowered below the count that
    /// keeps its elements within the maximum element size, unless the counts that are fixed leave no other way: every
    /// element short of it costs more than giving up a structured row. A structured or a preferred row may be given up
    /// at a price; with the hard rows relaxed as well, at a far higher price, the counts that come back name the rows
    /// that cannot be kept.
    /// </summary>
    public class IntervalProblem
    {
        // Variables
        private const double _upWeight = 1.5;
        private const double _downWeight = 1.0;
        private const int _maxUpRatio = 64;
        private const int _priceResolution = 8;
        private const double _orderStep = 0.01;
        private const double _hardPenalty = 1E4;
        private const double _maxSizePenalty = 1E3;
        private const double _structuredPenalty = 1E2;
        private const double _preferredPenalty = 5;
        private readonly Func<int, int> _getTarget;
        private readonly Dictionary<int, int> _edgeIdTarget;
        private readonly Dictionary<int, int> _edgeIdFixedCount;
        private readonly Dictionary<int, double> _edgeIdWeight;
        private readonly Dictionary<int, double> _edgeIdDownWeight;
        private readonly Dictionary<int, int> _edgeIdMinimum;
        private readonly Dictionary<int, int> _edgeIdMaximum;
        private readonly Dictionary<int, int> _edgeIdMaxSizeCount;
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
            _edgeIdMaxSizeCount = new Dictionary<int, int>();
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
        /// <summary>
        /// The fewest elements that keep the elements of the edge within the maximum element size, which the edge is
        /// given fewer of only when the counts that are fixed leave no other way.
        /// </summary>
        public void SetMaxSizeCount(int edgeId, int numOfElements)
        {
            AddEdge(edgeId);
            _edgeIdMaxSizeCount[edgeId] = Math.Max(1, numOfElements);
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
        /// <summary>
        /// The side carries a total that is a multiple of the given number, an even one unless set.
        /// </summary>
        public void AddEvenSum(string name, IntervalRowTierEnum tier, int[] side, int multiple = 2)
        {
            AddRow(new IntervalRow(name, tier, IntervalRowKindEnum.EvenSum, new int[][] { side }, 0, 0, multiple));
        }
        public void AddAtLeastSum(string name, IntervalRowTierEnum tier, int[] side, int bound, int surfaceId = 0)
        {
            AddRow(new IntervalRow(name, tier, IntervalRowKindEnum.AtLeastSum, new int[][] { side }, surfaceId, bound));
        }
        public void AddAtLeastDifference(string name, IntervalRowTierEnum tier, int[] sideA, int[] sideB, int bound,
                                         int surfaceId = 0)
        {
            AddRow(new IntervalRow(name, tier, IntervalRowKindEnum.AtLeastDifference, new int[][] { sideA, sideB },
                                   surfaceId, bound));
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
            Dictionary<int, double> edgeIdOrderFactor = GetOrderFactors();
            foreach (var entry in _edgeIdTarget)
            {
                int difference = getCount(entry.Key) - entry.Value;
                double rest = Math.Abs(difference);
                foreach (var segment in GetSegments(entry.Key, entry.Value, difference > 0,
                                                    edgeIdOrderFactor[entry.Key]))
                {
                    if (rest <= 0) break;
                    double inSegment = Math.Min(rest, segment.Item1);
                    deviation += inSegment * segment.Item2;
                    rest -= inSegment;
                }
            }
            return deviation;
        }
        /// <summary>
        /// The price of a count of the edge. Below the target it is the number of times the elements are larger than
        /// asked for, less one. Above it, it is the number of times more elements the edge has, times its logarithm.
        /// Both start from nothing at the target with a price for the first element, and every further element costs
        /// more than the one before.
        /// </summary>
        private double GetPrice(int edgeId, int target, int count)
        {
            double ratio = (double)count / target;
            if (count < target) return GetWeight(edgeId) * GetDownWeight(edgeId) * _downWeight * (1 / ratio - 1);
            else return GetWeight(edgeId) * _upWeight * ratio * Math.Log(ratio);
        }
        /// <summary>
        /// The price of the counts from the target up or down as the solver gets it, in segments: the number of
        /// elements each segment holds and the price of one of them. A segment holds one element, and more only far
        /// from the target and from a single element, where it holds one _priceResolution-th of the distance to the
        /// nearer of them, so the edges of a side that are priced the same share its deviation to within that part.
        /// Above _maxUpRatio times the target the price of an element stays. Every price is multiplied by the order
        /// factor of the edge.
        /// </summary>
        private List<Tuple<double, double>> GetSegments(int edgeId, int target, bool above, double orderFactor)
        {
            List<Tuple<double, double>> segments = new List<Tuple<double, double>>();
            int count = target;
            int last = above ? target * _maxUpRatio : 1;
            double price = 0;
            while (count != last)
            {
                int distance = Math.Abs(count - target);
                int width = Math.Max(1, (above ? distance : Math.Min(distance, count - 1)) / _priceResolution);
                int next = above ? Math.Min(last, count + width) : Math.Max(last, count - width);
                price = (GetPrice(edgeId, target, next) - GetPrice(edgeId, target, count)) / Math.Abs(next - count);
                segments.Add(new Tuple<double, double>(Math.Abs(next - count), price * orderFactor));
                count = next;
            }
            if (above) segments.Add(new Tuple<double, double>(Highs.Infinity, price * orderFactor));
            return segments;
        }
        /// <summary>
        /// The factors that settle which of the edges priced the same gives way first: every edge costs up to
        /// _orderStep more by the place the rows name it at, which follows the faces and their loops and not the
        /// numbers of the edges. The place gives the square root of a prime of its own, less its whole part: no sum of
        /// such roots equals another one, so no two sets of counts cost the same and the counts do not depend on which
        /// of them the solver meets first.
        /// </summary>
        private Dictionary<int, double> GetOrderFactors()
        {
            List<int> edgeIds = new List<int>();
            HashSet<int> namedEdgeIds = new HashSet<int>();
            foreach (var row in _rows)
            {
                foreach (var side in row.Sides)
                {
                    foreach (var edgeId in side)
                    {
                        if (namedEdgeIds.Add(edgeId)) edgeIds.Add(edgeId);
                    }
                }
            }
            foreach (var edgeId in _edgeIdTarget.Keys)
            {
                if (namedEdgeIds.Add(edgeId)) edgeIds.Add(edgeId);
            }
            Dictionary<int, double> edgeIdOrderFactor = new Dictionary<int, double>();
            for (int number = 2; edgeIdOrderFactor.Count < edgeIds.Count; number++)
            {
                bool isPrime = true;
                for (int divisor = 2; isPrime && divisor * divisor <= number; divisor++) isPrime = number % divisor != 0;
                if (isPrime)
                {
                    edgeIdOrderFactor.Add(edgeIds[edgeIdOrderFactor.Count], 1 + _orderStep * (Math.Sqrt(number) % 1));
                }
            }
            return edgeIdOrderFactor;
        }
        public IntervalSolution Solve(bool relaxHardRows, int timeoutSeconds, string lpFileName = null)
        {
            Stopwatch watch = Stopwatch.StartNew();
            // Columns: the count of every edge with its deviation above and below the target, segment by segment, then
            // auxiliary integers and the slacks of the rows
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
                return lower.Count - 1;
            };
            List<Tuple<Dictionary<int, double>, double, double>> lpRows =
                new List<Tuple<Dictionary<int, double>, double, double>>();
            List<string> rowNames = new List<string>();
            //
            int fixedCount;
            int[] edgeIds = _edgeIdTarget.Keys.ToArray();
            Dictionary<int, int> edgeIdColumn = new Dictionary<int, int>();
            Dictionary<int, double> edgeIdOrderFactor = GetOrderFactors();
            foreach (var edgeId in edgeIds)
            {
                int target = _edgeIdTarget[edgeId];
                bool isFixed = _edgeIdFixedCount.TryGetValue(edgeId, out fixedCount);
                int minimum;
                if (!_edgeIdMinimum.TryGetValue(edgeId, out minimum)) minimum = 1;
                int maximum;
                double upperBound = _edgeIdMaximum.TryGetValue(edgeId, out maximum) ? Math.Max(minimum, maximum) : Highs.Infinity;
                int column = addColumn("n" + edgeId, isFixed ? fixedCount : minimum, isFixed ? fixedCount : upperBound, true, 0);
                Dictionary<int, double> targetRow = new Dictionary<int, double>() { { column, 1 } };
                List<Tuple<double, double>> segments = GetSegments(edgeId, target, true, edgeIdOrderFactor[edgeId]);
                for (int i = 0; i < segments.Count; i++)
                {
                    targetRow.Add(addColumn("up" + edgeId + "_" + i, 0, segments[i].Item1, false, segments[i].Item2),
                                  -1);
                }
                segments = GetSegments(edgeId, target, false, edgeIdOrderFactor[edgeId]);
                for (int i = 0; i < segments.Count; i++)
                {
                    targetRow.Add(addColumn("down" + edgeId + "_" + i, 0, segments[i].Item1, false, segments[i].Item2),
                                  1);
                }
                edgeIdColumn.Add(edgeId, column);
                rowNames.Add("target" + edgeId);
                lpRows.Add(new Tuple<Dictionary<int, double>, double, double>(targetRow, target, target));
                int maxSizeCount;
                if (!isFixed && _edgeIdMaxSizeCount.TryGetValue(edgeId, out maxSizeCount) && maxSizeCount > minimum)
                {
                    int shortColumn = addColumn("short" + edgeId, 0, Highs.Infinity, false, _maxSizePenalty);
                    rowNames.Add("maxSize" + edgeId);
                    lpRows.Add(new Tuple<Dictionary<int, double>, double, double>(
                        new Dictionary<int, double>() { { column, 1 }, { shortColumn, 1 } }, maxSizeCount,
                        Highs.Infinity));
                }
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
                bool atLeast = false;
                if (row.Kind == IntervalRowKindEnum.AtLeastSum)
                {
                    Dictionary<int, double> coefficients = new Dictionary<int, double>();
                    addSide(coefficients, row.Sides[0], 1);
                    rowCoefficients.Add(coefficients);
                    atLeast = true;
                }
                else if (row.Kind == IntervalRowKindEnum.AtLeastDifference)
                {
                    Dictionary<int, double> coefficients = new Dictionary<int, double>();
                    addSide(coefficients, row.Sides[0], 1);
                    addSide(coefficients, row.Sides[1], -1);
                    rowCoefficients.Add(coefficients);
                    atLeast = true;
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
                    coefficients.Add(addColumn("k" + r, 0, Highs.Infinity, true, 0), -row.Multiple);
                    rowCoefficients.Add(coefficients);
                }
                else
                {
                    // a = q + r, b = r + p, c = p + q
                    int[] blockColumns = Enumerable.Range(0, 3).Select(i => addColumn("b" + r + "_" + i, 1, Highs.Infinity, true, 0))
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
                        coefficients.Add(addColumn("plus" + r + "_" + i, 0, Highs.Infinity, false, penalty), 1);
                        coefficients.Add(addColumn("minus" + r + "_" + i, 0, Highs.Infinity, false, penalty), -1);
                    }
                    lpRows.Add(new Tuple<Dictionary<int, double>, double, double>(
                        coefficients, atLeast ? row.Bound : 0, atLeast ? Highs.Infinity : 0));
                    rowNames.Add("row" + r + (rowCoefficients.Count > 1 ? "_" + i : ""));
                }
            }
            //
            IntPtr highs = Highs.Create(timeoutSeconds);
            try
            {
                Highs.Check(Highs.Highs_addVars(highs, lower.Count, lower.ToArray(), upper.ToArray()));
                Highs.Check(Highs.Highs_changeColsCostByRange(highs, 0, cost.Count - 1, cost.ToArray()));
                int[] integrality = integer.Select(isInteger => isInteger ? Highs.Integer : Highs.Continuous).ToArray();
                Highs.Check(Highs.Highs_changeColsIntegralityByRange(highs, 0, integrality.Length - 1, integrality));
                Highs.Check(Highs.Highs_changeObjectiveSense(highs, Highs.Minimize));
                foreach (var lpRow in lpRows)
                {
                    int[] columns = lpRow.Item1.Where(entry => entry.Value != 0).Select(entry => entry.Key).ToArray();
                    double[] values = columns.Select(column => lpRow.Item1[column]).ToArray();
                    Highs.Check(Highs.Highs_addRow(highs, lpRow.Item2, lpRow.Item3, columns.Length, columns, values));
                }
                if (lpFileName != null)
                {
                    for (int i = 0; i < rowNames.Count; i++) Highs.Highs_passRowName(highs, i, rowNames[i]);
                    for (int i = 0; i < columnNames.Count; i++) Highs.Highs_passColName(highs, i, columnNames[i]);
                    Highs.Highs_writeModel(highs, lpFileName);
                    File.WriteAllLines(lpFileName + ".rows.txt", _rows.Select((row, index) => "row" + index + ": " +
                                                                                             row.Describe(id => _edgeIdTarget[id])));
                }
                //
                Highs.Highs_run(highs);
                int status = Highs.Highs_getModelStatus(highs);
                // No column has a negative price and every priced column starts at 0, so the problem is never unbounded
                if (status == Highs.UnboundedOrInfeasible) status = Highs.Infeasible;
                if (status != Highs.Optimal && !(status == Highs.TimeLimit && Highs.HasFeasibleSolution(highs)))
                    return new IntervalSolution(status, null, null, 0, watch.Elapsed.TotalSeconds);
                //
                double[] solution = Highs.GetColumnValues(highs, lower.Count);
                Dictionary<int, int> counts = edgeIds.ToDictionary(
                    edgeId => edgeId, edgeId => (int)Math.Round(solution[edgeIdColumn[edgeId]]));
                return new IntervalSolution(status, counts, GetViolatedRows(edgeId => counts[edgeId]),
                                            Highs.Highs_getObjectiveValue(highs), watch.Elapsed.TotalSeconds);
            }
            finally
            {
                Highs.Highs_destroy(highs);
            }
        }
    }
}
