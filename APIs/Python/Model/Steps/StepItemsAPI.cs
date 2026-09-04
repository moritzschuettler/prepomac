// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;

namespace APIs.Python.Model
{
    // A collection that belongs to one step. The step name travels with the collection, so a step
    // scoped item is addressed the same way a model item is.
    public abstract class StepItemsAPI<TItem, TItemAPI> : ModelItemsAPI<TItem, TItemAPI>
        where TItem : NamedClass
    {
        // Variables
        private readonly string _stepName;


        // Properties
        [PythonAPI("The name of the step the collection belongs to.", Returns = "str")]
        public string step_name { get { return _stepName; } }
        protected string StepName { get { return _stepName; } }


        // Constructors
        protected StepItemsAPI(string itemTypeName, string stepName)
            : base(itemTypeName)
        {
            _stepName = stepName;
        }
    }
}
