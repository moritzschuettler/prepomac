// PrePoMax for macOS - NetGenMesher: the command dispatcher. The first argument is the command, the others are its
// arguments; the exit code is 0 when the command succeeded.
#include "Common.h"

#include <Standard_Failure.hxx>

#include <clocale>
#include <exception>
#include <iostream>

namespace ngm
{
    static std::map<std::string, Command>& Commands()
    {
        static std::map<std::string, Command> commands;
        return commands;
    }
    void RegisterCommand(const std::string& name, Command command) { Commands()[name] = command; }
}

int main(int argc, char** argv)
{
    // Numbers are read and written with a decimal point whatever the language of the user
    std::setlocale(LC_ALL, "C");
    ngm::RegisterGeometryCommands();
    ngm::RegisterMeshCommands();
    ngm::RegisterOperationCommands();
    ngm::RegisterFaceCommands();
    ngm::RegisterEditCommands();
    if (argc < 2)
    {
        std::cerr << "Usage: NetGenMesher <command> <arguments>" << std::endl;
        return 1;
    }
    std::string name = argv[1];
    std::vector<std::string> args(argv + 2, argv + argc);
    auto it = ngm::Commands().find(name);
    if (it == ngm::Commands().end())
    {
        ngm::Error("The command " + name + " is not supported by the macOS NetGenMesher.");
        return 2;
    }
    try
    {
        return it->second(args);
    }
    catch (const Standard_Failure& failure)
    {
        ngm::Error(name + " failed: " + std::string(failure.GetMessageString() ? failure.GetMessageString() : "OpenCASCADE error"));
    }
    catch (const std::exception& ex)
    {
        ngm::Error(name + " failed: " + ex.what());
    }
    catch (...)
    {
        ngm::Error(name + " failed.");
    }
    return 1;
}
