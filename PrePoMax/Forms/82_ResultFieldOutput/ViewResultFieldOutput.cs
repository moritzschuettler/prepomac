// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeResults;
using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.Design.AxImporter;

namespace PrePoMax
{
    [Serializable]
    public abstract class ViewResultFieldOutput
    {
        // Variables                                                                                                                
        protected ResultFieldOutput _resultFieldOutput;
        protected DynamicCustomTypeDescriptor _dctd = null;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Name of the field output.")]
        [Id(1, 1)]
        public abstract string Name { get; set; }
        //                                                              
        [CategoryAttribute("Filter 1")]
        [OrderedDisplayName(0, 10, "Type")]
        [DescriptionAttribute("Select the filter 1 type.")]
        [Id(1, 10)]
        public FieldResultFilterTypeEnum Type1
        {
            get { return _resultFieldOutput.Filter1.Type; }
            set
            {
                _resultFieldOutput.Filter1.Type = value;
                //
                if (_resultFieldOutput.Filter1.Type == FieldResultFilterTypeEnum.None)
                    _resultFieldOutput.Filter2.Type = FieldResultFilterTypeEnum.None;
                //
                UpdateFilterVisibility();
            }
        }
        //
        [CategoryAttribute("Filter 1")]
        [OrderedDisplayName(1, 10, "Source type")]
        [DescriptionAttribute("Select the filter 1 source.")]
        [Id(2, 10)]
        public FieldResultFilterSourceTypeEnum Source1
        {
            get { return _resultFieldOutput.Filter1.SourceType; }
            set { _resultFieldOutput.Filter1.SourceType = value; }
        }
        //
        [CategoryAttribute("Filter 1")]
        [OrderedDisplayName(2, 10, "Number of layers")]
        [DescriptionAttribute("Enter the number of neighbouring layers for the filter 1 source data.")]
        [TypeConverter(typeof(StringIntegerConverter))]
        [Id(3, 10)]
        public int NumberOfNeighbouringLayers1
        {
            get { return _resultFieldOutput.Filter1.NumberOfNeighbouringLayers; }
            set { _resultFieldOutput.Filter1.NumberOfNeighbouringLayers = value; }
        }
        //
        [CategoryAttribute("Filter 1")]
        [OrderedDisplayName(3, 10, "Number of iterations")]
        [DescriptionAttribute("Enter the number of filter 1 iterations.")]
        [TypeConverter(typeof(StringIntegerConverter))]
        [Id(4, 10)]
        public int NumberOfIterations1
        {
            get { return _resultFieldOutput.Filter1.NumberOfIterations; }
            set { _resultFieldOutput.Filter1.NumberOfIterations = value; }
        }
        //                                                              
        [CategoryAttribute("Filter 2")]
        [OrderedDisplayName(0, 10, "Type ")]    // must be different display name
        [DescriptionAttribute("Select the filter 2 type.")]
        [Id(1, 11)]
        public FieldResultFilterTypeEnum Type2
        {
            get { return _resultFieldOutput.Filter2.Type; }
            set
            {
                _resultFieldOutput.Filter2.Type = value;
                //
                UpdateFilterVisibility();
            }
        }
        //
        [CategoryAttribute("Filter 2")]
        [OrderedDisplayName(1, 10, "Source type ")] // must be different display name
        [DescriptionAttribute("Select the filter 2 source.")]
        [Id(2, 11)]
        public FieldResultFilterSourceTypeEnum Source2
        {
            get { return _resultFieldOutput.Filter2.SourceType; }
            set { _resultFieldOutput.Filter2.SourceType = value; }
        }
        //
        [CategoryAttribute("Filter 2")]
        [OrderedDisplayName(2, 10, "Number of layers ")]    // must be different display name
        [DescriptionAttribute("Enter the number of neighbouring layers for the filter 2 source data.")]
        [TypeConverter(typeof(StringIntegerConverter))]
        [Id(3, 11)]
        public int NumberOfNeighbouringLayers2
        {
            get { return _resultFieldOutput.Filter2.NumberOfNeighbouringLayers; }
            set { _resultFieldOutput.Filter2.NumberOfNeighbouringLayers = value; }
        }
        //
        [CategoryAttribute("Filter 2")]
        [OrderedDisplayName(3, 10, "Number of iterations ")]    // must be different display name
        [DescriptionAttribute("Enter the number of filter 2 iterations.")]
        [TypeConverter(typeof(StringIntegerConverter))]
        [Id(4, 11)]
        public int NumberOfIterations2
        {
            get { return _resultFieldOutput.Filter2.NumberOfIterations; }
            set { _resultFieldOutput.Filter2.NumberOfIterations = value; }
        }


        // Constructors                                                                                                             
        public ViewResultFieldOutput(ResultFieldOutput resultFieldOutput)
        {
            // The order is important
            _resultFieldOutput = resultFieldOutput;
            //
            _dctd = ProviderInstaller.Install(this);
            //
            UpdateFilterVisibility();
        }

        // Methods
        public abstract ResultFieldOutput GetBase();
        public void UpdateFilterVisibility()
        {
            bool visible = _resultFieldOutput.Filter1.Type != FieldResultFilterTypeEnum.None;
            // Filter 1
            _dctd.GetProperty(nameof(Source1)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(NumberOfNeighbouringLayers1)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(NumberOfIterations1)).SetIsBrowsable(visible);
            // Filter 2
            _dctd.GetProperty(nameof(Type2)).SetIsBrowsable(visible);
            //
            visible &= _resultFieldOutput.Filter2.Type != FieldResultFilterTypeEnum.None;
            _dctd.GetProperty(nameof(Source2)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(NumberOfNeighbouringLayers2)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(NumberOfIterations2)).SetIsBrowsable(visible);
        }
        
    }
}
