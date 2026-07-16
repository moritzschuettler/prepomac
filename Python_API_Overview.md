# PrePoMax Python API Overview

This document is a simple overview of the Python API objects exposed by PrePoMax.
The root object available in the Python console is `pmx`.

## General conventions

- `pmx` is the root `PrePoMaxAPI` object.
- API objects implement `help()`, which prints documented members to the console.
- Collection-like objects usually support `len(...)`, indexing with `[...]`, and `in` checks.
- Ordered object collections provide `first()` and `last()` convenience methods.
- Name collections return `StringList` objects. ID collections return `IntList` objects.
- Long API arrays and named collections are shortened when evaluated directly. Use `print(array)` to display every item.
- Result names are strings. In many projects they are full file paths.
- Result steps and increments are indexed by result IDs, not by model step names.
- Field result data is node-based. Only history output can contain integration point values.
- Integration point history entry names use the form `elementId_intPointId`.
- NumPy is optional. Where available, numeric arrays are returned as NumPy arrays. Otherwise, `NumericArray` or typed API list wrappers are returned.

## Typical access path

```python
pmx
pmx.results
result = pmx.results.first()
step = result.steps[1]
increment = step.increments[1]
field = increment.fields['DISP']
component = field.components['U1']
data = component.get_data()
node_id = int(data.ids[0])
value = component.get_value(node_id)
```

## Common API base

### `BaseAPI`

Base class for user-facing API objects.

| Member | Description |
| --- | --- |
| `help()` | Prints documented API members for the object. |
| `repr(obj)` | Returns a concise object or array description. |
| `str(obj)` / `print(obj)` | Returns the complete array or named-collection contents when applicable. |

### `StringList`

Read-only list wrapper for strings.

| Member | Description |
| --- | --- |
| `count` | Number of strings. |
| `len(list)` | Number of strings. |
| `list[index]` | String at zero-based index. |
| `item in list` | Checks whether the string exists. |
| `contains(item)` | Checks whether the string exists. |
| `to_array()` | Returns a copied CLR string array. |

### `IntList`

Read-only list wrapper for integers.

| Member | Description |
| --- | --- |
| `count` | Number of IDs. |
| `len(list)` | Number of IDs. |
| `list[index]` | ID at zero-based index. |
| `item in list` | Checks whether the ID exists. |
| `contains(item)` | Checks whether the ID exists. |
| `to_array()` | Returns a copied CLR integer array. |

### `NumericArray`

Read-only fallback for numeric arrays when NumPy is unavailable.

| Member | Description |
| --- | --- |
| `shape` | Array dimensions as an `IntList`. |
| `count` | Total number of values. |
| `len(array)` | Length of the first dimension. |
| `array[index]` | Value or row at the specified index. |
| `to_array()` | Returns a copied CLR array. |

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
| `first()` | Returns the first loaded result. |
| `last()` | Returns the last loaded result. |
| `contains(result_name)` | Checks whether a result exists. |

Example:

```python
first_result = pmx.results.first()
latest_result = pmx.results.last()
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
| `history_outputs` | Result history outputs indexed by history output name. |
| `get_nodal_value(step_id, increment_id, field_name, component_name, node_id)` | Returns one nodal field component value. |
| `get_nodal_coordinates(node_id)` | Returns the undeformed node coordinates as `[x, y, z]`. |
| `get_deformed_nodal_coordinates(step_id, increment_id, node_id)` | Returns true-scale deformed coordinates using `DISP`. |
| `get_history_output_entries(history_name, field_name, component_name)` | Returns the entries in a history output component. |
| `get_history_output_entry(history_name, field_name, component_name, entry_name)` | Returns one named history output entry. |

Example:

```python
result.name
result.steps
result.mesh
result.history_outputs
node_id = int(result.mesh.nodes.get_ids()[0])
coordinates = result.get_nodal_coordinates(node_id)
deformed_coordinates = result.get_deformed_nodal_coordinates(1, 1, node_id)
value = result.get_nodal_value(1, 1, 'DISP', 'U1', node_id)
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
| `first()` | Returns the first result step. |
| `last()` | Returns the last result step. |
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
| `first()` | Returns the first increment in the step. |
| `last()` | Returns the last increment in the step. |
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
| `first()` | Returns the first field in the increment. |
| `last()` | Returns the last field in the increment. |
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
| `first()` | Returns the first field component. |
| `last()` | Returns the last field component. |
| `contains(component_name)` | Checks whether the component exists. |

