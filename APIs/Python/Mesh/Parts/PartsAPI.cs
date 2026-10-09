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
                string[] keys = get_names().ToArray();
                if (index < 0) index = keys.Length + index;
                if (index < 0 || index >= keys.Length)
                    throw new PythonAPIException("The index " + index + " is out of range.");
                //
                return this[keys[index]];
            }
        }
        public IEnumerator<PartAPI> GetEnumerator()
        {
            foreach (string key in get_names().ToArray()) yield return this[key];
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public bool __contains__(string partName)
        {
            return contains(partName);
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
            return PythonNameComparer.Contains(_meshService.GetPartNames(_resultName), name);
        }
        [PythonAPI("Hides result parts using command history.", Arguments = "part_names: list[str] or str",
                   Returns = "None")]
        public void hide(object part_names)
        {
            string[] partNames = ResolvePartNames(part_names, "part_names");
            EnsureCommandsAvailable();
            _commandGateway.HideResultParts(partNames);
        }
        [PythonAPI("Shows result parts using command history.", Arguments = "part_names: list[str] or str",
                   Returns = "None")]
        public void show(object part_names)
        {
            string[] partNames = ResolvePartNames(part_names, "part_names");
            EnsureCommandsAvailable();
            _commandGateway.ShowResultParts(partNames);
        }
        [PythonAPI("Sets the color of result parts using command history. The color is [R, G, B] or " +
                   "[R, G, B, A] with channels from 0 to 255, or a .NET color name such as 'Red'.",
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
        [PythonAPI("Sets the opacity of result parts using command history: the alpha channel of their color, " +
                   "as an integer from 0 for fully transparent to 255 for opaque.",
                   Arguments = "part_names: list[str] or str, opacity: int", Returns = "None")]
        public void set_opacity(object part_names, object opacity)
        {
            string[] names = ResolvePartNames(part_names);
            byte alpha = PythonColorConverter.ToAlpha(opacity, "opacity");
            EnsureCommandsAvailable();
            _commandGateway.SetResultPartTransparency(names, alpha);
        }
        [PythonAPI("Turns the result representation of result parts on or off using command history.",
                   Arguments = "part_names: list[str] or str, result_representation: bool", Returns = "None")]
        public void set_representation(object part_names, object result_representation)
        {
            string[] names = ResolvePartNames(part_names);
            bool resultRepresentation = PythonValueConverter.ToBool(result_representation, "result_representation");
            EnsureCommandsAvailable();
            _commandGateway.SetResultPartRepresentation(names, resultRepresentation);
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
            string storedOldName = ResolvePartNames(old_name, "old_name")[0];
            EnsureCommandsAvailable();
            CaeMesh.PartProperties properties =
                _meshService.ResolveResultWithMesh(_resultName).Mesh.Parts[storedOldName].GetProperties();
            if (name != null)
            {
                ValidateNewPartName(name, storedOldName);
                properties.Name = name;
            }
            if (color != null) properties.Color = PythonColorConverter.ToColor(color, "color");
            _commandGateway.ReplaceResultPartProperties(storedOldName, properties);
        }
        [PythonAPI("Renames a result part using command history.", Arguments = "old_name: str, new_name: str",
                   Returns = "None")]
        public void rename(string old_name, string new_name)
        {
            if (old_name == null) throw new PythonAPIException("The argument 'old_name' must not be None.");
            string storedOldName = ResolvePartNames(old_name, "old_name")[0];
            CaeMesh.BasePart part = _meshService.ResolveResultWithMesh(_resultName).Mesh.Parts[storedOldName];
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
            PythonNameComparer.ValidateFormat(name, "name", oldName);
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
            string[] storedNames = new string[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                storedNames[i] = existing.FirstOrDefault(name => PythonNameComparer.AreSame(name, names[i]));
                if (storedNames[i] == null)
                    throw new PythonAPIException("The result part '" + names[i] + "' does not exist.");
            }
            //
            return storedNames;
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
