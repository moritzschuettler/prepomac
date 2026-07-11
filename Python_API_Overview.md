# PrePoMax Python API Overview

This document is a simple overview of the Python API objects exposed by PrePoMax.
The root object available in the Python console is `pmx`.

## General conventions

- `pmx` is the root `PrePoMaxAPI` object.
- API objects implement `help()`, which prints documented members to the console.
- Collection-like objects usually support `len(...)`, indexing with `[...]`, and `in` checks.
- Name collections return `StringList` objects. ID collections return `IntList` objects.
- Result names are strings. In many projects they are full file paths.
- Result steps and increments are indexed by result IDs, not by model step names.
- Field result data is node-based. Only history output can contain integration point values.
- Integration point history entry names use the form `elementId_intPointId`.
- NumPy is optional. Where available, larger numeric arrays are returned as NumPy arrays. Otherwise, compatible .NET arrays or API list wrappers are returned.

## Typical access path

```python
pmx
pmx.results
result = pmx.results['C:\Temp\Analysis-1.frd']
step = result.steps[1]
increment = step.increments[1]
field = increment.fields['DISP']
component = field.components['D1']
data = component.get_data()
```

## Common API base

### `BaseAPI`

Base class for user-facing API objects.

| Member | Description |
| --- | --- |
| `help()` | Prints documented API members for the object. |
| `str(obj)` | Returns a concise object description. |

### `StringList`

Read-only list wrapper for strings.

| Member | Description |
| --- | --- |
| `count` | Number of strings. |
| `len(list)` | Number of strings. |
| `list[index]` | String at zero-based index. |
| `item in list` | Checks whether the string exists. |
| `contains(item)` | Checks whether the string exists. |
| `to_array()` | Returns the underlying string array. |

### `IntList`

Read-only list wrapper for integers.

| Member | Description |
| --- | --- |
| `count` | Number of IDs. |
| `len(list)` | Number of IDs. |
| `list[index]` | ID at zero-based index. |
| `item in list` | Checks whether the ID exists. |
| `contains(item)` | Checks whether the ID exists. |
| `to_array()` | Returns the underlying integer array. |

## Root API

### `PrePoMaxAPI`

Root object exposed as `pmx`.

| Member | Description |
| --- | --- |
| `results` | Loaded PrePoMax results selected explicitly by result name. |
| `help()` | Prints documented members. |

Example:

```python
pmx
pmx.results
pmx.help()
```

## Results collection

### `Results`

Collection of loaded result files.

| Member | Description |
| --- | --- |
| `len(pmx.results)` | Number of loaded results. |
| `pmx.results[result_name]` | Gets a `Result` by result name. |
| `result_name in pmx.results` | Checks whether a result exists. |
| `get_names()` | Returns result names as `StringList`. |
| `contains(result_name)` | Checks whether a result exists. |

Example:

```python
names = pmx.results.get_names()
result = pmx.results[names[0]]
```

### `Result`

Single loaded result file.

| Member | Description |
| --- | --- |
| `name` | Immutable user-visible result name. |
| `file_name` | Source result file name. |
| `date_time` | Result file date and time. |
| `unit_system` | Result unit system. |
| `steps` | Result steps indexed by result step ID. |
| `mesh` | Result mesh. |
| `history` | Result history outputs. |

Example:

```python
result.name
result.steps
result.mesh
result.history
```

## Steps and increments

### `Steps`

Collection of result steps.

| Member | Description |
| --- | --- |
| `len(result.steps)` | Number of result steps. |
| `result.steps[step_id]` | Gets a step directly by result step ID. |
| `step_id in result.steps` | Checks whether the step exists. |
| `get_ids()` | Returns step IDs as `IntList`. |
| `contains(step_id)` | Checks whether the step exists. |

### `Step`

Single result step.

| Member | Description |
| --- | --- |
| `id` | Result step ID. |
| `increments` | Increments belonging to the step. |

### `Increments`

Collection of increments inside a result step.

| Member | Description |
| --- | --- |
| `len(step.increments)` | Number of increments. |
| `step.increments[increment_id]` | Gets an increment directly by increment ID. |
| `increment_id in step.increments` | Checks whether the increment exists. |
| `get_ids()` | Returns increment IDs as `IntList`. |
| `contains(increment_id)` | Checks whether the increment exists. |

### `Increment`

Single result increment.

| Member | Description |
| --- | --- |
| `id` | Increment ID. |
| `step_time` | Step time for this increment. |
| `fields` | Field outputs available for this increment. |


## Field results

Field result data is node-based. The returned IDs are node IDs.

### `Fields`

Collection of field outputs for one increment.

