// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;


namespace vtkControl
{
    internal class TrackpadGestureRecognizer
    {
        // Variables
        // The contacts carry a small amount of jitter - the tracked quantities are filtered instead of
        // the deltas so that the smoothing lags behind the fingers without losing any of the motion
        private const double _smoothing = 0.6;
        private const double _minSpreadMm = 5;
        // The contacts need a few reports to settle after the second finger lands, and a tracking
        // glitch shows up as a jump no finger could have made - both rebase instead of moving the camera
        private const int _settleReports = 3;
        private const double _maxReportTravelMm = 8;
        //
        private bool _active;
        private int _reportsSinceStart;
        private int _firstId;
        private int _secondId;
        private double _centroidX;
        private double _centroidY;
        private double _spread;


        // Properties
        public bool Active { get { return _active; } }


        // Events
        public event Action GestureStarted;
        public event Action<double, double, double> GestureUpdated;
        public event Action GestureEnded;


        // Constructors
        public TrackpadGestureRecognizer()
        {
            _active = false;
        }


        // Methods
        public void Update(TouchContact[] contacts)
        {
            if (contacts == null || contacts.Length != 2)
            {
                End();
                return;
            }
            //
            TouchContact first = contacts[0].Id <= contacts[1].Id ? contacts[0] : contacts[1];
            TouchContact second = contacts[0].Id <= contacts[1].Id ? contacts[1] : contacts[0];
            //
            double centroidX = (first.X + second.X) / 2;
            double centroidY = (first.Y + second.Y) / 2;
            double spread = Math.Sqrt(Math.Pow(second.X - first.X, 2) + Math.Pow(second.Y - first.Y, 2));
            if (spread < _minSpreadMm) spread = _minSpreadMm;
            // A lifted and replaced finger restarts the gesture so that the change of contacts is not
            // mistaken for a very large movement
            if (!_active || first.Id != _firstId || second.Id != _secondId)
            {
                bool wasActive = _active;
                _firstId = first.Id;
                _secondId = second.Id;
                _centroidX = centroidX;
                _centroidY = centroidY;
                _spread = spread;
                _active = true;
                _reportsSinceStart = 0;
                if (!wasActive && GestureStarted != null) GestureStarted();
                return;
            }
            //
            _reportsSinceStart++;
            double travelX = centroidX - _centroidX;
            double travelY = centroidY - _centroidY;
            if (_reportsSinceStart <= _settleReports ||
                Math.Sqrt(travelX * travelX + travelY * travelY) > _maxReportTravelMm)
            {
                _centroidX = centroidX;
                _centroidY = centroidY;
                _spread = spread;
                return;
            }
            //
            double previousX = _centroidX;
            double previousY = _centroidY;
            double previousSpread = _spread;
            //
            _centroidX += _smoothing * (centroidX - _centroidX);
            _centroidY += _smoothing * (centroidY - _centroidY);
            _spread += _smoothing * (spread - _spread);
            //
            if (GestureUpdated != null)
                GestureUpdated(_centroidX - previousX, _centroidY - previousY, _spread / previousSpread);
        }
        public void End()
        {
            if (!_active) return;
            //
            _active = false;
            if (GestureEnded != null) GestureEnded();
        }
    }
}
