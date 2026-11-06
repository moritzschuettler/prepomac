#!/bin/bash
# Slice S1: the pick plan of slice C on the meshed solid and the hinge, the native picks resolved by the native
# helpers only. The output folder is $1.
bash "$(dirname "$0")/Run_Selection_Picks.sh" "${1:-$(cygpath -w "${TEMP:-/tmp}")\VtkPortS1}" femodel \
    solid_femodel hinge_femodel
