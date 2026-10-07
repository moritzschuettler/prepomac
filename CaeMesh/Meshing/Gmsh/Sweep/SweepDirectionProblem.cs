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
using System.Linq;

namespace CaeMesh
{
    /// <summary>
    /// The choice of one way of sweeping every cell of a compound, so that the cells agree on every surface they
    /// share. A cell takes one of the ways it can be swept, or is left to tetrahedra. Two cells constrain each other
    /// only through the surfaces they share, so the problem is a graph of the cells with a table of the compatible
    /// pairs of choices on every edge, and a count of the surfaces on which a pair of choices continues one sweep
    /// into the next. The best assignment sweeps the most cells; among those, the one whose sweeps continue across
    /// the most shared surfaces - a sweep carried on from a target, two sweeps leaving one source, or two cells
    /// swept alongside along a wall - since a sweep that turns at a shared surface lays its layers across those of
    /// its neighbour; and among those the one whose ways score best. It is an integer program solved by lp_solve in
    /// three stages, one goal at a time, each stage holding what the stages before it reached: weighing the goals
    /// against each other in one objective would need weights apart by orders of magnitude, which the relaxation of
    /// the program bounds poorly. The score stage stops within one point of the best score there can be: proving
    /// which of many nearly equal scores is the highest costs far more than the difference is worth, and a gap,
    /// unlike a time limit, gives the same assignment on every computer. What no pair of cells shows - an order
    /// of meshing the chosen cells cannot meet - is excluded afterwards, as a combination of choices no assignment
    /// may contain, and the problem is solved again.
    /// </summary>
    public class SweepDirectionProblem
    {
        // Variables
        private readonly double[][] _scores;
        private readonly Dictionary<long, bool[,]> _compatibility;
        private readonly Dictionary<long, int[,]> _continuity;
        private readonly List<int[][]> _exclusions;
        private readonly int _timeoutSeconds;
        private const double _scoreGap = 1;


        // Properties
        /// <summary>
        /// Whether every stage of the last solve was solved to the end, so that its assignment is the best there is.
        /// </summary>
        public bool IsOptimal { get; private set; }


        // Constructors
        /// <summary>
        /// The scores of the ways every cell can be swept, higher being better. Choice k of cell i is its way k, and
        /// choice scores[i].Length leaves it to tetrahedra. Every stage of a solve may take the given time.
        /// </summary>
        public SweepDirectionProblem(double[][] scores, int timeoutSeconds = 20)
        {
            _scores = scores;
            _timeoutSeconds = timeoutSeconds;
            _compatibility = new Dictionary<long, bool[,]>();
            _continuity = new Dictionary<long, int[,]>();
            _exclusions = new List<int[][]>();
        }


