# HiGHS (vendored binary)

The mixed integer linear programming solver that settles the number of elements on every edge of a
mesh in one solve (the interval assignment) and chooses the way every cell of a compound is swept,
loaded by `GmshCaller.exe` and `PrePoMax.exe` through P/Invoke (`CaeMesh/Meshing/IntervalAssignment/Highs.cs`).

| File | Version | Notes |
|---|---|---|
| `highs.dll` | HiGHS 1.16.0, git f3f113d | x64, 32-bit `HighsInt`, imports `KERNEL32.dll` and the Visual C++ runtime (`MSVCP140.dll`, `VCRUNTIME140.dll`, `VCRUNTIME140_1.dll`), which `lib\gmsh-4.13.dll` needs as well |

The unmodified `bin\highs.dll` of `highs-1.16.0-x86_64-windows-mit.zip` (6,105,132 bytes, SHA-256
`e464ed05a8a209dba8f97163851fa7d3235f9167636305f50cd02adc28014545`) from
https://github.com/ERGO-Code/HiGHS/releases/tag/v1.16.0. `highs.exe`, `highs_extras.dll`, the headers and
the import libraries of the archive are not used. The C interface the wrapper declares is
`include/highs/interfaces/highs_c_api.h` of the same archive.

Upstream: https://github.com/ERGO-Code/HiGHS - MIT. The license text that ships alongside the binary is
in `Licenses/highs/`, not here.

The `PostBuildEvent` in `PrePoMax.csproj` copies the dll into `$(TargetDir)lib`, next to `GmshCaller.exe`.
