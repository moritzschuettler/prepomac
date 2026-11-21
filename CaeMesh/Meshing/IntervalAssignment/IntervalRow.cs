// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Linq;

namespace CaeMesh
{
    /// <summary>
    /// One requirement of the interval assignment on the element counts of the edges, its sides being lists of edges
    /// whose counts are added up.
    /// </summary>
    public class IntervalRow
    {
        // Variables
        private readonly string _name;
        private readonly IntervalRowTierEnum _tier;
        private readonly IntervalRowKindEnum _kind;
        private readonly int[][] _sides;
        private readonly int _surfaceId;
        private readonly int _bound;
        private readonly int _multiple;


        // Properties
        public string Name { get { return _name; } }
        public IntervalRowTierEnum Tier { get { return _tier; } }
        public IntervalRowKindEnum Kind { get { return _kind; } }
        public int[][] Sides { get { return _sides; } }
        /// <summary>The surface whose grid the row keeps, or 0.</summary>
        public int SurfaceId { get { return _surfaceId; } }
        /// <summary>The fewest elements an AtLeastSum row lets its side carry, or an AtLeastDifference row its first side
        /// carry more than its second.</summary>
        public int Bound { get { return _bound; } }
        /// <summary>The number the total of an EvenSum row is a multiple of, 2 unless set.</summary>
        public int Multiple { get { return _multiple; } }


        // Constructors
        public IntervalRow(string name, IntervalRowTierEnum tier, IntervalRowKindEnum kind, int[][] sides,
                           int surfaceId = 0, int bound = 0, int multiple = 2)
        {
            _name = name;
            _tier = tier;
            _kind = kind;
            _sides = sides;
            _surfaceId = surfaceId;
            _bound = bound;
            _multiple = multiple;
        }


        // Methods
        public bool IsSatisfied(Func<int, int> getCount)
        {
            int[] sums = _sides.Select(side => side.Sum(getCount)).ToArray();
            if (_kind == IntervalRowKindEnum.EqualSums) return sums[0] == sums[1];
            if (_kind == IntervalRowKindEnum.EvenSum) return sums[0] % _multiple == 0;
            if (_kind == IntervalRowKindEnum.AtLeastSum) return sums[0] >= _bound;
            if (_kind == IntervalRowKindEnum.AtLeastDifference) return sums[0] - sums[1] >= _bound;
            int total = sums.Sum();
            return total % 2 == 0 && sums.All(sum => 2 * sum < total);
        }
        public string Describe(Func<int, int> getCount)
        {
            return _name + " [" + _tier + "]: " + DescribeSides(getCount);
        }
        public string DescribeSides(Func<int, int> getCount)
        {
            return string.Join(" | ", _sides.Select(side => string.Join(" + ", side.Select(id => "Edge " + id + "=" +
                                                                                                getCount(id))) +
                                                           " = " + side.Sum(getCount))) +
                   (_kind == IntervalRowKindEnum.AtLeastSum ? ", at least " + _bound : "") +
                   (_kind == IntervalRowKindEnum.AtLeastDifference ? ", the first at least " + _bound + " more" : "") +
                   (_kind == IntervalRowKindEnum.EvenSum && _multiple != 2 ? ", a multiple of " + _multiple : "");
        }
    }
}
