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
    public class ViewLamina : ViewMaterialProperty
    {
        // Variables   
        private List<LaminaDataPoint> _points;


        // Properties      
        public override string Name
        {
            get { return "Elastic - Lamina"; }
        }
        //
        [Browsable(false)]
        public List<LaminaDataPoint> DataPoints { get { return _points; } set { _points = value; } }


        // Constructors     
        public ViewLamina(Lamina lamina)
        {
            _points = new List<LaminaDataPoint>();
            for (int i = 0; i < lamina.LaminaTemp.Length; i++)
            {
                _points.Add(new LaminaDataPoint(
            lamina.LaminaTemp[i][0], // E1
              lamina.LaminaTemp[i][1], // E2
                lamina.LaminaTemp[i][2], // Nu12
            lamina.LaminaTemp[i][3], // G12
            lamina.LaminaTemp[i][4], // G13
                lamina.LaminaTemp[i][5], // G23
               lamina.LaminaTemp[i][6]  // Temperature
            ));
            }
            //
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods      
        public override MaterialProperty GetBase()
        {
            int i = 0;
            EquationContainer[][] laminaTemp = new EquationContainer[_points.Count][];
            //
            foreach (LaminaDataPoint point in _points)
            {
                laminaTemp[i] = new EquationContainer[7];
                laminaTemp[i][0] = point.E1;
                laminaTemp[i][1] = point.E2;
                laminaTemp[i][2] = point.Nu12;
                laminaTemp[i][3] = point.G12;
                laminaTemp[i][4] = point.G13;
                laminaTemp[i][5] = point.G23;
                laminaTemp[i][6] = point.Temperature;
                i++;
            }
            Lamina lamina = new Lamina(laminaTemp);
            //
            return lamina;
        }
        public double GetE1Value()
        {
            if (_points != null && _points.Count > 0) return _points[0].E1.Value;
            else return 0;
        }
    }
}
