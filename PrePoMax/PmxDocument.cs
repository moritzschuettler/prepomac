// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeGlobals;
using CaeJob;
using CaeModel;
using CaeResults;
using PrePoMax.Commands;
using UserControls;

namespace PrePoMax
{
    // What a .pmx file keeps: the model without its mesh, the results without their field data, the command history,
    // the annotations, the analyses and the state of the user interface. The mesh and the field data follow it in the
    // file in their own binary form.
    [PmxType(10522)]
    public class PmxDocument
    {
        // Variables                                                                                                                
        [PmxKey(0)] private FeModel _model;
        [PmxKey(1)] private ResultsCollection _allResults;
        [PmxKey(2)] private CommandsCollection _commands;
        [PmxKey(3)] private AnnotationContainer _annotations;
        [PmxKey(4)] private CaeGlobals.OrderedDictionary<string, AnalysisJob> _jobs;
        [PmxKey(5)] private bool[][] _treeStates;
        [PmxKey(6)] private SidePanelsState _sidePanelsState;


        // Properties                                                                                                               
        public FeModel Model { get { return _model; } set { _model = value; } }
        public ResultsCollection AllResults { get { return _allResults; } set { _allResults = value; } }
        public CommandsCollection Commands { get { return _commands; } }
        public AnnotationContainer Annotations { get { return _annotations; } }
        public CaeGlobals.OrderedDictionary<string, AnalysisJob> Jobs { get { return _jobs; } }
        public bool[][] TreeStates { get { return _treeStates; } }
        public SidePanelsState SidePanelsState { get { return _sidePanelsState; } }


        // Constructors                                                                                                             
        public PmxDocument(FeModel model, ResultsCollection allResults, CommandsCollection commands,
                           AnnotationContainer annotations, CaeGlobals.OrderedDictionary<string, AnalysisJob> jobs, bool[][] treeStates,
                           SidePanelsState sidePanelsState)
        {
            _model = model;
            _allResults = allResults;
            _commands = commands;
            _annotations = annotations;
            _jobs = jobs;
            _treeStates = treeStates;
            _sidePanelsState = sidePanelsState;
        }
    }
}
