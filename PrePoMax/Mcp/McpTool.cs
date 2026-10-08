// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace PrePoMax.Mcp
{
    // One tool as an agent sees it. The schema is written as JSON rather than built from a type,
    // because it is documentation for a reader that has to get the call right first time. A tool is
    // read only - sent to the client as readOnlyHint - only when it changes nothing: not the model, not
    // the view and not a file, and it runs no Python the caller wrote.
    public class McpTool
    {
        // Variables                                                                                                                
        private readonly string _name;
        private readonly string _title;
        private readonly string _description;
        private readonly string _inputSchema;
        private readonly bool _isReadOnly;


        // Properties                                                                                                               
        public string Name { get { return _name; } }
        public string Title { get { return _title; } }
        public string Description { get { return _description; } }
        public string InputSchema { get { return _inputSchema; } }
        public bool IsReadOnly { get { return _isReadOnly; } }


        // Constructor                                                                                                              
        public McpTool(string name, string title, string description, string inputSchema, bool isReadOnly)
        {
            _name = name;
            _title = title;
            _description = description;
            _inputSchema = inputSchema;
            _isReadOnly = isReadOnly;
        }
    }
}
