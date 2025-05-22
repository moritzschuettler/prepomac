using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace PrePoMax
{
    [Serializable]
    public class ViewComplexFrequencyStep : ViewStep
    {
        // Variables                                                                                                                
        private CaeModel.ComplexFrequencyStep _complexFrequencyStep;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(4, 10, "Perturbation")]
        [DescriptionAttribute("Perturbation parameter set to On applies preloads from the previous step if it exists.")]
        [Id(5, 1)]
        public bool Perturbation
        {
            get { return _complexFrequencyStep.Perturbation; }
            set { _complexFrequencyStep.Perturbation = value; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(6, 10, "Number of frequencies")]
        [DescriptionAttribute("Number of eigenfrequencies to compute.")]
        [Id(7, 1)]
        public int NumOfFrequencies
        {
            get { return _complexFrequencyStep.NumOfFrequencies; }
            set { _complexFrequencyStep.NumOfFrequencies = value; }
        }


        // Constructors                                                                                                             
        public ViewComplexFrequencyStep(CaeModel.ComplexFrequencyStep step, bool installProvider = true)
            : base(step)
        {
            _complexFrequencyStep = step;
            //
            if (installProvider)
            {
                InstallProvider();
                UpdateVisibility();
            }
        }


        // Methods
        public override CaeModel.Step GetBase()
        {
            return _complexFrequencyStep;
        }
        public override void InstallProvider()
        {
            base.InstallProvider();
            //
            _dctd.RenameBooleanPropertyToOnOff("Perturbation");
        }
        public override void UpdateVisibility()
        {
            base.UpdateVisibility();
            //
            _dctd.GetProperty(nameof(SolverType)).SetIsBrowsable(false);
        }
    }
}
