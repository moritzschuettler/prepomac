// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;

namespace APIs.Python
{
    public class ResultFieldOutputFilterAPI : BaseAPI
    {
        // Variables
        private readonly FieldResultFilterTypeEnum _type;
        private readonly FieldResultFilterSourceTypeEnum _source;
        private readonly int _layers;
        private readonly int _iterations;


        // Properties
        [PythonAPI("The filter type: Median, Average or Smooth.", Returns = "str")]
        public string type { get { return _type.ToString(); } }
        [PythonAPI("The source nodes: AllNodes or SurfaceNodes.", Returns = "str")]
        public string source { get { return _source.ToString(); } }
        [PythonAPI("The number of neighbouring node layers.", Returns = "int")]
        public int number_of_layers { get { return _layers; } }
        [PythonAPI("The number of filter iterations.", Returns = "int")]
        public int number_of_iterations { get { return _iterations; } }


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
                   ", number_of_layers=" + _layers + ", number_of_iterations=" + _iterations + ")";
        }
    }
}
