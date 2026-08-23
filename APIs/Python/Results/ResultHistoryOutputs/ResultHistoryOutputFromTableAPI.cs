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
    public class ResultHistoryOutputFromTableAPI : ResultHistoryOutputAPI
    {
        // Properties
        [PythonAPI("The table rows, each holding the X value followed by one value per Y column.",
                   Returns = "numpy.ndarray or NumericArray")]
        public object data_points { get { return NumPyConverter.ToPythonArray(GetRectangularDataPoints()); } }
        [PythonAPI("The Y-column names.", Returns = "StringList")]
        public StringListAPI y_column_names { get { return new StringListAPI(GetYColumnNames()); } }
        [PythonAPI("The number of Y columns in the table.", Returns = "int")]
        public int number_of_y_columns { get { return ResolveFromTable().NumberOfYColumns; } }
        [PythonAPI("The user-defined X-axis unit.", Returns = "str")]
        public string x_axis_unit { get { return ResolveFromTable().XAxisUnit; } }
        [PythonAPI("The user-defined unit shared by all Y columns.", Returns = "str")]
        public string y_axis_unit { get { return ResolveFromTable().YAxisUnit; } }


        // Constructors
        internal ResultHistoryOutputFromTableAPI(string resultName, string historyOutputName,
                                                 ResultHistoryOutputDataService resultHistoryService)
            : base(resultName, historyOutputName, resultHistoryService)
        {
        }


        // Methods
        private ResultHistoryOutputFromTable ResolveFromTable()
        {
            ResultHistoryOutputFromTable historyOutput = Resolve() as ResultHistoryOutputFromTable;
            if (historyOutput == null)
                throw new PythonAPIException("The history output '" + _historyOutputName +
                                             "' is not a From Table history output.");
            return historyOutput;
        }
        private string[] GetYColumnNames()
        {
            ResultHistoryOutputFromTable historyOutput = ResolveFromTable();
            string[] columnNames = new string[historyOutput.NumberOfYColumns];
            for (int column = 0; column < columnNames.Length; column++)
                columnNames[column] = historyOutput.GetYColumnName(column);
            return columnNames;
        }
        private double[,] GetRectangularDataPoints()
        {
            ResultHistoryOutputFromTable historyOutput = ResolveFromTable();
            double[][] rows = historyOutput.DataPoints;
            if (rows == null) rows = new double[0][];
            double[,] dataPoints = new double[rows.Length, historyOutput.NumberOfYColumns + 1];
            for (int row = 0; row < rows.Length; row++)
            {
                if (rows[row] == null) continue;
                int count = rows[row].Length < dataPoints.GetLength(1) ? rows[row].Length : dataPoints.GetLength(1);
                for (int column = 0; column < count; column++) dataPoints[row, column] = rows[row][column];
            }
            return dataPoints;
        }
        public override string ToString()
        {
            ResultHistoryOutputFromTable historyOutput = ResolveFromTable();
            int rowCount = historyOutput.DataPoints != null ? historyOutput.DataPoints.Length : 0;
            return "HistoryOutputFromTable(name=" + StringListAPI.ToDisplayString(_historyOutputName) +
                   ", rows=" + rowCount + ", y_columns=" + historyOutput.NumberOfYColumns + ")";
        }
    }
}
