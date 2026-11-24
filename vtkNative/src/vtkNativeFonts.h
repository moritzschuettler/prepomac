// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The font files the text of every view is drawn with, set once for the library by vtkn_set_font_files.
#pragma once

#include <vtkTextProperty.h>

#include <string>

namespace vtkn
{
    namespace Fonts
    {
        enum Style
        {
            Regular = 0,
            Bold = 1,
            Italic = 2,
            BoldItalic = 3
        };

        // UTF-8 paths; an empty one draws its style with the regular file
        void SetFiles(const std::string (&files)[4]);
        // The file of the style, or the Arial VTK embeds while no regular file is set
        void Apply(vtkTextProperty* property, bool bold, bool italic);
        // Counts the changes of the files, so that a text property kept across them can be renewed
        unsigned int GetGeneration();
    }
}
