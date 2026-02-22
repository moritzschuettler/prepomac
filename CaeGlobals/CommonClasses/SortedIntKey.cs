using System;

namespace CaeGlobals
{
    public readonly struct SortedIntKey : IEquatable<SortedIntKey>
    {
        private readonly int _length;
        private readonly int _n0, _n1, _n2, _n3, _n4, _n5, _n6, _n7;

        public SortedIntKey(int[] nodes)
        {
            int n0 = 0, n1 = 0, n2 = 0, n3 = 0, n4 = 0, n5 = 0, n6 = 0, n7 = 0;
            int count = 0;

            // ---- Deduplicate (max 8 → O(n²) is fastest here) ----
            for (int i = 0; i < nodes.Length; i++)
            {
                int v = nodes[i];

                if ((count > 0 && n0 == v) ||
                    (count > 1 && n1 == v) ||
                    (count > 2 && n2 == v) ||
                    (count > 3 && n3 == v) ||
                    (count > 4 && n4 == v) ||
                    (count > 5 && n5 == v) ||
                    (count > 6 && n6 == v) ||
                    (count > 7 && n7 == v))
                    continue;

                switch (count)
                {
                    case 0: n0 = v; break;
                    case 1: n1 = v; break;
                    case 2: n2 = v; break;
                    case 3: n3 = v; break;
                    case 4: n4 = v; break;
                    case 5: n5 = v; break;
                    case 6: n6 = v; break;
                    case 7: n7 = v; break;
                }

                count++;
            }

            // ---- Sort (insertion sort on registers only) ----
            // We manually shift values instead of using an array.

            void Insert(ref int a, ref int b)
            {
                if (a > b)
                {
                    int t = a;
                    a = b;
                    b = t;
                }
            }

            if (count > 1) Insert(ref n0, ref n1);
            if (count > 2)
            {
                Insert(ref n1, ref n2);
                Insert(ref n0, ref n1);
            }
            if (count > 3)
            {
                Insert(ref n2, ref n3);
                Insert(ref n1, ref n2);
                Insert(ref n0, ref n1);
            }
            if (count > 4)
            {
                Insert(ref n3, ref n4);
                Insert(ref n2, ref n3);
                Insert(ref n1, ref n2);
                Insert(ref n0, ref n1);
            }
            if (count > 5)
            {
                Insert(ref n4, ref n5);
                Insert(ref n3, ref n4);
                Insert(ref n2, ref n3);
                Insert(ref n1, ref n2);
                Insert(ref n0, ref n1);
            }
            if (count > 6)
            {
                Insert(ref n5, ref n6);
                Insert(ref n4, ref n5);
                Insert(ref n3, ref n4);
                Insert(ref n2, ref n3);
                Insert(ref n1, ref n2);
                Insert(ref n0, ref n1);
            }
            if (count > 7)
            {
                Insert(ref n6, ref n7);
                Insert(ref n5, ref n6);
                Insert(ref n4, ref n5);
                Insert(ref n3, ref n4);
                Insert(ref n2, ref n3);
                Insert(ref n1, ref n2);
                Insert(ref n0, ref n1);
            }

            _length = count;

            _n0 = n0; _n1 = n1; _n2 = n2; _n3 = n3;
            _n4 = n4; _n5 = n5; _n6 = n6; _n7 = n7;
        }

        public bool Equals(SortedIntKey other)
        {
            return _length == other._length &&
                   _n0 == other._n0 &&
                   _n1 == other._n1 &&
                   _n2 == other._n2 &&
                   _n3 == other._n3 &&
                   _n4 == other._n4 &&
                   _n5 == other._n5 &&
                   _n6 == other._n6 &&
                   _n7 == other._n7;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = _length;
                hash = hash * 31 + _n0;
                hash = hash * 31 + _n1;
                hash = hash * 31 + _n2;
                hash = hash * 31 + _n3;
                hash = hash * 31 + _n4;
                hash = hash * 31 + _n5;
                hash = hash * 31 + _n6;
                hash = hash * 31 + _n7;
                return hash;
            }
        }
    }
}
