// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using vtkControl.Native;

namespace vtkControl
{
    // The native view in the window is the view of this control: its camera is the camera of this control, whose
    // views and zooms are the native ones, deferred so that this control animates from where the user is to the
    // camera they reach, and its pictures, videos and bounds are those of this control
    public partial class vtkControl
    {
        // Variables
        private bool _gradientBackground = true;
        private double[] _backgroundTop = new double[] { 0.8, 0.8, 0.8 };
        private double[] _backgroundBottom = new double[] { 0.4, 0.4, 0.4 };
        private bool _pointSmoothing = true;
        private bool _lineSmoothing = true;
        private double[] _scalarRange;


        // Properties
        private bool NativeShowsView { get { return _nativeMirror != null; } }


        // Methods
        // The port of PrepareActorLookupTable: the colors of the spectrum over the range, and below and above it the
        // min and max colors when the user range lies inside the scalar range; returns the range of the table
        private double[] GetLookupTableColors(double scalarRangeMin, double scalarRangeMax, out Color[] colors)
        {
            double min = scalarRangeMin;
            double max = scalarRangeMax;
            bool addMinColor = false;
            bool addMaxColor = false;
            int numOfColors = _colorSpectrum.Mode == vtkColorSpectrumMode.Smooth ? vtkMaxColorSpectrum.SmoothNumberOfColors
                                                                                 : _colorSpectrum.NumberOfColors;
            if (_colorSpectrum.MinMaxType == vtkColorSpectrumMinMaxType.Manual)
            {
                min = Math.Min(scalarRangeMin, _colorSpectrum.MinUserValue);
                max = Math.Max(scalarRangeMax, _colorSpectrum.MaxUserValue);
                if (_colorSpectrum.MinUserValue > min && max != min)
                {
                    addMinColor = true;
                    min = _colorSpectrum.MinUserValue;
                }
                if (_colorSpectrum.MaxUserValue < max && max != min)
                {
                    addMaxColor = true;
                    max = _colorSpectrum.MaxUserValue;
                }
            }
            double delta = (max - min) / numOfColors;
            if (addMinColor) min -= delta;
            if (addMaxColor) max += delta;
            //
            VtkNativeColorSpectrum spectrum = VtkNativeColorSpectrum.FromColorSpectrum(_colorSpectrum);
            List<Color> table = new List<Color>();
            if (addMinColor) table.Add(_colorSpectrum.MinColor);
            for (int i = 0; i < numOfColors; i++)
            {
                double[] rgb = spectrum.GetColor(i / (numOfColors - 1.0));
                table.Add(Color.FromArgb(255, (int)(rgb[0] * 255), (int)(rgb[1] * 255), (int)(rgb[2] * 255)));
            }
            if (addMaxColor) table.Add(_colorSpectrum.MaxColor);
            colors = table.ToArray();
            return new double[] { min, max };
        }
        // The bounds VTK gives when nothing is visible are inverted
        private double[] GetNativeBounds()
        {
            if (_nativeMirror == null) return new double[] { 1, -1, 1, -1, 1, -1 };
            return _nativeMirror.GetBounds() ?? new double[] { 1, -1, 1, -1, 1, -1 };
        }
        private void SaveNativeAnimationAsAVI(string fileName, int[] firstLastFrame, int step, int fps,
                                              bool scalarRangeFromAllFrames, bool swing, bool encoderOptions)
        {
            int width;
            int height;
            _nativeMirror.GetSize(out width, out height);
            using (AviWriter writer = new AviWriter())
            {
                if (!writer.Start(fileName, width, height, fps, 2, encoderOptions)) return;
                //
                List<int> frames = new List<int>();
                for (int i = firstLastFrame[0]; i <= firstLastFrame[1]; i += step) frames.Add(i);
                if (swing)
                {
                    for (int i = firstLastFrame[1]; i >= firstLastFrame[0]; i -= step) frames.Add(i);
                }
                foreach (int frame in frames)
                {
                    SetAnimationFrame(frame, scalarRangeFromAllFrames);
                    int frameWidth;
                    int frameHeight;
                    byte[] pixels = _nativeMirror.ReadPixels(1, out frameWidth, out frameHeight);
                    if (frameWidth != width || frameHeight != height)
                        throw new CaeGlobals.CaeException("The viewport changed size while the video was being saved.");
                    writer.Write(pixels);
                }
            }
        }
        private void RunNativeViewChange(bool animate, Action change, int timeMs = 500)
        {
            if (_nativeMirror == null)
            {
                _animating = false;
                return;
            }
            //
            if (_drawCameraPath) ClearOverlay();
            double[] start = _nativeMirror.GetCamera();
            _nativeMirror.SetViewChangeDeferred(true);
            try
            {
                change();
            }
            finally
            {
                _nativeMirror.SetViewChangeDeferred(false);
            }
            double[] end = _nativeMirror.GetCamera();
            if (animate && _viewAnimationEnabled) AnimateNativeCamera(start, end, timeMs);
            else
            {
                _animating = false;
                _nativeMirror.Render();
            }
            OnNativeCameraChanged();
        }
        private double[] GetNativeViewPlaneNormal()
        {
            double[] camera = _nativeMirror.GetCamera();
            double[] normal = new double[] { camera[0] - camera[3], camera[1] - camera[4], camera[2] - camera[5] };
            Normalize(normal);
            return normal;
        }
        // The rotation of a view transform: the right, the orthogonal up and the view plane normal as its rows
        private static double[][] GetViewRotation(double[] camera)
        {
            double[] normal = new double[] { camera[0] - camera[3], camera[1] - camera[4], camera[2] - camera[5] };
            Normalize(normal);
            double[] right = Cross(new double[] { camera[6], camera[7], camera[8] }, normal);
            Normalize(right);
            double[] up = Cross(normal, right);
            return new double[][] { right, up, normal };
        }
        private static double GetDistance(double[] camera)
        {
            double dx = camera[0] - camera[3];
            double dy = camera[1] - camera[4];
            double dz = camera[2] - camera[5];
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        private static double[] Cross(double[] a, double[] b)
        {
            return new double[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };
        }
        private static void Normalize(double[] v)
        {
            double length = Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
            if (length == 0) return;
            for (int i = 0; i < 3; i++) v[i] /= length;
        }
        private static double[] Slerp(double[] q1, double[] q2, double t)
        {
            double cosTheta = q1[0] * q2[0] + q1[1] * q2[1] + q1[2] * q2[2] + q1[3] * q2[3];
            double a;
            double b;
            if (cosTheta > 0.9995)
            {
                a = 1 - t;
                b = t;
            }
            else
            {
                double theta = Math.Acos(Math.Max(-1, Math.Min(1, cosTheta)));
                double sinTheta = Math.Sin(theta);
                a = Math.Sin((1 - t) * theta) / sinTheta;
                b = Math.Sin(t * theta) / sinTheta;
            }
            double[] q = new double[4];
            double length = 0;
            for (int i = 0; i < 4; i++)
            {
                q[i] = a * q1[i] + b * q2[i];
                length += q[i] * q[i];
            }
            length = Math.Sqrt(length);
            for (int i = 0; i < 4; i++) q[i] /= length;
            return q;
        }
        // The port of AnimateCamera on the camera of the native view: the orientation is interpolated with quaternions
        // around the focal point of the end camera, which is panned in display coordinates from where it is seen at
        // the start to where it is seen at the end
        private void AnimateNativeCamera(double[] cameraStart, double[] cameraEnd, int timeMs)
        {
            MirrorStyleAnimating(true);
            try
            {
                double[][] rEnd = GetViewRotation(cameraEnd);
                double[] fpEnd = new double[] { cameraEnd[3], cameraEnd[4], cameraEnd[5] };
                double zEnd = cameraEnd[9];
                double distanceEnd = GetDistance(cameraEnd);
                double[] q2 = QuaternionHelper.QuaternionFromMatrix3x3(rEnd);
                //
                _nativeMirror.SetCamera(cameraEnd);
                double[] fpEndXY = _nativeMirror.WorldToDisplay(fpEnd);
                _nativeMirror.SetCamera(cameraStart);
                double[] fpStartXY = _nativeMirror.WorldToDisplay(fpEnd);
                double[] fpEndWorld = _nativeMirror.DisplayToWorld(fpEndXY);
                double[] fpStartWorld = _nativeMirror.DisplayToWorld(fpStartXY);
                double[] start = cameraStart.ToArray();
                for (int i = 0; i < 3; i++)
                {
                    double delta = fpStartWorld[i] - fpEndWorld[i];
                    start[i] += delta;
                    start[3 + i] += delta;
                }
                double[][] rStart = GetViewRotation(start);
                double zStart = start[9];
                double distanceStart = GetDistance(start);
                double[] q1 = QuaternionHelper.QuaternionFromMatrix3x3(rStart);
                if (q1.Concat(q2).Any(double.IsNaN)) throw new NotSupportedException();
                if (q1[0] * q2[0] + q1[1] * q2[1] + q1[2] * q2[2] + q1[3] * q2[3] < 0)
                {
                    for (int i = 0; i < q2.Length; i++) q2[i] = -q2[i];
                }
                bool sameOrientation = Math.Abs(q1[0] - q2[0]) + Math.Abs(q1[1] - q2[1]) + Math.Abs(q1[2] - q2[2]) +
                                       Math.Abs(q1[3] - q2[3]) < 0.00001;
                //
                _positions = new List<double[]>();
                _focalPoints = new List<double[]>();
                DateTime startTime = DateTime.Now;
                double currentDelta;
                double[] frame = new double[10];
                do
                {
                    currentDelta = (DateTime.Now - startTime).TotalMilliseconds;
                    double t = Math.Min(1, currentDelta / timeMs);
                    double[] q = sameOrientation ? q1 : Slerp(q1, q2, t);
                    double z = zStart + t * (zEnd - zStart);
                    double distance = distanceStart + t * (distanceEnd - distanceStart);
                    double[][] r = QuaternionHelper.Matrix3x3FromQuaternion(q);
                    for (int i = 0; i < 3; i++)
                    {
                        frame[i] = fpEnd[i] + distance * r[2][i];
                        frame[3 + i] = fpEnd[i];
                        frame[6 + i] = r[1][i];
                    }
                    frame[9] = z;
                    _nativeMirror.SetCamera(frame);
                    double[] fpXY = QuaternionHelper.VectorLerp(fpStartXY, fpEndXY, t);
                    fpEndWorld = _nativeMirror.DisplayToWorld(fpEndXY);
                    fpStartWorld = _nativeMirror.DisplayToWorld(fpXY);
                    for (int i = 0; i < 3; i++)
                    {
                        double delta = fpEndWorld[i] - fpStartWorld[i];
                        frame[i] += delta;
                        frame[3 + i] += delta;
                    }
                    _nativeMirror.SetViewParameters(frame);
                    OnNativeCameraChanged();
                    //
                    if (_drawCameraPath)
                    {
                        _positions.Add(new double[] { frame[0], frame[1], frame[2] });
                        _focalPoints.Add(new double[] { frame[3], frame[4], frame[5] });
                    }
                    Application.DoEvents();
                }
                while (currentDelta < timeMs);
                _nativeMirror.SetViewParameters(cameraEnd);
            }
            finally
            {
                _animating = false;
                MirrorStyleAnimating(false);
                if (_drawCameraPath) DrawCameraPath();
            }
        }
    }
}
