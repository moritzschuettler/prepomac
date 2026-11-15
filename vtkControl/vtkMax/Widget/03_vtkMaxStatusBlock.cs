// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeGlobals;
using vtkControl.Native;

namespace vtkControl
{
    // The lines are made here, with the .NET number format; the native status block lays them out and finds the
    // deformation scale factor among them
    class vtkMaxStatusBlockWidget : vtkMaxTextWidget
    {
        // Variables
        private string _name;
        private DateTime _dateTime;
        private float _analysisTime;
        private string _analysisTimeUnit;
        private float _animationScaleFactor;
        private float _deformationScaleFactor;
        private string _deformationVariable;
        private vtkMaxFieldDataType _fieldType;
        private vtkMaxAnimationType _animationType;
        private int _stepNumber;
        private int _incrementNumber;
        private int _nodalDiameter;
        private string[] _lines;


        // Properties
        public string Name { get { return _name; } set { _name = value; SetText(); } }
        public DateTime DateTime { get { return _dateTime; } set { _dateTime = value; SetText(); } }
        public float AnalysisTime { get { return _analysisTime; } set { _analysisTime = value; SetText(); } }
        public string AnalysisTimeUnit { get { return _analysisTimeUnit; } set { _analysisTimeUnit = value; SetText(); } }
        public float DeformationScaleFactor
        {
            get { return _deformationScaleFactor; }
            set { _deformationScaleFactor = value; SetText(); }
        }
        public string DeformationVariable { get { return _deformationVariable; } set { _deformationVariable = value; SetText(); } }
        public float AnimationScaleFactor
        {
            get { return _animationScaleFactor; }
            set { _animationScaleFactor = value; SetText(); }
        }
        public vtkMaxFieldDataType FieldType { get { return _fieldType; } set { _fieldType = value; SetText(); } }
        public vtkMaxAnimationType AnimationType { get { return _animationType; } set { _animationType = value; SetText(); } }
        public int StepNumber { get { return _stepNumber; } set { _stepNumber = value; SetText(); } }
        public int IncrementNumber { get { return _incrementNumber; } set { _incrementNumber = value; SetText(); } }
        public int NodalDiameter { get { return _nodalDiameter; } set { _nodalDiameter = value; SetText(); } }


        // Constructors
        public vtkMaxStatusBlockWidget()
        {
            _name = "name";
            _dateTime = DateTime.Now;
            _analysisTime = 0;
            _analysisTimeUnit = "";
            _animationScaleFactor = -1;
            _deformationScaleFactor = 1;
            _fieldType = vtkMaxFieldDataType.Static;
            _nodalDiameter = -1;
            _animationType = vtkMaxAnimationType.ScaleFactor;
            _mirrorText = false;
        }


        // Private methods
        private void SetText()
        {
            string sysUIFormat = System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern;
            //
            string line1 = null;
            string line2 = null;
            string line3fix = null;
            string line3factor = null;
            string line4 = null;
            //
            line1 = "Name: " + _name + "   ";
            line1 += "Date: " + _dateTime.ToString(sysUIFormat) + "   Time: " + _dateTime.ToString("HH:mm:ss");
            //
            if (_fieldType == vtkMaxFieldDataType.Static)
            {
                line2 = "Step: #" + _stepNumber + "   Increment: #" + _incrementNumber +
                        "   Analysis time: " + _analysisTime.ToString(_numberFormat) + " " + _analysisTimeUnit;
            }
            else if (_fieldType == vtkMaxFieldDataType.Frequency)
            {
                line2 = "Step: #" + _stepNumber + "   Mode: #" + _incrementNumber;
                if (_nodalDiameter >= 0) line2 += "   Nodal diameter: " + _nodalDiameter;
                line2 += "   Frequency: " + _analysisTime.ToString(_numberFormat) + " " +
                         _analysisTimeUnit;
            }
            else if (_fieldType == vtkMaxFieldDataType.FrequencySensitivity)
            {
                // subtract 1 since first increment contains only normals
                line2 = "Step: #" + _stepNumber + "   Mode: #" + _incrementNumber + "   Frequency: " +
                        _analysisTime.ToString(_numberFormat) + " " + _analysisTimeUnit;
            }
            else if (_fieldType == vtkMaxFieldDataType.Buckling)
            {
                line2 = "Step: #" + _stepNumber + "   Buckling factor: " + _analysisTime.ToString(_numberFormat);
            }
            else if (_fieldType == vtkMaxFieldDataType.SteadyStateDynamic)
            {
                line2 = "Step: #" + _stepNumber + "   Data point: #" + _incrementNumber + "   Frequency: " +
                        _analysisTime.ToString(_numberFormat) + " " + _analysisTimeUnit;
            }
            else if (_fieldType == vtkMaxFieldDataType.LastIterations)
            {
                line2 = "Increment: #" + _stepNumber + "   Iteration: #" + _incrementNumber +
                        "   Analysis time: " + _analysisTime.ToString(_numberFormat) + " " + _analysisTimeUnit;
            }
            else throw new NotSupportedException();
            //
            line3fix = "Deformation variable: " + _deformationVariable + "   " + "Deformation scale factor: ";
            line3factor = _deformationScaleFactor.ToString(_numberFormat);
            //
            if (_animationScaleFactor >= 0)
            {
                if (_animationType == vtkMaxAnimationType.ScaleFactor)
                {
                    line4 = "Animation scale factor: " + _animationScaleFactor.ToString(_numberFormat);
                }
                else if (_animationType == vtkMaxAnimationType.TimeIncrements) { }
                else if (_animationType == vtkMaxAnimationType.Harmonic)
                {
                    line4 = "Complex angle: " + _animationScaleFactor.ToString(_numberFormat) + " " +
                            StringAngleDegConverter.GetUnitAbbreviation();
                }
            }
            //
            string text = line1 + Environment.NewLine + line2 + Environment.NewLine + line3fix + line3factor;
            if (line4 != null) text += Environment.NewLine + line4;
            SetText(text);
            //
            _lines = new string[] { line1, line2, line3fix, line3factor, line4 ?? "" };
            _nativeMirror?.SetStatusLines(_lines[0], _lines[1], _lines[2], _lines[3], _lines[4]);
        }
        protected override void CopyStateToNativeMirror(VtkNativeWidget nativeMirror)
        {
            if (_lines != null) nativeMirror.SetStatusLines(_lines[0], _lines[1], _lines[2], _lines[3], _lines[4]);
            base.CopyStateToNativeMirror(nativeMirror);
        }
    }
}
