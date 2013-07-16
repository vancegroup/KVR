using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;

namespace KinectWithVRServer
{
    public class GestureEventArgs : EventArgs
    {
        public GestureEventArgs(string name, int trackingId)
        {
            this.TrackingId = trackingId;
            this.GestureName = name;
        }

        public string GestureName { get; set; }

        public int TrackingId { get; set; }
    }
}
