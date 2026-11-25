// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The picture of a view: RenderToPNG of vtkControl with its magnification and its transparent background, which
// is composited from two renders of the scene, one over black and one over white.
#include "vtkNativeError.h"
#include "vtkNativeView.h"

#include <vtkImageData.h>
#include <vtkNew.h>
#include <vtkPNGWriter.h>
#include <vtkPointData.h>
#include <vtkUnsignedCharArray.h>

#include <algorithm>

namespace vtkn
{
    namespace
    {
        // The three channels give the same opacity in exact arithmetic; their mean keeps a channel that rounded the
        // wrong way from tilting an edge
        int GetAlpha(const std::vector<uint8_t>& overBlack, const std::vector<uint8_t>& overWhite, size_t index)
        {
            int opacity = 0;
            for (size_t i = 0; i < 3; i++)
            {
                int transparency = std::max(0, overWhite[index + i] - overBlack[index + i]);
                opacity += 255 - transparency;
            }
            return (opacity + 1) / 3;
        }

        uint8_t Unpremultiply(uint8_t premultiplied, int alpha)
        {
            if (alpha <= 0) return 0;
            int value = (premultiplied * 255 + alpha / 2) / alpha;
            return static_cast<uint8_t>(std::min(255, value));
        }
    }

    // Port of RenderToPNG, WritePng, WriteTransparentPng and ComposePng. The renders of a capture are not meant for
    // the screen, so the buffer swap is off meanwhile and the scene is drawn once at the end
    void View::RenderToPng(const std::string& fileName, bool transparentBackground, int magnification)
    {
        magnification = std::max(1, std::min(magnification, 8));
        int swapBuffers = RenderWindow->GetSwapBuffers();
        RenderWindow->SetSwapBuffers(0);
        try
        {
            if (!transparentBackground) WritePng(fileName, magnification);
            else
            {
                double background[3];
                double background2[3];
                Renderer->GetBackground(background);
                Renderer->GetBackground2(background2);
                bool gradient = Renderer->GetGradientBackground();
                int width = 0;
                int height = 0;
                int whiteWidth = 0;
                int whiteHeight = 0;
                std::vector<uint8_t> overBlack;
                std::vector<uint8_t> overWhite;
                try
                {
                    Renderer->GradientBackgroundOff();
                    Renderer->SetBackground(0, 0, 0);
                    overBlack = ReadPixels(magnification, width, height);
                    Renderer->SetBackground(1, 1, 1);
                    overWhite = ReadPixels(magnification, whiteWidth, whiteHeight);
                }
                catch (...)
                {
                    Renderer->SetGradientBackground(gradient);
                    Renderer->SetBackground(background);
                    Renderer->SetBackground2(background2);
                    throw;
                }
                Renderer->SetGradientBackground(gradient);
                Renderer->SetBackground(background);
                Renderer->SetBackground2(background2);
                Require(whiteWidth == width && whiteHeight == height,
                        "The viewport changed size while the picture was being taken.");
                // The image keeps the rows of the render window, bottom up, as the PNG writer expects them
                vtkNew<vtkUnsignedCharArray> rgba;
                rgba->SetNumberOfComponents(4);
                rgba->SetNumberOfTuples(static_cast<vtkIdType>(width) * height);
                uint8_t* out = rgba->GetPointer(0);
                for (size_t i = 0; i < static_cast<size_t>(width) * height; i++)
                {
                    int alpha = GetAlpha(overBlack, overWhite, 3 * i);
                    for (size_t c = 0; c < 3; c++) out[4 * i + c] = Unpremultiply(overBlack[3 * i + c], alpha);
                    out[4 * i + 3] = static_cast<uint8_t>(alpha);
                }
                vtkNew<vtkImageData> image;
                image->SetDimensions(width, height, 1);
                image->GetPointData()->SetScalars(rgba);
                vtkNew<vtkPNGWriter> writer;
                writer->SetInputData(image);
                writer->SetFileName(fileName.c_str());
                writer->Write();
                if (writer->GetErrorCode() != 0)
                    throw Error(VTKN_ERROR_VTK, "The PNG file could not be written: " + fileName);
            }
        }
        catch (...)
        {
            RenderWindow->SetSwapBuffers(swapBuffers);
            RenderWindow->Render();
            throw;
        }
        RenderWindow->SetSwapBuffers(swapBuffers);
        RenderWindow->Render();
    }
}
