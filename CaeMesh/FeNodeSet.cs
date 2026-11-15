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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeMesh
{
    [Serializable]
    [PmxType(2078)]
    public class FeNodeSet : FeGroup
    {
        // Variables                                                                                                                
        [PmxKey(0)] private Selection _creationData;
        [PmxKey(1)] private int[] _creationIds;
        [PmxKey(2)] private double[] _centerOfGravity;
        [PmxKey(3)] private double[][] _boundingBox;


        // Properties                                                                                                               
        public Selection CreationData { get { return _creationData; } set { _creationData = value; } }
        public int[] CreationIds { get { return _creationIds; } set { _creationIds = value; } }
        public double[] CenterOfGravity { get { return _centerOfGravity; } set { _centerOfGravity = value; } }
        public double[][] BoundingBox { get { return _boundingBox; } set { _boundingBox = value; } }


        // Constructors                                                                                                             
        public FeNodeSet(string name, int[] labels) 
            : base(name, labels)
        {
            _creationData = null;
            _creationIds = null;
            _centerOfGravity = null;
            _boundingBox = null;
        }
        public FeNodeSet(string name, int[] labels, double[] centerOfGravity)
            : base(name, labels)
        {
            _creationData = null;
            _creationIds = null;
            _centerOfGravity = centerOfGravity;
            _boundingBox = null;
        }


        // Methods                                                                                                                  
        public int[] GetMaxNRandomNodeIds(int n)
        {
            if (Labels.Length < n) return Labels;
            else
            {
                List<int> notSelectedNodes = new List<int>(Labels);
                int[] selectedNodes = new int[n];
                Random rand = new Random();
                int id;
                for (int i = 0; i < n; i++)
                {
                    id = rand.Next(0, notSelectedNodes.Count - 1);
                    selectedNodes[i] = notSelectedNodes[id];
                    notSelectedNodes.RemoveAt(id);
                }
                return selectedNodes.ToArray();
            }
        }
    }
}
