// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;

namespace PrePoMax.API.Python
{
    public class ResultFieldOutputFilterAPI : BaseAPI
    {
        // Variables
        private readonly FieldResultFilterTypeEnum _type;
        private readonly FieldResultFilterSourceTypeEnum _source;
        private readonly int _layers;
        private readonly int _iterations;


        // Properties
        [PythonAPI("The filter type. Possible values are 'median', 'average', and 'smooth'.", Returns = "str")]
        public string type { get { return GetTypeName(_type); } }
        [PythonAPI("The source nodes. Possible values are 'all_nodes' and 'surface_nodes'.", Returns = "str")]
        public string source { get { return GetSourceName(_source); } }
        [PythonAPI("The number of neighbouring node layers.", Returns = "int")]
        public int layers { get { return _layers; } }
        [PythonAPI("The number of filter iterations.", Returns = "int")]
        public int iterations { get { return _iterations; } }


        // Constructors
        internal ResultFieldOutputFilterAPI(FieldResultFilterTypeEnum type,
                                            FieldResultFilterSourceTypeEnum source,
                                            int layers, int iterations)
        {
            _type = type;
            _source = source;
            _layers = layers;
            _iterations = iterations;
        }


        // Methods
        internal static ResultFieldOutputFilterAPI FromBase(FieldResultFilter filter)
        {
            if (filter == null || filter.Type == FieldResultFilterTypeEnum.None) return null;
            return new ResultFieldOutputFilterAPI(filter.Type, filter.SourceType,
                                                  filter.NumberOfNeighbouringLayers,
                                                  filter.NumberOfIterations);
        }
        internal FieldResultFilter CreateBase()
        {
            FieldResultFilter filter = new FieldResultFilter();
            filter.Type = _type;
            filter.SourceType = _source;
            filter.NumberOfNeighbouringLayers = _layers;
            filter.NumberOfIterations = _iterations;
            return filter;
        }
        public override string ToString()
        {
            return "FieldOutputFilter(type=" + StringListAPI.ToDisplayString(type) +
                   ", source=" + StringListAPI.ToDisplayString(source) +
                   ", layers=" + _layers + ", iterations=" + _iterations + ")";
        }
        private static string GetTypeName(FieldResultFilterTypeEnum type)
        {
            if (type == FieldResultFilterTypeEnum.Median) return "median";
            if (type == FieldResultFilterTypeEnum.Average) return "average";
            if (type == FieldResultFilterTypeEnum.Smooth) return "smooth";
            throw new PythonAPIException("Unsupported field filter type '" + type + "'.");
        }
        private static string GetSourceName(FieldResultFilterSourceTypeEnum source)
        {
            if (source == FieldResultFilterSourceTypeEnum.AllNodes) return "all_nodes";
            if (source == FieldResultFilterSourceTypeEnum.SurfaceNodes) return "surface_nodes";
            throw new PythonAPIException("Unsupported field filter source '" + source + "'.");
        }
    }
}
