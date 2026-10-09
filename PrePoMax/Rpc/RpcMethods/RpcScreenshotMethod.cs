// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.IO;

namespace PrePoMax.Rpc
{
    // A picture of the viewport. It has to be served from inside the application: the viewport is an
    // OpenGL child window, so nothing outside the process can capture it, and only the renderer can
    // read its own buffer. The image is written to a file and the path is returned, because an image
    // does not belong in a JSON answer; the MCP endpoint attaches the picture itself as an image block.
    // A relative file name is taken in the temporary folder rather than in the working directory of
    // the process, which a caller cannot know. A folder the file name points into is created when it is
    // missing, the way the temporary folder is, because a caller collecting pictures for a report
    // names the folder before anything has been written to it. The two options are the ones the
    // capture dialog offers the user, served by the same renderer call.
    public static class RpcScreenshotMethod
    {
        // Variables                                                                                                                
        // The arguments the method takes; any other is refused before anything is done
        public static readonly string[] Parameters = { "file_name", "transparent_background", "magnification" };
        private const int MaxMagnification = 4;


        // Static methods                                                                                                           
        public static string Execute(RpcRequest request, RpcServer server)
        {
            string fileName = request.GetString("file_name", null);
            if (fileName == null) fileName = GetTemporaryFileName();
            else if (Path.GetExtension(fileName).ToLower() != ".png")
                throw new RpcException(RpcErrorCode.InvalidParams, "A screenshot is written as a .png file.");
            else if (!Path.IsPathRooted(fileName)) fileName = Path.Combine(GetTemporaryDirectory(), fileName);
            //
            bool transparentBackground = request.GetBool("transparent_background", false);
            int magnification = request.GetInt("magnification", 1);
            if (magnification < 1 || magnification > MaxMagnification)
                throw new RpcException(RpcErrorCode.InvalidParams, "The magnification must be from 1 to " +
                                                                    MaxMagnification + ".");
            //
            string directory = Path.GetDirectoryName(fileName);
            if (directory != null && directory.Length > 0 && !Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception exception)
                {
                    throw new RpcException(RpcErrorCode.InvalidParams, "The directory " + directory +
                                                                       " does not exist and could not be created: " +
                                                                       exception.Message);
                }
            }
            //
            server.Form.SaveAutomationScreenshot(fileName, transparentBackground, magnification);
            //
            long length = File.Exists(fileName) ? new FileInfo(fileName).Length : 0;
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("fileName", fileName);
            writer.WriteValue("length", (double)length);
            writer.WriteValue("isEmpty", length == 0);
            writer.EndObject();
            return writer.ToString();
        }
        private static string GetTemporaryFileName()
        {
            string directory = GetTemporaryDirectory();
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            return Path.Combine(directory, "viewport_" + Guid.NewGuid().ToString("N") + ".png");
        }
        private static string GetTemporaryDirectory()
        {
            return Path.Combine(Path.GetTempPath(), "PrePoMax");
        }
    }
}
