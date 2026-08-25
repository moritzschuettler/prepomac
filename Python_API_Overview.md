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
- Result names are strings. In many projects they are full file paths and are used to select the current result.
- Result data is exposed through `pmx.results.current`. Use `set_current(result_name)` to change it.
- Result steps and increments are indexed by result IDs, not by model step names.
- Field result data is node-based. Only history output can contain integration point values.
- Integration point history entry names use the form `elementId_intPointId`.
- NumPy is optional. Where available, numeric arrays are returned as NumPy arrays. Otherwise, `NumericArray` or typed API list wrappers are returned.
- Methods that construct Python-side drafts or specifications start with `create_` and do not modify PrePoMax.
- Collection `add(...)` methods perform validation and make the command-recorded database change.

## Script execution and regeneration

- **Run script** and **Run selection** execute in the shared interactive Python scope. State-changing API calls are recorded
  as individual PrePoMax commands.
- **Record and Run** stores the complete editor source as one postprocessing command and then executes it. Commands invoked
  by that script still use the normal controller command path, but they are not added to history a second time.
- Recorded scripts run in a fresh scope containing a new `pmx`, `__name__ = "__main__"`, and `__file__ = "<string>"`.
  They cannot depend on variables or imports left in the interactive console by earlier work.
- A recorded script is added to history only when it completes without an error. Regeneration executes the embedded source
  at the same position in the postprocessing history.

## Typical access path

```python
pmx
pmx.results
result_names = pmx.results.get_names()
pmx.results.set_current(result_names[-1])
result = pmx.results.current
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

### `StringDoubleMap`

Read-only ordered mapping from string names to floating-point values.

| Member | Description |
| --- | --- |
| `count` | Number of name-value pairs. |
| `len(mapping)` | Number of name-value pairs. |
| `mapping[name]` | Value for the specified name. |
| `name in mapping` | Checks whether a name exists. |
| `get_names()` | Returns names as `StringList`. |
| `contains(name)` | Checks whether a name exists. |

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
| `results` | Loaded result names and the currently selected result. |
| `selection` | Creates and highlights reusable view-aware selections. |
| `help()` | Prints documented members. |

Example:

```python
pmx
pmx.results
pmx.selection
pmx.help()
```

## Selections

### `SelectionFactory`

`pmx.selection` creates selection drafts that are independent of the live UI selection. The `view` is required and remains stored in the selection so command replay uses the correct mesh.

| Member | Description |
| --- | --- |
| `create_by_ids(view, target, source, ids)` | Creates an ID-based selection. |
| `highlight(selection)` | Switches to the stored view and highlights the selection. |
| `clear_highlight()` | Clears the interactive selection and its highlight. |

Possible `view` values are `"geometry"`, `"femodel"`, and `"results"`.

Possible `target` values are `"nodes"`, `"elements"`, `"surfaces"`, `"edges"`, `"parts"`, and `"geometry"`. Possible `source` values are `"nodes"`, `"elements"`, `"vertices"`, `"edges"`, `"surfaces"`, and `"parts"`.

Nodes, elements, vertices, and parts use a list of IDs. Vertex IDs are the node IDs displayed by Query. Edge and surface IDs are 1-based within each part and use a part-to-ID mapping:

```python
selection = pmx.selection.create_by_ids(
    view="results",
    target="nodes",
    source="edges",
    ids={3: [1, 2], 7: [4]}
)

