// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;
using System.Collections.Generic;

namespace APIs.Python.Model
{
    public class DistributionAPI : BaseAPI
    {
        // Variables
        private static readonly Dictionary<string, string[]> MappedArgumentValueNames =
            new Dictionary<string, string[]>
            {
                { "interpolator_radius", new string[] { "interpolator_radius" } },
                { "scale", new string[] { "scale_x", "scale_y", "scale_z" } },
                { "translate", new string[] { "translate_x", "translate_y", "translate_z" } }
            };
        private readonly string _distributionName;
        private readonly DistributionService _service;


        // Properties
        [PythonAPI("The distribution name.", Returns = "str")]
        public string name { get { return _service.ResolveDistribution(_distributionName).Name; } }
        [PythonAPI("The distribution type, named after the add_* method that makes it without the add_ " +
                   "prefix: from_equation or from_file.", Returns = "str")]
        public string item_type { get { return GetItemType(_service.ResolveDistribution(_distributionName)); } }
        [PythonAPI("The equation of a distribution defined by an equation, in the shape add_from_equation " +
                   "takes it: one string for a scalar distribution and a list of three, one per direction, for " +
                   "a vector one. None for a distribution read from a file.", Returns = "str, StringList or None")]
        public object equation { get { return GetEquation(Resolve()); } }
        [PythonAPI("The point cloud file of a distribution read from a file, or None.", Returns = "str or None")]
        public string file_name
        {
            get
            {
                DistributionFromFile distribution = Resolve() as DistributionFromFile;
                return distribution == null ? null : distribution.FileName;
            }
        }
        [PythonAPI("The interpolator type of a distribution read from a file: ClosestPoint, Gauss or Shepard. " +
                   "None for a distribution defined by an equation.", Returns = "str or None")]
        public string interpolator_type
        {
            get
            {
                MappedDistribution distribution = Resolve() as MappedDistribution;
                return distribution == null ? null : distribution.InterpolatorType.ToString();
            }
        }
        [PythonAPI("The interpolator radius of a distribution read from a file, in model units, or None. The " +
                   "ClosestPoint interpolator has no radius, so it reads back None.", Returns = "float or None")]
        public object interpolator_radius
        {
            get
            {
                MappedDistribution distribution = Resolve() as MappedDistribution;
                if (distribution == null || !UsesInterpolatorRadius(distribution)) return null;
                //
                return distribution.InterpolatorRadius.Value;
            }
        }
        [PythonAPI("The scale factors of the point cloud of a distribution read from a file, one per " +
                   "direction, or None.", Returns = "list[float] or None")]
        public object scale
        {
            get
            {
                MappedDistribution distribution = Resolve() as MappedDistribution;
                if (distribution == null) return null;
                //
                return PythonListConverter.ToPythonList(new double[] { distribution.ScaleX.Value,
                                                                       distribution.ScaleY.Value,
                                                                       distribution.ScaleZ.Value });
            }
        }
        [PythonAPI("The translation of the point cloud of a distribution read from a file, in model units, or " +
                   "None.", Returns = "list[float] or None")]
        public object translate
        {
            get
            {
                MappedDistribution distribution = Resolve() as MappedDistribution;
                if (distribution == null) return null;
                //
                return PythonListConverter.ToPythonList(new double[] { distribution.TranslateX.Value,
                                                                       distribution.TranslateY.Value,
                                                                       distribution.TranslateZ.Value });
            }
        }
        [PythonAPI("The name of the coordinate system the distribution is defined in.", Returns = "str")]
        public string coordinate_system_name { get { return Resolve().CoordinateSystemName; } }
        [PythonAPI("True when the distribution resolved during the last regeneration: its coordinate system.",
                   Returns = "bool")]
        public bool valid { get { return Resolve().Valid; } }


        // Constructors
        internal DistributionAPI(string distributionName, DistributionService service)
        {
            _distributionName = distributionName;
            _service = service;
        }


        // Static methods
        internal static string GetItemType(Distribution distribution)
        {
            if (distribution is DistributionFromEquation) return "from_equation";
            if (distribution is DistributionFromFile) return "from_file";
            //
            throw new PythonAPIException("The distribution '" + distribution.Name + "' is a " +
                                         distribution.GetType().Name + ", which no add_* method of the Python API " +
                                         "makes.");
        }
        private static bool UsesInterpolatorRadius(MappedDistribution distribution)
        {
            return distribution.InterpolatorType != CaeResults.CloudInterpolatorEnum.ClosestPoint;
        }
        private static object GetEquation(Distribution distribution)
        {
            DistributionFromEquation fromEquation = distribution as DistributionFromEquation;
            if (fromEquation == null) return null;
            if (fromEquation.DistributionType == DistributionTypeEnum.Scalar) return fromEquation.EquationMagnitude;
            //
            return new StringListAPI(new string[] { fromEquation.EquationD1, fromEquation.EquationD2,
                                                    fromEquation.EquationD3 });
        }


        // Methods
        [PythonAPI("Returns the expressions a distribution read from a file was entered with, without " +
                   "evaluating them, under the add_from_file argument names: interpolator_radius, and scale and " +
                   "translate as lists of three. Only a value entered as an expression is returned, with its " +
                   "leading equals sign; a value entered as a plain number is left out, and the property of the " +
                   "same name returns it as a number. A list with an expression in any component is returned " +
                   "whole, its plain components as numbers. A distribution defined by an equation returns an " +
                   "empty dict; its equation is the equation property.", Returns = "dict[str, str or list]")]
        public Dictionary<string, object> get_equations()
        {
            Distribution distribution = Resolve();
            MappedDistribution mappedDistribution = distribution as MappedDistribution;
            if (mappedDistribution == null) return new Dictionary<string, object>();
            //
            Dictionary<string, object> equations = ModelItemValueReader.GetEquations(distribution,
                                                                                     MappedArgumentValueNames);
            if (!UsesInterpolatorRadius(mappedDistribution)) equations.Remove("interpolator_radius");
            return equations;
        }
        public override string ToString()
        {
            Distribution distribution = _service.ResolveDistribution(_distributionName);
            return "Distribution(name=" + StringListAPI.ToDisplayString(distribution.Name) +
                   ", type=" + GetItemType(distribution) + ")";
        }
        //
        private Distribution Resolve()
        {
            return _service.ResolveDistribution(_distributionName);
        }
    }
}
