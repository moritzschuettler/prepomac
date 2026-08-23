// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;

namespace APIs.Python
{
    public class ResultHistoryOutputFromTableDraftAPI : ResultHistoryOutputDraftAPI
    {
        // Variables
        private readonly double[][] _dataPoints;
        private readonly string[] _yColumnNames;
        private string _xAxisUnit;
        private string _yAxisUnit;


        // Properties
        [PythonAPI("The table rows, each holding the X value followed by one value per Y column.",
                   Returns = "numpy.ndarray or NumericArray")]
        public object data_points { get { return NumPyConverter.ToPythonArray(GetRectangularDataPoints()); } }
        [PythonAPI("The Y-column names.", Returns = "StringList")]
        public StringListAPI y_column_names { get { return new StringListAPI(_yColumnNames); } }
        [PythonAPI("The number of Y columns in the table.", Returns = "int")]
        public int number_of_y_columns { get { return _yColumnNames.Length; } }
        [PythonAPI("The user-defined X-axis unit.", Returns = "str")]
        public string x_axis_unit { get { return _xAxisUnit; } set { _xAxisUnit = value; } }
        [PythonAPI("The user-defined unit shared by all Y columns.", Returns = "str")]
        public string y_axis_unit { get { return _yAxisUnit; } set { _yAxisUnit = value; } }


        // Constructors
        internal ResultHistoryOutputFromTableDraftAPI(string name, double[][] dataPoints, string[] yColumnNames,
                                                       string xAxisUnit, string yAxisUnit)
            : base(name, null)
        {
            _dataPoints = dataPoints;
            _yColumnNames = yColumnNames;
            _xAxisUnit = xAxisUnit;
            _yAxisUnit = yAxisUnit;
        }


        // Methods
        protected override ResultHistoryOutput CreateHistoryOutput()
        {
            ResultHistoryOutputFromTable output = new ResultHistoryOutputFromTable(name);
            output.NumberOfYColumns = _yColumnNames.Length;
            output.DataPoints = CloneDataPoints();
            output.YColumnNames = _yColumnNames;
            output.XAxisUnit = _xAxisUnit;
            output.YAxisUnit = _yAxisUnit;
            return output;
        }
        protected override string GetRequiredTarget()
        {
            return null;
        }
        private double[][] CloneDataPoints()
        {
            double[][] dataPoints = new double[_dataPoints.Length][];
            for (int row = 0; row < _dataPoints.Length; row++)
                dataPoints[row] = (double[])_dataPoints[row].Clone();
            return dataPoints;
        }
        private double[,] GetRectangularDataPoints()
        {
            double[,] dataPoints = new double[_dataPoints.Length, _yColumnNames.Length + 1];
            for (int row = 0; row < _dataPoints.Length; row++)
            {
                for (int column = 0; column < _dataPoints[row].Length; column++)
                    dataPoints[row, column] = _dataPoints[row][column];
            }
            return dataPoints;
        }
        public override string ToString()
        {
            return "HistoryOutputFromTableDraft(name=" + StringListAPI.ToDisplayString(name) +
                   ", rows=" + _dataPoints.Length + ", y_columns=" + _yColumnNames.Length + ")";
        }
    }
}