selection.add_by_ids(source="vertices", ids=[10282])
pmx.selection.highlight(selection)
```

Supported conversions:

| Target | Sources |
| --- | --- |
| `nodes` | `nodes`, `vertices`, `edges`, `surfaces`, `parts` |
| `elements` | `elements`, `surfaces`, `parts` |
| `surfaces` | `surfaces`, `parts` |
| `edges` | `edges` |
| `parts` | `vertices`, `edges`, `surfaces`, `parts` |
| `geometry` | `vertices`, `edges`, `surfaces`, `parts` |

### `Selection`

| Member | Description |
| --- | --- |
| `view` | Immutable selection view. |
| `target` | Immutable resulting item type. |
| `count` | Number of source IDs. |
| `add_by_ids(source, ids)` | Adds another compatible ID source. |
| `get_sources()` | Returns the source types as `StringList`. |
## Results collection

### `Results`

Collection of loaded result files.

| Member | Description |
| --- | --- |
| `len(pmx.results)` | Number of loaded results. |
| `current` | Returns the currently selected `Result`. |
| `history_outputs` | Returns history outputs for the currently selected result. |
| `field_outputs` | Returns field outputs for the currently selected result. |
| `get_names()` | Returns result names as `StringList`. |
| `set_current(result_name)` | Sets the current result through command history and returns it. |
| `contains(result_name)` | Checks whether a result exists. |

Example:

```python
names = pmx.results.get_names()
result = pmx.results.set_current(names[-1])
# Equivalent after selection:
result = pmx.results.current
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
| `field_outputs` | User-defined result field output definitions. |
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
result.field_outputs
node_id = int(result.mesh.nodes.get_ids()[0])
coordinates = result.get_nodal_coordinates(node_id)
deformed_coordinates = result.get_deformed_nodal_coordinates(1, 1, node_id)
value = result.get_nodal_value(1, 1, 'DISP', 'U1', node_id)
```

## Result field output definitions

### `FieldOutputFilter`

Immutable Python filter specification used while creating result field outputs.

| Member | Description |
| --- | --- |
| `type` | `"median"`, `"average"`, or `"smooth"`. |
| `source` | `"all_nodes"` or `"surface_nodes"`. |
| `layers` | Number of neighbouring node layers. |
| `iterations` | Number of filter iterations. |

### `FieldOutputs`

Collection of user-defined result field outputs. These are postprocessing definitions that generate derived fields.

| Member | Description |
| --- | --- |
| `len(result.field_outputs)` | Number of user-defined result field outputs. |
| `result.field_outputs[name]` | Gets a field output definition by name. |
| `name in result.field_outputs` | Checks whether a definition exists. |
| `get_names()` | Returns definition names as `StringList`. |
| `first()` | Returns the first field output definition. |
| `last()` | Returns the last field output definition. |
| `contains(name)` | Checks whether a definition exists. |
| `create_field_output_limit(name, field_name, component_name, based_on, item_limits)` | Creates a part-, element-set-, or all-elements-based limit draft without modifying the result. |
| `create_field_output_envelope(name, field_name, component_name)` | Creates an envelope draft with `MAX`, `MIN`, and `AVERAGE` components. |
| `create_field_output_equation(name, equation, unit="/")` | Creates an equation draft. References use case-sensitive `FIELD.COMPONENT` names. |
| `create_field_output_coordinate_system_transform(name, field_name, coordinate_system_name)` | Creates a coordinate-system transform draft for a vector or tensor field. |
| `create_median_filter(source="all_nodes", layers=1, iterations=1)` | Creates a median filter. `source` can be `"all_nodes"` or `"surface_nodes"`. |
| `create_average_filter(source="all_nodes", layers=1, iterations=1)` | Creates an average filter. `source` can be `"all_nodes"` or `"surface_nodes"`. |
| `create_smooth_filter(source="all_nodes", layers=1, iterations=1)` | Creates a smooth filter. `source` can be `"all_nodes"` or `"surface_nodes"`. |
| `add(output)` | Validates and adds a `FieldOutputDraft` using command history. |
| `replace(old_name, output)` | Validates and replaces a field-output definition using command history. |
| `remove(field_output_names)` | Removes field outputs using command history. The argument is a non-empty list of names. |
| `remove_components(field_output_name, component_names)` | Removes components from a field output using command history. |
| `export_to_csv(file_name, field_output_name, step_id, increment_id)` | Exports all field components at one result increment using command history. |

`based_on` is required and must be `"parts"`, `"element_sets"`, or `"all_elements"`. `item_limits` must be a non-empty Python dictionary with finite, nonzero values. Its keys are part names, element-set names, or the single key `"All elements"` when `based_on="all_elements"`.

Creation only produces a Python-side draft and does not call the controller or modify command history. Assign optional filters to the draft, then call `add(output)`. The complete draft is converted and validated before the postprocessing command is created. If the current result changes after a `Result` object was obtained, get `pmx.results.current` again before adding the draft.

Filter creation is available directly on `pmx.results.field_outputs` and `result.field_outputs`. The filter `source` must be `"all_nodes"` or `"surface_nodes"`; `layers` and `iterations` must both be at least 1.

One limit for all elements:

```python
result = pmx.results.current

output = result.field_outputs.create_field_output_limit(
    name="Stress_limit",
    field_name="STRESS",
    component_name="MISES",
    based_on="all_elements",
    item_limits={"All elements": 235.0}
)