        // Methods
        /// <summary>
        /// Which choices of two cells agree on a surface they share, compatible[choice of cellA, choice of cellB], and
        /// on which of them the two sweeps continue each other there, continuing[choice of cellA, choice of cellB].
        /// A second surface the two cells share narrows the agreement and adds to the continuity.
        /// </summary>
        public void AddSurface(int cellA, int cellB, bool[,] compatible, bool[,] continuing)
        {
            if (cellA == cellB) return;
            if (cellA > cellB)
            {
                AddSurface(cellB, cellA, Transpose(compatible), Transpose(continuing));
                return;
            }
            long key = GetKey(cellA, cellB);
            bool[,] existing;
            if (!_compatibility.TryGetValue(key, out existing))
            {
                existing = new bool[compatible.GetLength(0), compatible.GetLength(1)];
                for (int i = 0; i < compatible.GetLength(0); i++)
                {
                    for (int j = 0; j < compatible.GetLength(1); j++) existing[i, j] = true;
                }
                _compatibility.Add(key, existing);
                _continuity.Add(key, new int[compatible.GetLength(0), compatible.GetLength(1)]);
            }
            int[,] counts = _continuity[key];
            for (int i = 0; i < compatible.GetLength(0); i++)
            {
                for (int j = 0; j < compatible.GetLength(1); j++)
                {
                    existing[i, j] &= compatible[i, j];
                    if (continuing[i, j]) counts[i, j]++;
                }
            }
        }
        /// <summary>
        /// Excludes the combination of choices, choices[k] being the way of cells[k]: no assignment the solver finds
        /// gives all of these cells these ways at once.
        /// </summary>
        public void AddExclusion(int[] cells, int[] choices)
        {
            _exclusions.Add(cells.Select((cell, k) => new int[] { cell, choices[k] }).ToArray());
        }
        /// <summary>
        /// The choice of every cell - the index of its way, or -1 for tetrahedra - of the best assignment, or null when
        /// lp_solve finds none. The program has a binary column for every choice of every cell, one of them taken per
        /// cell, and for every pair of neighbours a column for every pair of their choices that agree: the pairs with
        /// one choice of one cell sum to that choice, so a choice is taken only with an agreeing choice of every
        /// neighbour, and the continuing surfaces are read off the pairs taken. Unlike columns held below both choices,
        /// this keeps the relaxation of the program close to the integers. A row excludes every excluded combination.
        /// The stages maximize the swept cells, then the continuing surfaces, then the score, each holding the result
        /// of the stages before.
        /// </summary>
        public int[] Solve()
        {
            int numOfCells = _scores.Length;
            int[][] columns = new int[numOfCells][];
            int numOfColumns = 0;
            for (int i = 0; i < numOfCells; i++)
            {
                columns[i] = new int[_scores[i].Length + 1];
                for (int k = 0; k <= _scores[i].Length; k++) columns[i][k] = ++numOfColumns;
            }
            int numOfChoiceColumns = numOfColumns;
            List<Tuple<int[], double[], int, double>> rows = new List<Tuple<int[], double[], int, double>>();
            for (int i = 0; i < numOfCells; i++)
                rows.Add(GetRow(columns[i], columns[i].Select(c => 1.0).ToArray(), LpSolve.EQ, 1));
            // The goals, column by column
            Dictionary<int, double> sweptCells = new Dictionary<int, double>();
            Dictionary<int, double> continuity = new Dictionary<int, double>();
            Dictionary<int, double> score = new Dictionary<int, double>();
            for (int i = 0; i < numOfCells; i++)
            {
                for (int k = 0; k < _scores[i].Length; k++)
                {
                    sweptCells.Add(columns[i][k], 1);
                    if (_scores[i][k] != 0) score.Add(columns[i][k], _scores[i][k]);
                }
            }
            foreach (var entry in _compatibility)
            {
                int cellA = (int)(entry.Key >> 32);
                int cellB = (int)(entry.Key & 0xFFFFFFFF);
                bool[,] compatible = entry.Value;
                int[,] counts = _continuity[entry.Key];
                int[,] pairColumns = new int[compatible.GetLength(0), compatible.GetLength(1)];
                for (int a = 0; a < compatible.GetLength(0); a++)
                {
                    for (int b = 0; b < compatible.GetLength(1); b++)
                    {
                        if (!compatible[a, b]) continue;
                        pairColumns[a, b] = ++numOfColumns;
                        if (counts[a, b] > 0) continuity.Add(pairColumns[a, b], counts[a, b]);
                    }
                }
                for (int a = 0; a < compatible.GetLength(0); a++)
                {
                    int[] pairs = Enumerable.Range(0, compatible.GetLength(1)).Where(b => compatible[a, b])
                                            .Select(b => pairColumns[a, b]).ToArray();
                    rows.Add(GetMarginalRow(columns[cellA][a], pairs));
                }
                for (int b = 0; b < compatible.GetLength(1); b++)
                {
                    int[] pairs = Enumerable.Range(0, compatible.GetLength(0)).Where(a => compatible[a, b])
                                            .Select(a => pairColumns[a, b]).ToArray();
                    rows.Add(GetMarginalRow(columns[cellB][b], pairs));
                }
            }
            foreach (var exclusion in _exclusions)
            {
                int[] excludedColumns = exclusion.Select(cellChoice => columns[cellChoice[0]][cellChoice[1]]).ToArray();
                rows.Add(GetRow(excludedColumns, excludedColumns.Select(c => 1.0).ToArray(), LpSolve.LE,
                                excludedColumns.Length - 1));
            }
            //
            IntPtr lp = LpSolve.make_lp(0, numOfColumns);
            if (lp == IntPtr.Zero) return null;
            try
            {
                LpSolve.set_verbose(lp, LpSolve.Critical);
                LpSolve.set_add_rowmode(lp, 1);
                foreach (var row in rows)
                    LpSolve.add_constraintex(lp, row.Item1.Length, row.Item2, row.Item1, row.Item3, row.Item4);
                LpSolve.set_add_rowmode(lp, 0);
                for (int column = 1; column <= numOfColumns; column++)
                {
                    LpSolve.set_bounds(lp, column, 0, 1);
                    if (column <= numOfChoiceColumns) LpSolve.set_int(lp, column, 1);
                }
                LpSolve.set_minim(lp);
                LpSolve.set_timeout(lp, _timeoutSeconds);
                //
                IsOptimal = true;
                double[] solution = null;
                foreach (var goal in new Dictionary<int, double>[] { sweptCells, continuity, score })
                {
                    if (goal.Count == 0) continue;
                    // The goal is maximized, lp_solve minimizing its negative
                    int[] goalColumns = goal.Keys.ToArray();
                    LpSolve.set_mip_gap(lp, 1, goal == score ? _scoreGap : 1E-9);
                    LpSolve.set_obj_fnex(lp, goalColumns.Length, goalColumns.Select(column => -goal[column]).ToArray(),
                                         goalColumns);
                    int status = LpSolve.solve(lp);
                    if (status != LpSolve.Optimal && status != LpSolve.SubOptimal) return solution != null
                        ? GetChoices(solution, columns) : null;
                    IsOptimal &= status == LpSolve.Optimal;
                    solution = new double[numOfColumns];
                    LpSolve.get_variables(lp, solution);
                    // The next stages hold what this one reached, short of rounding
                    double reached = goalColumns.Sum(column => goal[column] * solution[column - 1]);
                    double slack = goal == score ? 0 : 0.5;
                    double[] goalValues = goalColumns.Select(column => goal[column]).ToArray();
                    LpSolve.add_constraintex(lp, goalColumns.Length, goalValues, goalColumns, LpSolve.GE,
                                             reached - slack);
                }
                return solution != null ? GetChoices(solution, columns) : null;
            }
            finally
            {
                LpSolve.delete_lp(lp);
            }
        }
        private int[] GetChoices(double[] solution, int[][] columns)
        {
            int[] choices = new int[columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                int chosen = Enumerable.Range(0, columns[i].Length).OrderByDescending(k => solution[columns[i][k] - 1])
                                       .First();
                choices[i] = chosen == _scores[i].Length ? -1 : chosen;
            }
            return choices;
        }
        /// <summary>
        /// The row making the choice column equal to the sum of the pair columns it takes part in.
        /// </summary>
        private static Tuple<int[], double[], int, double> GetMarginalRow(int choiceColumn, int[] pairColumns)
        {
            int[] rowColumns = new int[] { choiceColumn }.Concat(pairColumns).ToArray();
            return GetRow(rowColumns, rowColumns.Select((c, k) => k == 0 ? 1.0 : -1.0).ToArray(), LpSolve.EQ, 0);
        }
        private static Tuple<int[], double[], int, double> GetRow(int[] columns, double[] values, int type,
                                                                  double rightHandSide)
        {
            return new Tuple<int[], double[], int, double>(columns, values, type, rightHandSide);
        }
        private static T[,] Transpose<T>(T[,] table)
        {
            T[,] transposed = new T[table.GetLength(1), table.GetLength(0)];
            for (int i = 0; i < table.GetLength(0); i++)
            {
                for (int j = 0; j < table.GetLength(1); j++) transposed[j, i] = table[i, j];
            }
            return transposed;
        }
        private static long GetKey(int cellA, int cellB)
        {
            return ((long)cellA << 32) | (uint)cellB;
        }
    }
}
