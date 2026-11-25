// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;

namespace CaeMesh
{
    /// <summary>
    /// What the mirror planes of a part ask of the balance of its element counts, all at the preferred tier, so that a
    /// mirrored mesh never costs a structured face or a number the user set. EqualSides are pairs of sides that carry
    /// as many elements: an edge and its image, and a split edge and its pieces together. MultipleSides are the edges
    /// a mirrored grid needs a sum on that is a multiple of the number given with them. An edge that is split unlike
    /// its image is counted by its pieces, which have negative ids: PieceTargets gives each the number it aims at,
    /// and the balance leaves the number it gets in PieceCounts.
    /// </summary>
    public class MirrorCountRows
    {
        // Variables                                                                                                    
        private readonly List<int[][]> _equalSides;
        private readonly List<Tuple<int[], int>> _multipleSides;
        private readonly Dictionary<int, int> _pieceTargets;
        private readonly Dictionary<int, int> _pieceCounts;


        // Properties                                                                                                   
        public List<int[][]> EqualSides { get { return _equalSides; } }
        public List<Tuple<int[], int>> MultipleSides { get { return _multipleSides; } }
        public Dictionary<int, int> PieceTargets { get { return _pieceTargets; } }
        public Dictionary<int, int> PieceCounts { get { return _pieceCounts; } }


        // Constructors                                                                                                 
        public MirrorCountRows()
        {
            _equalSides = new List<int[][]>();
            _multipleSides = new List<Tuple<int[], int>>();
            _pieceTargets = new Dictionary<int, int>();
            _pieceCounts = new Dictionary<int, int>();
        }
    }
}
