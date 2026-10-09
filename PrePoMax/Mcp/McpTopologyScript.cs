// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using PrePoMax.Python.Journal;
using System.Text;

namespace PrePoMax.Mcp
{
    // The read_topology tool, as the Python it runs. It answers the question a model snapshot cannot:
    // which faces, edges and vertices a part has, where each one is, and which ID to hand a selection.
    // Without it a caller that cannot see the viewport has to guess a point and hope the nearest face
    // is the one it meant.
    //
    // The answer is bounded twice, because an assembly of eighty parts with a hundred faces each
    // would otherwise bury it. Each part lists at most a given number of faces, edges and vertices -
    // the largest faces and the longest edges first, which is the order they are worth reading in, and
    // the vertices, which have no size, in index order - and no more than what is left of a budget the
    // whole answer shares, which every listed row is taken from, so the answer never lists more rows
    // than the budget. A part reached once it is spent is not detailed. Every part is still summarized, so
    // the caller sees what is there and can ask for one part by name. A part name is compared the way
    // the model keys its parts, regardless of case; it is not trimmed, so a padded name matches no part,
    // and a name that matched no part is reported rather than silently left out. Like read_model it runs
    // as one private function, so it leaves none of its names in the console scope.
    public static class McpTopologyScript
    {
        // Variables                                                                                                                
        private const string Source =
            "import json\n" +
            "\n" +
            "\n" +
            "def coordinates(array, index):\n" +
            "    return [round(float(value), 6) for value in list(array[index])]\n" +
            "\n" +
            "\n" +
            "def largest_first(values, limit):\n" +
            "    order = sorted(range(len(values)), key=lambda i: -abs(values[i]))\n" +
            "    return order[:limit]\n" +
            "\n" +
            "\n" +
            "def face_rows(faces, limit):\n" +
            "    areas = [float(value) for value in list(faces.areas)]\n" +
            "    types = list(faces.types)\n" +
            "    listed = []\n" +
            "    for i in largest_first(areas, limit):\n" +
            "        listed.append({'id': int(faces.ids[i]), 'geometry_id': int(faces.geometry_ids[i]),\n" +
            "                       'area': round(areas[i], 6), 'type': str(types[i]),\n" +
            "                       'centroid': coordinates(faces.centroids, i),\n" +
            "                       'normal': coordinates(faces.normals, i)})\n" +
            "    return listed\n" +
            "\n" +
            "\n" +
            "def edge_rows(edges, limit):\n" +
            "    lengths = [float(value) for value in list(edges.lengths)]\n" +
            "    types = list(edges.types)\n" +
            "    listed = []\n" +
            "    for i in largest_first(lengths, limit):\n" +
            "        listed.append({'id': int(edges.ids[i]), 'geometry_id': int(edges.geometry_ids[i]),\n" +
            "                       'length': round(lengths[i], 6), 'type': str(types[i]),\n" +
            "                       'centroid': coordinates(edges.centroids, i)})\n" +
            "    return listed\n" +
            "\n" +
            "\n" +
            "def vertex_rows(vertices, limit):\n" +
            "    listed = []\n" +
            "    for i in range(min(vertices.count, limit)):\n" +
            "        listed.append({'node_id': int(vertices.node_ids[i]),\n" +
            "                       'geometry_id': int(vertices.geometry_ids[i]),\n" +
            "                       'coordinates': coordinates(vertices.coordinates, i)})\n" +
            "    return listed\n" +
            "\n" +
            "\n" +
            "def summarize(part, faces, edges, vertices):\n" +
            "    return {'name': part.name, 'id': part.id, 'part_type': part.part_type,\n" +
            "            'node_count': part.node_count, 'element_count': part.element_count,\n" +
            "            'bounding_box': [round(v, 6) for v in part.get_bounding_box()],\n" +
            "            'face_count': faces.count, 'edge_count': edges.count,\n" +
            "            'vertex_count': vertices.count}\n" +
            "\n" +
            "\n" +
            "def read_part(part, budget):\n" +
            "    faces = part.get_face_data()\n" +
            "    edges = part.get_edge_data()\n" +
            "    vertices = part.get_vertex_data()\n" +
            "    entry = summarize(part, faces, edges, vertices)\n" +
            "    if budget[0] <= 0:\n" +
            "        entry['detail'] = ('Not listed: the answer is full. Ask for this part by name to see its '\n" +
            "                           'faces, edges and vertices.')\n" +
            "        return entry\n" +
            "    entry['faces'] = face_rows(faces, min(LIMIT, budget[0]))\n" +
            "    budget[0] -= len(entry['faces'])\n" +
            "    entry['edges'] = edge_rows(edges, min(LIMIT, budget[0]))\n" +
            "    budget[0] -= len(entry['edges'])\n" +
            "    entry['vertices'] = vertex_rows(vertices, min(LIMIT, budget[0]))\n" +
            "    budget[0] -= len(entry['vertices'])\n" +
            "    if (len(entry['faces']) < faces.count or len(entry['edges']) < edges.count or\n" +
            "            len(entry['vertices']) < vertices.count):\n" +
            "        entry['listed'] = ('Listed %d of %d faces and %d of %d edges, largest first, and %d of %d '\n" +
            "                           'vertices, in index order. Read the rest with get_face_data(), '\n" +
            "                           'get_edge_data() and get_vertex_data() on the part, or ask for this '\n" +
            "                           'part by name.'\n" +
            "                           % (len(entry['faces']), faces.count, len(entry['edges']), edges.count,\n" +
            "                              len(entry['vertices']), vertices.count))\n" +
            "    return entry\n" +
            "\n" +
            "\n" +
            "def read_topology():\n" +
            "    budget = [BUDGET]\n" +
            "    wanted = {name.lower(): name for name in PART_NAMES}\n" +
            "    matched = set()\n" +
            "    topology = {'note': 'A face or an edge is selected by its id with the surfaces or edges '\n" +
            "                        'source, grouped by part id, or by its geometry_id with the geometry '\n" +
            "                        'source; a vertex is selected by its node id. Areas, lengths and '\n" +
            "                        'coordinates are in model units.'}\n" +
            "    for view, parts in [('model', pmx.model.parts), ('geometry', pmx.model.geometry.parts)]:\n" +
            "        listed = []\n" +
            "        for name in list(parts.get_names()):\n" +
            "            key = name.lower()\n" +
            "            if wanted and key not in wanted:\n" +
            "                continue\n" +
            "            matched.add(key)\n" +
            "            try:\n" +
            "                listed.append(read_part(parts[name], budget))\n" +
            "            except Exception as reason:\n" +
            "                listed.append({'name': name, 'unreadable': str(reason).splitlines()[0]})\n" +
            "        topology[view] = listed\n" +
            "    unmatched = [wanted[key] for key in wanted if key not in matched]\n" +
            "    if unmatched:\n" +
            "        topology['unmatched_part_names'] = unmatched\n" +
            "        topology['unmatched'] = ('No geometry or model part is named %s. The part names are listed by '\n" +
            "                                 'read_model.' % ', '.join(unmatched))\n" +
            "    return topology\n" +
            "\n" +
            "\n" +
            "print(json.dumps(read_topology(), indent=2, default=str))\n";


        // Static methods                                                                                                           
        // The script with its inputs bound, so that the tool stays a piece of Python the catalog holds
        // rather than a method the application has to grow
        public static string GetSource(string[] partNames, int limit, int budget)
        {
            return McpPrivateScript.Wrap("PART_NAMES = " + ToPythonList(partNames) + "\n" +
                                         "LIMIT = " + limit + "\n" +
                                         "BUDGET = " + budget + "\n" +
                                         Source);
        }
        private static string ToPythonList(string[] names)
        {
            if (names == null || names.Length == 0) return "[]";
            //
            StringBuilder text = new StringBuilder("[");
            for (int i = 0; i < names.Length; i++)
            {
                if (i > 0) text.Append(", ");
                text.Append(PythonValueFormatter.Text(names[i]));
            }
            //
            return text.Append("]").ToString();
        }
    }
}
