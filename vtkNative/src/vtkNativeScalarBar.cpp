// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
#include "vtkNativeScalarBar.h"
#include "vtkNativeError.h"

#include <mutex>
#include <set>

namespace vtkn
{
    namespace
    {
        std::mutex registryMutex;
        std::set<ScalarBar*> liveScalarBars;
    }

    ScalarBar::ScalarBar(View* owner, int id) : Owner(owner), Id(id)
    {
        std::lock_guard<std::mutex> lock(registryMutex);
        liveScalarBars.insert(this);
    }

    ScalarBar::~ScalarBar()
    {
        std::lock_guard<std::mutex> lock(registryMutex);
        liveScalarBars.erase(this);
    }

    ScalarBar* ScalarBar::FromHandle(vtkn_scalar_bar handle)
    {
        ScalarBar* bar = static_cast<ScalarBar*>(handle);
        std::lock_guard<std::mutex> lock(registryMutex);
        if (handle == nullptr || liveScalarBars.find(bar) == liveScalarBars.end())
            throw Error(VTKN_ERROR_INVALID_HANDLE, "The scalar bar handle is not a live scalar bar.");
        return bar;
    }
}
