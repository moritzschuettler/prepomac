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
    public class SectionAPI : BaseAPI
    {
        // Variables
        private readonly string _sectionName;
        private readonly SectionService _service;


        // Properties
        [PythonAPI("The section name.", Returns = "str")]
        public string name { get { return _service.ResolveSection(_sectionName).Name; } }
        [PythonAPI("The section type, for example SolidSection or ShellSection.", Returns = "str")]
        public string section_type { get { return _service.ResolveSection(_sectionName).GetType().Name; } }
        [PythonAPI("The name of the material used by the section.", Returns = "str")]
        public string material_name { get { return _service.ResolveSection(_sectionName).MaterialName; } }
        [PythonAPI("The name of the region the section is applied to.", Returns = "str")]
        public string region_name { get { return _service.ResolveSection(_sectionName).RegionName; } }
        [PythonAPI("The region type the section is applied to.", Returns = "str")]
        public string region_type { get { return _service.ResolveSection(_sectionName).RegionType.ToString(); } }
        [PythonAPI("The section thickness.", Returns = "float")]
        public double thickness { get { return _service.ResolveSection(_sectionName).Thickness.Value; } }
        [PythonAPI("True when the section resolved during the last regeneration.", Returns = "bool")]
        public bool valid { get { return _service.ResolveSection(_sectionName).Valid; } }


        // Constructors
        internal SectionAPI(string sectionName, SectionService service)
        {
            _sectionName = sectionName;
            _service = service;
        }


        // Methods
        public override string ToString()
        {
            Section section = _service.ResolveSection(_sectionName);
            return "Section(name=" + StringListAPI.ToDisplayString(section.Name) +
                   ", type=" + section.GetType().Name +
                   ", material=" + StringListAPI.ToDisplayString(section.MaterialName) + ")";
        }
    }
}