limit_output = result.field_outputs.add(output)
```

One limit with two filters:

```python
output = result.field_outputs.create_field_output_limit(
    name="Filtered_stress_limit",
    field_name="STRESS",
    component_name="MISES",
    based_on="all_elements",
    item_limits={"All elements": 235.0}
)

output.filter1 = pmx.results.field_outputs.create_median_filter(
    source="surface_nodes",
    layers=2
)
output.filter2 = pmx.results.field_outputs.create_smooth_filter(
    iterations=3
)

filtered_output = result.field_outputs.add(output)
```

Different limits by part:

```python
output = result.field_outputs.create_field_output_limit(
    name="Stress_limits_by_part",
    field_name="STRESS",
    component_name="MISES",
    based_on="parts",
    item_limits={
        "Part-1": 235.0,
        "Part-2": 180.0
    }
)

limit_output = result.field_outputs.add(output)
```

Envelope across all result increments:

```python
output = result.field_outputs.create_field_output_envelope(
    name="Stress_envelope",
    field_name="STRESS",
    component_name="MISES"
)
result.field_outputs.add(output)
```

Equation with the generated component `VALUE`:

```python
output = result.field_outputs.create_field_output_equation(
    name="Normalized_stress",
    equation="=STRESS.MISES / 235.0",
    unit="/"
)
result.field_outputs.add(output)
```

Coordinate-system transform:

```python
output = result.field_outputs.create_field_output_coordinate_system_transform(
    name="Local_stress",
    field_name="STRESS",
    coordinate_system_name="Local_CS"
)
result.field_outputs.add(output)
```

Replace an existing definition:

```python
replacement = result.field_outputs.create_field_output_limit(
    name="Stress_limit",
    field_name="STRESS",
    component_name="MISES",
    based_on="all_elements",
    item_limits={"All elements": 250.0}
)

