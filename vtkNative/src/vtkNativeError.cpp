// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeError.h"

namespace vtkn
{
    namespace
    {
        thread_local std::string lastError;
    }

    void SetLastError(const std::string& message)
    {
        lastError = message;
    }

    const std::string& GetLastError()
    {
        return lastError;
    }

    vtkn_status CopyOut(const std::string& text, char* buffer, int32_t capacity, int32_t* required)
    {
        Require(required != nullptr, "The argument 'required' must not be null.");
        size_t size = text.size() + 1;
        *required = static_cast<int32_t>(size);
        if (buffer == nullptr || capacity < static_cast<int32_t>(size)) return VTKN_ERROR_BUFFER_TOO_SMALL;
        std::memcpy(buffer, text.data(), text.size());
        buffer[text.size()] = '\0';
        return VTKN_OK;
    }
}