| Member | Description |
| --- | --- |
| `len(increment.fields)` | Number of fields. |
| `increment.fields[field_name]` | Gets a field by name. |
| `field_name in increment.fields` | Checks whether the field exists. |
| `get_names()` | Returns field names as `StringList`. |
| `contains(field_name)` | Checks whether the field exists. |

### `Field`

Single field output, such as displacement, stress, or temperature.

| Member | Description |
| --- | --- |
| `name` | Field name. |
| `components` | Components available for this field. |
| `get_data()` | Returns all components as `FieldMultiComponentData`. |

### `FieldComponents`

Collection of field components.

| Member | Description |
| --- | --- |
| `len(field.components)` | Number of components. |
| `field.components[component_name]` | Gets a component by name. |
| `component_name in field.components` | Checks whether the component exists. |
| `get_names()` | Returns component names as `StringList`. |
| `contains(component_name)` | Checks whether the component exists. |

### `FieldComponent`

Single scalar component of a field result.

| Member | Description |
| --- | --- |
| `name` | Component name. |
| `get_data()` | Returns scalar component data as `FieldData`. |

### `FieldData`

Node-based scalar field component data.

| Member | Description |
| --- | --- |
| `ids` | Node IDs. |
| `values` | Scalar values for the component. |
| `location` | Result location, normally node-based. |
| `unit` | Component unit. |
| `count` | Number of values. |

Example:

```python
disp = increment.fields['DISP']
d1 = disp.components['D1'].get_data()
d1.ids
d1.values
```

### `FieldMultiComponentData`

Node-based data for all components of a field.

| Member | Description |
| --- | --- |
| `ids` | Node IDs. |
| `values` | Values arranged by node and component. |
| `component_names` | Component names as `StringList`. |
| `location` | Result location, normally node-based. |
| `unit` | Field unit. |
| `count` | Number of nodes. |

Example:

```python
data = increment.fields['DISP'].get_data()
data.component_names
data.values
```

## Mesh

### `Mesh`

Mesh associated with a result.

| Member | Description |
| --- | --- |
| `nodes` | Mesh nodes. |
| `elements` | Mesh elements. |
| `parts` | Mesh parts. |

### `Nodes`

Collection of mesh nodes.

| Member | Description |
| --- | --- |
| `count` | Number of nodes. |
| `get_data()` | Returns `NodeData`. |
| `get_ids()` | Returns node IDs. |
| `get_coordinates()` | Returns node coordinates. |

### `NodeData`

Node coordinate data.

| Member | Description |
| --- | --- |
| `ids` | Node IDs. |
| `coordinates` | Node coordinates. |
| `count` | Number of nodes. |

### `Elements`

Collection of mesh elements.

| Member | Description |
| --- | --- |
| `count` | Number of elements. |
| `len(result.mesh.elements)` | Number of elements. |
| `result.mesh.elements[element_id]` | Gets an element by ID. |
| `element_id in result.mesh.elements` | Checks whether the element exists. |
| `get_data()` | Returns `ElementData`. |
| `get_ids()` | Returns element IDs as `IntList`. |
| `contains(element_id)` | Checks whether the element exists. |
| `get_node_ids(element_id)` | Returns node IDs for one element as `IntList`. |

### `Element`

Single mesh element.

| Member | Description |
| --- | --- |
| `id` | Element ID. |
| `type_name` | Element type name. |
| `dimension` | Element dimension. |
| `node_ids` | All node IDs of the element in PrePoMax element order. |
| `node_count` | Number of element nodes. |

Example:

```python
element = result.mesh.elements[10]
element.node_ids
```

### `ElementData`

Bulk element connectivity data.

| Member | Description |
| --- | --- |
| `ids` | Element IDs. |
| `offsets` | Start index of each element connectivity span. |
| `node_counts` | Number of nodes for each element. |
| `connectivity` | Flattened node IDs for all elements. |
| `count` | Number of elements. |
| `get_node_ids(element_id)` | Returns node IDs for one element as `IntList`. |

`offsets` are needed only when reading the flattened `connectivity` array directly.
For element index `i`, its node IDs are stored in:

```python
connectivity[offsets[i]:offsets[i + 1]]
```

For normal use, prefer:

```python
node_ids = result.mesh.elements.get_node_ids(element_id)
# or
node_ids = result.mesh.elements[element_id].node_ids
```

### `Parts`

Collection of mesh parts.

| Member | Description |
| --- | --- |
| `len(result.mesh.parts)` | Number of parts. |
| `result.mesh.parts[part_name]` | Gets a part by name. |
| `part_name in result.mesh.parts` | Checks whether the part exists. |
| `get_names()` | Returns part names as `StringList`. |
| `contains(part_name)` | Checks whether the part exists. |