### `FieldComponent`

Single scalar component of a field result.

| Member | Description |
| --- | --- |
| `name` | Component name. |
| `get_data()` | Returns scalar component data as `FieldData`. |
| `get_value(node_id)` | Returns this component value for one node ID. |

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
u1 = disp.components['U1'].get_data()
u1.ids
u1.values
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
| `get_coordinates()` | Returns undeformed node coordinates. |

### `NodeData`

Undeformed node coordinate data.

| Member | Description |
| --- | --- |
| `ids` | Node IDs. |
| `coordinates` | Undeformed node coordinates. |
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
| `first()` | Returns the first mesh element. |
| `last()` | Returns the last mesh element. |
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
| `first()` | Returns the first mesh part. |
| `last()` | Returns the last mesh part. |
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

### `HistoryOutputs`

Collection of result history outputs.

| Member | Description |
| --- | --- |
| `len(result.history_outputs)` | Number of history outputs. |
| `result.history_outputs[history_name]` | Gets a history output by name. |
| `history_name in result.history_outputs` | Checks whether the history output exists. |
| `get_names()` | Returns history output names as `StringList`. |
| `first()` | Returns the first history output. |
| `last()` | Returns the last history output. |
| `contains(history_name)` | Checks whether the history output exists. |

### `HistoryOutput`

Single history output.

| Member | Description |
| --- | --- |
| `name` | History output name. |
| `fields` | History fields in the output. |

### `HistoryFields`

Collection of history fields.

| Member | Description |
| --- | --- |
| `len(history_output.fields)` | Number of history fields. |
| `history_output.fields[field_name]` | Gets a history field by name. |
| `field_name in history_output.fields` | Checks whether the field exists. |
| `get_names()` | Returns field names as `StringList`. |
| `first()` | Returns the first history field. |
| `last()` | Returns the last history field. |
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
| `first()` | Returns the first history component. |
| `last()` | Returns the last history component. |
| `contains(component_name)` | Checks whether the component exists. |

### `HistoryComponent`

Single history component.

| Member | Description |
| --- | --- |
| `name` | Component name. |
| `entries` | Named history entries for this component. |
| `get_entry_names()` | Returns entry names as `StringList`. |
| `get_data()` | Returns data when entries share a compatible time axis or only one entry exists. |

### `HistoryEntries`

Collection of named history entries.

| Member | Description |
| --- | --- |
| `len(component.entries)` | Number of entries. |
| `component.entries[entry_name]` | Gets one entry by name. |
| `entry_name in component.entries` | Checks whether the entry exists. |
| `get_names()` | Returns entry names as `StringList`. |
| `first()` | Returns the first history entry. |
| `last()` | Returns the last history entry. |
| `contains(entry_name)` | Checks whether the entry exists. |

### `HistoryEntry`

Single named history entry.

| Member | Description |
| --- | --- |
| `name` | Entry name. |
| `get_data()` | Returns data for this entry as `HistoryEntryData`. |

### `HistoryComponentData`

History data returned from a complete history component.

| Member | Description |
| --- | --- |
| `time` | Time values. |
| `values` | History values. |
| `unit` | Value unit. |
| `count` | Number of time points. |
| `entry_names` | Entry names represented in `values`. |

### `HistoryEntryData`

History data for one selected entry.

| Member | Description |
| --- | --- |
| `entry_name` | Selected entry name as a string. |
| `time` | Time values for this entry. |
| `values` | History values for this entry. |
| `unit` | Value unit. |
| `count` | Number of time points. |

Example:

```python
history_names = result.history_outputs.get_names()
history_output = result.history_outputs[history_names[0]]
field = history_output.fields[history_output.fields.get_names()[0]]
component = field.components[field.components.get_names()[0]]
entry_names = component.get_entry_names()
entry = component.entries[entry_names[0]]
data = entry.get_data()

# Equivalent convenience access
entries = result.get_history_output_entries(history_names[0], field.name, component.name)
entry = result.get_history_output_entry(history_names[0], field.name, component.name, entry_names[0])
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
- `ToString()` implementations provide concise expression output; `print(...)` uses the full display text for arrays and named collections.
- Keep implementation-only methods unmarked by `[PythonAPI]` if they should be hidden from help and autocomplete.
- Prefer documenting collection access with `[...]` because this is the intended Python style.




