// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace PrePoMax.Mcp
{
    // How to drive a PrePoMax session, as the get_guide tool serves it. It is procedure rather than
    // reference: which order the work goes in, how to find the face that was meant, what a number
    // without a unit means, and what to do when a call is refused. The reference is describe_api,
    // which is read off the API itself and cannot drift.
    //
    // It is served from the application rather than written into a client, so that a client of any
    // kind gets the guide of the version it is talking to. Python_API_Overview.md documents the same
    // surface for a reader, and the two are updated together.
    public static class McpAgentGuide
    {
        // Variables                                                                                                                
        public const string Text = @"# Driving a PrePoMax session

You are working in the PrePoMax session the user has open. The model changes in their viewport and
their model tree while you work, and every change you make is recorded as the command they would
have produced by hand. Treat it as their model: read it before you change it.

## The order the work goes in

1. `get_status` - is it idle, which model is open, which unit system, is the server read only.
2. `read_model` - what the model already holds. Cheaper than asking for pieces one at a time.
3. `read_topology` - the faces, edges and vertices of the parts, when you have to select geometry.
4. `describe_api` - the exact call and its arguments. Look it up; do not guess a member name.
5. `run_python` - make the change.
6. `get_history` with `include_journal` - what the user did by hand, written as Python.

## Writing the Python

`pmx` is the root object. A model item is created by `add_<type>` on the collection that owns it:

```python
pmx.model.materials.add('Steel', [pmx.create.elastic(210000, 0.3), pmx.create.density(7.85e-9)])
pmx.model.sections.add_solid('Section-1', 'Steel', part_selection)
step = pmx.model.steps.add_static('Step-1', time_period=1.0)
step.bcs.add_fixed('Fix-1', pmx.create.region('NodeSetName', 'Bottom'))
```

- `pmx.create.*` makes the detached parts that an `add` takes. It records nothing.
- Every `add_*` has a `replace_*` taking `old_name` first. On the model side what you do not pass is
  kept, so a replace changes only what you name.
- Variables survive from one `run_python` call to the next, because the scope is the Python console's.
- `describe_api` also takes an API type name - `Loads`, `Model.Steps` - so you can read what a
  collection offers before the step that would hold it exists, and a type name followed by a member
  path - `Result.mesh`, `Result.steps.last().increments` - so you can read the results side before
  an analysis has run. A name the model and the results sides share is refused with the candidates
  to choose from. A path that ends at a method or a property - `pmx.create.selection_by_ids`,
  `Loads.add_pressure` - describes that one member with its parameters, and a mistyped path is answered
  with the paths it most likely meant.
- A map the API returns is a Python dict: print it, index it, or loop over `.items()`.

## Numbers have units

A plain number is a value in the model's unit system, which `get_status` reports as `unitSystem`. In
`MM_TON_S_C` a length is a millimetre, a force a newton, a stress a megapascal. Nothing converts for
you, and nothing warns you: 210000 is a correct steel modulus in that system and wrong in `M_KG_S_C`.
Read the unit system before you write the first number.

Anywhere the user could type an expression you may pass a string instead of a number, and it is
stored as an equation that re-evaluates - `'=2*width'` follows the parameter `width`. Use
`pmx.model.parameters` for a value the user will want to change.

## Selecting the thing you mean

This is where a scripted model goes wrong most often, so spend a call on it.

- `read_topology` gives a face its area, centroid, normal and the ID a selection takes. The top face
  of a box is the one whose normal is `[0, 0, 1]`; you do not need a picture to find it. It lists the
  largest faces of each part up to a cap, so name the part when a big assembly is only summarized.
- Select it by ID, grouped by part ID:
  `pmx.create.selection_by_ids('femodel', 'surfaces', 'surfaces', {1: [3]})`
- Or by a point, which the application resolves the way a mouse click does:
  `pmx.create.selection_by_pick('femodel', 'nodes', [0.5, 0.5, 1.0], 'Surface')`
- A point pick takes the **nearest** entity, so a mistyped coordinate selects the wrong face rather
  than failing. An area or box pick selects only what lies inside it, and reports nothing when empty.
- Check before you commit: `selection.preview()` resolves it and reports the count, the parts, the
  bounding box and the centroid, and records nothing. A count of zero or a centroid in the wrong
  place is the mistake, caught before it becomes a boundary condition.
- A pick survives a mesh change, because it is resolved again rather than replayed from IDs. Prefer
  it for a script meant to outlive one mesh.

## Running an analysis

```python
pmx.jobs.add('Job-1')
```

Then `run_job`, which starts the solver and returns at once, and poll `get_job`. `OK` means it
finished. Open the results with `pmx.jobs.open_results('Job-1')` and read values through
`pmx.results.current`; result steps and increments are indexed by result IDs, not by model step names.

## Looking at it

`pmx.view` is what the application shows, and none of it is a model change, so none of it is
recorded. Set the view before a screenshot and put it back afterwards:

```python
camera = pmx.view.get_parameters()
pmx.view.current = 'results'
pmx.view.set_result_field('DISP', 'U3')
pmx.view.set_orientation('Isometric')
pmx.view.zoom_to_fit()
```

`screenshot` then writes a PNG of the viewport and returns its path. Use it when a question is about
orientation, a selection or a result plot - no amount of reading the model answers those. Its
`transparent_background` leaves the viewport background out, for a picture that goes onto a page of
its own, and `magnification` renders it up to four times larger than the viewport, for one that is
going to be printed - it draws the model larger, not the legend, so it is not the way to read a
label that is too small.

A report picture is set up the same way. `rotate(azimuth_deg, elevation_deg)` turns a standard view a little,
`look_at(point, parallel_scale)` frames a detail, `deformation_type` and `undeformed_model` control
the deformed plot, and `set_legend_range(min, max)` gives pictures of different increments or models
one color scale - reset it afterwards. `pmx.view.annotations` adds the labels: `add_text(name, text,
anchor_point)`, `add_node(node_id)`, `add_element(element_id)`, and `show_min_max(show_min, show_max)`
for the extreme value labels, whose setting `get_min_max()` reads so it can be put back. Every one of
these that needs an open result says so when there is none.

## Long operations

Meshing a large part, importing a file and regenerating a model can take minutes. A `run_python` or
`eval_expression` call waits for its answer as long as `requestTimeout` in `get_status` says - 30 s
unless the user changed it - or as long as its own `timeout` argument says, up to 600 s. A call that
takes longer is **not stopped and not lost**: it is answered with `running`, a `callId` and how long
it has run, and it goes on to the end and records its commands as usual.

- Collect the answer with `get_call` and that `callId`. It waits again and returns exactly what the
  original call would have - output, error and `recordedCommands` - or `running` once more.
- Do not send the same work again. Every call that changes the model waits for the running one, and
  is refused as busy if it waits longer than the timeout; `get_status` names the call that holds it.
- When you know a call is long, pass a larger `timeout` - or `timeout=0` to be answered at once with
  the `callId` and do the waiting through `get_call`.
- An answer is handed over once. `get_status` lists under `calls` the ones still running or not
  collected yet.

## When a call is refused

- **Busy.** The user has a dialog open or a mesher is running, and nothing was done. Wait and send
  the same call again. Do not work around it. If the reason names an automation call, it is one of
  yours that is still running: collect it with `get_call` first.
- **Read only.** The user has protected the model. Reading still works; ask them before asking for
  more.
- **An API error.** The message names the reason and usually the accepted values. Read it and fix
  the call rather than trying variations.
- **Nothing answers on the address.** PrePoMax is closed, or its automation server is off. Ask the
  user to open it and switch on Tools - Automation Server. Retrying will not help.

## What not to do

- Do not save over the user's file and do not close their application.
- Do not invent a member name. `describe_api` is cheap and the API refuses what it does not know.
- Do not use `eval_expression` to change the model. It reports a command it recorded as a warning,
  which means you asked the wrong tool.
- Do not use `record='script'` for ordinary work. It records the whole source as one command, which
  keeps a computed model parametric but leaves the user one undo for everything you did.

## Undoing your work

Everything you recorded is in **Edit - Commands**, which lists it, removes it and reorders it, and
**Edit - Regenerate** rebuilds the model without it. Say so when you tell the user what you changed.
";
    }
}
