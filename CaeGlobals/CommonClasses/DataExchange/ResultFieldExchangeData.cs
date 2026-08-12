// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;

namespace CaeGlobals
{
    [Serializable]
    public enum ResultFieldTypeEnum
    {
        None,
        Vector,
        SymmetricTensor
    }

    [Serializable]
    public enum ResultFieldValueModeEnum
    {
        Direct,
        ComplexMagnitude,
        ComplexPhase
    }

    /// <summary>
    /// Complete nodal field data required to reconstruct a selected, direction-dependent
    /// result component after display-only geometry transformations.
    /// </summary>
    [Serializable]
    public class ResultFieldExchangeData
    {
        public ResultFieldTypeEnum Type;
        public ResultFieldValueModeEnum ValueMode;
        public int ComponentIndex;
        public float[][] ComponentValues;
        public float[][] ImaginaryComponentValues;
        public float ScaleFactor;


        public ResultFieldExchangeData()
        {
            Type = ResultFieldTypeEnum.None;
            ValueMode = ResultFieldValueModeEnum.Direct;
            ComponentIndex = -1;
            ComponentValues = null;
            ImaginaryComponentValues = null;
            ScaleFactor = 1;
        }

        public ResultFieldExchangeData(ResultFieldExchangeData source, bool copyValues)
            : this()
        {
            if (source == null) return;
            //
            Type = source.Type;
            ValueMode = source.ValueMode;
            ComponentIndex = source.ComponentIndex;
            ComponentValues = copyValues ? Copy(source.ComponentValues) : source.ComponentValues;
            ImaginaryComponentValues = copyValues ? Copy(source.ImaginaryComponentValues) :
                                                     source.ImaginaryComponentValues;
            ScaleFactor = source.ScaleFactor;
        }


        public ResultFieldExchangeData DeepCopy()
        {
            return new ResultFieldExchangeData(this, true);
        }

        public ResultFieldExchangeData WithScaleFactor(float scaleFactor)
        {
            ResultFieldExchangeData scaled = new ResultFieldExchangeData(this, false);
            scaled.ScaleFactor *= scaleFactor;
            return scaled;
        }

        public int GetNumberOfValues()
        {
            if (ComponentValues == null || ComponentValues.Length == 0 || ComponentValues[0] == null) return 0;
            return ComponentValues[0].Length;
        }

        public bool IsValid()
        {
            int componentCount = Type == ResultFieldTypeEnum.Vector ? 3 :
                                 Type == ResultFieldTypeEnum.SymmetricTensor ? 6 : 0;
            if (componentCount == 0 || ComponentIndex < 0 || ComponentIndex >= componentCount ||
                ComponentValues == null || ComponentValues.Length != componentCount) return false;
            //
            int valueCount = -1;
            for (int i = 0; i < ComponentValues.Length; i++)
            {
                if (ComponentValues[i] == null) return false;
                if (valueCount < 0) valueCount = ComponentValues[i].Length;
                else if (ComponentValues[i].Length != valueCount) return false;
            }
            //
            if (ValueMode == ResultFieldValueModeEnum.ComplexMagnitude ||
                ValueMode == ResultFieldValueModeEnum.ComplexPhase)
            {
                if (ImaginaryComponentValues == null || ImaginaryComponentValues.Length != componentCount) return false;
                for (int i = 0; i < ImaginaryComponentValues.Length; i++)
                {
                    if (ImaginaryComponentValues[i] == null || ImaginaryComponentValues[i].Length != valueCount)
                        return false;
                }
            }
            return true;
        }

        public float[] GetTransformedValues(double[] transformation)
        {
            if (!IsValid()) return null;
            if (transformation == null || transformation.Length != 9)
                throw new ArgumentException("A 3 x 3 row-major transformation matrix is required.",
                                            nameof(transformation));
            //
            int count = GetNumberOfValues();
            float[] values = new float[count];
            for (int i = 0; i < count; i++)
            {
                double real = GetTransformedComponent(ComponentValues, i, transformation);
                double value;
                if (ValueMode == ResultFieldValueModeEnum.Direct)
                {
                    value = real;
                }
                else
                {
                    double imaginary = GetTransformedComponent(ImaginaryComponentValues, i, transformation);
                    if (ValueMode == ResultFieldValueModeEnum.ComplexMagnitude)
                        value = Tools.GetComplexMagnitude(real, imaginary);
                    else
                        value = Tools.GetComplexPhaseDeg(real, imaginary);
                }
                values[i] = (float)(ScaleFactor * value);
            }
            return values;
        }

        public bool TryGetTransformedRange(double[] transformation, out float min, out float max)
        {
            min = float.MaxValue;
            max = -float.MaxValue;
            float[] values = GetTransformedValues(transformation);
            if (values == null) return false;
            //
            bool found = false;
            for (int i = 0; i < values.Length; i++)
            {
                float value = values[i];
                if (float.IsNaN(value) || float.IsInfinity(value)) continue;
                if (value < min) min = value;
                if (value > max) max = value;
                found = true;
            }
            return found;
        }

        public static double[] IdentityTransformation()
        {
            return new double[] { 1, 0, 0,
                                  0, 1, 0,
                                  0, 0, 1 };
        }

        public static double[] MultiplyTransformations(double[] left, double[] right)
        {
            if (left == null || right == null || left.Length != 9 || right.Length != 9)
                throw new ArgumentException("Two 3 x 3 row-major transformation matrices are required.");
            //
            double[] result = new double[9];
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    double value = 0;
                    for (int i = 0; i < 3; i++) value += left[3 * row + i] * right[3 * i + column];
                    result[3 * row + column] = value;
                }
            }
            return result;
        }


        private double GetTransformedComponent(float[][] components, int valueIndex, double[] transformation)
        {
            if (Type == ResultFieldTypeEnum.Vector)
            {
                int row = ComponentIndex;
                return transformation[3 * row] * components[0][valueIndex] +
                       transformation[3 * row + 1] * components[1][valueIndex] +
                       transformation[3 * row + 2] * components[2][valueIndex];
            }
            else if (Type == ResultFieldTypeEnum.SymmetricTensor)
            {
                int row;
                int column;
                GetTensorIndices(ComponentIndex, out row, out column);
                // T' = Q * T * Q^T. Components are ordered as 11, 22, 33, 12, 23, 13.
                double value = 0;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        value += transformation[3 * row + i] * GetTensorValue(components, valueIndex, i, j) *
                                 transformation[3 * column + j];
                    }
                }
                return value;
            }
            else throw new NotSupportedException();
        }

        private static void GetTensorIndices(int componentIndex, out int row, out int column)
        {
            if (componentIndex == 0) { row = 0; column = 0; }
            else if (componentIndex == 1) { row = 1; column = 1; }
            else if (componentIndex == 2) { row = 2; column = 2; }
            else if (componentIndex == 3) { row = 0; column = 1; }
            else if (componentIndex == 4) { row = 1; column = 2; }
            else if (componentIndex == 5) { row = 0; column = 2; }
            else throw new NotSupportedException();
        }

        private static double GetTensorValue(float[][] components, int valueIndex, int row, int column)
        {
            if (row == column) return components[row][valueIndex];
            if ((row == 0 && column == 1) || (row == 1 && column == 0)) return components[3][valueIndex];
            if ((row == 1 && column == 2) || (row == 2 && column == 1)) return components[4][valueIndex];
            return components[5][valueIndex];
        }

        private static float[][] Copy(float[][] values)
        {
            if (values == null) return null;
            float[][] copy = new float[values.Length][];
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != null) copy[i] = (float[])values[i].Clone();
            }
            return copy;
        }
    }
}