result.field_outputs.replace("Stress_limit", replacement)
```

Remove definitions or stored fields and components:

```python
result.field_outputs.remove(["Stress_limit", "Old_field"])
result.field_outputs.remove_components("DISP", ["U3"])
```

Export every component of a field at one increment to a file:

```python
result.field_outputs.export_to_csv(
    file_name=r"C:\Temp\Stress_limit.csv",
    field_output_name="Stress_limit",
    step_id=1,
    increment_id=1
)
```

### `FieldOutputDraft`

Python-side field-output definition that has not yet been added to the result.

| Member | Description |
| --- | --- |
| `name` | Field output name. |
| `filter1` | Optional first `FieldOutputFilter`; writable before `add(output)`. |
| `filter2` | Optional second `FieldOutputFilter`; writable before `add(output)`. |

`filter2` cannot be used unless `filter1` is also assigned.

### `FieldOutputLimitDraft`

Limit field-output draft returned by `create_field_output_limit`.

| Member | Description |
| --- | --- |
| `name` | Field output name. |
| `field_name` | Source field name. |
| `component_name` | Source component name. |
| `based_on` | `"parts"`, `"element_sets"`, or `"all_elements"`. |
| `item_limits` | Draft limit values as a read-only `StringDoubleMap`. |

### `FieldOutputEnvelopeDraft`

Envelope draft returned by `create_field_output_envelope`. It produces `MAX`, `MIN`, and `AVERAGE` components.

| Member | Description |
| --- | --- |
| `name` | Field output name. |
| `field_name` | Source field name. |
| `component_name` | Source component name. |

### `FieldOutputEquationDraft`

Equation draft returned by `create_field_output_equation`.

| Member | Description |
| --- | --- |
| `name` | Field output name. |
| `equation` | Equation using case-sensitive `FIELD.COMPONENT` references. |
| `unit` | User-defined result unit. |
| `component_name` | Generated component name, always `VALUE`. |

### `FieldOutputCoordinateSystemTransformDraft`

Coordinate-system transform draft returned by `create_field_output_coordinate_system_transform`.

| Member | Description |
| --- | --- |
| `name` | Field output name. |
| `field_name` | Source vector or tensor field name. |
| `coordinate_system_name` | Result coordinate system name. |

### `FieldOutput`

Base representation of a user-defined result field output.

| Member | Description |
| --- | --- |
| `name` | Field output name. |
| `type_name` | C# field output definition type. |
| `filter1` | First `FieldOutputFilter`, or `None` when disabled. |
| `filter2` | Second `FieldOutputFilter`, or `None` when disabled. |

### `FieldOutputLimit`

Limit-based field output returned by `add(output)` or collection lookup.

| Member | Description |
| --- | --- |
| `name` | Field output name. |
| `field_name` | Source field name. |
| `component_name` | Source component name. |
| `based_on` | `"parts"`, `"element_sets"`, or `"all_elements"`. |
| `item_limits` | Limit values as a `StringDoubleMap`. |

### `FieldOutputEnvelope`

| Member | Description |
| --- | --- |
| `field_name` | Source field name. |
| `component_name` | Source component name. |

### `FieldOutputEquation`

| Member | Description |
| --- | --- |
| `equation` | Validated field equation. |
| `unit` | User-defined result unit. |
| `component_name` | Generated component name, always `VALUE`. |

### `FieldOutputCoordinateSystemTransform`

| Member | Description |
| --- | --- |
| `field_name` | Source vector or tensor field name. |
| `coordinate_system_name` | Result coordinate system name. |

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
| `node_sets` | User-visible, read-only result node sets. |
| `element_sets` | User-visible, read-only result element sets. |
| `surfaces` | User-visible, read-only result surfaces. |

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

### Read-only result regions

Node sets, element sets, and surfaces are copied from the model into the result mesh. They can be inspected and
used by result history outputs, but they cannot be created, changed, or removed through the results API.

The `node_sets`, `element_sets`, and `surfaces` collections support indexing by name, `len()`, `get_names()`,
`first()`, `last()`, and `contains(name)`.

| Object | Members |
| --- | --- |
| `NodeSet` | `name`, `count`, `node_ids` |
| `ElementSet` | `name`, `count`, `element_ids` |
| `Surface` | `name`, `face_ids`, `node_ids`, `area` |

Example:

```python
node_set = result.mesh.node_sets["Node_Set-1"]
element_set = result.mesh.element_sets["Element_Set-1"]
surface = result.mesh.surfaces["Surface-1"]
```

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
| `create_history_output_from_field(name, field_name, component_names, region)` | Creates a From Field draft using a node selection, node set, or surface. |
| `create_history_output_from_equation(name, equation, unit="/")` | Creates a From History Output by Equation draft. |
| `create_history_output_from_integration(name, integral_type, field_name=None, component_names=None, region=None, integral_result="geometry", deformation_variable_name="Displacements", unit="/", cross_section="none", thickness=1.0)` | Creates a line, surface, or volume integration draft. `integral_result` is `"geometry"` (region size), `"field"` (field integral), or `"field_by_geometry"` (region average of the field); `field_name`/`component_names` are ignored for `"geometry"`. `cross_section` (`"none"`, `"thickness"`, or `"axisymmetric"`) applies the out-of-plane scaling for 2D output on line and volume integrals. |
| `create_history_output_from_merged_histories(name, source1, source2, source3=None, source4=None)` | Creates a From Merged Histories draft that merges two to four history components into one history output. |
| `create_history_output_from_extracted_entry(name, source, entry_name)` | Creates a From Extracted Entry draft that copies one named entry of a history component into its own history output. |
| `create_history_output_xy(name, x_source, y_source)` | Creates an XY draft that plots the entries of the Y history component against the values of the X history component. |
| `create_history_output_from_table(name, data_points, y_column_names=None, x_axis_unit="/", y_axis_unit="/")` | Creates a From Table draft from user-defined data. Every row holds the X value followed by one value per Y column. |
| `create_minimum_filter(return_value="column")` | Creates a minimum filter; `return_value` is `"row"` or `"column"`. |
| `create_maximum_filter(return_value="column")` | Creates a maximum filter; `return_value` is `"row"` or `"column"`. |
| `create_sum_filter(operate_on="columns")` | Creates a sum filter; `operate_on` is `"rows"` or `"columns"`. |
| `create_average_filter(operate_on="columns")` | Creates an average filter; `operate_on` is `"rows"` or `"columns"`. |
| `add(output)` | Validates and adds a draft using command history. |
| `replace(old_name, output)` | Validates and replaces a result history-output definition. |
| `duplicate(history_output_names)` | Duplicates user-defined history outputs using command history. Solver-generated history outputs cannot be duplicated. |
| `remove(history_output_names)` | Removes history outputs using command history. |
| `remove_fields(history_output_name, field_names)` | Removes fields from a history output using command history. |
| `remove_components(history_output_name, field_name, component_names)` | Removes components from a history-output field using command history. |
| `export_to_csv(file_name, history_output_names, delimiter=",")` | Exports all fields and components of the selected history outputs to CSV using command history. |


Creation examples:

```python
result = pmx.results.current

