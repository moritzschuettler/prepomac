// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace CaeMesh
{
    /// <summary>
    /// What a row of the interval assignment asks of its sides. EqualSums: the two sides carry the same total.
    /// EvenSum: the one side carries an even total. ThreeSided: the three sides of a face meshed as three blocks of
    /// quads, a = q + r, b = r + p, c = p + q with p, q, r at least 1 - an even sum in which no side carries as many
    /// elements as the other two together. AtLeastSum: the one side carries at least the bound of the row. AtLeastDifference:
    /// the first side carries at least the bound of the row more than the second.
    /// </summary>
    public enum IntervalRowKindEnum
    {
        EqualSums,
        EvenSum,
        ThreeSided,
        AtLeastSum,
        AtLeastDifference
    }
}
