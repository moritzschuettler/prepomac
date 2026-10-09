---
name: prepomax
description: Drive a live PrePoMax finite element session over its MCP endpoint - build or change a model, mesh it, run a CalculiX analysis, read results back, and picture the viewport. Use whenever the prepomax MCP tools are available and the task is about a PrePoMax model, a CalculiX analysis, or the PrePoMax Python API.
---

# Driving PrePoMax

The `prepomax` MCP tools act on **the session the user has open on their own desktop**. The model
changes in their viewport while you work, and everything you change is recorded as the command they
would have produced by hand.

## Read the guide first

**Call `get_guide` before your first change of a session.** It is short, it is served by the
application so it matches the version you are talking to, and it covers the order the work goes in,
what a number without a unit means, how to find the face you meant, and what to do when a call is
refused. Do not reconstruct that from memory, and do not rely on this file for it - this file only
names the habits.

## The habits that matter

- **Read before you write.** `get_status`, then `read_model`. `read_topology` when you have to
  select geometry. Then `describe_api` for the exact call.
- **Look the call up.** `describe_api` is read off the API itself, reports the accepted values and
  the defaults of every argument, and also takes an API type name such as `Loads` when the
  collection does not exist yet, or a type name followed by a member path such as `Result.mesh`
  before an analysis has run. Guessing a member name wastes a call; the API refuses what it does
  not know.
- **Check a selection before you use it.** `selection.preview()` resolves it and reports the count,
  the parts, the bounding box and the centroid, and records nothing. A point pick takes the nearest
  entity, so a mistyped coordinate silently selects the wrong face.
- **Read the unit system.** `get_status` reports it as `model.unitSystem`. A plain number is a value
  in that system and nothing converts it.
- **A screenshot answers what reading cannot.** Orientation, a selection, a result plot. The
  `screenshot` tool can set the view up first.
- **A report picture is `pmx.view` work.** `rotate` and `look_at` frame it, `deformation_type`,
  `undeformed_model` and `set_legend_range` control the result plot, and `pmx.view.annotations`
  adds the labels, including the min/max labels through `show_min_max`. None of it is recorded, so
  put the camera, the legend range and the label setting back afterwards.

## When something is refused

- **Busy** means the user has a dialog open or a mesher is running, and nothing was done. Wait and
  send the same call again rather than working around it. If it names an automation call, that is
  one of yours still running - collect it first.
- **`running` with a `callId`** is not a failure, whichever tool answered it - `read_model`,
  `read_topology`, `run_job` and `get_job` run Python too. The call outlasted its wait and goes on to
  the end, recording its commands as usual; collect its answer with `get_call`, which returns what
  that tool would have returned, and do not send the work again.
  Give a call you know is long - meshing a big part, an import, a regeneration - a larger `timeout`,
  but keep it below your client's own request timeout; for long work pass `timeout` 0 and wait with
  `get_call`. An answer your client gave up on is kept, and `get_status` lists it under `calls`.
  A call that should not finish - a loop that never ends - is stopped with `cancel_call` and its
  `callId`: it stops at its next Python statement, but a PrePoMax operation it is inside, such as
  meshing, finishes first. What it recorded before that stays in the history.
- **Nothing answers on the address** means PrePoMax is closed or its automation server is off. Ask
  the user to open it and switch on **Tools - Automation Server**. Retrying will not help, and there
  is nothing for you to start: the endpoint is served by the application itself.

## Their session, not yours

Do not save over their file and do not close their application. Say what you changed and that
**Edit - Edit History** lists it and **Edit - Regenerate History** rebuilds the model without it.
