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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace PrePoMax
{
    [Serializable]
    public class ExplodedViewsCollection
    {
        // Variables                                                                                                                
        [NonSerialized] private Controller _controller;
        private ExplodedViewParameters _geometryExplodedViewParameters;
        private ExplodedViewParameters _modelExplodedViewParameters;
        private Dictionary<string, ExplodedViewParameters> _resultNameExplodedViewParameters;
        private ExplodedViewParameters _lastUsedParameters;


        // Properties                                                                                                               
        public string CurrentResultName { get { return _controller.AllResults.GetCurrentResultName(); } }


        // Constructors                                                                                                             
        public ExplodedViewsCollection(Controller controller)
        {
            _controller = controller;
            ClearModelExplodedViews();
            _resultNameExplodedViewParameters = new Dictionary<string, ExplodedViewParameters>();
            //
            _lastUsedParameters = new ExplodedViewParameters();
            _lastUsedParameters.ScaleFactor = 0.5;
        }


        // Methods                                                                                                                  
        public bool IsExplodedViewActive()
        {
            return GetCurrentExplodedViewParametersNull() != null;
        }
        public bool IsGeometryExplodedViewActive()
        {
            return _geometryExplodedViewParameters != null;
        }
        public bool IsModelExplodedViewActive()
        {
            return _modelExplodedViewParameters != null;
        }
        public bool IsResultExplodedViewActive(string name)
        {
            ExplodedViewParameters parameters;
            _resultNameExplodedViewParameters.TryGetValue(name, out parameters);
            return parameters != null;
        }
        public ExplodedViewParameters GetResultExplodedViewParameters(string name)
        {
            ExplodedViewParameters parameters;
            _resultNameExplodedViewParameters.TryGetValue(name, out parameters);
            return parameters;
        }
        /// <summary>
        /// The parameters of the applied exploded view, or the settings that were last applied when there
        /// is none. The scale factor of the remembered settings is reset to -1, which is what the callers
        /// use to recognize that no exploded view is applied.
        /// </summary>
        public ExplodedViewParameters GetCurrentExplodedViewParameters()
        {
            ExplodedViewParameters parameters = GetCurrentExplodedViewParametersNull();
            if (parameters == null)
            {
                parameters = _lastUsedParameters.DeepClone();
                parameters.ScaleFactor = -1;
            }
            return parameters;
        }
        /// <summary>
        /// The scale factor the exploded view was last applied with, used as the starting position when it
        /// is turned on again
        /// </summary>
        public double GetLastUsedScaleFactor()
        {
            return _lastUsedParameters.ScaleFactor;
        }
        private ExplodedViewParameters GetCurrentExplodedViewParametersNull()
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry) return _geometryExplodedViewParameters;
            else if (_controller.CurrentView == ViewGeometryModelResults.Model) return _modelExplodedViewParameters;
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                string name = CurrentResultName;
                ExplodedViewParameters parameters;
                if (name != null && _resultNameExplodedViewParameters.TryGetValue(name, out parameters)) return parameters;
                else return null;
            }
            else throw new NotSupportedException();
        }
        public void SetCurrentExplodedViewParameters(ExplodedViewParameters parameters)
        {
            ExplodedViewParameters evp;
            if (parameters.ScaleFactor == -1) evp = null;
            else
            {
                evp = parameters;
                _lastUsedParameters = parameters.DeepClone();
            }
            //
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry) _geometryExplodedViewParameters = evp;
            else if (_controller.CurrentView == ViewGeometryModelResults.Model) _modelExplodedViewParameters = evp;
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                string name = CurrentResultName;
                if (name != null)
                {
                    if (!_resultNameExplodedViewParameters.ContainsKey(name))
                        _resultNameExplodedViewParameters.Add(name, evp);
                    else _resultNameExplodedViewParameters[name] = evp;
                }
            }
            else throw new NotSupportedException();
        }


        // Clear
        public void ClearModelExplodedViews()
        {
            _geometryExplodedViewParameters = null;
            _modelExplodedViewParameters = null;
        }
        public void ClearResultsExplodedViews()
        {
            _resultNameExplodedViewParameters.Clear();
        }
        // Remove
        public void RemoveCurrentExplodedView()
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry) _geometryExplodedViewParameters = null;
            else if (_controller.CurrentView == ViewGeometryModelResults.Model) _modelExplodedViewParameters = null;
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                string name = CurrentResultName;
                if (name != null) _resultNameExplodedViewParameters.Remove(CurrentResultName);
            }
            else throw new NotSupportedException();
        }
    }
}
