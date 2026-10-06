// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace APIs.Python
{
    public class NumericArrayAPI : BaseAPI, IReadOnlyList<object>
    {
        // Variables
        private const int CompactItemLimit = 8;
        private const int EdgeItemCount = 3;
        private readonly Array _items;


        // Properties
        int IReadOnlyCollection<object>.Count { get { return _items.GetLength(0); } }

        [PythonAPI("The array dimensions.", Returns = "IntList")]
        public IntListAPI shape
        {
            get
            {
                int[] dimensions = new int[_items.Rank];
                for (int i = 0; i < dimensions.Length; i++) dimensions[i] = _items.GetLength(i);
                return new IntListAPI(dimensions);
            }
        }
        [PythonAPI("The total number of values: the product of the shape, as numpy reports it. len() is the " +
                   "number of rows.", Returns = "int")]
        public int size { get { return _items.Length; } }
        public object this[int index]
        {
            get
            {
                int length = _items.GetLength(0);
                if (index < 0) index += length;
                if (index < 0 || index >= length) throw new IndexOutOfRangeException();
                if (_items.Rank == 1) return _items.GetValue(index);
                Type elementType = _items.GetType().GetElementType();
                Array row = Array.CreateInstance(elementType, _items.GetLength(1));
                for (int column = 0; column < row.Length; column++)
                    row.SetValue(_items.GetValue(index, column), column);
                return new NumericArrayAPI(row);
            }
        }


        // Constructors
        internal NumericArrayAPI(Array items)
        {
            if (items == null) throw new ArgumentNullException("items");
            if (items.Rank < 1 || items.Rank > 2)
                throw new ArgumentException("Only one- and two-dimensional arrays are supported.", "items");
            _items = ToRectangular(items);
        }


        // Methods
        // A table handed over as rows of equal length is stored as the two-dimensional array it is, so shape and size
        // describe the table rather than the list of rows
        private static Array ToRectangular(Array items)
        {
            if (items.Rank != 1 || items.Length == 0 || !items.GetType().GetElementType().IsArray) return items;
            //
            Array first = items.GetValue(0) as Array;
            if (first == null || first.Rank != 1) return items;
            int columns = first.Length;
            Type elementType = first.GetType().GetElementType();
            foreach (object row in items)
            {
                Array values = row as Array;
                if (values == null || values.Rank != 1 || values.Length != columns ||
                    values.GetType().GetElementType() != elementType) return items;
            }
            Array table = Array.CreateInstance(elementType, items.Length, columns);
            for (int i = 0; i < items.Length; i++)
            {
                Array values = (Array)items.GetValue(i);
                for (int j = 0; j < columns; j++) table.SetValue(values.GetValue(j), i, j);
            }
            return table;
        }
        public int __len__()
        {
            return _items.GetLength(0);
        }
        public object __getitem__(int index)
        {
            return this[index];
        }

        [PythonAPI("Returns a copied CLR array.", Returns = "Array")]
        public Array to_array()
        {
            return (Array)_items.Clone();
        }
        public IEnumerator<object> GetEnumerator()
        {
            for (int i = 0; i < _items.GetLength(0); i++) yield return this[i];
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public override string ToString()
        {
            return FormatArray(true);
        }
        protected override string ToFullString()
        {
            return FormatArray(false);
        }
        private string FormatArray(bool compact)
        {
            if (_items.Rank == 1)
                return FormatSequence(_items.GetLength(0), i => FormatValue(_items.GetValue(i)), compact);
            return FormatSequence(_items.GetLength(0), i => FormatRow(i, compact), compact);
        }
        private string FormatRow(int row, bool compact)
        {
            return FormatSequence(_items.GetLength(1), column => FormatValue(_items.GetValue(row, column)), compact);
        }
        private static string FormatSequence(int count, Func<int, string> formatter, bool compact)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            if (compact && count > CompactItemLimit)
            {
                for (int i = 0; i < EdgeItemCount; i++) AppendItem(sb, formatter(i));
                AppendItem(sb, "...");
                for (int i = count - EdgeItemCount; i < count; i++) AppendItem(sb, formatter(i));
            }
            else
            {
                for (int i = 0; i < count; i++) AppendItem(sb, formatter(i));
            }
            sb.Append(']');
            return sb.ToString();
        }
        private static void AppendItem(StringBuilder sb, string item)
        {
            if (sb.Length > 1) sb.Append(", ");
            sb.Append(item);
        }
        private static string FormatValue(object value)
        {
            if (value == null) return "None";
            if (value is bool) return (bool)value ? "True" : "False";
            IFormattable formattable = value as IFormattable;
            return formattable != null ? formattable.ToString(null, CultureInfo.InvariantCulture) : value.ToString();
        }
    }
}
