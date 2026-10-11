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
        public PartAPI __getitem__(object partName)
        {
            string partNameText = PythonValueConverter.ToText(partName, "name", false);
            return this[partNameText];
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
                int position = index < 0 ? keys.Length + index : index;
                if (position < 0 || position >= keys.Length)
                    throw new PythonAPIException("The index " + index + " is out of range.");
                //
                return this[keys[position]];
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
        public bool __contains__(object partName)
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
        public bool contains(object name)
        {
            string nameText = PythonValueConverter.ToText(name, "name", true);
            return PythonNameComparer.Contains(_meshService.GetPartNames(_resultName), nameText);
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
                   "[R, G, B, A] with channels from 0 to 255, or a .NET color name such as 'Red'. As in the " +
                   "dialog, an alpha below 25 is refused, so a part never becomes invisible.",
                   Arguments = "part_names: list[str] or str, color: list[int] or str", Returns = "None")]
        public void set_color(object part_names, object color)
        {
            string[] names = ResolvePartNames(part_names);
            System.Drawing.Color value = PythonColorConverter.ToPartColor(color, "color");
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
                   "as an integer from 25, the most transparent value the dialog allows, to 255 for opaque.",
                   Arguments = "part_names: list[str] or str, opacity: int", Returns = "None")]
        public void set_opacity(object part_names, object opacity)
        {
            string[] names = ResolvePartNames(part_names);
            byte alpha = PythonColorConverter.ToPartAlpha(opacity, "opacity");
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
                   "given is kept. A color with an alpha below 25 is refused, as in the dialog.",
                   Arguments = "old_name: str, name: str = None, color: list[int] or str = None",
                   Returns = "None")]
        public void replace_properties(object old_name, object name = null, object color = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", true);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            if (oldName == null) throw new PythonAPIException("The argument 'old_name' must not be None.");
            string storedOldName = ResolvePartNames(oldName, "old_name")[0];
            EnsureCommandsAvailable();
            CaeMesh.PartProperties properties =
                _meshService.ResolveResultWithMesh(_resultName).Mesh.Parts[storedOldName].GetProperties();
            if (nameText != null)
            {
                ValidateNewPartName(nameText, storedOldName);
                properties.Name = nameText;
            }
            if (color != null) properties.Color = PythonColorConverter.ToPartColor(color, "color");
            _commandGateway.ReplaceResultPartProperties(storedOldName, properties);
        }
        [PythonAPI("Renames a result part using command history.", Arguments = "old_name: str, new_name: str",
                   Returns = "None")]
        public void rename(object old_name, object new_name)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", true);
            string newName = PythonValueConverter.ToText(new_name, "new_name", false);
            if (oldName == null) throw new PythonAPIException("The argument 'old_name' must not be None.");
            string storedOldName = ResolvePartNames(oldName, "old_name")[0];
            CaeMesh.BasePart part = _meshService.ResolveResultWithMesh(_resultName).Mesh.Parts[storedOldName];
            PythonNameComparer.ValidateFormat(newName, "new_name", part.Name);
            if (string.Equals(part.Name, newName)) return;
            if (!PythonNameComparer.AreSame(newName, part.Name) &&
                PythonNameComparer.Contains(_meshService.GetPartNames(_resultName), newName))
                throw new PythonAPIException("The result part '" + newName + "' already exists.");
            // The results view renames only the parts a result was read with
            if (!(part is CaeMesh.ResultPart))
                throw new PythonAPIException("The result part '" + oldName + "' is a " + part.GetType().Name +
                                             ", which cannot be renamed. Use replace_properties instead.");
            //
            EnsureCommandsAvailable();
            _commandGateway.RenameResultItem(part, newName);
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
