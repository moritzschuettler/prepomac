#!/bin/bash
# Slice S3: the pick plan in the geometry selection modes (geometry, vertex, edge, surface, edge angle, surface angle,
# part) on the geometry view of the solid, the native picks resolved by the native helpers only. The output folder
# is $1.
bash "$(dirname "$0")/Run_Selection_Picks.sh" "${1:-$(cygpath -w "${TEMP:-/tmp}")\VtkPortS3}" geometry solid_geometry