nodes = pmx.selection.create_by_ids(
    view="results",
    target="nodes",
    source="edges",
    ids={3: [1, 2]}
)

output = result.history_outputs.create_history_output_from_field(
    name="Edge_DISP",
    field_name="DISP",
    component_names=["U1", "U2", "U3"],
    region=nodes
)
output.step_id = None
output.increment_id = None
output.filter1 = result.history_outputs.create_average_filter(operate_on="rows")
output.filter2 = result.history_outputs.create_maximum_filter(return_value="row")
result.history_outputs.add(output)
```

Existing result regions are passed directly through the same `region` argument:

```python
output = result.history_outputs.create_history_output_from_field(
    name="Node_set_DISP",
    field_name="DISP",
    component_names=["U1"],
    region=result.mesh.node_sets["Node_Set-1"]
)
result.history_outputs.add(output)
```

```python
output = result.history_outputs.create_history_output_from_equation(
    name="Energy_sum",
    equation="Energy_1 + Energy_2",
    unit="J"
)
result.history_outputs.add(output)
```

Line integration integrates a field over geometry edges (works for both shell and solid parts).
For 2D output (plane stress/strain or axisymmetric) set `cross_section` to apply the out-of-plane
scaling; `"thickness"` additionally uses the `thickness` argument, while `"axisymmetric"` scales by
`2*pi*r` (the radial coordinate is the X coordinate):

```python
edges = pmx.selection.create_by_ids(
    view="results",
    target="edges",
    source="edges",
    ids={1: [3, 4]}
)

