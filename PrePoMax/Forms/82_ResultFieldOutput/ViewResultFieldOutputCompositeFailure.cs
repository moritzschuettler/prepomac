using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;
using CaeResults;
using DynamicTypeDescriptor;

namespace PrePoMax
{
    [Serializable]
    public class ViewResultFieldOutputCompositeFailure : ViewResultFieldOutput
    {
        // Variables
        private ResultFieldOutputCompositeFailure _resultFieldOutput;
        private Dictionary<string, string[]> _filedNameComponentNames;


        // Properties
        public override string Name
        {
            get { return _resultFieldOutput.Name; }
            set { _resultFieldOutput.Name = value; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Field name")]
        [DescriptionAttribute("Stress field name to use for failure criteria computation.")]
        public string FieldName
        {
            get { return _resultFieldOutput.FieldName; }
            set { _resultFieldOutput.FieldName = value; }
        }
        //
        [CategoryAttribute("Strength")]
        [OrderedDisplayName(2, 10, "Xt - Longitudinal tensile")]
        [DescriptionAttribute("Longitudinal tensile strength (fiber direction).")]
        public double Xt
        {
            get { return _resultFieldOutput.Xt; }
            set { _resultFieldOutput.Xt = value; }
        }
        //
        [CategoryAttribute("Strength")]
        [OrderedDisplayName(3, 10, "Xc - Longitudinal compressive")]
        [DescriptionAttribute("Longitudinal compressive strength (fiber direction).")]
        public double Xc
        {
            get { return _resultFieldOutput.Xc; }
            set { _resultFieldOutput.Xc = value; }
        }
        //
        [CategoryAttribute("Strength")]
        [OrderedDisplayName(4, 10, "Yt - Transverse tensile")]
        [DescriptionAttribute("Transverse tensile strength (perpendicular to fiber direction).")]
        public double Yt
        {
            get { return _resultFieldOutput.Yt; }
            set { _resultFieldOutput.Yt = value; }
        }
        //
        [CategoryAttribute("Strength")]
        [OrderedDisplayName(5, 10, "Yc - Transverse compressive")]
        [DescriptionAttribute("Transverse compressive strength (perpendicular to fiber direction).")]
        public double Yc
        {
            get { return _resultFieldOutput.Yc; }
            set { _resultFieldOutput.Yc = value; }
        }
        //
        [CategoryAttribute("Strength")]
        [OrderedDisplayName(6, 10, "S12 - In-plane shear")]
        [DescriptionAttribute("In-plane shear strength (used in Tsai-Hill and Tsai-Wu).")]
        public double S12
        {
            get { return _resultFieldOutput.S12; }
            set { _resultFieldOutput.S12 = value; }
        }
        //
        [CategoryAttribute("Strength")]
        [OrderedDisplayName(7, 10, "Sl - Longitudinal shear")]
        [DescriptionAttribute("Longitudinal shear strength (used in Hashin criteria). Defaults to S12 if left at zero.")]
        public double Sl
        {
            get { return _resultFieldOutput.Sl; }
            set { _resultFieldOutput.Sl = value; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(8, 10, "Fiber angle [deg]")]
        [DescriptionAttribute("Rotation angle (degrees) from the stress output coordinate system to the ply fiber direction. " +
                              "Apply the ply angle here so that S11/S22/S12 are correctly interpreted as fiber/transverse/shear stresses.")]
        public double FiberAngleDeg
        {
            get { return _resultFieldOutput.FiberAngleDeg; }
            set { _resultFieldOutput.FiberAngleDeg = value; }
        }


        // Constructors
        public ViewResultFieldOutputCompositeFailure(ResultFieldOutputCompositeFailure resultFieldOutput)
        {
            _resultFieldOutput = resultFieldOutput;
            _dctd = ProviderInstaller.Install(this);
        }


        // Methods
        public override ResultFieldOutput GetBase()
        {
            return _resultFieldOutput;
        }
        public void PopulateDropDownLists(Dictionary<string, string[]> filedNameComponentNames)
        {
            _filedNameComponentNames = filedNameComponentNames;
            // Only offer tensor (stress) fields
            string[] stressFields = filedNameComponentNames.Keys
                .Where(k => filedNameComponentNames[k] != null &&
                            filedNameComponentNames[k].Contains(FOComponentNames.S11) &&
                            filedNameComponentNames[k].Contains(FOComponentNames.S22) &&
                            filedNameComponentNames[k].Contains(FOComponentNames.S12))
                .ToArray();
            if (stressFields.Length > 0)
                _dctd.PopulateProperty(nameof(FieldName), stressFields);
        }
    }
}
