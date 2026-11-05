// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The thread local last error and the guard that turns every C++ exception into a status code.
#pragma once

#include "vtkNative.h"

#include <cstring>
#include <stdexcept>
#include <string>

namespace vtkn
{
    class Error : public std::runtime_error
    {
    public:
        Error(vtkn_status status, const std::string& message) : std::runtime_error(message), Status(status) {}
        vtkn_status Status;
    };

    void SetLastError(const std::string& message);
    const std::string& GetLastError();

    // Copies text with its terminating zero into a caller buffer, following the required size convention
    vtkn_status CopyOut(const std::string& text, char* buffer, int32_t capacity, int32_t* required);

    template <typename Function>
    vtkn_status Guard(Function function)
    {
        try
        {
            SetLastError(std::string());
            return function();
        }
        catch (const Error& error)
        {
            SetLastError(error.what());
            return error.Status;
        }
        catch (const std::exception& exception)
        {
            SetLastError(std::string("Internal error: ") + exception.what());
            return VTKN_ERROR_INTERNAL;
        }
        catch (...)
        {
            SetLastError("Internal error of an unknown kind.");
            return VTKN_ERROR_INTERNAL;
        }
    }

    inline void Require(bool condition, const char* message)
    {
        if (!condition) throw Error(VTKN_ERROR_INVALID_ARGUMENT, message);
    }
}