### `Part`

Single mesh part.

| Member | Description |
| --- | --- |
| `name` | Part name. |
| `node_ids` | Node IDs belonging to the part. |
| `element_ids` | Element IDs belonging to the part. |

## History results

History outputs can contain node, element, or integration point values depending on the output definition.
Integration point entries use names like `elementId_intPointId`.

### `History`

Root object for result history outputs.

| Member | Description |
| --- | --- |
| `sets` | History output sets. |

### `HistorySets`

Collection of history output sets.

| Member | Description |
| --- | --- |
| `len(result.history.sets)` | Number of history sets. |
| `result.history.sets[set_name]` | Gets a history set by name. |
| `set_name in result.history.sets` | Checks whether the set exists. |
| `get_names()` | Returns set names as `StringList`. |
| `contains(set_name)` | Checks whether the set exists. |

### `HistorySet`

Single history output set.

| Member | Description |
| --- | --- |
| `name` | History set name. |
| `fields` | History fields in the set. |

### `HistoryFields`

Collection of history fields.

| Member | Description |
| --- | --- |
| `len(history_set.fields)` | Number of history fields. |
| `history_set.fields[field_name]` | Gets a history field by name. |
| `field_name in history_set.fields` | Checks whether the field exists. |
| `get_names()` | Returns field names as `StringList`. |
| `contains(field_name)` | Checks whether the field exists. |

### `HistoryField`

Single history field.

| Member | Description |
| --- | --- |
| `name` | History field name. |
| `components` | Components in the history field. |

### `HistoryComponents`

Collection of history components.

| Member | Description |
| --- | --- |
| `len(history_field.components)` | Number of components. |
| `history_field.components[component_name]` | Gets a component by name. |
| `component_name in history_field.components` | Checks whether the component exists. |
| `get_names()` | Returns component names as `StringList`. |
| `contains(component_name)` | Checks whether the component exists. |

### `HistoryComponent`

Single history component.

| Member | Description |
| --- | --- |
| `name` | Component name. |
| `entries` | Named history entries for this component. |
| `get_entry_names()` | Returns entry names as `StringList`. |
| `get_data()` | Returns data when entries share a compatible time axis or only one entry exists. |
| `get_data(entry_name)` | Returns data for one named entry. |

### `HistoryEntries`

Collection of named history entries.

| Member | Description |
| --- | --- |
| `len(component.entries)` | Number of entries. |
| `component.entries[entry_name]` | Gets one entry by name. |
| `entry_name in component.entries` | Checks whether the entry exists. |
| `get_names()` | Returns entry names as `StringList`. |
| `contains(entry_name)` | Checks whether the entry exists. |

### `HistoryEntry`

Single named history entry.

| Member | Description |
| --- | --- |
| `name` | Entry name. |
| `get_data()` | Returns data for this entry as `HistoryComponentData`. |

### `HistoryComponentData`

History data returned from a history component or entry.

| Member | Description |
| --- | --- |
| `time` | Time values. |
| `values` | History values. |
| `unit` | Value unit. |
| `count` | Number of time points. |
| `entry_names` | Entry names represented in `values`. |

Example:

```python
set_names = result.history.sets.get_names()
hset = result.history.sets[set_names[0]]
field = hset.fields[hset.fields.get_names()[0]]
component = field.components[field.components.get_names()[0]]
entry_names = component.get_entry_names()
data = component.get_data(entry_names[0])
```

## Internal and infrastructure classes

These classes exist in the implementation but are not normally used directly from the Python console.

| Class | Purpose |
| --- | --- |
| `PythonAPIAttribute` | Marks API members that should appear in generated help and autocomplete. |
| `PythonAPIException` | Exception type used for API errors reported to Python. |
| `PythonEngineManager` | Hosts and manages the embedded Python engine and console execution. |
| `PythonConsoleResult` | Holds execution output, errors, and display text for console commands. |
| API service classes | Resolve PrePoMax result objects and convert internal data into API objects. |


## Source Folder Layout

The C# API source is grouped by object area. The C# source stays grouped by folders, but all Python API classes use the flat `PrePoMax.API.Python` namespace.

```text
PrePoMax/API/Python
  Mesh
    Elements
    Nodes
    Parts
  Results
    Fields
    History
    Steps
  Services
    History
    Mesh
    Results
```

## Notes for documentation generation

- The `[PythonAPI]` attribute descriptions are the best source for generated member documentation.
- `ToString()` implementations provide concise console display text and should remain user-oriented.
- Keep implementation-only methods unmarked by `[PythonAPI]` if they should be hidden from help and autocomplete.
- Prefer documenting collection access with `[...]` because this is the intended Python style.




