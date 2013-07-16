//Referencing https://github.com/EvilClosetMonkey/Fizbin.Kinect.Gestures/tree/master/Fizbin.Kinect.Gestures

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;

namespace KinectWithVRServer
{
    public class GestureController
    {
        private List<Gesture> gestures = new List<Gesture>();

        public GestureController()
        {
        }

        public event EventHandler<GestureEventArgs> GestureRecognized;

        public void UpdateAllGestures(Skeleton data)
        {
            foreach (Gesture gesture in this.gestures)
            {
                gesture.UpdateGesture(data);
            }
        }

        public void AddGesture(string name, IRelativeGestureSegment[] gestureDefinition)
        {
            Gesture gesture = new Gesture(name, gestureDefinition);
            gesture.GestureRecognized += OnGestureRecognized;
            this.gestures.Add(gesture);
        }

        private void OnGestureRecognized(object sender, GestureEventArgs e)
        {
            if (this.GestureRecognized != null)
            {
                this.GestureRecognized(this, e);
            }

            foreach (Gesture g in this.gestures)
            {
                g.Reset();
            }
        }
    }
}
