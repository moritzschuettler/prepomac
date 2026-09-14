# CalculiX crashes on an *EQUATION between the nodes of two rigid bodies

`RigidBodyEquation_Crash.inp` makes ccx exit with a segmentation fault and no message.
`RigidBodyEquation_Solves.inp` is the same model with the three `*Equation` lines removed and solves.
The two files differ in nothing else, so the diff between them is the whole report.

## The model

Two bars of four C3D8 elements each, 1 x 1 in cross section, lying along x and separated by a gap. A
`*RIGID BODY` covers the four nodes of the facing end face of each bar; the reference and rotation
nodes sit between the bars. Both bars are clamped at their far ends, the far end of the second one by
a prescribed displacement, so the model is fully constrained with or without the equation. The single
equation ties the two reference nodes in the global x direction:

```
*Rigid body, Nset=MasterFace, Ref node=1001, Rot node=1002
*Rigid body, Nset=SlaveFace,  Ref node=1003, Rot node=1004
*Equation
2
1003, 1, 1, 1001, 1, -1
```

## What happens

The run ends about a second after

```
 Nonlinear geometric effects are taken into account
```

with no `*ERROR` line, an empty `.sta` file and no results. Under Git Bash the shell reports
`Segmentation fault` and exit code 139. Removing only the three `*Equation` lines makes the same model
solve to completion.

Reproduced with:

| build | result |
| --- | --- |
| ccx 2.22 `ccx_dynamic` | segmentation fault |
| ccx 2.22 `ccx_static` | segmentation fault |
| ccx 2.21 `ccx_dynamic` | segmentation fault |
| ccx 2.22 with `*Static, Solver=Pardiso` | segmentation fault |

The Fortran output is block buffered when it is redirected, so the last lines can be lost. Set
`GFORTRAN_UNBUFFERED_ALL=y` to see how far the run really got.

## What decides whether a deck survives

Editing one deck of a larger model one equation at a time gave two conditions. An equation that ties
nodes of two rigid bodies survives only when

- it has **more than two terms**, and
- two such equations do not use each other's dependent degree of freedom.

Break either and the run crashes the same way. The conditions are structural, not mechanical:

- Replacing one four term equation by the two term equation that imposes the same constraint crashes.
- Adding a term whose coefficient is `1E-9` to a two term equation makes it solve. Writing that term
  with the coefficient `0` crashes again, because the zero term is dropped before it is used.
- Scaling every coefficient of a working equation by 2 solves, so the values themselves do not matter.

## Why this looks like a defect rather than unsupported input

The manual recommends this construct. The `*RIGID BODY` page says that to remove an undefined rigid
body rotation the user should specify that the rotation about the axis is zero by means of an
`*EQUATION` card of the form `a . u_R = 0`, where `u_R` is the displacement of the ROT NODE. So an
equation on the nodes of a rigid body is documented usage, and ccx does handle the same constraint
correctly whenever the equation happens to be written with four or six terms.

## Where it appears to go wrong

This section is inference from the ccx 2.21 sources, not a debugger session.

`rigidmpc.f` writes three MPCs per rigid body node, each of six terms in a fixed order: the node's own
degree of freedom, the same degree of freedom of the reference node, all three degrees of freedom of
the rotation node, and an internal node that is held at zero by an SPC of type `R`.

`nonlinmpc.f` walks each `RIGID` MPC by position and assumes exactly that shape. Having stepped to
what it believes is the sixth term it does

```fortran
idof=8*(nodempc(1,index)-1)+nodempc(2,index)
call nident(ikboun,idof,nboun,id)
xbounact(ilboun(id))= ...
```

`nident` returns an insertion position, and `ilboun(id)` is used without first testing
`ikboun(id).eq.idof`.

An `*EQUATION` whose dependent degree of freedom is a reference or rotation node makes that node
dependent while it is independent in every MPC of its rigid body, so `cascade` substitutes one into
the other. It says so itself:

```
 *INFO in cascade: linear MPCs and nonlinear MPCs depend on each other
```

After the substitution the term count and the order of the MPC are no longer what the walk assumes,
`id` can be zero or point at an unrelated SPC, and the read of `ilboun(id)` and the write to
`xbounact` leave their arrays. That matches the symptom: the crash comes after decascading and before
the first increment, and it depends on how many terms the equation has rather than on anything
mechanical.

## How PrePoMax avoids it

A joint constrains two surfaces through a pair of rigid bodies. Where a relative motion is locked in
all three directions the two rigid bodies are given the same node rather than an equation saying their
nodes are equal, which is both a stronger statement of the constraint and one that leaves ccx nothing
to cascade. A partially locked direction still needs equations, and where the joint axis lies along a
global axis those equations cannot be given a third meaningful term, because the plane perpendicular
to the axis spans only two degrees of freedom. They therefore carry two degrees of freedom of a node
the writer holds at zero, which is worth nothing mathematically and gives the equation the length ccx
needs.
