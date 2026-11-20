// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace UserControls
{
    [PmxType(6204)]
    public class PanelItemLinkLabel : PanelItem
    {
        // Variables                                                                                                                
        [PmxKey(0)] protected List<Action<object, EventArgs>> _actions;
        [PmxKey(1)] protected List<object> _senders;
        [PmxKey(2)] protected List<EventArgs> _eventArgs;


        // Properties                                                                                                               
        //public List<Action<object, EventArgs>> Actions { get { return _actions; } set { _actions = value; } }


        // Constructors                                                                                                             
        public PanelItemLinkLabel()
        {
            _actions = new List<Action<object, EventArgs>>();
            _senders = new List<object>();
            _eventArgs = new List<EventArgs>();
        }

        // Methods                                                                                                                  
        public void AddAction(Action<object, EventArgs> action)
        {
            AddAction(action, null, null);
        }
        public void AddAction(Action<object, EventArgs> action, object sender, EventArgs e)
        {
            _actions.Add(action);
            _senders.Add(sender);
            _eventArgs.Add(e);
        }
        public void Activate()
        {
            for (int i = 0; i < _actions.Count; i++) _actions[i](_senders[i], _eventArgs[i]);
        }
    }
}
