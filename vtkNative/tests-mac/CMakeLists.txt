# PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#
# The macOS smoke test of vtkNative: an off screen view and an external view drawn with a CGL context and a
# framebuffer of the test, each written to PNG files. Build vtkNative into vtkNative/build-mac first, then
#
#   cmake -S vtkNative/tests-mac -B vtkNative/tests-mac/build -G Ninja -DCMAKE_BUILD_TYPE=Release
#   cmake --build vtkNative/tests-mac/build
#   vtkNative/tests-mac/build/vtknative_test vtkNative/tests-mac/build
cmake_minimum_required(VERSION 3.21)
project(vtkNativeTestsMac LANGUAGES C)

set(VTKNATIVE_BUILD_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../build-mac" CACHE PATH "The build folder of vtkNative")
find_library(VTKNATIVE_LIBRARY vtkNative PATHS "${VTKNATIVE_BUILD_DIR}" NO_DEFAULT_PATH REQUIRED)
find_package(ZLIB REQUIRED)

add_executable(vtknative_test vtknative_test.c)
target_include_directories(vtknative_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}/../include")
target_compile_definitions(vtknative_test PRIVATE GL_SILENCE_DEPRECATION)
target_link_libraries(vtknative_test PRIVATE "${VTKNATIVE_LIBRARY}" ZLIB::ZLIB "-framework OpenGL")
set_target_properties(vtknative_test PROPERTIES BUILD_RPATH "${VTKNATIVE_BUILD_DIR}")
