// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;
using System.Linq;

namespace CaeMesh
{
    [Serializable]
    public class GeometryMetadataProperty
    {
        // Variables
        private readonly double[] _numericValues;
        private readonly string[] _textValues;


        // Properties
        public bool IsNumeric { get { return _numericValues != null; } }
        public double[] NumericValues { get { return _numericValues; } }
        public string[] TextValues { get { return _textValues; } }


        // Constructors
        public GeometryMetadataProperty(double[] values)
        {
            _numericValues = values != null ? values.ToArray() : new double[0];
            _textValues = null;
        }
        public GeometryMetadataProperty(string[] values)
        {
            _numericValues = null;
            _textValues = values != null ? values.ToArray() : new string[0];
        }
        public GeometryMetadataProperty(GeometryMetadataProperty property)
        {
            _numericValues = property?._numericValues != null ? property._numericValues.ToArray() : null;
            _textValues = property?._textValues != null ? property._textValues.ToArray() : null;
        }


        // Methods
        public GeometryMetadataProperty DeepCopy()
        {
            return new GeometryMetadataProperty(this);
        }
    }
}
