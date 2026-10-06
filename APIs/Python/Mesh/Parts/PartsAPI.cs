// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python
{
    public class PartsAPI : BaseAPI, IReadOnlyList<PartAPI>
    {
        // Variables
        private readonly string _resultName;
        private readonly MeshDataService _meshService;
        private readonly IPythonCommandGateway _commandGateway;


        // Constructors
        internal PartsAPI(string resultName, MeshDataService meshService, IPythonCommandGateway commandGateway)
        {
            _resultName = resultName;
            _meshService = meshService;
            _commandGateway = commandGateway;
        }


        // Methods
        public PartAPI __getitem__(string partName)
        {
            return this[partName];
        }
        public PartAPI this[string partName]
        {
            get { return _meshService.GetPart(_resultName, partName); }
        }
        public int __len__()
        {
            return _meshService.GetPartNames(_resultName).Length;
        }
        // The collection interface is what makes len() and iteration work on the Python side; the
        // positional indexer is explicit so that the Python side resolves the name indexer
        public int Count { get { return __len__(); } }
        PartAPI IReadOnlyList<PartAPI>.this[int index]
        {
            get
            {
                string[] keys = get_names().to_array();
                if (index < 0) index = keys.Length + index;
                if (index < 0 || index >= keys.Length)
                    throw new PythonAPIException("The index " + index + " is out of range.");
                //
                return this[keys[index]];
            }
        }
        public IEnumerator<PartAPI> GetEnumerator()
        {
            foreach (string key in get_names().to_array()) yield return this[key];
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [PythonAPI("Returns result mesh part names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_meshService.GetPartNames(_resultName));
        }

        [PythonAPI("Returns the first result mesh part.", Returns = "Part")]
        public PartAPI first()
        {
            string[] names = _meshService.GetPartNames(_resultName);
            if (names.Length == 0) throw new PythonAPIException("The parts collection is empty.");
            return this[names[0]];
        }

        [PythonAPI("Returns the last result mesh part.", Returns = "Part")]
        public PartAPI last()
        {
            string[] names = _meshService.GetPartNames(_resultName);
            if (names.Length == 0) throw new PythonAPIException("The parts collection is empty.");
            return this[names[names.Length - 1]];
        }

        [PythonAPI("Returns true if the result mesh contains the specified part.", Arguments = "name: str", Returns = "bool")]
        public bool contains(string name)
        {
            return _meshService.GetPartNames(_resultName).Contains(name);
        }
        [PythonAPI("Hides result parts using command history.", Arguments = "names: list[str] or str",
                   Returns = "None")]
        public void hide(object names)
        {
            string[] partNames = ResolvePartNames(names, "names");
            EnsureCommandsAvailable();
            _commandGateway.HideResultParts(partNames);
        }
        [PythonAPI("Shows result parts using command history.", Arguments = "names: list[str] or str",
                   Returns = "None")]
        public void show(object names)
        {
            string[] partNames = ResolvePartNames(names, "names");
            EnsureCommandsAvailable();
            _commandGateway.ShowResultParts(partNames);
        }
        [PythonAPI("Sets the color of result parts using command history.",
                   Arguments = "part_names: list[str] or str, color: list[int] or str", Returns = "None")]
        public void set_color(object part_names, object color)
        {
            string[] names = ResolvePartNames(part_names);
            System.Drawing.Color value = PythonColorConverter.ToColor(color, "color");
            EnsureCommandsAvailable();
            _commandGateway.SetResultPartColor(names, value);
        }
        [PythonAPI("Restores the default color of result parts using command history.",
                   Arguments = "part_names: list[str] or str", Returns = "None")]
        public void reset_color(object part_names)
        {
            string[] names = ResolvePartNames(part_names);
            EnsureCommandsAvailable();
            _commandGateway.ResetResultPartColor(names);
        }
        [PythonAPI("Sets the transparency of result parts using command history. The value is the alpha " +
                   "channel, from 0 for invisible to 255 for opaque.",
                   Arguments = "part_names: list[str] or str, transparency: int", Returns = "None")]
        public void set_transparency(object part_names, object transparency)
        {
            string[] names = ResolvePartNames(part_names);
            byte alpha = PythonColorConverter.ToAlpha(transparency, "transparency");
            EnsureCommandsAvailable();
            _commandGateway.SetResultPartTransparency(names, alpha);
        }
        [PythonAPI("Turns the result representation of result parts on or off using command history.",
                   Arguments = "part_names: list[str] or str, result_representation: bool", Returns = "None")]
        public void set_representation(object part_names, bool result_representation)
        {
            string[] names = ResolvePartNames(part_names);
            EnsureCommandsAvailable();
            _commandGateway.SetResultPartRepresentation(names, result_representation);
        }
        [PythonAPI("Merges result parts into one using command history.",
                   Arguments = "part_names: list[str] or str", Returns = "None")]
        public void merge(object part_names)
        {
            string[] names = ResolvePartNames(part_names);
            EnsureCommandsAvailable();
            _commandGateway.MergeResultParts(names);
        }
        [PythonAPI("Replaces the properties of a result part using command history. A property that is not " +
                   "given is kept.",
                   Arguments = "old_name: str, name: str = None, color: list[int] or str = None",
                   Returns = "None")]
        public void replace_properties(string old_name, string name = null, object color = null)
        {
            if (old_name == null) throw new PythonAPIException("The argument 'old_name' must not be None.");
            ResolvePartNames(old_name);
            EnsureCommandsAvailable();
            CaeMesh.PartProperties properties =
                _meshService.ResolveResultWithMesh(_resultName).Mesh.Parts[old_name].GetProperties();
            if (name != null)
            {
                ValidateNewPartName(name, old_name);
                properties.Name = name;
            }
            if (color != null) properties.Color = PythonColorConverter.ToColor(color, "color");
            _commandGateway.ReplaceResultPartProperties(old_name, properties);
        }
        [PythonAPI("Renames a result part using command history.", Arguments = "old_name: str, new_name: str",
                   Returns = "None")]
        public void rename(string old_name, string new_name)
        {
            if (old_name == null) throw new PythonAPIException("The argument 'old_name' must not be None.");
            ResolvePartNames(old_name);
            CaeMesh.BasePart part = _meshService.ResolveResultWithMesh(_resultName).Mesh.Parts[old_name];
            PythonNameComparer.ValidateFormat(new_name, "new_name", part.Name);
            if (string.Equals(part.Name, new_name)) return;
            if (!PythonNameComparer.AreSame(new_name, part.Name) &&
                PythonNameComparer.Contains(_meshService.GetPartNames(_resultName), new_name))
                throw new PythonAPIException("The result part '" + new_name + "' already exists.");
            // The results view renames only the parts a result was read with
            if (!(part is CaeMesh.ResultPart))
                throw new PythonAPIException("The result part '" + old_name + "' is a " + part.GetType().Name +
                                             ", which cannot be renamed. Use replace_properties instead.");
            //
            EnsureCommandsAvailable();
            _commandGateway.RenameResultItem(part, new_name);
        }
        [PythonAPI("Removes result parts using command history.", Arguments = "names: list[str] or str",
                   Returns = "None")]
        public void remove(object names)
        {
            string[] partNames = ResolvePartNames(names, "names");
            EnsureCommandsAvailable();
            _commandGateway.RemoveResultParts(partNames);
        }
        //
        private void EnsureCommandsAvailable()
        {
            if (_commandGateway == null)
                throw new PythonAPIException("Result modifications are not available in this Python API context.");
            _meshService.EnsureCurrentResult(_resultName);
        }
        // The replace renames the live part before the collection is told, so a name the collection would refuse
        // is refused here, against the result part names the dialog checks
        private void ValidateNewPartName(string name, string oldName)
        {
            if (name.Trim().Length == 0) throw new PythonAPIException("The argument 'name' must not be empty.");
            string error = CaeGlobals.NamedClass.CheckNameError(name, null);
            if (error != null) throw new PythonAPIException("The argument 'name' is not valid: " + error);
            if (!PythonNameComparer.AreSame(name, oldName) &&
                PythonNameComparer.Contains(_meshService.GetPartNames(_resultName), name))
                throw new PythonAPIException("The result part '" + name + "' already exists.");
        }
        private string[] ResolvePartNames(object value)
        {
            return ResolvePartNames(value, "part_names");
        }
        private string[] ResolvePartNames(object value, string argumentName)
        {
            string[] names = PythonStringListConverter.ToStringArrayOrSingle(value, argumentName);
            if (names == null || names.Length == 0)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be empty.");
            //
            string[] existing = _meshService.GetPartNames(_resultName);
            foreach (string name in names)
            {
                if (!existing.Contains(name))
                    throw new PythonAPIException("The result part '" + name + "' does not exist.");
            }
            //
            return names;
        }
        public override string ToString()
        {
            string[] names = _meshService.GetPartNames(_resultName);
            return "Parts(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        protected override string ToFullString()
        {
            string[] names = _meshService.GetPartNames(_resultName);
            return "Parts(count=" + names.Length + ", names=" +
                   new StringListAPI(names).GetFullDisplayString() + ")";
        }
    }
}
