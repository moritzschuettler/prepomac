// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using CaeGlobals;
using System;
using System.Collections.Generic;

namespace CaeResults
{
    [Serializable]
    public class ResultHistoryOutputFromTable : ResultHistoryOutput
    {
        // Properties                                                                                                               
        public const string FieldName = "Table";
        public const string ComponentName = "Values";
        public const string XAxisName = "X";
        public const string YAxisName = "Y";

        private string _xAxisUnit;
        private string _yAxisUnit;
        private int _numberOfYColumns;
        private double[][] _dataPoints;
        private string[] _yColumnNames;

        public string XAxisUnit { get { return _xAxisUnit; } set { _xAxisUnit = value; } }
        public string YAxisUnit { get { return _yAxisUnit; } set { _yAxisUnit = value; } }
        public int NumberOfYColumns { get { return _numberOfYColumns; } set { SetNumberOfYColumns(value); } }
        public double[][] DataPoints { get { return _dataPoints; } set { _dataPoints = value; } }
        public string[] YColumnNames { get { return _yColumnNames; } set { SetYColumnNames(value); } }


        // Constructors                                                                                                              
        public ResultHistoryOutputFromTable(string name)
            : base(name, "", RegionTypeEnum.None)
        {
            _xAxisUnit = "/";
            _yAxisUnit = "/";
            _numberOfYColumns = 1;
            _dataPoints = new double[][]{};
            _yColumnNames = null;
        }


        // Methods                                                                                                                  
        public override string[] GetParentNames()
        {
            return new string[0];
        }
        public bool IsDataValid()
        {
            if (_numberOfYColumns < 1 || _dataPoints == null || _dataPoints.Length == 0)
                return false;

            HashSet<string> columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int column = 0; column < _numberOfYColumns; column++)
            {
                if (!columnNames.Add(GetYColumnName(column))) return false;
            }

            foreach (double[] point in _dataPoints)
            {
                if (point == null || point.Length != _numberOfYColumns + 1) return false;
                foreach (double value in point)
                {
                    if (double.IsNaN(value) || double.IsInfinity(value)) return false;
                }
            }
            return true;
        }
        public void CheckData()
        {
            if (!IsDataValid())
                throw new CaeException("The table must contain at least one Y column, unique Y-column names and " +
                                       "finite data in every cell.");
        }
        public string GetYColumnName(int column)
        {
            if (column < 0 || column >= _numberOfYColumns)
                throw new ArgumentOutOfRangeException(nameof(column));
            if (_yColumnNames != null && column < _yColumnNames.Length &&
                !string.IsNullOrWhiteSpace(_yColumnNames[column]))
                return _yColumnNames[column];
            return YAxisName + (column + 1);
        }
        private void SetNumberOfYColumns(int numberOfYColumns)
        {
            if (numberOfYColumns < 1)
                throw new CaeException("The number of Y columns must be at least one.");
            if (_numberOfYColumns == numberOfYColumns) return;

            _numberOfYColumns = numberOfYColumns;
            if (_dataPoints != null)
            {
                for (int row = 0; row < _dataPoints.Length; row++)
                {
                    double[] resized = new double[numberOfYColumns + 1];
                    if (_dataPoints[row] != null)
                        Array.Copy(_dataPoints[row], resized, Math.Min(_dataPoints[row].Length, resized.Length));
                    _dataPoints[row] = resized;
                }
            }
            if (_yColumnNames != null)
            {
                string[] resized = new string[numberOfYColumns];
                Array.Copy(_yColumnNames, resized, Math.Min(_yColumnNames.Length, resized.Length));
                _yColumnNames = resized;
            }
        }
        private void SetYColumnNames(string[] yColumnNames)
        {
            if (yColumnNames == null) _yColumnNames = null;
            else
            {
                _yColumnNames = new string[_numberOfYColumns];
                Array.Copy(yColumnNames, _yColumnNames, Math.Min(yColumnNames.Length, _yColumnNames.Length));
            }
        }
    }
}
