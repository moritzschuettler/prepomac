// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The integer rectangle of System.Drawing.Rectangle, with its semantics: Right and Bottom are exclusive, Contains
// tests the half open box, and the empty rectangle is all zeros. The widgets compute their hit and highlight
// boxes with it exactly as the C# widgets did.
#pragma once

#include <algorithm>

namespace vtkn
{
    struct Rect
    {
        int X = 0;
        int Y = 0;
        int Width = 0;
        int Height = 0;

        Rect() = default;
        Rect(int x, int y, int width, int height) : X(x), Y(y), Width(width), Height(height) {}

        static Rect FromLTRB(int left, int top, int right, int bottom)
        {
            return Rect(left, top, right - left, bottom - top);
        }
        static Rect Union(const Rect& a, const Rect& b)
        {
            int left = std::min(a.X, b.X);
            int top = std::min(a.Y, b.Y);
            int right = std::max(a.Right(), b.Right());
            int bottom = std::max(a.Bottom(), b.Bottom());
            return FromLTRB(left, top, right, bottom);
        }

        int Left() const { return X; }
        int Top() const { return Y; }
        int Right() const { return X + Width; }
        int Bottom() const { return Y + Height; }
        bool IsEmpty() const { return X == 0 && Y == 0 && Width == 0 && Height == 0; }
        bool Contains(int x, int y) const { return X <= x && x < X + Width && Y <= y && y < Y + Height; }
        void Inflate(int dx, int dy)
        {
            X -= dx;
            Y -= dy;
            Width += 2 * dx;
            Height += 2 * dy;
        }
        bool operator==(const Rect& other) const
        {
            return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        }
        bool operator!=(const Rect& other) const { return !(*this == other); }
    };
}
