// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The font files the text of every view is drawn with. VTK hands the path of a font file to FreeType, which opens
// it with fopen, so on Windows every path is stored in a form that is plain ASCII. Elsewhere fopen takes the UTF-8
// path as it is.
#include "vtkNativeFonts.h"
#include "vtkNativeError.h"

#if defined(_WIN32)
#  ifndef NOMINMAX
#    define NOMINMAX
#  endif
#  include <windows.h>
#else
#  include <sys/stat.h>
#endif

namespace vtkn
{
    namespace
    {
        std::string Files[4];
        unsigned int Generation = 0;

#if defined(_WIN32)
        std::wstring ToWide(const std::string& text)
        {
            int length = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, text.c_str(), -1, nullptr, 0);
            Require(length > 0, "A font file path is not valid UTF-8.");
            std::wstring wide(static_cast<size_t>(length), L'\0');
            MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, text.c_str(), -1, &wide[0], length);
            wide.resize(static_cast<size_t>(length - 1));
            return wide;
        }

        bool IsAscii(const std::wstring& text)
        {
            for (wchar_t character : text)
            {
                if (character > 127) return false;
            }
            return true;
        }

        std::string ToFontPath(const std::string& utf8Path)
        {
            if (utf8Path.empty()) return std::string();
            std::wstring path = ToWide(utf8Path);
            DWORD attributes = GetFileAttributesW(path.c_str());
            if (attributes == INVALID_FILE_ATTRIBUTES || (attributes & FILE_ATTRIBUTE_DIRECTORY) != 0)
                throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The font file does not exist: " + utf8Path);
            if (!IsAscii(path))
            {
                DWORD length = GetShortPathNameW(path.c_str(), nullptr, 0);
                std::wstring shortPath(length, L'\0');
                if (length == 0 || GetShortPathNameW(path.c_str(), &shortPath[0], length) == 0)
                    throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The font file has no short path: " + utf8Path);
                shortPath.resize(length - 1);
                if (!IsAscii(shortPath))
                    throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The font file path is not plain ASCII: " + utf8Path);
                path = shortPath;
            }
            return std::string(path.begin(), path.end());
        }
#else
        std::string ToFontPath(const std::string& utf8Path)
        {
            if (utf8Path.empty()) return std::string();
            struct stat status;
            if (stat(utf8Path.c_str(), &status) != 0 || !S_ISREG(status.st_mode))
                throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The font file does not exist: " + utf8Path);
            return utf8Path;
        }
#endif
    }

    void Fonts::SetFiles(const std::string (&files)[4])
    {
        std::string paths[4];
        for (int i = 0; i < 4; i++) paths[i] = ToFontPath(files[i]);
        for (int i = 0; i < 4; i++) Files[i] = paths[i];
        Generation++;
    }

    unsigned int Fonts::GetGeneration()
    {
        return Generation;
    }

    void Fonts::Apply(vtkTextProperty* property, bool bold, bool italic)
    {
        const std::string& styleFile = Files[(bold ? Bold : Regular) | (italic ? Italic : Regular)];
        const std::string& file = styleFile.empty() ? Files[Regular] : styleFile;
        if (file.empty()) property->SetFontFamilyToArial();
        else
        {
            property->SetFontFamily(VTK_FONT_FILE);
            property->SetFontFile(file.c_str());
        }
        property->SetBold(bold);
        property->SetItalic(italic);
    }
}
