// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using CaeResults;

namespace PrePoMax
{
    [Serializable]
    public class ViewResultHistoryOutputFromAffineFit : ViewResultHistoryOutput
    {
        // Variables
        private ResultHistoryOutputFromAffineFit _historyOutput;


        // Properties
        public override string Name { get { return _historyOutput.Name; } set { _historyOutput.Name = value; } }
        //
        [CategoryAttribute("Center and radius")]
        [OrderedDisplayName(0, 10, "Center X")]
        [DescriptionAttribute("X coordinate of the measurement region center, in the undeformed geometry.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 3)]
        public double CenterX { get { return _historyOutput.CenterX; } set { _historyOutput.CenterX = value; } }
        //
        [CategoryAttribute("Center and radius")]
        [OrderedDisplayName(1, 10, "Center Y")]
        [DescriptionAttribute("Y coordinate of the measurement region center, in the undeformed geometry.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(2, 3)]
        public double CenterY { get { return _historyOutput.CenterY; } set { _historyOutput.CenterY = value; } }
        //
        [CategoryAttribute("Center and radius")]
        [OrderedDisplayName(2, 10, "Radius")]
        [DescriptionAttribute("Radius of the circular measurement region.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 3)]
        public double Radius { get { return _historyOutput.Radius; } set { _historyOutput.Radius = value; } }
        //
        [CategoryAttribute("Center and radius")]
        [OrderedDisplayName(3, 10, "Taper start fraction")]
        [DescriptionAttribute("Fraction of the radius (0 to 1) where the weight starts to taper from 1 to 0. " +
                              "A value of 1 gives a hard cutoff at the radius.")]
        [Id(4, 3)]
        public double TaperStartFraction { get { return _historyOutput.TaperStartFraction; } set { _historyOutput.TaperStartFraction = value; } }
        //
        [CategoryAttribute("Source points")]
        [OrderedDisplayName(0, 10, "Point source")]
        [DescriptionAttribute("Fit the deformation gradient to mesh nodes, or to the area centroids of the closed " +
                              "cell/pore edge loops found in the whole model.")]
        [Id(1, 4)]
        public AffineFitPointSourceEnum PointSource
        {
            get { return _historyOutput.PointSource; }
            set { _historyOutput.PointSource = value; UpdateVisibility(); }
        }
        //
        [CategoryAttribute("Source points")]
        [OrderedDisplayName(1, 10, "Weighting")]
        [DescriptionAttribute("Weighting scheme for cell centroid points. Equal weights every cell the same; " +
                              "Area and Perimeter favor larger cells, which average out non-affine wall " +
                              "fluctuation better.")]
        [Id(2, 4)]
        public AffineFitWeightingEnum Weighting { get { return _historyOutput.Weighting; } set { _historyOutput.Weighting = value; } }
        //
        [CategoryAttribute("Source points")]
        [OrderedDisplayName(2, 10, "Area filter relative median")]
        [DescriptionAttribute("Relative median (0 to 2) of the candidate cell area distribution used as the " +
                              "reference area for the tolerance window below. 1 is the median. With many small " +
                              "cells and a few large but important ones, raise this value to move the " +
                              "reference near the large cells so the small ones fall outside the window.")]
        [Id(3, 4)]
        public double AreaFilterRelativeMedian
        {
            get { return _historyOutput.AreaFilterRelativeMedian; }
            set { _historyOutput.AreaFilterRelativeMedian = value; }
        }
        //
        [CategoryAttribute("Source points")]
        [OrderedDisplayName(3, 10, "Cell area tolerance")]
        [DescriptionAttribute("Cells whose reference area deviates from the reference cell area (set by the " +
                              "relative median) by more than this fraction are rejected as cropped, degenerate, " +
                              "or the wrong size (0.2 = accept 20% to 120% of the reference area).")]
        [Id(4, 4)]
        public double CellAreaToleranceFraction
        {
            get { return _historyOutput.CellAreaToleranceFraction; }
            set { _historyOutput.CellAreaToleranceFraction = value; }
        }
        //
        [CategoryAttribute("Evaluation")]
        [OrderedDisplayName(0, 10, "Strain measure")]
        [DescriptionAttribute("Linear (small strain) uses sym(F-I). Biot and Logarithmic remove the rigid " +
                              "rotation from F by a polar decomposition first and are valid for finite strain.")]
        [Id(1, 5)]
        public AffineFitStrainMeasureEnum StrainMeasure { get { return _historyOutput.StrainMeasure; } set { _historyOutput.StrainMeasure = value; } }
        //
        [CategoryAttribute("Evaluation")]
        [OrderedDisplayName(1, 10, "Loading direction X")]
        [DescriptionAttribute("X component of the loading direction e_L. Does not need to be normalized.")]
        [Id(2, 5)]
        public double LoadingDirectionX { get { return _historyOutput.LoadingDirectionX; } set { _historyOutput.LoadingDirectionX = value; } }
        //
        [CategoryAttribute("Evaluation")]
        [OrderedDisplayName(2, 10, "Loading direction Y")]
        [DescriptionAttribute("Y component of the loading direction e_L. Does not need to be normalized.")]
        [Id(3, 5)]
        public double LoadingDirectionY { get { return _historyOutput.LoadingDirectionY; } set { _historyOutput.LoadingDirectionY = value; } }
        //
        [CategoryAttribute("Radius convergence study")]
        [OrderedDisplayName(0, 10, "Number of radius steps")]
        [DescriptionAttribute("1 evaluates only at the Radius above, against every step/increment, as usual. " +
                              "A value above 1 instead sweeps the radius from Radius/N to Radius in N equal " +
                              "steps, evaluated once at the last available increment, and saves the result " +
                              "against radius (instead of time) as the time history - a radius convergence study.")]
        [Id(1, 6)]
        public int RadiusSweepStepCount
        {
            get { return _historyOutput.RadiusSweepStepCount; }
            set { _historyOutput.RadiusSweepStepCount = Math.Max(1, value); }
        }


        // Constructors
        public ViewResultHistoryOutputFromAffineFit(ResultHistoryOutputFromAffineFit historyOutput)
            : base(historyOutput)
        {
            // The order is important
            _historyOutput = historyOutput;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.None, nameof(SelectionHidden));
            //
            SetBase(_historyOutput, regionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            //
            UpdateVisibility();
        }


        // Methods
        public override ResultHistoryOutput GetBase()
        {
            return _historyOutput;
        }
        public void PopulateDropDownLists()
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.None, new string[] { "None" });
            base.PopulateDropDownLists(regionTypeListItemsPairs);
            //
            DynamicCustomTypeDescriptor dctd = DynamicCustomTypeDescriptor;
            dctd.GetProperty(nameof(RegionType)).SetIsBrowsable(false);
            dctd.GetProperty(nameof(SelectionHidden)).SetIsBrowsable(false);
        }
        private void UpdateVisibility()
        {
            bool cellCentroids = _historyOutput.PointSource == AffineFitPointSourceEnum.CellCentroids;
            DynamicCustomTypeDescriptor.GetProperty(nameof(Weighting)).SetIsBrowsable(cellCentroids);
            DynamicCustomTypeDescriptor.GetProperty(nameof(AreaFilterRelativeMedian)).SetIsBrowsable(cellCentroids);
            DynamicCustomTypeDescriptor.GetProperty(nameof(CellAreaToleranceFraction)).SetIsBrowsable(cellCentroids);
        }
    }
}
