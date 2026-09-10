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
    // The read_model tool, as the Python it runs. A tool that is not a protocol primitive is a piece
    // of Python the catalog holds rather than a method the application has to grow: it reads through
    // the same API an agent uses, so it cannot report something the API does not have, and adding
    // another one costs nothing but a string.
    //
    // A collection it cannot read is named in "unreadable" rather than left as a null nobody notices.
    // The first version of this script asked for boundary_conditions, which the step API calls bcs,
    // and the snapshot reported the step as having none instead of saying it had asked wrongly.
    public static class McpModelScript
    {
        // Variables                                                                                                                
        public const string Source =
            "import json\n" +
            "\n" +
            "unreadable = []\n" +
            "\n" +
            "\n" +
            "def names(owner, name, path):\n" +
            "    try:\n" +
            "        collection = getattr(owner, name)\n" +
            "        return list(collection.get_names())\n" +
            "    except Exception as reason:\n" +
            "        unreadable.append(path + ': ' + str(reason).splitlines()[0])\n" +
            "        return None\n" +
            "\n" +
            "\n" +
            "def value(owner, name, path):\n" +
            "    try:\n" +
            "        return getattr(owner, name)\n" +
            "    except Exception as reason:\n" +
            "        unreadable.append(path + ': ' + str(reason).splitlines()[0])\n" +
            "        return None\n" +
            "\n" +
            "\n" +
            "MODEL_COLLECTIONS = ['parts', 'node_sets', 'element_sets', 'surfaces', 'reference_points',\n" +
            "                     'coordinate_systems', 'materials', 'sections', 'constraints',\n" +
            "                     'surface_interactions', 'contact_pairs', 'distributions', 'amplitudes',\n" +
            "                     'initial_conditions', 'parameters', 'steps']\n" +
            "STEP_COLLECTIONS = ['bcs', 'loads', 'defined_fields', 'field_outputs', 'history_outputs']\n" +
            "PROPERTIES = ['name', 'model_space', 'unit_system', 'model_type']\n" +
            "\n" +
            "\n" +
            "def read_properties():\n" +
            "    properties = {}\n" +
            "    for name in PROPERTIES:\n" +
            "        properties[name] = value(pmx.model.properties, name, 'properties.' + name)\n" +
            "    return properties\n" +
            "\n" +
            "\n" +
            "def read_parameters():\n" +
            "    parameters = []\n" +
            "    for parameter_name in names(pmx.model, 'parameters', 'model.parameters') or []:\n" +
            "        parameter = pmx.model.parameters[parameter_name]\n" +
            "        parameters.append({'name': parameter_name,\n" +
            "                           'equation': value(parameter, 'equation', parameter_name + '.equation'),\n" +
            "                           'value': value(parameter, 'value', parameter_name + '.value'),\n" +
            "                           'description': value(parameter, 'description',\n" +
            "                                                parameter_name + '.description')})\n" +
            "    return parameters\n" +
            "\n" +
            "\n" +
            "def read_steps():\n" +
            "    steps = []\n" +
            "    for step_name in names(pmx.model, 'steps', 'model.steps') or []:\n" +
            "        step = pmx.model.steps[step_name]\n" +
            "        entry = {'name': step_name, 'type': value(step, 'step_type', step_name + '.step_type')}\n" +
            "        for collection in STEP_COLLECTIONS:\n" +
            "            entry[collection] = names(step, collection, step_name + '.' + collection)\n" +
            "        steps.append(entry)\n" +
            "    return steps\n" +
            "\n" +
            "\n" +
            "def read_jobs():\n" +
            "    jobs = []\n" +
            "    for job_name in names(pmx, 'jobs', 'jobs') or []:\n" +
            "        job = pmx.jobs[job_name]\n" +
            "        jobs.append({'name': job_name, 'status': value(job, 'status', job_name + '.status'),\n" +
            "                     'is_up_to_date': value(job, 'is_up_to_date', job_name + '.is_up_to_date'),\n" +
            "                     'results_file_name': value(job, 'results_file_name',\n" +
            "                                                job_name + '.results_file_name')})\n" +
            "    return jobs\n" +
            "\n" +
            "\n" +
            "def read_model():\n" +
            "    model = {'properties': read_properties()}\n" +
            "    for collection in MODEL_COLLECTIONS:\n" +
            "        model[collection] = names(pmx.model, collection, 'model.' + collection)\n" +
            "    # The geometry parts are the unmeshed ones; model parts are what meshing produced\n" +
            "    model['geometry_parts'] = names(pmx.model.geometry, 'parts', 'model.geometry.parts')\n" +
            "    # A parameter is read whole: a name alone says nothing about what it drives\n" +
            "    model['parameters_detail'] = read_parameters()\n" +
            "    model['steps_detail'] = read_steps()\n" +
            "    model['jobs'] = read_jobs()\n" +
            "    model['results'] = names(pmx, 'results', 'results')\n" +
            "    model['unreadable'] = unreadable\n" +
            "    return model\n" +
            "\n" +
            "\n" +
            "print(json.dumps(read_model(), indent=2, default=str))\n";
    }
}