output = result.history_outputs.create_history_output_from_integration(
    name="Line_Force",
    integral_type="line",
    field_name="STRESS",
    component_names=["S11"],
    region=edges,
    cross_section="thickness",
    thickness=0.01,
    unit="N"
)
result.history_outputs.add(output)
```

`integral_result` selects what the integral evaluates: `"geometry"` (the region size, integrating a unit
field; the default), `"field"` (the field integral), or `"field_by_geometry"` (the region average of the
field). `"geometry"` and `"field"` report one entry per element, so a Sum filter gives the total size
and the region total; `"field_by_geometry"` instead sums the element integrals and the element measures
over the whole region and divides the sums, so it returns a single `REGION` entry that is already the
measure-weighted average (no Sum filter needed). `field_name` and `component_names` are not needed for
`"geometry"`. The geometry result is reported in a single `SIZE` component; note that the cross section
scaling changes its dimension, so a scaled line integral returns an area and a scaled surface integral a
volume:

```python
output = result.history_outputs.create_history_output_from_integration(
    name="Surface_area",
    integral_type="surface",
    region=faces,
    integral_result="geometry",
    unit="mm2"
)
result.history_outputs.add(output)
```

`create_history_output_from_merged_histories`, `create_history_output_from_extracted_entry`, and
`create_history_output_xy` build a new history output from history components that already exist in the result.
Each source argument is either a `HistoryComponent` object or a list of three strings
`[history_output_name, field_name, component_name]`:

```python
history_output = result.history_outputs["Node_DISP"]
source = history_output.fields["DISP"].components["U1"]
same_source = ["Node_DISP", "DISP", "U1"]
```

Merging collects the entries of two to four history components into a single history output. All merged entries
must use the same unit and must share a common reference time; entries of the later sources are interpolated to
the times of the first source. Each source may be used only once:

```python
output = result.history_outputs.create_history_output_from_merged_histories(
    name="Merged_reactions",
    source1=["Node_set_1_FORC", "FORC", "F1"],
    source2=["Node_set_2_FORC", "FORC", "F1"]
)
result.history_outputs.add(output)
```

Extraction copies a single named entry of a history component into its own history output. The entry must exist
in the source component and must contain finite data:

```python
component = result.history_outputs["Node_DISP"].fields["DISP"].components["U1"]
output = result.history_outputs.create_history_output_from_extracted_entry(
    name="Node_5_U1",
    source=component,
    entry_name=str(component.get_entry_names()[0])
)
result.history_outputs.add(output)
```

An XY output replaces the time axis with the values of another history component. The X component must contain
exactly one entry, all entries of the Y component must use the same unit, and both sources must share a common
reference time:

```python
output = result.history_outputs.create_history_output_xy(
    name="Force_vs_displacement",
    x_source=["Node_DISP", "DISP", "U1"],
    y_source=["Node_FORC", "FORC", "F1"]
)
result.history_outputs.add(output)
```

A table output needs no result data at all. The number of Y columns is taken from the row length, all rows must
contain the same number of values, every value must be finite, and the Y-column names must be unique. When
`y_column_names` is omitted, the columns are named `Y1`, `Y2`, ...:

```python
output = result.history_outputs.create_history_output_from_table(
    name="Measured_curve",
    data_points=[[0.0, 0.0, 0.0], [1.0, 12.5, 11.0], [2.0, 24.0, 23.5]],
    y_column_names=["Test_1", "Test_2"],
    x_axis_unit="mm",
    y_axis_unit="N"
)
result.history_outputs.add(output)
```

All four output types produce a single `Table.Values` field component whose entries are the Y columns, and they
accept `filter1`/`filter2` like every other history-output draft.

The supported region combinations are:

| History output | Mode | Selection target | Existing result region |
| --- | --- | --- | --- |
| From Field | All | `"nodes"` | Node set or surface |
| From Integration | `"line"` | `"edges"` | None (selection only) |
| From Integration | `"surface"` | `"surfaces"` | Surface |
| From Integration | `"volume"` | `"elements"` | Element set |
| From Equation | All | None | None |
| From Merged Histories | All | None | None |
| From Extracted Entry | All | None | None |
| XY | All | None | None |
| From Table | All | None | None |

Possible deformation variable names are `"Displacements"`, `"Forces"`, `"Surface Normals"`, `"Wear Depths"`, `"Mesh Deformation"`, `"Disp&Def"`, and `"Disp&Def&Depth"`.

### `HistoryOutputFilter`

Immutable Python filter specification assigned to `filter1` or `filter2` on any history-output draft.
Filters are applied in order, and `filter2` cannot be used without `filter1`.

| Member | Description |
| --- | --- |
| `type` | `"minimum"`, `"maximum"`, `"sum"`, or `"average"`. |
| `option` | `"row"`, `"column"`, `"rows"`, or `"columns"`, depending on the filter type. |

### `HistoryOutputDraft`

Base object for history-output definitions that have not yet been added to the result.

| Member | Description |
| --- | --- |
| `name` | History output name. |
| `region` | Integrated selection or read-only result mesh region; `None` for the equation, merged, extracted, XY, and table drafts. |
| `filter1` | Optional first `HistoryOutputFilter`. |
| `filter2` | Optional second `HistoryOutputFilter`; requires `filter1`. |

### `HistoryOutputFromMergedHistoriesDraft`

From Merged Histories draft. Adds to `HistoryOutputDraft`:

| Member | Description |
| --- | --- |
| `sources` | Merged source components as `"history_output_name.field_name.component_name"` strings. |

### `HistoryOutputFromExtractedEntryDraft`

From Extracted Entry draft. Adds to `HistoryOutputDraft`:

| Member | Description |
| --- | --- |
| `source` | Source component as `"history_output_name.field_name.component_name"`. |
| `entry_name` | Name of the extracted history entry. |

### `HistoryOutputXYDraft`

XY draft. Adds to `HistoryOutputDraft`:

| Member | Description |
| --- | --- |
| `x_source` | X component as `"history_output_name.field_name.component_name"`. |
| `y_source` | Y component as `"history_output_name.field_name.component_name"`. |

### `HistoryOutputFromTableDraft`

From Table draft. Adds to `HistoryOutputDraft`:

| Member | Description |
| --- | --- |
| `data_points` | Table rows, each holding the X value followed by one value per Y column. |
| `y_column_names` | Y-column names. |
| `number_of_y_columns` | Number of Y columns. |
| `x_axis_unit` | User-defined X-axis unit. |
| `y_axis_unit` | User-defined unit shared by all Y columns. |

### `HistoryOutput`

Base representation of a history output. The `history_outputs` collection holds both the history outputs produced
by the solver and those defined by the user, so `is_user_defined` tells the two apart. Looking a user-defined
output up by name returns the matching type-specific object below; solver-generated outputs and From Affine Fit
outputs return this base object.

| Member | Description |
| --- | --- |
| `name` | History output name. |
| `fields` | History fields in the output. |
| `is_user_defined` | `True` when the output was defined by the user instead of produced by the solver. |
| `type_name` | C# history output definition type, or `None` for a solver-generated output. |
| `filter1` | First `HistoryOutputFilter`, or `None` when disabled or the output is solver-generated. |
| `filter2` | Second `HistoryOutputFilter`, or `None` when disabled or the output is solver-generated. |

Reading a definition back:

```python
history_output = result.history_outputs["Edge_DISP"]
if history_output.is_user_defined and history_output.type_name == "ResultHistoryOutputFromField":
    print(history_output.field_name, list(history_output.component_names), history_output.region_type)
