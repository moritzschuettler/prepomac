using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CaeMesh;
using CaeGlobals;
using System.Configuration;
using Octree;

namespace CaeResults
{
    [Serializable]
    public static class CloudPointReader
    {
        // Variables                                                                                                                
        static private string[] _splitter = new string[] { " ", ",", ":", ";", "\t" };


        // Methods                                                                                                                  
        static public CloudPoint[] Read(string fileName)
        {
            //# X Y Z Fx/A Fy/A Fz/A
            //0 0 0 1 0 0
            //10 0 0 1 0 0
            //10 10 0 1 0 0
            //0 10 0 1 0 0
            if (fileName != null && File.Exists(fileName))
            {
                string[] lines = Tools.ReadAllLines(fileName, true);
                //
                int numValues = -1;
                string line;
                string[] tmp;
                CloudPoint cloudPoint;
                List<CloudPoint> cloudPoints = new List<CloudPoint>();
                for (int i = 0; i < lines.Length; i++)
                {
                    line = lines[i].Trim();
                    if (line.Length > 0)
                    {
                        if (line[0] == '#') continue;
                        //
                        tmp = line.Split(_splitter, StringSplitOptions.RemoveEmptyEntries);
                        if (tmp.Length > 3)
                        {
                            cloudPoint = new CloudPoint();
                            cloudPoint.Coor = new double[] { double.Parse(tmp[0]), double.Parse(tmp[1]), double.Parse(tmp[2]) };
                            if (numValues == -1) numValues = tmp.Length - 3;
                            //
                            if (numValues != tmp.Length - 3)
                                throw new CaeException("The data file does not contain the same number of values in each line");
                            //
                            cloudPoint.Values = new double[numValues];
                            for (int j = 0; j < numValues; j++) cloudPoint.Values[j] = double.Parse(tmp[j + 3]);
                            //
                            cloudPoints.Add(cloudPoint);

                        }
                        else throw new CaeException("The data file does not contain at least 4 values in line number: " + i);
                    }
                }
                return cloudPoints.ToArray();
            }
            //
            return null;
        }
    }
}
