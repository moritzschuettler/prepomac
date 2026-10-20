# PrePoMax for macOS - NetGenMesher, built against the OpenCASCADE and gmsh libraries of Homebrew
# (brew install opencascade gmsh). The app bundle carries these libraries in Contents/Frameworks.
cmake_minimum_required(VERSION 3.20)
project(NetGenMesher CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
if(NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Release)
endif()

set(HOMEBREW_PREFIX "/opt/homebrew" CACHE PATH "The Homebrew prefix with OpenCASCADE and gmsh")
set(OCC_INCLUDE_DIR "${HOMEBREW_PREFIX}/include/opencascade")
set(OCC_LIBRARIES
    TKernel TKMath TKG2d TKG3d TKGeomBase TKGeomAlgo TKBRep TKTopAlgo TKPrim TKBO TKBool TKFillet TKOffset
    TKShHealing TKMesh TKFeat TKHLR TKXSBase TKDE TKDESTEP TKDEIGES TKCDF TKLCAF TKCAF TKXCAF)

add_executable(NetGenMesher main.cpp Common.cpp Visualization.cpp Geometry.cpp Mesh.cpp Operations.cpp)
target_include_directories(NetGenMesher PRIVATE "${OCC_INCLUDE_DIR}" "${HOMEBREW_PREFIX}/include")
target_link_directories(NetGenMesher PRIVATE "${HOMEBREW_PREFIX}/lib")
target_link_libraries(NetGenMesher PRIVATE ${OCC_LIBRARIES} gmsh)
# The libraries are found next to the program in the app bundle, else in Homebrew
set_target_properties(NetGenMesher PROPERTIES
    BUILD_RPATH "${HOMEBREW_PREFIX}/lib"
    INSTALL_RPATH "@executable_path/../../Frameworks;@executable_path;${HOMEBREW_PREFIX}/lib")
