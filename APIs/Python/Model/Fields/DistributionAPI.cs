// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;

namespace APIs.Python.Model
{
    public class DistributionAPI : BaseAPI
    {
        // Variables
        private readonly string _distributionName;
        private readonly DistributionService _service;


        // Properties
        [PythonAPI("The distribution name.", Returns = "str")]
        public string name { get { return _service.ResolveDistribution(_distributionName).Name; } }
        [PythonAPI("The distribution type, named after the add_* method that makes it without the add_ " +
                   "prefix: from_equation or from_file.", Returns = "str")]
        public string item_type { get { return GetItemType(_service.ResolveDistribution(_distributionName)); } }


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


        // Methods
        public override string ToString()
        {
            Distribution distribution = _service.ResolveDistribution(_distributionName);
            return "Distribution(name=" + StringListAPI.ToDisplayString(distribution.Name) +
                   ", type=" + GetItemType(distribution) + ")";
        }
    }
}
