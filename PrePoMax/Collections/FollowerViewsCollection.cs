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
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax
{
    [Serializable]
    public class FollowerViewsCollection
    {
        // Variables                                                                                                                
        [NonSerialized] private Controller _controller;
        private Dictionary<string, FollowerViewParameters> _resultNameViewParameters;


        // Properties                                                                                                               
        public string CurrentResultName { get { return _controller.AllResults.GetCurrentResultName(); } }


        // Constructors                                                                                                             
        public FollowerViewsCollection(Controller controller)
        {
            _controller = controller;
            _resultNameViewParameters = new Dictionary<string, FollowerViewParameters>();
            _controller.ClearAllFollowerViews();
        }


        // Methods                                                                                                                  
        public bool IsFollowerViewActive()
        {
            return GetCurrentFollowerViewParameters() != null;
        }
        public FollowerViewParameters GetCurrentFollowerViewParameters()
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry) return null;
            else if (_controller.CurrentView == ViewGeometryModelResults.Model) return null;
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                string name = CurrentResultName;
                if (name != null && _resultNameViewParameters.TryGetValue(name, out FollowerViewParameters followerViewParameters))
                    return followerViewParameters;
                else return null;
            }
            else throw new NotSupportedException();
        }
        public void SetCurrentFollowerViewParameters(FollowerViewParameters followerViewParameters)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry) { }
            else if (_controller.CurrentView == ViewGeometryModelResults.Model) { }
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                string name = CurrentResultName;
                if (name != null) _resultNameViewParameters[name] = followerViewParameters;
            }
            else throw new NotSupportedException();
        }
        // Clear
        public void ClearResultFollowerViews()
        {
            _resultNameViewParameters.Clear();
        }
        // Remove
        public void RemoveCurrentFollowerView()
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry) { }
            else if (_controller.CurrentView == ViewGeometryModelResults.Model) { }
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                string name = CurrentResultName;
                if (name != null) _resultNameViewParameters.Remove(CurrentResultName);
            }
            else throw new NotSupportedException();
        }
        
    }
}
