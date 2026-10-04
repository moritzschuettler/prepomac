# lp_solve (vendored binary)

The mixed integer linear programming solver that settles the number of elements on every edge of a
mesh in one solve (the interval assignment), loaded by `GmshCaller.exe` through P/Invoke.

| File | Version | Notes |
|---|---|---|
| `lpsolve55.dll` | lp_solve 5.5.2.11 | x64, C runtime linked in statically, imports only `KERNEL32.dll` |
| `build_lpsolve55.bat` | | the command line it was built with |

Built from the lp_solve 5.5.2.11 sources carried by the QuadWild fork in
`libs/lpsolve/lp_solve_5.5`, with the flags of lp_solve's own `lpsolve55\cvc8msvcrt.bat` and the exports
of its `lp_solve.def`, the static C runtime (`/MT`) being the only change, so that no Visual C++
redistributable has to travel with it.

Upstream: https://sourceforge.net/projects/lpsolve - LGPL-2.1. The license text that must ship alongside
the binary is in `Licenses/lpsolve/`, not here.

The `PostBuildEvent` in `PrePoMax.csproj` copies the dll into `$(TargetDir)lib`, next to `GmshCaller.exe`.
