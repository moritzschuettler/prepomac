How to chanage the Gmsh version

1.) Download the latest Gmsh SDK and extract it
2.) Copy the extracted folder to: ..\GmshCommon\GmshLib\deps
3.) You can remove the folder deps\gmsh-#.#.#-Windows64-sdk\bin
4.) In the \gmsh-#.#.#-Windows64-sdk\include folder swithch the files:
    gmsh.h → gmsh.h_original
    gmsh.h_cwrap → gmsh.h
5.) In the GmshCommon project properties replace the Gmsh folder names in:
    Configuration Properties → C/C++ → General → Additional Include Directories
    Configuration Properties → Linker → General → Additional Include Directories
6.) Copy the Gmsh-#.#.dll to the ..\PrePoMax\bin\x64\Debug\lib and ..\PrePoMax\bin\x64\Release\lib folders


