# PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#
# Copies vtkNative.dll and the DLLs it loads from SEARCH_DIR - the VTK runtime - into OUTPUT_DIR. The folder is
# emptied first, so a module that is no longer needed does not stay behind.
if(POLICY CMP0207)
    cmake_policy(SET CMP0207 NEW)
endif()
file(REMOVE_RECURSE "${OUTPUT_DIR}")
file(MAKE_DIRECTORY "${OUTPUT_DIR}")
file(GET_RUNTIME_DEPENDENCIES
    LIBRARIES "${LIBRARY}"
    DIRECTORIES "${SEARCH_DIR}"
    RESOLVED_DEPENDENCIES_VAR resolved
    UNRESOLVED_DEPENDENCIES_VAR unresolved
    PRE_EXCLUDE_REGEXES "^api-ms-.*" "^ext-ms-.*")
file(COPY "${LIBRARY}" DESTINATION "${OUTPUT_DIR}")
# Only the VTK runtime travels; the system and C++ runtime DLLs are resolved from the machine
set(count 0)
foreach(dependency IN LISTS resolved)
    cmake_path(IS_PREFIX SEARCH_DIR "${dependency}" NORMALIZE in_search_dir)
    if(in_search_dir)
        file(COPY "${dependency}" DESTINATION "${OUTPUT_DIR}")
        math(EXPR count "${count} + 1")
    endif()
endforeach()
message(STATUS "vtkNative runtime: ${count} VTK DLLs copied to ${OUTPUT_DIR}")