```

### `HistoryOutputFromField`

| Member | Description |
| --- | --- |
| `field_name` | Source field name. |
| `component_names` | Selected field component names. |
| `region_type` | `"selection"`, `"node_set"`, or `"surface"`. |
| `region_name` | Region name, or `None` when the region is a selection. |
| `complex_result_type` | Complex result representation. |
| `complex_angle_deg` | Complex result angle in degrees. |
| `step_id` | Result step ID, or `None` when all steps are used. |
| `increment_id` | Result increment ID, or `None` when all increments are used. |
| `harmonic` | Whether harmonic oscillations are included. |
| `output_node_coordinates` | `"off"`, `"undeformed"`, or `"deformed"`. |

### `HistoryOutputFromEquation`

| Member | Description |
| --- | --- |
| `equation` | Validated history-output equation. |
| `unit` | User-defined result unit. |
| `component_name` | Generated component name, always `VALUE`. |

### `HistoryOutputFromIntegration`

| Member | Description |
| --- | --- |
| `integral_type` | `"line"`, `"surface"`, or `"volume"`. |
| `integral_result` | `"geometry"`, `"field"`, or `"field_by_geometry"`. |
| `field_name` | Source field name. |
| `component_names` | Selected field component names. |
| `region_type` | `"selection"`, `"element_set"`, or `"surface"`. |
| `region_name` | Region name, or `None` when the region is a selection. |
| `deformation_variable_name` | Deformation variable name. |
| `unit` | User-defined result unit. |
| `cross_section` | `"none"`, `"thickness"`, or `"axisymmetric"`. |
| `thickness` | Out-of-plane thickness used when `cross_section` is `"thickness"`. |

### `HistoryOutputFromMergedHistories`

| Member | Description |
| --- | --- |
| `sources` | Merged source components as `"history_output_name.field_name.component_name"` strings. |

### `HistoryOutputFromExtractedEntry`

| Member | Description |
| --- | --- |
| `source` | Source component as `"history_output_name.field_name.component_name"`. |
| `entry_name` | Name of the extracted history entry. |

### `HistoryOutputXY`

| Member | Description |
| --- | --- |
| `x_source` | X component as `"history_output_name.field_name.component_name"`. |
| `y_source` | Y component as `"history_output_name.field_name.component_name"`. |

### `HistoryOutputFromTable`

| Member | Description |
| --- | --- |
| `data_points` | Table rows, each holding the X value followed by one value per Y column. |
| `y_column_names` | Y-column names. |
| `number_of_y_columns` | Number of Y columns. |
| `x_axis_unit` | User-defined X-axis unit. |
| `y_axis_unit` | User-defined unit shared by all Y columns. |

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

The Python API resides in the `APIs` class-library project. Its source is grouped by object area under the `Python` folder, with helper and data-service classes stored beside the APIs they support. All Python API classes use the flat `APIs.Python` namespace. PrePoMax keeps only the controller and embedded-engine host adapters.

C# classes and files use explicit names such as `ResultStepAPI`, `ResultIncrementAPI`, `ResultHistoryOutputAPI`, and `ResultHistoryOutputFieldAPI`. Python-visible names remain `Step`, `Increment`, `HistoryOutput`, and `HistoryField`.

```text
APIs
  Python
    Commands
    Common
    Mesh
      Elements
      Nodes
      Parts
    Selection
    Results
      Fields
      ResultFieldOutputs
      ResultHistoryOutputs

PrePoMax/Python
  Commands
  Engine
```

## Notes for documentation generation

- The `[PythonAPI]` attribute descriptions are the best source for generated member documentation.
- `ToString()` implementations provide concise expression output; `print(...)` uses the full display text for arrays and named collections.
- Keep implementation-only methods unmarked by `[PythonAPI]` if they should be hidden from help and autocomplete.
- Prefer documenting collection access with `[...]` because this is the intended Python style.
