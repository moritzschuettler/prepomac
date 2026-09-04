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
        [PythonAPI("The distribution type: DistributionFromEquation, DistributionFromFile or " +
                   "MappedDistribution.", Returns = "str")]
        public string distribution_type
        {
            get { return _service.ResolveDistribution(_distributionName).GetType().Name; }
        }


        // Constructors
        internal DistributionAPI(string distributionName, DistributionService service)
        {
            _distributionName = distributionName;
            _service = service;
        }


        // Methods
        public override string ToString()
        {
            Distribution distribution = _service.ResolveDistribution(_distributionName);
            return "Distribution(name=" + StringListAPI.ToDisplayString(distribution.Name) +
                   ", type=" + distribution.GetType().Name + ")";
        }
    }
}
