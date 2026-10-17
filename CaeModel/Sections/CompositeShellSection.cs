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
using System.Runtime.Serialization;
using CaeGlobals;

namespace CaeModel
{
    // A shell section made of layers, written as a CalculiX composite shell. Layer 1 lies on the side opposite to the
    // shell normal and the layers stack along the normal. A symmetric layup is completed by its mirror image.
    [Serializable]
    public class CompositeShellSection : ShellSection, ISerializable
    {
        // Variables                                                                                                                
        private List<CompositeLayer> _layers;       //ISerializable
        private bool _symmetric;                    //ISerializable


        // Properties                                                                                                               
        public List<CompositeLayer> Layers { get { return _layers; } set { SetLayers(value); } }
        public bool Symmetric { get { return _symmetric; } set { _symmetric = value; UpdateTotalThickness(); } }


        // Constructors                                                                                                             
        public CompositeShellSection(string name, string regionName, RegionTypeEnum regionType,
                                     List<CompositeLayer> layers, bool twoD)
            : base(name, null, regionName, regionType, 1, twoD)
        {
            _symmetric = false;
            SetLayers(layers);
        }
        public CompositeShellSection(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_layers":
                        _layers = (List<CompositeLayer>)entry.Value; break;
                    case "_symmetric":
                        _symmetric = (bool)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        private void SetLayers(List<CompositeLayer> layers)
        {
            _layers = layers ?? new List<CompositeLayer>();
            // The section material is the one of layer 1, which colors the section by material
            MaterialName = _layers.Count > 0 ? _layers[0].MaterialName : null;
            UpdateTotalThickness();
        }
        // The layers the solver gets, from layer 1 up
        public List<CompositeLayer> GetSolverLayers()
        {
            List<CompositeLayer> layers = new List<CompositeLayer>(_layers);
            if (_symmetric)
            {
                for (int i = _layers.Count - 1; i >= 0; i--) layers.Add(_layers[i]);
            }
            return layers;
        }
        public double GetTotalThickness()
        {
            double thickness = 0;
            foreach (CompositeLayer layer in GetSolverLayers()) thickness += layer.Thickness.Value;
            return thickness;
        }
        public HashSet<string> GetMaterialNames()
        {
            HashSet<string> materialNames = new HashSet<string>();
            foreach (CompositeLayer layer in _layers) materialNames.Add(layer.MaterialName);
            return materialNames;
        }
        private void UpdateTotalThickness()
        {
            try
            {
                double thickness = GetTotalThickness();
                if (thickness > 0) Thickness.SetEquationFromValue(thickness);
            }
            catch { }
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            if (_layers.Count == 0) throw new CaeException("A composite shell section must contain at least one layer.");
            foreach (CompositeLayer layer in _layers) layer.CheckEquations();
            UpdateTotalThickness();
            //
            base.CheckEquations();
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_layers", _layers, typeof(List<CompositeLayer>));
            info.AddValue("_symmetric", _symmetric, typeof(bool));
        }
    }
}
