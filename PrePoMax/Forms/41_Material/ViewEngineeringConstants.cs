using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using DynamicTypeDescriptor;
using CaeGlobals;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class ViewEngineeringConstants : ViewMaterialProperty
    {
        // Variables  
        private List<EngineeringConstantsDataPoint> _points;


        // Properties           
        public override string Name
        {
            get { return "Elastic - Engineering Constants"; }
        }
        //
        [Browsable(false)]
        public List<EngineeringConstantsDataPoint> DataPoints { get { return _points; } set { _points = value; } }


        // Constructors
        public ViewEngineeringConstants(EngineeringConstants engineeringConstants)
        {
            _points = new List<EngineeringConstantsDataPoint>();
            for (int i = 0; i < engineeringConstants.EngineeringConstantsTemp.Length; i++)
            {
                _points.Add(new EngineeringConstantsDataPoint(
                    engineeringConstants.EngineeringConstantsTemp[i][0], // E1
                    engineeringConstants.EngineeringConstantsTemp[i][1], // E2
                    engineeringConstants.EngineeringConstantsTemp[i][2], // E3
                    engineeringConstants.EngineeringConstantsTemp[i][3], // Nu12
                    engineeringConstants.EngineeringConstantsTemp[i][4], // Nu13
                    engineeringConstants.EngineeringConstantsTemp[i][5], // Nu23
                    engineeringConstants.EngineeringConstantsTemp[i][6], // G12
                    engineeringConstants.EngineeringConstantsTemp[i][7], // G13
                    engineeringConstants.EngineeringConstantsTemp[i][8], // G23
                    engineeringConstants.EngineeringConstantsTemp[i][9]  // Temperature
                    ));
            }
            //
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods   
        public override MaterialProperty GetBase()
        {
            int i = 0;
            EquationContainer[][] engineeringConstantsTemp = new EquationContainer[_points.Count][];
            //
            foreach (EngineeringConstantsDataPoint point in _points)
            {
                engineeringConstantsTemp[i] = new EquationContainer[10];
                engineeringConstantsTemp[i][0] = point.E1;
                engineeringConstantsTemp[i][1] = point.E2;
                engineeringConstantsTemp[i][2] = point.E3;
                engineeringConstantsTemp[i][3] = point.Nu12;
                engineeringConstantsTemp[i][4] = point.Nu13;
                engineeringConstantsTemp[i][5] = point.Nu23;
                engineeringConstantsTemp[i][6] = point.G12;
                engineeringConstantsTemp[i][7] = point.G13;
                engineeringConstantsTemp[i][8] = point.G23;
                engineeringConstantsTemp[i][9] = point.Temperature;
                i++;
            }
            EngineeringConstants engineeringConstants = new EngineeringConstants(engineeringConstantsTemp);
            //
            return engineeringConstants;
        }
        public double GetE1Value()
        {
            if (_points != null && _points.Count > 0) return _points[0].E1.Value;
            else return 0;
        }
    }
}
