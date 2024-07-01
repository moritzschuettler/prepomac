using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;
using CaeMesh;
using CaeModel;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalTransform : CalculixKeyword
    {
        // Variables                                                                                                                
        private CoordinateSystem _coordinateSystem;
        private string _nodeSetName;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalTransform(CoordinateSystem coordinateSystem, string nodeSetName)
        {
            _coordinateSystem = coordinateSystem;
            _nodeSetName = nodeSetName;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string type;
            if (_coordinateSystem.Type == CoordinateSystemTypeEnum.Rectangular) type = "R";
            else if (_coordinateSystem.Type == CoordinateSystemTypeEnum.Cylindrical) type = "C";
            else throw new NotSupportedException();
            //
            return string.Format("*Transform, Nset={0}, Type={1}{2}", _nodeSetName, type, Environment.NewLine);
        }
        public override string GetDataString()
        {
            double[] a;
            double[] b;
            //
            if (_coordinateSystem.Type == CoordinateSystemTypeEnum.Rectangular)
            {
                a = _coordinateSystem.DirectionX().Coor;
                b = _coordinateSystem.DirectionY().Coor;
            }
            else if (_coordinateSystem.Type == CoordinateSystemTypeEnum.Cylindrical)
            {
                a = _coordinateSystem.Center().Coor;
                b = (_coordinateSystem.Center() + _coordinateSystem.DirectionZ()).Coor;
            }
            else throw new NotSupportedException();
            //
            return string.Format("{0}, {1}, {2}, {3}, {4}, {5}{6}", a[0].ToCalculiX16String(), a[1].ToCalculiX16String(),
                                                                    a[2].ToCalculiX16String(), b[0].ToCalculiX16String(),
                                                                    b[1].ToCalculiX16String(), b[2].ToCalculiX16String(),
                                                                    Environment.NewLine);
        }
    }
}