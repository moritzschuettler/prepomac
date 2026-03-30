// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.IO;
using System.Text;

namespace CaeResults
{
    public static class ResultHistoryOutputWriter
    {
        public static void Write(string fileName, string[] historyOutputNames, string delimiter, FeResults results)
        {
            HistoryResultSet[] historyResultSets = new HistoryResultSet[historyOutputNames.Length];
            for (int i = 0; i < historyOutputNames.Length; i++)
            {
                historyResultSets[i] = results.GetHistoryResultSet(historyOutputNames[i]);
            }
            //
            StringBuilder sb = new StringBuilder();
            HistoryResultData historyData;
            string[] columnNames;
            object[][] rowBasedData;
            //
            foreach (HistoryResultSet historyResultSet in historyResultSets)
            {
                foreach (var fieldEntry in historyResultSet.Fields)
                {
                    foreach (var componentEntry in fieldEntry.Value.Components)
                    {
                        historyData = new HistoryResultData(historyResultSet.Name, fieldEntry.Key, componentEntry.Key);
                        results.GetHistoryOutputData(historyData, out columnNames, out rowBasedData, true);
                        // Title
                        sb.AppendLine("History output component" + delimiter +
                                      historyResultSet.Name + "." + fieldEntry.Key + "." + componentEntry.Key);
                        // Column names
                        for (int i = 0; i < columnNames.Length; i++)
                        {
                            if (i == 0) sb.Append(columnNames[i]);
                            else
                            {
                                sb.Append(delimiter);
                                sb.Append(columnNames[i]);
                            }
                        }
                        sb.AppendLine();
                        // Data
                        for (int i = 0; i < rowBasedData.Length; i++)
                        {
                            for (int j = 0; j < rowBasedData[i].Length; j++)
                            {
                                if (j != 0) sb.Append(delimiter);
                                sb.Append(rowBasedData[i][j]);
                            }
                            sb.AppendLine();
                        }
                        sb.AppendLine("End component");
                    }
                }
            }
            //
            File.WriteAllText(fileName, sb.ToString());
        }
    }
}
