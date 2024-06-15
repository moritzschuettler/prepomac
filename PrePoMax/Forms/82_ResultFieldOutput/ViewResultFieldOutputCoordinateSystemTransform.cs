using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using CaeResults;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class ViewResultFieldOutputCoordinateSystemTransform : ViewResultFieldOutput
    {
        // Variables                                                                                                                
        private ResultFieldOutputCoordinateSystemTransform _resultFieldOutput;


        // Properties                                                                                                               
        public override string Name { get { return _resultFieldOutput.Name; } set { _resultFieldOutput.Name = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Field name")]
        [DescriptionAttribute("Filed name for the field output.")]
        public string FieldName { get { return _resultFieldOutput.FieldName; } set { _resultFieldOutput.FieldName = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Coordinate system")]
        [DescriptionAttribute("Coordinate system name for the field output.")]
        public string CoordinateSystemName
        {
            get { return _resultFieldOutput.CoordinateSystemName; }
            set { _resultFieldOutput.CoordinateSystemName = value; }
        }


        // Constructors                                                                                                             
        public ViewResultFieldOutputCoordinateSystemTransform(ResultFieldOutputCoordinateSystemTransform resultFieldOutput)
        {
            // The order is important
            _resultFieldOutput = resultFieldOutput;
            //
            _dctd = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
        public override ResultFieldOutput GetBase()
        {
            return _resultFieldOutput;
        }
        public void PopulateDropDownLists(Dictionary<string, string[]> filedNameComponentNames, string[] coordinateSystemNames)
        {
            _dctd.PopulateProperty(nameof(FieldName), filedNameComponentNames.Keys.ToArray());
            _dctd.PopulateProperty(nameof(CoordinateSystemName), coordinateSystemNames);
        }
        private void UpdateVisibility()
        {
           
        }
    }



   
}
